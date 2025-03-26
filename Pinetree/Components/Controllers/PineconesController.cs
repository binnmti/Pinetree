using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Shared.Model;

namespace Pinetree.Components.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PineconesController(ApplicationDbContext context) : ControllerBase
{
    private const string Untitled = "Untitled";
    private ApplicationDbContext DbContext { get; } = context;

    [HttpPost("add-top")]
    public async Task<Pinecone> AddTop()
    {
        var userName = User.Identity?.Name ?? "";

        var title = Untitled;
        var counter = 1;
        while (await DbContext.Pinecone.AnyAsync(p => p.UserName == userName && p.Title == title && p.ParentId == null))
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
        await DbContext.Pinecone.AddAsync(pinecone);
        await DbContext.SaveChangesAsync();
        pinecone.GroupId = pinecone.Id;
        await DbContext.SaveChangesAsync();
        return pinecone;
    }

    public record PineconeUpdateModel(long Id, string Title, string Content, long GroupId, long ParentId);

    [HttpPost("add-child")]
    public async Task<Pinecone> AddChild([FromBody] PineconeUpdateModel pinecone)
    {
        var userName = User.Identity?.Name ?? "";
        var child = new Pinecone()
        {
            Title = pinecone.Title,
            Content = pinecone.Content,
            GroupId = pinecone.GroupId,
            ParentId = pinecone.ParentId,
            UserName = userName,
            IsDelete = false,
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
            Delete = DateTime.UtcNow,
        };
        await DbContext.Pinecone.AddAsync(child);
        await DbContext.SaveChangesAsync();
        return child;
    }

    [HttpPost("update")]
    public async Task Update([FromBody] PineconeUpdateModel pinecone)
    {
        var userName = User.Identity?.Name ?? "";
        var current = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Id == pinecone.Id)
            ?? throw new KeyNotFoundException($"Pinecone with ID {pinecone.Id} not found.");
        if (current.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        current.Title = pinecone.Title;
        current.Content = pinecone.Content;
        current.Update = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
    }

    [HttpDelete("delete-include-child/{id}")]
    public async Task<Pinecone> DeleteIncludeChild(long id)
    {
        var userName = User.Identity?.Name ?? "";
        var pinecone = await SingleIncludeChild(id);
        if (pinecone.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        pinecone.Delete = DateTime.UtcNow;
        pinecone.IsDelete = true;
        DeleteChildren(pinecone);
        pinecone.Parent?.Children.Remove(pinecone);
        await DbContext.SaveChangesAsync();
        return pinecone;
    }

    [HttpGet("get-include-child/{id}")]
    public async Task<Pinecone> GetIncludeChild(long id)
    {
        var userName = User.Identity?.Name ?? "";
        var parent = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Pinecone with ID {id} not found.");
        if (parent.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        return await LoadChildrenRecursivel(parent);
    }

    [HttpGet("get-user-top-list")]
    public async Task<List<Pinecone>> GetUserTopList([FromQuery] int pageNumber, [FromQuery] int pageSize)
    {
        var userName = User.Identity?.Name ?? "";
        return await GetUserTopList(userName).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    [HttpGet("get-user-top-count")]
    public async Task<int> GetUserTopCount()
    {
        var userName = User.Identity?.Name ?? "";
        return await GetUserTopList(userName).CountAsync();
    }

    public async Task<Pinecone> SingleIncludeChild(long id)
    {
        var userName = User.Identity?.Name ?? "";
        var parent = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new KeyNotFoundException($"Pinecone with ID {id} not found.");
        if (parent.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        return await LoadChildrenRecursivel(parent);
    }

    private static void DeleteChildren(Pinecone parent)
    {
        foreach (var child in parent.Children)
        {
            DeleteChildren(child);
            child.Delete = DateTime.UtcNow;
            child.IsDelete = true;
        }
        parent.Children.Clear();
    }

    private IQueryable<Pinecone> GetUserTopList(string userName)
        => DbContext.Pinecone.Where(x => x.UserName == userName)
            .Where(x => x.ParentId == null)
            .Where(x => x.IsDelete == false);

    private async Task<Pinecone> LoadChildrenRecursivel(Pinecone parent)
    {
        await DbContext.Entry(parent).Collection(p => p.Children).Query().Where(x => x.IsDelete == false).LoadAsync();
        foreach (var child in parent.Children.Where(x => x.IsDelete == false))
        {
            await LoadChildrenRecursivel(child);
        }
        return parent;
    }
}
