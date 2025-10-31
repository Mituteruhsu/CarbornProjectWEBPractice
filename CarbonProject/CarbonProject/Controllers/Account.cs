using CarbonProject.Models;
using CarbonProject.Models.EFModels;
using CarbonProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Text.Json;   // 用來轉 JSON 格式
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

// From -> Service/ActivityLogService.cs
namespace CarbonProject.Controllers
{
    public class Account : Controller
    {
        private IWebHostEnvironment Environment;
        private readonly ILogger<Account> _logger;
        private readonly IConfiguration _config;
        private readonly ActivityLogService _activityLog;
        public Account(IWebHostEnvironment _environment, ILogger<Account> logger, IConfiguration config, ActivityLogService activityLog)
        {
            Environment = _environment;
            _logger = logger;
            _config = config;
            _activityLog = activityLog;

            Members.Init(config);       // 初始化 Members DB 連線字串
            Company.Init(config);       // 初始化 Company DB 連線字串
            Industry.Init(config);      // 初始化 Industry DB 連線字串
        }
        //===============登入===============
        // Include ActivityLogService
        // From -> Service/ActivityLogService.cs
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string UID, string password)
        {
            // 正規驗證(UID:至少4位數，password:至少8位數，兩者可包含英文大小寫及數字 0~9)
            var uidRegex = new Regex(@"^[a-zA-Z0-9]{4,}$");
            var pwdRegex = new Regex(@"^[a-zA-Z0-9]{8,}$");

            if (!uidRegex.IsMatch(UID))
            {
                ViewBag.Error = "帳號至少4位，且僅能包含英文與數字";
                return View("Login");
            }
            if (!pwdRegex.IsMatch(password))
            {
                ViewBag.Error = "密碼至少8位，且僅能包含英文與數字";
                return View("Login");
            }

            // 驗證帳號密碼
            var member = Members.CheckLogin(UID, password);
            if (member != null)
            {
                // 如果帳號已被鎖定
                if (!member.IsActive)
                {
                    ViewBag.Error = "帳號已暫時鎖定，請稍後再試。";
                    return View("Login");
                }
                // 登入成功：更新最後登入時間、重置錯誤次數
                Members.UpdateLastLoginAt(member.MemberId);

                // 寫入登入成功 EF Core 紀錄
                // From -> Service/ActivityLogService.cs
                await _activityLog.LogAsync(
                    memberId: member.MemberId,
                    companyId: member.CompanyId,
                    actionType: "Auth.Login.Success",
                    actionCategory: "Auth",
                    outcome: "Success",
                    ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString(),
                    createdBy: member.Username,
                    detailsObj: new { username = member.Username }
                );

                // 設定 Session
                HttpContext.Session.SetString("isLogin", "true");
                HttpContext.Session.SetString("Role", member.Role);
                HttpContext.Session.SetString("Username", member.Username);
                HttpContext.Session.SetInt32("MemberId", member.MemberId);
                if (member.CompanyId > 0)
                    HttpContext.Session.SetInt32("CompanyId", member.CompanyId);

                // 根據 Role 導向不同頁面
                if (member.Role == "Admin")
                    return RedirectToAction("Admin_read", "Account"); // 管理頁
                else
                    return RedirectToAction("Index", "Home"); // 一般會員測試頁
            }
            else
            {
                // 登入失敗：增加錯誤次數
                Members.IncrementFailedLogin(UID);

                // 寫入登入失敗 EF Core 紀錄
                // From -> Service/ActivityLogService.cs
                await _activityLog.LogAsync(
                    memberId: null,
                    companyId: null,
                    actionType: "Auth.Login.Failed",
                    actionCategory: "Auth",
                    outcome: "Failure",
                    ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString(),
                    createdBy: "System",
                    detailsObj: new { username = UID }
                );

                ViewBag.Error = "帳號或密碼錯誤";
                return View("Login");
            }
        }
        //===============登出===============
        //Include ActivityLogService
        public async Task<IActionResult> Logout()
        {
            // 取得目前登入的會員
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                int memberId = Members.GetMemberIdByUsername(username);
                if (memberId > 0)
                {
                    Members.UpdateLastLogoutAt(memberId);

                    // 登出 EF Core 紀錄
                    // From -> Service/ActivityLogService.cs
                    await _activityLog.LogAsync(
                        memberId: memberId,
                        companyId: null,
                        actionType: "Auth.Logout",
                        actionCategory: "Auth",
                        outcome: "Success",
                        ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                        userAgent: Request.Headers["User-Agent"].ToString(),
                        createdBy: username,
                        detailsObj: new { username }
                    );
                }
            }

