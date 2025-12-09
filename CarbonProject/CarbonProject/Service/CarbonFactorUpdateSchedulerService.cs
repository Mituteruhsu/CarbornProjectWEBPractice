using System.Diagnostics;

public class CarbonFactorUpdateScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CarbonFactorUpdateScheduler> _logger;

    public CarbonFactorUpdateScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<CarbonFactorUpdateScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var importService = scope.ServiceProvider.GetRequiredService<CarbonFactorImportService>();
                    Debug.WriteLine($"CarbonFactor 自動排程開始");
                    _logger.LogInformation("CarbonFactor 自動排程開始");

                    var factors = await importService.FetchAll("cfp_p_02", "e9370020-f106-4efc-8521-a9cef11b10aa");
                    await importService.SyncToDb(factors);

                    _logger.LogInformation("CarbonFactor 自動排程完成");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"CarbonFactor 排程異常: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
