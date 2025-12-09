// 用來對應 dbo.CarbonFactor 的數據
namespace CarbonProject.Models.EFModels
{    public class CarbonFactor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Coe { get; set; }
        public string Unit { get; set; }
        public string? DepartmentName { get; set; }
        public int AnnouncementYear { get; set; }
    }
}