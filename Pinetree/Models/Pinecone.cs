using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Pinetree.Models;

[Index(nameof(Guid), IsUnique = true)]
public class Pinecone
{
    [Key]
    public long Id { get; set; }
    public required Guid Guid { get; set; }
    public required Guid GroupGuid { get; set; }
    // From a database perspective, it is more convenient to set this to NULL, so we decided to allow long?
    public required Guid? ParentGuid { get; set; }
    
    [MaxLength(1000)]
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required int Order { get; set; }
    public required bool IsPublic { get; set; }

    [MaxLength(256)]
    public required string UserName { get; set; }
    public Pinecone? Parent { get; set; }
    public ICollection<Pinecone> Children { get; set; } = [];

    public required DateTime Create { get; set; } = DateTime.UtcNow;
    public required DateTime Update { get; set; } = DateTime.UtcNow;    // Trash/Soft Delete properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string DeleteType { get; set; } = "single"; // "single" or "bulk"

    public static readonly Pinecone None = new()
    {
        Title = null!,
        Content = null!,
        GroupGuid = default,
        ParentGuid = null,
        Order = default,
        UserName = null!,
        Guid = default,
        IsPublic = default,
        Create = default,
        Update = default,
    };
}
