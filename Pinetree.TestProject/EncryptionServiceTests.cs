using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pinetree.Data;
using Pinetree.Services;

namespace Pinetree.TestProject;

[TestClass]
public class EncryptionServiceTests
{
    private static ServiceProvider? _serviceProvider;
    private static string? _testUserId;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Create in-memory database and services - shared across all tests
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("EncryptionTestDb"));
        
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        
        services.AddScoped<EncryptionService>();
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Ensure database is created and create test user
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var testUser = new ApplicationUser
        {
            UserName = "testuser@example.com",
            Email = "testuser@example.com"
        };
        
        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        
        _testUserId = testUser.Id;
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _serviceProvider?.Dispose();
    }[TestMethod]
    public async Task EncryptDecrypt_ValidContent_ReturnsOriginalText()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var originalText = "This is a secret message that should be encrypted!";
        
        // Act
        var encryptedText = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);
        var decryptedText = await encryptionService.DecryptContentAsync(encryptedText, false, _testUserId!);
        
        // Assert
        Assert.IsNotNull(encryptedText);
        Assert.AreNotEqual(originalText, encryptedText, "Encrypted text should be different from original");
        Assert.AreEqual(originalText, decryptedText, "Decrypted text should match original");
    }

    [TestMethod]
    public async Task EncryptContentAsync_PublicContent_ReturnsUnchanged()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var publicContent = "This is public content";
        
        // Act
        var result = await encryptionService.EncryptContentAsync(publicContent, true, _testUserId!);
        
        // Assert
        Assert.AreEqual(publicContent, result, "Public content should not be encrypted");
    }

    [TestMethod]
    public async Task DecryptContentAsync_PublicContent_ReturnsUnchanged()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var publicContent = "This is public content";
        
        // Act
        var result = await encryptionService.DecryptContentAsync(publicContent, true, _testUserId!);
        
        // Assert
        Assert.AreEqual(publicContent, result, "Public content should not be decrypted");
    }

    [TestMethod]
    public async Task DecryptContentAsync_TamperedCiphertext_ThrowsException()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var originalText = "Secret message";
        
        // Act - encrypt first
        var encryptedText = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);
        Assert.IsNotNull(encryptedText);
        
        // Tamper with the encrypted text
        var tamperedText = encryptedText.Substring(0, encryptedText.Length - 5) + "XXXXX";
        
        // Assert - should return original content for backward compatibility
        var result = await encryptionService.DecryptContentAsync(tamperedText, false, _testUserId!);
        Assert.AreEqual(tamperedText, result, "Should return original content when decryption fails");
    }

    [TestMethod]
    public async Task EncryptContentAsync_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        
        // Act & Assert
        var result1 = await encryptionService.EncryptContentAsync("", false, _testUserId!);
        var result2 = await encryptionService.EncryptContentAsync(null, false, _testUserId!);
        
        Assert.AreEqual("", result1);
        Assert.IsNull(result2);
    }

    [TestMethod]
    public async Task EncryptContentAsync_InvalidUserId_ThrowsException()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var invalidUserId = Guid.NewGuid().ToString();
        
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => encryptionService.EncryptContentAsync("secret", false, invalidUserId));
    }    [TestMethod]
    public async Task EncryptDecrypt_DifferentUsers_ProduceDifferentResults()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Create second user
        var user2 = new ApplicationUser
        {
            UserName = "testuser2@example.com",
            Email = "testuser2@example.com"
        };
        var result = await userManager.CreateAsync(user2, "TestPassword456!");
        Assert.IsTrue(result.Succeeded);
        
        var originalText = "Same secret message";
        
        // Act
        var encrypted1 = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);
        var encrypted2 = await encryptionService.EncryptContentAsync(originalText, false, user2.Id);
        
        // Assert
        Assert.AreNotEqual(encrypted1, encrypted2, "Different users should produce different encrypted texts");
        
        // Both should decrypt correctly with their respective keys
        var decrypted1 = await encryptionService.DecryptContentAsync(encrypted1, false, _testUserId!);
        var decrypted2 = await encryptionService.DecryptContentAsync(encrypted2, false, user2.Id);
        
        Assert.AreEqual(originalText, decrypted1);
        Assert.AreEqual(originalText, decrypted2);
    }

    [TestMethod]
    public async Task EncryptDecrypt_SameContent_ProducesDifferentCiphertext()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var originalText = "Same message encrypted twice";
        
        // Act - encrypt the same content twice
        var encrypted1 = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);
        var encrypted2 = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);
        
        // Assert - ciphertext should be different due to random nonce
        Assert.AreNotEqual(encrypted1, encrypted2, "Same content encrypted twice should produce different ciphertext");
        
        // Both should decrypt to the same original text
        var decrypted1 = await encryptionService.DecryptContentAsync(encrypted1, false, _testUserId!);
        var decrypted2 = await encryptionService.DecryptContentAsync(encrypted2, false, _testUserId!);
        
        Assert.AreEqual(originalText, decrypted1);
        Assert.AreEqual(originalText, decrypted2);
    }
}
