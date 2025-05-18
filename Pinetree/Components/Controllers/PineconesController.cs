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
        await GetPineconeAndVerifyOwnership(request.RootId);
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
        while (await DbContext.Pinecone.AnyAsync(p => p.UserName == userName && p.Title == title && p.ParentGuid == null))
        {
            title = $"{Untitled} {counter}";
            counter++;
        }

        var maxOrder = (await DbContext.Pinecone
            .Where(p => p.UserName == userName && p.ParentGuid == null)
            .MaxAsync(p => (int?)p.Order)) ?? -1;
        maxOrder += 1;

        var guid = Guid.NewGuid();
        var pinecone = new Pinecone
        {
            Title = title,
            Content = "",
            GroupGuid = guid,
            ParentGuid = null,
            Order = maxOrder,
            UserName = userName,
            Guid = guid,
            IsPublic = false,
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
        };
        await DbContext.Pinecone.AddAsync(pinecone);
        await DbContext.SaveChangesAsync();
        return pinecone;
    }

    [HttpDelete("delete-include-child/{id}")]
    public async Task<IActionResult> DeleteIncludeChild(Guid id)
    {
        var userName = User.Identity?.Name ?? "";
        var pinecone = await SingleIncludeChild(id);
        if (pinecone.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        var parentGuid = pinecone.ParentGuid;
        await RemoveChildrenFromDatabaseAsync(pinecone);
        DbContext.Pinecone.Remove(pinecone);
        await DbContext.SaveChangesAsync();
        if (parentGuid != null)
        {
            await ReindexSiblingsAsync((Guid)parentGuid);
        }

        return Ok();
    }

    private async Task ReindexSiblingsAsync(Guid parentGuid)
    {
        var siblings = await DbContext.Pinecone
            .Where(p => p.ParentGuid == parentGuid)
            .OrderBy(p => p.Order)
            .ToListAsync();

        for (int i = 0; i < siblings.Count; i++)
        {
            siblings[i].Order = i;
        }

        await DbContext.SaveChangesAsync();
    }

    [HttpGet("get-include-child/{guid}")]
    public async Task<Pinecone> GetIncludeChild(Guid guid)
    {
        var pinecone = await GetPineconeAndVerifyOwnership(guid);
        var rootPinecone = await GetPineconeAndVerifyOwnership(pinecone?.GroupGuid ?? Guid.Empty);
        if (rootPinecone == null) return Pinecone.None;
        return await LoadChildrenRecursively(rootPinecone);
    }

    [HttpGet("get-view-include-child/{guid}")]
    [AllowAnonymous]
    public async Task<Pinecone?> GetViewIncludeChild(Guid guid)
    {
        var pinecone = await GetPineconeAndVerifyOwnership(guid);
        if (pinecone == null || !pinecone.IsPublic) return Pinecone.None;
        var rootPinecone = await GetPineconeAndVerifyOwnership(pinecone.GroupGuid);
        if (rootPinecone == null) return Pinecone.None;
        return await LoadChildrenRecursively(rootPinecone);
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

    [HttpPut("toggle-visibility/{guid}")]
    public async Task<IActionResult> ToggleVisibility(Guid guid, [FromBody] VisibilityUpdateRequest request)
    {
        var userName = User.Identity?.Name ?? "";
        var pinecone = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Guid == guid)
            ?? throw new KeyNotFoundException($"Pinecone with ID {guid} not found.");

        if (pinecone.UserName != userName)
        {
            return Unauthorized("You do not own this document.");
        }

        pinecone.IsPublic = request.IsPublic;
        pinecone.Update = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        return Ok();
    }

    public class VisibilityUpdateRequest
    {
        public bool IsPublic { get; set; }
    }

    private async Task RebuildTreeAsync(Guid rootId, List<PineconeDto> nodes, string userName)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            var currentTree = await SingleIncludeChild(rootId);

            var rootNodeDto = nodes.FirstOrDefault(n => n.ParentGuid == null)
                ?? throw new InvalidOperationException("Root node is missing in the provided nodes.");
            if (currentTree.UserName != userName)
            {
                throw new UnauthorizedAccessException("You do not own this Pinecone.");
            }

            currentTree.Title = rootNodeDto.Title;
            currentTree.Content = rootNodeDto.Content;
            currentTree.Update = DateTime.UtcNow;

            var currentNodeGuids = CollectNodeGuids(currentTree);
            var newNodeGuids = nodes.Select(n => n.Guid).ToHashSet();

            var nodesToRemove = currentNodeGuids.Where(guid => !newNodeGuids.Contains(guid)).ToList();

            foreach (var guidToRemove in nodesToRemove)
            {
                var nodeToRemove = await DbContext.Pinecone.FirstOrDefaultAsync(p => p.Guid == guidToRemove);
                if (nodeToRemove != null)
                {
                    DbContext.Pinecone.Remove(nodeToRemove);
                }
            }

            var nodeMap = nodes.ToDictionary(n => n.Guid, n => n);
            var childrenNodes = nodeMap.Values
                .Where(n => n.ParentGuid == rootNodeDto.Guid)
                .OrderBy(n => n.Order)
                .ToList();

            foreach (var childNode in childrenNodes)
            {
                await CreateNodeRecursivelyAsync(childNode, currentTree.Guid, nodeMap, userName, currentTree.GroupGuid);
            }

            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static HashSet<Guid> CollectNodeGuids(Pinecone root)
    {
        var guids = new HashSet<Guid>();
        CollectNodeGuidsRecursively(root, guids);
        return guids;
    }

    private static void CollectNodeGuidsRecursively(Pinecone node, HashSet<Guid> guids)
    {
        guids.Add(node.Guid);

        foreach (var child in node.Children)
        {
            CollectNodeGuidsRecursively(child, guids);
        }
    }

    private async Task RemoveChildrenFromDatabaseAsync(Pinecone parent)
    {
        if (!DbContext.Entry(parent).Collection(p => p.Children).IsLoaded)
        {
            await DbContext.Entry(parent)
                .Collection(p => p.Children)
                .LoadAsync();
        }

        var children = parent.Children.ToList();

        foreach (var child in children)
        {
            await RemoveChildrenFromDatabaseAsync(child);
            DbContext.Pinecone.Remove(child);
        }
    }

    private async Task<Pinecone> CreateNodeRecursivelyAsync(
        PineconeDto nodeDto,
        Guid? parentGuid,
        Dictionary<Guid, PineconeDto> nodeMap,
        string userName,
        Guid groupGuid)
    {
        var existingPinecone = await DbContext.Pinecone.SingleOrDefaultAsync(p => p.Guid == nodeDto.Guid);

        Pinecone pinecone;
        if (existingPinecone != null && existingPinecone.UserName == userName)
        {
            pinecone = existingPinecone;
            pinecone.Title = nodeDto.Title;
            pinecone.Content = nodeDto.Content;
            pinecone.ParentGuid = parentGuid;
            pinecone.Order = nodeDto.Order;
            pinecone.Update = DateTime.UtcNow;
        }
        else
        {
            pinecone = new Pinecone
            {
                Id = 0,
                Title = nodeDto.Title,
                Content = nodeDto.Content,
                GroupGuid = groupGuid,
                ParentGuid = parentGuid,
                Order = nodeDto.Order,
                UserName = userName,
                Guid = nodeDto.Guid != Guid.Empty ? nodeDto.Guid : Guid.NewGuid(),
                IsPublic = false,
                Create = DateTime.UtcNow,
                Update = DateTime.UtcNow,
            };
            await DbContext.Pinecone.AddAsync(pinecone);
        }

        await DbContext.SaveChangesAsync();

        var childrenNodes = nodeMap.Values
            .Where(n => n.ParentGuid == nodeDto.Guid)
            .OrderBy(n => n.Order)
            .ToList();

        foreach (var childNode in childrenNodes)
        {
            await CreateNodeRecursivelyAsync(childNode, pinecone.Guid, nodeMap, userName, groupGuid);
        }

        return pinecone;
    }

    private async Task UpdateNodesAsync(List<PineconeDto> nodes, string userName)
    {
        foreach (var node in nodes)
        {
            var dbNode = await DbContext.Pinecone.SingleOrDefaultAsync(p => p.Guid == node.Guid);
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

    private async Task<Pinecone> SingleIncludeChild(Guid id)
    {
        var userName = User.Identity?.Name ?? "";
        var parent = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Guid == id)
                    ?? throw new KeyNotFoundException($"Pinecone with ID {id} not found.");
        if (parent.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        return await LoadChildrenRecursively(parent);
    }

    private IQueryable<Pinecone> GetUserTopList(string userName)
        => DbContext.Pinecone
            .Where(x => x.UserName == userName)
            .Where(x => x.ParentGuid == null);

    private async Task<Pinecone> LoadChildrenRecursively(Pinecone parent)
    {
        await DbContext.Entry(parent)
            .Collection(p => p.Children)
            .Query()
            .OrderBy(x => x.Order)
            .LoadAsync();

        foreach (var child in parent.Children)
        {
            await LoadChildrenRecursively(child);
        }
        return parent;
    }

    private async Task<Pinecone?> GetPineconeAndVerifyOwnership(Guid guid) 
        => await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Guid == guid);
}
