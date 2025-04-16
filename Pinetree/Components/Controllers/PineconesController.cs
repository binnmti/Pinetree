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

        // 1. ルートノードの所有権確認
        var rootNode = await GetPineconeAndVerifyOwnership(request.RootId, userName);

        if (request.HasStructuralChanges)
        {
            // 2a. 構造変更がある場合：全削除全保存
            await RebuildTreeAsync(request.RootId, request.Nodes, userName);
        }
        else
        {
            // 2b. 構造変更がない場合：変更されたノードのみを更新
            await UpdateNodesAsync(request.Nodes, userName);
        }

        return Ok();
    }

    private async Task RebuildTreeAsync(long rootId, List<PineconeDto> nodes, string userName)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            // 1. 現在のツリーを論理削除
            var currentTree = await SingleIncludeChild(rootId);
            await LogicalDeleteTreeAsync(currentTree, userName);

            // 2. 新しいツリーを挿入
            await CreateNewTreeAsync(nodes, userName);

            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
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
            dbNode.Order = node.Order; // Order属性を更新
            dbNode.Update = DateTime.UtcNow;
        }

        await DbContext.SaveChangesAsync();
    }

    private async Task LogicalDeleteTreeAsync(Pinecone root, string userName)
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

    private async Task CreateNewTreeAsync(List<PineconeDto> nodes, string userName)
    {
        // ノード間の親子関係をマッピング
        var nodeMap = nodes.ToDictionary(n => n.Id, n => n);

        // ルートノードを特定して作成
        var rootNode = nodes.FirstOrDefault(n => n.ParentId == null);
        if (rootNode == null) return;

        // ルートから再帰的に作成
        await CreateNodeRecursivelyAsync(rootNode, null, nodeMap, userName);
    }

    private async Task<Pinecone> CreateNodeRecursivelyAsync(
        PineconeDto nodeDto,
        long? parentId,
        Dictionary<long, PineconeDto> nodeMap,
        string userName)
    {
        var pinecone = new Pinecone
        {
            Id = 0, // DB will assign new ID
            Title = nodeDto.Title,
            Content = nodeDto.Content,
            GroupId = nodeDto.GroupId,
            ParentId = parentId,
            Order = nodeDto.Order, // Set Order from DTO
            UserName = userName,
            IsDelete = false,
            Guid = Guid.NewGuid(),
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
            Delete = DateTime.UtcNow
        };

        await DbContext.Pinecone.AddAsync(pinecone);
        await DbContext.SaveChangesAsync(); // Save to get ID

        // Process child nodes in Order - get all children of this node and sort by Order
        var childrenNodes = nodeMap.Values
            .Where(n => n.ParentId == nodeDto.Id)
            .OrderBy(n => n.Order)
            .ToList();

        foreach (var childNode in childrenNodes)
        {
            await CreateNodeRecursivelyAsync(childNode, pinecone.Id, nodeMap, userName);
        }

        return pinecone;
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
            Order = maxOrder, // Set new item at the end
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

    [HttpPost("add-child")]
    public async Task<Pinecone> AddChild([FromBody] PineconeUpdateModel pinecone)
    {
        var userName = User.Identity?.Name ?? "";

        // Get maximum order for siblings
        int maxOrder = 0;
        if (pinecone.ParentId.HasValue)
        {
            maxOrder = (await DbContext.Pinecone
                .Where(p => p.ParentId == pinecone.ParentId && !p.IsDelete)
                .MaxAsync(p => (int?)p.Order)) ?? -1;
            maxOrder += 1;
        }

        var child = new Pinecone()
        {
            Title = pinecone.Title,
            Content = pinecone.Content,
            GroupId = pinecone.GroupId,
            ParentId = pinecone.ParentId,
            Order = pinecone.Order >= 0 ? pinecone.Order : maxOrder, // Use provided order or append at end
            UserName = userName,
            IsDelete = false,
            Guid = Guid.NewGuid(),
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
            Delete = DateTime.UtcNow,
        };
        await DbContext.Pinecone.AddAsync(child);
        await DbContext.SaveChangesAsync();
        return child;
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] PineconeUpdateModel pinecone)
    {
        var userName = User.Identity?.Name ?? "";
        var current = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Id == pinecone.Id)
            ?? throw new KeyNotFoundException($"Pinecone with ID {pinecone.Id} not found.");
        if (current.UserName != userName)
        {
            return Unauthorized("You do not own this Pinecone.");
        }

        // 親変更の検出
        bool parentChanged = current.ParentId != pinecone.ParentId;
        if (parentChanged)
        {
            // 新しい親の配下のノードの最大順序を取得
            int maxOrder = 0;
            if (pinecone.ParentId.HasValue)
            {
                maxOrder = (await DbContext.Pinecone
                    .Where(p => p.ParentId == pinecone.ParentId && !p.IsDelete)
                    .MaxAsync(p => (int?)p.Order)) ?? -1;
            }

            current.ParentId = pinecone.ParentId;
            current.Order = pinecone.Order >= 0 ? pinecone.Order : maxOrder; // 指定がない場合は末尾に追加
        }
        else if (current.Order != pinecone.Order)
        {
            // 順序変更の処理
            await ReorderSiblingsAsync(current, pinecone.Order);
        }

        current.Title = pinecone.Title;
        current.Content = pinecone.Content;
        current.Update = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();
        return Ok();
    }

    // 兄弟ノード間の順序を調整する内部メソッド
    private async Task ReorderSiblingsAsync(Pinecone node, int newOrder)
    {
        // 同じ親を持つノードを取得
        var siblings = await DbContext.Pinecone
            .Where(p => p.ParentId == node.ParentId && !p.IsDelete)
            .OrderBy(p => p.Order)
            .ToListAsync();

        // 現在のノードを除外
        siblings.Remove(node);

        // 新しい位置に挿入
        if (newOrder < 0)
            newOrder = 0;
        if (newOrder > siblings.Count)
            newOrder = siblings.Count;

        siblings.Insert(newOrder, node);

        // 順序を更新
        for (int i = 0; i < siblings.Count; i++)
        {
            siblings[i].Order = i;
        }
    }

    [HttpDelete("delete-include-child/{id}")]
    public async Task<IActionResult> DeleteIncludeChild(long id)
    {
        var userName = User.Identity?.Name ?? "";
        var pinecone = await SingleIncludeChild(id);
        if (pinecone.UserName != userName)
        {
            return Unauthorized("You do not own this Pinecone.");
        }

        var parentId = pinecone.ParentId;

        pinecone.Delete = DateTime.UtcNow;
        pinecone.IsDelete = true;
        await DeleteChildrenAsync(pinecone);

        // 削除されたノードの後続の兄弟ノードの順序を再調整
        if (parentId.HasValue)
        {
            await ReindexSiblingsAsync(parentId.Value);
        }

        await DbContext.SaveChangesAsync();
        return Ok();
    }

    // ノード削除後に兄弟ノードの順序を再インデックス化
    private async Task ReindexSiblingsAsync(long parentId)
    {
        var siblings = await DbContext.Pinecone
            .Where(p => p.ParentId == parentId && !p.IsDelete)
            .OrderBy(p => p.Order)
            .ToListAsync();

        for (int i = 0; i < siblings.Count; i++)
        {
            siblings[i].Order = i;
        }
    }

    private async Task DeleteChildrenAsync(Pinecone parent)
    {
        foreach (var child in parent.Children.ToList())
        {
            child.Delete = DateTime.UtcNow;
            child.IsDelete = true;
            await DeleteChildrenAsync(child);
        }
    }

    [HttpGet("get-include-child/{id}")]
    public async Task<Pinecone> GetIncludeChild(long id)
    {
        var userName = User.Identity?.Name ?? "";
        var pinecone = await GetPineconeAndVerifyOwnership(id, userName);
        var rootPinecone = await GetPineconeAndVerifyOwnership(pinecone.GroupId, userName);
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

    // ノードの順序を明示的に変更するためのエンドポイント
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderNodes([FromBody] ReorderRequest request)
    {
        var userName = User.Identity?.Name ?? "";

        // 親ノードの取得と所有権確認
        Pinecone? parentNode = null;
        if (request.ParentId.HasValue)
        {
            parentNode = await DbContext.Pinecone.FindAsync(request.ParentId.Value);
            if (parentNode == null || parentNode.UserName != userName)
            {
                return NotFound("Parent node not found or you don't have access.");
            }
        }

        // 並べ替え対象のノードを取得
        var nodes = await DbContext.Pinecone
            .Where(p => p.ParentId == request.ParentId && !p.IsDelete && request.NodeIds.Contains(p.Id))
            .ToListAsync();

        // 所有権確認
        if (nodes.Any(n => n.UserName != userName))
        {
            return Unauthorized("You do not own all of these nodes.");
        }

        // IDから順序マップを作成
        var orderMap = new Dictionary<long, int>();
        for (int i = 0; i < request.NodeIds.Count; i++)
        {
            orderMap[request.NodeIds[i]] = i;
        }

        // 各ノードの順序を更新
        foreach (var node in nodes)
        {
            if (orderMap.TryGetValue(node.Id, out int newOrder))
            {
                node.Order = newOrder;
            }
        }

        await DbContext.SaveChangesAsync();
        return Ok();
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

    private IQueryable<Pinecone> GetUserTopList(string userName)
        => DbContext.Pinecone
            .Where(x => x.UserName == userName)
            .Where(x => x.ParentId == null)
            .Where(x => x.IsDelete == false);

    private async Task<Pinecone> LoadChildrenRecursively(Pinecone parent)
    {
        // Order でソートして子ノードを読み込む
        await DbContext.Entry(parent)
            .Collection(p => p.Children)
            .Query()
            .Where(x => x.IsDelete == false)
            .OrderBy(x => x.Order) // Order でソート
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

    public class TreeUpdateRequest
    {
        public long RootId { get; set; }
        public bool HasStructuralChanges { get; set; }
        public List<PineconeDto> Nodes { get; set; } = new();
    }

    public class PineconeDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public long GroupId { get; set; }
        public long? ParentId { get; set; }
        public int Order { get; set; }
    }

    public record PineconeUpdateModel(long Id, string Title, string Content, long GroupId, long? ParentId, int Order = -1);

    public class ReorderRequest
    {
        public long? ParentId { get; set; }
        public List<long> NodeIds { get; set; } = new();
    }
}
