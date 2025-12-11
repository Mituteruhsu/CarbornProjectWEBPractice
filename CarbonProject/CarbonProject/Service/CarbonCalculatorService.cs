using CarbonProject.Models.EFModels;

public class CarbonCalculationService
{
    private readonly CarbonCalculationRepository _repo;

    public CarbonCalculationService(CarbonCalculationRepository repo)
    {
        _repo = repo;
    }

    public async Task SaveRecordAsync(
        int userId,
        string name,
        decimal inputValue,
        decimal factor,
        decimal resultValue,
        string role)
    {
        // 依名稱找 FactorId
        var factorEntity = await _repo.GetFactorByNameAsync(name);

        var calc = new CarbonCalculation
        {
            UserId = userId,
            FactorId = factorEntity?.Id ?? 0,
            InputValue = inputValue,
            ResultValue = resultValue,
            RoleAtCalculation = role,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(calc);
    }
}
