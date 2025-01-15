using System.ComponentModel.DataAnnotations;

namespace Pinetree.Model;

public class Pinecone
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";
    [Required]
    public string Content { get; set; } = "";

    public int? ParentId { get; set; }
    public Pinecone? Parent { get; set; }

    public ICollection<Pinecone> Children { get; set; } = [];
}
