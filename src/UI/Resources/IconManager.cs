using System;
using System.Drawing;
using System.Collections.Generic;

namespace ListaCompras.UI.Resources
{
    public class IconManager
    {
        private static readonly Dictionary<string, Image> _iconCache = new();

        public static Image GetIcon(string name)
        {
            if (_iconCache.TryGetValue(name, out var icon))
            {
                return icon;
            }

            var newIcon = DrawIcon(name);
            _iconCache[name] = newIcon;
            return newIcon;
        }

        private static Image DrawIcon(string name)
        {
            var bitmap = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                switch (name.ToLower())
                {
                    case "cancel":
                        DrawCancelIcon(g);
                        break;
                    case "list":
                        DrawListIcon(g);
                        break;
                    case "price":
                        DrawPriceIcon(g);
                        break;
                    case "chart":
                        DrawChartIcon(g);
                        break;
                    case "sync":
                        DrawSyncIcon(g);
                        break;
                    case "backup":
                        DrawBackupIcon(g);
                        break;
                    case "export":
                        DrawExportIcon(g);
                        break;
                    case "sort-asc":
                        DrawSortAscIcon(g);
                        break;
                    case "sort-desc":
                        DrawSortDescIcon(g);
                        break;
                }
            }

            return bitmap;
        }

        private static void DrawCancelIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawLine(pen, 6, 6, 18, 18);
            g.DrawLine(pen, 6, 18, 18, 6);
        }

        private static void DrawListIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            for (int i = 0; i < 3; i++)
            {
                g.DrawLine(pen, 8, 8 + (i * 6), 16, 8 + (i * 6));
            }
        }

        private static void DrawPriceIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawString("$", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, 6, 4);
        }

        private static void DrawChartIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawLine(pen, 4, 20, 4, 4);
            g.DrawLine(pen, 4, 20, 20, 20);
            g.DrawLine(pen, 8, 16, 8, 8);
            g.DrawLine(pen, 12, 12, 12, 8);
            g.DrawLine(pen, 16, 8, 16, 4);
        }

        private static void DrawSyncIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawArc(pen, 4, 4, 16, 16, 0, 300);
            g.DrawLine(pen, 16, 4, 20, 8);
            g.DrawLine(pen, 16, 4, 12, 8);
        }

        private static void DrawBackupIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawRectangle(pen, 4, 4, 16, 16);
            g.DrawLine(pen, 8, 12, 12, 16);
            g.DrawLine(pen, 16, 12, 12, 16);
        }

        private static void DrawExportIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawRectangle(pen, 4, 4, 16, 16);
            g.DrawLine(pen, 12, 8, 12, 16);
            g.DrawLine(pen, 8, 12, 12, 16);
            g.DrawLine(pen, 16, 12, 12, 16);
        }

        private static void DrawSortAscIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawLine(pen, 12, 4, 12, 20);
            g.DrawLine(pen, 8, 8, 12, 4);
            g.DrawLine(pen, 16, 8, 12, 4);
        }

        private static void DrawSortDescIcon(Graphics g)
        {
            using var pen = new Pen(Color.Black, 2);
            g.DrawLine(pen, 12, 4, 12, 20);
            g.DrawLine(pen, 8, 16, 12, 20);
            g.DrawLine(pen, 16, 16, 12, 20);
        }
    }
}