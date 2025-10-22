using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Data;

namespace CarbonProject.Models
{
    // �s�W Model�GESG ��ʤ��
    public class ESGAction
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";          // �d��: "�෽", "��q", "�]��"
        public string Description { get; set; } = "";
        public decimal ExpectedReductionTon { get; set; }   // �w����Ҷq (��/�~)
        public decimal ProgressPercent { get; set; }        // 0~100
        public string OwnerDepartment { get; set; } = "";
        public int Year { get; set; }                       // ���ݦ~��
        public bool IsCompleted => ProgressPercent >= 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
    // �Ω� Index View �� ViewModel
    public class ActionsViewModel
    {
        public List<ESGAction> Actions { get; set; } = new List<ESGAction>();
        public List<string> Categories { get; set; } = new List<string>();
        public int SelectedYear { get; set; }
        public string SelectedCategory { get; set; } = "";
    }
    // �ק� Repository�A�s�u DB�C
    public static class ActionsRepository
    {
        // �s�u�r��q appsettings.json ���o
        private static string connStr;
        public static void Init(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // ���o�Ҧ� ESGAction
        public static List<ESGAction> GetAll()
        {
            var list = new List<ESGAction>();
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT * FROM ESGActions ORDER BY Year DESC, Title ASC";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadAction(reader));
                        }
                    }
                }
            }
            catch
            {
                // �ҥ~�� Controller log
            }
            return list;
        }

        // �̦~�����o
        public static List<ESGAction> GetByYear(int year)
        {
            var list = new List<ESGAction>();
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT * FROM ESGActions WHERE Year=@Year ORDER BY ProgressPercent DESC, Title ASC";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@Year", SqlDbType.Int).Value = year;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                list.Add(ReadAction(reader));
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // �����O���o
        public static List<ESGAction> GetByCategory(string category)
        {
            var list = new List<ESGAction>();
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT * FROM ESGActions WHERE Category=@Category ORDER BY Year DESC, Title ASC";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 200).Value = category;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                list.Add(ReadAction(reader));
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // �� Id ���o
        public static ESGAction? GetById(int id)
        {
            ESGAction? action = null;
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT * FROM ESGActions WHERE Id=@Id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                                action = ReadAction(reader);
                        }
                    }
                }
            }
            catch { }
            return action;
        }

        // �s�W��ʤ��
        public static bool Add(ESGAction action)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"INSERT INTO ESGActions
                                   (Title, Category, Description, ExpectedReductionTon, ProgressPercent, OwnerDepartment, Year, CreatedAt)
                                   VALUES (@Title, @Category, @Description, @ExpectedReductionTon, @ProgressPercent, @OwnerDepartment, @Year, GETDATE())";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = action.Title;
                        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = action.Category;
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = action.Description;
                        cmd.Parameters.Add("@ExpectedReductionTon", SqlDbType.Decimal).Value = action.ExpectedReductionTon;
                        cmd.Parameters.Add("@ProgressPercent", SqlDbType.Decimal).Value = action.ProgressPercent;
                        cmd.Parameters.Add("@OwnerDepartment", SqlDbType.NVarChar, 100).Value = action.OwnerDepartment;
                        cmd.Parameters.Add("@Year", SqlDbType.Int).Value = action.Year;
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // ��s
        public static bool Update(ESGAction action)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"UPDATE ESGActions SET 
                                   Title=@Title, Category=@Category, Description=@Description, 
                                   ExpectedReductionTon=@ExpectedReductionTon, ProgressPercent=@ProgressPercent, 
                                   OwnerDepartment=@OwnerDepartment, Year=@Year, UpdatedAt=GETDATE()
                                   WHERE Id=@Id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = action.Id;
                        cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = action.Title;
                        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = action.Category;
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = action.Description;
                        cmd.Parameters.Add("@ExpectedReductionTon", SqlDbType.Decimal).Value = action.ExpectedReductionTon;
                        cmd.Parameters.Add("@ProgressPercent", SqlDbType.Decimal).Value = action.ProgressPercent;
                        cmd.Parameters.Add("@OwnerDepartment", SqlDbType.NVarChar, 100).Value = action.OwnerDepartment;
                        cmd.Parameters.Add("@Year", SqlDbType.Int).Value = action.Year;
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // �R��
        public static bool Delete(int id)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "DELETE FROM ESGActions WHERE Id=@Id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // ���o�Ҧ����O
        public static List<string> GetCategories()
        {
            var list = new List<string>();
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT DISTINCT Category FROM ESGActions ORDER BY Category ASC";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(reader["Category"].ToString() ?? "");
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // ���o�Ҧ��~��
        public static List<int> GetYears()
        {
            var list = new List<int>();
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT DISTINCT Year FROM ESGActions ORDER BY Year DESC";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Convert.ToInt32(reader["Year"]));
                    }
                }
            }
            catch { }
            return list;
        }

        // Ū�� SqlDataReader
        private static ESGAction ReadAction(SqlDataReader reader)
        {
            return new ESGAction
            {
                Id = Convert.ToInt32(reader["Id"]),
                Title = reader["Title"].ToString() ?? "",
                Category = reader["Category"].ToString() ?? "",
                Description = reader["Description"].ToString() ?? "",
                ExpectedReductionTon = Convert.ToDecimal(reader["ExpectedReductionTon"]),
                ProgressPercent = Convert.ToDecimal(reader["ProgressPercent"]),
                OwnerDepartment = reader["OwnerDepartment"].ToString() ?? "",
                Year = Convert.ToInt32(reader["Year"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["UpdatedAt"])
            };
        }
    }
}