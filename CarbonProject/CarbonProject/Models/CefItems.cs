using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Data;

namespace CarbonProject.Models
{
    // 新增 Model：個人行動方案
    public class CefItems
    {
        public string name { get; set; }
        public decimal coe { get; set; }
        public string unit { get; set; }
        public string departmentname { get; set; }
        public int announcementyear { get; set; }
    }
    public class CefItemsViewModel
    {
        public List<CefItems> items { get; set; } = new List<CefItems>();
    }
}