namespace Pinetree.Model;

public class PineTree(long id, string title, string content)
{
    public long Id { get; } = id;
    public string Title { get; } = title;
    public string Content { get; } = content;
    public bool IsExpanded { get; set; }
    public List<PineTree> Children { get; } = [];
};