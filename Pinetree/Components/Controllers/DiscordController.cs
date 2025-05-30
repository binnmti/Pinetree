using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Pinetree.Components.Controllers;

[ApiController]
[Route("api/discord")]
public class DiscordController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;

    [HttpPost("webhook")]
    public async Task<IActionResult> SendToDiscordWebhook([FromBody] JsonElement payload)
    {
        try
        {
            var webhookUrl = _configuration.GetConnectionString("DiscordWebhookUrl");
            if (string.IsNullOrEmpty(webhookUrl))
            {
                return StatusCode(500, "Discord webhook configuration is missing");
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(webhookUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { success = true });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}