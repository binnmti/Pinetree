using Microsoft.EntityFrameworkCore;
using Pinetree.Components.Account.Pages;
using Pinetree.Data;
using Pinetree.Model;

namespace Pinetree.Components.Services;

public static class ApplicationDbContextUserervice
{
    public static async Task AddOrUpdateTryUserAsync(this ApplicationDbContext dbContext, string id, string ip)
    {
        var hit = await dbContext.TryUser.SingleOrDefaultAsync(x => x.Id == id);
        if (hit == null)
        {
            var tryUser = new TryUser()
            {
                Id = id,
                Ip = ip,
                CreateCount = 1,
                FileCount = 1,
                IsRegister = false,
                IsConfirmed = false,
                Create = DateTime.UtcNow,
                Update = DateTime.UtcNow,
            };
            await dbContext.TryUser.AddAsync(tryUser);
        }
        else
        {
            hit.CreateCount++;
            hit.Update = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync();
    }

    public static async Task RegisterTryUserAsync(this ApplicationDbContext dbContext, string id)
    {
        var hit = await dbContext.TryUser.SingleOrDefaultAsync(x => x.Id == id);
        if (hit == null) return;

        var fileCount = await dbContext.Pinecone.CountAsync(x => x.UserId == id);
        hit.FileCount = fileCount;
        hit.Update = DateTime.UtcNow;
        hit.IsRegister = true;
        await dbContext.SaveChangesAsync();
    }

    public static async Task ConfirmedTryUserAsync(this ApplicationDbContext dbContext, string id, string ip)
    {
        var hit = await dbContext.TryUser.SingleOrDefaultAsync(x => x.Id == id);
        if (hit == null) return;

        var fileCount = await dbContext.Pinecone.CountAsync(x => x.UserId == id);
        hit.FileCount = fileCount;
        hit.Update = DateTime.UtcNow;
        hit.IsConfirmed = true;
        await dbContext.SaveChangesAsync();
    }
}