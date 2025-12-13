namespace CarbonProject.Models.EFModels
{
    public class CarbonCalculation
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? BatchId { get; set; }
        public CarbonCalculationBatch? Batch { get; set; }
        public int FactorId { get; set; }
        public decimal InputValue { get; set; }
        public decimal ResultValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class CarbonCalculationBatch
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string RoleAtCalculation { get; set; } = "";
        public string? CalculationName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public decimal TotalResultValue { get; set; }  // 新增：存該批次總碳排放
        public List<CarbonCalculation> Calculations { get; set; } = new();
    }
}
