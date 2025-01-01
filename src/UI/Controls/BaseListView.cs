using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseListView : ListView
    {
        #region Fields
        private bool _isLoading;
        private string _loadingText = "Carregando...";
        private readonly Timer _sortTimer;
        private readonly ToolTip _tooltip;
        private readonly Dictionary<int, ListViewItem> _virtualItems;
        private readonly Queue<int> _virtualItemsQueue;
        private const int CacheSize = 1000;
        private bool _isVirtualMode;
        private int _totalItems;
        private int _pageSize = 100;
        private readonly object _lockObject = new object();
        private readonly Timer _scrollTimer;
        private float _dpiScale = 1.0f;
        #endregion

        public BaseListView()
        {
            InitializeListView();
            InitializeTimers();
            InitializeTooltip();
            SubscribeToTheme();
            InitializeVirtualization();
            HandleDpiChanged();
        }

        #region Initialization
        private void InitializeListView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint, true);
            
            FullRowSelect = true;
            GridLines = true;
            View = View.Details;
            
            Font = ThemeManager.Instance.GetFont();
            BorderStyle = BorderStyle.None;
            DoubleBuffered = true;

            ResizeRedraw = true;
            AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void InitializeTimers()
        {
            _sortTimer = new Timer { Interval = 300 };
            _sortTimer.Tick += (s, e) => 
            {
                _sortTimer.Stop();
                BeginInvoke(new Action(() => SortItems()));
            };

            _scrollTimer = new Timer { Interval = 150 };
            _scrollTimer.Tick += (s, e) =>
            {
                _scrollTimer.Stop();
                if (_isVirtualMode)
                    LoadVisibleItems();
            };
        }

        private void InitializeVirtualization()
        {
            _virtualItems = new Dictionary<int, ListViewItem>();
            _virtualItemsQueue = new Queue<int>();
            VirtualMode = true;
            VirtualListSize = 0;
            RetrieveVirtualItem += OnRetrieveVirtualItem;
            CacheVirtualItems += OnCacheVirtualItems;
        }

        private void HandleDpiChanged()
        {
            _dpiScale = CreateGraphics().DpiX / 96f;
            AdjustControlsForDpi();
        }
        #endregion

        #region DPI Handling
        private void AdjustControlsForDpi()
        {
            Font = new Font(Font.FontFamily, Font.Size * _dpiScale);
            
            foreach (ColumnHeader column in Columns)
            {
                column.Width = (int)(column.Width * _dpiScale);
            }

            ItemHeight = (int)(ItemHeight * _dpiScale);
            Invalidate();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            HandleDpiChanged();
        }
        #endregion

        #region Virtualization
        private void OnRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (_virtualItems.TryGetValue(e.ItemIndex, out ListViewItem item))
            {
                e.Item = item;
                return;
            }

            e.Item = new ListViewItem($"Loading item {e.ItemIndex}...");
            LoadItem(e.ItemIndex);
        }

        private void OnCacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            LoadItems(e.StartIndex, e.EndIndex);
        }

        private async void LoadItem(int index)
        {
            if (!_virtualItems.ContainsKey(index))
            {
                var item = await Task.Run(() => CreateListViewItem(index));
                AddToCache(index, item);
                RefreshItem(index);
            }
        }

        private async void LoadItems(int startIndex, int endIndex)
        {
            var tasks = new List<Task>();
            
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (!_virtualItems.ContainsKey(i))
                {
                    tasks.Add(Task.Run(() => 
                    {
                        var item = CreateListViewItem(i);
                        AddToCache(i, item);
                    }));
                }
            }

            await Task.WhenAll(tasks);
            RefreshItems(startIndex, endIndex);
        }

        private void LoadVisibleItems()
        {
            if (VirtualListSize == 0) return;

            int firstVisible = TopItem?.Index ?? 0;
            int visibleCount = LabelWrap ? ClientSize.Height / ItemHeight : ClientSize.Height / ItemHeight;
            int lastVisible = Math.Min(firstVisible + visibleCount, VirtualListSize - 1);

            LoadItems(firstVisible, lastVisible);
        }

        private ListViewItem CreateListViewItem(int index)
        {
            // Override this method in derived classes to create actual items
            return new ListViewItem($"Item {index}");
        }

        private void AddToCache(int index, ListViewItem item)
        {
            lock (_lockObject)
            {
                if (_virtualItems.Count >= CacheSize)
                {
                    int oldestIndex = _virtualItemsQueue.Dequeue();
                    _virtualItems.Remove(oldestIndex);
                }

                _virtualItems[index] = item;
                _virtualItemsQueue.Enqueue(index);
            }
        }

        private void ClearCache()
        {
            lock (_lockObject)
            {
                _virtualItems.Clear();
                _virtualItemsQueue.Clear();
            }
        }
        #endregion

        #region Scroll Handling
        protected override void OnScroll(ScrollEventArgs e)
        {
            base.OnScroll(e);
            
            if (_isVirtualMode)
            {
                _scrollTimer.Stop();
                _scrollTimer.Start();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            
            if (_isVirtualMode)
            {
                _scrollTimer.Stop();
                _scrollTimer.Start();
            }
        }
        #endregion

        #region Public Methods
        public void SetVirtualDataSource<T>(IList<T> data)
        {
            _isVirtualMode = true;
            _totalItems = data.Count;
            VirtualListSize = _totalItems;
            ClearCache();
            Refresh();
        }

        public void AddColumn(string text, int width, ContentAlignment alignment = ContentAlignment.MiddleLeft)
        {
            width = (int)(width * _dpiScale);
            var column = Columns.Add(text, width);
            column.TextAlign = alignment;
        }

        private void RefreshItem(int index)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RefreshItem(index)));
                return;
            }

            RedrawItems(index, index, true);
        }

        private void RefreshItems(int startIndex, int endIndex)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RefreshItems(startIndex, endIndex)));
                return;
            }

            RedrawItems(startIndex, endIndex, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            if (_isVirtualMode)
                LoadVisibleItems();

            AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public async Task LoadDataAsync<T>(IList<T> data)
        {
            IsLoading = true;

            try
            {
                if (data.Count > 1000)
                {
                    SetVirtualDataSource(data);
                }
                else
                {
                    _isVirtualMode = false;
                    VirtualMode = false;
                    Items.Clear();
                    
                    await Task.Run(() => 
                    {
                        var items = data.Select(item => CreateListViewItemFromData(item)).ToList();
                        BeginInvoke(new Action(() => 
                        {
                            Items.AddRange(items.ToArray());
                        }));
                    });
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected virtual ListViewItem CreateListViewItemFromData<T>(T data)
        {
            // Override this method to create items from data
            return new ListViewItem(data.ToString());
        }
        #endregion

        #region Disposal
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sortTimer?.Dispose();
                _scrollTimer?.Dispose();
                _tooltip?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}