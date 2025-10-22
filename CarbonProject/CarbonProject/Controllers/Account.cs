using CarbonProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;

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

            Members.Init(config); // 初始化 DB 連線字串
        }
        //===============登入===============
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
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
                    return RedirectToAction("testLogin", "Account"); // 一般會員測試頁
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
        public IActionResult Register(string username, string email, string password, string passwordConfirm, string fullname)
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
                FullName = fullname
            };
            // 4. 寫入資料表
            bool success = Members.AddMember(member);
            if (!success)
            {
                ViewBag.Error = "帳號或Email已存在";
                return View();
            }

            TempData["RegisterSuccess"] = "註冊成功，請登入！";
            return RedirectToAction("Login");
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
        public IActionResult DeleteMember(int id)
        {
            // 僅限 Admin
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

        // 編輯會員 (示範)
        [HttpPost]
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

        //==================== 一般會員測試頁 ====================
        public IActionResult testLogin(int row, int col)
        {
            if (HttpContext.Session.GetString("isLogin") != "true")
            {
                TempData["LoginAlert"] = "請登入後使用";
                return RedirectToAction("Login"); // 回登入頁
            }
            var result = new List<string>();
            for (int r = 1; r <= row; r++)
            {
                for (int c = 1; c <= col; c++)
                {
                    result.Add(r + "*" + c + "=" + r * c);
                }
            }
            ViewData["row"] = row;
            ViewData["col"] = col;
            ViewData["result"] = result;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
