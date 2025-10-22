using CarbonProject.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections.Generic;

namespace CarbonProject.Controllers
{
    public class Actions : Controller
    {
        // �C�� + �z��
        public IActionResult Index(int year = -1, string category = "")
        {
            var all = ActionsRepository.GetAll();
            var years = ActionsRepository.GetYears();

            // �z��w�]�G�Y�����w�~���A��̷ܳs�@�~
            // �u���b�S���~���B�S�����O�z��ɤ~�w�]�̷s�~��
            // year = -1 ��ܩ|�����w�~�� �� ���̷s�@�~
            // year = 0 ��ܨϥΪ̿�ܡu�����~�סv
            if (year == -1)
            {
                year = years.FirstOrDefault(); // ���̷s�~��
            }

            var filtered = all.Where(a => (year == 0 || a.Year == year)
                                        && (string.IsNullOrEmpty(category) || a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
                              .ToList();

            // �إ� ViewModel
            var vm = new ActionsViewModel
            {
                Actions = filtered,
                Categories = ActionsRepository.GetCategories(),
                SelectedYear = year,          // -1 ���|�Ǩ� View�A0 ��ܥ����~��
                SelectedCategory = category ?? ""
            };

            return View(vm);
        }

        // Details
        public IActionResult Details(int id)
        {
            var item = ActionsRepository.GetById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([FromForm] ESGAction action)    // �[�W [FromForm] ���T���w�ӷ�
        {
            if (!ModelState.IsValid)
            {
                return View(action);
            }
            // �T�O��r���T�ন Unicode
            action.Title = action.Title?.Trim() ?? "";
            action.Category = action.Category?.Trim() ?? "";
            action.Description = action.Description?.Trim() ?? "";
            action.OwnerDepartment = action.OwnerDepartment?.Trim() ?? "";

            // �g�J DB
            bool ok = ActionsRepository.Add(action);
            if (ok)
            {
                TempData["Alert"] = "��ʤ�׷s�W���\";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["Alert"] = "��Ƽg�J���ѡA�нT�{��Ʈ榡�θ�Ʈw�s�u";
                return View(action);
            }
        }

        // GET: Edit
        public IActionResult Edit(int id)
        {
            var item = ActionsRepository.GetById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromForm] ESGAction action)  // �[�W [FromForm] ���T���w�ӷ�
        {
            if (!ModelState.IsValid)
            {
                return View(action);
            }
            var ok = ActionsRepository.Update(action);
            if (!ok) TempData["Alert"] = "��s����";
            else TempData["Alert"] = "��s���\";

            return RedirectToAction(nameof(Index));
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var ok = ActionsRepository.Delete(id);
            TempData["Alert"] = ok ? "�R�����\" : "�R������";
            return RedirectToAction(nameof(Index));
        }

        // �U�� PDF ���i (�ץX��e�z�ﵲ�G)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadReport(int year = 0, string category = "")
        {
            // ���o��ơ]�M Index �z��@�P�^
            var all = ActionsRepository.GetAll();

            var filtered = all
                    .Where(a => (year == 0 || a.Year == year) &&
                                (string.IsNullOrEmpty(category) ||
                                 a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            // �ˬd�O�_�����
            if (!filtered.Any())
            {
                TempData["Alert"] = "�ثe�z�����U�S���i�ץX����ơC";
                return RedirectToAction("Index", new { year, category });
            }

            using (var stream = new MemoryStream())
            {
                // �إߤ��
                var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36);
                PdfWriter.GetInstance(doc, stream);
                doc.Open();

                // ======== �r���]�w ========
                // �ϥΨt�Τ����u�L�n������v�Ψ�L�䴩���媺�r��
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc,0"); // �L�n������
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                // �D�r���]���D�^
                Font titleFont = new Font(baseFont, 12, Font.BOLD, BaseColor.BLACK);

                // ���e�r���]�@���ơ^
                Font textFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.BLACK);

                // �y�z��r�r���]�Ǧ�^
                Font descFont = new Font(baseFont, 9, Font.ITALIC, new BaseColor(100, 100, 100));

                // �����D
                doc.Add(new Paragraph($"���~��ʤ�׳��i ({year})", titleFont));
                doc.Add(new Paragraph(" ", textFont));

                // ��椺�e
                var table = new PdfPTable(5) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 8f, 18f, 25f, 12f, 10f });

                // ���D�C�I����
                string[] headers = { "�~��", "���O", "�W�� / �y�z", "�w����� (��/�~)", "�i��" };
                
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, textFont));
                    cell.BackgroundColor = iTextSharp.text.BaseColor.LIGHT_GRAY;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                // ���e�C
                foreach (var a in filtered)
                {
                    table.AddCell(new Phrase(a.Year.ToString(), textFont));
                    table.AddCell(new Phrase(a.Category, textFont));

                    // Title + Description ���P�r��
                    Phrase phrase = new Phrase();
                    phrase.Add(new Chunk(a.Title + "\n", textFont));    // �D���D
                    phrase.Add(new Chunk(a.Description ?? "", descFont)); // �y�z�]�Ǧ�^
                    table.AddCell(phrase);

                    table.AddCell(new Phrase(a.ExpectedReductionTon.ToString("N0"), textFont));
                    table.AddCell(new Phrase($"{a.ProgressPercent:N0}%", textFont));
                }

                doc.Add(table);
                doc.Close();

                // ======== �ɦW�]�w ========
                string fileName = year == 0
                    ? "ActionsReport_All.pdf"
                    : $"ActionsReport_{year}.pdf";

                return File(stream.ToArray(), "application/pdf", fileName);
            }
        }
        public IActionResult Progress()
        {
            return View();
        }

