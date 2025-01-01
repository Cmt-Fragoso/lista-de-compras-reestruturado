using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class BaseChart : UserControl
    {
        #region Fields
        private bool _isLoading;
        private string _loadingText = "Carregando...";
        private string _title = "";
        private string _xAxisLabel = "";
        private string _yAxisLabel = "";
        private List<double> _data = new List<double>();
        private List<string> _labels = new List<string>();
        private bool _showAverage;
        private bool _showTrend;
        private string _chartType = "Linha";
        private bool _showMinMax = false;
        private bool _showPercentageChange = false;
        private bool _enableZoom = false;
        private float _zoomFactor = 1.0f;
        private Point _panStart;
        private bool _isPanning = false;
        private RectangleF _zoomRegion;
        private bool _showTooltips = true;
        private bool _showGrid = true;
        private int _gridLines = 5;
        private float _lineThickness = 2f;
        private float _pointSize = 6f;
        #endregion

        #region Events
        public event EventHandler<Point> PointClicked;
        public event EventHandler<Rectangle> BarClicked;
        public event EventHandler<double> ValueSelected;
        #endregion

        #region Properties
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; Invalidate(); }
        }

        public string LoadingText
        {
            get => _loadingText;
            set { _loadingText = value; Invalidate(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public string XAxisLabel
        {
            get => _xAxisLabel;
            set { _xAxisLabel = value; Invalidate(); }
        }

        public string YAxisLabel
        {
            get => _yAxisLabel;
            set { _yAxisLabel = value; Invalidate(); }
        }

        public bool ShowAverage
        {
            get => _showAverage;
            set { _showAverage = value; Invalidate(); }
        }

        public bool ShowTrend
        {
            get => _showTrend;
            set { _showTrend = value; Invalidate(); }
        }

        public string ChartType
        {
            get => _chartType;
            set 
            { 
                if (value.ToLower() is "linha" or "barra")
                {
                    _chartType = value;
                    Invalidate();
                }
            }
        }

        public bool ShowMinMax
        {
            get => _showMinMax;
            set { _showMinMax = value; Invalidate(); }
        }

        public bool ShowPercentageChange
        {
            get => _showPercentageChange;
            set { _showPercentageChange = value; Invalidate(); }
        }

        public bool EnableZoom
        {
            get => _enableZoom;
            set 
            { 
                _enableZoom = value;
                if (value)
                {
                    this.MouseWheel += BaseChart_MouseWheel;
                }
                else
                {
                    this.MouseWheel -= BaseChart_MouseWheel;
                    _zoomFactor = 1.0f;
                    _zoomRegion = RectangleF.Empty;
                }
                Invalidate();
            }
        }

        public bool ShowTooltips
        {
            get => _showTooltips;
            set { _showTooltips = value; }
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set { _showGrid = value; Invalidate(); }
        }

        public int GridLines
        {
            get => _gridLines;
            set 
            { 
                if (value > 0)
                {
                    _gridLines = value;
                    Invalidate();
                }
            }
        }

        public float LineThickness
        {
            get => _lineThickness;
            set 
            { 
                if (value > 0)
                {
                    _lineThickness = value;
                    Invalidate();
                }
            }
        }

        public float PointSize
        {
            get => _pointSize;
            set 
            { 
                if (value > 0)
                {
                    _pointSize = value;
                    Invalidate();
                }
            }
        }
        #endregion

        #region Constructor
        public BaseChart()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);

            DoubleBuffered = true;
            Size = new Size(400, 300);
            Font = ThemeManager.Instance.GetFont();

            ThemeManager.Instance.ThemeChanged += (s, e) => Invalidate();
            MouseClick += BaseChart_MouseClick;
            MouseDown += BaseChart_MouseDown;
            MouseMove += BaseChart_MouseMove;
            MouseUp += BaseChart_MouseUp;
        }
        #endregion

        #region Public Methods
        public void SetData(List<double> data)
        {
            _data = data ?? new List<double>();
            Invalidate();
        }

        public void SetLabels(List<string> labels)
        {
            _labels = labels ?? new List<string>();
            Invalidate();
        }

        public void Clear()
        {
            _data.Clear();
            _labels.Clear();
            Invalidate();
        }

        public void ResetZoom()
        {
            _zoomFactor = 1.0f;
            _zoomRegion = RectangleF.Empty;
            Invalidate();
        }
        #endregion

        #region Protected Methods
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var theme = ThemeManager.Instance.CurrentTheme;
            using (var backBrush = new SolidBrush(theme.Background))
            {
                e.Graphics.FillRectangle(backBrush, ClientRectangle);
            }

            var chartArea = CalculateChartArea();
            
            if (_isLoading)
            {
                DrawLoading(e.Graphics, chartArea, theme);
                return;
            }

            if (_data.Count == 0)
            {
                DrawNoData(e.Graphics, chartArea, theme);
                return;
            }

            if (_enableZoom)
            {
                e.Graphics.TranslateTransform(_zoomRegion.X, _zoomRegion.Y);
                e.Graphics.ScaleTransform(_zoomFactor, _zoomFactor);
            }

            DrawAxes(e.Graphics, chartArea, theme);

            switch (_chartType.ToLower())
            {
                case "linha":
                    DrawLineChart(e.Graphics, chartArea, theme);
                    break;
                case "barra":
                    DrawBarChart(e.Graphics, chartArea, theme);
                    break;
            }

            if (_showTrend)
                DrawTrend(e.Graphics, chartArea, theme);

            if (_showAverage)
                DrawAverage(e.Graphics, chartArea, theme);

            if (_showMinMax)
                DrawMinMax(e.Graphics, chartArea, theme);

            if (_showPercentageChange)
                DrawPercentageChange(e.Graphics, chartArea, theme);

            if (_enableZoom)
                e.Graphics.ResetTransform();

            DrawTitle(e.Graphics, chartArea, theme);
        }
        #endregion

        #region Private Methods
        private void DrawLoading(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            using (var brush = new SolidBrush(theme.TextSecondary))
            {
                var size = g.MeasureString(_loadingText, Font);
                g.DrawString(_loadingText, Font, brush,
                    chartArea.Left + (chartArea.Width - size.Width) / 2,
                    chartArea.Top + (chartArea.Height - size.Height) / 2);
            }
        }

        private void DrawNoData(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            using (var brush = new SolidBrush(theme.TextSecondary))
            {
                string text = "Sem dados para exibir";
                var size = g.MeasureString(text, Font);
                g.DrawString(text, Font, brush,
                    chartArea.Left + (chartArea.Width - size.Width) / 2,
                    chartArea.Top + (chartArea.Height - size.Height) / 2);
            }
        }

        private void DrawAxes(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            using (var pen = new Pen(theme.Border))
            using (var brush = new SolidBrush(theme.TextPrimary))
            {
                // Eixo X
                g.DrawLine(pen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);
                
                // Eixo Y
                g.DrawLine(pen, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom);

                if (_showGrid)
                {
                    DrawGrid(g, chartArea, theme);
                }

                DrawAxisLabels(g, chartArea, theme);

                // Labels dos eixos
                if (!string.IsNullOrEmpty(_xAxisLabel))
                {
                    var size = g.MeasureString(_xAxisLabel, Font);
                    g.DrawString(_xAxisLabel, Font, brush,
                        chartArea.Left + (chartArea.Width - size.Width) / 2,
                        chartArea.Bottom + 20);
                }

                if (!string.IsNullOrEmpty(_yAxisLabel))
                {
                    var size = g.MeasureString(_yAxisLabel, Font);
                    var point = new PointF(
                        chartArea.Left - 35,
                        chartArea.Top + (chartArea.Height + size.Width) / 2);

                    g.TranslateTransform(point.X, point.Y);
                    g.RotateTransform(-90);
                    g.DrawString(_yAxisLabel, Font, brush, 0, 0);
                    g.ResetTransform();
                }
            }
        }

        private void DrawGrid(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            using (var pen = new Pen(theme.Border) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
            {
                // Linhas horizontais
                float yStep = chartArea.Height / (float)_gridLines;
                for (int i = 1; i < _gridLines; i++)
                {
                    float y = chartArea.Bottom - (yStep * i);
                    g.DrawLine(pen, chartArea.Left, y, chartArea.Right, y);
                }

                // Linhas verticais
                if (_labels.Count > 0)
                {
                    float xStep = chartArea.Width / (float)_labels.Count;
                    for (int i = 1; i < _labels.Count; i++)
                    {
                        float x = chartArea.Left + (xStep * i);
                        g.DrawLine(pen, x, chartArea.Top, x, chartArea.Bottom);
                    }
                }
            }
        }

        private void DrawAxisLabels(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            using (var brush = new SolidBrush(theme.TextPrimary))
            {
                // Labels do eixo X
                if (_labels.Count > 0)
                {
                    float xStep = chartArea.Width / (float)_labels.Count;
                    for (int i = 0; i < _labels.Count; i++)
                    {
                        var size = g.MeasureString(_labels[i], Font);
                        float x = chartArea.Left + (xStep * i) - (size.Width / 2);
                        g.DrawString(_labels[i], Font, brush, x, chartArea.Bottom + 5);
                    }
                }

                // Labels do eixo Y
                if (_data.Count > 0)
                {
                    double min = _data.Min();
                    double max = _data.Max();
                    double step = (max - min) / _gridLines;

                    for (int i = 0; i <= _gridLines; i++)
                    {
                        double value = min + (step * i);
                        string label = value.ToString("N2");
                        var size = g.MeasureString(label, Font);
                        float y = chartArea.Bottom - ((float)i / _gridLines) * chartArea.Height - (size.Height / 2);
                        g.DrawString(label, Font, brush, chartArea.Left - size.Width - 5, y);
                    }
                }
            }
        }

        private void DrawLineChart(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (_data.Count < 2) return;

            using (var pen = new Pen(theme.Primary, _lineThickness))
            using (var pointBrush = new SolidBrush(theme.Primary))
            {
                double min = _data.Min();
                double max = _data.Max();
                float xStep = chartArea.Width / (float)(_data.Count - 1);
                
                var points = new PointF[_data.Count];
                for (int i = 0; i < _data.Count; i++)
                {
                    float x = chartArea.Left + (xStep * i);
                    float y = chartArea.Bottom - 
                        (float)(((_data[i] - min) / (max - min)) * chartArea.Height);
                    points[i] = new PointF(x, y);
                }

                // Desenha a linha
                g.DrawLines(pen, points);

                // Desenha os pontos
                float halfPoint = _pointSize / 2;
                foreach (var point in points)
                {
                    g.FillEllipse(pointBrush,
                        point.X - halfPoint,
                        point.Y - halfPoint,
                        _pointSize, _pointSize);
                }
            }
        }

        private void DrawBarChart(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (_data.Count == 0) return;

            float barWidth = (chartArea.Width / _data.Count) * 0.8f;
            float spacing = (chartArea.Width / _data.Count) * 0.2f;
            double min = _data.Min();
            double max = _data.Max();

            using (var brush = new SolidBrush(theme.Primary))
            {
                for (int i = 0; i < _data.Count; i++)
                {
                    float x = chartArea.Left + (i * (barWidth + spacing)) + (spacing / 2);
                    float height = (float)(((_data[i] - min) / (max - min)) * chartArea.Height);
                    float y = chartArea.Bottom - height;

                    var rect = new RectangleF(x, y, barWidth, height);
                    g.FillRectangle(brush, rect);

                    if (_showTooltips && rect.Contains(PointToClient(MousePosition)))
                    {
                        DrawTooltip(g, new PointF(x + barWidth/2, y), _data[i], theme);
                    }
                }
            }
        }

        private void DrawTooltip(Graphics g, PointF location, double value, ThemeColors theme)
        {
            string text = value.ToString("N2");
            var size = g.MeasureString(text, Font);
            
            var rect = new RectangleF(
                location.X - size.Width/2 - 5,
                location.Y - size.Height - 10,
                size.Width + 10,
                size.Height + 6);

            using (var backBrush = new SolidBrush(Color.FromArgb(230, theme.Background)))
            using (var borderPen = new Pen(theme.Border))
            using (var textBrush = new SolidBrush(theme.TextPrimary))
            {
                g.FillRectangle(backBrush, rect);
                g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawString(text, Font, textBrush, 
                    rect.X + 5, 
                    rect.Y + 3);
            }
        }

        private void DrawMinMax(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (!_showMinMax || _data.Count == 0) return;

            double min = _data.Min();
            double max = _data.Max();
            
            using (var pen = new Pen(theme.Warning, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            using (var brush = new SolidBrush(theme.Warning))
            {
                // Linha e label do máximo
                float yMax = chartArea.Bottom - (float)((max - _data.Min()) / (_data.Max() - _data.Min()) * chartArea.Height);
                g.DrawLine(pen, chartArea.Left, yMax, chartArea.Right, yMax);
                
                string maxLabel = $"Máx: {max:N2}";
                var maxSize = g.MeasureString(maxLabel, Font);
                g.DrawString(maxLabel, Font, brush, 
                    chartArea.Right - maxSize.Width - 5, 
                    yMax - maxSize.Height - 2);

                // Linha e label do mínimo
                float yMin = chartArea.Bottom - (float)((min - _data.Min()) / (_data.Max() - _data.Min()) * chartArea.Height);
                g.DrawLine(pen, chartArea.Left, yMin, chartArea.Right, yMin);
                
                string minLabel = $"Mín: {min:N2}";
                var minSize = g.MeasureString(minLabel, Font);
                g.DrawString(minLabel, Font, brush,
                    chartArea.Right - minSize.Width - 5,
                    yMin + 2);
            }
        }

        private void DrawPercentageChange(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (!_showPercentageChange || _data.Count < 2) return;

            double first = _data.First();
            double last = _data.Last();
            double percentChange = ((last - first) / first) * 100;
            Color color = percentChange >= 0 ? theme.Success : theme.Error;

            using (var brush = new SolidBrush(color))
            {
                string label = $"Variação: {percentChange:+0.##;-0.##}%";
                var size = g.MeasureString(label, Font);
                g.DrawString(label, Font, brush,
                    chartArea.Left + 5,
                    chartArea.Top + 5);
            }
        }

        private Rectangle CalculateChartArea()
        {
            int margin = 40;
            int titleHeight = string.IsNullOrEmpty(_title) ? 0 : 30;
            
            return new Rectangle(
                margin,
                margin + titleHeight,
                Width - (margin * 2),
                Height - (margin * 2) - titleHeight
            );
        }

        private void DrawTitle(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (string.IsNullOrEmpty(_title)) return;

            using (var brush = new SolidBrush(theme.TextPrimary))
            {
                var size = g.MeasureString(_title, Font);
                g.DrawString(_title, Font, brush,
                    chartArea.Left + (chartArea.Width - size.Width) / 2,
                    chartArea.Top - 25);
            }
        }

        #region Event Handlers
        private void BaseChart_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!_enableZoom) return;

            float oldZoom = _zoomFactor;
            
            if (e.Delta > 0)
                _zoomFactor *= 1.1f;
            else
                _zoomFactor /= 1.1f;

            _zoomFactor = Math.Max(1.0f, Math.Min(_zoomFactor, 3.0f));

            if (_zoomFactor != oldZoom)
            {
                Point mousePos = e.Location;
                _zoomRegion.X -= (mousePos.X * (_zoomFactor - oldZoom));
                _zoomRegion.Y -= (mousePos.Y * (_zoomFactor - oldZoom));
                Invalidate();
            }
        }

        private void BaseChart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle && _enableZoom)
            {
                _isPanning = true;
                _panStart = e.Location;
                Cursor = Cursors.Hand;
            }
        }

        private void BaseChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                int dx = e.X - _panStart.X;
                int dy = e.Y - _panStart.Y;
                
                _zoomRegion.X += dx;
                _zoomRegion.Y += dy;
                _panStart = e.Location;
                
                Invalidate();
            }
            else if (_showTooltips)
            {
                Invalidate(); // Atualiza para mostrar/esconder tooltips
            }
        }

        private void BaseChart_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }

        private void BaseChart_MouseClick(object sender, MouseEventArgs e)
        {
            var chartArea = CalculateChartArea();
            if (!chartArea.Contains(e.Location)) return;

            int index = GetDataIndexFromLocation(e.Location, chartArea);
            if (index >= 0 && index < _data.Count)
            {
                ValueSelected?.Invoke(this, _data[index]);
            }
        }

        private int GetDataIndexFromLocation(Point location, Rectangle chartArea)
        {
            if (_data.Count == 0) return -1;

            float xStep = chartArea.Width / (float)(_data.Count - 1);
            float relativeX = location.X - chartArea.Left;
            int index = (int)Math.Round(relativeX / xStep);

            return Math.Max(0, Math.Min(_data.Count - 1, index));
        }
        #endregion
        #endregion
    }
}