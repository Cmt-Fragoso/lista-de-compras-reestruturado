using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseDataGrid : DataGridView
    {
        #region Fields
        private bool _isLoading;
        private string _loadingText = "Carregando...";
        private string _emptyText = "Nenhum dado encontrado";
        private readonly Timer _sortTimer;
        private readonly object _lockObject = new object();
        private int _virtualItemCount;
        private int _pageSize = 100;
        private int _currentPage;
        private bool _isVirtualMode;
        private readonly Dictionary<int, object> _cachedRows;
        private readonly Queue<int> _cacheQueue;
        private const int MaxCacheSize = 1000;
        private CancellationTokenSource _loadingCts;
        private bool _isPaging;
        private IList<object> _sourceData;
        #endregion

        #region Events
        public event EventHandler<int> PageChanged;
        public event EventHandler<DataGridViewCellEventArgs> VirtualCellValueNeeded;
        public event EventHandler<int> LoadPageRequested;
        #endregion

        #region Constructor
        public BaseDataGrid()
        {
            InitializeGrid();
            InitializeSort();
            SubscribeToTheme();

            _cachedRows = new Dictionary<int, object>();
            _cacheQueue = new Queue<int>();
            _sortTimer = new Timer { Interval = 300 };
            _sortTimer.Tick += HandleDelayedSort;
        }
        #endregion

        #region Properties
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    if (value)
                    {
                        _loadingCts?.Cancel();
                        _loadingCts = new CancellationTokenSource();
                    }
                    Invalidate();
                }
            }
        }

        public bool IsPaging
        {
            get => _isPaging;
            set
            {
                if (_isPaging != value)
                {
                    _isPaging = value;
                    if (value)
                    {
                        ScrollBars = ScrollBars.Both;
                        _currentPage = 0;
                    }
                    RefreshData();
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value > 0 && _pageSize != value)
                {
                    _pageSize = value;
                    if (_isPaging)
                    {
                        RefreshData();
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private void InitializeGrid()
        {
            AutoGenerateColumns = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToOrderColumns = true;
            AllowUserToResizeRows = false;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.Single;
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            EnableHeadersVisualStyles = false;
            MultiSelect = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            Font = ThemeManager.Instance.GetFont();
            
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint, true);
                    
            DoubleBuffered = true;
        }

        private void HandleDelayedSort(object sender, EventArgs e)
        {
            _sortTimer.Stop();
            if (SortedColumn != null)
            {
                BeginInvoke(new Action(() => 
                {
                    Sort(SortedColumn, SortOrder);
                }));
            }
        }

        protected override void OnScroll(ScrollEventArgs e)
        {
            base.OnScroll(e);

            if (_isPaging && e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                if (NearBottom() && _currentPage * _pageSize < _virtualItemCount)
                {
                    LoadNextPage();
                }
            }
        }

        private bool NearBottom()
        {
            int displayedRows = DisplayedRowCount(false);
            int firstDisplayed = FirstDisplayedScrollingRowIndex;
            return (firstDisplayed + displayedRows) >= RowCount - 5;
        }

        private async void LoadNextPage()
        {
            if (IsLoading) return;

            IsLoading = true;
            _currentPage++;

            try
            {
                LoadPageRequested?.Invoke(this, _currentPage);
                await LoadPageDataAsync(_currentPage, _loadingCts.Token);
                PageChanged?.Invoke(this, _currentPage);
            }
            catch (OperationCanceledException)
            {
                // Carregamento cancelado
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPageDataAsync(int page, CancellationToken ct)
        {
            if (_sourceData == null) return;

            int startIndex = page * _pageSize;
            int endIndex = Math.Min(startIndex + _pageSize, _sourceData.Count);

            var pageData = _sourceData.Skip(startIndex).Take(_pageSize).ToList();

            await Task.Run(() =>
            {
                for (int i = 0; i < pageData.Count && !ct.IsCancellationRequested; i++)
                {
                    AddToCache(startIndex + i, pageData[i]);
                }
            }, ct);

            if (!ct.IsCancellationRequested)
            {
                BeginInvoke(new Action(() => RefreshDisplay()));
            }
        }

        private void AddToCache(int index, object data)
        {
            lock (_lockObject)
            {
                if (_cachedRows.Count >= MaxCacheSize)
                {
                    int oldestIndex = _cacheQueue.Dequeue();
                    _cachedRows.Remove(oldestIndex);
                }

                _cachedRows[index] = data;
                _cacheQueue.Enqueue(index);
            }
        }

        private void ClearCache()
        {
            lock (_lockObject)
            {
                _cachedRows.Clear();
                _cacheQueue.Clear();
            }
        }

        protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
        {
            if (_isVirtualMode)
            {
                object value = null;
                lock (_lockObject)
                {
                    if (_cachedRows.TryGetValue(e.RowIndex, out object rowData))
                    {
                        var property = rowData.GetType().GetProperty(Columns[e.ColumnIndex].DataPropertyName);
                        if (property != null)
                        {
                            value = property.GetValue(rowData);
                        }
                    }
                }

                e.Value = value;
                VirtualCellValueNeeded?.Invoke(this, new DataGridViewCellEventArgs(e.ColumnIndex, e.RowIndex));
            }
            else
            {
                base.OnCellValueNeeded(e);
            }
        }

        public async Task LoadDataAsync<T>(IList<T> data, bool useVirtualization = true)
        {
            if (data == null) return;

            IsLoading = true;
            ClearCache();

            try
            {
                _sourceData = data.Cast<object>().ToList();
                _virtualItemCount = data.Count;

                if (useVirtualization && data.Count > 1000)
                {
                    _isVirtualMode = true;
                    VirtualMode = true;
                    RowCount = _virtualItemCount;
                    await LoadPageDataAsync(0, _loadingCts.Token);
                }
                else
                {
                    _isVirtualMode = false;
                    VirtualMode = false;
                    DataSource = new BindingList<T>(data.ToList());
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshDisplay()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshDisplay));
                return;
            }

            Invalidate();
            Update();
        }

        public void AddColumn(string name, string headerText, string dataPropertyName, 
            int width = 100, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft,
            bool sortable = true)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                DataPropertyName = dataPropertyName,
                Width = width,
                DefaultCellStyle = { Alignment = alignment },
                SortMode = sortable ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable
            };

            Columns.Add(column);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (RowCount == 0 || _isLoading)
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                using (var brush = new SolidBrush(theme.TextSecondary))
                {
                    var text = _isLoading ? _loadingText : _emptyText;
                    var size = e.Graphics.MeasureString(text, Font);
                    var point = new PointF(
                        (Width - size.Width) / 2,
                        (Height - size.Height) / 2);
                    e.Graphics.DrawString(text, Font, brush, point);
                }
            }
        }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            if (!_sortTimer.Enabled && RowCount > 0)
            {
                _sortTimer.Start();
            }
        }

        protected override void Sort(DataGridViewColumn dataGridViewColumn, ListSortDirection direction)
        {
            if (_isVirtualMode)
            {
                // Para modo virtual, ordena os dados fonte e recarrega
                var prop = dataGridViewColumn.DataPropertyName;
                var sorted = direction == ListSortDirection.Ascending ?
                    _sourceData.OrderBy(r => GetPropertyValue(r, prop)) :
                    _sourceData.OrderByDescending(r => GetPropertyValue(r, prop));

                _sourceData = sorted.ToList();
                ClearCache();
                LoadPageDataAsync(0, _loadingCts.Token).ConfigureAwait(false);
            }
            else
            {
                base.Sort(dataGridViewColumn, direction);
            }
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sortTimer?.Dispose();
                _loadingCts?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}