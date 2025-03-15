namespace Pinetree.Model;

public static class PinetreeUpdater
{
    public static int GetTotalFileCount(this PineTree node)
    {
        int count = 0;
        GetTotalFileCountChild(GetRoot(node), ref count);
        return count;
    }

    public static int GetDepth(this PineTree node)
    {
        int depth = 1;
        var parent = node.Parent;
        while (parent != null)
        {
            depth++;
            parent = parent.Parent;
        }
        return depth;
    }

    public static PineTree SetCurrentIncludeChild(this PineTree pineTree, long id, ref int fileCount, int currentDepth, ref int maxDepth)
    {
        PineTree? result = null;
        pineTree.IsCurrent = pineTree.Id == id;
        if (pineTree.IsCurrent)
        {
            result = pineTree;
        }
        if (currentDepth > maxDepth)
        {
            maxDepth = currentDepth;
        }
        fileCount++;
        foreach (var child in pineTree.Children)
        {
            var childResult = child.SetCurrentIncludeChild(id, ref fileCount, currentDepth + 1, ref maxDepth);
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

    private static PineTree GetRoot(this PineTree pineTree)
    {
        var node = pineTree;
        while (node.Parent != null)
        {
            node = node.Parent;
        }
        return node;
    }

    private static void GetTotalFileCountChild(this PineTree node, ref int count)
    {
        count++;
        foreach (var child in node.Children)
        {
            GetTotalFileCountChild(child, ref count);
        }
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
