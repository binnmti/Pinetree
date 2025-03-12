namespace Pinetree.Model;

public static class PinetreeUpdater
{
    public static PineTree SetCurrentIncludeChild(this PineTree pineTree, long id)
    {
        PineTree? result = null;
        pineTree.IsCurrent = pineTree.Id == id;
        if (pineTree.IsCurrent)
        {
            result = pineTree;
        }
        foreach (var child in pineTree.Children)
        {
            var childResult = child.SetCurrentIncludeChild(id);
            if (childResult != null)
            {
                result = childResult;
            }
        }
        // This method assumes the provided ID always exists somewhere in the tree.
        // While recursive calls may return null, the top-level call is guaranteed 
        // to find a match, so we use the null-forgiving operator.
        return result!;
    }

    public static long DeleteIncludeChild(this PineTree tree)
    {
        DeleteChildren(tree);
        tree.Parent?.Children.Remove(tree);
        return tree.Parent?.Id ?? -1;
    }

    private static void DeleteChildren(this PineTree tree)
    {
        foreach (var child in tree.Children)
        {
            DeleteChildren(child);
        }
        tree.Children.Clear();
    }
}
