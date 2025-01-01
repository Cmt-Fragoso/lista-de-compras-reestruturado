using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using ListaCompras.UI.Themes;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;

namespace ListaCompras.UI.Controls
{
    public class PriceChart : BaseChart
    {
        #region Fields
        private bool _showMMA = false;
        private int _mmaperiod = 5;
        private bool _showVolatility = false;
        private List<double> _volumes = new List<double>();
        private BufferedGraphics _graphicsBuffer;
        private readonly object _lockObject = new object();
        private Dictionary<string, List<PointF>> _cachedLines = new Dictionary<string, List<PointF>>();
        private bool _isDirty = true;
        private Rectangle _lastChartArea;
        private double _lastMin, _lastMax;
        private readonly Timer _redrawTimer;
        private readonly Timer _tooltipTimer;
        private ToolTip _currentTooltip;
        #endregion

        #region Properties
        public bool ShowMovingAverage
        {
            get => _showMMA;
            set { _showMMA = value; InvalidateCache(); }
        }

        public int MovingAveragePeriod
        {
            get => _mmaperiod;
            set 
            { 
                if (value >= 2)
                {
                    _mmaperiod = value;
                    InvalidateCache();
                }
            }
        }

        public bool ShowVolatility
        {
            get => _showVolatility;
            set { _showVolatility = value; InvalidateCache(); }
        }
        #endregion

        #region Constructor
        public PriceChart() : base()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint, true);

            _redrawTimer = new Timer { Interval = 16 }; // ~60 FPS
            _redrawTimer.Tick += (s, e) => {
                _redrawTimer.Stop();
                Invalidate();
            };

            _tooltipTimer = new Timer { Interval = 500 };
            _tooltipTimer.Tick += (s, e) => {
                _tooltipTimer.Stop();
                ShowPriceDetails(PointToClient(MousePosition));
            };

