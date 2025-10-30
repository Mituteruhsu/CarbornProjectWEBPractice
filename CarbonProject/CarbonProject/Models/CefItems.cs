using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Data;

namespace CarbonProject.Models
{
    // �s�W Model�G�ӤH��ʤ��
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