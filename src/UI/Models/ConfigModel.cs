using System;

namespace ListaCompras.UI.Models
{
    public class ConfigModel
    {
        // Tema
        public string Theme { get; set; } = "Light";
        public bool UsarTemaDoSistema { get; set; } = true;

        // Dados
        public bool SalvarLocalmente { get; set; } = true;
        public bool SincronizarAutomaticamente { get; set; } = true;
        public int IntervaloSincronizacao { get; set; } = 5; // minutos
        public bool NotificarAtualizacoes { get; set; } = true;

        // Interface
        public bool MostrarGraficos { get; set; } = true;
        public string TipoGrafico { get; set; } = "Linha"; // Linha, Barra, Candlestick
        public int QuantidadePontosGrafico { get; set; } = 30;
        public bool MostrarMediaMovel { get; set; } = true;
        public bool MostrarTendencia { get; set; } = true;
        public bool MostrarEstatisticas { get; set; } = true;

        // Alertas
        public bool AlertaPrecoAlto { get; set; } = true;
        public decimal PercentualAlertaPrecoAlto { get; set; } = 20; // %
        public bool AlertaPrecoBaixo { get; set; } = true;
        public decimal PercentualAlertaPrecoBaixo { get; set; } = 20; // %

        // Backup
        public bool BackupAutomatico { get; set; } = true;
        public string DiretorioBackup { get; set; } = "";
        public int DiasManterBackup { get; set; } = 30;

        // Exportação
        public string FormatoExportacao { get; set; } = "Excel"; // Excel, CSV, PDF
        public string DiretorioExportacao { get; set; } = "";
        public bool IncluirEstatisticas { get; set; } = true;
        public bool IncluirGraficos { get; set; } = true;

        public static ConfigModel Default => new ConfigModel();
    }
}