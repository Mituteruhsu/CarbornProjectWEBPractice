using CarbonProject.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;

namespace CarbonProject.Repositories
{
    public class ESGActionRepository
    {
        // 連線字串從 appsettings.json 取得
        private readonly string connStr;
        public ESGActionRepository(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // 取得所有 ESGAction
        public List<ESGActionViewModel> GetAll()
        {
            var list = new List<ESGActionViewModel>();
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
                // 例外由 Controller log
            }
            return list;
        }

        // 依年份取得
        public List<ESGActionViewModel> GetByYear(int year)
        {
            var list = new List<ESGActionViewModel>();
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

        // 依類別取得
        public List<ESGActionViewModel> GetByCategory(string category)
        {
            var list = new List<ESGActionViewModel>();
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

        // 依 Id 取得
        public ESGActionViewModel? GetById(int id)
        {
            ESGActionViewModel? action = null;
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

        // Create 新增行動方案
        public bool Add(ESGActionViewModel action)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"INSERT INTO ESGActions
                                (Title, Category, Description, ExpectedReductionTon, ProgressPercent, OwnerDepartment, Year, CreatedAt)
                                VALUES (@Title, @Category, @Description, @ExpectedReductionTon, @ProgressPercent, @OwnerDepartment, @Year, SYSUTCDATETIME())";
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
        // Read 讀取 SqlDataReader
        private ESGActionViewModel ReadAction(SqlDataReader reader)
        {
            return new ESGActionViewModel
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
        // Upade 更新
        public bool Update(ESGActionViewModel action)
        {
            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"UPDATE ESGActions SET 
                                Title=@Title, Category=@Category, Description=@Description, 
                                ExpectedReductionTon=@ExpectedReductionTon, ProgressPercent=@ProgressPercent, 
                                OwnerDepartment=@OwnerDepartment, Year=@Year, UpdatedAt=SYSUTCDATETIME()
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

        // Delete 刪除
        public bool Delete(int id)
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

        // 取得所有類別
        public List<string> GetCategories()
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

        // 取得所有年份
        public List<int> GetYears()
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
    }
}