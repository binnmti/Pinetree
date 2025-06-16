using Pinetree.Shared.ViewModels;

namespace Pinetree.Client.VModel;

public static class PineconeConvert
{
    public static PinetreeView ToPinetree(this PineconeViewModel pinecone, PinetreeView? parent)
        => new(pinecone.Guid, pinecone.Title, pinecone.Content, parent, pinecone.GroupGuid, pinecone.IsPublic);

    public static (PinetreeView, int) ToPinetreeIncludeChild(this PineconeViewModelWithChildren pinecone)
    {
        var pinetree = pinecone.ToPinetree(null);
        var fileCount = 0;
        CreateChildFromHierarchical(pinetree, pinetree, pinecone.Children, ref fileCount, 1);
        return (pinetree, fileCount);
    }

    private static void CreateChild(PinetreeView targetTree, PinetreeView parent, List<PineconeViewModel> pinecones, List<PineconeViewModel> allPinecones, ref int fileCount, int currentDepth)
    {
        foreach (var pinecone in pinecones)
        {
            var child = pinecone.ToPinetree(parent);
            parent.Children.Add(child);
            fileCount++;

            var grandChildren = allPinecones.Where(p => p.ParentGuid == pinecone.Guid).ToList();
            if (grandChildren.Any())
            {
                CreateChild(targetTree, child, grandChildren, allPinecones, ref fileCount, currentDepth + 1);
            }
        }
    }

    // Extension method for hierarchical ViewModels
    private static PinetreeView ToPinetree(this PineconeViewModelWithChildren pinecone, PinetreeView? parent)
        => new(pinecone.Guid, pinecone.Title, pinecone.Content, parent, pinecone.GroupGuid, pinecone.IsPublic);

    private static void CreateChildFromHierarchical(PinetreeView targetTree, PinetreeView parent, List<PineconeViewModelWithChildren> children, ref int fileCount, int currentDepth)
    {
        foreach (var child in children)
        {
            var childView = child.ToPinetree(parent);
            parent.Children.Add(childView);
            fileCount++;

            if (child.Children.Any())
            {
                CreateChildFromHierarchical(targetTree, childView, child.Children, ref fileCount, currentDepth + 1);
            }
        }
    }
}
