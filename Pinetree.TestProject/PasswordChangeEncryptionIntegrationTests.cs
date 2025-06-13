using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pinetree.Data;
using Pinetree.Services;

namespace Pinetree.TestProject;

[TestClass]
public class PasswordChangeEncryptionIntegrationTests
{
    private static ServiceProvider? _serviceProvider;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        var services = new ServiceCollection();        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("PasswordChangeIntegrationTestDb_Fixed"));
        
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddDataProtection();
        services.AddScoped<EncryptionService>();
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    public async Task FullWorkflow_PasswordChange_MaintainsDataAccess()
    {        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        
        // Create user
        var user = new ApplicationUser
        {
            UserName = "workflow@example.com",
            Email = "workflow@example.com"
        };
        var createResult = await userManager.CreateAsync(user, "OriginalPassword123!");
        Assert.IsTrue(createResult.Succeeded);
        
        var secretData = "This is very secret information!";
        
        // Step 1: Encrypt some data
        var encryptedData = await encryptionService.EncryptContentAsync(secretData, false, user.Id);
        Assert.IsNotNull(encryptedData);
        Assert.AreNotEqual(secretData, encryptedData);
        
        // Step 2: Verify we can decrypt it
        var decryptedData1 = await encryptionService.DecryptContentAsync(encryptedData, false, user.Id);
        Assert.AreEqual(secretData, decryptedData1);
          // Step 3: Change password (with Data Protection API, no re-encryption needed)
        var changeResult = await userManager.ChangePasswordAsync(user, "OriginalPassword123!", "NewPassword456!");
        Assert.IsTrue(changeResult.Succeeded);
        
        // Step 4: Verify we can still decrypt the data after password change
        // With Data Protection API, password changes don't affect encryption keys
        var decryptedData2 = await encryptionService.DecryptContentAsync(encryptedData, false, user.Id);
        Assert.AreEqual(secretData, decryptedData2, "Data should still be accessible after password change");
        
        // Step 5: Verify we can encrypt new data with the same key
        var newSecretData = "More secret data after password change";
        var newEncryptedData = await encryptionService.EncryptContentAsync(newSecretData, false, user.Id);
        var newDecryptedData = await encryptionService.DecryptContentAsync(newEncryptedData, false, user.Id);
        Assert.AreEqual(newSecretData, newDecryptedData);
    }    [TestMethod]
    public async Task PasswordReset_WithDataProtectionAPI_PreservesData()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        
        var user = new ApplicationUser
        {
            UserName = "resettest@example.com",
            Email = "resettest@example.com"
        };
        var createResult = await userManager.CreateAsync(user, "OriginalPassword123!");
        Assert.IsTrue(createResult.Succeeded);
        
        var secretData = "Secret data before reset";
        
        // Encrypt data with original password
        var encryptedData = await encryptionService.EncryptContentAsync(secretData, false, user.Id);
        var decryptedData1 = await encryptionService.DecryptContentAsync(encryptedData, false, user.Id);
        Assert.AreEqual(secretData, decryptedData1);
        
        // Simulate password reset (which generates a new key)
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, "ResetPassword789!");
        Assert.IsTrue(resetResult.Succeeded);        // After reset with Data Protection API, old data should still be accessible
        // because keys are not tied to passwords
        var decryptedData2 = await encryptionService.DecryptContentAsync(encryptedData, false, user.Id);
        Assert.AreEqual(secretData, decryptedData2, "Old encrypted data should remain accessible after password reset with Data Protection API");
        
        // But new data should work fine
        var newSecretData = "New secret data after reset";
        var newEncryptedData = await encryptionService.EncryptContentAsync(newSecretData, false, user.Id);
        var newDecryptedData = await encryptionService.DecryptContentAsync(newEncryptedData, false, user.Id);
        Assert.AreEqual(newSecretData, newDecryptedData);
    }

    [TestMethod]
    public async Task PublicContent_UnaffectedByPasswordChanges()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        
        var user = new ApplicationUser
        {
            UserName = "publictest@example.com",
            Email = "publictest@example.com"
        };
        var createResult = await userManager.CreateAsync(user, "Password123!");
        Assert.IsTrue(createResult.Succeeded);
        
        var publicData = "This is public information";
        
        // Public content should not be encrypted
        var result1 = await encryptionService.EncryptContentAsync(publicData, true, user.Id);
        Assert.AreEqual(publicData, result1, "Public content should not be encrypted");
        
        // Change password
        var changeResult = await userManager.ChangePasswordAsync(user, "Password123!", "NewPassword456!");
        Assert.IsTrue(changeResult.Succeeded);
        
        // Public content should still be accessible unchanged
        var result2 = await encryptionService.DecryptContentAsync(publicData, true, user.Id);
        Assert.AreEqual(publicData, result2, "Public content should remain unchanged after password change");
    }
}
