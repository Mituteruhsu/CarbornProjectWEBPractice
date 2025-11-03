// 版本2.0
using CarbonProject.Data; // DbContext 命名空間
using CarbonProject.Repositories;
using CarbonProject.Services;  // Services 命名空間
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// === 環境偵測 ===
var environment = builder.Environment.EnvironmentName;
Console.WriteLine($"[Environment] ASPNETCORE_ENVIRONMENT = {environment}");

// === 讀取環境變數 ===
var AZURE_SQL_USER = Environment.GetEnvironmentVariable("AZURE_SQL_USER");
var AZURE_SQL_PWD = Environment.GetEnvironmentVariable("AZURE_SQL_PWD");

var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection")
    .Replace("{SQL_USER}", AZURE_SQL_USER)
    .Replace("{SQL_PWD}", AZURE_SQL_PWD);

// 把連線字串存回設定
builder.Configuration["ConnectionStrings:DefaultConnection"] = rawConnStr;

// === 註冊 MVC 基本設定 === 自動依據環境載入 appsettings.{Environment}.json
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// 註冊 DbContext From -> Data/CarbonDbContext.cs
builder.Services.AddDbContext<CarbonDbContext>(options =>
    options.UseSqlServer(rawConnStr));

// 註冊 Service
// 方法          意義                             生命週期
// AddTransient  每次使用都產生新物件             短暫，request 內每次注入都是新物件
// AddScoped     每個 HTTP Request                都共用同一個物件	Request 內共用，下一個 Request 會重建
// AddSingleton  整個應用程式都共用同一個物件     生命週期最長，直到 App 關閉
// 註冊 Service From -> Service/.
builder.Services.AddScoped<EmissionService>();
builder.Services.AddScoped<ActivityLogService>();
// 註冊 Service From -> Repositories/.
builder.Services.AddScoped<MembersRepository>();
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<HomeIndexRepository>();
builder.Services.AddScoped<ESGActionRepository>();
builder.Services.AddScoped<IndustryRepository>();
builder.Services.AddScoped<ActivityLogRepository>();

// 強化 session 與 cookie 設定
builder.Services.AddDistributedMemoryCache();   // 存放在新空間
// 創建 session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 視需求調整
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // production: Always
});

// === 初始化資料庫 ===
// builder.Configuration 內已經是替換後的連線字串，因為將連線方式改為非 static 改為註冊 Services - Repositories
//CarbonProject.Models.MembersViewModel.Init(builder.Configuration);           // 初始化 Members 連線字串

// === 啟用 app ===
var app = builder.Build();

// === 啟動時輸出連線資訊（非敏感部分）===
Console.WriteLine($"[Connection] Using database: {builder.Configuration.GetConnectionString("DefaultConnection")?.Split(';')[1]}");
Console.WriteLine($"[Connection] SQL User: {AZURE_SQL_USER}");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();   //使用 session 必須在 UseRouting 後、UseEndpoints 前
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
