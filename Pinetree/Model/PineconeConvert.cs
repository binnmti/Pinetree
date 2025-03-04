using System.Diagnostics;

namespace Pinetree.Model;

public static class PineconeConvert
{
    public static PineTree ToPineTree(List<Pinecone> pinecones)
    {
        var pinecone = pinecones.SingleOrDefault(p => p.ParentId == null);
        Debug.Assert(pinecone != null);

        var pinetree = pinecone.ToPineTree();
        pinetree.Create(pinetree.Id, pinecones);
        return pinetree;
    }

    private static void Create(this PineTree pinetree, long id, ICollection<Pinecone> pinecones)
    {
        foreach (var pinecone in pinecones)
        {
            if (pinecone.ParentId != id) continue;

            var current = pinecone.ToPineTree();
            pinetree.Children.Add(current);
            current.Create(current.Id, pinecone.Children);
        }
    }

    private static PineTree ToPineTree(this Pinecone pinecone)
        => new(pinecone.Id, pinecone.Title, pinecone.Content, pinecone.ParentId ?? -1, pinecone.GroupId);
}
