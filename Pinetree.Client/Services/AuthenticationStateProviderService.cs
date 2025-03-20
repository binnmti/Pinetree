using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Principal;

namespace Pinetree.Client.Services;

public static class AuthenticationStateProviderService
{
    public static async Task<IIdentity?> GetIdentityAsync(this AuthenticationStateProvider authenticationStateProvider)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity;
    }
}