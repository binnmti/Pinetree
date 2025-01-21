using System.ComponentModel.DataAnnotations;

namespace Pinetree.Model;

public class Pinecone
{
    [Key]
    public long Id { get; set; }
    [MaxLength(200)]
    public required string Title { get; set; } = "";
    public required string Content { get; set; } = "";
    public required int ParentId { get; set; } = -1;
    public required long GroupId { get; set; } = -1;

    //
    public Pinecone? Parent { get; set; }
    public ICollection<Pinecone> Children { get; set; } = [];
}
