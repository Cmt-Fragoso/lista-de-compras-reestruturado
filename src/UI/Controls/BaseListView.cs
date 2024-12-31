using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseListView : ListView
    {
        private bool _isLoading;
        private string _loadingText = "Carregando...";
        private readonly Timer _sortTimer;
        private readonly ToolTip _tooltip;

        public BaseListView()
        {
            InitializeListView();
            InitializeSort();
            InitializeTooltip();
            SubscribeToTheme();
        }

        private void InitializeListView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint, true);
            
            FullRowSelect = true;
            GridLines = true;
            View = View.Details;
            
            Font = ThemeManager.Instance.GetFont();
            
            BorderStyle = BorderStyle.None;
            DoubleBuffered = true;
        }

        private void InitializeSort()
        {
            _sortTimer = new Timer { Interval = 300 };
            _sortTimer.Tick += (s, e) => 
            {
                _sortTimer.Stop();
                Sort();
            };
        }

        private void InitializeTooltip()
        {
            _tooltip = new ToolTip
            {
                ShowAlways = true,
                InitialDelay = 500,
                ReshowDelay = 100
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
            BackColor = theme.Surface;
            ForeColor = theme.TextPrimary;

            if (Items.Count == 0 && _isLoading)
            {
                Invalidate();
            }
        }

        protected override void OnColumnClick(ColumnClickEventArgs e)
        {
            base.OnColumnClick(e);
            
            if (Items.Count > 0)
            {
                // Adia a ordenação para evitar múltiplos sorts
                _sortTimer.Stop();
                _sortTimer.Start();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var item = GetItemAt(e.X, e.Y);
            if (item != null)
            {
                var tip = item.ToolTipText;
                if (!string.IsNullOrEmpty(tip) && _tooltip.GetToolTip(this) != tip)
                {
                    _tooltip.SetToolTip(this, tip);
                }
            }
            else
            {
                _tooltip.SetToolTip(this, string.Empty);
            }
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Items.Count == 0)
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                using (var brush = new SolidBrush(theme.TextSecondary))
                {
                    var text = _isLoading ? _loadingText : "Nenhum item encontrado";
                    var size = e.Graphics.MeasureString(text, Font);
                    var point = new PointF(
                        (Width - size.Width) / 2,
                        (Height - size.Height) / 2);
                    e.Graphics.DrawString(text, Font, brush, point);
                }
            }
        }

        public void AddColumn(string text, int width, bool sortable = true)
        {
            var column = Columns.Add(text, width);
            column.Tag = sortable;
        }

        public new void Sort()
        {
            if (Items.Count == 0 || VirtualMode) return;

            var column = Columns[sortColumn];
            if (column?.Tag is bool sortable && !sortable) return;

            ListViewItemSorter = new ListViewItemComparer(sortColumn, sortOrder);
            base.Sort();
        }

        private int sortColumn = 0;
        private SortOrder sortOrder = SortOrder.None;

        public void SetSortColumn(int column, SortOrder order)
        {
            sortColumn = column;
            sortOrder = order;
            Sort();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sortTimer?.Dispose();
                _tooltip?.Dispose();
            }
            base.Dispose(disposing);
        }

        private class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int _column;
            private readonly SortOrder _order;

            public ListViewItemComparer(int column, SortOrder order)
            {
                _column = column;
                _order = order;
            }

            public int Compare(object x, object y)
            {
                if (_order == SortOrder.None) return 0;

                var itemX = (ListViewItem)x;
                var itemY = (ListViewItem)y;
                
                var textX = itemX.SubItems[_column].Text;
                var textY = itemY.SubItems[_column].Text;

                int result;

                // Tenta comparar como número
                if (decimal.TryParse(textX.Replace("R$", "").Trim(), out decimal numX) &&
                    decimal.TryParse(textY.Replace("R$", "").Trim(), out decimal numY))
                {
                    result = numX.CompareTo(numY);
                }
                // Tenta comparar como data
                else if (DateTime.TryParse(textX, out DateTime dateX) &&
                         DateTime.TryParse(textY, out DateTime dateY))
                {
                    result = dateX.CompareTo(dateY);
                }
                // Compara como texto
                else
                {
                    result = string.Compare(textX, textY, StringComparison.CurrentCulture);
                }

                return _order == SortOrder.Ascending ? result : -result;
            }
        }
    }
}