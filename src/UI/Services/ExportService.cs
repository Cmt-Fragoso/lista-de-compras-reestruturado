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
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;

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
                var worksheet = package.Workbook.Worksheets.Add("Histórico de Preços");

                // Estilo do título
                worksheet.Cells["A1:F1"].Merge = true;
                worksheet.Cells["A1"].Value = $"Histórico de Preços - {item.Nome}";
                worksheet.Cells["A1"].Style.Font.Size = 14;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1:F1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells["A1:F1"].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);

                // Cabeçalhos
                var headers = new[] { "Data", "Valor", "Local", "Observação" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[3, i + 1].Value = headers[i];
                    worksheet.Cells[3, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    worksheet.Cells[3, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Dados
                var row = 4;
                foreach (var preco in precos.OrderBy(p => p.Data))
                {
                    worksheet.Cells[row, 1].Value = preco.Data;
                    worksheet.Cells[row, 2].Value = preco.Valor;
                    worksheet.Cells[row, 3].Value = preco.Local;
                    worksheet.Cells[row, 4].Value = preco.Observacao;

                    // Estilo alternado de linhas
                    var range = worksheet.Cells[row, 1, row, 4];
                    if (row % 2 == 0)
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    }

                    row++;
                }

                // Formatação das colunas
                worksheet.Column(1).Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
                worksheet.Column(2).Style.Numberformat.Format = "R$ #,##0.00";
                worksheet.Cells.AutoFitColumns();

                // Adicionar bordas à tabela
                var table = worksheet.Cells[3, 1, row - 1, 4];
                table.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                if (_config.IncluirEstatisticas)
                {
                    AddStatisticsToExcel(worksheet, precos, row + 2);
                }

                if (_config.IncluirGraficos && precos.Count > 0)
                {
                    // Gráfico de linha
                    AddLineChartToExcel(worksheet, precos, row + 2);

                    // Gráfico de barras
                    AddBarChartToExcel(worksheet, precos, row + 25);

                    // Gráfico de variação percentual
                    AddPercentageChangeChartToExcel(worksheet, precos, row + 48);
                }

                await package.SaveAsAsync(new FileInfo(fileName));
            }
        }

        private void AddLineChartToExcel(ExcelWorksheet worksheet, List<PrecoModel> precos, int startRow)
        {
            var lineChart = worksheet.Drawings.AddChart("PriceLineChart", eChartType.Line);
            lineChart.SetPosition(startRow, 0, 0, 0);
            lineChart.SetSize(800, 400);

            var series = lineChart.Series.Add(
                worksheet.Cells[$"B4:B{precos.Count + 3}"],
                worksheet.Cells[$"A4:A{precos.Count + 3}"]);
            
            series.Header = "Preço";
            lineChart.Title.Text = "Evolução do Preço";
            lineChart.YAxis.Title.Text = "Valor (R$)";
            lineChart.XAxis.Title.Text = "Data";
            
            // Estilo
            series.Fill.Color = Color.RoyalBlue;
            lineChart.Legend.Position = eLegendPosition.Bottom;
            lineChart.Border.Fill.Style = eFillStyle.SolidFill;
            lineChart.Border.Fill.Color = Color.DarkGray;
        }

        private void AddBarChartToExcel(ExcelWorksheet worksheet, List<PrecoModel> precos, int startRow)
        {
            var barChart = worksheet.Drawings.AddChart("PriceBarChart", eChartType.ColumnClustered);
            barChart.SetPosition(startRow, 0, 0, 0);
            barChart.SetSize(800, 400);

            var series = barChart.Series.Add(
                worksheet.Cells[$"B4:B{precos.Count + 3}"],
                worksheet.Cells[$"A4:A{precos.Count + 3}"]);
            
            series.Header = "Preço";
            barChart.Title.Text = "Preços por Data";
            barChart.YAxis.Title.Text = "Valor (R$)";
            barChart.XAxis.Title.Text = "Data";

            // Estilo
            series.Fill.Color = Color.CornflowerBlue;
            barChart.Legend.Position = eLegendPosition.Bottom;
        }

        private void AddPercentageChangeChartToExcel(ExcelWorksheet worksheet, List<PrecoModel> precos, int startRow)
        {
            // Calcular variação percentual
            var sortedPrecos = precos.OrderBy(p => p.Data).ToList();
            var percentChanges = new List<decimal>();
            var previousPrice = sortedPrecos[0].Valor;

            for (int i = 1; i < sortedPrecos.Count; i++)
            {
                var percentChange = ((sortedPrecos[i].Valor - previousPrice) / previousPrice) * 100;
                percentChanges.Add(percentChange);
                previousPrice = sortedPrecos[i].Valor;
            }

            // Adicionar dados de variação
            var changeRow = startRow - 20;
            worksheet.Cells[changeRow, 6].Value = "Data";
            worksheet.Cells[changeRow, 7].Value = "Variação %";

            for (int i = 0; i < percentChanges.Count; i++)
            {
                worksheet.Cells[changeRow + i + 1, 6].Value = sortedPrecos[i + 1].Data;
                worksheet.Cells[changeRow + i + 1, 7].Value = percentChanges[i];
            }

            // Criar gráfico
            var changeChart = worksheet.Drawings.AddChart("PriceChangeChart", eChartType.Line);
            changeChart.SetPosition(startRow, 0, 0, 0);
            changeChart.SetSize(800, 400);

            var series = changeChart.Series.Add(
                worksheet.Cells[changeRow + 1, 7, changeRow + percentChanges.Count, 7],
                worksheet.Cells[changeRow + 1, 6, changeRow + percentChanges.Count, 6]);

            series.Header = "Variação Percentual";
            changeChart.Title.Text = "Variação Percentual do Preço";
            changeChart.YAxis.Title.Text = "Variação (%)";
            changeChart.XAxis.Title.Text = "Data";

            // Estilo
            series.Fill.Color = Color.Orange;
            changeChart.Legend.Position = eLegendPosition.Bottom;
            
            // Adicionar linha de referência zero
            var zeroLine = changeChart.PlotArea.ChartTypes.Add(eChartType.Line);
            zeroLine.Series.Add();
            zeroLine.Series[0].Fill.Color = Color.Red;
            zeroLine.Series[0].Border.Width = 1;
            zeroLine.Series[0].Border.DashType = eDashStyle.Dash;
        }

        private async Task ExportToPdfAsync(ItemModel item, List<PrecoModel> precos, string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                // Título
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var title = new Paragraph($"Histórico de Preços - {item.Nome}", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20f;
                document.Add(title);

                // Tabela de dados
                var table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2f, 1.5f, 2f, 3f });

                // Cabeçalhos
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                foreach (var header in new[] { "Data", "Valor", "Local", "Observação" })
                {
                    var cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(211, 211, 211);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.Padding = 5;
                    table.AddCell(cell);
                }

                // Dados
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                foreach (var preco in precos.OrderBy(p => p.Data))
                {
                    table.AddCell(new Phrase(preco.Data.ToString("dd/MM/yyyy HH:mm"), normalFont));
                    table.AddCell(new Phrase(preco.Valor.ToString("C2"), normalFont));
                    table.AddCell(new Phrase(preco.Local, normalFont));
                    table.AddCell(new Phrase(preco.Observacao ?? "", normalFont));
                }

                document.Add(table);

                // Estatísticas
                if (_config.IncluirEstatisticas)
                {
                    document.Add(new Paragraph("\n"));
                    var statsTitle = new Paragraph("Estatísticas", headerFont);
                    statsTitle.SpacingAfter = 10f;
                    document.Add(statsTitle);

                    var values = precos.Select(p => p.Valor);
                    var stats = new Dictionary<string, decimal>
                    {
                        { "Menor Preço", values.Min() },
                        { "Maior Preço", values.Max() },
                        { "Preço Médio", values.Average() },
                        { "Mediana", GetMedian(values) }
                    };

                    foreach (var stat in stats)
                    {
                        document.Add(new Paragraph($"{stat.Key}: {stat.Value:C2}", normalFont));
                    }
                }

                // Gráficos
                if (_config.IncluirGraficos && precos.Count > 0)
                {
                    document.Add(new Paragraph("\n"));
                    var chartTitle = new Paragraph("Gráficos", headerFont);
                    chartTitle.SpacingAfter = 10f;
                    document.Add(chartTitle);

                    // TODO: Implementar geração de gráficos para PDF
                    // Requer biblioteca adicional para geração de gráficos
                    document.Add(new Paragraph("Gráficos não disponíveis na exportação PDF", normalFont));
                }

                document.Close();
            }
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

        [Códigos anteriores mantidos...]
    }
}