namespace Pinetree.Client.VModel;

public static class PinetreeUpdater
{
    public static bool IsEqual(this PinetreeView src, PinetreeView dst)
        => IsEqual(src.GetRoot(), dst.GetRoot());

    public static long GetUniqueId(this PinetreeView pinetree)
    {
        var root = pinetree.GetRoot();
        var usedIds = new List<long>();
        CollectUsedIds(root, usedIds);
        var orderedEnumerable = usedIds.OrderBy(id => id);
        return orderedEnumerable.Last() + 1;
    }

    private static void CollectUsedIds(PinetreeView node, List<long> usedIds)
    {
        usedIds.Add(node.Id);
        foreach (var child in node.Children)
        {
            CollectUsedIds(child, usedIds);
        }
    }

    public static int GetTotalFileCount(this PinetreeView node)
    {
        int count = 0;
        GetTotalFileCountChild(GetRoot(node), ref count);
        return count;
    }

    public static int GetDepth(this PinetreeView node)
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

    public static PinetreeView? SetCurrentIncludeChild(this PinetreeView pineTree, long id, ref int fileCount, int currentDepth)
    {
        PinetreeView? result = null;
        pineTree.IsCurrent = pineTree.Id == id;
        if (pineTree.IsCurrent)
        {
            result = pineTree;
            pineTree.IsExpanded = true;
        }
        fileCount++;
        foreach (var child in pineTree.Children)
        {
            var childResult = child.SetCurrentIncludeChild(id, ref fileCount, currentDepth + 1);
            if (childResult != null)
            {
                result = childResult;
                pineTree.IsExpanded = true;
            }
        }
        return result;
    }

    public static long DeleteIncludeChild(this PinetreeView tree)
    {
        DeleteChildren(tree);
        tree.Parent?.Children.Remove(tree);
        return tree.Parent?.Id ?? -1;
    }

    private static PinetreeView GetRoot(this PinetreeView pineTree)
    {
        var node = pineTree;
        while (node.Parent != null)
        {
            node = node.Parent;
        }
        return node;
    }

    private static void GetTotalFileCountChild(this PinetreeView node, ref int count)
    {
        count++;
        foreach (var child in node.Children)
        {
            GetTotalFileCountChild(child, ref count);
        }
    }


    private static void DeleteChildren(this PinetreeView tree)
    {
        foreach (var child in tree.Children)
        {
            DeleteChildren(child);
        }
        tree.Children.Clear();
    }
}
