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

    [HttpPost("update-tree")]
    public async Task<IActionResult> UpdateTree([FromBody] TreeUpdateRequest request)
    {
        var userName = User.Identity?.Name ?? "";
        await GetPineconeAndVerifyOwnership(request.RootId, userName);
        if (request.HasStructuralChanges)
        {
            await RebuildTreeAsync(request.RootId, request.Nodes, userName);
        }
        else
        {
            await UpdateNodesAsync(request.Nodes, userName);
        }

        return Ok();
    }

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

        var maxOrder = (await DbContext.Pinecone
            .Where(p => p.UserName == userName && p.ParentId == null && !p.IsDelete)
            .MaxAsync(p => (int?)p.Order)) ?? -1;
        maxOrder += 1;

        var pinecone = new Pinecone
        {
            Title = title,
            Content = "",
            GroupId = 0,
            ParentId = null,
            Order = maxOrder,
            UserName = userName,
            IsDelete = false,
            Guid = Guid.NewGuid(),
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
        var pinecone = await GetPineconeAndVerifyOwnership(id, userName);

        if (pinecone.GroupId <= 0)
        {
            return await LoadChildrenRecursively(pinecone);
        }

        try
        {
            var rootPinecone = await GetPineconeAndVerifyOwnership(pinecone.GroupId, userName);
            return await LoadChildrenRecursively(rootPinecone);
        }
        catch (KeyNotFoundException)
        {
            return await LoadChildrenRecursively(pinecone);
        }
    }

    [HttpGet("get-user-top-list")]
    public async Task<List<Pinecone>> GetUserTopList([FromQuery] int pageNumber, [FromQuery] int pageSize)
    {
        var userName = User.Identity?.Name ?? "";
        return await GetUserTopList(userName)
            .OrderBy(p => p.Order) // Order でソート
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    [HttpGet("get-user-top-count")]
    public async Task<int> GetUserTopCount()
    {
        var userName = User.Identity?.Name ?? "";
        return await GetUserTopList(userName).CountAsync();
    }

    private async Task RebuildTreeAsync(long rootId, List<PineconeDto> nodes, string userName)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            var currentTree = await SingleIncludeChild(rootId);
            await LogicalDeleteTreeAsync(currentTree, userName);

            var newRoot = await CreateNewTreeAsync(nodes, userName);

            await UpdateChildrenGroupIds(newRoot.Id, newRoot.Id, userName);

            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<Pinecone> CreateNewTreeAsync(List<PineconeDto> nodes, string userName)
    {
        var nodeMap = nodes.ToDictionary(n => n.Id, n => n);

        var rootNode = nodes.FirstOrDefault(n => n.ParentId == null);
        if (rootNode == null) return null;

        var newRoot = await CreateNodeRecursivelyAsync(rootNode, null, nodeMap, userName, 0);

        newRoot.GroupId = newRoot.Id;
        await DbContext.SaveChangesAsync();

        return newRoot;
    }

    private async Task UpdateChildrenGroupIds(long nodeId, long groupId, string userName)
    {
        var children = await DbContext.Pinecone
            .Where(p => p.ParentId == nodeId && !p.IsDelete && p.UserName == userName)
            .ToListAsync();

        foreach (var child in children)
        {
            child.GroupId = groupId;
            await UpdateChildrenGroupIds(child.Id, groupId, userName);
        }
    }

    private async Task<Pinecone> CreateNodeRecursivelyAsync(
        PineconeDto nodeDto,
        long? parentId,
        Dictionary<long, PineconeDto> nodeMap,
        string userName,
        long groupId)
    {
        var pinecone = new Pinecone
        {
            Id = 0,
            Title = nodeDto.Title,
            Content = nodeDto.Content,
            GroupId = groupId,
            ParentId = parentId,
            Order = nodeDto.Order,
            UserName = userName,
            IsDelete = false,
            Guid = Guid.NewGuid(),
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
            Delete = DateTime.UtcNow
        };

        await DbContext.Pinecone.AddAsync(pinecone);
        await DbContext.SaveChangesAsync();

        var childrenNodes = nodeMap.Values
            .Where(n => n.ParentId == nodeDto.Id)
            .OrderBy(n => n.Order)
            .ToList();

        foreach (var childNode in childrenNodes)
        {
            await CreateNodeRecursivelyAsync(childNode, pinecone.Id, nodeMap, userName, groupId);
        }

        return pinecone;
    }

    private async Task UpdateNodesAsync(List<PineconeDto> nodes, string userName)
    {
        foreach (var node in nodes)
        {
            var dbNode = await DbContext.Pinecone.FindAsync(node.Id);
            if (dbNode == null || dbNode.UserName != userName)
            {
                continue;
            }

            dbNode.Title = node.Title;
            dbNode.Content = node.Content;
            dbNode.Order = node.Order;
            dbNode.Update = DateTime.UtcNow;
        }

        await DbContext.SaveChangesAsync();
    }

    private static async Task LogicalDeleteTreeAsync(Pinecone root, string userName)
    {
        if (root.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }

        var now = DateTime.UtcNow;
        root.IsDelete = true;
        root.Delete = now;

        foreach (var child in root.Children)
        {
            await LogicalDeleteTreeAsync(child, userName);
        }
    }

    private async Task<Pinecone> SingleIncludeChild(long id)
    {
        var userName = User.Identity?.Name ?? "";
        var parent = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Id == id)
                    ?? throw new KeyNotFoundException($"Pinecone with ID {id} not found.");
        if (parent.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        return await LoadChildrenRecursively(parent);
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
        => DbContext.Pinecone
            .Where(x => x.UserName == userName)
            .Where(x => x.ParentId == null)
            .Where(x => x.IsDelete == false);

    private async Task<Pinecone> LoadChildrenRecursively(Pinecone parent)
    {
        await DbContext.Entry(parent)
            .Collection(p => p.Children)
            .Query()
            .Where(x => x.IsDelete == false)
            .OrderBy(x => x.Order)
            .LoadAsync();

        foreach (var child in parent.Children)
        {
            await LoadChildrenRecursively(child);
        }
        return parent;
    }

    private async Task<Pinecone> GetPineconeAndVerifyOwnership(long id, string userName)
    {
        var pinecone = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException($"Pinecone with ID {id} not found.");
        if (pinecone.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        return pinecone;
    }
}
