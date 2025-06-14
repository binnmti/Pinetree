using Pinetree.Data;
using Pinetree.Shared.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Pinetree.Services;

public interface IAuditLogService
{
    Task LogAsync(string httpMethod, string requestPath, string? queryString, string ipAddress, 
                  string? userAgent, string? userId, string? userName, string? userRole, 
                  int statusCode, long responseTimeMs, string? errorMessage = null, 
                  object? additionalData = null,
                  string? auditCategory = null,
                  string? priority = null);
    
    Task<(IEnumerable<AuditLog> logs, int totalCount)> GetLogsAsync(
        int page = 1, int pageSize = 50, string? searchTerm = null, 
        DateTime? startDate = null, DateTime? endDate = null, 
        string? userId = null, int? statusCode = null);
    
    Task<byte[]> ExportToCsvAsync(string? searchTerm = null, DateTime? startDate = null, 
                                  DateTime? endDate = null, string? userId = null, int? statusCode = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task LogAsync(string httpMethod, string requestPath, string? queryString, 
                               string ipAddress, string? userAgent, string? userId, string? userName, 
                               string? userRole, int statusCode, long responseTimeMs, 
                               string? errorMessage = null, object? additionalData = null, 
                               string? auditCategory = null, string? priority = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                HttpMethod = httpMethod,
                RequestPath = requestPath,
                QueryString = queryString,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                StatusCode = statusCode,
                ResponseTimeMs = responseTimeMs,
                ErrorMessage = errorMessage,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
                AuditCategory = auditCategory,
                Priority = priority,
                CreatedAt = DateTime.UtcNow
            };

            // Generate hash for tamper detection
            auditLog.HashValue = GenerateHash(auditLog);

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry");
        }
    }

    public async Task<(IEnumerable<AuditLog> logs, int totalCount)> GetLogsAsync(
        int page = 1, int pageSize = 50, string? searchTerm = null,
        DateTime? startDate = null, DateTime? endDate = null,
        string? userId = null, int? statusCode = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(l => l.RequestPath.Contains(searchTerm) ||
                                     l.UserName!.Contains(searchTerm) ||
                                     l.IpAddress.Contains(searchTerm));
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(l => l.UserId == userId);
        }

        if (statusCode.HasValue)
        {
            query = query.Where(l => l.StatusCode == statusCode.Value);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<byte[]> ExportToCsvAsync(string? searchTerm = null, DateTime? startDate = null,
                                               DateTime? endDate = null, string? userId = null, int? statusCode = null)
    {
        var (logs, _) = await GetLogsAsync(1, int.MaxValue, searchTerm, startDate, endDate, userId, statusCode);

        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,HTTP Method,Request Path,Query String,IP Address,User Agent,User ID,User Name,User Role,Status Code,Response Time (ms),Error Message,Success");

        foreach (var log in logs)
        {
            csv.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.HttpMethod}\",\"{log.RequestPath}\",\"{log.QueryString}\",\"{log.IpAddress}\",\"{log.UserAgent}\",\"{log.UserId}\",\"{log.UserName}\",\"{log.UserRole}\",{log.StatusCode},{log.ResponseTimeMs},\"{log.ErrorMessage}\",{log.IsSuccess}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private string GenerateHash(AuditLog auditLog)
    {
        var data = $"{auditLog.Timestamp:yyyy-MM-dd HH:mm:ss.fff}{auditLog.HttpMethod}{auditLog.RequestPath}{auditLog.IpAddress}{auditLog.UserId}{auditLog.StatusCode}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
