using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Data;

namespace CarbonProject.Models
{    
    // 新增 Model：ESG 行動方案
    public class ESGActionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";          // 範例: "能源", "交通", "設備"
        public string Description { get; set; } = "";
        public decimal ExpectedReductionTon { get; set; }   // 預期減碳量 (噸/年)
        public decimal ProgressPercent { get; set; }        // 0~100
        public string OwnerDepartment { get; set; } = "";
        public int Year { get; set; }                       // 所屬年度
        public bool IsCompleted => ProgressPercent >= 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
    // 用於 Index View 的 ViewModel
    public class ActionsViewModel
    {
        public List<ESGActionViewModel> Actions { get; set; } = new List<ESGActionViewModel>();
        public List<string> Categories { get; set; } = new List<string>();
        public int SelectedYear { get; set; }
        public string SelectedCategory { get; set; } = "";
    }
}