            // 清除 Session
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        //===============註冊===============
        //Include ActivityLogService
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string username, string email, string password, string passwordConfirm,
            string fullname, string userType,
            string CompanyName, string TaxId, string Address, string Contact_Email,
            string IndustryId)
        {
            // 1. 密碼確認
            if (password != passwordConfirm)
            {
                ViewBag.Error = "密碼與確認密碼不一致";
                return View();
            }

            // 2. 正則驗證
            var usernameRegex = new Regex(@"^[a-zA-Z0-9]{4,}$"); // 帳號至少4位
            var pwdRegex = new Regex(@"^[a-zA-Z0-9]{8,}$");      // 密碼至少8位
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$"); // Email格式
            if (!usernameRegex.IsMatch(username))
            {
                ViewBag.Error = "帳號至少4位，且僅能包含英文與數字";
                return View();
            }

            if (!pwdRegex.IsMatch(password))
            {
                ViewBag.Error = "密碼至少8位，且僅能包含英文與數字";
                return View();
            }

            if (!emailRegex.IsMatch(email))
            {
                ViewBag.Error = "Email 格式不正確";
                return View();
            }

            // 3. 建立會員物件
            var member = new Members
            {
                Username = username,
                Email = email,
                PasswordHash = password,
                FullName = fullname,
                Role = (userType == "Company") ? "Company" : "Viewer",
                IsActive = true
            };

            // 4. 寫入資料表
            int newMemberId = Members.AddMember(member);
            if (newMemberId == -1)
            {
                ViewBag.Error = "帳號或Email已存在";
                return View();
            }
            if (newMemberId <= 0)
            {
                ViewBag.Error = "會員註冊失敗，請稍後再試。";
                return View();
            }

            int? companyId = null; // 只有公司用戶才會有 CompanyId

            // 5. 若為企業用戶，建立或綁定公司資料
            if (userType == "Company")
            {
                if (string.IsNullOrEmpty(TaxId))
                {
                    ViewBag.Error = "請輸入統一編號（Tax ID）";
                    return View();
                }

                // 檢查是否已有此公司
                var existingCompany = Company.GetCompanyByTaxId(TaxId.Trim());
                if (existingCompany != null)
                {
                    // 已存在公司，直接綁定
                    companyId = existingCompany.CompanyId;
                }
                else
                {
                    // 建立新公司
                    if (string.IsNullOrEmpty(CompanyName) ||
                        string.IsNullOrEmpty(Address) ||
                        string.IsNullOrEmpty(Contact_Email) ||
                        string.IsNullOrEmpty(IndustryId))
                    {
                        ViewBag.Error = "請填寫完整企業資料";
                        return View();
                    }

                    var newCompany = new Company
                    {
                        CompanyName = CompanyName.Trim(),
                        TaxId = TaxId.Trim(),
                        Industry = IndustryId.Trim(),
                        Address = Address.Trim(),
                        Contact_Email = Contact_Email.Trim(),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    companyId = Company.AddCompany(newCompany);
                    if (companyId <= 0)
                    {
                        ViewBag.Error = "企業資料新增失敗，請稍後再試。";
                        return View();
                    }
                }
                // 更新會員綁定公司
                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    string updateSql = @"UPDATE Users SET CompanyId=@CompanyId WHERE MemberId=@MemberId";
                    using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompanyId", companyId);
                        cmd.Parameters.AddWithValue("@MemberId", newMemberId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            // 註冊成功寫入 ActivityLog
            await _activityLog.LogAsync(
                memberId: newMemberId,
                companyId: companyId,
                actionType: "Auth.Register",
                actionCategory: "Auth",
                outcome: "Success",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: username,
                detailsObj: new { username, email, role = member.Role, companyId }
            );
            // 6. 註冊完成
            TempData["RegisterSuccess"] = "註冊成功，請登入！";
                return RedirectToAction("Login");
        }

        // 給前端 AJAX 用的查詢 API - register.cshtml/
        [HttpGet]
        public IActionResult GetIndustries()
        {
            var grouped = Industry.GetGrouped();
            return Json(grouped);
        }
        [HttpGet]
        public IActionResult GetCompanyByTaxId(string taxId)
        {
            if (string.IsNullOrEmpty(taxId))
                return Json(new { exists = false });

            var company = Company.GetCompanyByTaxId(taxId);
            if (company == null)
                return Json(new { exists = false });

            return Json(new
            {
                exists = true,
                company = new
                {
                    company.CompanyId,
                    company.CompanyName,
                    company.TaxId,
                    company.Address,
                    company.Contact_Email,
                    company.Industry
                }
            });
        }
        //===============Admin 會員管理===============
        //Include ActivityLogService
        public IActionResult Admin_read()
        {
            // 檢查是否為 Admin 登入
            if (HttpContext.Session.GetString("isLogin") != "true" ||
                HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["LoginAlert"] = "您沒有管理權限";
                return RedirectToAction("Login");
            }

            var members = Members.GetAllMembers();
            return View(members);
        }

        // 刪除會員
        //Include ActivityLogService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id)
        {
            Debug.WriteLine($"==========here is ID==========");
            Debug.WriteLine(id);
            Debug.WriteLine($"==========here is ID==========");
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            bool success = Members.DeleteMember(id);
            // 刪除會員寫入 ActivityLog
            await _activityLog.LogAsync(
                memberId: id,
                companyId: null,
                actionType: "Admin.DeleteMember",
                actionCategory: "Admin",
                outcome: success ? "Success" : "Failure",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: HttpContext.Session.GetString("Username"),
                detailsObj: new { memberId = id }
            );
            if (!success)
            {
                TempData["AdminError"] = "刪除失敗";
            }
            return RedirectToAction("Admin_read");
        }

        // 編輯會員
        //Include ActivityLogService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMember(int id, string username, string email, string fullname)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            bool success = Members.UpdateMember(id, username, email, fullname);
            // 編輯會員寫入 ActivityLog
            await _activityLog.LogAsync(
                memberId: id,
                companyId: null,
                actionType: "Admin.EditMember",
                actionCategory: "Admin",
                outcome: success ? "Success" : "Failure",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: HttpContext.Session.GetString("Username"),
                detailsObj: new { username, email, fullname }
            );

            if (!success)
            {
                TempData["AdminError"] = "更新失敗";
            }
            return RedirectToAction("Admin_read");
        }
    }
}
