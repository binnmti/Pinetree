namespace Pinetree.Client.VModel;

public class PinetreeView(Guid guid, string title, string content, PinetreeView? parent, Guid groupId, bool isPublic) : IEquatable<PinetreeView>
{
    public Guid Guid { get; } = guid;
    public string Title { get; set; } = title;
    public string Content { get; set; } = content;
    public PinetreeView? Parent { get; set; } = parent;
    public Guid GroupGuid { get; } = groupId;
    public bool IsCurrent { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsPublic { get; set; } = isPublic;

    public List<PinetreeView> Children { get; } = [];
    public Stack<string> UndoStack { get; } = new();
    public Stack<string> RedoStack { get; } = new();
    public bool CanUndo => UndoStack.Count != 0;
    public bool CanRedo => RedoStack.Count != 0;

    public static PinetreeView Nothing => new(default, "", "", null, default, false);

    public void SaveContentToHistory(string previousContent)
    {
        UndoStack.Push(previousContent);
        RedoStack.Clear();
    }

    public string? Undo()
    {
        if (UndoStack.Count == 0) return null;

        RedoStack.Push(Content);
        Content = UndoStack.Pop();
        return Content;
    }

    public string? Redo()
    {
        if (RedoStack.Count == 0) return null;

        UndoStack.Push(Content);
        Content = RedoStack.Pop();
        return Content;
    }

    public bool Equals(PinetreeView? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Title != other.Title || Content != other.Content || Guid != other.Guid || GroupGuid != other.GroupGuid)
        {
            return false;
        }
        bool parentEqual = (Parent is null && other.Parent is null) ||
                           (Parent is not null && other.Parent is not null && Parent.Guid == other.Parent.Guid);
        if (!parentEqual) return false;
        if (Children.Count != other.Children.Count) return false;
        for (int i = 0; i < Children.Count; i++)
        {
            if (!Children[i].Equals(other.Children[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as PinetreeView);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Title);
        hash.Add(Content);
        hash.Add(Parent?.Guid ?? default);
        hash.Add(GroupGuid);
        hash.Add(Guid);
        foreach (var child in Children)
        {
            hash.Add(child.Guid);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(PinetreeView? left, PinetreeView? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PinetreeView? left, PinetreeView? right)
        => !(left == right);

    public PinetreeView DeepClone()
    {
        var clone = new PinetreeView(Guid, Title, Content, Parent, GroupGuid, IsPublic)
        {
            IsCurrent = IsCurrent,
            IsExpanded = IsExpanded
        };
        foreach (var child in Children)
        {
            var childClone = child.DeepClone();
            clone.Children.Add(childClone);
        }
        return clone;
    }
};