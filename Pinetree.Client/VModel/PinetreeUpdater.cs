namespace Pinetree.Client.VModel;

public static class PinetreeUpdater
{
    public static bool IsEqual(this PinetreeView src, PinetreeView dst)
        => IsEqual(src.GetRoot(), dst.GetRoot());

    public static Guid GetUniqueId(this PinetreeView pinetree)
    {
        var root = pinetree.GetRoot();
        var usedIds = new List<Guid>();
        CollectUsedIds(root, usedIds);
        var orderedEnumerable = usedIds.OrderBy(id => id);
        return Guid.NewGuid();
    }

    private static void CollectUsedIds(PinetreeView node, List<Guid> usedIds)
    {
        usedIds.Add(node.Guid);
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

    public static PinetreeView? SetCurrentIncludeChild(this PinetreeView pineTree, Guid guid, ref int fileCount, int currentDepth)
    {
        PinetreeView? result = null;
        pineTree.IsCurrent = pineTree.Guid == guid;
        if (pineTree.IsCurrent)
        {
            result = pineTree;
            pineTree.IsExpanded = true;
        }
        fileCount++;
        foreach (var child in pineTree.Children)
        {
            var childResult = child.SetCurrentIncludeChild(guid, ref fileCount, currentDepth + 1);
            if (childResult != null)
            {
                result = childResult;
                pineTree.IsExpanded = true;
            }
        }
        return result;
    }

    public static Guid DeleteIncludeChild(this PinetreeView tree)
    {
        DeleteChildren(tree);
        tree.Parent?.Children.Remove(tree);
        return tree.Parent?.Guid ?? default;
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
