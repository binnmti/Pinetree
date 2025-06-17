using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinetree.Models;
using Pinetree.Shared.ViewModels;

namespace Pinetree.TestProject;

[TestClass]
public class AuditLogModelViewModelTests
{
    [TestMethod]
    public void AuditLogViewModel_Properties_ShouldBeSettable()
    {
        // Arrange
        var id = 123;
        var timestamp = DateTime.UtcNow;
        var httpMethod = "GET";
        var requestPath = "/api/test";
        var queryString = "param=value";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var userId = "user123";
        var userName = "testuser";
        var userRole = "User";
        var statusCode = 200;
        var responseTimeMs = 150L;
        var errorMessage = "Test error";
        var additionalData = "{\"key\":\"value\"}";
        var auditCategory = "API";
        var priority = "Medium";
        var createdAt = DateTime.UtcNow;

        // Act
        var viewModel = new AuditLogViewModel
        {
            Id = id,
            Timestamp = timestamp,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            QueryString = queryString,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            UserId = userId,
            UserName = userName,
            UserRole = userRole,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            AdditionalData = additionalData,
            AuditCategory = auditCategory,
            Priority = priority,
            IsSuccess = true,
            CreatedAt = createdAt
        };

        // Assert
        Assert.AreEqual(id, viewModel.Id);
        Assert.AreEqual(timestamp, viewModel.Timestamp);
        Assert.AreEqual(httpMethod, viewModel.HttpMethod);
        Assert.AreEqual(requestPath, viewModel.RequestPath);
        Assert.AreEqual(queryString, viewModel.QueryString);
        Assert.AreEqual(ipAddress, viewModel.IpAddress);
        Assert.AreEqual(userAgent, viewModel.UserAgent);
        Assert.AreEqual(userId, viewModel.UserId);
        Assert.AreEqual(userName, viewModel.UserName);
        Assert.AreEqual(userRole, viewModel.UserRole);
        Assert.AreEqual(statusCode, viewModel.StatusCode);
        Assert.AreEqual(responseTimeMs, viewModel.ResponseTimeMs);
        Assert.AreEqual(errorMessage, viewModel.ErrorMessage);
        Assert.AreEqual(additionalData, viewModel.AdditionalData);
        Assert.AreEqual(auditCategory, viewModel.AuditCategory);
        Assert.AreEqual(priority, viewModel.Priority);
        Assert.IsTrue(viewModel.IsSuccess);
        Assert.AreEqual(createdAt, viewModel.CreatedAt);
    }

    [TestMethod]
    public void AuditLogFilterRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var userName = "testuser";
        var httpMethod = "POST";
        var statusCode = 404;
        var auditCategory = "Authentication";
        var priority = "High";
        var page = 2;
        var pageSize = 25;

        // Act
        var filter = new AuditLogFilterRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            UserName = userName,
            HttpMethod = httpMethod,
            StatusCode = statusCode,
            AuditCategory = auditCategory,
            Priority = priority,
            Page = page,
            PageSize = pageSize
        };

        // Assert
        Assert.AreEqual(startDate, filter.StartDate);
        Assert.AreEqual(endDate, filter.EndDate);
        Assert.AreEqual(userName, filter.UserName);
        Assert.AreEqual(httpMethod, filter.HttpMethod);
        Assert.AreEqual(statusCode, filter.StatusCode);
        Assert.AreEqual(auditCategory, filter.AuditCategory);
        Assert.AreEqual(priority, filter.Priority);
        Assert.AreEqual(page, filter.Page);
        Assert.AreEqual(pageSize, filter.PageSize);
    }

    [TestMethod]
    public void AuditLogFilterRequest_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var filter = new AuditLogFilterRequest();

        // Assert
        Assert.IsNull(filter.StartDate);
        Assert.IsNull(filter.EndDate);
        Assert.IsNull(filter.UserName);
        Assert.IsNull(filter.HttpMethod);
        Assert.IsNull(filter.StatusCode);
        Assert.IsNull(filter.AuditCategory);
        Assert.IsNull(filter.Priority);
        Assert.AreEqual(1, filter.Page);
        Assert.AreEqual(50, filter.PageSize);
    }

    [TestMethod]
    public void AuditLog_IsSuccess_ShouldCalculateCorrectly()
    {
        // Test success status codes
        var successCodes = new[] { 200, 201, 204, 302, 304, 399 };
        foreach (var code in successCodes)
        {
            var log = new AuditLog { StatusCode = code };
            Assert.IsTrue(log.IsSuccess, $"Status code {code} should be considered success");
        }

        // Test error status codes
        var errorCodes = new[] { 400, 401, 403, 404, 500, 502, 503 };
        foreach (var code in errorCodes)
        {
            var log = new AuditLog { StatusCode = code };
            Assert.IsFalse(log.IsSuccess, $"Status code {code} should be considered error");
        }
    }

    [TestMethod]
    public void AuditLog_Properties_ShouldBeSettable()
    {
        // Arrange
        var id = 456;
        var timestamp = DateTime.UtcNow;
        var httpMethod = "POST";
        var requestPath = "/api/create";
        var queryString = "format=json";
        var ipAddress = "10.0.0.1";
        var userAgent = "Chrome/91.0";
        var userId = "user456";
        var userName = "adminuser";
        var userRole = "Admin";
        var statusCode = 201;
        var responseTimeMs = 75L;
        var errorMessage = "Validation error";
        var additionalData = "{\"operation\":\"create\"}";
        var auditCategory = "DataModification";
        var priority = "High";
        var createdAt = DateTime.UtcNow;
        var hashValue = "abc123hash";

        // Act
        var model = new AuditLog
        {
            Id = id,
            Timestamp = timestamp,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            QueryString = queryString,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            UserId = userId,
            UserName = userName,
            UserRole = userRole,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            AdditionalData = additionalData,
            AuditCategory = auditCategory,
            Priority = priority,
            CreatedAt = createdAt,
            HashValue = hashValue
        };

        // Assert
        Assert.AreEqual(id, model.Id);
        Assert.AreEqual(timestamp, model.Timestamp);
        Assert.AreEqual(httpMethod, model.HttpMethod);
        Assert.AreEqual(requestPath, model.RequestPath);
        Assert.AreEqual(queryString, model.QueryString);
        Assert.AreEqual(ipAddress, model.IpAddress);
        Assert.AreEqual(userAgent, model.UserAgent);
        Assert.AreEqual(userId, model.UserId);
        Assert.AreEqual(userName, model.UserName);
        Assert.AreEqual(userRole, model.UserRole);
        Assert.AreEqual(statusCode, model.StatusCode);
        Assert.AreEqual(responseTimeMs, model.ResponseTimeMs);
        Assert.AreEqual(errorMessage, model.ErrorMessage);
        Assert.AreEqual(additionalData, model.AdditionalData);
        Assert.AreEqual(auditCategory, model.AuditCategory);
        Assert.AreEqual(priority, model.Priority);
        Assert.AreEqual(createdAt, model.CreatedAt);
        Assert.AreEqual(hashValue, model.HashValue);
        Assert.IsTrue(model.IsSuccess); // 201 is success
    }
}
