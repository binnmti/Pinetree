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
                                  UserManager<ApplicationUser> userManager)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await LogRequestAsync(context, auditLogService, userManager, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Only log if it meets compliance requirements
            if (ShouldLogForCompliance(context))
            {
                await LogRequestAsync(context, auditLogService, userManager, stopwatch.ElapsedMilliseconds);
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

        // Administrative actions
        if (path.StartsWith("/admin"))
        {
            return true;
        }

        // Data modification operations (API calls)
        if (path.StartsWith("/api") && method != "GET")
        {
            return true;
        }

        // Failed requests (potential security issues)
        if (context.Response.StatusCode >= 400)
        {
            return true;
        }

        // Skip everything else (static files, normal page views, etc.)
        return false;
    }

    private async Task LogRequestAsync(HttpContext context, IAuditLogService auditLogService,
                                       UserManager<ApplicationUser> userManager, long responseTimeMs, 
                                       string? errorMessage = null)
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

            await auditLogService.LogAsync(
                httpMethod: request.Method,
                requestPath: request.Path,
                queryString: request.QueryString.HasValue ? request.QueryString.Value : null,
                ipAddress: ipAddress,
                userAgent: request.Headers.UserAgent.FirstOrDefault(),
                userId: userId,
                userName: userName,
                userRole: userRole,
                statusCode: response.StatusCode,
                responseTimeMs: responseTimeMs,
                errorMessage: errorMessage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry in middleware");
        }
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
