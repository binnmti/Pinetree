using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Models;
using Pinetree.Shared.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Pinetree.Services;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "images";
    private readonly ApplicationDbContext _dbContext;
    private readonly EncryptionService _encryptionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private const int DefaultQuotaInBytes = 1024 * 1024 * 3;
    
    public BlobStorageService(IConfiguration configuration, ApplicationDbContext dbContext, EncryptionService encryptionService, UserManager<ApplicationUser> userManager)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage")
                            ?? throw new ArgumentNullException("Azure Storage connection string is missing from configuration");

        _blobServiceClient = new BlobServiceClient(connectionString);
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _userManager = userManager;
    }

    public async Task<string> UploadImageAsync(Stream content, string fileExtension, string userName, Guid pineconeGuid)
    {
        if (content.CanSeek && content.Position != 0)
        {
            content.Position = 0;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var folderPath = $"{userName}/";
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var fullBlobPath = $"{folderPath}{uniqueFileName}";
        var blobClient = containerClient.GetBlobClient(fullBlobPath);

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
            UserName = userName,
            BlobName = uniqueFileName,
            SizeInBytes = fileSize,
            ContentType = ImageConstants.GetContentType(fileExtension),
            UploadedAt = DateTime.UtcNow,
            PineconeGuid = pineconeGuid
        };

        _dbContext.UserBlobInfos.Add(blobInfo);

        var usage = await _dbContext.UserStorageUsages
                                    .SingleOrDefaultAsync(x => x.UserName == userName);
        if (usage == null)
        {
            usage = new UserStorageUsage
            {
                UserName = userName,
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

    public async Task<bool> DeleteBlobAsync(int blobInfoId, string userName)
    {
        var blobInfo = await _dbContext.UserBlobInfos
            .FirstOrDefaultAsync(b => b.Id == blobInfoId && b.UserName == userName);

        if (blobInfo == null || blobInfo.IsDeleted)
        {
            return false;
        }

        blobInfo.IsDeleted = true;
        blobInfo.DeletedAt = DateTime.UtcNow;

        var usage = await _dbContext.UserStorageUsages.SingleOrDefaultAsync(x => x.UserName == userName);
        if (usage != null)
        {
            usage.TotalSizeInBytes -= blobInfo.SizeInBytes;
            usage.LastUpdated = DateTime.UtcNow;
            _dbContext.UserStorageUsages.Update(usage);
        }

        _dbContext.UserBlobInfos.Update(blobInfo);
        await _dbContext.SaveChangesAsync();

        return true;
    }
    
    public async Task<UserStorageUsage> GetUserStorageUsageAsync(string userName)
    {
        var usage = await _dbContext.UserStorageUsages.SingleOrDefaultAsync(x => x.UserName == userName);
        // TODO: Ensure creation at creation time, not acquisition time. Currently there is no creation time.
        if (usage == null)
        {
            usage = new UserStorageUsage
            {
                UserName = userName,
                TotalSizeInBytes = 0,
                QuotaInBytes = DefaultQuotaInBytes,
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.UserStorageUsages.Add(usage);
            await _dbContext.SaveChangesAsync();
        }
        return usage;
    }

    public async Task<UserStorageUsageViewModel> GetUserStorageUsageViewModelAsync(string userName)
    {
        var usage = await GetUserStorageUsageAsync(userName);
        return ToUserStorageUsageViewModel(usage);
    }

    private string GenerateBlobUrl(string userName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var folderPath = $"{userName}/";
        var fullBlobPath = $"{folderPath}{blobName}";
        var blobClient = containerClient.GetBlobClient(fullBlobPath);
        return blobClient.Uri.ToString();
    }    
    
    public async Task<List<UserBlobViewModel>> GetUserBlobViewModelsAsync(string userName)
    {
        // Get user ID for decryption
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            throw new InvalidOperationException($"User not found: {userName}");
        }
        
        var results = await _dbContext.UserBlobInfos
            .Where(b => b.UserName == userName && !b.IsDeleted)
            .OrderByDescending(b => b.UploadedAt)
            .GroupJoin(
                _dbContext.Pinecone,
                blob => blob.PineconeGuid,
                pinecone => pinecone.Guid,
                (blob, pinecones) => new { Blob = blob, Pinecones = pinecones }
            )
            .SelectMany(
                x => x.Pinecones.DefaultIfEmpty(),
                (x, pinecone) => new
                {
                    x.Blob.Id,
                    x.Blob.UserName,
                    x.Blob.BlobName,
                    x.Blob.SizeInBytes,
                    x.Blob.ContentType,
                    x.Blob.PineconeGuid,
                    PineconeTitle = pinecone != null ? pinecone.Title : "",
                    PineconeIsPublic = pinecone != null ? pinecone.IsPublic : true,
                    x.Blob.UploadedAt
                }
            )
            .ToListAsync();
        
        var viewModels = new List<UserBlobViewModel>();
        
        foreach (var item in results)
        {
            string decryptedTitle = "";
            if (!string.IsNullOrEmpty(item.PineconeTitle))
            {
                try
                {
                    decryptedTitle = await _encryptionService.DecryptContentAsync(
                        item.PineconeTitle, 
                        item.PineconeIsPublic, 
                        user.Id) ?? "";
                }
                catch (Exception)
                {
                    // If decryption fails, use empty string
                    decryptedTitle = "";
                }
            }
            
            viewModels.Add(new UserBlobViewModel
            {
                Id = item.Id,
                BlobUrl = GenerateBlobUrl(item.UserName, item.BlobName),
                FileName = item.BlobName,
                SizeInBytes = item.SizeInBytes,
                ContentType = item.ContentType,
                PineconeGuid = item.PineconeGuid,
                PineconeTitle = decryptedTitle,
                UploadedAt = item.UploadedAt
            });
        }
        
        return viewModels;
    }

    // Model to ViewModel conversion methods
    private static UserStorageUsageViewModel ToUserStorageUsageViewModel(UserStorageUsage model)
    {
        return new UserStorageUsageViewModel
        {
            UserName = model.UserName,
            TotalSizeInBytes = model.TotalSizeInBytes,
            QuotaInBytes = model.QuotaInBytes,
            LastUpdated = model.LastUpdated
        };
    }
}
