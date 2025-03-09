namespace Pinetree.Model;

public static class PinetreeUpdater
{
    public static PineTree SetCurrent(this PineTree pineTree, long id)
    {
        PineTree? result = null;
        pineTree.IsCurrent = pineTree.Id == id;
        if (pineTree.IsCurrent)
        {
            result = pineTree;
        }
        foreach (var child in pineTree.Children)
        {
            var childResult = child.SetCurrent(id);
            if (childResult != null)
            {
                result = childResult;
            }
        }
        // The final return value will not be null, so do not use PineTree?.
        return result!;
    }
}
