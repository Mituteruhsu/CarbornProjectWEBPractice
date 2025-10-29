using CarbonProject.Models;
using Microsoft.EntityFrameworkCore;
//EF Core
//[ SQL Server 資料庫 ]
//        │
//        ▼
//[ CarbonDbContext.cs ]  ←─── 連線 + 管理 Table 對應
//        │
//        ▼
//[Models][EFModels]
//   ├─ CompanyEmission.cs  ←───資料表模型（Entity）
//   └─ CompanyEmissionTarget.cs  ←───資料表模型（Entity）
//        │
//        ▼
//[DashboardController.cs]  ←── 控制器：呼叫資料、處理邏輯
//        │
//        ▼
//[DashboardViewModel.cs]   ←── 封裝多個資料集供 View 使用
//        │
//        ▼
//[Views/Dashboard/Index.cshtml] ←── 前端顯示

namespace CarbonProject.Data
{
    public class CarbonDbContext : DbContext
    {
        public CarbonDbContext(DbContextOptions<CarbonDbContext> options) : base(options) { }

        public DbSet<CompanyEmissionTarget> CompanyEmissionTargets { get; set; }
        public DbSet<CompanyEmission> CompanyEmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompanyEmissionTarget>().HasKey(t => t.TargetId);
            modelBuilder.Entity<CompanyEmission>().HasKey(e => e.EmissionId);
        }
    }
}