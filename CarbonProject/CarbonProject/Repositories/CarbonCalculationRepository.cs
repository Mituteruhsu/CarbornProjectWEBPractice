using CarbonProject.Data;
using CarbonProject.Models.EFModels;
using Microsoft.EntityFrameworkCore;

public class CarbonCalculationRepository
{
    private readonly CarbonDbContext _db;

    public CarbonCalculationRepository(CarbonDbContext db)
    {
        _db = db;
    }
    // =========================
    // CarbonFactor
    // =========================
    public async Task<CarbonFactor?> GetFactorByNameAsync(string name)
    {
        return await _db.CarbonFactors
            .FirstOrDefaultAsync(f => f.Name == name);
    }
    // =========================
    // CarbonCalculationBatch
    // =========================
    public async Task AddBatchAsync(CarbonCalculationBatch batch)
    {
        await _db.CarbonCalculationBatches.AddAsync(batch);
        await _db.SaveChangesAsync();
    }
    public async Task<int> GetBatchCountByDateAsync(int userId, DateTime date)
    {
        return await _db.CarbonCalculationBatches
            .Where(b => b.UserId == userId && b.CreatedAt.Date == date.Date)
            .CountAsync();
    }
    
    public async Task<CarbonCalculationBatch?> GetBatchByIdAsync(int batchId)
    {
        return await _db.CarbonCalculationBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);
    }
    // =========================
    // CarbonCalculation
    // =========================
    public async Task AddCalculationAsync(CarbonCalculation calc)
    {
        await _db.CarbonCalculations.AddAsync(calc);
        await _db.SaveChangesAsync();
    }

    public async Task<List<CarbonCalculation>> GetCalculationsByBatchAsync(int batchId)
    {
        return await _db.CarbonCalculations
            .Where(c => c.BatchId == batchId)
            .ToListAsync();
    }
    // =========================
    // SaveChanges
    // =========================
    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}