using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Services;
using System.Security.Claims;

namespace Pinetree.Components.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController(BlobStorageService blobStorageService) : ControllerBase
{
    private readonly BlobStorageService _blobStorageService = blobStorageService;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromQuery] string extension)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var jsonContent = await reader.ReadToEndAsync();

            var base64String = System.Text.Json.JsonSerializer.Deserialize<string>(jsonContent)
                ?? throw new ArgumentNullException("Base64 string is null");
            if (base64String.Contains(','))
            {
                base64String = base64String.Split(',')[1];
            }
            var bytes = Convert.FromBase64String(base64String);
            var stream = new MemoryStream(bytes);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var usage = await _blobStorageService.GetUserStorageUsageAsync(userId);
            if (usage.TotalSizeInBytes + bytes.Length > usage.QuotaInBytes)
            {
                return BadRequest(new { error = "Storage quota exceeded" });
            }

            var url = await _blobStorageService.UploadImageAsync(stream, extension, userId);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error uploading image: {ex.Message}" });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListUserImages()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var images = await _blobStorageService.GetUserBlobsAsync(userId);

        return Ok(images);
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetStorageUsage()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var usage = await _blobStorageService.GetUserStorageUsageAsync(userId);

        return Ok(new
        {
            used = usage.TotalSizeInBytes,
            quota = usage.QuotaInBytes,
            percentage = (double)usage.TotalSizeInBytes / usage.QuotaInBytes * 100
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _blobStorageService.DeleteBlobAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }
}
