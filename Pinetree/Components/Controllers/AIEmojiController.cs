using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Services;
using System.Security.Claims;

namespace Pinetree.Components.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AIEmojiController : ControllerBase
    {
        private readonly AIEmojiService _aiEmojiService;
        private readonly RateLimitService _rateLimitService;

        public AIEmojiController(AIEmojiService aiEmojiService, RateLimitService rateLimitService)
        {
            _aiEmojiService = aiEmojiService;
            _rateLimitService = rateLimitService;
        }

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

            // Check rate limit with detailed information
            var rateLimitResult = _rateLimitService.CheckRateLimit(userId ?? string.Empty, ipAddress);
            if (!rateLimitResult.IsAllowed)
            {
                var errorMessage = rateLimitResult.LimitType == "per minute" 
                    ? $"AI Emoji feature is limited to {rateLimitResult.MaxRequests} requests per minute. You have used {rateLimitResult.CurrentRequests} requests. Please wait {rateLimitResult.WaitTimeSeconds} seconds before trying again."
                    : $"AI Emoji feature is limited to {rateLimitResult.MaxRequests} requests per 10 minutes. You have used {rateLimitResult.CurrentRequests} requests. Please wait {Math.Ceiling(rateLimitResult.WaitTimeSeconds / 60.0)} minutes before trying again.";
                
                return StatusCode(429, errorMessage);
            }

            try
            {
                var emoji = await _aiEmojiService.GetEmojiForTextAsync(request.Text);
                return Ok(new SuggestEmojiResponse { Emoji = emoji });
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
}
