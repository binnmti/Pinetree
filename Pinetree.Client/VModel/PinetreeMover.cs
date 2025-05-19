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

    // ドラッグアンドドロップ操作のための追加メソッド
    
    /// <summary>
    /// sourceItemをtargetItemの前に移動します。
    /// 同じ親を持つ場合は、上ボタンと同様に位置を交換します。
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

        // 同じ親を持つ場合は、位置を交換（MoveItemUpのような動作）
        if (sourceItem.Parent == targetParent)
        {
            int sourceIndex = targetParent.Children.IndexOf(sourceItem);
            
            // 既に隣接している場合は何もしない
            if (sourceIndex == targetIndex - 1)
                return true;
                
            // MoveItemUpと同様の動作
            if (sourceIndex > targetIndex)
            {
                // sourceがtargetより後ろにある場合
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex, sourceItem);
            }
            else
            {
                // sourceがtargetより前にある場合
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex - 1, sourceItem);
            }
        }
        else
        {
            // 異なる親からの移動
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
    /// sourceItemをtargetItemの後ろに移動します。
    /// 同じ親を持つ場合は、下ボタンと同様に位置を交換します。
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

        // 同じ親を持つ場合は、位置を交換（MoveItemDownのような動作）
        if (sourceItem.Parent == targetParent)
        {
            int sourceIndex = targetParent.Children.IndexOf(sourceItem);
            
            // 既に隣接している場合は何もしない
            if (sourceIndex == targetIndex + 1)
                return true;
                
            // MoveItemDownと同様の動作
            if (sourceIndex > targetIndex)
            {
                // sourceがtargetより後ろにある場合
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex + 1, sourceItem);
            }
            else
            {
                // sourceがtargetより前にある場合
                targetParent.Children.RemoveAt(sourceIndex);
                targetParent.Children.Insert(targetIndex, sourceItem);
            }
        }
        else
        {
            // 異なる親からの移動
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
    /// sourceItemをtargetItemの子として移動します（右ボタンと同様）。
    /// </summary>
    public static bool MoveItemAsChildOf(this PinetreeView sourceItem, PinetreeView targetItem)
    {
        if (sourceItem == null || targetItem == null)
            return false;

        if (sourceItem == targetItem)
            return false;
            
        // 循環参照を防ぐ
        if (IsDescendantOf(targetItem, sourceItem))
            return false;

        // 古い親から削除
        if (sourceItem.Parent != null)
        {
            sourceItem.Parent.Children.Remove(sourceItem);
        }
        
        // ターゲットの子として追加
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
