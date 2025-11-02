using CarbonProject.Helpers;
using CarbonProject.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace CarbonProject.Repositories
{
    public class IndustryRepository
    {
        private readonly string connStr;

        public IndustryRepository(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }
        // 取得所有產業清單
        public List<IndustryViewModel> GetAll()
        {
            var list = new List<IndustryViewModel>();

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT Industry_Id, Major_Category_Code, Major_Category_Name, 
                           Middle_Category_Code, Middle_Category_Name
                    FROM Industries
                    ORDER BY Major_Category_Code, Middle_Category_Code", conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new IndustryViewModel
                    {
                        Industry_Id = reader["Industry_Id"].ToString(),
                        Major_Category_Code = reader["Major_Category_Code"].ToString(),
                        Major_Category_Name = reader["Major_Category_Name"].ToString(),
                        Middle_Category_Code = reader["Middle_Category_Code"].ToString(),
                        Middle_Category_Name = reader["Middle_Category_Name"].ToString()
                    });
                }
            }

            return list;
        }

        // 依大類分組，供前端顯示
        public IEnumerable<object> GetGrouped()
        {
            var industries = GetAll();

            return industries
                .GroupBy(i => new { i.Major_Category_Code, i.Major_Category_Name })
                .Select(g => new
                {
                    MajorCode = g.Key.Major_Category_Code,
                    MajorName = g.Key.Major_Category_Name,
                    Middles = g.Select(m => new
                    {
                        m.Middle_Category_Code,
                        m.Middle_Category_Name,
                        m.Industry_Id
                    }).ToList()
                });
        }

        // 取得單一產業
        public IndustryViewModel GetById(string id)
        {
            return GetAll().FirstOrDefault(i => i.Industry_Id == id);
        }

        // (預留) 新增產業
        public bool AddIndustry(IndustryViewModel model)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    INSERT INTO Industries 
                    (Industry_Id, Major_Category_Code, Major_Category_Name, Middle_Category_Code, Middle_Category_Name)
                    VALUES (@Industry_Id, @Major_Category_Code, @Major_Category_Name, @Middle_Category_Code, @Middle_Category_Name)
                ", conn);
                cmd.Parameters.AddWithValue("@Industry_Id", model.Industry_Id);
                cmd.Parameters.AddWithValue("@Major_Category_Code", model.Major_Category_Code);
                cmd.Parameters.AddWithValue("@Major_Category_Name", model.Major_Category_Name);
                cmd.Parameters.AddWithValue("@Middle_Category_Code", model.Middle_Category_Code);
                cmd.Parameters.AddWithValue("@Middle_Category_Name", model.Middle_Category_Name);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // (預留) 更新產業
        public bool UpdateIndustry(IndustryViewModel model)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    UPDATE Industries SET 
                        Major_Category_Code=@Major_Category_Code,
                        Major_Category_Name=@Major_Category_Name,
                        Middle_Category_Code=@Middle_Category_Code,
                        Middle_Category_Name=@Middle_Category_Name
                    WHERE Industry_Id=@Industry_Id
                ", conn);
                cmd.Parameters.AddWithValue("@Industry_Id", model.Industry_Id);
                cmd.Parameters.AddWithValue("@Major_Category_Code", model.Major_Category_Code);
                cmd.Parameters.AddWithValue("@Major_Category_Name", model.Major_Category_Name);
                cmd.Parameters.AddWithValue("@Middle_Category_Code", model.Middle_Category_Code);
                cmd.Parameters.AddWithValue("@Middle_Category_Name", model.Middle_Category_Name);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // (預留) 刪除產業
        public bool DeleteIndustry(string id)
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand("DELETE FROM Industries WHERE Industry_Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}