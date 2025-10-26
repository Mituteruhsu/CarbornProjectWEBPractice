using Microsoft.Data.SqlClient;
using System;

namespace CarbonProject.Models
{
    public class Company
    {
        public int CompanyId { get; set; }
        public int MemberId { get; set; } // 關聯到 Members
        public string CompanyName { get; set; }
        public string TaxId { get; set; }
        public string Industry { get; set; } // 存 Industry_Id (A-01)
        public string Address { get; set; }
        public string Contact_Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        private static string connStr;

        public static void Init(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        public static bool AddCompany(Company company)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sql = @"INSERT INTO Companies
                    (CompanyId, CompanyName, TaxId, Industry, Address, Contact_Email, CreatedAt, UpdatedAt)
                    VALUES (@CompanyId, @CompanyName, @TaxId, @Industry, @Address, @Contact_Email, GETDATE(), GETDATE())";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyId", company.CompanyId);
                    cmd.Parameters.AddWithValue("@CompanyName", company.CompanyName ?? "");
                    cmd.Parameters.AddWithValue("@TaxId", company.TaxId ?? "");
                    cmd.Parameters.AddWithValue("@Industry", company.Industry ?? "");
                    cmd.Parameters.AddWithValue("@Address", company.Address ?? "");
                    cmd.Parameters.AddWithValue("@Contact_Email", company.Contact_Email ?? "");
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}