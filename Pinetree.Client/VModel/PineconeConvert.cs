using Pinetree.Shared.Model;

namespace Pinetree.Client.VModel;

public static class PineconeConvert
{
    public static PinetreeView ToPinetree(this Pinecone pinecone, PinetreeView? parent)
        => new(pinecone.Id, pinecone.Title, pinecone.Content, parent, pinecone.GroupId);

    public static (PinetreeView, int) ToPinetreeIncludeChild(this Pinecone pinecone)
    {
        var pinetree = pinecone.ToPinetree(null);
        var fileCount = 0;
        CreateChild(pinetree, pinetree, pinecone.Children, ref fileCount, 1);
        return (pinetree, fileCount);
    }

    private static void CreateChild(PinetreeView targetTree, PinetreeView parent, ICollection<Pinecone> pinecones, ref int fileCount, int currentDepth)
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
