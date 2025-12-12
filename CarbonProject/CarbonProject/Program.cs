// 版本2.0
using CarbonProject.Data; // DbContext 命名空間
using CarbonProject.Middleware;
using CarbonProject.Repositories;
using CarbonProject.Services;  // Services 命名空間
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using CarbonProject.Service.Logging;
using CarbonProject.Service.RBAC;

var builder = WebApplication.CreateBuilder(args);

// === 環境偵測 ===
var environment = builder.Environment.EnvironmentName;
Debug.WriteLine("===== Program.cs =====");
Debug.WriteLine($"[Environment] ASPNETCORE_ENVIRONMENT = {environment}");

// 判斷是否為開發環境
var isDevelopment = builder.Environment.IsDevelopment();
Debug.WriteLine($"[Environment] 是否為開發環境 = {isDevelopment}");

// === 調整 ASP.NET Core Logging Filter ===
// --- 減少偵錯輸出 ---
//builder.Logging.ClearProviders(); // 清空 ASP.NET Core 預設的所有 Logging Provider。
//builder.Logging.AddConsole();
// 以下兩項是常見的
Debug.WriteLine("--- Microsoft.Hosting.Lifetime --- ");
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
Debug.WriteLine("--- Microsoft.EntityFrameworkCore.Database.Command --- ");
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// === 讀取環境變數 ===
var AZURE_SQL_USER = Environment.GetEnvironmentVariable("AZURE_SQL_USER");
var AZURE_SQL_PWD = Environment.GetEnvironmentVariable("AZURE_SQL_PWD");

// === 建立連線 (使用環境變數) ===
var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection")
    .Replace("{SQL_USER}", AZURE_SQL_USER)
    .Replace("{SQL_PWD}", AZURE_SQL_PWD);

// 把連線字串存回設定
builder.Configuration["ConnectionStrings:DefaultConnection"] = rawConnStr;

// === 註冊 MVC 基本設定 === 自動依據環境載入 appsettings.{Environment}.json
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// === 註冊 DbContext 依環境設定日誌===
// 註冊 DbContext From -> Data/CarbonDbContext.cs
builder.Services.AddDbContext<CarbonDbContext>(options =>
{
    options.UseSqlServer(rawConnStr);
    if (isDevelopment)
    {
        // 開發環境：顯示 SQL 指令與參數
        options.EnableSensitiveDataLogging(false);   // 可選，避免輸出參數 (true), (false)
        options.LogTo(Console.WriteLine, LogLevel.Error); // 輸出 LogLevel.Information, LogLevel.Warning 可改成想要的輸出
    }
    else
    {
        // 生產環境：只顯示 Warning 以上，或完全不輸出
        options.LogTo(_ => { }, LogLevel.None);     // 完全不輸出
    }
});

// 註冊 DbContext From -> Data/RbacDbContext.cs
builder.Services.AddDbContext<RbacDbContext>(options =>
{
    options.UseSqlServer(rawConnStr);
    if (isDevelopment)
    {
        // 開發環境：顯示 SQL 指令與參數
        options.EnableSensitiveDataLogging(false);   // 可選，避免輸出參數 (true), (false)
        options.LogTo(Console.WriteLine, LogLevel.Error); // 輸出 LogLevel.Information, LogLevel.Warning, LogLevel.Error 可改成想要的輸出
    }
    else
    {
        // 生產環境：只顯示 Warning 以上，或完全不輸出
        options.LogTo(_ => { }, LogLevel.None);     // 完全不輸出
    }
});

// === 註冊 Service ===
// 方法          意義                             生命週期
// AddTransient  每次使用都產生新物件             短暫，request 內每次注入都是新物件
// AddScoped     每個 HTTP Request                都共用同一個物件	Request 內共用，下一個 Request 會重建
// AddSingleton  整個應用程式都共用同一個物件     生命週期最長，直到 App 關閉

// 註冊 DI (RBAC 用) From -> Repositories/. + Service/.
builder.Services.AddScoped<MembersRepository>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<CapabilityService>();
builder.Services.AddScoped<RBACService>();
builder.Services.AddSingleton<JWTService>(); // 注冊 Service 為 singleton（stateless）或 transient 都可；singleton 比較省資源且安全
builder.Services.AddScoped<CarbonCalculationService>();

// 註冊 DI Service From -> Repositories/.
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<HomeIndexRepository>();
builder.Services.AddScoped<ESGActionRepository>();
builder.Services.AddScoped<IndustryRepository>();
builder.Services.AddScoped<RbacRepository>();
builder.Services.AddScoped<ActivityLogRepository>();
builder.Services.AddScoped<CarbonCalculationRepository>();

// 註冊 Service From -> Service/.
builder.Services.AddScoped<EmissionService>();
builder.Services.AddScoped<ActivityLogService>();

// 註冊 HttpClient From -> Service/.
builder.Services.AddHttpClient<CarbonFactorImportService>();
// 註冊 Scheduler From -> Service/.
Debug.WriteLine($"CarbonFactorUpdateScheduler 開啟自動排程");
builder.Services.AddHostedService<CarbonFactorUpdateScheduler>();

// JWT 設定 (僅做 Token 驗證，不做授權)
// 使用內建 Jwt auth middleware 加：
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])
        ),
        ClockSkew = TimeSpan.Zero
    };
    // 這裡是關鍵：允許從 Cookie 讀取 Token JWT (RememberMe)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

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

// === 初始化 RBAC 資料 ===
// 由 appsettings.json 來操作啟動，可開啟或關閉每次執行
var seedEnabled = builder.Configuration.GetValue<bool>("EnableRbacSeed");
if (seedEnabled)
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var rbacContext = scope.ServiceProvider.GetRequiredService<RbacDbContext>();
            Debug.WriteLine("===== Program.cs =====");
            Debug.WriteLine("--- RBAC 資料初始化 --- ");
            Debug.WriteLine("[RBAC] Database 初始化-開始...");
            RbacDbInitializer.Initialize(rbacContext);
            Debug.WriteLine("[RBAC] 已初始化-完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RBAC] 初始化-失敗: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }
    }
}

// === 啟動時輸出連線資訊（非敏感部分）===
Debug.WriteLine("===== Program.cs - 連線資訊 =====");
Debug.WriteLine("--- 連線資訊 ---");
Debug.WriteLine($"[Connection] Using database: {builder.Configuration.GetConnectionString("DefaultConnection")?.Split(';')[1]}");
Debug.WriteLine($"[Connection] SQL User: {AZURE_SQL_USER}");

// === Pipeline 中介層 ===
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
// 1️ 啟用 Session
app.UseSession();   //使用 session 必須在 UseRouting 後、UseEndpoints 前，必須先啟用 Session（中間件用到 session）
// 2️ 自訂 RBAC Middleware（從 JWT 自動補 Session）
app.UseMiddleware<RBACMiddleware>();
//app.UseRBAC(); // 自動恢復 Session, 放在 UseRouting() 之前或之後都可，但必須在 Controller 執行前

// 接著是 Authentication / Authorization
// 3️ 啟用 Authentication（只驗證 JWT，不進行 RBAC）
app.UseAuthentication(); // JWT middleware
// 4️ Authorization (讓 MVC Attribute 生效，但 RBAC 由你自訂控制)
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
