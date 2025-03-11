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
            IsDelete = false,
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
            Delete = DateTime.UtcNow,
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
            IsDelete = false,
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
            Delete = DateTime.UtcNow,
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
        current.Update = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public static async Task<Pinecone> DeleteIncludeChildAsync(this ApplicationDbContext dbContext, long id)
    {
        var pinecone = await dbContext.SingleIncludeChildAsync(id);
        Debug.Assert(pinecone != null);
        pinecone.Delete = DateTime.UtcNow;
        pinecone.IsDelete = true;
        await dbContext.DeleteChildren(pinecone);
        pinecone.Parent?.Children.Remove(pinecone);
        await dbContext.SaveChangesAsync();
        return pinecone;
    }

    public static async Task<Pinecone> SingleIncludeChildAsync(this ApplicationDbContext dbContext, long id)
    {
        var parent = await dbContext.Pinecone.SingleAsync(x => x.Id == id);
        return await dbContext.LoadChildrenRecursively(parent);
    }

    public static async Task<Pinecone> GetUserTop(this ApplicationDbContext dbContext, string userId)
    {
        var hit = await dbContext.Pinecone.Where(x => x.UserId == userId).Where(x => x.IsDelete == false).Where(x => x.ParentId == null).SingleOrDefaultAsync();
        hit ??= await dbContext.AddTopAsync(userId);
        return hit;
    }

    private static async Task<Pinecone> LoadChildrenRecursively(this ApplicationDbContext dbContext, Pinecone parent)
    {
        await dbContext.Entry(parent).Collection(p => p.Children).Query().Where(x => x.IsDelete == false).LoadAsync();
        foreach (var child in parent.Children.Where(x => x.IsDelete == false))
        {
            await dbContext.LoadChildrenRecursively(child);
        }
        return parent;
    }

    private static async Task DeleteChildren(this ApplicationDbContext dbContext, Pinecone parent)
    {
        foreach (var child in parent.Children)
        {
            await dbContext.DeleteChildren(child);
            child.Delete = DateTime.UtcNow;
            child.IsDelete = true;
        }
        parent.Children.Clear();
    }
}