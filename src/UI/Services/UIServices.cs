using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using ListaCompras.Core.Services;
using ListaCompras.UI.Models;
using ListaCompras.UI.ViewModels;

namespace ListaCompras.UI.Services
{
    public interface IDialogService
    {
        DialogResult ShowMessage(string message, string title = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None);
        DialogResult ShowQuestion(string message, string title = "");
        void ShowError(string message, string title = "Erro");
        string ShowOpenFileDialog(string filter = "Todos os arquivos (*.*)|*.*", string title = "Abrir arquivo");
        string ShowSaveFileDialog(string filter = "Todos os arquivos (*.*)|*.*", string title = "Salvar arquivo");
        string ShowFolderBrowserDialog(string description = "Selecione uma pasta");
    }

    public class DialogService : IDialogService
    {
        public DialogResult ShowMessage(string message, string title = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            return MessageBox.Show(message, title, buttons, icon);
        }

        public DialogResult ShowQuestion(string message, string title = "")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }

        public void ShowError(string message, string title = "Erro")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public string ShowOpenFileDialog(string filter = "Todos os arquivos (*.*)|*.*", string title = "Abrir arquivo")
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = filter;
                dialog.Title = title;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        public string ShowSaveFileDialog(string filter = "Todos os arquivos (*.*)|*.*", string title = "Salvar arquivo")
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = filter;
                dialog.Title = title;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }

        public string ShowFolderBrowserDialog(string description = "Selecione uma pasta")
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = description;
                return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }
    }

    public interface INotificationService
    {
        void ShowNotification(string message, NotificationType type = NotificationType.Info);
        void ShowNotificationWithAction(string message, Action action, string actionText, NotificationType type = NotificationType.Info);
        void ClearNotifications();
    }

    public class NotificationService : INotificationService
    {
        private readonly Form _mainForm;
        private readonly List<Form> _activeNotifications = new();
        private readonly int _maxNotifications = 3;
        private readonly int _notificationDuration = 3000; // ms

        public NotificationService(Form mainForm)
        {
            _mainForm = mainForm;
        }

        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            _mainForm.BeginInvoke(new Action(() =>
            {
                ManageNotifications();
                CreateNotification(message, type);
            }));
        }

        public void ShowNotificationWithAction(string message, Action action, string actionText, NotificationType type = NotificationType.Info)
        {
            _mainForm.BeginInvoke(new Action(() =>
            {
                ManageNotifications();
                CreateNotification(message, type, action, actionText);
            }));
        }

        public void ClearNotifications()
        {
            foreach (var notification in _activeNotifications.ToList())
            {
                notification.Close();
            }
            _activeNotifications.Clear();
        }

        private void ManageNotifications()
        {
            while (_activeNotifications.Count >= _maxNotifications)
            {
                var oldest = _activeNotifications[0];
                oldest.Close();
                _activeNotifications.RemoveAt(0);
            }
        }

        private void CreateNotification(string message, NotificationType type, Action action = null, string actionText = null)
        {
            var notification = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                Size = new System.Drawing.Size(300, 80)
            };

            // Posicionar no canto inferior direito
            var workingArea = Screen.GetWorkingArea(_mainForm);
            notification.Location = new System.Drawing.Point(
                workingArea.Right - notification.Width - 20,
                workingArea.Bottom - notification.Height - ((_activeNotifications.Count) * (notification.Height + 10))
            );

            // TODO: Adicionar controles e estilo baseado no tipo
            // ...

            _activeNotifications.Add(notification);
            notification.Show(_mainForm);

            if (action == null)
            {
                var timer = new Timer { Interval = _notificationDuration };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    notification.Close();
                    _activeNotifications.Remove(notification);
                };
                timer.Start();
            }
        }
    }

    public interface INavigationService
    {
        void NavigateTo<T>() where T : ViewModelBase;
        void NavigateBack();
        void NavigateToMain();
        bool CanNavigateBack { get; }
    }

    public class NavigationService : INavigationService
    {
        private readonly Stack<ViewModelBase> _navigationStack = new();
        private readonly MainWindowViewModel _mainViewModel;
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(MainWindowViewModel mainViewModel, IServiceProvider serviceProvider)
        {
            _mainViewModel = mainViewModel;
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo<T>() where T : ViewModelBase
        {
            var viewModel = (T)_serviceProvider.GetService(typeof(T));
            _navigationStack.Push(_mainViewModel.CurrentView);
            _mainViewModel.CurrentView = viewModel;
        }

        public void NavigateBack()
        {
            if (CanNavigateBack)
            {
                _mainViewModel.CurrentView = _navigationStack.Pop();
            }
        }

        public void NavigateToMain()
        {
            _navigationStack.Clear();
            _mainViewModel.CurrentView = _serviceProvider.GetService<ListasViewViewModel>();
        }

        public bool CanNavigateBack => _navigationStack.Count > 0;
    }

    public interface IStateService
    {
        T GetState<T>(string key) where T : class;
        void SetState<T>(string key, T state) where T : class;
        void ClearState(string key);
        void ClearAll();
    }

    public class StateService : IStateService
    {
        private readonly Dictionary<string, object> _states = new();

        public T GetState<T>(string key) where T : class
        {
            return _states.TryGetValue(key, out var state) ? state as T : null;
        }

        public void SetState<T>(string key, T state) where T : class
        {
            _states[key] = state;
        }

        public void ClearState(string key)
        {
            _states.Remove(key);
        }

        public void ClearAll()
        {
            _states.Clear();
        }
    }

    public interface IThemeService
    {
        void SetTheme(bool isDark);
        void ApplyTheme(Control control);
        void CustomizeTheme(ThemeSettingsModel settings);
        ThemeSettingsModel GetCurrentSettings();
    }

    public class ThemeService : IThemeService
    {
        private readonly ThemeManager _themeManager;
        private ThemeSettingsModel _currentSettings;

        public ThemeService()
        {
            _themeManager = ThemeManager.Instance;
            _currentSettings = new ThemeSettingsModel();
        }

        public void SetTheme(bool isDark)
        {
            _themeManager.SetTheme(isDark ? ThemeColors.Dark : ThemeColors.Default);
        }

        public void ApplyTheme(Control control)
        {
            _themeManager.ApplyTheme(control);
        }

        public void CustomizeTheme(ThemeSettingsModel settings)
        {
            _currentSettings = settings;
            // Aplicar configurações customizadas
            // ...
        }

        public ThemeSettingsModel GetCurrentSettings()
        {
            return _currentSettings;
        }
    }

    public interface IValidationService
    {
        ValidationModel Validate<T>(T model);
        void ApplyValidation(Control control, ValidationModel validation);
        void ClearValidation(Control control);
    }

    public class ValidationService : IValidationService
    {
        public ValidationModel Validate<T>(T model)
        {
            var validation = new ValidationModel { IsValid = true };
            
            // Implementar validação baseada em atributos ou regras
            // ...

            return validation;
        }

        public void ApplyValidation(Control control, ValidationModel validation)
        {
            // Aplicar indicadores visuais de validação
            if (!validation.IsValid)
            {
                control.BackColor = System.Drawing.Color.MistyRose;
                if (validation.Errors.TryGetValue(control.Name, out var errors))
                {
                    var tooltip = new ToolTip();
                    tooltip.SetToolTip(control, string.Join(Environment.NewLine, errors));
                }
            }
        }

        public void ClearValidation(Control control)
        {
            control.BackColor = System.Drawing.SystemColors.Window;
            foreach (ToolTip tooltip in control.Controls.OfType<ToolTip>())
            {
                tooltip.RemoveAll();
            }
        }
    }
}
