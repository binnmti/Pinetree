using Stripe.Checkout;
using Stripe;
using System.Security.Claims;

namespace Pinetree.Components.Account.Services;

public class PaymentService
{
    private readonly string StripeProductID;
    private readonly IHttpContextAccessor HttpContextAccessor;

    public PaymentService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        StripeConfiguration.ApiKey = configuration["StripeSecretKey"];
        StripeProductID = configuration["StripeProductID"] ?? "";
        HttpContextAccessor = httpContextAccessor;
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
        var successUrl = $"{requestScheme}://{requestHost}/Account/Manage/Plan?payment=success";
        var cancelUrl = $"{requestScheme}://{requestHost}/Account/Manage/Plan?payment=cancel";
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
}
