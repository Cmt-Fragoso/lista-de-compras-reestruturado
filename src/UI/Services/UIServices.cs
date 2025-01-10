using System;
using System.Windows.Forms;
using ListaCompras.UI.Themes;

namespace ListaCompras.UI.Services
{
    public class UIServices
    {
        public static void ShowError(string message, Control control, int timeout = 3000)
        {
            Show(message, NotificationType.Error, control, timeout);
        }

        public static void ShowSuccess(string message, Control control, int timeout = 3000)
        {
            Show(message, NotificationType.Success, control, timeout);
        }

        public static void ShowInfo(string message, Control control, int timeout = 3000)
        {
            Show(message, NotificationType.Info, control, timeout);
        }

        public static void ShowWarning(string message, Control control, int timeout = 3000)
        {
            Show(message, NotificationType.Warning, control, timeout);
        }

        private static void Show(string message, NotificationType type, Control control, int timeout)
        {
            var theme = ThemeManager.Instance.GetCurrentTheme();
            var notificationColor = type switch
            {
                NotificationType.Error => theme.Error,
                NotificationType.Success => theme.Success,
                NotificationType.Warning => theme.Warning,
                _ => theme.Info
            };

            // Implementação da notificação
            MessageBox.Show(message, type.ToString(), MessageBoxButtons.OK);
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}