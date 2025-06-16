using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinetree.Models;
using Pinetree.Shared.ViewModels;

namespace Pinetree.TestProject;

[TestClass]
public class PineconeModelViewModelTests
{
    [TestMethod]
    public void ToPineconeViewModel_ShouldMapAllProperties()
    {
        // Arrange
        var pinecone = new Pinecone
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            Title = "Test Title",
            Content = "Test Content",
            GroupGuid = Guid.NewGuid(),
            ParentGuid = null,
            Order = 1,
            IsPublic = true,
            UserName = "testuser",
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow
        };

        // Act
        var viewModel = ToPineconeViewModel(pinecone);

        // Assert
        Assert.AreEqual(pinecone.Guid, viewModel.Guid);
        Assert.AreEqual(pinecone.Title, viewModel.Title);
        Assert.AreEqual(pinecone.Content, viewModel.Content);
        Assert.AreEqual(pinecone.GroupGuid, viewModel.GroupGuid);
        Assert.AreEqual(pinecone.ParentGuid, viewModel.ParentGuid);
        Assert.AreEqual(pinecone.Order, viewModel.Order);
        Assert.AreEqual(pinecone.IsPublic, viewModel.IsPublic);
        Assert.AreEqual(pinecone.UserName, viewModel.UserName);
        Assert.AreEqual(pinecone.Create, viewModel.Create);
        Assert.AreEqual(pinecone.Update, viewModel.Update);
    }

    [TestMethod]
    public void UpdatePineconeFromRequest_ShouldUpdateAllProperties()
    {
        // Arrange
        var pinecone = new Pinecone
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            Title = "Old Title",
            Content = "Old Content",
            GroupGuid = Guid.NewGuid(),
            ParentGuid = null,
            Order = 1,
            IsPublic = false,
            UserName = "olduser",
            Create = DateTime.UtcNow.AddDays(-1),
            Update = DateTime.UtcNow.AddDays(-1)
        };

        var request = new PineconeUpdateRequest
        {
            Guid = pinecone.Guid,
            Title = "New Title",
            Content = "New Content",
            GroupGuid = Guid.NewGuid(),
            ParentGuid = Guid.NewGuid(),
            Order = 2,
            IsPublic = true
        };

        var userName = "newuser";
        var originalUpdate = pinecone.Update;

        // Act
        UpdatePineconeFromRequest(pinecone, request, userName);

        // Assert
        Assert.AreEqual(request.Title, pinecone.Title);
        Assert.AreEqual(request.Content, pinecone.Content);
        Assert.AreEqual(request.GroupGuid, pinecone.GroupGuid);
        Assert.AreEqual(request.ParentGuid, pinecone.ParentGuid);
        Assert.AreEqual(request.Order, pinecone.Order);
        Assert.AreEqual(request.IsPublic, pinecone.IsPublic);
        Assert.AreEqual(userName, pinecone.UserName);
        Assert.IsTrue(pinecone.Update > originalUpdate); // Update timestamp should be newer
    }

    [TestMethod]
    public void ToPineconeViewModelWithChildren_ShouldMapHierarchy()
    {
        // Arrange
        var parentGuid = Guid.NewGuid();
        var childGuid1 = Guid.NewGuid();
        var childGuid2 = Guid.NewGuid();

        var parent = new Pinecone
        {
            Id = 1,
            Guid = parentGuid,
            Title = "Parent Title",
            Content = "Parent Content",
            GroupGuid = parentGuid,
            ParentGuid = null,
            Order = 0,
            IsPublic = true,
            UserName = "testuser",
            Create = DateTime.UtcNow,
            Update = DateTime.UtcNow,
            Children = new List<Pinecone>
            {
                new Pinecone
                {
                    Id = 2,
                    Guid = childGuid1,
                    Title = "Child 1 Title",
                    Content = "Child 1 Content",
                    GroupGuid = parentGuid,
                    ParentGuid = parentGuid,
                    Order = 0,
                    IsPublic = true,
                    UserName = "testuser",
                    Create = DateTime.UtcNow,
                    Update = DateTime.UtcNow,
                    Children = new List<Pinecone>()
                },
                new Pinecone
                {
                    Id = 3,
                    Guid = childGuid2,
                    Title = "Child 2 Title",
                    Content = "Child 2 Content",
                    GroupGuid = parentGuid,
                    ParentGuid = parentGuid,
                    Order = 1,
                    IsPublic = true,
                    UserName = "testuser",
                    Create = DateTime.UtcNow,
                    Update = DateTime.UtcNow,
                    Children = new List<Pinecone>()
                }
            }
        };

        // Act
        var viewModel = ToPineconeViewModelWithChildren(parent);

        // Assert
        Assert.AreEqual(parent.Guid, viewModel.Guid);
        Assert.AreEqual(parent.Title, viewModel.Title);
        Assert.AreEqual(parent.Content, viewModel.Content);
        Assert.AreEqual(2, viewModel.Children.Count);
        
        var child1 = viewModel.Children.First(c => c.Guid == childGuid1);
        Assert.AreEqual("Child 1 Title", child1.Title);
        Assert.AreEqual("Child 1 Content", child1.Content);
        Assert.AreEqual(parentGuid, child1.ParentGuid);
        
        var child2 = viewModel.Children.First(c => c.Guid == childGuid2);
        Assert.AreEqual("Child 2 Title", child2.Title);
        Assert.AreEqual("Child 2 Content", child2.Content);
        Assert.AreEqual(parentGuid, child2.ParentGuid);
    }

    [TestMethod]
    public void TreeUpdateRequest_ShouldHaveValidProperties()
    {
        // Arrange & Act
        var request = new TreeUpdateRequest
        {
            RootId = Guid.NewGuid(),
            HasStructuralChanges = true,
            Nodes = new List<PineconeUpdateRequest>
            {
                new PineconeUpdateRequest
                {
                    Guid = Guid.NewGuid(),
                    Title = "Test Node",
                    Content = "Test Content",
                    GroupGuid = Guid.NewGuid(),
                    ParentGuid = null,
                    Order = 0,
                    IsPublic = false
                }
            }
        };

        // Assert
        Assert.IsTrue(request.RootId != Guid.Empty);
        Assert.IsTrue(request.HasStructuralChanges);
        Assert.AreEqual(1, request.Nodes.Count);
        Assert.AreEqual("Test Node", request.Nodes[0].Title);
        Assert.AreEqual("Test Content", request.Nodes[0].Content);
        Assert.IsFalse(request.Nodes[0].IsPublic);
    }

    [TestMethod]
    public void PineconeViewModel_Properties_ShouldBeSettable()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupGuid = Guid.NewGuid();
        var parentGuid = Guid.NewGuid();
        var createDate = DateTime.UtcNow;
        var updateDate = DateTime.UtcNow.AddMinutes(5);

        // Act
        var viewModel = new PineconeViewModel
        {
            Guid = guid,
            Title = "Test Title",
            Content = "Test Content",
            GroupGuid = groupGuid,
            ParentGuid = parentGuid,
            Order = 5,
            IsPublic = true,
            UserName = "testuser",
            Create = createDate,
            Update = updateDate
        };

        // Assert
        Assert.AreEqual(guid, viewModel.Guid);
        Assert.AreEqual("Test Title", viewModel.Title);
        Assert.AreEqual("Test Content", viewModel.Content);
        Assert.AreEqual(groupGuid, viewModel.GroupGuid);
        Assert.AreEqual(parentGuid, viewModel.ParentGuid);
        Assert.AreEqual(5, viewModel.Order);
        Assert.IsTrue(viewModel.IsPublic);
        Assert.AreEqual("testuser", viewModel.UserName);
        Assert.AreEqual(createDate, viewModel.Create);
        Assert.AreEqual(updateDate, viewModel.Update);
    }

    [TestMethod]
    public void PineconeViewModelWithChildren_ShouldExtendPineconeViewModel()
    {
        // Arrange & Act
        var viewModel = new PineconeViewModelWithChildren
        {
            Guid = Guid.NewGuid(),
            Title = "Parent",
            Content = "Parent Content",
            IsPublic = true,
            Children = new List<PineconeViewModelWithChildren>
            {
                new PineconeViewModelWithChildren
                {
                    Guid = Guid.NewGuid(),
                    Title = "Child",
                    Content = "Child Content",
                    Children = new List<PineconeViewModelWithChildren>()
                }
            }
        };

        // Assert
        Assert.AreEqual("Parent", viewModel.Title);
        Assert.AreEqual("Parent Content", viewModel.Content);
        Assert.IsTrue(viewModel.IsPublic);
        Assert.AreEqual(1, viewModel.Children.Count);
        Assert.AreEqual("Child", viewModel.Children[0].Title);
        Assert.AreEqual("Child Content", viewModel.Children[0].Content);
        Assert.AreEqual(0, viewModel.Children[0].Children.Count);
    }

    // Helper methods to simulate the conversion logic
    private static PineconeViewModel ToPineconeViewModel(Pinecone model)
    {
        return new PineconeViewModel
        {
            Guid = model.Guid,
            Title = model.Title,
            Content = model.Content,
            GroupGuid = model.GroupGuid,
            ParentGuid = model.ParentGuid,
            Order = model.Order,
            IsPublic = model.IsPublic,
            UserName = model.UserName,
            Create = model.Create,
            Update = model.Update
        };
    }

    private static void UpdatePineconeFromRequest(Pinecone pinecone, PineconeUpdateRequest request, string userName)
    {
        pinecone.Title = request.Title;
        pinecone.Content = request.Content;
        pinecone.GroupGuid = request.GroupGuid;
        pinecone.ParentGuid = request.ParentGuid;
        pinecone.Order = request.Order;
        pinecone.IsPublic = request.IsPublic;
        pinecone.UserName = userName;
        pinecone.Update = DateTime.UtcNow;
    }

    private static PineconeViewModelWithChildren ToPineconeViewModelWithChildren(Pinecone model)
    {
        var viewModel = new PineconeViewModelWithChildren
        {
            Guid = model.Guid,
            Title = model.Title,
            Content = model.Content,
            GroupGuid = model.GroupGuid,
            ParentGuid = model.ParentGuid,
            Order = model.Order,
            IsPublic = model.IsPublic,
            UserName = model.UserName,
            Create = model.Create,
            Update = model.Update,
            Children = new List<PineconeViewModelWithChildren>()
        };

        if (model.Children != null && model.Children.Count > 0)
        {
            foreach (var child in model.Children.OrderBy(c => c.Order))
            {
                var childViewModel = ToPineconeViewModelWithChildren(child);
                viewModel.Children.Add(childViewModel);
            }
        }

        return viewModel;
    }
}
