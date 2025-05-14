using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pinetree.Shared.Model;

[Index(nameof(Guid), IsUnique = true)] // Move the Index attribute to the class level
public class Pinecone
{
    [Key]
    public long Id { get; set; }
    public required Guid Guid { get; set; }
    public required Guid GroupGuid { get; set; }
    // From a database perspective, it is more convenient to set this to NULL, so we decided to allow long?    [ForeignKey("Parent")]
    public required Guid? ParentGuid { get; set; }
    [MaxLength(200)]
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required int Order { get; set; }
    public required bool IsPublic { get; set; }

    [MaxLength(256)]
    public required string UserName { get; set; }
    public Pinecone? Parent { get; set; }
    public ICollection<Pinecone> Children { get; set; } = [];

    public required DateTime Create { get; set; } = DateTime.UtcNow;
    public required DateTime Update { get; set; } = DateTime.UtcNow;
}
public class PineconeDto
{
    public Guid Guid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid GroupGuid { get; set; }
    public Guid? ParentGuid { get; set; }
    public int Order { get; set; }
}

public class TreeUpdateRequest
{
    public Guid RootId { get; set; }
    public bool HasStructuralChanges { get; set; }
    public List<PineconeDto> Nodes { get; set; } = [];
}

