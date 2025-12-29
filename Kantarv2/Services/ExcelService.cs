
using ClosedXML.Excel;
using System.Reflection;

namespace Kantarv2.Services
{
    public class ExcelService : IExcelServiceInterface
    {
        public byte[] GenerateExcel<T>(IEnumerable<T> data, string sheetName)
        {
            using (var workbook = new XLWorkbook())
            {
                var Worksheet = workbook.Worksheets.Add(sheetName);
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < properties.Length; i++)
                {
                    Worksheet.Cell(1, i + 1).Value = properties[i].Name;
                    Worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                var dataList = data.ToList();
                for (int row = 0; row < dataList.Count; row++)
                {
                    for (int col = 0; col < properties.Length; col++)
                    {
                        var value = properties[col].GetValue(dataList[row]);
                        Worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
                    }
                }
                Worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}