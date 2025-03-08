using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Model;

namespace Pinetree.Components.Services;

public static class PineconeService
{
    public static async Task<string> CreateAsync(ApplicationDbContext dbContext, string userId)
    {
        var baseTitle = "Untitled";
        var title = baseTitle;
        var counter = 1;
        while (await dbContext.Pinecone.AnyAsync(p => p.UserId == userId && p.Title == title && p.ParentId == null))
        {
            title = $"{baseTitle} {counter}";
            counter++;
        }
        var pinecone = new Pinecone { Title = title, Content = "", GroupId = 0, ParentId = null, UserId = userId, IsSandbox = true };
        await dbContext.Pinecone.AddAsync(pinecone);
        await dbContext.SaveChangesAsync();
        pinecone.GroupId = pinecone.Id;
        await dbContext.SaveChangesAsync();

        return $"/{userId}/Edit/{pinecone.Id}";
    }

    public static async Task DeleteAsync(ApplicationDbContext dbContext, Pinecone pinecone)
    {
        await DeleteChildren(dbContext, pinecone);
        dbContext.Pinecone.RemoveRange(pinecone);
        await dbContext.SaveChangesAsync();
    }

    private static async Task DeleteChildren(ApplicationDbContext dbContext, Pinecone parent)
    {
        foreach (var child in parent.Children)
        {
            var childEntity = await dbContext.Pinecone.FirstOrDefaultAsync(p => p.Id == child.Id);
            if (childEntity != null)
            {
                await DeleteChildren(dbContext, childEntity);
                dbContext.Pinecone.Remove(childEntity);
            }
        }
    }
}