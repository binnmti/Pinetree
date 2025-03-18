using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Pinetree.Data;

namespace Pinetree.Components.Account;

// TODO:Delete the email domain once you have it
public class CustomSignInManager : SignInManager<ApplicationUser>
{
    public CustomSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    protected override async Task<SignInResult> PreSignInCheck(ApplicationUser user)
    {
        // EmailConfirmed Skip
        if (await IsLockedOut(user))
        {
            return await LockedOut(user);
        }

        return SignInResult.Success;
    }
}
