namespace Pinetree.Model;

public static class PinetreeUpdater
{
    public static long GetUniqueId(this PineTree pinetree)
    {
        var root = pinetree.GetRoot();
        var usedIds = new List<long>();
        CollectUsedIds(root, usedIds);
        var orderedEnumerable = usedIds.OrderBy(id => id);
        return orderedEnumerable.Last() + 1;
    }

    private static void CollectUsedIds(PineTree node, List<long> usedIds)
    {
        usedIds.Add(node.Id);
        foreach (var child in node.Children)
        {
            CollectUsedIds(child, usedIds);
        }
    }

    public static int GetTotalFileCount(this PineTree node)
    {
        int count = 0;
        GetTotalFileCountChild(GetRoot(node), ref count);
        return count;
    }

    public static int GetDepth(this PineTree node)
    {
        int depth = 0;
        var parent = node.Parent;
        while (parent != null)
        {
            depth++;
            parent = parent.Parent;
        }
        return depth;
    }

    public static PineTree SetCurrentIncludeChild(this PineTree pineTree, long id, ref int fileCount, int currentDepth)
    {
        PineTree? result = null;
        pineTree.IsCurrent = pineTree.Id == id;
        if (pineTree.IsCurrent)
        {
            result = pineTree;
        }
        fileCount++;
        foreach (var child in pineTree.Children)
        {
            var childResult = child.SetCurrentIncludeChild(id, ref fileCount, currentDepth + 1);
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
