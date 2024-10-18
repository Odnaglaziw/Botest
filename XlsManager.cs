using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Botest
{
    static class XlsManager
    {
        public static async Task GetDataFrom(string filePath, string worksheetName)
        {
            try
            {
                string[,] data = ReadDataFromXls(filePath, worksheetName, "A6", "F68");

                CreateTableImageFromData(data);
            }
            catch (Exception ex)
            {
                Program.Log(ex.Message);
            }
        }

        static string[,] ReadDataFromXls(string filePath, string sheetName, string startCell, string endCell)
        {
            int startRow = 5;
            int endRow = 67; 
            int startCol = 0;
            int endCol = 5; 

            string[,] data = new string[endRow - startRow + 1, endCol - startCol + 1];

            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                HSSFWorkbook workbook = new HSSFWorkbook(file);
                ISheet sheet = workbook.GetSheet(sheetName);

                for (int i = startRow; i <= endRow; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row != null)
                    {
                        for (int j = startCol; j <= endCol; j++)
                        {
                            ICell cell = row.GetCell(j);
                            data[i - startRow, j - startCol] = cell != null ? cell.ToString() : string.Empty;
                        }
                    }
                }
            }

            return data;
        }

        static void CreateTableImageFromData(string[,] data)
        {
            int cellWidth = 100;
            int cellHeight = 30;
            int tableWidth = cellWidth * data.GetLength(1);
            int tableHeight = cellHeight * data.GetLength(0);

            using (Bitmap bitmap = new Bitmap(tableWidth + 20, tableHeight + 20))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.White);
                System.Drawing.Font font = new System.Drawing.Font("Arial", 10);
                System.Drawing.Brush brush = Brushes.Black;
                Pen pen = new Pen(System.Drawing.Color.Black);

                for (int i = 0; i < data.GetLength(0); i++)
                {
                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        graphics.DrawRectangle(pen, j * cellWidth + 10, i * cellHeight + 10, cellWidth, cellHeight);

                        string text = data[i, j];
                        var textSize = graphics.MeasureString(text, font);
                        float textX = j * cellWidth + 10 + (cellWidth - textSize.Width) / 2;
                        float textY = i * cellHeight + 10 + (cellHeight - textSize.Height) / 2;
                        graphics.DrawString(text, font, brush, new PointF(textX, textY));
                    }
                }

                string outputPath = ".\\Downloads\\Pr-31.png";
                bitmap.Save(outputPath);
                Program.Log($"Изображение таблицы сохранено по адресу: {outputPath}");
            }
        }
    }
}
