using CarbonProject.Data;
using CarbonProject.Models.EFModels;
using CarbonProject.Models.JSONModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

public class CarbonFactorImportService
{
    private readonly HttpClient _http;
    private readonly ILogger<CarbonFactorImportService> _logger;
    private readonly CarbonDbContext _context;
    
    private const string BaseUrl = "https://data.moenv.gov.tw/api/v2";

    public CarbonFactorImportService(HttpClient http, ILogger<CarbonFactorImportService> logger, CarbonDbContext context)
    {
        _http = http; 
        _logger = logger;
        _context = context;
    }

    // 取得環境部資歷集 (API)
    public async Task<List<CarbonFactorRecord>> FetchDataset(
    string dataset,
    int offset,
    int limit,
    string apiKey,
    string format = "json")
    {
        var url = $"{BaseUrl}/{dataset}?format={format}&offset={offset}&limit={limit}&api_key={apiKey}";
        _logger.LogInformation($"[Moenv API] Fetching: {url}");

        try
        {
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API 回應錯誤: {response.StatusCode}");
                return new List<CarbonFactorRecord>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CarbonFactorApiResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (result == null || result.Records == null)
            {
                _logger.LogWarning("API 回傳 null 或 records 為 null");
                return new List<CarbonFactorRecord>();
            }

            return result.Records;
        }
        catch (Exception ex)
        {
            _logger.LogError($"API 讀取異常: {ex.Message}");
            return new List<CarbonFactorRecord>();
        }
    }
    // 下載資料集（支援分頁）
    public async Task<List<CarbonFactorRecord>> FetchAll(string dataset, string apiKey, int pageSize = 1000)
    {
        int offset = 0;
        var all = new List<CarbonFactorRecord>();

        while (true)
        {
            var batch = await FetchDataset(dataset, offset, pageSize, apiKey);
            //Debug.WriteLine($"FetchAll-batch:{batch}");
            if (batch.Count == 0) break;

            all.AddRange(batch);
            Debug.WriteLine($"FetchAll-batch.Count:{batch.Count}");
            if (batch.Count < pageSize) break;
            offset += pageSize;
        }
        Debug.WriteLine($"FetchAll-all:{all}");
        return all;
    }
    // 自動/手動同步資料到 DB
    public async Task<int> SyncToDb(List<CarbonFactorRecord> factors)
    {
        int newCount = 0;

        foreach (var dto in factors)
        {
            // 轉換 Coe 字串為 decimal
            decimal coeValue = 0;
            if (!decimal.TryParse(dto.Coe, out coeValue))
            {
                _logger.LogWarning($"無法解析 Coe: '{dto.Coe}' for {dto.Name}, 設為 0");
            }

            // 檢查是否已存在相同名稱 + 年份
            bool exists = await _context.CarbonFactors
                .AnyAsync(c => c.Name == dto.Name && c.AnnouncementYear == int.Parse(dto.AnnouncementYear));

            if (!exists)
            {
                _context.CarbonFactors.Add(new CarbonFactor
                {
                    Name = dto.Name,
                    Coe = coeValue,
                    Unit = dto.Unit,
                    DepartmentName = dto.DepartmentName,
                    AnnouncementYear = int.Parse(dto.AnnouncementYear)
                });
                newCount++;
            }
        }

        if (newCount > 0)
            await _context.SaveChangesAsync();

        _logger.LogInformation($"CarbonFactor 同步完成，新增 {newCount} 筆資料");
        return newCount;
    }
    // 取得最新公告年份的資料清單
    public async Task<List<CarbonFactor>> GetAllFactors()
    {
        // 先確認表中有資料
        if (!await _context.CarbonFactors.AnyAsync())
        {
            return new List<CarbonFactor>();
        }
        return await _context.CarbonFactors
            .OrderByDescending(c => c.AnnouncementYear)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }
}

// 泛型用於序列化 API 回傳 JSON
public class MoenvApiResponse<T>
{
    public List<T> Records { get; set; } = new List<T>();
}