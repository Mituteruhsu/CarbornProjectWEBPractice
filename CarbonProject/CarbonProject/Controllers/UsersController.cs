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
            else
            {
                TempData["AdminSuccess"] = "刪除成功";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "Edit" })]
        public async Task<IActionResult> ResetPassword(int id)
        {
            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login", "Account");
            }

            // 產生臨時密碼（可改為更安全的亂數或發送重置連結）
            string newPlainPassword = GenerateSecureTemporaryPassword(); // 我也給出簡單方法
            bool success = _membersRepository.ResetPassword(id, newPlainPassword);

            await _activityLog.LogAsync(
                memberId: id,
                companyId: null,
                actionType: "Admin.ResetPassword",
                actionCategory: "Admin",
                outcome: success ? "Success" : "Failure",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: HttpContext.Session.GetString("Username"),
                detailsObj: new { newPasswordHint = MaskPasswordForLog(newPlainPassword) }
            );

            if (!success)
            {
                TempData["AdminError"] = "重設密碼失敗";
                return RedirectToAction("Detail", new { id });
            }

            // 直接把明碼顯示給管理員
            TempData["AdminSuccess"] = $"密碼已重設";
            TempData["NewPassword"] = newPlainPassword;

            return RedirectToAction("Detail", new { id });
        }
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "Edit" })]
        public IActionResult AssignRole(int id)
        {
            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login", "Account");
            }

            var member = _membersRepository.GetMemberById(id);
            if (member == null)
            {
                TempData["AdminError"] = "找不到該會員";
                return RedirectToAction("Index");
            }

            // 取得系統所有角色（簡單做法：從 DB 取得 Roles 清單）
            var roles = _membersRepository.GetAllRoles(); // 我會在 repository 加此方法

            var vm = new AssignRoleViewModel
            {
                MemberId = member.MemberId,
                Username = member.Username,
                CurrentRoleIds = member.UserRoles?.Select(ur => ur.RoleId).ToList() ?? new List<int>(),
                Roles = roles // List<Role> (含 RoleId, RoleName)
            };

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "Edit" })]
        public async Task<IActionResult> AssignRole(AssignRoleViewModel model)
        {
            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login", "Account");
            }

            // 移除該 Member 既有所有 role，然後依 model.SelectedRoleIds 加回
            try
            {
                _membersRepository.RemoveAllRolesForMember(model.MemberId);

                if (model.SelectedRoleIds != null && model.SelectedRoleIds.Any())
                {
                    foreach (var roleId in model.SelectedRoleIds)
                    {
                        _membersRepository.AddRoleToMember(model.MemberId, roleId);
                    }
                }

                await _activityLog.LogAsync(
                    memberId: model.MemberId,
                    companyId: null,
                    actionType: "Admin.AssignRole",
                    actionCategory: "Admin",
                    outcome: "Success",
                    ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString(),
                    createdBy: HttpContext.Session.GetString("Username"),
                    detailsObj: new { assignedRoles = model.SelectedRoleIds }
                );

                TempData["AdminSuccess"] = "角色設定成功";
            }
            catch (Exception ex)
            {
                await _activityLog.LogAsync(
                    memberId: model.MemberId,
                    companyId: null,
                    actionType: "Admin.AssignRole",
                    actionCategory: "Admin",
                    outcome: "Failure",
                    ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString(),
                    createdBy: HttpContext.Session.GetString("Username"),
                    detailsObj: new { error = ex.Message }
                );

                TempData["AdminError"] = "角色設定失敗";
            }

            return RedirectToAction("Detail", new { id = model.MemberId });
        }


        // ====== 補助方法（放在 UsersController 類別內或外部 helper） ======
        private string GenerateSecureTemporaryPassword(int length = 12)
        {
            // 簡單實作：產生包含數字 + 大小寫字母的亂數字串（可替換為更嚴謹產生器）
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rnd = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
        }

        private string MaskPasswordForLog(string pw)
        {
            if (string.IsNullOrEmpty(pw)) return "";
            if (pw.Length <= 4) return new string('*', pw.Length);
            return new string('*', pw.Length - 4) + pw[^4..];
        }

        // 編輯會員
        // 顯示編輯畫面 (GET)
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "Edit" })]
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            var user = _membersRepository.GetMemberById(id);
            if (user == null)
            {
                TempData["AdminError"] = "找不到會員";
                return RedirectToAction("Index");
            }

            return View(user);  // View 要使用 MembersViewModel
        }

        //Include ActivityLogService
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(roles: new[] { "Admin" }, capabilities: new[] { "Edit" })]
        public async Task<IActionResult> Edit(MembersViewModel model)
        {
            if (HttpContext.Session.GetString("Roles") != "Admin")
            {
                TempData["AdminError"] = "無權限操作";
                return RedirectToAction("Login");
            }

            bool success = _membersRepository.UpdateMember(
                                                            model.MemberId,
                                                            model.Username,
                                                            model.Email,
                                                            model.FullName,
                                                            model.Birthday,
                                                            model.Gender,
                                                            model.Address,
                                                            model.IsActive
                                                            );
            // 編輯會員寫入 ActivityLog
            await _activityLog.LogAsync(
                memberId: model.MemberId,
                companyId: null,
                actionType: "Admin.EditMember",
                actionCategory: "Admin",
                outcome: success ? "Success" : "Failure",
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString(),
                createdBy: HttpContext.Session.GetString("Username"),
                detailsObj: new {
                    model.Username,
                    model.Email,
                    model.FullName
                }
            );

            if (!success)
            {
                TempData["AdminError"] = "更新失敗";
                return View(model);
            }
            
            TempData["AdminSuccess"] = "更新成功";
            return RedirectToAction("Detail", new { id = model.MemberId });
        }
    }
}
