using System.Collections.Concurrent;

namespace Pinetree.Services;

public class RateLimitService
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();
    private const int MaxRequestsPerMinute = 20; // Per user per minute - generous limit
    private const int MaxRequestsPer10Minutes = 100; // Per user per 10 minutes
    private readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(15);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public bool IsAllowed(string userId, string? ipAddress = null)
    {
        var result = CheckRateLimit(userId, ipAddress);
        return result.IsAllowed;
    }

    public RateLimitResult CheckRateLimit(string userId, string? ipAddress = null)
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
