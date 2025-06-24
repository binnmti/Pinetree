using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Data;
using Pinetree.Services;
using Pinetree.Shared.ViewModels;

namespace Pinetree.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly BlobStorageService _blobStorageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public ImagesController(
        BlobStorageService blobStorageService, 
        UserManager<ApplicationUser> userManager, 
        ApplicationDbContext dbContext)
    {
        _blobStorageService = blobStorageService;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromQuery] string extension, [FromQuery] Guid pineconeGuid)
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

            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }

            var usage = await _blobStorageService.GetUserStorageUsageAsync(userName);
            if (usage.TotalSizeInBytes + bytes.Length > usage.QuotaInBytes)
            {
                return BadRequest(new { error = "Storage quota exceeded" });
            }
            
            var url = await _blobStorageService.UploadImageAsync(stream, extension, userName, pineconeGuid);
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
        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            return Unauthorized();
        }

        var images = await _blobStorageService.GetUserBlobViewModelsAsync(userName);

        return Ok(images);
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetStorageUsage()
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            return Unauthorized();
        }

        var usage = await _blobStorageService.GetUserStorageUsageViewModelAsync(userName);

        return Ok(new
        {
            used = usage.TotalSizeInBytes,
            quota = usage.QuotaInBytes,
            percentage = usage.UsagePercentage,
            remaining = usage.RemainingBytes,
            isOverQuota = usage.IsOverQuota
        });
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            return Unauthorized();
        }

        // Delete the blob and check if it was a profile icon
        var (success, wasProfileIcon) = await _blobStorageService.DeleteBlobWithProfileCheckAsync(id, userName);
        if (!success)
        {
            return NotFound();
        }

        // If this was the profile icon, clear it from the user profile
        if (wasProfileIcon)
        {
            await _blobStorageService.ClearUserProfileIconAsync(userName);
        }

        return Ok();
    }

    [HttpGet("user-profile-icon/{userName}")]
    [AllowAnonymous] // Public view should be accessible without authentication
    public async Task<IActionResult> GetUserProfileIcon(string userName)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);            if (user == null)
            {
                return NotFound(new UserProfileIconViewModel { Url = null });
            }

            return Ok(new UserProfileIconViewModel { Url = user.ProfileIconUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error retrieving profile icon", error = ex.Message });
        }
    }
}
