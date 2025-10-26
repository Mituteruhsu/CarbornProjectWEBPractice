using CarbonProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CarbonProject.Controllers
{
    public class Account : Controller
    {
        private IWebHostEnvironment Environment;
        private readonly ILogger<Account> _logger;
        private readonly IConfiguration _config;
        public Account(IWebHostEnvironment _environment, ILogger<Account> logger, IConfiguration config)
        {
            Environment = _environment;
            _logger = logger;
            _config = config;

            Members.Init(config);   // 初始化 Members DB 連線字串
            Company.Init(config);   // 初始化 Company DB 連線字串
            Industry.Init(config);  // 初始化 Industry DB 連線字串
        }
        //===============登入===============
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string UID, string password)
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
                // 設定 Session
                HttpContext.Session.SetString("isLogin", "true");
                HttpContext.Session.SetString("Role", member.Role);
                HttpContext.Session.SetString("Username", member.Username);

                // 根據 Role 導向不同頁面
                if (member.Role == "Admin")
                    return RedirectToAction("Admin_read", "Account"); // 管理頁
                else
                    return RedirectToAction("Index", "Home"); // 一般會員測試頁
            }
            else
            {
                ViewBag.Error = "帳號或密碼錯誤";
                return View("Login");
            }
        }
        //===============登出===============
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("isLogin");
            return RedirectToAction("Login");
        }
        //===============註冊===============
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(
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
            
            // 5. 若為企業用戶，建立公司資料 + 關聯
            if (userType == "Company")
            {
                if (string.IsNullOrEmpty(CompanyName) ||
                    string.IsNullOrEmpty(TaxId) ||
                    string.IsNullOrEmpty(IndustryId) ||
                    string.IsNullOrEmpty(Address) ||
                    string.IsNullOrEmpty(Contact_Email))
                {
                    ViewBag.Error = "請填寫完整企業資料";
                    return View();
                }

                // 建立公司物件
                var company = new Company
                {
                    MemberId = newMemberId,
                    CompanyName = CompanyName.Trim(),
                    TaxId = TaxId.Trim(),
                    Industry = IndustryId.Trim(),
                    Address = Address.Trim(),
                    Contact_Email = Contact_Email.Trim(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                Debug.WriteLine(company);
                // 寫入 Companies 資料庫
                int newCompanyId = Company.AddCompany(company);
                if (newCompanyId == -1)
                {
                    ViewBag.Error = "企業名，統編或聯絡Email已存在";
                    return View();
                }
                if (newCompanyId <= 0)
                {
                    ViewBag.Error = "企業資料新增失敗，請稍後再試。";
                    return View();
                }
            }
            // 6. 註冊完成
            TempData["RegisterSuccess"] = "註冊成功，請登入！";
                return RedirectToAction("Login");
        }

        // 給前端 AJAX 用的產業查詢 API
        [HttpGet]
        public IActionResult GetIndustries()
        {
            var grouped = Industry.GetGrouped();
            return Json(grouped);
        }
        //===============Admin 會員管理===============
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMember(int id)
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
            if (!success)
            {
                TempData["AdminError"] = "刪除失敗";
            }
            return RedirectToAction("Admin_read");
        }

        // 編輯會員
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMember(int id, string username, string email, string fullname)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            bool success = Members.UpdateMember(id, username, email, fullname);
            if (!success)
            {
                TempData["AdminError"] = "更新失敗";
            }
            return RedirectToAction("Admin_read");
        }
    }
}
