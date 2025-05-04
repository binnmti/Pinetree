using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Pinetree.Constants;
using Pinetree.Data;
using Pinetree.Shared.Model;

namespace Pinetree.Services;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "images";
    private readonly ApplicationDbContext _dbContext;
    private const int DefaultQuotaInBytes = 1024 * 1024 * 100;

    public BlobStorageService(IConfiguration configuration, ApplicationDbContext dbContext)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage")
                            ?? throw new ArgumentNullException("Azure Storage connection string is missing from configuration");

        _blobServiceClient = new BlobServiceClient(connectionString);
        _dbContext = dbContext;
    }

    public async Task<string> UploadImageAsync(Stream content, string fileExtension, string userId)
    {
        if (content.CanSeek && content.Position != 0)
        {
            content.Position = 0;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = ImageConstants.GetContentType(fileExtension)
            }
        };

        long fileSize = content.Length;

        await blobClient.UploadAsync(content, options);

        var blobInfo = new UserBlobInfo
        {
            UserId = userId,
            BlobUrl = blobClient.Uri.ToString(),
            BlobName = uniqueFileName,
            SizeInBytes = fileSize,
            ContentType = ImageConstants.GetContentType(fileExtension),
            UploadedAt = DateTime.UtcNow
        };

        _dbContext.UserBlobInfos.Add(blobInfo);

        var usage = await _dbContext.UserStorageUsages.SingleOrDefaultAsync(x => x.UserId == userId);
        if (usage == null)
        {
            usage = new UserStorageUsage
            {
                UserId = userId,
                TotalSizeInBytes = fileSize,
                QuotaInBytes = DefaultQuotaInBytes,
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.UserStorageUsages.Add(usage);
        }
        else
        {
            usage.TotalSizeInBytes += fileSize;
            usage.LastUpdated = DateTime.UtcNow;
            _dbContext.UserStorageUsages.Update(usage);
        }

        await _dbContext.SaveChangesAsync();

        return blobClient.Uri.ToString();
    }

    public async Task<List<UserBlobInfo>> GetUserBlobsAsync(string userId)
        => await _dbContext.UserBlobInfos
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.UploadedAt)
            .ToListAsync();

    public async Task<bool> DeleteBlobAsync(int blobInfoId, string userId)
    {
        var blobInfo = await _dbContext.UserBlobInfos
            .FirstOrDefaultAsync(b => b.Id == blobInfoId && b.UserId == userId);

        if (blobInfo == null)
        {
            return false;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobInfo.BlobName);
        await blobClient.DeleteIfExistsAsync();

        var usage = await _dbContext.UserStorageUsages.SingleOrDefaultAsync(x => x.UserId == userId);
        if (usage != null)
        {
            usage.TotalSizeInBytes -= blobInfo.SizeInBytes;
            usage.LastUpdated = DateTime.UtcNow;
            _dbContext.UserStorageUsages.Update(usage);
        }
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<UserStorageUsage> GetUserStorageUsageAsync(string userId)
    {
        var usage = await _dbContext.UserStorageUsages.SingleOrDefaultAsync(x => x.UserId == userId);
        // TODO: Ensure creation at creation time, not acquisition time. Currently there is no creation time.
        if (usage == null)
        {
            usage = new UserStorageUsage
            {
                UserId = userId,
                TotalSizeInBytes = 0,
                QuotaInBytes = DefaultQuotaInBytes,
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.UserStorageUsages.Add(usage);
            await _dbContext.SaveChangesAsync();
        }
        return usage;
    }
}
