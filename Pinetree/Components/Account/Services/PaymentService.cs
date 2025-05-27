using Stripe.Checkout;
using Stripe;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Pinetree.Data;

namespace Pinetree.Components.Account.Services;

public class PaymentService
{
    private readonly string StripeProductID;
    private readonly IHttpContextAccessor HttpContextAccessor;
    private readonly UserManager<ApplicationUser> UserManager;

    public PaymentService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        StripeConfiguration.ApiKey = configuration.GetConnectionString("StripeSecretKey");
        StripeProductID = configuration.GetConnectionString("StripeProductID") ?? "";
        HttpContextAccessor = httpContextAccessor;
        UserManager = userManager;
    }

    public async Task<string> CreateCheckoutSessionAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext!;
        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated。");
        }

        var requestScheme = httpContext.Request.Scheme;
        var requestHost = httpContext.Request.Host.Value;
        var successUrl = $"{requestScheme}://{requestHost}/Account/Manage/Plan?payment=success&session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{requestScheme}://{requestHost}/Account/Manage/Plan?payment=cancel&session_id={{CHECKOUT_SESSION_ID}}";
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = StripeProductID,
                    Quantity = 1,
                },
            ],
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "UserId", userId }
            },
        };
        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task<string> CreatePortalSession()
    {
        var httpContext = HttpContextAccessor.HttpContext!;
        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated。");
        }
        var applicationUser = await UserManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("User not found。");

        var requestScheme = httpContext.Request.Scheme;
        var requestHost = httpContext.Request.Host.Value;
        var returnUrl = $"{requestScheme}://{requestHost}/Account/Manage/Plan";
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = applicationUser.StripeCustomerId,
            ReturnUrl = returnUrl,
        };
        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }
}
