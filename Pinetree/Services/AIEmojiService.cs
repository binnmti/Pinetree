using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace Pinetree.Services;

public class AIEmojiService
{
    private readonly ChatClient _client;
    // Simple in-memory cache for faster responses
    private static readonly Dictionary<string, (string emoji, DateTime timestamp)> _cache = [];
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public AIEmojiService(IConfiguration configuration)
    {
        var endpoint = configuration.GetConnectionString("AzureOpenAIEndpoint") ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        var apiKey = configuration.GetConnectionString("AzureOpenAIApiKey") ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)).GetChatClient("gpt-35-turbo");
    }

    public async Task<string> GetEmojiForTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "📝";

        // Check cache first
        var cacheKey = GetCacheKey(text);
        if (_cache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.timestamp < CacheExpiration)
        {
            return cached.emoji;
        }

        try
        {
            var chatOptions = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = 10,
                Temperature = 0.3f,
            };
            var chatResponse = await _client.CompleteChatAsync([
                    new SystemChatMessage("You are an emoji selector. Respond with ONLY a single emoji that best represents the given text. No explanation, just one emoji."),
                    new UserChatMessage(text),
            ], chatOptions);

            if (chatResponse.Value.Content.Count > 0)
            {
                var emoji = chatResponse.Value.Content[0].Text;

                // Cache the result
                _cache[cacheKey] = (emoji, DateTime.UtcNow);

                return emoji;
            }

            return "📝"; // Default emoji if no choices
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting emoji from AI: {ex.Message}");
            return "📝"; // Default emoji on error
        }
    }

    private static string GetCacheKey(string text)
    {
        // Create a simple hash of the first 100 characters for caching
        var keyText = text.Length > 100 ? text[..100] : text;
        return keyText.ToLowerInvariant().Trim();
    }
}