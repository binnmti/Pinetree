using System.Diagnostics;

namespace Pinetree.Model;

public static class PinneconeConvert
{
    public static PineTree ToPineTree(long currentId, List<Pinecone> pinecones)
    {
        var pinecone = pinecones.SingleOrDefault(p => p.ParentId == null);
        if (pinecone == null)
        {
            Debug.Assert(pinecones.Count != 0);
            return ToPineTree(pinecones.FirstOrDefault());
        }
        else
        {
            var pinetree = pinecone.ToPineTree();
            pinetree.Create(currentId, pinecones);
            return pinetree;
        }
    }

    private static void Create(this PineTree pinetree, long currentId, ICollection<Pinecone> pinecones)
    {
        if (pinetree.Id == currentId)
        {
            pinetree.IsCurrent = true;
        }
        foreach (var pinecone in pinecones)
        {
            if (pinecone.ParentId != pinetree.Id) continue;
            pinetree.Children.Add(pinecone.ToPineTree());
            Create(pinetree, currentId, pinecone.Children);
        }
    }

    private static PineTree ToPineTree(this Pinecone pinecone)
        => new(pinecone.Id, pinecone.Title, pinecone.Content, pinecone.ParentId);
}
