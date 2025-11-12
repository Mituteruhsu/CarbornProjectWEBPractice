using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels
{
    public class ActivityLog
    {
        [Key]
        public long LogId { get; set; }
        public int? MemberId { get; set; }
        public int? CompanyId { get; set; }
        public string ActionType { get; set; }        // e.g. "Create"
        public string ActionCategory { get; set; }    // e.g. "CompanyEmission"
        public DateTime ActionTime { get; set; }
        public string Outcome { get; set; }           // e.g. "Success"
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Source { get; set; }
        public Guid CorrelationId { get; set; }
        public string Details { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}