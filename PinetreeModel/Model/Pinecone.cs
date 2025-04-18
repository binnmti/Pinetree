﻿using System.ComponentModel.DataAnnotations;

namespace Pinetree.Shared.Model;

public class Pinecone
{
    [Key]
    public long Id { get; set; }
    [MaxLength(200)]
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required long GroupId { get; set; }
    public required bool IsDelete { get; set; }

    // DB的にここはNULLに出来た方が便利なのでlong?を許容することにした
    // そもそも最親はParentがない。最初の１個を作る時にダミーデータが必要になるが制約によりエラーになる
    public required long? ParentId { get; set; }

    public required int Order { get; set; }

    public required Guid Guid { get; set; }

    [MaxLength(256)]
    public required string UserName { get; set; }
    public Pinecone? Parent { get; set; }
    public ICollection<Pinecone> Children { get; set; } = [];

    public required DateTime Create { get; set; } = DateTime.UtcNow;
    public required DateTime Update { get; set; } = DateTime.UtcNow;
    public required DateTime Delete { get; set; } = DateTime.UtcNow;
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

public class TreeUpdateRequest
{
    public long RootId { get; set; }
    public bool HasStructuralChanges { get; set; }
    public List<PineconeDto> Nodes { get; set; } = [];
}

