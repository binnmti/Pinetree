using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Collections.Concurrent;

namespace Pinetree.Services;

public class AIEmojiWithRateLimitService
{
    private readonly ChatClient _client;
    // Simple in-memory cache for faster responses
    private static readonly Dictionary<string, (string emoji, DateTime timestamp)> _emojiCache = [];
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);
    
    // Rate limiting properties
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();
    private const int MaxRequestsPerMinute = 10; // Reduced for AI API calls
    private const int MaxRequestsPer10Minutes = 50; // Reduced for AI API calls
    private readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(15);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public AIEmojiWithRateLimitService(IConfiguration configuration)
    {
        var endpoint = configuration.GetConnectionString("AzureOpenAIEndpoint") ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        var apiKey = configuration.GetConnectionString("AzureOpenAIApiKey") ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)).GetChatClient("gpt-35-turbo");
    }

    public async Task<EmojiResult> GetEmojiForTextAsync(string text, string userId, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new EmojiResult { Emoji = "📝", IsSuccess = true };

        // Check cache first - if cached, bypass rate limiting
        var cacheKey = GetCacheKey(text);
        if (_emojiCache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.timestamp < CacheExpiration)
        {
            return new EmojiResult { Emoji = cached.emoji, IsSuccess = true, IsCached = true };
        }

        // Check rate limit for new API calls
        var rateLimitResult = CheckRateLimit(userId, ipAddress);
        if (!rateLimitResult.IsAllowed)
        {
            return new EmojiResult 
            { 
                Emoji = "📝", // Default emoji
                IsSuccess = false, 
                RateLimitResult = rateLimitResult,
                ErrorMessage = $"Rate limit exceeded ({rateLimitResult.LimitType}). Please wait {rateLimitResult.WaitTimeSeconds} seconds."
            };
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
                _emojiCache[cacheKey] = (emoji, DateTime.UtcNow);

                return new EmojiResult { Emoji = emoji, IsSuccess = true };
            }

            return new EmojiResult { Emoji = "📝", IsSuccess = true }; // Default emoji if no choices
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting emoji from AI: {ex.Message}");
            return new EmojiResult 
            { 
                Emoji = "📝", // Default emoji on error
                IsSuccess = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    private RateLimitResult CheckRateLimit(string userId, string? ipAddress = null)
    {
        CleanupOldEntries();
        
        var key = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{ipAddress ?? "unknown"}";
        var now = DateTime.UtcNow;
        
        _requestLog.AddOrUpdate(key, 
            new List<DateTime> { now }, 
            (_, existing) => 
            {
                existing.Add(now);
                return existing;
            });

        var requests = _requestLog[key];
        
        // Check 1-minute limit
        var recentRequests = requests.Count(r => now - r < TimeSpan.FromMinutes(1));
        if (recentRequests > MaxRequestsPerMinute)
        {
            var oldestRequestInMinute = requests.Where(r => now - r < TimeSpan.FromMinutes(1)).Min();
            var waitTimeSeconds = (int)(60 - (now - oldestRequestInMinute).TotalSeconds);
            return new RateLimitResult
            {
                IsAllowed = false,
                LimitType = "per minute",
                MaxRequests = MaxRequestsPerMinute,
                CurrentRequests = recentRequests,
                WaitTimeSeconds = waitTimeSeconds,
                ResetTime = oldestRequestInMinute.AddMinutes(1)
            };
        }
            
        // Check 10-minute limit  
        var requests10Min = requests.Count(r => now - r < TimeSpan.FromMinutes(10));
        if (requests10Min > MaxRequestsPer10Minutes)
        {
            var oldestRequestIn10Min = requests.Where(r => now - r < TimeSpan.FromMinutes(10)).Min();
            var waitTimeSeconds = (int)(600 - (now - oldestRequestIn10Min).TotalSeconds);
            return new RateLimitResult
            {
                IsAllowed = false,
                LimitType = "per 10 minutes",
                MaxRequests = MaxRequestsPer10Minutes,
                CurrentRequests = requests10Min,
                WaitTimeSeconds = waitTimeSeconds,
                ResetTime = oldestRequestIn10Min.AddMinutes(10)
            };
        }
            
        return new RateLimitResult
        {
            IsAllowed = true,
            LimitType = "",
            MaxRequests = MaxRequestsPerMinute,
            CurrentRequests = recentRequests,
            WaitTimeSeconds = 0,
            ResetTime = DateTime.UtcNow
        };
    }

    private void CleanupOldEntries()
    {
        if (DateTime.UtcNow - _lastCleanup < CleanupInterval)
            return;
            
        var cutoffTime = DateTime.UtcNow - TimeSpan.FromMinutes(15);
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _requestLog)
        {
            kvp.Value.RemoveAll(time => time < cutoffTime);
            if (kvp.Value.Count == 0)
                keysToRemove.Add(kvp.Key);
        }
        foreach (var key in keysToRemove)
            _requestLog.TryRemove(key, out _);
            
        _lastCleanup = DateTime.UtcNow;
    }

    private static string GetCacheKey(string text)
    {
        // Create a simple hash of the first 100 characters for caching
        var keyText = text.Length > 100 ? text[..100] : text;
        return keyText.ToLowerInvariant().Trim();
    }
}

public class EmojiResult
{
    public string Emoji { get; set; } = "📝";
    public bool IsSuccess { get; set; }
    public bool IsCached { get; set; }
    public string? ErrorMessage { get; set; }
    public RateLimitResult? RateLimitResult { get; set; }
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string LimitType { get; set; } = "";
    public int MaxRequests { get; set; }
    public int CurrentRequests { get; set; }
    public int WaitTimeSeconds { get; set; }
    public DateTime ResetTime { get; set; }
}
