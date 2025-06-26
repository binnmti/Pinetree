namespace Pinetree.Shared.ViewModels;

public class PineconeViewModel
{
    public Guid Guid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid GroupGuid { get; set; }
    public Guid? ParentGuid { get; set; }
    public int Order { get; set; }
    public bool IsPublic { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime Create { get; set; }
    public DateTime Update { get; set; }
    
    // Trash/Soft Delete properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedParentTitle { get; set; }
    public string DeleteType { get; set; } = "single";
}

public class PineconeCreateRequest
{
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = string.Empty;
    public Guid GroupGuid { get; set; }
    public Guid? ParentGuid { get; set; }
    public bool IsPublic { get; set; }
}

public class PineconeUpdateRequest
{
    public Guid Guid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid GroupGuid { get; set; }
    public Guid? ParentGuid { get; set; }
    public int Order { get; set; }
    public bool IsPublic { get; set; }
}

public class PineconeViewModelWithChildren : PineconeViewModel
{
    public List<PineconeViewModelWithChildren> Children { get; set; } = new();
}

public class TreeUpdateRequest
{
    public Guid RootId { get; set; }
    public bool HasStructuralChanges { get; set; }
    public List<PineconeUpdateRequest> Nodes { get; set; } = [];
}
