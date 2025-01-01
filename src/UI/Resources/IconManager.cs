using System;
using System.Drawing;
using System.Collections.Generic;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Resources
{
    public class IconManager
    {
        private static IconManager _instance;
        public static IconManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new IconManager();
                return _instance;
            }
        }

        private Dictionary<string, IconSet> _icons;
        private ThemeColors _currentTheme;

        private IconManager()
        {
            _icons = new Dictionary<string, IconSet>();
            _currentTheme = ThemeManager.Instance.CurrentTheme;
            ThemeManager.Instance.ThemeChanged += (s, e) => 
            {
                _currentTheme = ThemeManager.Instance.CurrentTheme;
                RegenerateIcons();
            };

            InitializeIcons();
        }

        private void InitializeIcons()
        {
            // Ícones de Navegação
            AddIcon("menu", CreateMenuIcon);
            AddIcon("back", CreateBackIcon);
            AddIcon("forward", CreateForwardIcon);
            AddIcon("home", CreateHomeIcon);

            // Ícones de Ação
            AddIcon("add", CreateAddIcon);
            AddIcon("edit", CreateEditIcon);
            AddIcon("delete", CreateDeleteIcon);
            AddIcon("save", CreateSaveIcon);
            AddIcon("cancel", CreateCancelIcon);
            AddIcon("search", CreateSearchIcon);
            AddIcon("filter", CreateFilterIcon);
            AddIcon("sort", CreateSortIcon);
            
            // Ícones de Lista
            AddIcon("list", CreateListIcon);
            AddIcon("grid", CreateGridIcon);
            AddIcon("check", CreateCheckIcon);
            AddIcon("uncheck", CreateUncheckIcon);
            
            // Ícones de Dados
            AddIcon("chart", CreateChartIcon);
            AddIcon("export", CreateExportIcon);
            AddIcon("import", CreateImportIcon);
            AddIcon("sync", CreateSyncIcon);
            
            // Ícones de Sistema
            AddIcon("settings", CreateSettingsIcon);
            AddIcon("info", CreateInfoIcon);
            AddIcon("warning", CreateWarningIcon);
            AddIcon("error", CreateErrorIcon);
            AddIcon("success", CreateSuccessIcon);
        }

        private void AddIcon(string name, Func<ThemeColors, Size, Bitmap> creator)
        {
            _icons[name] = new IconSet(creator);
        }

        public Bitmap GetIcon(string name, int size = 16)
        {
            if (!_icons.ContainsKey(name))
                throw new ArgumentException($"Ícone '{name}' não encontrado");

            return _icons[name].GetIcon(_currentTheme, new Size(size, size));
        }

        private void RegenerateIcons()
        {
            foreach (var iconSet in _icons.Values)
            {
                iconSet.ClearCache();
            }
        }

        #region Icon Creators
        private Bitmap CreateMenuIcon(ThemeColors theme, Size size)
        {
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            using (var pen = new Pen(theme.TextPrimary, size.Width / 8f))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                float y1 = size.Height * 0.25f;
                float y2 = size.Height * 0.5f;
                float y3 = size.Height * 0.75f;
                
                g.DrawLine(pen, size.Width * 0.2f, y1, size.Width * 0.8f, y1);
                g.DrawLine(pen, size.Width * 0.2f, y2, size.Width * 0.8f, y2);
                g.DrawLine(pen, size.Width * 0.2f, y3, size.Width * 0.8f, y3);
            }
            return bmp;
        }

        private Bitmap CreateAddIcon(ThemeColors theme, Size size)
        {
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            using (var pen = new Pen(theme.Success, size.Width / 8f))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                float mid = size.Width / 2f;
                g.DrawLine(pen, mid, size.Height * 0.2f, mid, size.Height * 0.8f);
                g.DrawLine(pen, size.Width * 0.2f, mid, size.Width * 0.8f, mid);
            }
            return bmp;
        }

        private Bitmap CreateDeleteIcon(ThemeColors theme, Size size)
        {
            var bmp = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bmp))
            using (var pen = new Pen(theme.Error, size.Width / 8f))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                g.DrawLine(pen, 
                    size.Width * 0.2f, size.Height * 0.2f,
                    size.Width * 0.8f, size.Height * 0.8f);
                g.DrawLine(pen,
                    size.Width * 0.2f, size.Height * 0.8f,
                    size.Width * 0.8f, size.Height * 0.2f);
            }
            return bmp;
        }

        // [Implementar outros criadores de ícones conforme necessário]
        #endregion
    }

    internal class IconSet
    {
        private readonly Func<ThemeColors, Size, Bitmap> _creator;
        private Dictionary<string, Bitmap> _cache;

        public IconSet(Func<ThemeColors, Size, Bitmap> creator)
        {
            _creator = creator;
            _cache = new Dictionary<string, Bitmap>();
        }

        public Bitmap GetIcon(ThemeColors theme, Size size)
        {
            string key = $"{theme.GetHashCode()}_{size.Width}_{size.Height}";
            
            if (!_cache.ContainsKey(key))
            {
                _cache[key] = _creator(theme, size);
            }

            return _cache[key];
        }

        public void ClearCache()
        {
            foreach (var icon in _cache.Values)
            {
                icon.Dispose();
            }
            _cache.Clear();
        }
    }
}