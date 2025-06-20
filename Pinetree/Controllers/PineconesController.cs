using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;
using Pinetree.Models;
using Pinetree.Services;
using Pinetree.Shared.ViewModels;

namespace Pinetree.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PineconesController(ApplicationDbContext context, IEncryptionService encryptionService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private const string Untitled = "Untitled";
    private ApplicationDbContext DbContext { get; } = context;
    private IEncryptionService EncryptionService { get; } = encryptionService;
    private UserManager<ApplicationUser> UserManager { get; } = userManager;    /// <summary>
    /// Gets the current user ID for encryption operations
    /// </summary>
    private async Task<string> GetCurrentUserIdAsync()
    {
        var user = await UserManager.GetUserAsync(User);
        return user?.Id ?? throw new UnauthorizedAccessException("User not found");
    }

    /// <summary>
    /// Helper method to encrypt content if it's private
    /// </summary>
    private string? EncryptContentIfPrivate(string? content, bool isPublic)
    {
        if (isPublic || string.IsNullOrEmpty(content))
        {
            return content;
        }

        // Check if already encrypted
        if (EncryptionService.CanDecrypt(content))
        {
            return content;
        }

        return EncryptionService.Encrypt(content);
    }

    /// <summary>
    /// Helper method to decrypt content if it's private and encrypted
    /// </summary>
    private string? DecryptContentIfPrivate(string? content, bool isPublic)
    {
        if (isPublic || string.IsNullOrEmpty(content))
        {
            return content;
        }

        // Check if encrypted
        if (EncryptionService.CanDecrypt(content))
        {
            return EncryptionService.Decrypt(content);
        }

        // Legacy plain text data
        return content;
    }

    /// <summary>
    /// Converts Pinecone model to PineconeViewModel
    /// </summary>
    private static PineconeViewModel ToPineconeViewModel(Pinecone model)
    {
        return new PineconeViewModel
        {
            Guid = model.Guid,
            Title = model.Title,
            Content = model.Content,
            GroupGuid = model.GroupGuid,
            ParentGuid = model.ParentGuid,
            Order = model.Order,
            IsPublic = model.IsPublic,
            UserName = model.UserName,
            Create = model.Create,
            Update = model.Update
        };
    }
    
    /// <summary>
    /// Converts Pinecone model with children to PineconeViewModelWithChildren for hierarchical data
    /// </summary>
    private PineconeViewModelWithChildren ToPineconeViewModelWithChildren(Pinecone model)
    {
        // Decrypt content
        var decryptedTitle = DecryptContentIfPrivate(model.Title, model.IsPublic);
        var decryptedContent = DecryptContentIfPrivate(model.Content, model.IsPublic);

        var viewModel = new PineconeViewModelWithChildren
        {
            Guid = model.Guid,
            Title = decryptedTitle ?? "",
            Content = decryptedContent ?? "",
            GroupGuid = model.GroupGuid,
            ParentGuid = model.ParentGuid,
            Order = model.Order,
            IsPublic = model.IsPublic,
            UserName = model.UserName,
            Create = model.Create,
            Update = model.Update,
            Children = new List<PineconeViewModelWithChildren>()
        };

        // Convert children recursively
        foreach (var child in model.Children.OrderBy(c => c.Order))
        {
            var childViewModel = ToPineconeViewModelWithChildren(child);
            viewModel.Children.Add(childViewModel);
        }

        return viewModel;
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
    public async Task<PineconeViewModel> AddTop()
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

        var maxOrder = await DbContext.Pinecone
            .Where(p => p.UserName == userName && p.ParentGuid == null)
            .MaxAsync(p => (int?)p.Order) ?? -1;
        maxOrder += 1;

        var guid = Guid.NewGuid();
        var pinecone = new Pinecone
        {
            Title = EncryptContentIfPrivate(title, false)!, // New documents are private by default
            Content = EncryptContentIfPrivate("", false)!,
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
        
        // Return decrypted data to client
        pinecone.Title = DecryptContentIfPrivate(pinecone.Title, pinecone.IsPublic)!;
        pinecone.Content = DecryptContentIfPrivate(pinecone.Content, pinecone.IsPublic)!;

        return ToPineconeViewModel(pinecone);
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
    public async Task<PineconeViewModelWithChildren?> GetIncludeChild(Guid guid)
    {
        var userId = await GetCurrentUserIdAsync();
        var pinecone = await GetPineconeById(guid);
        var rootPinecone = await GetPineconeById(pinecone?.GroupGuid ?? Guid.Empty);
        if (rootPinecone == null) return null;
        var loadedPinecone = await LoadChildrenRecursively(rootPinecone);
        return ToPineconeViewModelWithChildren(loadedPinecone);
    }
    [HttpGet("get-view-include-child/{guid}")]
    [AllowAnonymous]
    public async Task<PineconeViewModelWithChildren?> GetViewIncludeChild(Guid guid)
    {
        var pinecone = await GetPineconeById(guid);
        if (pinecone == null || !pinecone.IsPublic) return null;
        var rootPinecone = await GetPineconeById(pinecone.GroupGuid);
        if (rootPinecone == null) return null;        // For public content, we can use the decryption helper
        var loadedPinecone = await LoadChildrenRecursively(rootPinecone);
        return ToPineconeViewModelWithChildren(loadedPinecone);
    }

    [HttpGet("get-user-top-list")]
    public async Task<List<PineconeViewModel>> GetUserTopList([FromQuery] int pageNumber, [FromQuery] int pageSize)
    {
        var userName = User.Identity?.Name ?? "";
        var userId = await GetCurrentUserIdAsync();
        var documents = await GetUserTopList(userName)
            .AsTracking()
            .OrderBy(p => p.Order)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();        // Decrypt content and convert to ViewModels
        var result = new List<PineconeViewModel>();
        foreach (var doc in documents)
        {
            doc.Title = DecryptContentIfPrivate(doc.Title, doc.IsPublic)!;
            doc.Content = DecryptContentIfPrivate(doc.Content, doc.IsPublic)!;
            result.Add(ToPineconeViewModel(doc));
        }

        return result;
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
        {            // Decrypt current content
            var currentTitle = DecryptContentIfPrivate(pinecone.Title, pinecone.IsPublic);
            var currentContent = DecryptContentIfPrivate(pinecone.Content, pinecone.IsPublic);
            // Re-encrypt with new visibility setting
            pinecone.Title = EncryptContentIfPrivate(currentTitle, request.IsPublic)!;
            pinecone.Content = EncryptContentIfPrivate(currentContent, request.IsPublic)!;;
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

    private async Task RebuildTreeAsync(Guid rootId, List<PineconeUpdateRequest> nodes, string userName, string userId)
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
            }            currentTree.Title = EncryptContentIfPrivate(rootNodeDto.Title, currentTree.IsPublic)!;
            currentTree.Content = EncryptContentIfPrivate(rootNodeDto.Content, currentTree.IsPublic)!;
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
      PineconeUpdateRequest nodeDto,
      Guid? parentGuid,
      Dictionary<Guid, PineconeUpdateRequest> nodeMap,
      string userName,
      Guid groupGuid,
      string userId)
    {
        var existingPinecone = await DbContext.Pinecone.SingleOrDefaultAsync(p => p.Guid == nodeDto.Guid);
        Pinecone pinecone;
        if (existingPinecone != null && existingPinecone.UserName == userName)
        {
            pinecone = existingPinecone;
            pinecone.Title = EncryptContentIfPrivate(nodeDto.Title, pinecone.IsPublic)!;
            pinecone.Content = EncryptContentIfPrivate(nodeDto.Content, pinecone.IsPublic)!;
            pinecone.ParentGuid = parentGuid;
            pinecone.Order = nodeDto.Order;
            pinecone.Update = DateTime.UtcNow;
        }
        else
        {
            pinecone = new Pinecone
            {
                Id = 0,
                Title = EncryptContentIfPrivate(nodeDto.Title, false)!, // New nodes are private by default
                Content = EncryptContentIfPrivate(nodeDto.Content, false)!,
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

    private async Task UpdateNodesAsync(List<PineconeUpdateRequest> nodes, string userName, string userId)
    {
        foreach (var node in nodes)
        {
            var dbNode = await DbContext.Pinecone.SingleOrDefaultAsync(p => p.Guid == node.Guid);
            if (dbNode == null || dbNode.UserName != userName)
            {
                continue;
            }
            dbNode.Title = EncryptContentIfPrivate(node.Title, dbNode.IsPublic)!;
            dbNode.Content = EncryptContentIfPrivate(node.Content, dbNode.IsPublic)!;;
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
        return await LoadChildrenRecursively(parent);
    }

    private IQueryable<Pinecone> GetUserTopList(string userName)
        => DbContext.Pinecone
            .Where(x => x.UserName == userName)
            .Where(x => x.ParentGuid == null);
    
    private async Task<Pinecone> LoadChildrenRecursively(Pinecone parent)
    {
        // Decrypt parent content
        parent.Title = DecryptContentIfPrivate(parent.Title, parent.IsPublic)!;
        parent.Content = DecryptContentIfPrivate(parent.Content, parent.IsPublic)!;

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
        // Decrypt content and convert to ViewModels
        var userId = await GetCurrentUserIdAsync();
        var documentViewModels = new List<PineconeViewModel>();        foreach (var doc in documents)
        {
            doc.Title = DecryptContentIfPrivate(doc.Title, doc.IsPublic)!;
            doc.Content = DecryptContentIfPrivate(doc.Content, doc.IsPublic)!;
            documentViewModels.Add(ToPineconeViewModel(doc));
        }

        return new UserDocumentsResponse
        {
            Documents = documentViewModels,
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
        public List<PineconeViewModel> Documents { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
