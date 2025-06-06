using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Services;

namespace Pinetree.Components.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AIEmojiController : ControllerBase
    {
        private readonly AIEmojiService _aiEmojiService;

        public AIEmojiController(AIEmojiService aiEmojiService)
        {
            _aiEmojiService = aiEmojiService;
        }

        [HttpPost("suggest")]
        public IActionResult SuggestEmoji([FromBody] SuggestEmojiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Text is required");
            }

            try
            {
                var emoji =  _aiEmojiService.GetEmojiForText(request.Text);
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
