using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections.Generic;
using ListaCompras.Core.Models;
using ListaCompras.UI.Models;
using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ListaCompras.UI.Services
{
    public class ExportService
    {
        public async Task ExportPrecosAsync(List<PrecoModel> precos, string fileName)
        {
            if (fileName.EndsWith(".xlsx"))
            {
                await ExportToExcelAsync(precos, fileName);
            }
            else if (fileName.EndsWith(".pdf"))
            {
                await ExportToPdfAsync(precos, fileName);
            }
            else if (fileName.EndsWith(".csv"))
            {
                await ExportToCsvAsync(precos, fileName);
            }
        }

        private async Task ExportToExcelAsync(List<PrecoModel> precos, string fileName)
        {
            await Task.Run(() => {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Histórico de Preços");

                // Cabeçalhos
                worksheet.Cells["A1"].Value = "Data";
                worksheet.Cells["B1"].Value = "Valor";
                worksheet.Cells["C1"].Value = "Local";
                worksheet.Cells["D1"].Value = "Observação";

                // Dados
                var row = 2;
                foreach (var preco in precos.OrderBy(p => p.Data))
                {
                    worksheet.Cells[row, 1].Value = preco.Data;
                    worksheet.Cells[row, 2].Value = preco.Valor;
                    worksheet.Cells[row, 3].Value = preco.Local;
                    worksheet.Cells[row, 4].Value = preco.Observacao;
                    row++;
                }

                // Formatação
                worksheet.Cells[1, 1, 1, 4].Style.Font.Bold = true;
                worksheet.Cells[2, 1, row - 1, 4].AutoFitColumns();

                package.SaveAs(new FileInfo(fileName));
            });
        }

        private async Task ExportToPdfAsync(List<PrecoModel> precos, string fileName)
        {
            await Task.Run(() => {
                using var fs = new FileStream(fileName, FileMode.Create);
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, fs);
                document.Open();

                var table = new PdfPTable(4);
                table.AddCell("Data");
                table.AddCell("Valor");
                table.AddCell("Local");
                table.AddCell("Observação");

                foreach (var preco in precos.OrderBy(p => p.Data))
                {
                    table.AddCell(preco.Data.ToString("dd/MM/yyyy"));
                    table.AddCell(preco.Valor.ToString("C2"));
                    table.AddCell(preco.Local ?? "");
                    table.AddCell(preco.Observacao ?? "");
                }

                document.Add(table);
                document.Close();
            });
        }

        private async Task ExportToCsvAsync(List<PrecoModel> precos, string fileName)
        {
            await Task.Run(() => {
                using var writer = new StreamWriter(fileName);
                writer.WriteLine("Data,Valor,Local,Observacao");

                foreach (var preco in precos.OrderBy(p => p.Data))
                {
                    writer.WriteLine($"{preco.Data:dd/MM/yyyy},{preco.Valor:F2},{preco.Local},{preco.Observacao}");
                }
            });
        }
    }
}