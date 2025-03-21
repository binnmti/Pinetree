using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        StripeConfiguration.ApiKey = configuration.GetValue<string>("StripeSecretKey");
        UserManager = userManager;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                Request.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["StripeWebhookSecret"]
            );
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                if (stripeEvent.Data.Object is Session session)
                {
                    var userId = session.Metadata["UserId"];
                    var user = await UserManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        await UserManager.RemoveFromRoleAsync(user, Roles.Free);
                        await UserManager.AddToRoleAsync(user, Roles.Professional);
                    }
                }
            }
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}
