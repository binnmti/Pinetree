namespace Pinetree.Model;

public class PineTree(long id, string title, string content, PineTree? parent, long groupId)
{
    public long Id { get; } = id;
    public string Title { get; set; } = title;
    public string Content { get; set; } = content;
    public PineTree? Parent { get; } = parent;

    public long GroupId { get; set; } = groupId;

    public bool IsCurrent { get; set; }
    public bool IsExpanded { get; set; }
    public string Url { get; } = "";
    public List<PineTree> Children { get; } = [];
};