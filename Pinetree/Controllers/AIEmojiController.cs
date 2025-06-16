using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Services;
using System.Security.Claims;

namespace Pinetree.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AIEmojiController(AIEmojiWithRateLimitService aiEmojiService) : ControllerBase
{
    private readonly AIEmojiWithRateLimitService _aiEmojiService = aiEmojiService;

    [HttpPost("suggest")]
    public async Task<IActionResult> SuggestEmojiAsync([FromBody] SuggestEmojiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text is required");
        }

        // Get user ID for rate limiting
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var result = await _aiEmojiService.GetEmojiForTextAsync(request.Text, userId ?? string.Empty, ipAddress);
            
            if (!result.IsSuccess && result.RateLimitResult != null)
            {
                var rateLimitResult = result.RateLimitResult;
                var errorMessage = rateLimitResult.LimitType == "per minute" 
                    ? $"AI Emoji feature is limited to {rateLimitResult.MaxRequests} requests per minute. You have used {rateLimitResult.CurrentRequests} requests. Please wait {rateLimitResult.WaitTimeSeconds} seconds before trying again."
                    : $"AI Emoji feature is limited to {rateLimitResult.MaxRequests} requests per 10 minutes. You have used {rateLimitResult.CurrentRequests} requests. Please wait {Math.Ceiling(rateLimitResult.WaitTimeSeconds / 60.0)} minutes before trying again.";
                
                return StatusCode(429, errorMessage);
            }

            return Ok(new SuggestEmojiResponse { Emoji = result.Emoji });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error suggesting emoji: {ex.Message}");
        }
    }
}

public class SuggestEmojiRequest
{
    public string Text { get; set; } = string.Empty;
}

public class SuggestEmojiResponse
{
    public string Emoji { get; set; } = string.Empty;
}
