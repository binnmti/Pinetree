using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Constants;
using Pinetree.Services;

namespace Pinetree.Components.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(BlobStorageService blobStorageService, ILogger<ImagesController> logger) : ControllerBase
{
    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> Upload(string extension, [FromBody] string base64Image)
    {
        try
        {
            if (string.IsNullOrEmpty(base64Image))
            {
                return BadRequest(new { Error = "No image data provided" });
            }

            var fileExtension = extension.StartsWith('.') ? extension : $".{extension}";
            if (!ImageConstants.IsAllowedExtension(fileExtension))
            {
                return BadRequest(new { Error = $"Unsupported file format. Supported formats: {string.Join(", ", ImageConstants.AllowedExtensions)}" });
            }

            byte[] imageBytes = Convert.FromBase64String(base64Image);

            if (imageBytes.Length > ImageConstants.MaxFileSizeBytes)
            {
                return BadRequest(new { Error = $"File size is too large. Please keep files under {ImageConstants.MaxFileSizeMB}MB" });
            }

            using var stream = new MemoryStream(imageBytes);

            var blobUrl = await blobStorageService.UploadImageAsync(stream, fileExtension);

            logger.LogInformation("Image uploaded from IndexedDB: {BlobUrl}", blobUrl);

            return Ok(new { Url = blobUrl });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image from IndexedDB");
            return StatusCode(500, new { Error = "An error occurred while uploading the image" });
        }
    }
}
