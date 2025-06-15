using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Services;
using Pinetree.Shared.Model;

namespace Pinetree.Components.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PineconesController(ApplicationDbContext context, EncryptionService encryptionService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private const string Untitled = "Untitled";
    private ApplicationDbContext DbContext { get; } = context;
    private EncryptionService EncryptionService { get; } = encryptionService;
    private UserManager<ApplicationUser> UserManager { get; } = userManager;

    /// <summary>
    /// Gets the current user ID for encryption operations
    /// </summary>
    private async Task<string> GetCurrentUserIdAsync()
    {
        var user = await UserManager.GetUserAsync(User);
        return user?.Id ?? throw new UnauthorizedAccessException("User not found");
    }
    
    [HttpPost("update-tree")]
    public async Task<IActionResult> UpdateTree([FromBody] TreeUpdateRequest request)
    {
        var userName = User.Identity?.Name ?? "";
        var userId = await GetCurrentUserIdAsync();
        await GetPineconeById(request.RootId);
        if (request.HasStructuralChanges)
        {
            await RebuildTreeAsync(request.RootId, request.Nodes, userName, userId);
        }
        else
        {
            await UpdateNodesAsync(request.Nodes, userName, userId);
        }

        return Ok();
    }
    
    [HttpPost("add-top")]
    public async Task<Pinecone> AddTop()
    {
        var userName = User.Identity?.Name ?? "";
        var userId = await GetCurrentUserIdAsync();

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
            Title = (await EncryptionService.EncryptContentAsync(title, false, userId))!, // New documents are private by default
            Content = (await EncryptionService.EncryptContentAsync("", false, userId))!,
            GroupGuid = guid,
            ParentGuid = null,
            Order = maxOrder,
            UserName = userName,
            Guid = guid,
            IsPublic = false,
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
        };await DbContext.Pinecone.AddAsync(pinecone);
        await DbContext.SaveChangesAsync();
        
        // Return decrypted data to client
        pinecone.Title = (await EncryptionService.DecryptContentAsync(pinecone.Title, pinecone.IsPublic, userId))!;
        pinecone.Content = (await EncryptionService.DecryptContentAsync(pinecone.Content, pinecone.IsPublic, userId))!;
        
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
        var userId = await GetCurrentUserIdAsync();
        var pinecone = await GetPineconeById(guid);
        var rootPinecone = await GetPineconeById(pinecone?.GroupGuid ?? Guid.Empty);
        if (rootPinecone == null) return Pinecone.None;
        return await LoadChildrenRecursively(rootPinecone, userId);
    }
    
    [HttpGet("get-view-include-child/{guid}")]
    [AllowAnonymous]
    public async Task<Pinecone?> GetViewIncludeChild(Guid guid)
    {
        var pinecone = await GetPineconeById(guid);
        if (pinecone == null || !pinecone.IsPublic) return Pinecone.None;
        var rootPinecone = await GetPineconeById(pinecone.GroupGuid);
        if (rootPinecone == null) return Pinecone.None;
        // For public content, we can use a dummy userId since public content is not encrypted
        return await LoadChildrenRecursively(rootPinecone, "anonymous");
    }
    
    [HttpGet("get-user-top-list")]
    public async Task<List<Pinecone>> GetUserTopList([FromQuery] int pageNumber, [FromQuery] int pageSize)
    {
        var userName = User.Identity?.Name ?? "";
        var userId = await GetCurrentUserIdAsync();
        var documents = await GetUserTopList(userName)
            .OrderBy(p => p.Order)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Decrypt content for client
        foreach (var doc in documents)
        {
            doc.Title = (await EncryptionService.DecryptContentAsync(doc.Title, doc.IsPublic, userId))!;
            doc.Content = (await EncryptionService.DecryptContentAsync(doc.Content, doc.IsPublic, userId))!;
        }

        return documents;
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
        var userId = await GetCurrentUserIdAsync();
        var pinecone = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Guid == guid)
            ?? throw new KeyNotFoundException($"Pinecone with ID {guid} not found.");

        if (pinecone.UserName != userName)
        {
            return Unauthorized("You do not own this document.");
        }

        // If changing from private to public, decrypt first
        // If changing from public to private, encrypt
        if (pinecone.IsPublic != request.IsPublic)
        {
            // Decrypt current content
            var currentTitle = await EncryptionService.DecryptContentAsync(pinecone.Title, pinecone.IsPublic, userId);
            var currentContent = await EncryptionService.DecryptContentAsync(pinecone.Content, pinecone.IsPublic, userId);
              // Re-encrypt with new visibility setting
            pinecone.Title = (await EncryptionService.EncryptContentAsync(currentTitle, request.IsPublic, userId))!;
            pinecone.Content = (await EncryptionService.EncryptContentAsync(currentContent, request.IsPublic, userId))!;
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
    
    private async Task RebuildTreeAsync(Guid rootId, List<PineconeDto> nodes, string userName, string userId)
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
            currentTree.Title = (await EncryptionService.EncryptContentAsync(rootNodeDto.Title, currentTree.IsPublic, userId))!;
            currentTree.Content = (await EncryptionService.EncryptContentAsync(rootNodeDto.Content, currentTree.IsPublic, userId))!;
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
                await CreateNodeRecursivelyAsync(childNode, currentTree.Guid, nodeMap, userName, currentTree.GroupGuid, userId);
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
        Guid groupGuid,
        string userId)
    {
        var existingPinecone = await DbContext.Pinecone.SingleOrDefaultAsync(p => p.Guid == nodeDto.Guid);
        Pinecone pinecone;
        if (existingPinecone != null && existingPinecone.UserName == userName)
        {
            pinecone = existingPinecone;
            pinecone.Title = (await EncryptionService.EncryptContentAsync(nodeDto.Title, pinecone.IsPublic, userId))!;
            pinecone.Content = (await EncryptionService.EncryptContentAsync(nodeDto.Content, pinecone.IsPublic, userId))!;
            pinecone.ParentGuid = parentGuid;
            pinecone.Order = nodeDto.Order;
            pinecone.Update = DateTime.UtcNow;
        }
        else
        {
            pinecone = new Pinecone
            {
                Id = 0,
                Title = (await EncryptionService.EncryptContentAsync(nodeDto.Title, false, userId))!, // New nodes are private by default
                Content = (await EncryptionService.EncryptContentAsync(nodeDto.Content, false, userId))!,
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
            await CreateNodeRecursivelyAsync(childNode, pinecone.Guid, nodeMap, userName, groupGuid, userId);
        }

        return pinecone;
    }
    
    private async Task UpdateNodesAsync(List<PineconeDto> nodes, string userName, string userId)
    {
        foreach (var node in nodes)
        {
            var dbNode = await DbContext.Pinecone.SingleOrDefaultAsync(p => p.Guid == node.Guid);
            if (dbNode == null || dbNode.UserName != userName)
            {
                continue;
            }
            dbNode.Title = (await EncryptionService.EncryptContentAsync(node.Title, dbNode.IsPublic, userId))!;
            dbNode.Content = (await EncryptionService.EncryptContentAsync(node.Content, dbNode.IsPublic, userId))!;
            dbNode.Order = node.Order;
            dbNode.Update = DateTime.UtcNow;
        }

        await DbContext.SaveChangesAsync();
    }
    
    private async Task<Pinecone> SingleIncludeChild(Guid id)
    {
        var userName = User.Identity?.Name ?? "";
        var userId = await GetCurrentUserIdAsync();
        var parent = await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Guid == id)
                    ?? throw new KeyNotFoundException($"Pinecone with ID {id} not found.");
        if (parent.UserName != userName)
        {
            throw new UnauthorizedAccessException("You do not own this Pinecone.");
        }
        return await LoadChildrenRecursively(parent, userId);
    }

    private IQueryable<Pinecone> GetUserTopList(string userName)
        => DbContext.Pinecone
            .Where(x => x.UserName == userName)
            .Where(x => x.ParentGuid == null);
    
    private async Task<Pinecone> LoadChildrenRecursively(Pinecone parent, string userId)
    {
        // Decrypt parent content
        parent.Title = (await EncryptionService.DecryptContentAsync(parent.Title, parent.IsPublic, userId))!;
        parent.Content = (await EncryptionService.DecryptContentAsync(parent.Content, parent.IsPublic, userId))!;

        await DbContext.Entry(parent)
            .Collection(p => p.Children)
            .Query()
            .OrderBy(x => x.Order)
            .LoadAsync();

        foreach (var child in parent.Children)
        {
            await LoadChildrenRecursively(child, userId);
        }
        return parent;
    }

    private async Task<Pinecone?> GetPineconeById(Guid guid) 
        => await DbContext.Pinecone.SingleOrDefaultAsync(x => x.Guid == guid);

    [HttpGet("get-user-documents")]
    public async Task<UserDocumentsResponse> GetUserDocuments([FromQuery] UserDocumentsRequest request)
    {
        var userName = User.Identity?.Name ?? "";
        var query = GetUserTopList(userName);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p => 
                EF.Functions.Like(p.Title, $"%{request.Search}%") ||
                EF.Functions.Like(p.Content, $"%{request.Search}%"));
        }

        // Apply public-only filter
        if (request.PublicOnly)
        {
            query = query.Where(p => p.IsPublic);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "title" => request.SortDescending 
                ? query.OrderByDescending(p => p.Title) 
                : query.OrderBy(p => p.Title),
            "createdat" => request.SortDescending 
                ? query.OrderByDescending(p => p.Create) 
                : query.OrderBy(p => p.Create),
            "updatedat" => request.SortDescending 
                ? query.OrderByDescending(p => p.Update) 
                : query.OrderBy(p => p.Update),
            _ => request.SortDescending 
                ? query.OrderByDescending(p => p.Update) 
                : query.OrderBy(p => p.Update)
        };

        // Get total count before pagination
        var totalRecords = await query.CountAsync();
        
        // Apply pagination
        var documents = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
        
        // Decrypt content for client
        var userId = await GetCurrentUserIdAsync();
        foreach (var doc in documents)
        {
            doc.Title = (await EncryptionService.DecryptContentAsync(doc.Title, doc.IsPublic, userId))!;
            doc.Content = (await EncryptionService.DecryptContentAsync(doc.Content, doc.IsPublic, userId))!;
        }

        return new UserDocumentsResponse
        {
            Documents = documents,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize),
            CurrentPage = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public class UserDocumentsRequest
    {
        public string Search { get; set; } = "";
        public bool PublicOnly { get; set; } = false;
        public string SortBy { get; set; } = "updatedat";
        public bool SortDescending { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class UserDocumentsResponse
    {
        public List<Pinecone> Documents { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
