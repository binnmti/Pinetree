using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Pinetree.Services;
using System.Security.Claims;

namespace Pinetree.Components.Filters;

public class EmojiRateLimitAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var rateLimitService = context.HttpContext.RequestServices.GetRequiredService<RateLimitService>();
        
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!rateLimitService.IsAllowed(userId ?? string.Empty, ipAddress))
        {
            context.Result = new ObjectResult("Rate limit exceeded. Please try again later.")
            {
                StatusCode = 429
            };
            return;
        }

        await next();
    }
}
