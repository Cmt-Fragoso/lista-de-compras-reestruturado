using System;
using System.Collections.Generic;
using System.Drawing;

namespace ListaCompras.UI.Models
{
    public class ConfigModel
    {
        public string FormatoExportacao { get; set; } = "excel";
        public bool IncluirEstatisticas { get; set; } = true;
        public bool IncluirGraficos { get; set; } = true;
        public string PastaBackup { get; set; }
        public bool BackupAutomatico { get; set; }
        public int IntervaloBackup { get; set; } = 60; // minutos
        public bool TemaEscuro { get; set; }
        public Font FontePadrao { get; set; }
        public int ItensPerPage { get; set; } = 20;
    }

    public class UIStateModel
    {
        public bool IsLoading { get; set; }
        public string LoadingMessage { get; set; }
        public bool HasErrors { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> ViewState { get; set; } = new();
    }

    public class ThemeSettingsModel
    {
        public string ThemeName { get; set; } = "Default";
        public float FontSize { get; set; } = 9.0f;
        public string FontFamily { get; set; } = "Segoe UI";
        public Dictionary<string, Color> CustomColors { get; set; } = new();
    }

    public class ExportSettingsModel
    {
        public string DefaultFormat { get; set; } = "excel";
        public string OutputPath { get; set; }
        public bool AutoGenerateFilename { get; set; } = true;
        public Dictionary<string, bool> IncludeFields { get; set; } = new()
        {
            ["data"] = true,
            ["valor"] = true,
            ["local"] = true,
            ["observacao"] = true
        };
        public Dictionary<string, bool> ChartSettings { get; set; } = new()
        {
            ["linha"] = true,
            ["barra"] = true,
            ["variacao"] = true
        };
    }

    public class NotificationModel
    {
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);
        public bool RequiresAction { get; set; }
        public Action OnAction { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class ValidationModel
    {
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        public Action<string, string> OnError { get; set; }
        public Action<string> OnClear { get; set; }
    }

    public class DialogModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public DialogType Type { get; set; }
        public List<DialogButton> Buttons { get; set; } = new();
        public bool ShowCancel { get; set; } = true;
        public Action<DialogResult> OnResult { get; set; }
    }

    public enum DialogType
    {
        Info,
        Warning,
        Error,
        Confirmation,
        Custom
    }

    public class DialogButton
    {
        public string Text { get; set; }
        public DialogResult Result { get; set; }
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }
    }

    public class FormStateModel
    {
        public bool IsDirty { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, bool> FieldStates { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public Action OnStateChanged { get; set; }
    }
}
