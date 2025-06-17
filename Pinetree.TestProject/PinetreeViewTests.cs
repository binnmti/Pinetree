using Pinetree.Client.ViewModels;

namespace Pinetree.TestProject;

[TestClass]
public class PinetreeViewTests
{
    private static PinetreeView CreateTestHierarchy()
    {
        var guid = Guid.NewGuid();

        // Root
        var root = new PinetreeView(guid, "Root", "Root Content", null, guid, false);

        // Parent
        var parent = new PinetreeView(Guid.NewGuid(), "Parent", "Parent Content", root, guid, false);
        root.Children.Add(parent);

        // Child 1
        var child1 = new PinetreeView(Guid.NewGuid(), "Child 1", "Child 1 Content", parent, guid, false);
        parent.Children.Add(child1);

        // Child 2
        var child2 = new PinetreeView(Guid.NewGuid(), "Child 2", "Child 2 Content", parent, guid, false);
        parent.Children.Add(child2);

        return root;
    }

    [TestMethod]
    public void HierarchyStructure_InitialState_IsCorrect()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0];
        var child2 = parent.Children[1];

        // Assert
        Assert.IsNull(root.Parent);
        Assert.AreEqual(root, parent.Parent);
        Assert.AreEqual(parent, child1.Parent);
        Assert.AreEqual(parent, child2.Parent);
        Assert.AreEqual(1, root.Children.Count);
        Assert.AreEqual(2, parent.Children.Count);
    }

    [TestMethod]
    public void MoveChild_ToSibling_UpdatesRelationships()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0];
        var child2 = parent.Children[1];

        // Act - Move child1 to be after child2
        parent.Children.RemoveAt(0);
        parent.Children.Insert(1, child1);

        // Assert
        Assert.AreEqual(2, parent.Children.Count);
        Assert.AreEqual(child2, parent.Children[0]);
        Assert.AreEqual(child1, parent.Children[1]);
        Assert.AreEqual(parent, child1.Parent);
        Assert.AreEqual(parent, child2.Parent);
    }

    [TestMethod]
    public void MoveChild_ToParentLevel_UpdatesRelationships()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0];

        // Act - Move child1 to root level
        parent.Children.RemoveAt(0);
        child1.Parent = root;
        root.Children.Add(child1);

        // Assert
        Assert.AreEqual(2, root.Children.Count);
        Assert.AreEqual(parent, root.Children[0]);
        Assert.AreEqual(child1, root.Children[1]);
        Assert.AreEqual(root, child1.Parent);
        Assert.AreEqual(1, parent.Children.Count);
    }

    [TestMethod]
    public void MoveItem_ToChild_UpdatesRelationships()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0];
        var child2 = parent.Children[1];

        // Act - Move child2 under child1
        parent.Children.RemoveAt(1);
        child2.Parent = child1;
        child1.Children.Add(child2);

        // Assert
        Assert.AreEqual(1, parent.Children.Count);
        Assert.AreEqual(child1, parent.Children[0]);
        Assert.AreEqual(1, child1.Children.Count);
        Assert.AreEqual(child2, child1.Children[0]);
        Assert.AreEqual(child1, child2.Parent);
    }

    [TestMethod]
    public void DeepClone_CreatesIndependentCopy()
    {
        // Arrange
        var root = CreateTestHierarchy();

        // Act
        var clone = root.DeepClone();

        // Assert
        Assert.AreEqual(root.Guid, clone.Guid);
        Assert.AreEqual(root.Title, clone.Title);
        Assert.AreEqual(root.Content, clone.Content);
        Assert.AreEqual(root.Children.Count, clone.Children.Count);

        // Verify that modifying clone doesn't affect original
        clone.Title = "Modified Title";
        clone.Children[0].Title = "Modified Child";

        Assert.AreNotEqual(root.Title, clone.Title);
        Assert.AreNotEqual(root.Children[0].Title, clone.Children[0].Title);
    }

    [TestMethod]
    public void Equals_ComparesStructureCorrectly()
    {
        // Arrange
        var root1 = CreateTestHierarchy();
        var root2 = root1.DeepClone();
        var root3 = root1.DeepClone();
        root3.Children[0].Title = "Different Title";

        // Assert
        Assert.IsTrue(root1.Equals(root2));
        Assert.IsFalse(root1.Equals(root3));
    }

    [TestMethod]
    public void RemoveChild_UpdatesRelationships()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0];

        // Act
        parent.Children.Remove(child1);

        // Assert
        Assert.AreEqual(1, parent.Children.Count);
        Assert.IsFalse(parent.Children.Contains(child1));
    }

    [TestMethod]
    public void MoveItemUp_WhenSuccessful_ExchangesOrder()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0]; // index=0
        var child2 = parent.Children[1]; // index=1

        // Act
        var result = child2.MoveItemUp();

        // Assert
        Assert.IsTrue(result, "Expected to move child2 up");
        Assert.AreEqual(child2, parent.Children[0]);
        Assert.AreEqual(child1, parent.Children[1]);
    }

    [TestMethod]
    public void MoveItemDown_WhenAlreadyLast_ReturnsFalse()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child2 = parent.Children[1]; // already last

        // Act
        var result = child2.MoveItemDown();

        // Assert
        Assert.IsFalse(result, "Expected false when at bottom");
        Assert.AreEqual(child2, parent.Children[1]);
    }

    [TestMethod]
    public void MoveItemLeft_WhenPossible_MovesToUpperLevel()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0];

        // Act
        var result = child1.MoveItemLeft();

        // Assert
        Assert.IsTrue(result, "Expected true when moving left");
        Assert.AreEqual(root, child1.Parent);
        Assert.IsTrue(root.Children.Contains(child1));
    }

    [TestMethod]
    public void MoveItemLeft_WhenTopLevel_ReturnsFalse()
    {
        // Arrange
        var root = CreateTestHierarchy();

        // Act
        var result = root.MoveItemLeft();

        // Assert
        Assert.IsFalse(result, "Cannot move left if you have no parent");
    }

    [TestMethod]
    public void MoveItemRight_WhenPossible_MovesUnderLeftSibling()
    {
        // Arrange
        var root = CreateTestHierarchy();
        var parent = root.Children[0];
        var child1 = parent.Children[0];
        var child2 = parent.Children[1]; // will move under child1

        // Act
        var result = child2.MoveItemRight();

        // Assert
        Assert.IsTrue(result, "Expected true when moving right");
        Assert.AreEqual(child1, child2.Parent);
        Assert.IsTrue(child1.Children.Contains(child2));
        Assert.IsFalse(parent.Children.Contains(child2));
    }
}
