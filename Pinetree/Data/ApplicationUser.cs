using Microsoft.AspNetCore.Identity;

namespace Pinetree.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string StripeCustomerId { get; set; } = "";
    public string StripeSubscriptionId { get; set; } = "";
    public string SubscriptionStatus { get; set; } = "";
    public bool HasFreeProfessionalAccess { get; set; } = false; // For beta users with free access
    public DateTime? FreeProfessionalExpiryDate { get; set; } // Optional: set expiry date
}
