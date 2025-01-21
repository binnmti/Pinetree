namespace Pinetree.Model;

public static class PinneconeConvert
{
    public static PineTree ToPineTree(long id, List<Pinecone> pinecones)
    {
        var pinetree = pinecones.Single(p => p.Id == id).ToPineTree();
        pinetree.Create(pinecones);
        return pinetree;
    }

    private static void Create(this PineTree pinetree, ICollection<Pinecone> pinecones)
    {
        foreach (var pinecone in pinecones)
        {
            pinetree.Children.Add(pinecone.ToPineTree());
            Create(pinetree, pinecone.Children);
        }
    }

    private static PineTree ToPineTree(this Pinecone pinecone)
        => new(pinecone.Id, pinecone.Title, pinecone.Content);
}