            SizeChanged += (s, e) => InvalidateCache();
            ThemeManager.Instance.ThemeChanged += (s, e) => InvalidateCache();
        }
        #endregion

        #region Public Methods
        public void SetPriceData(List<double> prices, List<DateTime> dates, List<double> volumes = null)
        {
            if (prices == null || dates == null || prices.Count != dates.Count)
                throw new ArgumentException("Dados inválidos");

            lock (_lockObject)
            {
                SetData(prices);
                SetLabels(dates.ConvertAll(d => d.ToString("dd/MM")));
                _volumes = volumes ?? new List<double>();
                
                ShowMinMax = true;
                ShowPercentageChange = true;
                EnableZoom = true;
                
                InvalidateCache();
            }
        }

        public override void Invalidate()
        {
            _isDirty = true;
            base.Invalidate();
        }

        private void InvalidateCache()
        {
            lock (_lockObject)
            {
                _cachedLines.Clear();
                _isDirty = true;
                _redrawTimer.Stop();
                _redrawTimer.Start();
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnPaint(PaintEventArgs e)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var chartArea = CalculateChartArea();

            if (_graphicsBuffer == null || _isDirty || 
                _lastChartArea != chartArea || 
                _lastMin != _data.Min() || 
                _lastMax != _data.Max())
            {
                CreateBuffer(e.Graphics);
                DrawToBuffer(theme, chartArea);
            }

            // Renderiza o buffer
            _graphicsBuffer.Render(e.Graphics);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            _tooltipTimer.Stop();
            _tooltipTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _graphicsBuffer?.Dispose();
                _redrawTimer?.Dispose();
                _tooltipTimer?.Dispose();
                _currentTooltip?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Private Methods
        private void CreateBuffer(Graphics g)
        {
            _graphicsBuffer?.Dispose();
            
            var context = BufferedGraphicsManager.Current;
            context.MaximumBuffer = new Size(Width + 1, Height + 1);
            _graphicsBuffer = context.Allocate(g, ClientRectangle);
            
            _graphicsBuffer.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphicsBuffer.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        }

        private void DrawToBuffer(ThemeColors theme, Rectangle chartArea)
        {
            var g = _graphicsBuffer.Graphics;
            
            // Limpa o buffer
            using (var backBrush = new SolidBrush(theme.Background))
            {
                g.FillRectangle(backBrush, ClientRectangle);
            }

            // Desenha o gráfico base
            base.OnPaint(new PaintEventArgs(g, ClientRectangle));

            if (_showMMA)
                DrawMovingAverageAsync(g, chartArea, theme);

            if (_showVolatility)
                DrawVolatilityAsync(g, chartArea, theme);

            _lastChartArea = chartArea;
            _lastMin = _data.Min();
            _lastMax = _data.Max();
            _isDirty = false;
        }

        private async void DrawMovingAverageAsync(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            const string cacheKey = "mma";
            List<PointF> points;

            if (_cachedLines.ContainsKey(cacheKey))
            {
                points = _cachedLines[cacheKey];
            }
            else
            {
                points = await Task.Run(() => {
                    var mma = CalculateMovingAverage();
                    return GetPointsFromData(mma, chartArea);
                });

                lock (_lockObject)
                {
                    _cachedLines[cacheKey] = points;
                }
            }

            using (var pen = new Pen(theme.Info, 2f))
            {
                g.DrawLines(pen, points.ToArray());
            }

            // Desenha legenda
            string label = $"MMA({_mmaperiod})";
            var size = g.MeasureString(label, Font);
            using (var brush = new SolidBrush(theme.Info))
            {
                g.DrawString(label, Font, brush,
                    chartArea.Left + 5,
                    chartArea.Top + 5);
            }
        }

        private async void DrawVolatilityAsync(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            const string cacheKey = "volatility";
            List<PointF> points;

            if (_cachedLines.ContainsKey(cacheKey))
            {
                points = _cachedLines[cacheKey];
            }
            else
            {
                points = await Task.Run(() => {
                    var volatility = CalculateVolatility();
                    return GetPointsFromData(volatility, chartArea);
                });

                lock (_lockObject)
                {
                    _cachedLines[cacheKey] = points;
                }
            }

            using (var brush = new SolidBrush(Color.FromArgb(64, theme.Warning.R, theme.Warning.G, theme.Warning.B)))
            {
                float xStep = chartArea.Width / (float)(points.Count - 1);
                float heightScale = chartArea.Height * 0.2f;

                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];
                    var rect = new RectangleF(
                        point.X - (xStep / 4),
                        point.Y,
                        xStep / 2,
                        chartArea.Bottom - point.Y);

                    g.FillRectangle(brush, rect);
                }
            }

            // Desenha legenda
            string label = "Volatilidade";
            var size = g.MeasureString(label, Font);
            using (var brush = new SolidBrush(theme.Warning))
            {
                g.DrawString(label, Font, brush,
                    chartArea.Right - size.Width - 5,
                    chartArea.Bottom - size.Height - 5);
            }
        }

        private List<PointF> GetPointsFromData(List<double> data, Rectangle chartArea)
        {
            if (data.Count == 0) return new List<PointF>();

            double min = data.Min();
            double max = data.Max();
            float xStep = chartArea.Width / (float)(data.Count - 1);

            return data.Select((value, index) => new PointF(
                chartArea.Left + (xStep * index),
                chartArea.Bottom - (float)((value - min) / (max - min) * chartArea.Height)
            )).ToList();
        }

        private List<double> CalculateMovingAverage()
        {
            if (_data.Count < _mmaperiod)
                return new List<double>();

            var result = new double[_data.Count];
            double sum = 0;

            // Primeira média
            for (int i = 0; i < _mmaperiod; i++)
                sum += _data[i];
            result[_mmaperiod - 1] = sum / _mmaperiod;

            // Médias subsequentes usando janela deslizante
            for (int i = _mmaperiod; i < _data.Count; i++)
            {
                sum = sum - _data[i - _mmaperiod] + _data[i];
                result[i] = sum / _mmaperiod;
            }

            return result.ToList();
        }

        private List<double> CalculateVolatility()
        {
            if (_data.Count < 2)
                return new List<double>();

            return _data.Skip(1)
                       .Zip(_data, (current, previous) => 
                           Math.Abs((current - previous) / previous))
                       .ToList();
        }

        private void ShowPriceDetails(Point location)
        {
            _currentTooltip?.Dispose();

            var chartArea = CalculateChartArea();
            if (!chartArea.Contains(location)) return;

            int index = GetDataIndexFromLocation(location, chartArea);
            if (index < 0 || index >= _data.Count) return;

            _currentTooltip = new ToolTip { InitialDelay = 0, ReshowDelay = 0 };
            string details = $"Preço: {_data[index]:C2}\n";
            
            if (_showMMA && index >= _mmaperiod - 1)
            {
                var mma = CalculateMovingAverage();
                details += $"MMA({_mmaperiod}): {mma[index]:C2}\n";
            }

            if (_volumes.Count > index)
            {
                details += $"Volume: {_volumes[index]}";
            }

            _currentTooltip.Show(details, this, location.X + 10, location.Y - 10, 3000);
        }
        #endregion
    }
}