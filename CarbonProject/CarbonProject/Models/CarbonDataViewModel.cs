namespace CarbonProject.Models
{
    // 新增 Model：CarbonData
    // 這個模型用於首頁展示簡易的碳排放數據。
    public class CarbonDataViewModel
    {
        public string CompanyName { get; set; }
        public decimal CurrentEmission { get; set; }   // 目前碳排放量
        public decimal TargetEmission { get; set; }    // 年度目標碳排放量
        public decimal AchievementRate { get; set; }   // 達成率 (0~1)
    }
    // 年度碳排放資料
    // Useby - DataGoalsViewModel
    public class AnnualEmission
    {
        public int Year { get; set; }
        public decimal Emission { get; set; } // 單位：噸
    }
    // 企業碳排放目標
    public class CarbonGoal
    {
        public decimal CurrentEmission { get; set; }  // 目前排放量
        public decimal TargetEmission { get; set; }   // 目標排放量
        public decimal ProgressRate => TargetEmission == 0 ? 0 : 1 - (CurrentEmission / TargetEmission);
    }
    // 整合 年度碳排/企業碳排放 兩個 Model 成 View 便於顯示在同一個頁面
    public class DataGoalsViewModel
    {
        public List<AnnualEmission> AnnualEmissions { get; set; }
        public CarbonGoal Goal { get; set; }    // 這裡不用 List 因為公司通常只會有一個目標
    }
    // CompanyEmissions 相關
    // Useby - DashboardChartsViewModel
    public class YearlyTotalDto
    {
        public int Year { get; set; }
        public decimal TotalEmission { get; set; }
    }
    // Useby - DashboardChartsViewModel
    public class YearlyAvgPerCompanyDto
    {
        public int Year { get; set; }
        public decimal AvgPerCompany { get; set; }
    }
    // Useby - DashboardChartsViewModel
    public class ScopeShareDto
    {
        public string Scope { get; set; } = "";
        public decimal Value { get; set; }
    }
    // Useby - DashboardChartsViewModel
    public class TopCompanyDto
    {
        public int? CompanyId { get; set; }
        public decimal TotalEmission { get; set; }
    }

    public class DashboardChartsViewModel
    {
        public List<YearlyTotalDto> YearlyTotals { get; set; } = new();
        public List<YearlyAvgPerCompanyDto> YearlyAverages { get; set; } = new();
        public List<ScopeShareDto> LatestScopeShares { get; set; } = new();
        public List<TopCompanyDto> TopCompaniesLatest { get; set; } = new();
    }
}