using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pinetree.Common;
using Pinetree.Data;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace Pinetree.Controller;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private string StripeProductID { get; } = "";
    private readonly UserManager<ApplicationUser> UserManager;

    public PaymentsController(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        StripeConfiguration.ApiKey = configuration.GetValue<string>("StripeSecretKey");
        StripeProductID = configuration.GetValue<string>("StripeProductID") ?? "";
        UserManager = userManager;
    }

    public class CreateCheckoutSessionRequest
    {
        public string SuccessUrl { get; set; } = "";
        public string CancelUrl { get; set; } = "";
    }
    [HttpPost("create-checkout-session")]
    public ActionResult CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

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
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
        };

        var service = new SessionService();
        var session = service.Create(options);
        return Ok(session.Url);
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
                var session = stripeEvent.Data.Object as Session;

                if (session != null)
                {
                    // ���^�f�[�^���烆�[�U�[ID���擾
                    var userId = session.Metadata["UserId"];
                    var user = await UserManager.FindByIdAsync(userId);

                    if (user != null)
                    {
                        // ���[�U�[�̃��[�����X�V
                        await UserManager.RemoveFromRoleAsync(user, Roles.Free);
                        await UserManager.AddToRoleAsync(user, Roles.Professional);

                        // �K�v�ɉ����Ēʒm�⃍�O��ǉ�
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            // �G���[����
            return BadRequest();
        }
    }
}
