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
        public string Role { get; set; } // Admin / Viewer / Company
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}