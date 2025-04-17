namespace Pinetree.Client.VModel;

public static class PinetreeMoverExtention
{
    public static bool MoveItemUp(this PinetreeView currentItem)
    {
        if (currentItem?.Parent == null) return false;
        var parent = currentItem.Parent;
        var index = parent.Children.IndexOf(currentItem);

        if (index <= 0) return false; // Cannot move up

        parent.Children.RemoveAt(index);
        parent.Children.Insert(index - 1, currentItem);
        return true;
    }

    public static bool MoveItemDown(this PinetreeView currentItem)
    {
        if (currentItem?.Parent == null) return false;
        var parent = currentItem.Parent;
        var index = parent.Children.IndexOf(currentItem);

        if (index < 0 || index >= parent.Children.Count - 1) return false; // Cannot move down

        parent.Children.RemoveAt(index);
        parent.Children.Insert(index + 1, currentItem);
        return true;
    }

    public static bool MoveItemLeft(this PinetreeView currentItem)
    {
        if (currentItem?.Parent == null || currentItem.Parent.Parent == null) return false;
        var currentParent = currentItem.Parent;
        var grandParent = currentParent.Parent;

        var indexInParent = currentParent.Children.IndexOf(currentItem);
        if (indexInParent < 0) return false;

        currentParent.Children.RemoveAt(indexInParent);
        currentItem.Parent = grandParent;

        var parentIndex = grandParent.Children.IndexOf(currentParent);
        if (parentIndex < 0) return false;

        grandParent.Children.Insert(parentIndex + 1, currentItem);
        return true;
    }

    public static bool MoveItemRight(this PinetreeView currentItem)
    {
        if (currentItem?.Parent == null) return false;
        var parent = currentItem.Parent;
        var index = parent.Children.IndexOf(currentItem);

        // Need a left sibling
        if (index <= 0) return false;

        var newParent = parent.Children[index - 1];
        parent.Children.RemoveAt(index);
        currentItem.Parent = newParent;
        newParent.Children.Add(currentItem);
        return true;
    }
}
