using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Controls
{
    public class PriceChart : BaseChart
    {
        private bool _showMMA = false; // Média Móvel Aritmética
        private int _mmaperiod = 5;    // Período da média móvel
        private bool _showVolatility = false;
        private List<double> _volumes = new List<double>();

        public bool ShowMovingAverage
        {
            get => _showMMA;
            set { _showMMA = value; Invalidate(); }
        }

        public int MovingAveragePeriod
        {
            get => _mmaperiod;
            set 
            { 
                if (value >= 2)
                {
                    _mmaperiod = value;
                    Invalidate();
                }
            }
        }

        public bool ShowVolatility
        {
            get => _showVolatility;
            set { _showVolatility = value; Invalidate(); }
        }

        public void SetPriceData(List<double> prices, List<DateTime> dates, List<double> volumes = null)
        {
            if (prices == null || dates == null || prices.Count != dates.Count)
                throw new ArgumentException("Dados inválidos");

            SetData(prices);
            SetLabels(dates.ConvertAll(d => d.ToString("dd/MM")));
            _volumes = volumes ?? new List<double>();
            
            ShowMinMax = true;
            ShowPercentageChange = true;
            EnableZoom = true;
            
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var theme = ThemeManager.Instance.CurrentTheme;
            var chartArea = CalculateChartArea();

            if (_showMMA)
                DrawMovingAverage(e.Graphics, chartArea, theme);

            if (_showVolatility)
                DrawVolatility(e.Graphics, chartArea, theme);
        }

        private void DrawMovingAverage(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (_data.Count < _mmaperiod) return;

            var mma = CalculateMovingAverage();
            double min = _data.Min();
            double max = _data.Max();
            float xStep = chartArea.Width / (float)(_data.Count - 1);

            using (var pen = new Pen(theme.Info, 2f))
            {
                var points = new List<PointF>();
                for (int i = _mmaperiod - 1; i < mma.Count; i++)
                {
                    float x = chartArea.Left + (xStep * i);
                    float y = chartArea.Bottom - 
                        (float)((mma[i] - min) / (max - min) * chartArea.Height);
                    points.Add(new PointF(x, y));
                }

                if (points.Count > 1)
                {
                    g.DrawLines(pen, points.ToArray());
                }

                // Desenha legenda da MMA
                string label = $"MMA({_mmaperiod})";
                var size = g.MeasureString(label, Font);
                using (var brush = new SolidBrush(theme.Info))
                {
                    g.DrawString(label, Font, brush,
                        chartArea.Left + 5,
                        chartArea.Top + 5);
                }
            }
        }

        private List<double> CalculateMovingAverage()
        {
            var result = new List<double>();
            for (int i = 0; i < _data.Count; i++)
            {
                if (i < _mmaperiod - 1)
                {
                    result.Add(0);
                    continue;
                }

                double sum = 0;
                for (int j = 0; j < _mmaperiod; j++)
                {
                    sum += _data[i - j];
                }
                result.Add(sum / _mmaperiod);
            }
            return result;
        }

        private void DrawVolatility(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (_data.Count < 2) return;

            var volatility = CalculateVolatility();
            double maxVol = volatility.Max();
            
            using (var brush = new SolidBrush(Color.FromArgb(64, theme.Warning.R, theme.Warning.G, theme.Warning.B)))
            {
                float xStep = chartArea.Width / (float)(_data.Count - 1);
                float heightScale = chartArea.Height * 0.2f; // Usa 20% da altura para volatilidade

                for (int i = 1; i < volatility.Count; i++)
                {
                    float x = chartArea.Left + (xStep * i);
                    float height = (float)(volatility[i] / maxVol * heightScale);
                    
                    var rect = new RectangleF(
                        x - (xStep / 4),
                        chartArea.Bottom - height,
                        xStep / 2,
                        height);

                    g.FillRectangle(brush, rect);
                }

                // Desenha legenda da volatilidade
                string label = "Volatilidade";
                var size = g.MeasureString(label, Font);
                using (var textBrush = new SolidBrush(theme.Warning))
                {
                    g.DrawString(label, Font, textBrush,
                        chartArea.Right - size.Width - 5,
                        chartArea.Bottom - size.Height - 5);
                }
            }
        }

        private List<double> CalculateVolatility()
        {
            var volatility = new List<double> { 0 }; // Primeiro ponto não tem volatilidade
            
            for (int i = 1; i < _data.Count; i++)
            {
                double previousPrice = _data[i - 1];
                double currentPrice = _data[i];
                double percentChange = Math.Abs((currentPrice - previousPrice) / previousPrice);
                volatility.Add(percentChange);
            }

            return volatility;
        }

        public void ShowPriceDetails(Point location)
        {
            var chartArea = CalculateChartArea();
            if (!chartArea.Contains(location)) return;

            int index = GetDataIndexFromLocation(location, chartArea);
            if (index < 0 || index >= _data.Count) return;

            var tooltip = new ToolTip();
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

            tooltip.Show(details, this, location.X + 10, location.Y - 10, 3000);
        }

        private int GetDataIndexFromLocation(Point location, Rectangle chartArea)
        {
            if (_data.Count == 0) return -1;

            float xStep = chartArea.Width / (float)(_data.Count - 1);
            float relativeX = location.X - chartArea.Left;
            int index = (int)Math.Round(relativeX / xStep);

            return Math.Max(0, Math.Min(_data.Count - 1, index));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            var chartArea = CalculateChartArea();
            if (chartArea.Contains(e.Location))
            {
                ShowPriceDetails(e.Location);
            }
        }
    }
}