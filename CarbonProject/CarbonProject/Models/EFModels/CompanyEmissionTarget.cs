using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels
{
    public class CompanyEmissionTarget
    {
        [Key]
        public int TargetId { get; set; }
        public int CompanyId { get; set; }
        public int BaselineYear { get; set; }
        public int TargetYear { get; set; }
        public decimal BaselineEmission { get; set; }
        public decimal TargetEmission { get; set; }
        public decimal ReductionPercent { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}