        //          ==== �����D�ϰ�|�b�Ƹ� ====
        //        // �O�� Python �l�{�ǡ]�R�A�ܼơA��� App �@�Ρ^
        //        private static Process? _pythonProcess = null;
        //        private static DateTime _lastUsedTime = DateTime.MinValue;
        //        private static readonly object _lock = new object();
        //        // �����i����
        //        public async Task<IActionResult> Progress()
        //        {
        //            string apiUrl = "http://127.0.0.1:8000/verify";  // Python API �� URL
        //            string pythonPath = "python";                    // �i�令 "python3" ���t�Φөw
        //            string scriptName = "verify_service.py";
        //            string scriptDir = Path.Combine(Directory.GetCurrentDirectory(), "PythonAPI");
        //            string token = Environment.GetEnvironmentVariable("VERIFY_TOKEN");

        //            if (string.IsNullOrEmpty(token))
        //            {
        //                ViewBag.Error = "���A�������ܼ� VERIFY_TOKEN ���]�w�C";
        //                return View(new List<ESGProgress>());
        //            }

        //            // 1.�ˬd Python API �O�_�Ұ�
        //            bool apiRunning = IsPortInUse(8000);

        //            if (!apiRunning)
        //            {
        //                lock (_lock)
        //                {
        //                    // �p�G�|���ҰʡA�ΤW���� Process �w�g����
        //                    if (_pythonProcess == null || _pythonProcess.HasExited)
        //                    {
        //                        try
        //                        {
        //                            var psi = new ProcessStartInfo
        //                            {
        //                                FileName = pythonPath,
        //                                Arguments = scriptName,
        //                                WorkingDirectory = scriptDir,
        //                                UseShellExecute = false,
        //                                CreateNoWindow = true
        //                            };
        //                            _pythonProcess = Process.Start(psi);
        //                            _lastUsedTime = DateTime.Now;
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            ViewBag.Error = $"�Ұ� Python ���A�ȥ��ѡG{ex.Message}";
        //                            return View(new List<ESGProgress>());
        //                        }
        //                    }
        //                }
        //                // �� Python �X�����Ұʮɶ�
        //                await Task.Delay(3000);
        //            }

        //            using var http = new HttpClient();
        //            // ��ݭt�d�w�� Token
        //            http.DefaultRequestHeaders.Add("X-Verify-Token", Environment.GetEnvironmentVariable("VERIFY_TOKEN"));
        //            try
        //            {                   
        //                // 2.�I�s Python ��� API
        //                var response = await http.GetAsync(apiUrl);
        //                response.EnsureSuccessStatusCode();

        //                var json = await response.Content.ReadAsStringAsync();

        //                // �ϧǦC�ƱN Json �ন C# ���� ESGProgress �ҫ�
        //                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        //                var result = JsonSerializer.Deserialize<List<ESGProgress>>(json, options);

        //                // ��s�̫�ϥήɶ�
        //                _lastUsedTime = DateTime.Now;

        //                // �ҰʭI�����ȡA�ʱ��O�_���m�Ӥ[�۰�����
        //                _ = Task.Run(() => AutoShutdownPython());

        //                return View(result);
        //            }
        //            catch (Exception ex)
        //            {
        //                // �Y�o�Ϳ��~�A��ܪŭ��ô��ܰT��
        //                ViewBag.Error = $"�L�k�s�u�����A�ȡG{ex.Message}";
        //                return View(new List<CarbonProject.Models.ESGProgress>());
        //            }
        //        }
        //        // �۰����� Python �l�{�ǡ]�Y 10 �������ϥΡ^
        //        private void AutoShutdownPython()
        //        {
        //            lock (_lock)
        //            {
        //                if (_pythonProcess != null && !_pythonProcess.HasExited)
        //                {
        //                    TimeSpan idle = DateTime.Now - _lastUsedTime;
        //                    if (idle.TotalMinutes > 10)
        //                    {
        //                        try
        //                        {
        //                            _pythonProcess.Kill();
        //                            _pythonProcess.Dispose();
        //                            _pythonProcess = null;
        //                        }
        //                        catch { }
        //                    }
        //                }
        //            }
        //        }
        //        // �ˬd���w port �O�_�w�Q�e�� (Python API �O�_�b����)
        //        private bool IsPortInUse(int port)
        //        {
        //            bool inUse = false;
        //            TcpListener? listener = null;
        //            try
        //            {
        //                listener = new TcpListener(System.Net.IPAddress.Loopback, port);
        //                listener.Start();
        //            }
        //            catch (SocketException)
        //            {
        //                inUse = true; // �L�k��ť �� �w�Q����
        //            }
        //            finally
        //            {
        //                listener?.Stop();
        //            }
        //            return inUse;
        //        }
    }
}
