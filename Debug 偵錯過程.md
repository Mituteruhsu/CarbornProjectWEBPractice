2025/11/11
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