using System.Diagnostics;

namespace Pinetree.Model;

public static class PineconeConvert
{
    public static PineTree ToPineTree(List<Pinecone> pinecones)
    {
        var pinecone = pinecones.SingleOrDefault(p => p.ParentId == null);
        Debug.Assert(pinecone != null);

        var pinetree = pinecone.ToPineTree(null);
        pinetree.Create(pinetree, pinecones);
        return pinetree;
    }

    private static void Create(this PineTree pinetree, PineTree parent, ICollection<Pinecone> pinecones)
    {
        foreach (var pinecone in pinecones.Where(p => p.ParentId != null))
        {
            var current = pinecone.ToPineTree(parent);
            pinetree.Children.Add(current);
            current.Create(current, pinecone.Children);
        }
    }

    public static PineTree ToPineTree(this Pinecone pinecone, PineTree? parent)
        => new(pinecone.Id, pinecone.Title, pinecone.Content, parent, pinecone.GroupId);
}
