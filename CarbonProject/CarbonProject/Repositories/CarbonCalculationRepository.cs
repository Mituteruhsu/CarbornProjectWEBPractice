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

    public async Task AddAsync(CarbonCalculation calc)
    {
        await _db.Set<CarbonCalculation>().AddAsync(calc);
        await _db.SaveChangesAsync();
    }

    public async Task<CarbonFactor?> GetFactorByNameAsync(string name)
    {
        return await _db.CarbonFactors
            .FirstOrDefaultAsync(f => f.Name == name);
    }
}
