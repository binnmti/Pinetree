using PinetreeModel;

namespace Pinetree.Client.Model;

public static class PineconeConvert
{
    public static Pinetree ToPinetree(this Pinecone pinecone, Pinetree? parent)
        => new(pinecone.Id, pinecone.Title, pinecone.Content, parent, pinecone.GroupId);

    public static (Pinetree, int) ToPinetreeIncludeChild(this Pinecone pinecone)
    {
        var pinetree = pinecone.ToPinetree(null);
        var fileCount = 0;
        CreateChild(pinetree, pinetree, pinecone.Children, ref fileCount, 1);
        return (pinetree, fileCount);
    }

    private static void CreateChild(Pinetree targetTree, Pinetree parent, ICollection<Pinecone> pinecones, ref int fileCount, int currentDepth)
    {
        foreach (var pinecone in pinecones)
        {
            var current = pinecone.ToPinetree(parent);
            targetTree.Children.Add(current);
            fileCount++;
            CreateChild(current, current, pinecone.Children, ref fileCount, currentDepth + 1);
        }
    }
}
