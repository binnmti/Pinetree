using Microsoft.AspNetCore.Identity;

namespace Pinetree.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string StripeCustomerId { get; set; } = "";
    public string StripeSubscriptionId { get; set; } = "";
    public string SubscriptionStatus { get; set; } = "";
}
