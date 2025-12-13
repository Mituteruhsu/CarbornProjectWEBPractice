using CarbonProject.Models.EFModels;
using CarbonProject.Models.Request;
using System.Diagnostics;

public class CarbonCalculationService
{
    private readonly CarbonCalculationRepository _repo;

    public CarbonCalculationService(CarbonCalculationRepository repo)
    {
        _repo = repo;
    }

    public async Task<int> SaveBatchAsync(int userId, string role, List<SaveCarbonRequest> records, string BatchName)
    {
        // 建立批次
        Debug.WriteLine("===== CarbonCalculationService.cs =====");
        var today = DateTime.Today;
        int todayCount = await _repo.GetBatchCountByDateAsync(userId, today);
        var batch = new CarbonCalculationBatch
        {
            UserId = userId,
            RoleAtCalculation = role,
            CalculationName = $"{today:yyyy-MM-dd} 第{todayCount + 1}筆碳計算",
            TotalResultValue = records.Sum(r => r.Emission),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddBatchAsync(batch);
        await _repo.SaveChangesAsync(); // 先儲存 batch，才能拿到 batch.Id
        Debug.WriteLine($"Here is your BatchId:{batch.Id}");
        // 儲存每筆明細
        foreach (var item in records)
        {            
            var factorEntity = await _repo.GetFactorByNameAsync(item.Name);
            var calc = new CarbonCalculation
            {
                UserId = userId,
                BatchId = batch.Id,
                FactorId = factorEntity?.Id ?? 0,
                InputValue = item.Usage,
                ResultValue = item.Emission,
                CreatedAt = DateTime.UtcNow
            };
            Debug.WriteLine($"Here is your UserId:{userId}");
            Debug.WriteLine($"Here is your BatchId:{batch.Id}");
            Debug.WriteLine($"Here is your FactorId:{factorEntity?.Id ?? 0}");
            Debug.WriteLine($"Here is your InputValue:{item.Usage}");
            Debug.WriteLine($"Here is your ResultValue:{item.Emission}");
            await _repo.AddCalculationAsync(calc);
        }

        await _repo.SaveChangesAsync();

        return records.Count;
    }
}
