using Pinetree.Services;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Pinetree.Data;

namespace Pinetree.Middleware;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService, 
                                  UserManager<ApplicationUser> userManager,
                                  ISensitiveDataDetectorService sensitiveDataDetector)
    {
        var stopwatch = Stopwatch.StartNew();
          try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await LogRequestAsync(context, auditLogService, userManager, sensitiveDataDetector, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Only log if it meets compliance requirements
            if (ShouldLogForCompliance(context))
            {
                await LogRequestAsync(context, auditLogService, userManager, sensitiveDataDetector, stopwatch.ElapsedMilliseconds);
            }
        }
    }
    
    private bool ShouldLogForCompliance(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        // Authentication events (login/logout/register)
        if (path.Contains("/login") || 
            path.Contains("/logout") || 
            path.Contains("/register") ||
            path.StartsWith("/account"))
        {
            return true;
        }

        // User creation/deletion and permission changes
        if (path.Contains("/files") || 
            path.Contains("/role") || 
            path.Contains("/permission") ||
            path.StartsWith("/identity"))
        {
            return true;
        }

        // Setting changes and administrative actions
        if (path.StartsWith("/admin") || 
            path.Contains("/setting") ||
            path.Contains("/config"))
        {
            return true;
        }
        
        // File upload/download operations
        if (path.Contains("/upload") || 
            path.Contains("/download") ||
            path.Contains("/file") ||
            (method == "POST" && context.Request.HasFormContentType))
        {
            return true;
        }

        // Data update operations (API calls and form submissions)
        if (path.StartsWith("/api") && (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH"))
        {
            return true;
        }

        // Plan changes (subscription/billing related)
        if (path.Contains("/plan") || 
            path.Contains("/subscription") ||
            path.Contains("/billing"))
        {
            return true;
        }

        // Data viewing of confidential information
        if (path.Contains("/audit") || 
            path.Contains("/report") ||
            path.Contains("/export") ||
            (path.StartsWith("/api") && method == "GET" && path.Contains("/confidential")))
        {
            return true;
        }

        // Error operations (4xx, 5xx status codes)
        if (context.Response.StatusCode >= 400)
        {
            return true;
        }

        // Skip everything else (static files, normal page views, etc.)
        return false;
    }
    
    private async Task LogRequestAsync(HttpContext context, IAuditLogService auditLogService,
                                       UserManager<ApplicationUser> userManager,
                                       ISensitiveDataDetectorService sensitiveDataDetector,
                                       long responseTimeMs, string? errorMessage = null)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            var user = context.User;

            string? userId = null;
            string? userName = null;
            string? userRole = null;

            if (user.Identity?.IsAuthenticated == true)
            {
                userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                userName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.Email);
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var applicationUser = await userManager.FindByIdAsync(userId);
                    if (applicationUser != null)
                    {
                        var roles = await userManager.GetRolesAsync(applicationUser);
                        userRole = string.Join(",", roles);
                    }
                }
            }
            
            var ipAddress = GetClientIpAddress(context);
            var (category, priority) = DetermineAuditCategoryAndPriority(context);

            // Mask sensitive query parameters
            var maskedQueryString = request.QueryString.HasValue 
                ? sensitiveDataDetector.MaskQueryString(request.QueryString.Value) 
                : null;

            await auditLogService.LogAsync(
                httpMethod: request.Method,
                requestPath: request.Path,
                queryString: maskedQueryString,
                ipAddress: ipAddress,
                userAgent: request.Headers["User-Agent"].FirstOrDefault(),
                userId: userId,
                userName: userName,
                userRole: userRole,
                statusCode: response.StatusCode,
                responseTimeMs: responseTimeMs,
                errorMessage: errorMessage,
                auditCategory: category,
                priority: priority
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry in middleware");
        }
    }

    private (string category, string priority) DetermineAuditCategoryAndPriority(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode;

        // Authentication events
        if (path.Contains("/login") || path.Contains("/logout") || path.Contains("/register") || path.StartsWith("/account"))
        {
            return ("Authentication", "Highest");
        }

        // User/Permission management
        if (path.Contains("/files") || path.Contains("/role") || path.Contains("/permission") || path.StartsWith("/identity"))
        {
            return ("User Management", "Highest");
        }

        // Settings/Configuration changes
        if (path.StartsWith("/admin") || path.Contains("/setting") || path.Contains("/config"))
        {
            return ("Configuration", "Highest");
        }

        // File operations
        if (path.Contains("/upload") || path.Contains("/download") || path.Contains("/file") || 
            (method == "POST" && context.Request.HasFormContentType))
        {
            return ("File Operations", "High");
        }

        // Data modification
        if (path.StartsWith("/api") && (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH"))
        {
            return ("Data Modification", "High");
        }

        // Plan/Subscription changes
        if (path.Contains("/plan") || path.Contains("/subscription") || path.Contains("/billing"))
        {
            return ("Business Operations", "High");
        }
        
        // Confidential data viewing
        if (path.Contains("/audit") || path.Contains("/report") || path.Contains("/export") ||
            (path.StartsWith("/api") && method == "GET" && path.Contains("/confidential")))
        {
            return ("Data Access", "Medium");
        }

        // Error conditions
        if (statusCode >= 400)
        {
            return (statusCode >= 500 ? "System Error" : "Client Error", "Medium");
        }

        // Default fallback
        return ("General", "Low");
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (useful when behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
