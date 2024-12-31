using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseDataGrid : DataGridView
    {
        private bool _isLoading;
        private string _loadingText = "Carregando...";
        private string _emptyText = "Nenhum dado encontrado";
        private readonly Timer _sortTimer;

        public BaseDataGrid()
        {
            InitializeGrid();
            InitializeSort();
            SubscribeToTheme();
        }

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
            
            // Double buffering via reflection
            var dgvType = this.GetType();
            var pi = dgvType.GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic);
            pi.SetValue(this, true, null);
        }

        private void InitializeSort()
        {
            _sortTimer = new Timer { Interval = 300 };
            _sortTimer.Tick += (s, e) => 
            {
                _sortTimer.Stop();
                Sort(SortedColumn, SortOrder);
            };
        }

        private void SubscribeToTheme()
        {
            ThemeManager.Instance.ThemeChanged += (s, e) => ApplyTheme();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            
            BackgroundColor = theme.Background;
            GridColor = theme.Border;
            DefaultCellStyle.BackColor = theme.Surface;
            DefaultCellStyle.ForeColor = theme.TextPrimary;
            DefaultCellStyle.SelectionBackColor = theme.Primary;
            DefaultCellStyle.SelectionForeColor = Color.White;
            
            ColumnHeadersDefaultCellStyle.BackColor = theme.BackgroundAlt;
            ColumnHeadersDefaultCellStyle.ForeColor = theme.TextPrimary;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.BackgroundAlt;
            ColumnHeadersDefaultCellStyle.SelectionForeColor = theme.TextPrimary;
            
            AlternatingRowsDefaultCellStyle.BackColor = theme.BackgroundAlt;
            AlternatingRowsDefaultCellStyle.ForeColor = theme.TextPrimary;
            AlternatingRowsDefaultCellStyle.SelectionBackColor = theme.Primary;
            AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            Invalidate();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    Invalidate();
                }
            }
        }

        public string LoadingText
        {
            get => _loadingText;
            set
            {
                if (_loadingText != value)
                {
                    _loadingText = value;
                    if (_isLoading) Invalidate();
                }
            }
        }

        public string EmptyText
        {
            get => _emptyText;
            set
            {
                if (_emptyText != value)
                {
                    _emptyText = value;
                    if (RowCount == 0) Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (RowCount == 0)
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
            base.OnColumnHeaderMouseClick(e);
            
            if (RowCount > 0)
            {
                // Adia a ordenação para evitar múltiplos sorts
                _sortTimer.Stop();
                _sortTimer.Start();
            }
        }

        public void AddColumn(string name, string headerText, string dataPropertyName, 
            int width = 100, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                DataPropertyName = dataPropertyName,
                Width = width,
                DefaultCellStyle = { Alignment = alignment },
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            Columns.Add(column);
        }

        public void AddButtonColumn(string name, string headerText, string text, 
            int width = 80, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleCenter)
        {
            var column = new DataGridViewButtonColumn
            {
                Name = name,
                HeaderText = headerText,
                Text = text,
                Width = width,
                DefaultCellStyle = { Alignment = alignment },
                SortMode = DataGridViewColumnSortMode.NotSortable,
                UseColumnTextForButtonValue = true
            };

            Columns.Add(column);
        }

        public void LoadData<T>(BindingList<T> data)
        {
            IsLoading = true;
            DataSource = null;

            try
            {
                if (data != null)
                {
                    DataSource = data;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sortTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}