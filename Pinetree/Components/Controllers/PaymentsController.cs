using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Shared;
using Stripe;
using Stripe.Checkout;

namespace Pinetree.Components.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> UserManager;

    public PaymentsController(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        StripeConfiguration.ApiKey = configuration.GetConnectionString("StripeSecretKey");
        UserManager = userManager;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
#if DEBUG
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                Request.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("StripeWebhookSecretDebug")
            );
#else
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                Request.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("StripeWebhookSecret")
            );
#endif
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                if (stripeEvent.Data.Object is Session session)
                {
                    var userId = session.Metadata["UserId"];
                    var user = await UserManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        return BadRequest();
                    }
                    await UserManager.RemoveFromRoleAsync(user, Roles.Free);
                    await UserManager.AddToRoleAsync(user, Roles.Professional);
                    if (string.IsNullOrEmpty(user.StripeCustomerId) && !string.IsNullOrEmpty(session.CustomerId))
                    {
                        user.StripeCustomerId = session.CustomerId;
                        await UserManager.UpdateAsync(user);
                    }
                    if (session.Mode == "subscription" && !string.IsNullOrEmpty(session.SubscriptionId))
                    {
                        user.StripeSubscriptionId = session.SubscriptionId;
                        await UserManager.UpdateAsync(user);
                    }
                }
            }
            switch (stripeEvent.Type)
            {
                case EventTypes.CustomerSubscriptionDeleted:
                    if (stripeEvent.Data.Object is Subscription deletedSubscription)
                    {
                        await HandleCancelledSubscription(deletedSubscription);
                    }
                    break;

                case EventTypes.CustomerSubscriptionUpdated:
                    if (stripeEvent.Data.Object is Subscription updatedSubscription)
                    {
                        if (updatedSubscription.CanceledAt.HasValue ||
                            updatedSubscription.Status == "canceled")
                        {
                            await HandleCancelledSubscription(updatedSubscription);
                        }
                    }
                    break;
            }
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    private async Task HandleCancelledSubscription(Subscription subscription)
    {
        var customerId = subscription.CustomerId;
        var user = await UserManager.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        if (user != null)
        {
            await UserManager.RemoveFromRoleAsync(user, Roles.Professional);
            await UserManager.AddToRoleAsync(user, Roles.Free);
            user.SubscriptionStatus = "canceled";
            //user.StripeSubscriptionId = null;
            await UserManager.UpdateAsync(user);

            // TODO: Mail
        }
    }
}
