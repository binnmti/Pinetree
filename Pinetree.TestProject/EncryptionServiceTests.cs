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
        var services = new ServiceCollection(); services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("EncryptionServiceTestDb_Fixed"));

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
    }
    [TestMethod]
    public async Task EncryptDecrypt_ValidContent_ReturnsOriginalText()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create test user for this specific test
        var testUser = new ApplicationUser
        {
            UserName = "encrypttest@example.com",
            Email = "encrypttest@example.com"
        };
        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        Assert.IsTrue(result.Succeeded);

        var originalText = "This is a secret message that should be encrypted!";

        // Act
        var encryptedText = await encryptionService.EncryptContentAsync(originalText, false, testUser.Id);
        var decryptedText = await encryptionService.DecryptContentAsync(encryptedText, false, testUser.Id);

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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create test user for this specific test
        var testUser = new ApplicationUser
        {
            UserName = "publictest@example.com",
            Email = "publictest@example.com"
        };
        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        Assert.IsTrue(result.Succeeded);

        var publicContent = "This is public content";

        // Act
        var result2 = await encryptionService.EncryptContentAsync(publicContent, true, testUser.Id);

        // Assert
        Assert.AreEqual(publicContent, result2, "Public content should not be encrypted");
    }

    [TestMethod]
    public async Task DecryptContentAsync_PublicContent_ReturnsUnchanged()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create test user for this specific test
        var testUser = new ApplicationUser
        {
            UserName = "publicdecrypttest@example.com",
            Email = "publicdecrypttest@example.com"
        };
        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        Assert.IsTrue(result.Succeeded);

        var publicContent = "This is public content";

        // Act
        var result2 = await encryptionService.DecryptContentAsync(publicContent, true, testUser.Id);

        // Assert
        Assert.AreEqual(publicContent, result2, "Public content should not be decrypted");
    }

    [TestMethod]
    public async Task DecryptContentAsync_TamperedCiphertext_ThrowsException()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create test user for this specific test
        var testUser = new ApplicationUser
        {
            UserName = "tamperedtest@example.com",
            Email = "tamperedtest@example.com"
        };
        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        Assert.IsTrue(result.Succeeded);

        var originalText = "Secret message";

        // Act - encrypt first
        var encryptedText = await encryptionService.EncryptContentAsync(originalText, false, testUser.Id);
        Assert.IsNotNull(encryptedText);

        // Tamper with the encrypted text
        var tamperedText = encryptedText.Substring(0, encryptedText.Length - 5) + "XXXXX";

        // Assert - should throw exception for tampered data
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => encryptionService.DecryptContentAsync(tamperedText, false, testUser.Id));
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
    }

    [TestMethod]
    public async Task EncryptDecrypt_DifferentUsers_ProduceDifferentResults()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create first user
        var user1 = new ApplicationUser
        {
            UserName = "testuser1@example.com",
            Email = "testuser1@example.com"
        };
        var result1 = await userManager.CreateAsync(user1, "TestPassword123!");
        Assert.IsTrue(result1.Succeeded);

        // Create second user
        var user2 = new ApplicationUser
        {
            UserName = "testuser2@example.com",
            Email = "testuser2@example.com"
        };
        var result2 = await userManager.CreateAsync(user2, "TestPassword456!");
        Assert.IsTrue(result2.Succeeded);

        var originalText = "Same secret message";

        // Act
        var encrypted1 = await encryptionService.EncryptContentAsync(originalText, false, user1.Id);
        var encrypted2 = await encryptionService.EncryptContentAsync(originalText, false, user2.Id);

        // Assert
        Assert.AreNotEqual(encrypted1, encrypted2, "Different users should produce different encrypted texts");

        // Both should decrypt correctly with their respective keys
        var decrypted1 = await encryptionService.DecryptContentAsync(encrypted1, false, user1.Id);
        var decrypted2 = await encryptionService.DecryptContentAsync(encrypted2, false, user2.Id);

        Assert.AreEqual(originalText, decrypted1);
        Assert.AreEqual(originalText, decrypted2);
    }
    [TestMethod]
    public async Task EncryptDecrypt_SameContent_ProducesDifferentCiphertext()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create test user for this specific test
        var testUser = new ApplicationUser
        {
            UserName = "samecontenttest@example.com",
            Email = "samecontenttest@example.com"
        };
        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        Assert.IsTrue(result.Succeeded);

        var originalText = "Same message encrypted twice";

        // Pre-generate the key to ensure consistency
        await encryptionService.GetUserEncryptionKeyAsync(testUser.Id);

        // Act - encrypt the same content twice
        var encrypted1 = await encryptionService.EncryptContentAsync(originalText, false, testUser.Id);
        var encrypted2 = await encryptionService.EncryptContentAsync(originalText, false, testUser.Id);

        // Assert - ciphertext should be different due to random nonce
        Assert.AreNotEqual(encrypted1, encrypted2, "Same content encrypted twice should produce different ciphertext");

        // Both should decrypt to the same original text
        var decrypted1 = await encryptionService.DecryptContentAsync(encrypted1, false, testUser.Id);
        var decrypted2 = await encryptionService.DecryptContentAsync(encrypted2, false, testUser.Id);

        Assert.AreEqual(originalText, decrypted1);
        Assert.AreEqual(originalText, decrypted2);
    }

    // === Key Management Tests (from EncryptionKeyServiceTests) ===

    [TestMethod]
    public async Task GetUserEncryptionKeyAsync_FirstTime_GeneratesAndStoresKey()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create test user for this specific test
        var testUser = new ApplicationUser
        {
            UserName = "firsttimetest@example.com",
            Email = "firsttimetest@example.com"
        };
        var result = await userManager.CreateAsync(testUser, "TestPassword123!");
        Assert.IsTrue(result.Succeeded);

        // Act
        var key1 = await encryptionService.GetUserEncryptionKeyAsync(testUser.Id);
        var key2 = await encryptionService.GetUserEncryptionKeyAsync(testUser.Id);

        // Assert
        Assert.IsNotNull(key1);
        Assert.AreEqual(32, key1.Length, "Key should be 256 bits (32 bytes)");
        CollectionAssert.AreEqual(key1, key2, "Same user should get the same key");
    }

    [TestMethod]
    public async Task GetUserEncryptionKeyAsync_InvalidUser_ThrowsException()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var invalidUserId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => encryptionService.GetUserEncryptionKeyAsync(invalidUserId));
    }

    [TestMethod]
    public async Task ReEncryptUserKeyAfterPasswordChange_ValidScenario_MaintainsKeyAccess()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create a user with a key
        var user = new ApplicationUser
        {
            UserName = "reencrypttest@example.com",
            Email = "reencrypttest@example.com"
        };
        var result = await userManager.CreateAsync(user, "OldPassword123!");
        Assert.IsTrue(result.Succeeded);

        // Generate initial key
        var originalKey = await encryptionService.GetUserEncryptionKeyAsync(user.Id);
        var oldPasswordHash = user.PasswordHash!;

        // Change password
        var changeResult = await userManager.ChangePasswordAsync(user, "OldPassword123!", "NewPassword456!");
        Assert.IsTrue(changeResult.Succeeded);

        // Act - Re-encrypt key with new password
        await encryptionService.ReEncryptUserKeyAfterPasswordChangeAsync(user.Id, oldPasswordHash);

        // Assert - Should still get the same key
        var newKey = await encryptionService.GetUserEncryptionKeyAsync(user.Id);
        CollectionAssert.AreEqual(originalKey, newKey, "Key should remain the same after re-encryption");
    }

    [TestMethod]
    public async Task ReEncryptUserKeyAfterPasswordChange_NoExistingKey_HandlesGracefully()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "nokey@example.com",
            Email = "nokey@example.com"
        };
        var result = await userManager.CreateAsync(user, "Password123!");
        Assert.IsTrue(result.Succeeded);

        var oldPasswordHash = user.PasswordHash!;

        // Act & Assert - Should not throw exception
        await encryptionService.ReEncryptUserKeyAfterPasswordChangeAsync(user.Id, oldPasswordHash);
    }

    [TestMethod]
    public async Task EncryptionKey_PersistsAcrossSessions_ConsistentKey()
    {
        // Arrange: Create user first
        using var setupScope = _serviceProvider!.CreateScope();
        var setupUserManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testUser = new ApplicationUser
        {
            UserName = "sessiontest@example.com",
            Email = "sessiontest@example.com"
        };
        var result = await setupUserManager.CreateAsync(testUser, "TestPassword123!");
        Assert.IsTrue(result.Succeeded);

        byte[] key1, key2;

        // First session - get key
        using (var scope1 = _serviceProvider!.CreateScope())
        {
            var encryptionService1 = scope1.ServiceProvider.GetRequiredService<EncryptionService>();
            key1 = await encryptionService1.GetUserEncryptionKeyAsync(testUser.Id);
        }

        // Second session - get key again (should be the same)
        using (var scope2 = _serviceProvider!.CreateScope())
        {
            var encryptionService2 = scope2.ServiceProvider.GetRequiredService<EncryptionService>();
            key2 = await encryptionService2.GetUserEncryptionKeyAsync(testUser.Id);
        }

        // Assert
        CollectionAssert.AreEqual(key1, key2, "Key should be consistent across sessions");
    }

    [TestMethod]
    public async Task EncryptionKey_DifferentUsers_DifferentKeys()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create first user
        var user1 = new ApplicationUser
        {
            UserName = "diffuser1key@example.com",
            Email = "diffuser1key@example.com"
        };
        var result1 = await userManager.CreateAsync(user1, "Password123!");
        Assert.IsTrue(result1.Succeeded);

        // Create second user
        var user2 = new ApplicationUser
        {
            UserName = "diffuser2key@example.com",
            Email = "diffuser2key@example.com"
        };
        var result2 = await userManager.CreateAsync(user2, "Password123!");
        Assert.IsTrue(result2.Succeeded);

        // Act
        var key1 = await encryptionService.GetUserEncryptionKeyAsync(user1.Id);
        var key2 = await encryptionService.GetUserEncryptionKeyAsync(user2.Id);

        // Assert
        CollectionAssert.AreNotEqual(key1, key2, "Different users should have different keys");
    }

    // === Debug Tests (from EncryptionDebugTests) ===

    [TestMethod]
    public async Task Debug_KeyConsistency_SameUserSameKey()
    {
        // Use a single scope for the entire test
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create user
        var user = new ApplicationUser
        {
            UserName = "debug@example.com",
            Email = "debug@example.com"
        };
        var createResult = await userManager.CreateAsync(user, "TestPassword123!");
        Assert.IsTrue(createResult.Succeeded, $"User creation failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        // Get key first time
        var key1 = await encryptionService.GetUserEncryptionKeyAsync(user.Id);

        // Get key second time - should be the same
        var key2 = await encryptionService.GetUserEncryptionKeyAsync(user.Id);

        // Compare keys
        CollectionAssert.AreEqual(key1, key2, "Same user should get the same key");
    }

    [TestMethod]
    public async Task Debug_EncryptionDecryption_SingleScope()
    {
        // Use a single scope for the entire test
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Create user
        var user = new ApplicationUser
        {
            UserName = "encrypt@example.com",
            Email = "encrypt@example.com"
        };
        var createResult = await userManager.CreateAsync(user, "TestPassword123!");
        Assert.IsTrue(createResult.Succeeded);

        var originalText = "This is a secret message that should be encrypted!";

        // Encrypt
        var encrypted = await encryptionService.EncryptContentAsync(originalText, false, user.Id);
        Assert.IsNotNull(encrypted);
        Assert.AreNotEqual(originalText, encrypted);

        // Decrypt
        var decrypted = await encryptionService.DecryptContentAsync(encrypted, false, user.Id);

        Assert.AreEqual(originalText, decrypted, "Decrypted text should match original");
    }

    #region Legacy Data Compatibility Tests

    [TestMethod]
    public async Task LegacyData_PlainTextContent_ShouldReturnAsIs()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        var plainTextContent = "This is legacy plain text data";

        // Decrypt should return plain text as-is when no encryption marker is present
        var result = await encryptionService.DecryptContentAsync(plainTextContent, false, _testUserId!);

        Assert.AreEqual(plainTextContent, result, "Legacy plain text should be returned unchanged");
    }

    [TestMethod]
    public async Task EncryptionMarker_NewContent_ShouldHaveMarker()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        var originalText = "This is new content to be encrypted";

        // Encrypt new content
        var encrypted = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);

        Assert.IsNotNull(encrypted);
        Assert.IsTrue(encrypted.StartsWith("ENC_V1:"), "Encrypted content should have encryption marker");
        Assert.AreNotEqual(originalText, encrypted, "Encrypted content should be different from original");
    }

    [TestMethod]
    public async Task EncryptionMarker_AlreadyEncrypted_ShouldNotDoubleEncrypt()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        var originalText = "This content will be encrypted once";

        // First encryption
        var firstEncryption = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);
        Assert.IsTrue(firstEncryption!.StartsWith("ENC_V1:"));

        // Second encryption attempt - should return same result
        var secondEncryption = await encryptionService.EncryptContentAsync(firstEncryption, false, _testUserId!);

        Assert.AreEqual(firstEncryption, secondEncryption, "Already encrypted content should not be encrypted again");
    }

    [TestMethod]
    public async Task EncryptionMarker_RoundTrip_ShouldWorkCorrectly()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        var originalText = "Test round trip encryption with marker";

        // Encrypt
        var encrypted = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);
        Assert.IsTrue(encrypted!.StartsWith("ENC_V1:"));

        // Decrypt
        var decrypted = await encryptionService.DecryptContentAsync(encrypted, false, _testUserId!);

        Assert.AreEqual(originalText, decrypted, "Round trip should preserve original content");
    }

    [TestMethod]
    public async Task LegacyDataMigration_TryMigrateLegacyData_ShouldReturnTrue()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        var plainTextContent = "Legacy plain text to migrate";

        // Try to migrate legacy data
        var migrationResult = await encryptionService.TryMigrateLegacyDataAsync(_testUserId!, "test-item-id", plainTextContent);

        Assert.IsTrue(migrationResult, "Legacy data migration should succeed");
    }

    [TestMethod]
    public async Task LegacyDataMigration_AlreadyEncrypted_ShouldReturnFalse()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        var originalText = "Content to encrypt first";
        var encryptedContent = await encryptionService.EncryptContentAsync(originalText, false, _testUserId!);

        // Try to migrate already encrypted data
        var migrationResult = await encryptionService.TryMigrateLegacyDataAsync(_testUserId!, "test-item-id", encryptedContent!);

        Assert.IsFalse(migrationResult, "Already encrypted data should not be migrated");
    }

    [TestMethod]
    public async Task PublicContent_ShouldNeverBeEncrypted()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        var publicContent = "This is public content";

        // Encrypt public content
        var encrypted = await encryptionService.EncryptContentAsync(publicContent, true, _testUserId!);
        Assert.AreEqual(publicContent, encrypted, "Public content should not be encrypted");

        // Decrypt public content
        var decrypted = await encryptionService.DecryptContentAsync(publicContent, true, _testUserId!);
        Assert.AreEqual(publicContent, decrypted, "Public content should not be decrypted");
    }

    [TestMethod]
    public async Task EmptyContent_ShouldBeHandledGracefully()
    {
        using var scope = _serviceProvider!.CreateScope();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();

        // Test null content
        var encryptedNull = await encryptionService.EncryptContentAsync(null, false, _testUserId!);
        Assert.IsNull(encryptedNull);

        var decryptedNull = await encryptionService.DecryptContentAsync(null, false, _testUserId!);
        Assert.IsNull(decryptedNull);

        // Test empty content
        var encryptedEmpty = await encryptionService.EncryptContentAsync("", false, _testUserId!);
        Assert.AreEqual("", encryptedEmpty);

        var decryptedEmpty = await encryptionService.DecryptContentAsync("", false, _testUserId!);
        Assert.AreEqual("", decryptedEmpty);
    }

    #endregion

}
