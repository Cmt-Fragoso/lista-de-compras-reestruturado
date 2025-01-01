using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;

namespace ListaCompras.UI.Resources
{
    public class IconManager
    {
        private static readonly Lazy<IconManager> _instance = new Lazy<IconManager>(() => new IconManager());
        public static IconManager Instance => _instance.Value;

        private readonly Dictionary<string, Dictionary<int, Bitmap>> _iconCache = new Dictionary<string, Dictionary<int, Bitmap>>();
        private readonly object _lockObject = new object();
        private const int DefaultSize = 16;

        private readonly Dictionary<string, Action<Graphics, Rectangle, Color>> _vectorIcons = new Dictionary<string, Action<Graphics, Rectangle, Color>>
        {
            ["add"] = (g, r, c) => DrawAddIcon(g, r, c),
            ["delete"] = (g, r, c) => DrawDeleteIcon(g, r, c),
            ["edit"] = (g, r, c) => DrawEditIcon(g, r, c),
            ["save"] = (g, r, c) => DrawSaveIcon(g, r, c),
            ["cancel"] = (g, r, c) => DrawCancelIcon(g, r, c),
            ["search"] = (g, r, c) => DrawSearchIcon(g, r, c),
            ["settings"] = (g, r, c) => DrawSettingsIcon(g, r, c),
            ["list"] = (g, r, c) => DrawListIcon(g, r, c),
            ["price"] = (g, r, c) => DrawPriceIcon(g, r, c),
            ["chart"] = (g, r, c) => DrawChartIcon(g, r, c),
            ["sync"] = (g, r, c) => DrawSyncIcon(g, r, c),
            ["backup"] = (g, r, c) => DrawBackupIcon(g, r, c),
            ["export"] = (g, r, c) => DrawExportIcon(g, r, c),
            ["sort_asc"] = (g, r, c) => DrawSortAscIcon(g, r, c),
            ["sort_desc"] = (g, r, c) => DrawSortDescIcon(g, r, c)
        };

        private IconManager() { }

        public Image GetIcon(string name, int size = DefaultSize)
        {
            lock (_lockObject)
            {
                if (!_vectorIcons.ContainsKey(name))
                    throw new ArgumentException($"Icon {name} not found");

                if (!_iconCache.TryGetValue(name, out var sizeCache))
                {
                    sizeCache = new Dictionary<int, Bitmap>();
                    _iconCache[name] = sizeCache;
                }

                if (!sizeCache.TryGetValue(size, out var icon))
                {
                    icon = CreateIcon(name, size);
                    sizeCache[size] = icon;
                }

                return icon;
            }
        }

        private Bitmap CreateIcon(string name, int size)
        {
            var bitmap = new Bitmap(size, size);
            bitmap.SetResolution(96, 96);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                var rect = new Rectangle(0, 0, size, size);
                _vectorIcons[name](g, rect, Color.Black);
            }

            return bitmap;
        }

        public void ClearCache()
        {
            lock (_lockObject)
            {
                foreach (var sizeCache in _iconCache.Values)
                {
                    foreach (var bitmap in sizeCache.Values)
                    {
                        bitmap.Dispose();
                    }
                    sizeCache.Clear();
                }
                _iconCache.Clear();
            }
        }

        #region Vector Icon Drawing Methods
        private static void DrawAddIcon(Graphics g, Rectangle r, Color c)
        {
            using (var pen = new Pen(c, r.Width * 0.1f))
            {
                float center = r.Width / 2f;
                float margin = r.Width * 0.2f;
                
                g.DrawLine(pen, margin, center, r.Width - margin, center);
                g.DrawLine(pen, center, margin, center, r.Width - margin);
            }
        }

        private static void DrawDeleteIcon(Graphics g, Rectangle r, Color c)
        {
            using (var pen = new Pen(c, r.Width * 0.1f))
            {
                float margin = r.Width * 0.2f;
                g.DrawLine(pen, margin, margin, r.Width - margin, r.Width - margin);
                g.DrawLine(pen, margin, r.Width - margin, r.Width - margin, margin);
            }
        }

        private static void DrawEditIcon(Graphics g, Rectangle r, Color c)
        {
            using (var pen = new Pen(c, r.Width * 0.1f))
            {
                float margin = r.Width * 0.2f;
                var path = new GraphicsPath();
                path.AddLines(new[] {
                    new PointF(margin, r.Height - margin),
                    new PointF(r.Width - margin * 2, margin),
                    new PointF(r.Width - margin, margin + margin),
                    new PointF(margin + margin, r.Height - margin + margin),
                    new PointF(margin, r.Height - margin)
                });
                g.DrawPath(pen, path);
            }
        }

        private static void DrawSaveIcon(Graphics g, Rectangle r, Color c)
        {
            using (var pen = new Pen(c, r.Width * 0.1f))
            {
                float margin = r.Width * 0.2f;
                var rect = new RectangleF(margin, margin, r.Width - 2 * margin, r.Height - 2 * margin);
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawLine(pen, rect.X + rect.Width / 3, rect.Y, rect.X + rect.Width / 3, rect.Y + rect.Height);
            }
        }

        private static void DrawSearchIcon(Graphics g, Rectangle r, Color c)
        {
            using (var pen = new Pen(c, r.Width * 0.1f))
            {
                float margin = r.Width * 0.2f;
                float glassSize = r.Width * 0.4f;
                g.DrawEllipse(pen, margin, margin, glassSize, glassSize);
                float handleStart = margin + glassSize * 0.7f;
                g.DrawLine(pen, handleStart, handleStart, r.Width - margin, r.Height - margin);
            }
        }

        private static void DrawSettingsIcon(Graphics g, Rectangle r, Color c)
        {
            using (var pen = new Pen(c, r.Width * 0.1f))
            {
                float margin = r.Width * 0.2f;
                float spacing = (r.Height - 2 * margin) / 2f;
                for (int i = 0; i < 3; i++)
                {
                    float y = margin + i * spacing;
                    g.DrawLine(pen, margin, y, r.Width - margin, y);
                    float x = margin + i * spacing;
                    float circleSize = r.Width * 0.15f;
                    g.FillEllipse(new SolidBrush(c), x - circleSize/2, y - circleSize/2, circleSize, circleSize);
                }
            }
        }

        // Implement other icon drawing methods similarly...
        #endregion
    }
}