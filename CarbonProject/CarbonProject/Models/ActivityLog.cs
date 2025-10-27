using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

namespace CarbonProject.Models
{
    public static class ActivityLog
    {
        // 連線字串從 appsettings.json 取得
        private static string connStr;
        public static void Init(IConfiguration configuration)
        {
            connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // 寫入紀錄
        public static void Write(
            int? memberId,
            int? companyId,
            string actionType,
            string actionCategory,
            string outcome,
            string ip = null,
            string userAgent = null,
            string source = "Web",
            Guid? correlationId = null,
            string detailsJson = null,
            string createdBy = null)
        {
            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("ActivityLog 尚未初始化，請先呼叫 ActivityLog.Init(config)");

            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sql = @"
                    INSERT INTO dbo.ActivityLog
                    (MemberId, CompanyId, ActionType, ActionCategory, ActionTime, Outcome, 
                     IpAddress, UserAgent, Source, CorrelationId, Details, CreatedBy, CreatedAt)
                    VALUES (@MemberId, @CompanyId, @ActionType, @ActionCategory, SYSUTCDATETIME(), @Outcome, 
                            @Ip, @UA, @Source, @CorrId, @Details, @CreatedBy, SYSUTCDATETIME());
                    ";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MemberId", (object)memberId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CompanyId", (object)companyId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ActionType", actionType);
                    cmd.Parameters.AddWithValue("@ActionCategory", (object)actionCategory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Outcome", (object)outcome ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ip", (object)ip ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UA", (object)userAgent ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Source", (object)source ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CorrId", (object)correlationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Details", (object)detailsJson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", (object)createdBy ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}