namespace Pinetree.Model;

public static class PineconeConvert
{
    public static PineTree ToPineTree(this Pinecone pinecone, PineTree? parent)
        => new(pinecone.Id, pinecone.Title, pinecone.Content, parent, pinecone.GroupId);

    public static PineTree ToPineTreeIncludeChild(this Pinecone pinecone)
    {
        var pinetree = pinecone.ToPineTree(null);
        CreateChild(pinetree, pinetree, pinecone.Children);
        return pinetree;
    }

    private static void CreateChild(PineTree targetTree, PineTree parent, ICollection<Pinecone> pinecones)
    {
        foreach (var pinecone in pinecones)
        {
            var current = pinecone.ToPineTree(parent);
            targetTree.Children.Add(current);
            CreateChild(current, current, pinecone.Children);
        }
    }
}
