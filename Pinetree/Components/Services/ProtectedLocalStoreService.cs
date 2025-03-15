using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
namespace Pinetree.Components.Services;

public static class ProtectedLocalStoreService
{
    public static async Task<string> GetUserIdAsync(this ProtectedLocalStorage storage)
    {
        var result = await storage.GetAsync<string>("UserId");
        return result.Success ? result.Value ?? "" : "";
    }

    public static async Task<string> GetOrCreateUserIdAsync(this ProtectedLocalStorage storage)
    {
        string userId;
        var userIdResult = await storage.GetAsync<string>("UserId");
        if (userIdResult.Success && userIdResult.Value != null)
        {
            userId = userIdResult.Value;
        }
        else
        {
            userId = Guid.NewGuid().ToString("N");
            await storage.SetAsync("UserId", userId);
        }
        return userId;
    }
}