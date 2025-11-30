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
        // 2. 編輯個人資料頁面
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
        // =========================
        // 2-1. 編輯個人資料頁面-更新文字資料(不處理圖片)
        // =========================
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
        // 3. 上傳更新頭像圖片
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfileImage(IFormFile profileImageFile)
        {
            int memberId = HttpContext.Session.GetInt32("MemberId").Value;
            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (profileImageFile == null || profileImageFile.Length == 0)
            {
                TempData["Error"] = "未選擇圖片";
                return RedirectToAction("EditProfile");
            }
            // 3_1. 限制大小:1MB
            if (profileImageFile.Length > 1 * 1024 * 1024)
            {
                TempData["Error"] = "圖片大小不能超過 1MB";
                return RedirectToAction("EditProfile");
            }
            // 3_2. 限制格式
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(profileImageFile.FileName).ToLower();

            if (!allowedExt.Contains(ext))
            {
                TempData["Error"] = "圖片格式僅限 JPG / JPEG / PNG / GIF";
                return RedirectToAction("EditProfile");
            }
            // 3_3. 儲存路徑 /wwwroot/img_profile
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img_profile");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
            
            //3_4. 檔名重新命名：避免檔名衝突
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(profileImageFile.FileName)}";
            string filePath = Path.Combine(uploadPath, fileName);

            //3_5. 儲存檔案
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                profileImageFile.CopyTo(stream);
            }

            //3_6. 刪除舊圖
            var member = _membersRepo.GetMemberById(memberId);
            if (!string.IsNullOrEmpty(member.ProfileImage))
            {
                string oldPath = Path.Combine(uploadPath, member.ProfileImage);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            //3_7. 更新 DB
            bool upload_success = _membersRepo.UpdateProfileImage(memberId, fileName);
            if(upload_success) 
                return Json(new{success = true, imageUrl = Url.Content($"~/img_profile/{fileName}") });
            TempData["Success"] = "頭像更新成功";
            return RedirectToAction("EditProfile");
        }
        // =========================
        // 3-1. 刪除頭像圖片
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProfileImage()
        {
            int memberId = HttpContext.Session.GetInt32("MemberId").Value;
            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _membersRepo.GetMemberById(memberId);
            if (member == null) return NotFound();

            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img_profile");
            
            // 刪除舊圖
            if (!string.IsNullOrEmpty(member.ProfileImage))
            {
                string oldPath = Path.Combine(uploadPath, member.ProfileImage);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            _membersRepo.UpdateProfileImage(memberId, null);

            TempData["Success"] = "頭像已刪除";
            return RedirectToAction("EditProfile");
        }
        // =========================
        // 4. 修改密碼頁面
        // =========================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(); // View -> Views/Profile/ChangePassword.cshtml
        }
        // =========================
        // 4-1. 修改密碼
        // =========================
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
