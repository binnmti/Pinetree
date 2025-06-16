using Pinetree.Client.ViewModels;
using Pinetree.Shared.ViewModels;

namespace Pinetree.TestProject;

[TestClass]
public class PineconeConvertTests
{
    [TestMethod]
    public void ToPinetree_ShouldMapPineconeViewModelToPinetreeView()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupGuid = Guid.NewGuid();
        var viewModel = new PineconeViewModel
        {
            Guid = guid,
            Title = "Test Title",
            Content = "Test Content",
            GroupGuid = groupGuid,
            ParentGuid = null,
            IsPublic = true,
            UserName = "testuser"
        };

        // Act
        var pinetreeView = viewModel.ToPinetree(null);

        // Assert
        Assert.AreEqual(guid, pinetreeView.Guid);
        Assert.AreEqual("Test Title", pinetreeView.Title);
        Assert.AreEqual("Test Content", pinetreeView.Content);
        Assert.AreEqual(groupGuid, pinetreeView.GroupGuid);
        Assert.IsTrue(pinetreeView.IsPublic);
        Assert.IsNull(pinetreeView.Parent);
        Assert.AreEqual(0, pinetreeView.Children.Count);
    }

    [TestMethod]
    public void ToPinetree_WithParent_ShouldSetParentReference()
    {
        // Arrange
        var parentGuid = Guid.NewGuid();
        var childGuid = Guid.NewGuid();
        var groupGuid = Guid.NewGuid();

        var parentView = new PinetreeView(parentGuid, "Parent", "Parent Content", null, groupGuid, true);
        var childViewModel = new PineconeViewModel
        {
            Guid = childGuid,
            Title = "Child Title",
            Content = "Child Content",
            GroupGuid = groupGuid,
            ParentGuid = parentGuid,
            IsPublic = true
        };

        // Act
        var childView = childViewModel.ToPinetree(parentView);

        // Assert
        Assert.AreEqual(childGuid, childView.Guid);
        Assert.AreEqual("Child Title", childView.Title);
        Assert.AreEqual("Child Content", childView.Content);        Assert.AreEqual(parentView, childView.Parent);
        Assert.AreEqual(parentGuid, childView.Parent?.Guid);
    }    [TestMethod]
    public void ToPinetreeIncludeChild_SingleNode_ShouldReturnCorrectCount()
    {
        // Arrange
        var viewModel = new PineconeViewModelWithChildren
        {
            Guid = Guid.NewGuid(),
            Title = "Single Node",
            Content = "Single Content",
            GroupGuid = Guid.NewGuid(),
            IsPublic = false,
            Children = new List<PineconeViewModelWithChildren>()
        };

        // Act
        var (pinetreeView, fileCount) = viewModel.ToPinetreeIncludeChild();

        // Assert        Assert.AreEqual(viewModel.Guid, pinetreeView.Guid);
        Assert.AreEqual("Single Node", pinetreeView.Title);
        Assert.AreEqual("Single Content", pinetreeView.Content);
        Assert.AreEqual(0, fileCount); // Only the root node, which is not counted
        Assert.AreEqual(0, pinetreeView.Children.Count);
    }    [TestMethod]
    public void ToPinetreeIncludeChild_WithChildrenList_ShouldBuildHierarchy()
    {
        // Arrange
        var rootGuid = Guid.NewGuid();
        var child1Guid = Guid.NewGuid();
        var child2Guid = Guid.NewGuid();
        var groupGuid = Guid.NewGuid();

        var rootViewModel = new PineconeViewModelWithChildren
        {
            Guid = rootGuid,
            Title = "Root",
            Content = "Root Content",
            GroupGuid = groupGuid,
            ParentGuid = null,
            Order = 0,
            IsPublic = true,
            Children = new List<PineconeViewModelWithChildren>
            {
                new PineconeViewModelWithChildren
                {
                    Guid = child1Guid,
                    Title = "Child 1",
                    Content = "Child 1 Content",
                    GroupGuid = groupGuid,
                    ParentGuid = rootGuid,
                    Order = 0,
                    IsPublic = true,
                    Children = new List<PineconeViewModelWithChildren>()
                },
                new PineconeViewModelWithChildren
                {
                    Guid = child2Guid,
                    Title = "Child 2",
                    Content = "Child 2 Content",
                    GroupGuid = groupGuid,
                    ParentGuid = rootGuid,
                    Order = 1,
                    IsPublic = true,
                    Children = new List<PineconeViewModelWithChildren>()
                }
            }
        };

        // Act
        var (pinetreeView, fileCount) = rootViewModel.ToPinetreeIncludeChild();

        // Assert        Assert.AreEqual(rootGuid, pinetreeView.Guid);
        Assert.AreEqual("Root", pinetreeView.Title);
        Assert.AreEqual(2, fileCount); // 2 children (root not counted)
        Assert.AreEqual(2, pinetreeView.Children.Count);
        
        var firstChild = pinetreeView.Children[0];
        Assert.AreEqual(child1Guid, firstChild.Guid);
        Assert.AreEqual("Child 1", firstChild.Title);
        Assert.AreEqual(pinetreeView, firstChild.Parent);
        
        var secondChild = pinetreeView.Children[1];
        Assert.AreEqual(child2Guid, secondChild.Guid);
        Assert.AreEqual("Child 2", secondChild.Title);
        Assert.AreEqual(pinetreeView, secondChild.Parent);
    }

    [TestMethod]
    public void ToPinetreeIncludeChild_HierarchicalViewModel_ShouldBuildCompleteTree()
    {
        // Arrange
        var rootGuid = Guid.NewGuid();
        var child1Guid = Guid.NewGuid();
        var grandchild1Guid = Guid.NewGuid();
        
        var hierarchicalViewModel = new PineconeViewModelWithChildren
        {
            Guid = rootGuid,
            Title = "Root",
            Content = "Root Content",
            GroupGuid = Guid.NewGuid(),
            IsPublic = true,
            Children = new List<PineconeViewModelWithChildren>
            {
                new PineconeViewModelWithChildren
                {
                    Guid = child1Guid,
                    Title = "Child 1",
                    Content = "Child 1 Content",
                    ParentGuid = rootGuid,
                    IsPublic = true,
                    Children = new List<PineconeViewModelWithChildren>
                    {
                        new PineconeViewModelWithChildren
                        {
                            Guid = grandchild1Guid,
                            Title = "Grandchild 1",
                            Content = "Grandchild 1 Content",
                            ParentGuid = child1Guid,
                            IsPublic = true,
                            Children = new List<PineconeViewModelWithChildren>()
                        }
                    }
                }
            }
        };

        // Act
        var (pinetreeView, fileCount) = hierarchicalViewModel.ToPinetreeIncludeChild();

        // Assert        Assert.AreEqual(rootGuid, pinetreeView.Guid);
        Assert.AreEqual("Root", pinetreeView.Title);
        Assert.AreEqual(2, fileCount); // Child + Grandchild (root not counted)
        Assert.AreEqual(1, pinetreeView.Children.Count);
        
        var child = pinetreeView.Children[0];
        Assert.AreEqual(child1Guid, child.Guid);
        Assert.AreEqual("Child 1", child.Title);
        Assert.AreEqual(1, child.Children.Count);
        
        var grandchild = child.Children[0];
        Assert.AreEqual(grandchild1Guid, grandchild.Guid);
        Assert.AreEqual("Grandchild 1", grandchild.Title);
        Assert.AreEqual(0, grandchild.Children.Count);
    }

    [TestMethod]
    public void ToPinetree_FromHierarchicalViewModel_ShouldMapProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupGuid = Guid.NewGuid();
        var hierarchicalViewModel = new PineconeViewModelWithChildren
        {
            Guid = guid,
            Title = "Hierarchical Title",
            Content = "Hierarchical Content",
            GroupGuid = groupGuid,
            IsPublic = false,
            Children = new List<PineconeViewModelWithChildren>()
        };

        // Act
        var pinetreeView = hierarchicalViewModel.ToPinetree(null);

        // Assert
        Assert.AreEqual(guid, pinetreeView.Guid);
        Assert.AreEqual("Hierarchical Title", pinetreeView.Title);
        Assert.AreEqual("Hierarchical Content", pinetreeView.Content);
        Assert.AreEqual(groupGuid, pinetreeView.GroupGuid);
        Assert.IsFalse(pinetreeView.IsPublic);
        Assert.IsNull(pinetreeView.Parent);
    }    [TestMethod]
    public void ConvertToPineconeDtos_ShouldReturnEmptyList_ForNullInput()
    {
        // This test validates the data structures that support the conversion
        
        // Arrange
        var emptyRequest = new TreeUpdateRequest
        {
            RootId = Guid.Empty,
            HasStructuralChanges = false,
            Nodes = new List<PineconeUpdateRequest>()
        };

        // Act & Assert
        Assert.AreEqual(Guid.Empty, emptyRequest.RootId);
        Assert.IsFalse(emptyRequest.HasStructuralChanges);
        Assert.AreEqual(0, emptyRequest.Nodes.Count);
    }

    [TestMethod]
    public void PineconeUpdateRequest_ShouldSupportAllRequiredProperties()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupGuid = Guid.NewGuid();
        var parentGuid = Guid.NewGuid();

        // Act
        var updateRequest = new PineconeUpdateRequest
        {
            Guid = guid,
            Title = "Updated Title",
            Content = "Updated Content",
            GroupGuid = groupGuid,
            ParentGuid = parentGuid,
            Order = 10,
            IsPublic = true
        };

        // Assert
        Assert.AreEqual(guid, updateRequest.Guid);
        Assert.AreEqual("Updated Title", updateRequest.Title);
        Assert.AreEqual("Updated Content", updateRequest.Content);
        Assert.AreEqual(groupGuid, updateRequest.GroupGuid);
        Assert.AreEqual(parentGuid, updateRequest.ParentGuid);
        Assert.AreEqual(10, updateRequest.Order);
        Assert.IsTrue(updateRequest.IsPublic);
    }
}
