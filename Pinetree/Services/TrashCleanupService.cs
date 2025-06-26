using Microsoft.EntityFrameworkCore;
using Pinetree.Data;

namespace Pinetree.Services;

public class TrashCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrashCleanupService> _logger;

    public TrashCleanupService(IServiceScopeFactory scopeFactory, ILogger<TrashCleanupService> logger)
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
                await CleanupExpiredTrashFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up trash files");
            }

            // Wait 24 hours before next cleanup
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupExpiredTrashFiles()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        var expiredFiles = await context.Pinecone
            .Where(p => p.IsDeleted && p.DeletedAt.HasValue && p.DeletedAt.Value < cutoffDate)
            .ToListAsync();

        if (expiredFiles.Count > 0)
        {
            _logger.LogInformation("Found {Count} expired files to permanently delete", expiredFiles.Count);
            
            foreach (var file in expiredFiles)
            {
                // Remove all children recursively
                await RemoveChildrenFromDatabaseAsync(context, file);
                context.Pinecone.Remove(file);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted {Count} expired files from trash", expiredFiles.Count);
        }
    }

    private async Task RemoveChildrenFromDatabaseAsync(ApplicationDbContext context, Models.Pinecone parent)
    {
        await context.Entry(parent)
            .Collection(p => p.Children)
            .LoadAsync();

        var children = parent.Children.ToList();

        foreach (var child in children)
        {
            await RemoveChildrenFromDatabaseAsync(context, child);
            context.Pinecone.Remove(child);
        }
    }
}
