using Microsoft.AspNetCore.Identity;
using Pinetree.Data;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace Pinetree.Services;

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

    public async Task<string> CreateCustomAmountCheckoutSessionAsync(decimal amount, bool isFreeTrial)
    {
        var httpContext = HttpContextAccessor.HttpContext!;
        var user = httpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var priceId = await GetOrCreatePriceForAmountAsync(amount);

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
                    Price = priceId,
                    Quantity = 1,
                },
            ],
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "UserId", userId },
                { "CustomAmount", amount.ToString() }
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "CustomAmount", amount.ToString() }
                }
            }
        };
        if (isFreeTrial)
        {
            options.Discounts =
            [
                new() { Coupon = "KeIGwcPl" }
            ];

        }
        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    private async Task<string> GetOrCreatePriceForAmountAsync(decimal amount)
    {
        var priceService = new PriceService();
        var unitAmountInCents = (long)(amount * 100);

        // Search for existing price with same amount
        var existingPrices = await priceService.ListAsync(new PriceListOptions
        {
            Product = StripeProductID,
            Currency = "usd",
            Active = true,
            Type = "recurring",
            Limit = 100
        });

        var existingPrice = existingPrices.Data.FirstOrDefault(p =>
            p.UnitAmount == unitAmountInCents &&
            p.Recurring?.Interval == "month");

        if (existingPrice != null)
        {
            return existingPrice.Id;
        }

        // Create new price if not found
        var newPrice = await priceService.CreateAsync(new PriceCreateOptions
        {
            Product = StripeProductID,
            Currency = "usd",
            UnitAmount = unitAmountInCents,
            Recurring = new PriceRecurringOptions
            {
                Interval = "month"
            },
            Nickname = $"${amount}/month",
            Metadata = new Dictionary<string, string>
            {
                { "Amount", amount.ToString() },
                { "CreatedBy", "PaymentService" }
            }
        });

        return newPrice.Id;
    }
}
