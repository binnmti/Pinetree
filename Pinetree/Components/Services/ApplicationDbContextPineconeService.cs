using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Model;
using System.Diagnostics;

namespace Pinetree.Components.Services;

public static class ApplicationDbContextPineconeService
{
    private const string Untitled = "Untitled";

    public static async Task<Pinecone> AddTopAsync(this ApplicationDbContext dbContext, string userName)
    {
        var title = Untitled;
        var counter = 1;
        while (await dbContext.Pinecone.AnyAsync(p => p.UserName == userName && p.Title == title && p.ParentId == null))
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
            UserName = userName,
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

    public static async Task<Pinecone> AddChildAsync(this ApplicationDbContext dbContext, long groupId, long parentId, string userName)
    {
        var pinecone = new Pinecone()
        {
            Title = Untitled,
            Content = "",
            GroupId = groupId,
            ParentId = parentId,
            UserName = userName,
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
        dbContext.DeleteChildren(pinecone);
        pinecone.Parent?.Children.Remove(pinecone);
        await dbContext.SaveChangesAsync();
        return pinecone;
    }

    public static async Task<Pinecone> SingleIncludeChildAsync(this ApplicationDbContext dbContext, long id)
    {
        var parent = await dbContext.Pinecone.SingleAsync(x => x.Id == id);
        return await dbContext.LoadChildrenRecursivelyAsync(parent);
    }

    public static async Task<Pinecone?> GetUserTopAsync(this ApplicationDbContext dbContext, string userName)
        => await dbContext.GetUserTopList(userName).SingleOrDefaultAsync();

    public static async Task<List<Pinecone>> GetUserTopListAsync(this ApplicationDbContext dbContext, string userName, int pageNumber, int pageSize)
        => await dbContext.GetUserTopList(userName).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

    public static async Task<int> GetUserTopCountAsync(this ApplicationDbContext dbContext, string userName)
        => await dbContext.GetUserTopList(userName).CountAsync();

    private static IQueryable<Pinecone> GetUserTopList(this ApplicationDbContext dbContext, string userName)
        => dbContext.Pinecone.Where(x => x.UserName == userName)
            .Where(x => x.ParentId == null)
            .Where(x => x.IsDelete == false);

    private static async Task<Pinecone> LoadChildrenRecursivelyAsync(this ApplicationDbContext dbContext, Pinecone parent)
    {
        await dbContext.Entry(parent).Collection(p => p.Children).Query().Where(x => x.IsDelete == false).LoadAsync();
        foreach (var child in parent.Children.Where(x => x.IsDelete == false))
        {
            await dbContext.LoadChildrenRecursivelyAsync(child);
        }
        return parent;
    }

    private static void DeleteChildren(this ApplicationDbContext dbContext, Pinecone parent)
    {
        foreach (var child in parent.Children)
        {
            dbContext.DeleteChildren(child);
            child.Delete = DateTime.UtcNow;
            child.IsDelete = true;
        }
        parent.Children.Clear();
    }
}