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

            Members.Init(config); // ��l�� DB �s�u�r��
        }
        //===============�n�J===============
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string UID, string password)
        {
            // ���W����(UID:�ܤ�4��ơApassword:�ܤ�8��ơA��̥i�]�t�^��j�p�g�μƦr 0~9)
            var uidRegex = new Regex(@"^[a-zA-Z0-9]{4,}$");
            var pwdRegex = new Regex(@"^[a-zA-Z0-9]{8,}$");

            if (!uidRegex.IsMatch(UID))
            {
                ViewBag.Error = "�b���ܤ�4��A�B�ȯ�]�t�^��P�Ʀr";
                return View("Login");
            }
            if (!pwdRegex.IsMatch(password))
            {
                ViewBag.Error = "�K�X�ܤ�8��A�B�ȯ�]�t�^��P�Ʀr";
                return View("Login");
            }

            // ���ұb���K�X
            var member = Members.CheckLogin(UID, password);
            if (member != null)
            {
                // �]�w Session
                HttpContext.Session.SetString("isLogin", "true");
                HttpContext.Session.SetString("Role", member.Role);
                HttpContext.Session.SetString("Username", member.Username);
                
                // �ھ� Role �ɦV���P����
                if (member.Role == "Admin")
                    return RedirectToAction("Admin_read", "Account"); // �޲z��
                else
                    return RedirectToAction("testLogin", "Account"); // �@��|�����խ�
            }
            else
            {
                ViewBag.Error = "�b���αK�X���~";
                return View("Login");
            }
        }
        //===============�n�X===============
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("isLogin");
            return RedirectToAction("Login");
        }
        //===============���U===============
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(string username, string email, string password, string passwordConfirm, string fullname)
        {
            // 1. �K�X�T�{
            if (password != passwordConfirm)
            {
                ViewBag.Error = "�K�X�P�T�{�K�X���@�P";
                return View();
            }
            // 2. ���h����
            var usernameRegex = new Regex(@"^[a-zA-Z0-9]{4,}$"); // �b���ܤ�4��
            var pwdRegex = new Regex(@"^[a-zA-Z0-9]{8,}$");      // �K�X�ܤ�8��
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$"); // Email�榡
            if (!usernameRegex.IsMatch(username))
            {
                ViewBag.Error = "�b���ܤ�4��A�B�ȯ�]�t�^��P�Ʀr";
                return View();
            }

            if (!pwdRegex.IsMatch(password))
            {
                ViewBag.Error = "�K�X�ܤ�8��A�B�ȯ�]�t�^��P�Ʀr";
                return View();
            }

            if (!emailRegex.IsMatch(email))
            {
                ViewBag.Error = "Email �榡�����T";
                return View();
            }
            // 3. �إ߷|������
            var member = new Members
            {
                Username = username,
                Email = email,
                PasswordHash = password,
                FullName = fullname
            };
            // 4. �g�J��ƪ�
            bool success = Members.AddMember(member);
            if (!success)
            {
                ViewBag.Error = "�b����Email�w�s�b";
                return View();
            }

            TempData["RegisterSuccess"] = "���U���\�A�еn�J�I";
            return RedirectToAction("Login");
        }
        //===============Admin �|���޲z===============
        public IActionResult Admin_read()
        {
            // �ˬd�O�_�� Admin �n�J
            if (HttpContext.Session.GetString("isLogin") != "true" ||
                HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["LoginAlert"] = "�z�S���޲z�v��";
                return RedirectToAction("Login");
            }

            var members = Members.GetAllMembers();
            return View(members);
        }

        // �R���|��
        [HttpPost]
        public IActionResult DeleteMember(int id)
        {
            // �ȭ� Admin
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["AdminError"] = "�L�v���ާ@";
                return RedirectToAction("Login");
            }

            bool success = Members.DeleteMember(id);
            if (!success)
            {
                TempData["AdminError"] = "�R������";
            }
            return RedirectToAction("Admin_read");
        }

        // �s��|�� (�ܽd)
        [HttpPost]
        public IActionResult EditMember(int id, string username, string email, string fullname)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                TempData["AdminError"] = "�L�v���ާ@";
                return RedirectToAction("Login");
            }

            bool success = Members.UpdateMember(id, username, email, fullname);
            if (!success)
            {
                TempData["AdminError"] = "��s����";
            }
            return RedirectToAction("Admin_read");
        }

        //==================== �@��|�����խ� ====================
        public IActionResult testLogin(int row, int col)
        {
            if (HttpContext.Session.GetString("isLogin") != "true")
            {
                TempData["LoginAlert"] = "�еn�J��ϥ�";
                return RedirectToAction("Login"); // �^�n�J��
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
