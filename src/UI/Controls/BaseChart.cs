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

        // Eventos
        public event EventHandler<Point> PointClicked;
        public event EventHandler<Rectangle> BarClicked;

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
        }

        #region Propriedades
        // [Propriedades anteriores mantidas...]

        public bool ShowTooltips { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public int GridLines { get; set; } = 5;
        public float LineThickness { get; set; } = 2f;
        public float PointSize { get; set; } = 6f;
        #endregion

        #region Métodos Privados
        private void DrawAxes(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            using (var pen = new Pen(theme.Border))
            using (var brush = new SolidBrush(theme.TextPrimary))
            {
                // Eixo X
                g.DrawLine(pen, chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom);
                
                // Eixo Y
                g.DrawLine(pen, chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom);

                if (ShowGrid)
                {
                    DrawGrid(g, chartArea, theme);
                }

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

                DrawAxisLabels(g, chartArea, theme);
            }
        }

        private void DrawGrid(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            using (var pen = new Pen(theme.Border) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
            {
                // Linhas horizontais
                float yStep = chartArea.Height / (float)GridLines;
                for (int i = 1; i < GridLines; i++)
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
                    double step = (max - min) / GridLines;

                    float yStep = chartArea.Height / (float)GridLines;
                    for (int i = 0; i <= GridLines; i++)
                    {
                        double value = min + (step * i);
                        string label = value.ToString("N2");
                        var size = g.MeasureString(label, Font);
                        float y = chartArea.Bottom - (yStep * i) - (size.Height / 2);
                        g.DrawString(label, Font, brush, chartArea.Left - size.Width - 5, y);
                    }
                }
            }
        }

        private void DrawLineChart(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (_data.Count < 2) return;

            using (var pen = new Pen(theme.Primary, LineThickness))
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
                float halfPoint = PointSize / 2;
                foreach (var point in points)
                {
                    g.FillEllipse(pointBrush, 
                        point.X - halfPoint,
                        point.Y - halfPoint, 
                        PointSize, PointSize);
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
                    float x = chartArea.Left + (i * (barWidth + spacing));
                    float height = (float)(((_data[i] - min) / (max - min)) * chartArea.Height);
                    float y = chartArea.Bottom - height;

                    var rect = new RectangleF(x, y, barWidth, height);
                    g.FillRectangle(brush, rect);
                }
            }
        }

        private void DrawTrend(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (_data.Count < 2) return;

            var points = new List<PointF>();
            float xStep = chartArea.Width / (float)(_data.Count - 1);
            double min = _data.Min();
            double max = _data.Max();

            // Calcula pontos para regressão linear
            for (int i = 0; i < _data.Count; i++)
            {
                float x = chartArea.Left + (xStep * i);
                float y = chartArea.Bottom - 
                    (float)(((_data[i] - min) / (max - min)) * chartArea.Height);
                points.Add(new PointF(x, y));
            }

            // Calcula linha de tendência
            float sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                sumX += points[i].X;
                sumY += points[i].Y;
                sumXY += points[i].X * points[i].Y;
                sumX2 += points[i].X * points[i].X;
            }

            float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            float intercept = (sumY - slope * sumX) / n;

            using (var pen = new Pen(theme.Secondary, 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                float x1 = chartArea.Left;
                float y1 = slope * x1 + intercept;
                float x2 = chartArea.Right;
                float y2 = slope * x2 + intercept;

                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        private void DrawAverage(Graphics g, Rectangle chartArea, ThemeColors theme)
        {
            if (_data.Count == 0) return;

            double average = _data.Average();
            double min = _data.Min();
            double max = _data.Max();
            float y = chartArea.Bottom - 
                (float)(((average - min) / (max - min)) * chartArea.Height);

            using (var pen = new Pen(theme.Info, 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
            {
                g.DrawLine(pen, chartArea.Left, y, chartArea.Right, y);

                // Desenha label da média
                using (var brush = new SolidBrush(theme.Info))
                {
                    string label = $"Média: {average:N2}";
                    var size = g.MeasureString(label, Font);
                    g.DrawString(label, Font, brush, 
                        chartArea.Right - size.Width - 5, 
                        y - size.Height - 2);
                }
            }
        }
        #endregion

        #region Eventos
        private void BaseChart_MouseClick(object sender, MouseEventArgs e)
        {
            var chartArea = CalculateChartArea();
            if (!chartArea.Contains(e.Location)) return;

            switch (_chartType)
            {
                case "Linha":
                    HandleLineChartClick(e.Location, chartArea);
                    break;
                case "Barra":
                    HandleBarChartClick(e.Location, chartArea);
                    break;
            }
        }

        private void HandleLineChartClick(Point location, Rectangle chartArea)
        {
            if (_data.Count == 0) return;

            float xStep = chartArea.Width / (float)(_data.Count - 1);
            float nearestDistance = float.MaxValue;
            Point nearestPoint = Point.Empty;

            for (int i = 0; i < _data.Count; i++)
            {
                float x = chartArea.Left + (xStep * i);
                float y = chartArea.Bottom - 
                    (float)(((_data[i] - _data.Min()) / (_data.Max() - _data.Min())) * chartArea.Height);

                var distance = Math.Sqrt(
                    Math.Pow(location.X - x, 2) + 
                    Math.Pow(location.Y - y, 2));

                if (distance < nearestDistance && distance < 10)
                {
                    nearestDistance = (float)distance;
                    nearestPoint = new Point((int)x, (int)y);
                }
            }

            if (nearestPoint != Point.Empty)
            {
                PointClicked?.Invoke(this, nearestPoint);
            }
        }

        private void HandleBarChartClick(Point location, Rectangle chartArea)
        {
            if (_data.Count == 0) return;

            float barWidth = (chartArea.Width / _data.Count) * 0.8f;
            float spacing = (chartArea.Width / _data.Count) * 0.2f;

            for (int i = 0; i < _data.Count; i++)
            {
                float x = chartArea.Left + (i * (barWidth + spacing));
                float height = (float)(((_data[i] - _data.Min()) / (_data.Max() - _data.Min())) * chartArea.Height);
                float y = chartArea.Bottom - height;

                var rect = new Rectangle((int)x, (int)y, (int)barWidth, (int)height);
                if (rect.Contains(location))
                {
                    BarClicked?.Invoke(this, rect);
                    break;
                }
            }
        }
        #endregion
    }
}