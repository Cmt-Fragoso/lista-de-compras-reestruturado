using System;

namespace ListaCompras.UI.Models
{
    public class ConfigModel
    {
        public bool IsDarkMode { get; set; }
        public string FormatoExportacao { get; set; } = "Excel";
        public bool IncluirEstatisticas { get; set; } = true;
        public bool IncluirGraficos { get; set; } = true;
        public string DiretorioExportacao { get; set; }
        public string DiretorioBackup { get; set; }
        public bool BackupAutomatico { get; set; }
        public int DiasManterBackup { get; set; } = 30;
    }
}