using System.ComponentModel.DataAnnotations;

namespace Pinetree.Model;

public class Pinecone
{
    [Key]
    public long Id { get; set; }
    [MaxLength(200)]
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required long GroupId { get; set; }
    // DB的にここはNULLに出来た方が便利なのでlong?を許容することにした
    // そもそも最親はParentがない。最初の１個を作る時にダミーデータが必要になるが制約によりエラーになる
    public required long? ParentId { get; set; }
    [MaxLength(256)]
    public required string UserId { get; set; }
    public Pinecone? Parent { get; set; }
    public ICollection<Pinecone> Children { get; set; } = [];
}
