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
using CarbonProject.Repositories; // <- 新增

namespace CarbonProject.Controllers
{
    public class ActionsController : Controller
    {
        private readonly ESGActionRepository _ESGRepo;

        // 建構式注入 Repository
        // From -> Repositories/ESGActionRepository.cs
        public ActionsController(ESGActionRepository ESGRepo)
        {
            _ESGRepo = ESGRepo;
        }

        // 列表 + 篩選
        public IActionResult Index(int year = -1, string category = "")
        {
            var all = _ESGRepo.GetAll();
            var years = _ESGRepo.GetYears();

            // 篩選預設：若未指定年份，選擇最新一年
            // 只有在沒有年份且沒有類別篩選時才預設最新年份
            // year = -1 表示尚未指定年份 → 取最新一年
            // year = 0 表示使用者選擇「全部年度」
            if (year == -1)
            {
                year = years.FirstOrDefault(); // 取最新年份
            }

            var filtered = all.Where(a => (year == 0 || a.Year == year)
                                        && (string.IsNullOrEmpty(category) || a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
                              .ToList();

            // 建立 ViewModel
            var vm = new ActionsViewModel
            {
                Actions = filtered,
                Categories = _ESGRepo.GetCategories(),
                SelectedYear = year,          // -1 不會傳到 View，0 表示全部年度
                SelectedCategory = category ?? ""
            };

            // **把年份也傳到 View**
            ViewBag.Years = years;
            
            return View(vm);
        }

        // Details
        public IActionResult Details(int id)
        {
            var item = _ESGRepo.GetById(id);
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
        public IActionResult Create([FromForm] ESGActionViewModel action)    // 加上 [FromForm] 明確指定來源
        {
            if (!ModelState.IsValid)
            {
                return View(action);
            }
            // 確保文字正確轉成 Unicode
            action.Title = action.Title?.Trim() ?? "";
            action.Category = action.Category?.Trim() ?? "";
            action.Description = action.Description?.Trim() ?? "";
            action.OwnerDepartment = action.OwnerDepartment?.Trim() ?? "";

            // 寫入 DB
            bool ok = _ESGRepo.Add(action);
            if (ok)
            {
                TempData["Alert"] = "行動方案新增成功";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["Alert"] = "資料寫入失敗，請確認資料格式或資料庫連線";
                return View(action);
            }
        }

        // GET: Edit
        public IActionResult Edit(int id)
        {
            var item = _ESGRepo.GetById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([FromForm] ESGActionViewModel action)  // 加上 [FromForm] 明確指定來源
        {
            if (!ModelState.IsValid)
            {
                return View(action);
            }
            var ok = _ESGRepo.Update(action);
            if (!ok) TempData["Alert"] = "更新失敗";
            else TempData["Alert"] = "更新成功";

            return RedirectToAction(nameof(Index));
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var ok = _ESGRepo.Delete(id);
            TempData["Alert"] = ok ? "刪除成功" : "刪除失敗";
            return RedirectToAction(nameof(Index));
        }

        // 下載 PDF 報告 (匯出當前篩選結果)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadReport(int year = 0, string category = "")
        {
            // 取得資料（和 Index 篩選一致）
            var all = _ESGRepo.GetAll();

            var filtered = all
                    .Where(a => (year == 0 || a.Year == year) &&
                                (string.IsNullOrEmpty(category) ||
                                 a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            // 檢查是否有資料
            if (!filtered.Any())
            {
                TempData["Alert"] = "目前篩選條件下沒有可匯出的資料。";
                return RedirectToAction("Index", new { year, category });
            }

            using (var stream = new MemoryStream())
            {
                // 建立文件
                var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36);
                PdfWriter.GetInstance(doc, stream);
                doc.Open();

                // ======== 字型設定 ========
                // 使用系統內的「微軟正黑體」或其他支援中文的字型
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc,0"); // 微軟正黑體
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                // 主字型（標題）
                Font titleFont = new Font(baseFont, 12, Font.BOLD, BaseColor.BLACK);

                // 內容字型（一般資料）
                Font textFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.BLACK);

                // 描述文字字型（灰色）
                Font descFont = new Font(baseFont, 9, Font.ITALIC, new BaseColor(100, 100, 100));

                // 文件標題
                doc.Add(new Paragraph($"企業行動方案報告 ({year})", titleFont));
                doc.Add(new Paragraph(" ", textFont));

                // 表格內容
                var table = new PdfPTable(5) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 8f, 18f, 25f, 12f, 10f });

                // 標題列背景色
                string[] headers = { "年度", "類別", "名稱 / 描述", "預期減碳 (噸/年)", "進度" };
                
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, textFont));
                    cell.BackgroundColor = iTextSharp.text.BaseColor.LIGHT_GRAY;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                // 內容列
                foreach (var a in filtered)
                {
                    table.AddCell(new Phrase(a.Year.ToString(), textFont));
                    table.AddCell(new Phrase(a.Category, textFont));

                    // Title + Description 不同字型
                    Phrase phrase = new Phrase();
                    phrase.Add(new Chunk(a.Title + "\n", textFont));    // 主標題
                    phrase.Add(new Chunk(a.Description ?? "", descFont)); // 描述（灰色）
                    table.AddCell(phrase);

                    table.AddCell(new Phrase(a.ExpectedReductionTon.ToString("N0"), textFont));
                    table.AddCell(new Phrase($"{a.ProgressPercent:N0}%", textFont));
                }

                doc.Add(table);
                doc.Close();

                // ======== 檔名設定 ========
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

        //          ==== 有問題區域尚在排解 ====
        //        // 記錄 Python 子程序（靜態變數，整個 App 共用）
        //        private static Process? _pythonProcess = null;
        //        private static DateTime _lastUsedTime = DateTime.MinValue;
        //        private static readonly object _lock = new object();
        //        // 驗算報告頁面
        //        public async Task<IActionResult> Progress()
        //        {
        //            string apiUrl = "http://127.0.0.1:8000/verify";  // Python API 的 URL
        //            string pythonPath = "python";                    // 可改成 "python3" 視系統而定
        //            string scriptName = "verify_service.py";
        //            string scriptDir = Path.Combine(Directory.GetCurrentDirectory(), "PythonAPI");
        //            string token = Environment.GetEnvironmentVariable("VERIFY_TOKEN");

        //            if (string.IsNullOrEmpty(token))
        //            {
        //                ViewBag.Error = "伺服器環境變數 VERIFY_TOKEN 未設定。";
        //                return View(new List<ESGProgress>());
        //            }

        //            // 1.檢查 Python API 是否啟動
        //            bool apiRunning = IsPortInUse(8000);

        //            if (!apiRunning)
        //            {
        //                lock (_lock)
        //                {
        //                    // 如果尚未啟動，或上次的 Process 已經關閉
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
        //                            _lastUsedTime = DateTime.UtcNow;
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            ViewBag.Error = $"啟動 Python 驗算服務失敗：{ex.Message}";
        //                            return View(new List<ESGProgress>());
        //                        }
        //                    }
        //                }
        //                // 給 Python 幾秒鐘啟動時間
        //                await Task.Delay(3000);
        //            }

        //            using var http = new HttpClient();
        //            // 後端負責安全 Token
        //            http.DefaultRequestHeaders.Add("X-Verify-Token", Environment.GetEnvironmentVariable("VERIFY_TOKEN"));
        //            try
        //            {                   
        //                // 2.呼叫 Python 驗算 API
        //                var response = await http.GetAsync(apiUrl);
        //                response.EnsureSuccessStatusCode();

        //                var json = await response.Content.ReadAsStringAsync();

        //                // 反序列化將 Json 轉成 C# 物件 ESGProgress 模型
        //                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        //                var result = JsonSerializer.Deserialize<List<ESGProgress>>(json, options);

        //                // 更新最後使用時間
        //                _lastUsedTime = DateTime.UtcNow;

        //                // 啟動背景任務，監控是否閒置太久自動關閉
        //                _ = Task.Run(() => AutoShutdownPython());

        //                return View(result);
        //            }
        //            catch (Exception ex)
        //            {
        //                // 若發生錯誤，顯示空頁並提示訊息
        //                ViewBag.Error = $"無法連線至驗算服務：{ex.Message}";
        //                return View(new List<CarbonProject.Models.ESGProgress>());
        //            }
        //        }
        //        // 自動關閉 Python 子程序（若 10 分鐘未使用）
        //        private void AutoShutdownPython()
        //        {
        //            lock (_lock)
        //            {
        //                if (_pythonProcess != null && !_pythonProcess.HasExited)
        //                {
        //                    TimeSpan idle = DateTime.UtcNow - _lastUsedTime;
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
        //        // 檢查指定 port 是否已被占用 (Python API 是否在執行)
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
        //                inUse = true; // 無法監聽 → 已被佔用
        //            }
        //            finally
        //            {
        //                listener?.Stop();
        //            }
        //            return inUse;
        //        }
    }
}
