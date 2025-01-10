namespace ListaCompras.UI.Models
{
    public class ThemeSettingsModel
    {
        public bool IsDarkMode { get; set; }
        public string PrimaryColor { get; set; } = "#1E90FF";
        public string SecondaryColor { get; set; } = "#4682B4";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public string TextColor { get; set; } = "#000000";
        public float FontSize { get; set; } = 12.0f;
        public string FontFamily { get; set; } = "Segoe UI";
    }

    public class ValidationModel
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string Field { get; set; }
        public ValidationModel[] Children { get; set; }

        public ValidationModel()
        {
            IsValid = true;
            Message = string.Empty;
            Field = string.Empty;
            Children = Array.Empty<ValidationModel>();
        }
    }
}