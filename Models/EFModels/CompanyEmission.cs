using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels
{
    public class CompanyEmission
    {
        [Key]
        public int EmissionId { get; set; }
        public int? CompanyId { get; set; }
        public int Year { get; set; }
        public byte Quarter { get; set; }   // 如果資料庫欄位是 TINYINT，使用 byte 類型
        // EF Core 在沒有指定精度的情況下，會自動使用預設的 decimal(18,2)。
        //但如果你的實際數值超出範圍（例如 999999999999999.999）
        [Precision(18, 4)] // 精度18位，小數4位
        public decimal Scope1Emission { get; set; }
        [Precision(18, 4)]
        public decimal Scope2Emission { get; set; }
        [Precision(18, 4)]
        public decimal Scope3Emission { get; set; }
        [Precision(18, 4)]
        public decimal TotalEmission { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
