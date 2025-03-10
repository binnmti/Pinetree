using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Model;
using System.Diagnostics;

namespace Pinetree.Components.Services;

public static class ApplicationDbContextService
{
    private const string Untitled = "Untitled";

    public static async Task<Pinecone> AddTopAsync(this ApplicationDbContext dbContext, string userId)
    {
        var title = Untitled;
        var counter = 1;
        while (await dbContext.Pinecone.AnyAsync(p => p.UserId == userId && p.Title == title && p.ParentId == null))
        {
            title = $"{Untitled} {counter}";
            counter++;
        }
        var pinecone = new Pinecone
        {
            Title = title,
            Content = "",
            GroupId = 0,
            ParentId = null,
            UserId = userId,
            IsSandbox = true
        };
        await dbContext.Pinecone.AddAsync(pinecone);
        await dbContext.SaveChangesAsync();
        pinecone.GroupId = pinecone.Id;
        await dbContext.SaveChangesAsync();
        return pinecone;
    }

    public static async Task<Pinecone> AddChildAsync(this ApplicationDbContext dbContext, long groupId, long parentId, string userId)
    {
        var pinecone = new Pinecone()
        {
            Title = Untitled,
            Content = "",
            GroupId = groupId,
            ParentId = parentId,
            UserId = userId,
            IsSandbox = false
        };
        await dbContext.Pinecone.AddAsync(pinecone);
        await dbContext.SaveChangesAsync();
        return pinecone;
    }

    public static async Task UpdateAsync(this ApplicationDbContext dbContext, long id, string title, string content)
    {
        var current = await dbContext.Pinecone.SingleAsync(x => x.Id == id);
        current.Title = title;
        current.Content = content;
        await dbContext.SaveChangesAsync();
    }

    public static async Task<Pinecone> DeleteIncludeChildAsync(this ApplicationDbContext dbContext, long id)
    {
        var pinecone = await dbContext.SingleIncludeChildAsync(id);
        Debug.Assert(pinecone != null);
        await dbContext.DeleteChildren(pinecone);
        dbContext.Pinecone.RemoveRange(pinecone);
        await dbContext.SaveChangesAsync();
        return pinecone;
    }

    public static async Task<Pinecone> SingleIncludeChildAsync(this ApplicationDbContext dbContext, long id)
    {
        // TODO:Performance check by Where is still
        return await dbContext.Pinecone.Where(x => x.Id == id).Include(p => p.Children).SingleAsync(x => x.Id == id);
    }

    private static async Task DeleteChildren(this ApplicationDbContext dbContext, Pinecone parent)
    {
        foreach (var child in parent.Children)
        {
            await dbContext.DeleteChildren(child);
            dbContext.Pinecone.Remove(child);
        }
    }
}