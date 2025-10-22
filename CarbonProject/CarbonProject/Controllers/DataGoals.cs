using CarbonProject.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CarbonProject.Controllers
{
    public class DataGoalsController : Controller
    {
        // 數據展示頁面
        public IActionResult Index()
        {
            // 模擬年度碳排放資料
            var emissions = new List<AnnualEmission>
            {
                new AnnualEmission { Year = 2020, Emission = 3500 },
                new AnnualEmission { Year = 2021, Emission = 3000 },
                new AnnualEmission { Year = 2022, Emission = 2700 },
                new AnnualEmission { Year = 2023, Emission = 2500 },
                new AnnualEmission { Year = 2024, Emission = 2300 },
                new AnnualEmission { Year = 2025, Emission = 2100 }
            };

            // 模擬企業目標
            var goal = new CarbonGoal
            {
                CurrentEmission = 2300,
                TargetEmission = 2000
            };

            var viewModel = new DataGoalsViewModel
            {
                AnnualEmissions = emissions,
                Goal = goal
            };
            // 指定路徑 Views/Home/DataGoals.cshtml
            return View("~/Views/Home/DataGoals.cshtml", viewModel);
        }

        // 匯出 PDF 報告
        public IActionResult DownloadReport()
        {
            using (var stream = new MemoryStream())
            {
                var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36);
                PdfWriter.GetInstance(doc, stream);
                doc.Open();

                // ======== 字型設定 ========
                // 先註冊使用 CodePagesEncodingProvider 否則 iTextSharp 內部可能會呼叫到 CodePage 編碼。
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                // 使用系統內的「微軟正黑體」
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc,0"); // 微軟正黑體
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                // 主標題字型
                var titleFont = new Font(baseFont, 14, Font.BOLD, BaseColor.BLACK);
                // 內容字型
                var textFont = new Font(baseFont, 11, Font.NORMAL, BaseColor.BLACK);

                // 文件標題
                doc.Add(new Paragraph("企業碳排放年度報告", titleFont));
                doc.Add(new Paragraph(" ", textFont));
                doc.Add(new Paragraph("本報告概述企業年度碳排放與減碳目標達成情況。", textFont));
                doc.Add(new Paragraph(" ", textFont));

                // 表格
                var table = new PdfPTable(2) { WidthPercentage = 50 };
                table.SetWidths(new float[] { 1f, 1f }); // 欄寬比例

                // 標題列
                table.AddCell(new PdfPCell(new Phrase("年度", textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("碳排放量 (噸)", textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });

                // 範例資料（實務上需由資料庫取值）
                var years = new[] { "2020", "2021", "2022", "2023", "2024", "2025" };
                var emissions = new[] { "3500", "3000", "2700", "2500", "2300", "2100" };
                for (int i = 0; i < years.Length; i++)
                {
                    table.AddCell(new Phrase(years[i], textFont));
                    table.AddCell(new Phrase(emissions[i], textFont));
                }

                doc.Add(table);
                doc.Add(new Paragraph(" ", textFont));
                doc.Add(new Paragraph("目標碳排放量：2000 噸", textFont));
                doc.Add(new Paragraph("目前碳排放量：2100 噸", textFont));
                doc.Add(new Paragraph("進度達成率：約 95%", textFont));

                doc.Close();

                return File(stream.ToArray(), "application/pdf", "CarbonReport.pdf");
            }
        }
    }
}