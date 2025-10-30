using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models
{
    public class Company
    {
        [Key]
        public int? CompanyId { get; set; }
        public int? MemberId { get; set; } // 關聯到 Members
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
        // 查詢 TaxId 看是否存在
        public static Company? GetCompanyByTaxId(string taxId)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = @"SELECT TOP 1 * FROM Companies WHERE TaxId=@TaxId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TaxId", taxId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Company
                            {
                                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                                CompanyName = reader["CompanyName"].ToString(),
                                TaxId = reader["TaxId"].ToString(),
                                Industry = reader["Industry"].ToString(),
                                Address = reader["Address"].ToString(),
                                Contact_Email = reader["Contact_Email"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"])
                            };
                        }
                    }
                }
            }
            return null;
        }
        public static int AddCompany(Company company)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                // --- 1. 檢查是否已有相同 TaxId ---
                string checkSql = @"SELECT COUNT(*) FROM Companies WHERE TaxId=@TaxId";
                using (var cmd = new SqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TaxId", company.TaxId ?? "");
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0) return -1;
                }

                // 2. 新增公司，並回傳 CompanyId
                string sql = @"
                    INSERT INTO Companies (CompanyName, TaxId, Industry, Address, Contact_Email, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.CompanyId
                    VALUES (@CompanyName, @TaxId, @Industry, @Address, @Contact_Email, GETDATE(), GETDATE());";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CompanyName", company.CompanyName ?? "");
                    cmd.Parameters.AddWithValue("@TaxId", company.TaxId ?? "");
                    cmd.Parameters.AddWithValue("@Industry", company.Industry ?? "");
                    cmd.Parameters.AddWithValue("@Address", company.Address ?? "");
                    cmd.Parameters.AddWithValue("@Contact_Email", company.Contact_Email ?? "");
                    return (int)cmd.ExecuteScalar();
                }
            }
        }
    }
}