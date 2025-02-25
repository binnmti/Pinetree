namespace Pinetree.Model;

public class PineTree(long id, string title, string content, long parentId, long groupId)
{
    public long Id { get; } = id;
    public string Title { get; } = title;
    public string Content { get; } = content;
    public long ParentId { get; set; } = parentId;
    public long GroupId { get; set; } = groupId;

    public bool IsCurrent { get; set; }
    public bool IsExpanded { get; set; }
    public string Url { get; } = "";
    public List<PineTree> Children { get; } = [];
};