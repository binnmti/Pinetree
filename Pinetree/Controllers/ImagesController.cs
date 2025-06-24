using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Services;
using System.Security.Claims;

namespace Pinetree.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImagesController : ControllerBase
{
    private readonly BlobStorageService _blobStorageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public ImagesController(BlobStorageService blobStorageService, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
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

        var result = await _blobStorageService.DeleteBlobAsync(id, userName);
        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }

    [HttpPost("upload-icon")]
    public async Task<IActionResult> UploadUserIcon(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Invalid file type. Only JPEG, PNG, and GIF files are allowed.");        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("File size exceeds 5MB limit.");

        try
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }

            // Check storage quota
            var usage = await _blobStorageService.GetUserStorageUsageAsync(userName);
            if (usage.TotalSizeInBytes + file.Length > usage.QuotaInBytes)
            {
                return BadRequest(new { error = "Storage quota exceeded" });
            }

            using var stream = file.OpenReadStream();
            var extension = Path.GetExtension(file.FileName);
            
            // Use existing UploadImageAsync with a special GUID for profile icons
            var profileIconGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var imageUrl = await _blobStorageService.UploadImageAsync(stream, extension, userName, profileIconGuid);
            
            // Update user profile
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.ProfileIconUrl = imageUrl;
                await _userManager.UpdateAsync(user);
            }
            
            return Ok(new { url = imageUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error uploading file: {ex.Message}");
        }
    }

    [HttpGet("user-icon/{userId}")]
    public async Task<IActionResult> GetUserIcon(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.ProfileIconUrl == null)
            return NotFound();
          return Ok(new { url = user.ProfileIconUrl });
    }

    [HttpDelete("user-icon")]
    public async Task<IActionResult> DeleteUserIcon()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.ProfileIconUrl != null)
            {
                var userName = User.Identity?.Name;
                if (!string.IsNullOrEmpty(userName))
                {
                    // Find the blob info for the profile icon
                    // Profile icons use a special GUID: 00000000-0000-0000-0000-000000000001
                    var profileIconGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
                    var blobInfo = await _dbContext.UserBlobInfos
                        .FirstOrDefaultAsync(b => b.UserName == userName && 
                                                 b.PineconeGuid == profileIconGuid && 
                                                 !b.IsDeleted);
                    
                    if (blobInfo != null)
                    {
                        await _blobStorageService.DeleteBlobAsync(blobInfo.Id, userName);
                    }
                }
                
                // Update user profile
                user.ProfileIconUrl = null;
                await _userManager.UpdateAsync(user);
            }
            
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting icon: {ex.Message}");
        }
    }
}
