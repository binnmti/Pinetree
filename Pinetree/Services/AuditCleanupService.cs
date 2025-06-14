using Microsoft.EntityFrameworkCore;
using Pinetree.Data;

namespace Pinetree.Services;

public class AuditCleanupService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<AuditCleanupService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<AuditCleanupService> _logger = logger;
    private readonly int _retentionDays = configuration.GetValue("AuditSettingsRetentionDays", 1095);
    private readonly int _cleanupIntervalHours = configuration.GetValue("AuditSettingsCleanupIntervalHours", 24);
    private readonly bool _autoCleanupEnabled = configuration.GetValue("AuditSettingsAutoCleanup", true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_autoCleanupEnabled)
        {
            _logger.LogInformation("Audit log auto-cleanup is disabled");
            return;
        }

        _logger.LogInformation("Audit cleanup service started. Retention: {Days} days, Interval: {Hours} hours", 
            _retentionDays, _cleanupIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync();
                await Task.Delay(TimeSpan.FromHours(_cleanupIntervalHours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit cleanup service");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Wait 30 minutes before retry
            }
        }
    }

    private async Task CleanupOldLogsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
            
            // Delete in batches to avoid large transactions
            int batchSize = 1000;
            int totalDeleted = 0;
            
            while (true)
            {
                var oldLogs = await context.AuditLogs
                    .Where(log => log.Timestamp < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (!oldLogs.Any())
                    break;

                context.AuditLogs.RemoveRange(oldLogs);
                await context.SaveChangesAsync();
                
                totalDeleted += oldLogs.Count;
                
                _logger.LogDebug("Deleted batch of {Count} audit logs", oldLogs.Count);
                
                // If we got less than batch size, we're done
                if (oldLogs.Count < batchSize)
                    break;
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation("Cleaned up {Count} audit logs older than {Days} days", 
                    totalDeleted, _retentionDays);
            }
            else
            {
                _logger.LogDebug("No audit logs to clean up");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cleaning up audit logs");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit cleanup service is stopping");
        return base.StopAsync(cancellationToken);
    }
}
