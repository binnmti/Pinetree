namespace Pinetree.Client.VModel;

public class PinetreeView(long id, string title, string content, PinetreeView? parent, long groupId)
{
    public long Id { get; } = id;
    public string Title { get; set; } = title;
    public string Content { get; set; } = content;
    public PinetreeView? Parent { get; } = parent;
    public long GroupId { get; } = groupId;
    public bool IsCurrent { get; set; }
    public bool IsExpanded { get; set; }
    public List<PinetreeView> Children { get; } = [];

    public static PinetreeView Nothing => new(0, "", "", null, 0);
};