using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using ListaCompras.Core.Models;
using ListaCompras.UI.Models;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace ListaCompras.UI.Services
{
    public class ExportService
    {
        private readonly IDataService _dataService;
        private readonly ConfigModel _config;

        public ExportService(IDataService dataService, ConfigModel config)
        {
            _dataService = dataService;
            _config = config;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task ExportPrecosAsync(int itemId, string fileName)
        {
            var precos = await _dataService.GetPrecosAsync(itemId);
            var item = await _dataService.GetItemByIdAsync(itemId);

            switch (_config.FormatoExportacao.ToLower())
            {
                case "excel":
                    await ExportToExcelAsync(item, precos, fileName);
                    break;
                case "csv":
                    await ExportToCsvAsync(precos, fileName);
                    break;
                case "pdf":
                    await ExportToPdfAsync(item, precos, fileName);
                    break;
                default:
                    throw new ArgumentException("Formato de exportação não suportado");
            }
        }

        private async Task ExportToExcelAsync(ItemModel item, List<PrecoModel> precos, string fileName)
        {
            using (var package = new ExcelPackage())
            {
                // Planilha de Dados
                var worksheet = package.Workbook.Worksheets.Add("Histórico de Preços");

                // Título
                worksheet.Cells["A1:F1"].Merge = true;
                worksheet.Cells["A1"].Value = $"Histórico de Preços - {item.Nome}";
                worksheet.Cells["A1"].Style.Font.Size = 14;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Cabeçalhos
                worksheet.Cells["A3"].Value = "Data";
                worksheet.Cells["B3"].Value = "Valor";
                worksheet.Cells["C3"].Value = "Local";
                worksheet.Cells["D3"].Value = "Observação";

                // Estilo dos cabeçalhos
                var headerRange = worksheet.Cells["A3:D3"];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // Dados
                var row = 4;
                foreach (var preco in precos.OrderBy(p => p.Data))
                {
                    worksheet.Cells[row, 1].Value = preco.Data;
                    worksheet.Cells[row, 2].Value = preco.Valor;
                    worksheet.Cells[row, 3].Value = preco.Local;
                    worksheet.Cells[row, 4].Value = preco.Observacao;
                    row++;
                }

                // Formatar colunas
                worksheet.Column(1).Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
                worksheet.Column(2).Style.Numberformat.Format = "R$ #,##0.00";

                // Ajustar largura das colunas
                worksheet.Cells.AutoFitColumns();

                // Adicionar estatísticas se configurado
                if (_config.IncluirEstatisticas)
                {
                    AddStatisticsToExcel(worksheet, precos, row + 2);
                }

                // Adicionar gráfico se configurado
                if (_config.IncluirGraficos && precos.Count > 0)
                {
                    AddChartToExcel(worksheet, precos, row + 7);
                }

                // Salvar arquivo
                await package.SaveAsAsync(new FileInfo(fileName));
            }
        }

        private void AddStatisticsToExcel(ExcelWorksheet worksheet, List<PrecoModel> precos, int startRow)
        {
            worksheet.Cells[startRow, 1].Value = "Estatísticas";
            worksheet.Cells[startRow, 1].Style.Font.Bold = true;

            var values = precos.Select(p => p.Valor);
            var stats = new Dictionary<string, decimal>
            {
                { "Menor Preço", values.Min() },
                { "Maior Preço", values.Max() },
                { "Preço Médio", values.Average() },
                { "Mediana", GetMedian(values) }
            };

            var row = startRow + 1;
            foreach (var stat in stats)
            {
                worksheet.Cells[row, 1].Value = stat.Key;
                worksheet.Cells[row, 2].Value = stat.Value;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "R$ #,##0.00";
                row++;
            }
        }

        private void AddChartToExcel(ExcelWorksheet worksheet, List<PrecoModel> precos, int startRow)
        {
            var chart = worksheet.Drawings.AddChart("PriceChart", eChartType.Line);
            chart.SetPosition(startRow, 0, 0, 0);
            chart.SetSize(600, 300);

            var series = chart.Series.Add(worksheet.Cells["B4:B" + (precos.Count + 3)],
                                        worksheet.Cells["A4:A" + (precos.Count + 3)]);
            series.Header = "Preço";

            chart.Title.Text = "Evolução do Preço";
            chart.YAxis.Title.Text = "Valor (R$)";
            chart.XAxis.Title.Text = "Data";
        }

        private async Task ExportToCsvAsync(List<PrecoModel> precos, string fileName)
        {
            var lines = new List<string>
            {
                "Data,Valor,Local,Observacao"
            };

            foreach (var preco in precos.OrderBy(p => p.Data))
            {
                lines.Add($"{preco.Data:dd/MM/yyyy HH:mm}," +
                         $"{preco.Valor:F2}," +
                         $"\"{preco.Local.Replace("\"", "\"\"")}\"," +
                         $"\"{preco.Observacao?.Replace("\"", "\"\"")}\"");
            }

            await File.WriteAllLinesAsync(fileName, lines);
        }

        private async Task ExportToPdfAsync(ItemModel item, List<PrecoModel> precos, string fileName)
        {
            // TODO: Implementar exportação para PDF
            // Requer biblioteca adicional como iTextSharp ou similar
            throw new NotImplementedException("Exportação para PDF ainda não implementada");
        }

        private decimal GetMedian(IEnumerable<decimal> values)
        {
            var sorted = values.OrderBy(n => n).ToList();
            var count = sorted.Count;
            var mid = count / 2;

            return count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2
                : sorted[mid];
        }
    }
}