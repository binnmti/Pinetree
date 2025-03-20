namespace Pinetree.Client.Model;

public class Pinetree(long id, string title, string content, Pinetree? parent, long groupId)
{
    public long Id { get; } = id;
    public string Title { get; set; } = title;
    public string Content { get; set; } = content;
    public Pinetree? Parent { get; } = parent;
    public long GroupId { get; } = groupId;
    public bool IsCurrent { get; set; }
    public bool IsExpanded { get; set; }
    public List<Pinetree> Children { get; } = [];

    public static Pinetree Nothing => new(0, "", "", null, 0);
};