using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinetree.Models;
using Pinetree.Shared.ViewModels;

namespace Pinetree.TestProject;

[TestClass]
public class UserBlobModelViewModelTests
{
    [TestMethod]
    public void UserBlobViewModel_Properties_ShouldBeSettable()
    {
        // Arrange
        var blobId = 123;
        var blobUrl = "https://example.com/blob.jpg";
        var fileName = "test-image.jpg";
        var sizeInBytes = 1024L;
        var contentType = "image/jpeg";
        var pineconeGuid = Guid.NewGuid();
        var pineconeTitle = "Test Pinecone";
        var uploadedAt = DateTime.UtcNow;

        // Act
        var viewModel = new UserBlobViewModel
        {
            Id = blobId,
            BlobUrl = blobUrl,
            FileName = fileName,
            SizeInBytes = sizeInBytes,
            ContentType = contentType,
            PineconeGuid = pineconeGuid,
            PineconeTitle = pineconeTitle,
            UploadedAt = uploadedAt
        };

        // Assert
        Assert.AreEqual(blobId, viewModel.Id);
        Assert.AreEqual(blobUrl, viewModel.BlobUrl);
        Assert.AreEqual(fileName, viewModel.FileName);
        Assert.AreEqual(sizeInBytes, viewModel.SizeInBytes);
        Assert.AreEqual(contentType, viewModel.ContentType);
        Assert.AreEqual(pineconeGuid, viewModel.PineconeGuid);
        Assert.AreEqual(pineconeTitle, viewModel.PineconeTitle);
        Assert.AreEqual(uploadedAt, viewModel.UploadedAt);
    }

    [TestMethod]
    public void UserBlobUploadRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var extension = ".jpg";
        var pineconeGuid = Guid.NewGuid();
        var base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";

        // Act
        var request = new UserBlobUploadRequest
        {
            Extension = extension,
            PineconeGuid = pineconeGuid,
            Base64Data = base64Data
        };

        // Assert
        Assert.AreEqual(extension, request.Extension);
        Assert.AreEqual(pineconeGuid, request.PineconeGuid);
        Assert.AreEqual(base64Data, request.Base64Data);
    }

    [TestMethod]
    public void UserBlobUploadResponse_Properties_ShouldBeSettable()
    {
        // Arrange
        var url = "https://example.com/uploaded-blob.jpg";
        var id = 456;
        var sizeInBytes = 2048L;

        // Act
        var response = new UserBlobUploadResponse
        {
            Url = url,
            Id = id,
            SizeInBytes = sizeInBytes
        };

        // Assert
        Assert.AreEqual(url, response.Url);
        Assert.AreEqual(id, response.Id);
        Assert.AreEqual(sizeInBytes, response.SizeInBytes);
    }

    [TestMethod]
    public void UserBlobInfo_Properties_ShouldBeSettable()
    {
        // Arrange
        var id = 789;
        var userName = "testuser";
        var blobName = "test-blob.jpg";
        var sizeInBytes = 4096L;
        var contentType = "image/jpeg";
        var pineconeGuid = Guid.NewGuid();
        var uploadedAt = DateTime.UtcNow;
        var deletedAt = DateTime.UtcNow.AddDays(1);

        // Act
        var model = new UserBlobInfo
        {
            Id = id,
            UserName = userName,
            BlobName = blobName,
            SizeInBytes = sizeInBytes,
            ContentType = contentType,
            PineconeGuid = pineconeGuid,
            IsDeleted = true,
            UploadedAt = uploadedAt,
            DeletedAt = deletedAt
        };

        // Assert
        Assert.AreEqual(id, model.Id);
        Assert.AreEqual(userName, model.UserName);
        Assert.AreEqual(blobName, model.BlobName);
        Assert.AreEqual(sizeInBytes, model.SizeInBytes);
        Assert.AreEqual(contentType, model.ContentType);
        Assert.AreEqual(pineconeGuid, model.PineconeGuid);
        Assert.IsTrue(model.IsDeleted);
        Assert.AreEqual(uploadedAt, model.UploadedAt);
        Assert.AreEqual(deletedAt, model.DeletedAt);
    }
}
