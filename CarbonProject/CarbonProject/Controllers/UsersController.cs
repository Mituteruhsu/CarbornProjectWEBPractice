using CarbonProject.Attributes;
using CarbonProject.Models;
using CarbonProject.Models.EFModels;
using CarbonProject.Repositories;
using CarbonProject.Service.Logging;
using CarbonProject.Services;
using Microsoft.AspNetCore.Authorization;
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
    public class UsersController : Controller
    {
        private IWebHostEnvironment Environment;
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _config;
        private readonly MembersRepository _membersRepository;
        private readonly ActivityLogService _activityLog;
        private readonly CompanyRepository _companyRepository;
        private readonly IndustryRepository _industryRepository;

        private readonly JWTService _jwtService;
        public UsersController(
            IWebHostEnvironment _environment, 
            ILogger<UsersController> logger, 
            IConfiguration config, 
            MembersRepository membersRepository, 
            ActivityLogService activityLog, 
            CompanyRepository companyRepository, 
            IndustryRepository industryRepository,
            JWTService jwtService
            )
        {
            // 非 static 用法，需要在Program.cs 註冊，建議使用較不會
            Environment = _environment;
            _logger = logger;
            _config = config;
            _activityLog = activityLog;
            _companyRepository = companyRepository;
            _industryRepository = industryRepository;
            _membersRepository = membersRepository;
            _jwtService = jwtService;   // 儲存注入物件
        }

        //===============Admin 會員管理===============
        //Include ActivityLogService
        //會員列表
        //[AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "View" }, permissions: new[] { "Administor" })]
        public IActionResult Index()
        {
            var sessionRole = HttpContext.Session.GetString("Roles");
            Debug.WriteLine("===== Controllers/AccountController.cs =====");
            Debug.WriteLine("--- Admin_read ---");
            Debug.WriteLine($"Session Role: {sessionRole}");

            var isLogin = HttpContext.Session.GetString("isLogin");
            Debug.WriteLine($"isLogin: {isLogin}");

            // 檢查是否為 Admin 登入
            if (HttpContext.Session.GetString("isLogin") != "true" ||
                HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["LoginAlert"] = "您沒有管理權限";
                return RedirectToAction("Login");
            }

            var members = _membersRepository.GetAllMembers();
            return View(members);
        }
        // 查看會員詳情（Detail）
        [AuthorizeRole(roles: new[] { "Admin" })]
        public IActionResult Detail(int id)
        {
            var role = HttpContext.Session.GetString("Roles");
            var caps = HttpContext.Session.GetString("Capabilities");
            var perms = HttpContext.Session.GetString("Permissions");
            Debug.WriteLine("===== Controllers/UsersController.cs =====");
            Debug.WriteLine("--- Detail Session ---");
            Debug.WriteLine($"Session Role: {role}");
            Debug.WriteLine($"Session caps: {caps}");
            Debug.WriteLine($"Session perms: {perms}");

            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            var user = _membersRepository.GetMemberById(id);

            if (user == null)
            {
                TempData["AdminError"] = "找不到該會員";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // 刪除會員
        //Include ActivityLogService
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "Delete" })]
        public async Task<IActionResult> DeleteMember(int id)
        {
            Debug.WriteLine("===== Controllers/AccountController.cs =====");
            Debug.WriteLine("--- DeleteMember ---");
            Debug.WriteLine($"=== ID : {id} ===");

            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            bool success = _membersRepository.DeleteMember(id);
            // 刪除會員寫入 ActivityLog
            await _activityLog.LogAsync(
                memberId: null,
                companyId: null,
                actionType: "Admin.DeleteMember",
                actionCategory: "Admin",
                outcome: success ? "Success" : "Failure",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: HttpContext.Session.GetString("Username"),
                detailsObj: new { deletememberId = id }
            );
            if (!success)
            {
                TempData["AdminError"] = "刪除失敗";
            }
            return RedirectToAction("Index");
        }

        // 編輯會員
        //Include ActivityLogService
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "Edit" })]
        public async Task<IActionResult> EditMember(int id, string username, string email, string fullname)
        {
            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            bool success = _membersRepository.UpdateMember(id, username, email, fullname);
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
            return RedirectToAction("Index");
        }
    }
}
