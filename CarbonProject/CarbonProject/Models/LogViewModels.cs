// Models/ViewModels/LogViewModels.cs
using System;

namespace CarbonProject.Models.ViewModels
{
    public class LogListViewModel
    {
        public long LogId { get; set; }
        public int? MemberId { get; set; }
        public int? CompanyId { get; set; }
        public string ActionType { get; set; }
        public string ActionCategory { get; set; }
        public DateTime ActionTime { get; set; }
        public string Outcome { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Source { get; set; }
        // 如果你之後有 Members 表，可以填入 MemberName
        public string MemberName { get; set; }
    }

    public class LogDetailViewModel
    {
        public long LogId { get; set; }
        public int? MemberId { get; set; }
        public int? CompanyId { get; set; }
        public string ActionType { get; set; }
        public string ActionCategory { get; set; }
        public DateTime ActionTime { get; set; }
        public string Outcome { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Source { get; set; }
        public Guid CorrelationId { get; set; }
        public string DetailsJson { get; set; } // pretty printed JSON string
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MemberName { get; set; }
    }

    public class LogQueryModel
    {
        // 篩選條件
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? MemberId { get; set; }
        public string ActionCategory { get; set; }
        public string ActionType { get; set; }
        public string Outcome { get; set; }
        public string IpAddress { get; set; }
        public string Keyword { get; set; } // 搜尋 details 或其他文字
        // 分頁
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
