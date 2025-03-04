namespace Pinetree.Model;

public static class PinetreeUpdater
{

    public static PineTree Update(this PineTree tree, long id, string title, string content)
    {
        if (tree.Id == id)
        {
            tree.Title = title;
            tree.Content = content;
            return tree;
        }
        foreach (var child in tree.Children)
        {
            Update(child, id, title, content);
        }
        return tree;
    }
}
