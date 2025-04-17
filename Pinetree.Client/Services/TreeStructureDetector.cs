using Pinetree.Client.VModel;

namespace Pinetree.Client.Services;

public static class TreeStructureDetector
{
    public static bool HasStructuralChanges(PinetreeView current, PinetreeView previous)
    {
        int currentCount = CountNodes(current);
        int previousCount = CountNodes(previous);
        if (currentCount != previousCount)
            return true;

        var currentIds = new HashSet<long>();
        var previousIds = new HashSet<long>();
        CollectIds(current, currentIds);
        CollectIds(previous, previousIds);

        if (!currentIds.SetEquals(previousIds))
            return true;

        return HasParentChildRelationshipChanged(current, previous);
    }

    private static int CountNodes(PinetreeView tree)
    {
        int count = 1;
        foreach (var child in tree.Children)
        {
            count += CountNodes(child);
        }
        return count;
    }

    private static void CollectIds(PinetreeView node, HashSet<long> ids)
    {
        ids.Add(node.Id);
        foreach (var child in node.Children)
        {
            CollectIds(child, ids);
        }
    }

    private static bool HasParentChildRelationshipChanged(PinetreeView current, PinetreeView previous)
    {
        var currentParentMap = new Dictionary<long, long?>();
        var previousParentMap = new Dictionary<long, long?>();

        BuildParentMap(current, currentParentMap, null);
        BuildParentMap(previous, previousParentMap, null);

        foreach (var id in currentParentMap.Keys)
        {
            if (!previousParentMap.ContainsKey(id))
                continue;

            if (currentParentMap[id] != previousParentMap[id])
                return true;
        }

        return HasSiblingOrderChanged(current, previous);
    }

    private static void BuildParentMap(PinetreeView node, Dictionary<long, long?> parentMap, long? parentId)
    {
        parentMap[node.Id] = parentId;
        foreach (var child in node.Children)
        {
            BuildParentMap(child, parentMap, node.Id);
        }
    }

    private static bool HasSiblingOrderChanged(PinetreeView current, PinetreeView previous)
    {
        if (current.Children.Count != previous.Children.Count)
            return true;

        for (int i = 0; i < current.Children.Count; i++)
        {
            if (current.Children[i].Id != previous.Children[i].Id)
                return true;
        }

        for (int i = 0; i < current.Children.Count; i++)
        {
            if (HasSiblingOrderChanged(current.Children[i], previous.Children[i]))
                return true;
        }

        return false;
    }
}
