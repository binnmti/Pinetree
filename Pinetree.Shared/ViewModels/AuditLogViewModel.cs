namespace Pinetree.Shared.ViewModels;

public class AuditLogViewModel
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AdditionalData { get; set; }
    public string? AuditCategory { get; set; }
    public string? Priority { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? UserName { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? AuditCategory { get; set; }
    public string? Priority { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
