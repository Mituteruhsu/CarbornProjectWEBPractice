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
        // �ƾڮi�ܭ���
        public IActionResult Index()
        {
            // �����~�׺ұƩ���
            var emissions = new List<AnnualEmission>
            {
                new AnnualEmission { Year = 2020, Emission = 3500 },
                new AnnualEmission { Year = 2021, Emission = 3000 },
                new AnnualEmission { Year = 2022, Emission = 2700 },
                new AnnualEmission { Year = 2023, Emission = 2500 },
                new AnnualEmission { Year = 2024, Emission = 2300 },
                new AnnualEmission { Year = 2025, Emission = 2100 }
            };

            // �������~�ؼ�
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
            // ���w���| Views/Home/DataGoals.cshtml
            return View("~/Views/Home/DataGoals.cshtml", viewModel);
        }

        // �ץX PDF ���i
        public IActionResult DownloadReport()
        {
            using (var stream = new MemoryStream())
            {
                var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36);
                PdfWriter.GetInstance(doc, stream);
                doc.Open();

                // ======== �r���]�w ========
                // �����U�ϥ� CodePagesEncodingProvider �_�h iTextSharp �����i��|�I�s�� CodePage �s�X�C
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                // �ϥΨt�Τ����u�L�n������v
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc,0"); // �L�n������
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                // �D���D�r��
                var titleFont = new Font(baseFont, 14, Font.BOLD, BaseColor.BLACK);
                // ���e�r��
                var textFont = new Font(baseFont, 11, Font.NORMAL, BaseColor.BLACK);

                // �����D
                doc.Add(new Paragraph("���~�ұƩ�~�׳��i", titleFont));
                doc.Add(new Paragraph(" ", textFont));
                doc.Add(new Paragraph("�����i���z���~�~�׺ұƩ�P��ҥؼйF�����p�C", textFont));
                doc.Add(new Paragraph(" ", textFont));

                // ���
                var table = new PdfPTable(2) { WidthPercentage = 50 };
                table.SetWidths(new float[] { 1f, 1f }); // ��e���

                // ���D�C
                table.AddCell(new PdfPCell(new Phrase("�~��", textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("�ұƩ�q (��)", textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER });

                // �d�Ҹ�ơ]��ȤW�ݥѸ�Ʈw���ȡ^
                var years = new[] { "2020", "2021", "2022", "2023", "2024", "2025" };
                var emissions = new[] { "3500", "3000", "2700", "2500", "2300", "2100" };
                for (int i = 0; i < years.Length; i++)
                {
                    table.AddCell(new Phrase(years[i], textFont));
                    table.AddCell(new Phrase(emissions[i], textFont));
                }

                doc.Add(table);
                doc.Add(new Paragraph(" ", textFont));
                doc.Add(new Paragraph("�ؼкұƩ�q�G2000 ��", textFont));
                doc.Add(new Paragraph("�ثe�ұƩ�q�G2100 ��", textFont));
                doc.Add(new Paragraph("�i�׹F���v�G�� 95%", textFont));

                doc.Close();

                return File(stream.ToArray(), "application/pdf", "CarbonReport.pdf");
            }
        }
    }
}