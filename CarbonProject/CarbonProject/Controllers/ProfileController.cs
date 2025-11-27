using CarbonProject.Models;
using CarbonProject.Models.EFModels.RBAC;
using CarbonProject.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Claims;

namespace CarbonProject.Controllers
{
    [Authorize] // 只允許登入使用者
    public class ProfileController : Controller
    {
        private readonly MembersRepository _membersRepo;

        public ProfileController(MembersRepository membersRepo)
        {
            _membersRepo = membersRepo;
        }

        // =========================
        // 1. Index / Profile 檢視頁面
        // =========================
        public IActionResult Index()
        {
            Debug.WriteLine("===== Controllers/ProfileController.cs =====");
            Debug.WriteLine("--- Index ---");
            // 取得目前登入使用者 ID
            int memberId = HttpContext.Session.GetInt32("MemberId").Value;
            Debug.WriteLine($"取得目前登入使用者 ID: {memberId}");
            if (memberId == null)
            {
                return RedirectToAction("Login", "Account"); // 沒登入，導向登入頁
            }
            // 取得會員資料
            var member = _membersRepo.GetMemberById(memberId);

            return View(member); // View -> Views/Profile/Index.cshtml
        }

        // =========================
        // 2. 編輯個人資料
        // =========================
        [HttpGet]
        public IActionResult EditProfile()
        {
            int memberId = HttpContext.Session.GetInt32("MemberId").Value;
            if (memberId == null)
            {
                return RedirectToAction("Login", "Account"); // 沒登入，導向登入頁
            }
            var member = _membersRepo.GetMemberById(memberId);
            if (member == null) return NotFound();

            return View(member); // View -> Views/Profile/EditProfile.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(MembersViewModel model)
        {
            if (ModelState.IsValid)
            {
                int memberId = HttpContext.Session.GetInt32("MemberId").Value;
                if (memberId == null)
                {
                    return RedirectToAction("Login", "Account"); // 沒登入，導向登入頁
                }

                // 更新會員資料
                bool success = _membersRepo.UpdateMember(
                    memberId,
                    model.Username,
                    model.Email,
                    model.FullName,
                    model.Birthday,
                    model.Gender,
                    model.Address,
                    model.IsActive
                );

                if (success)
                {
                    TempData["Success"] = "個人資料更新成功";
                    return RedirectToAction("Index");
                }
                TempData["Error"] = "更新失敗";
            }
            return View(model);
        }

        // =========================
        // 3. 修改密碼
        // =========================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(); // View -> Views/Profile/ChangePassword.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                int memberId = HttpContext.Session.GetInt32("MemberId").Value;
                if (memberId == null)
                {
                    return RedirectToAction("Login", "Account"); // 沒登入，導向登入頁
                }
                var member = _membersRepo.GetMemberById(memberId);

                // 驗證舊密碼
                if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, member.PasswordHash))
                {
                    ModelState.AddModelError("", "舊密碼錯誤");
                    return View(model);
                }

                // 更新新密碼
                _membersRepo.ResetPassword(memberId, model.NewPassword);

                TempData["Success"] = "密碼更新成功";
                return RedirectToAction("Index");
            }
            return View(model);
        }
    }
}
