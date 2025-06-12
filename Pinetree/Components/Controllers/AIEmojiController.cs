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
        public IActionResult SuggestEmoji([FromBody] SuggestEmojiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Text is required");
            }

            // Get user ID for rate limiting
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Check rate limit
            if (!_rateLimitService.IsAllowed(userId ?? string.Empty, ipAddress))
            {
                return StatusCode(429, "Rate limit exceeded. Please try again later.");
            }

            try
            {
                var emoji = _aiEmojiService.GetEmojiForText(request.Text);
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
