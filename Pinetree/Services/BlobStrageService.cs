using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Pinetree.Constants;

namespace Pinetree.Services;

public class BlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "images";

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage")
                            ?? throw new ArgumentNullException("Azure Storage connection string is missing from configuration");

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadImageAsync(Stream content, string fileExtension)
    {
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

        await blobClient.UploadAsync(content, options);

        return blobClient.Uri.ToString();
    }
}
