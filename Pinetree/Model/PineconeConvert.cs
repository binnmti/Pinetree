namespace Pinetree.Model;

public static class PineconeConvert
{
    public static PineTree ToPineTree(this Pinecone pinecone, PineTree? parent)
        => new(pinecone.Id, pinecone.Title, pinecone.Content, parent, pinecone.GroupId);

    public static (PineTree,int,int) ToPineTreeIncludeChild(this Pinecone pinecone)
    {
        var pinetree = pinecone.ToPineTree(null);
        int fileCount = 0;
        int maxDepth = 0;
        CreateChild(pinetree, pinetree, pinecone.Children, ref fileCount, 1, ref maxDepth);
        return (pinetree, fileCount, maxDepth);
    }

    private static void CreateChild(PineTree targetTree, PineTree parent, ICollection<Pinecone> pinecones, ref int fileCount, int currentDepth, ref int maxDepth)
    {
        if (currentDepth > maxDepth)
        {
            maxDepth = currentDepth;
        }
        foreach (var pinecone in pinecones)
        {
            var current = pinecone.ToPineTree(parent);
            targetTree.Children.Add(current);
            fileCount++;
            CreateChild(current, current, pinecone.Children, ref fileCount, currentDepth + 1, ref maxDepth);
        }
    }
}
