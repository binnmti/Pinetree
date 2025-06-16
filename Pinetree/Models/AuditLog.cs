using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Pinetree.Models;

[Index(nameof(Timestamp))]
[Index(nameof(UserId))]
[Index(nameof(IpAddress))]
[Index(nameof(StatusCode))]
[Index(nameof(RequestPath))]
public class AuditLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string HttpMethod { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2000)]
    public string RequestPath { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? QueryString { get; set; }
    
    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? UserAgent { get; set; }
    
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    [MaxLength(256)]
    public string? UserName { get; set; }
    
    [MaxLength(100)]
    public string? UserRole { get; set; }
      [Required]
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    [MaxLength(20)]
    public string? Priority { get; set; }
    
    [MaxLength(1000)]
    public string? AdditionalData { get; set; }
    
    // Audit category and priority tracking
    [MaxLength(50)]
    public string? AuditCategory { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // For tamper detection
    [MaxLength(64)]
    public string? HashValue { get; set; }
      // Navigation properties for common queries
    public bool IsError => StatusCode >= 400;
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 400;
    public bool IsRedirect => StatusCode >= 300 && StatusCode < 400;
    
    // Helper methods
    public string GetPriorityLevel()
    {
        return StatusCode switch
        {
            >= 500 => "Critical",
            >= 400 => "High",
            >= 300 => "Medium",
            _ => "Low"
        };
    }
    
    public string GetFormattedResponseTime()
    {
        return ResponseTimeMs < 1000 
            ? $"{ResponseTimeMs}ms" 
            : $"{ResponseTimeMs / 1000.0:F2}s";
    }
}
