using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models
{
    public class CompanyEmission
    {
        [Key]
        public int EmissionId { get; set; }
        public int CompanyId { get; set; }
        public int Year { get; set; }
        public byte Quarter { get; set; }   // 如果資料庫欄位是 TINYINT，使用 byte 類型
        public decimal Scope1Emission { get; set; }
        public decimal Scope2Emission { get; set; }
        public decimal Scope3Emission { get; set; }
        public decimal TotalEmission { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
