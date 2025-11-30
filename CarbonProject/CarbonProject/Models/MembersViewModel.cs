using CarbonProject.Models.EFModels;
using CarbonProject.Models.EFModels.RBAC;
using System;
using System.Collections.Generic;

// 從上方工具->NuGet套件管理員->管理方案的 NuGet 套件->瀏覽輸入後點選安裝(需選擇專案)
// 需安裝 MySql.Data
// 需安裝 BCrypt.Net-Next
// 需安裝 Microsoft.Data.SqlClient


namespace CarbonProject.Models
{
    public class MembersViewModel
    {
        public int MemberId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public int CompanyId { get; set; }
        public Company Company { get; set; }  // 使用 EFModels 的 Company
        public string Role { get; set; } // Admin / Viewer / Company / 單一角色
        public List<UserRole> UserRoles { get; set; } = new(); // 多角色
        public bool IsActive { get; set; }          // 帳號狀態
        // 驗證狀態
        public bool EmailConfirmed { get; set; }
        public bool PhoneConfirmed { get; set; }
        // 登入資訊
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastLogoutAt { get; set; }
        public DateTime? LastFailedLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        // 時間戳
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // 新增 ActivityLogs 屬性
        public List<ActivityLog> ActivityLogs { get; set; } = new();
        // 其他會員相關欄位
        public string? ProfileImage { get; set; }  // 顯示原圖
        public IFormFile? ProfileImageFile { get; set; } // 接收上傳檔案
        public DateTime? Birthday { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? MembershipLevel { get; set; }
        public int Points { get; set; }
    }
}