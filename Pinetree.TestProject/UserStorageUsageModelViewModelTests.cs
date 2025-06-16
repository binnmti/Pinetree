using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinetree.Models;
using Pinetree.Shared.ViewModels;

namespace Pinetree.TestProject;

[TestClass]
public class UserStorageUsageModelViewModelTests
{
    [TestMethod]
    public void UserStorageUsageViewModel_Properties_ShouldBeSettable()
    {
        // Arrange
        var userName = "testuser";
        var totalSizeInBytes = 1024L;
        var quotaInBytes = 10240L;
        var lastUpdated = DateTime.UtcNow;

        // Act
        var viewModel = new UserStorageUsageViewModel
        {
            UserName = userName,
            TotalSizeInBytes = totalSizeInBytes,
            QuotaInBytes = quotaInBytes,
            LastUpdated = lastUpdated
        };

        // Assert
        Assert.AreEqual(userName, viewModel.UserName);
        Assert.AreEqual(totalSizeInBytes, viewModel.TotalSizeInBytes);
        Assert.AreEqual(quotaInBytes, viewModel.QuotaInBytes);
        Assert.AreEqual(lastUpdated, viewModel.LastUpdated);
    }

    [TestMethod]
    public void UserStorageUsageViewModel_CalculatedProperties_ShouldBeCorrect()
    {
        // Arrange
        var totalSizeInBytes = 2048L;
        var quotaInBytes = 10240L;

        // Act
        var viewModel = new UserStorageUsageViewModel
        {
            UserName = "testuser",
            TotalSizeInBytes = totalSizeInBytes,
            QuotaInBytes = quotaInBytes,
            LastUpdated = DateTime.UtcNow
        };

        // Assert
        Assert.AreEqual(20.0, viewModel.UsagePercentage); // 2048 / 10240 * 100 = 20%
        Assert.AreEqual(8192L, viewModel.RemainingBytes); // 10240 - 2048 = 8192
        Assert.IsFalse(viewModel.IsOverQuota);
    }

    [TestMethod]
    public void UserStorageUsageViewModel_OverQuota_ShouldBeCorrect()
    {
        // Arrange
        var totalSizeInBytes = 15360L; // Over quota
        var quotaInBytes = 10240L;

        // Act
        var viewModel = new UserStorageUsageViewModel
        {
            UserName = "testuser",
            TotalSizeInBytes = totalSizeInBytes,
            QuotaInBytes = quotaInBytes,
            LastUpdated = DateTime.UtcNow
        };

        // Assert
        Assert.AreEqual(150.0, viewModel.UsagePercentage); // 15360 / 10240 * 100 = 150%
        Assert.AreEqual(0L, viewModel.RemainingBytes); // Max(0, 10240 - 15360) = 0
        Assert.IsTrue(viewModel.IsOverQuota);
    }

    [TestMethod]
    public void UserStorageUsageViewModel_ZeroQuota_ShouldHandleGracefully()
    {
        // Arrange
        var totalSizeInBytes = 1024L;
        var quotaInBytes = 0L;

        // Act
        var viewModel = new UserStorageUsageViewModel
        {
            UserName = "testuser",
            TotalSizeInBytes = totalSizeInBytes,
            QuotaInBytes = quotaInBytes,
            LastUpdated = DateTime.UtcNow
        };

        // Assert
        Assert.AreEqual(0.0, viewModel.UsagePercentage); // Should not divide by zero
        Assert.AreEqual(0L, viewModel.RemainingBytes);
        Assert.IsTrue(viewModel.IsOverQuota);
    }

    [TestMethod]
    public void UserStorageUsageUpdateRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var userName = "testuser";
        var totalSizeInBytes = 5120L;
        var quotaInBytes = 51200L;

        // Act
        var request = new UserStorageUsageUpdateRequest
        {
            UserName = userName,
            TotalSizeInBytes = totalSizeInBytes,
            QuotaInBytes = quotaInBytes
        };

        // Assert
        Assert.AreEqual(userName, request.UserName);
        Assert.AreEqual(totalSizeInBytes, request.TotalSizeInBytes);
        Assert.AreEqual(quotaInBytes, request.QuotaInBytes);
    }

    [TestMethod]
    public void UserStorageUsage_Properties_ShouldBeSettable()
    {
        // Arrange
        var id = 123;
        var userName = "testuser";
        var totalSizeInBytes = 2048L;
        var quotaInBytes = 20480L;
        var lastUpdated = DateTime.UtcNow;

        // Act
        var model = new UserStorageUsage
        {
            Id = id,
            UserName = userName,
            TotalSizeInBytes = totalSizeInBytes,
            QuotaInBytes = quotaInBytes,
            LastUpdated = lastUpdated
        };

        // Assert
        Assert.AreEqual(id, model.Id);
        Assert.AreEqual(userName, model.UserName);
        Assert.AreEqual(totalSizeInBytes, model.TotalSizeInBytes);
        Assert.AreEqual(quotaInBytes, model.QuotaInBytes);
        Assert.AreEqual(lastUpdated, model.LastUpdated);
    }
}
