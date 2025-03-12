using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Pinetree.Data;

namespace Pinetree.Components.Services;

public static class ProtectedLocalStoreService
{
    public static async Task<string> GetOrCreatePineconeAsync(this ProtectedLocalStorage storage, ApplicationDbContext dbContext)
    {
        string userId;
        long id;
        var userIdResult = await storage.GetAsync<string>("UserId");
        if (userIdResult.Success && userIdResult.Value != null)
        {
            userId = userIdResult.Value;
            var pinecode = await dbContext.GetUserTopAsync(userId) ?? await dbContext.AddTopAsync(userId);
            id = pinecode.Id;
        }
        else
        {
            userId = Guid.NewGuid().ToString("N");
            await storage.SetAsync("UserId", userId);
            var pinecode = await dbContext.AddTopAsync(userId);
            id = pinecode.Id;
        }
        return $"/{userId}/Edit/{id}";
    }
}