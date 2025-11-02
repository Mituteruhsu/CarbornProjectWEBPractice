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
        public string Industry_Id { get; set; } // 存 Industry_Id (A-01)
        public string Address { get; set; }
        public string Contact_Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}