using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ListaCompras.UI.Themes
{
    /// <summary>
    /// Gerenciador centralizado de temas da aplicação
    /// </summary>
    public class ThemeManager
    {
        private static readonly Lazy<ThemeManager> _instance = 
            new(() => new ThemeManager());

        public static ThemeManager Instance => _instance.Value;

        private readonly Dictionary<string, ThemeSettings> _themes;
        private readonly ILogger<ThemeManager> _logger;
        private ThemeSettings _currentTheme;
        private bool _isDarkMode;

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        private ThemeManager()
        {
            _themes = new Dictionary<string, ThemeSettings>();
            _currentTheme = new ThemeSettings();
            InitializeDefaultThemes();
        }

        public ThemeManager(ILogger<ThemeManager> logger) : this()
        {
            _logger = logger;
        }

        #region Propriedades Públicas

        public ThemeSettings CurrentTheme => _currentTheme;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    ApplyDarkMode();
                }
            }
        }

        public IEnumerable<string> AvailableThemes => _themes.Keys;

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Define o tema atual
        /// </summary>
        public void SetTheme(string themeName)
        {
            if (!_themes.ContainsKey(themeName))
                throw new ArgumentException($"Tema '{themeName}' não encontrado");

            var oldTheme = _currentTheme;
            _currentTheme = _themes[themeName];

            OnThemeChanged(oldTheme, _currentTheme);
            _logger?.LogInformation("Tema alterado para: {Theme}", themeName);
        }

        /// <summary>
        /// Aplica o tema atual ao controle
        /// </summary>
        public void ApplyTheme(Control control)
        {
            if (control == null) return;

            try
            {
                // Aplica tema ao controle atual
                ApplyThemeToControl(control);

                // Aplica recursivamente aos controles filhos
                foreach (Control child in control.Controls)
                {
                    ApplyTheme(child);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao aplicar tema ao controle {Control}", control.Name);
            }
        }

        /// <summary>
        /// Registra um novo tema
        /// </summary>
        public void RegisterTheme(string name, ThemeSettings settings)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nome do tema não pode ser vazio");

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _themes[name] = settings;
            _logger?.LogInformation("Tema registrado: {Theme}", name);
        }

        /// <summary>
        /// Carrega tema de um arquivo JSON
        /// </summary>
        public void LoadThemeFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Arquivo de tema não encontrado", filePath);

            try
            {
                var json = File.ReadAllText(filePath);
                var theme = JsonSerializer.Deserialize<ThemeSettings>(json);
                var name = Path.GetFileNameWithoutExtension(filePath);

                RegisterTheme(name, theme);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao carregar tema do arquivo {File}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Salva o tema atual em arquivo JSON
        /// </summary>
        public void SaveThemeToFile(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(_currentTheme, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);
                _logger?.LogInformation("Tema salvo em: {File}", filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao salvar tema no arquivo {File}", filePath);
                throw;
            }
        }

        #endregion

        #region Métodos Privados

        private void InitializeDefaultThemes()
        {
            // Tema Claro (Padrão)
            RegisterTheme("Light", new ThemeSettings
            {
                Name = "Light",
                Background = Color.White,
                Foreground = Color.Black,
                Primary = ColorTranslator.FromHtml("#007AFF"),
                Secondary = ColorTranslator.FromHtml("#5856D6"),
                Success = ColorTranslator.FromHtml("#34C759"),
                Warning = ColorTranslator.FromHtml("#FF9500"),
                Error = ColorTranslator.FromHtml("#FF3B30"),
                Surface = ColorTranslator.FromHtml("#F2F2F7"),
                Border = ColorTranslator.FromHtml("#C6C6C8"),
                TextPrimary = Color.Black,
                TextSecondary = ColorTranslator.FromHtml("#8E8E93"),
                BaseFont = new Font("Segoe UI", 9f)
            });

            // Tema Escuro
            RegisterTheme("Dark", new ThemeSettings
            {
                Name = "Dark",
                Background = Color.Black,
                Foreground = Color.White,
                Primary = ColorTranslator.FromHtml("#0A84FF"),
                Secondary = ColorTranslator.FromHtml("#5E5CE6"),
                Success = ColorTranslator.FromHtml("#32D74B"),
                Warning = ColorTranslator.FromHtml("#FF9F0A"),
                Error = ColorTranslator.FromHtml("#FF453A"),
                Surface = ColorTranslator.FromHtml("#1C1C1E"),
                Border = ColorTranslator.FromHtml("#38383A"),
                TextPrimary = Color.White,
                TextSecondary = ColorTranslator.FromHtml("#8E8E93"),
                BaseFont = new Font("Segoe UI", 9f)
            });

            // Tema Alto Contraste
            RegisterTheme("HighContrast", new ThemeSettings
            {
                Name = "HighContrast",
                Background = Color.Black,
                Foreground = Color.White,
                Primary = Color.Yellow,
                Secondary = Color.Cyan,
                Success = Color.Lime,
                Warning = Color.Yellow,
                Error = Color.Red,
                Surface = Color.Black,
                Border = Color.White,
                TextPrimary = Color.White,
                TextSecondary = Color.Yellow,
                BaseFont = new Font("Segoe UI", 10f, FontStyle.Bold)
            });
        }

        private void ApplyThemeToControl(Control control)
        {
            // Cores básicas
            control.BackColor = _currentTheme.Background;
            control.ForeColor = _currentTheme.Foreground;
            control.Font = _currentTheme.BaseFont;

            // Aplicação específica por tipo de controle
            switch (control)
            {
                case Button button:
                    ApplyThemeToButton(button);
                    break;

                case TextBox textBox:
                    ApplyThemeToTextBox(textBox);
                    break;

                case ComboBox comboBox:
                    ApplyThemeToComboBox(comboBox);
                    break;

                case ListView listView:
                    ApplyThemeToListView(listView);
                    break;

                case DataGridView gridView:
                    ApplyThemeToDataGridView(gridView);
                    break;

                case Form form:
                    ApplyThemeToForm(form);
                    break;
            }
        }

        private void ApplyThemeToButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = _currentTheme.Border;
            button.BackColor = _currentTheme.Primary;
            button.ForeColor = _currentTheme.TextPrimary;

            if (button.Tag?.ToString() == "secondary")
            {
                button.BackColor = _currentTheme.Secondary;
            }
            else if (button.Tag?.ToString() == "danger")
            {
                button.BackColor = _currentTheme.Error;
            }
        }

        private void ApplyThemeToTextBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = _currentTheme.Surface;
            textBox.ForeColor = _currentTheme.TextPrimary;
        }

        private void ApplyThemeToComboBox(ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = _currentTheme.Surface;
            comboBox.ForeColor = _currentTheme.TextPrimary;
        }

        private void ApplyThemeToListView(ListView listView)
        {
            listView.BackColor = _currentTheme.Surface;
            listView.ForeColor = _currentTheme.TextPrimary;
            listView.BorderStyle = BorderStyle.FixedSingle;
        }

        private void ApplyThemeToDataGridView(DataGridView gridView)
        {
            gridView.BackgroundColor = _currentTheme.Surface;
            gridView.ForeColor = _currentTheme.TextPrimary;
            gridView.GridColor = _currentTheme.Border;
            gridView.DefaultCellStyle.BackColor = _currentTheme.Background;
            gridView.DefaultCellStyle.ForeColor = _currentTheme.TextPrimary;
            gridView.DefaultCellStyle.SelectionBackColor = _currentTheme.Primary;
            gridView.DefaultCellStyle.SelectionForeColor = _currentTheme.TextPrimary;
        }

        private void ApplyThemeToForm(Form form)
        {
            form.BackColor = _currentTheme.Background;
            form.ForeColor = _currentTheme.TextPrimary;
        }

        private void ApplyDarkMode()
        {
            SetTheme(_isDarkMode ? "Dark" : "Light");
        }

        private void OnThemeChanged(ThemeSettings oldTheme, ThemeSettings newTheme)
        {
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, newTheme));
        }

        #endregion
    }

    public class ThemeSettings
    {
        public string Name { get; set; }
        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Color Primary { get; set; }
        public Color Secondary { get; set; }
        public Color Success { get; set; }
        public Color Warning { get; set; }
        public Color Error { get; set; }
        public Color Surface { get; set; }
        public Color Border { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Font BaseFont { get; set; }
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeSettings OldTheme { get; }
        public ThemeSettings NewTheme { get; }

        public ThemeChangedEventArgs(ThemeSettings oldTheme, ThemeSettings newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
        }
    }
}