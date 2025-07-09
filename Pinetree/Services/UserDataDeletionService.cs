using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Pinetree.Data;
using Pinetree.Models;
using Azure.Storage.Blobs;

namespace Pinetree.Services;

public class UserDataDeletionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<UserDataDeletionService> _logger;
    private readonly string _containerName = "images";

    public UserDataDeletionService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<UserDataDeletionService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        
        var connectionString = configuration.GetConnectionString("AzureStorage");
        if (!string.IsNullOrEmpty(connectionString))
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
    }

    public async Task<bool> DeleteAllUserDataAsync(ApplicationUser user)
    {
        var userName = user.UserName;
        var userId = user.Id;

        try
        {
            _logger.LogInformation("Starting comprehensive data deletion for user: {UserName} (ID: {UserId})", userName, userId);

            // Delete user's blob files from Azure Storage
            await DeleteUserBlobsFromStorageAsync(userName);

            // Delete user's blob info records from database
            await DeleteUserBlobInfosAsync(userName);

            // Delete user's storage usage records
            await DeleteUserStorageUsageAsync(userName);

            // Delete user's Pinecone content
            await DeleteUserPineconeContentAsync(userName);

            // Delete user's audit logs
            await DeleteUserAuditLogsAsync(userId, userName);

            // Finally, delete the user account from Identity
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to delete user account: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            // Sign out the user
            await _signInManager.SignOutAsync();

            _logger.LogInformation("Successfully completed comprehensive data deletion for user: {UserName} (ID: {UserId})", userName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during comprehensive data deletion for user: {UserName} (ID: {UserId})", userName, userId);
            return false;
        }
    }

    private async Task DeleteUserBlobsFromStorageAsync(string userName)
    {
        if (_blobServiceClient == null)
        {
            _logger.LogWarning("BlobServiceClient is not configured. Skipping blob storage deletion.");
            return;
        }

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var folderPath = $"{userName}/";

            // List all blobs in the user's folder
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: folderPath))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                await blobClient.DeleteIfExistsAsync();
                _logger.LogInformation("Deleted blob: {BlobName}", blobItem.Name);
            }

            _logger.LogInformation("Completed blob storage deletion for user: {UserName}", userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blobs from storage for user: {UserName}", userName);
            throw;
        }
    }

    private async Task DeleteUserBlobInfosAsync(string userName)
    {
        try
        {
            var blobInfos = await _dbContext.UserBlobInfos
                .Where(b => b.UserName == userName)
                .ToListAsync();

            if (blobInfos.Any())
            {
                _dbContext.UserBlobInfos.RemoveRange(blobInfos);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} blob info records for user: {UserName}", blobInfos.Count, userName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob info records for user: {UserName}", userName);
            throw;
        }
    }

    private async Task DeleteUserStorageUsageAsync(string userName)
    {
        try
        {
            var storageUsages = await _dbContext.UserStorageUsages
                .Where(u => u.UserName == userName)
                .ToListAsync();

            if (storageUsages.Any())
            {
                _dbContext.UserStorageUsages.RemoveRange(storageUsages);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} storage usage records for user: {UserName}", storageUsages.Count, userName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting storage usage records for user: {UserName}", userName);
            throw;
        }
    }

    private async Task DeleteUserPineconeContentAsync(string userName)
    {
        try
        {
            var pinecones = await _dbContext.Pinecone
                .Where(p => p.UserName == userName)
                .ToListAsync();

            if (pinecones.Any())
            {
                _dbContext.Pinecone.RemoveRange(pinecones);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} Pinecone content records for user: {UserName}", pinecones.Count, userName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Pinecone content for user: {UserName}", userName);
            throw;
        }
    }

    private async Task DeleteUserAuditLogsAsync(string userId, string userName)
    {
        try
        {
            var auditLogs = await _dbContext.AuditLogs
                .Where(a => a.UserId == userId || a.UserName == userName)
                .ToListAsync();

            if (auditLogs.Any())
            {
                _dbContext.AuditLogs.RemoveRange(auditLogs);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} audit log records for user: {UserName}", auditLogs.Count, userName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting audit logs for user: {UserName}", userName);
            throw;
        }
    }
}