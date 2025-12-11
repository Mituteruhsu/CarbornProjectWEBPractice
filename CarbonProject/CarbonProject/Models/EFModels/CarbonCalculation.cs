namespace CarbonProject.Models.EFModels
{
    public class CarbonCalculation
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int FactorId { get; set; }

        public decimal InputValue { get; set; }
        public decimal ResultValue { get; set; }

        public string RoleAtCalculation { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
