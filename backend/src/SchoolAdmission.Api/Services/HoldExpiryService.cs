namespace SchoolAdmission.Api.Services;

/// <summary>
/// Persists EXPIRED for overdue holds once per second. Seat counts and hold reads already
/// ignore overdue holds, so this keeps stored data tidy rather than guarding correctness.
/// </summary>
/// 
public class HoldExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<HoldExpiryService> _log;

    public HoldExpiryService(IServiceScopeFactory scopes, ILogger<HoldExpiryService> log)
    {
        _scopes = scopes;
        _log = log;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<AdmissionService>();
                    var n = await svc.ExpireOverdueAsync(stoppingToken);
                    if (n > 0) _log.LogInformation("Expired {Count} overdue hold(s)", n);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "Expiry sweep failed; will retry");
                }
            }
        }

        
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}