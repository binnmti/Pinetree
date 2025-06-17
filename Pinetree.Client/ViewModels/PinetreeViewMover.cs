namespace Pinetree.Client.ViewModels;

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

        var newParent = parent.Children[index - 1];        parent.Children.RemoveAt(index);
        currentItem.Parent = newParent;
        newParent.Children.Add(currentItem);
        return true;
    }

    // Additional methods for drag and drop operations
    
    /// <summary>
    /// Moves the sourceItem before the targetItem.
    /// If they have the same parent, it swaps positions similar to the up button.
    /// </summary>
    public static bool MoveItemBefore(this PinetreeView sourceItem, PinetreeView targetItem)
    {
        if (sourceItem == null || targetItem == null || targetItem.Parent == null)
            return false;

        if (sourceItem == targetItem)
            return false;

        var targetParent = targetItem.Parent;
        var targetIndex = targetParent.Children.IndexOf(targetItem);
          if (targetIndex < 0)
            return false;

        // If they have the same parent, swap positions (similar to MoveItemUp)
        if (sourceItem.Parent == targetParent)
        {            int sourceIndex = targetParent.Children.IndexOf(sourceItem);
            
            // Do nothing if already adjacent
            if (sourceIndex == targetIndex - 1)
                return true;
                
            // Behavior similar to MoveItemUp
            if (sourceIndex > targetIndex)
            {
                // If source is after target
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex, sourceItem);
            }
            else
            {
                // If source is before target
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex - 1, sourceItem);
            }
        }
        else
        {
            // Moving from a different parent
            if (sourceItem.Parent != null)
            {
                sourceItem.Parent.Children.Remove(sourceItem);
            }
            
            sourceItem.Parent = targetParent;
            targetParent.Children.Insert(targetIndex, sourceItem);
        }
        
        return true;
    }
      /// <summary>
    /// Moves the sourceItem after the targetItem.
    /// If they have the same parent, it swaps positions similar to the down button.
    /// </summary>
    public static bool MoveItemAfter(this PinetreeView sourceItem, PinetreeView targetItem)
    {
        if (sourceItem == null || targetItem == null || targetItem.Parent == null)
            return false;

        if (sourceItem == targetItem)
            return false;

        var targetParent = targetItem.Parent;
        var targetIndex = targetParent.Children.IndexOf(targetItem);
        
        if (targetIndex < 0)
            return false;

        // If they have the same parent, swap positions (similar to MoveItemDown)
        if (sourceItem.Parent == targetParent)
        {
            int sourceIndex = targetParent.Children.IndexOf(sourceItem);
            
            // Do nothing if already adjacent
            if (sourceIndex == targetIndex + 1)
                return true;
                
            // Behavior similar to MoveItemDown
            if (sourceIndex > targetIndex)
            {
                // If source is after target
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex + 1, sourceItem);
            }
            else
            {
                // If source is before target
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex, sourceItem);
            }
        }
        else
        {
            // Moving from a different parent
            if (sourceItem.Parent != null)
            {
                sourceItem.Parent.Children.Remove(sourceItem);
            }
            
            sourceItem.Parent = targetParent;
            targetParent.Children.Insert(targetIndex + 1, sourceItem);
        }
        
        return true;
    }
      /// <summary>
    /// Moves the sourceItem as a child of targetItem (similar to the right button).
    /// </summary>
    public static bool MoveItemAsChildOf(this PinetreeView sourceItem, PinetreeView targetItem)
    {
        if (sourceItem == null || targetItem == null)
            return false;

        if (sourceItem == targetItem)
            return false;
            
        // Prevent circular references
        if (IsDescendantOf(targetItem, sourceItem))
            return false;

        // Remove from old parent
        if (sourceItem.Parent != null)
        {
            sourceItem.Parent.Children.Remove(sourceItem);
        }
        
        // Add as child of target
        sourceItem.Parent = targetItem;
        targetItem.Children.Add(sourceItem);
        
        return true;
    }
    
    private static bool IsDescendantOf(PinetreeView descendant, PinetreeView ancestor)
    {
        var parent = descendant.Parent;
        while (parent != null)
        {
            if (parent == ancestor)
                return true;
            parent = parent.Parent;
        }
        return false;
    }
}
