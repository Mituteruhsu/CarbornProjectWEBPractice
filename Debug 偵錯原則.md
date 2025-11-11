VS 設定偵錯
暫時關閉偵錯訊息
工具 -> 選項 -> 偵錯 -> 輸出視窗 -> 關閉

- 模組載入訊息
- 模組謝載訊息
- 執行緒結束訊息

# Debug 註釋原則

Debug.WriteLine("===== Controllers/AccountController.cs ====="); // 資料夾檔案源
Debug.WriteLine("--- DeleteMember ---");	// 資料類
Debug.WriteLine($"=== ID : {id} ===");		// 檢測的項目

Program.cs 偵錯
// === 調整 ASP.NET Core Logging Filter ===
// --- 減少偵錯輸出 ---
//builder.Logging.ClearProviders(); // 清空 ASP.NET Core 預設的所有 Logging Provider。
//builder.Logging.AddConsole();
// 以下兩項是常見的
Debug.WriteLine("--- Microsoft.Hosting.Lifetime --- ");
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
Debug.WriteLine("--- Microsoft.EntityFrameworkCore.Database.Command --- ");
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

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

=============
Warring 產生
Microsoft.EntityFrameworkCore.Model.Validation: Warning: No store type was specified for the decimal property 'Scope1Emission' on entity type 'CompanyEmission'. This will cause values to be silently truncated if they do not fit in the default precision and scale. Explicitly specify the SQL server column type that can accommodate all the values in 'OnModelCreating' using 'HasColumnType', specify precision and scale using 'HasPrecision', or configure a value converter using 'HasConversion'.
* 實體（CompanyEmission）的 decimal 欄位（例如 Scope1Emission）沒有指定精度（precision）與小數位數（scale）

### 🧾 原因說明

EF Core 在沒有指定精度的情況下，會自動使用預設的 decimal(18,2)。
但如果你的實際數值超出範圍（例如 999999999999999.999），
就會「靜默截斷（silently truncated）」成符合 (18,2) 的範圍，導致資料損失。

---
處理

public class CompanyEmission
{
    public int Id { get; set; }

    [Precision(18, 4)] // EF Core 6 以上支援 <= 直接加在 Model 中修正
    public decimal Scope1Emission { get; set; }

    [Precision(18, 4)]
    public decimal Scope2Emission { get; set; }
}
============