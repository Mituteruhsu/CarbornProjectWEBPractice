using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace YourProject.Models
{
    public class Industry
    {
        public string Industry_Id { get; set; }
        public string Major_Category_Code { get; set; }
        public string Major_Category_Name { get; set; }
        public string Middle_Category_Code { get; set; }
        public string Middle_Category_Name { get; set; }

        // �s�u�r��q appsettings.json ���o
        private static string connStr;

        public static void Init(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // ���o�Ҧ����~�M��
        public static List<Industry> GetAll()
        {
            var list = new List<Industry>();

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
                    list.Add(new Industry
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

        // �̤j�����աA�ѫe�����
        public static IEnumerable<object> GetGrouped()
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

        // ���o��@���~
        public static Industry GetById(string id)
        {
            return GetAll().FirstOrDefault(i => i.Industry_Id == id);
        }

        // (�w�d) �s�W���~
        public static bool AddIndustry(Industry model)
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

        // (�w�d) ��s���~
        public static bool UpdateIndustry(Industry model)
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

        // (�w�d) �R�����~
        public static bool DeleteIndustry(string id)
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