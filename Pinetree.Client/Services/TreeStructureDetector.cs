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

    //private static void IndexNodes(PinetreeView node, Dictionary<long, PinetreeView> nodeMap)
    //{
    //    nodeMap[node.Id] = node;
    //    foreach (var child in node.Children)
    //    {
    //        IndexNodes(child, nodeMap);
    //    }
    //}

    //private static void FindChangedNodesRecursive(
    //    PinetreeView node,
    //    Dictionary<long, PinetreeView> previousNodeMap,
    //    List<PinetreeView> changedNodes)
    //{
    //    if (previousNodeMap.TryGetValue(node.Id, out var previousNode))
    //    {
    //        if (node.Title != previousNode.Title || node.Content != previousNode.Content)
    //        {
    //            changedNodes.Add(node);
    //        }
    //    }
    //    else
    //    {
    //        changedNodes.Add(node);
    //    }

    //    foreach (var child in node.Children)
    //    {
    //        FindChangedNodesRecursive(child, previousNodeMap, changedNodes);
    //    }
    //}

    //public static List<PineconeDto> ConvertToNodeDtos(PinetreeView tree)
    //{
    //    var result = new List<PineconeDto>();
    //    ConvertNodeRecursively(tree, null, result);
    //    return result;
    //}

    //private static void ConvertNodeRecursively(
    //    PinetreeView node,
    //    long? parentId,
    //    List<PineconeDto> result)
    //{
    //    int order = node.Parent?.Children.IndexOf(node) ?? 0;

    //    var dto = new PineconeDto
    //    {
    //        Id = node.Id,
    //        Title = node.Title,
    //        Content = node.Content,
    //        GroupId = node.GroupId,
    //        ParentId = parentId,
    //        Order = order
    //    };

    //    result.Add(dto);

    //    for (int i = 0; i < node.Children.Count; i++)
    //    {
    //        ConvertNodeRecursively(node.Children[i], node.Id, result);
    //    }
    //}

    //public static List<PineconeDto> FindChangedNodes(PinetreeView current, PinetreeView previous)
    //{
    //    var changedNodes = new List<PineconeDto>();

    //    var previousNodeMap = new Dictionary<long, (PinetreeView Node, int Order)>();
    //    IndexNodesWithOrder(previous, previousNodeMap);

    //    FindChangedNodesRecursive(current, null, previousNodeMap, changedNodes);

    //    return changedNodes;
    //}

    //private static void IndexNodesWithOrder(
    //    PinetreeView node,
    //    Dictionary<long, (PinetreeView Node, int Order)> nodeMap)
    //{
    //    int order = node.Parent?.Children.IndexOf(node) ?? 0;
    //    nodeMap[node.Id] = (node, order);

    //    foreach (var child in node.Children)
    //    {
    //        IndexNodesWithOrder(child, nodeMap);
    //    }
    //}

    //private static void FindChangedNodesRecursive(
    //    PinetreeView node,
    //    long? parentId,
    //    Dictionary<long, (PinetreeView Node, int Order)> previousNodeMap,
    //    List<PineconeDto> changedNodes)
    //{
    //    int currentOrder = node.Parent?.Children.IndexOf(node) ?? 0;
    //    bool isChanged = false;

    //    if (previousNodeMap.TryGetValue(node.Id, out var previousInfo))
    //    {
    //        var (previousNode, previousOrder) = previousInfo;

    //        isChanged = node.Title != previousNode.Title ||
    //                  node.Content != previousNode.Content ||
    //                  parentId != (previousNode.Parent?.Id) ||
    //                  currentOrder != previousOrder;
    //    }
    //    else
    //    {
    //        isChanged = true;
    //    }

    //    if (isChanged)
    //    {
    //        changedNodes.Add(new PineconeDto
    //        {
    //            Id = node.Id,
    //            Title = node.Title,
    //            Content = node.Content,
    //            GroupId = node.GroupId,
    //            ParentId = parentId,
    //            Order = currentOrder
    //        });
    //    }

    //    for (int i = 0; i < node.Children.Count; i++)
    //    {
    //        FindChangedNodesRecursive(node.Children[i], node.Id, previousNodeMap, changedNodes);
    //    }
    //}
}

public class PineconeDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long GroupId { get; set; }
    public long? ParentId { get; set; }
    public int Order { get; set; }
}

public class TreeUpdateRequest
{
    public long RootId { get; set; }
    public bool HasStructuralChanges { get; set; }
    public List<PineconeDto> Nodes { get; set; } = new();
}