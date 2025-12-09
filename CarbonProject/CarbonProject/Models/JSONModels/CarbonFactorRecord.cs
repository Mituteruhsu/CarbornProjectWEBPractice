// 用來串接 API 到 dbo.CarbonFactor
namespace CarbonProject.Models.JSONModels
{
    public class CarbonFactorRecord
    {
        public string Name { get; set; }
        public string Coe { get; set; }
        public string Unit { get; set; }
        public string DepartmentName { get; set; }
        public string AnnouncementYear { get; set; }
    }

    public class CarbonFactorApiResponse
    {
        public List<CarbonFactorRecord> Records { get; set; }
    }

}