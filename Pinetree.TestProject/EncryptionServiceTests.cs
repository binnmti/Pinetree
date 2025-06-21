using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Pinetree.Services;
using System.Security.Cryptography;

namespace Pinetree.TestProject
{
    /// <summary>
    /// Tests for the new simplified EncryptionService
    /// </summary>
    [TestClass]
    public class EncryptionServiceTests
    {
        private IEncryptionService? _encryptionService;
        private string _testKey = "";

        [TestInitialize]
        public void Initialize()
        {
            // Generate a test key (32 bytes for AES-256)
            using (var rng = RandomNumberGenerator.Create())
            {
                var keyBytes = new byte[32];
                rng.GetBytes(keyBytes);
                _testKey = Convert.ToBase64String(keyBytes);
            }

            // Create configuration with test key
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Encryption:Key"] = _testKey
                })
                .Build();

            // Create logger
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<EncryptionService>>();

            // Create encryption service
            _encryptionService = new EncryptionService(configuration);
        }

        [TestMethod]
        public void Encrypt_ValidPlainText_ReturnsEncryptedWithVersionPrefix()
        {
            // Arrange
            var plainText = "Hello, World!";

            // Act
            var encrypted = _encryptionService!.Encrypt(plainText);

            // Assert
            Assert.IsNotNull(encrypted);
            Assert.IsTrue(encrypted.StartsWith("ENC_V1:"));
            Assert.AreNotEqual(plainText, encrypted);
        }

        [TestMethod]
        public void Decrypt_ValidEncryptedText_ReturnsOriginalPlainText()
        {
            // Arrange
            var plainText = "Hello, World!";
            var encrypted = _encryptionService!.Encrypt(plainText);

            // Act
            var decrypted = _encryptionService.Decrypt(encrypted);

            // Assert
            Assert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void EncryptDecrypt_SamePlainTextMultipleTimes_ProducesDifferentEncrypted()
        {
            // Arrange
            var plainText = "Same text";

            // Act
            var encrypted1 = _encryptionService!.Encrypt(plainText);
            var encrypted2 = _encryptionService.Encrypt(plainText);
            var decrypted1 = _encryptionService.Decrypt(encrypted1);
            var decrypted2 = _encryptionService.Decrypt(encrypted2);

            // Assert
            Assert.AreNotEqual(encrypted1, encrypted2, "Each encryption should produce different result");
            Assert.AreEqual(plainText, decrypted1);
            Assert.AreEqual(plainText, decrypted2);
        }

        [TestMethod]
        public void CanDecrypt_ValidEncryptedText_ReturnsTrue()
        {
            // Arrange
            var plainText = "Test message";
            var encrypted = _encryptionService!.Encrypt(plainText);

            // Act
            var canDecrypt = _encryptionService.CanDecrypt(encrypted);

            // Assert
            Assert.IsTrue(canDecrypt);
        }

        [TestMethod]
        public void CanDecrypt_InvalidFormat_ReturnsFalse()
        {
            // Arrange
            var invalidText = "This is not encrypted";

            // Act
            var canDecrypt = _encryptionService!.CanDecrypt(invalidText);

            // Assert
            Assert.IsFalse(canDecrypt);
        }

        [TestMethod]
        public void CanDecrypt_EmptyString_ReturnsFalse()
        {
            // Act
            var canDecrypt = _encryptionService!.CanDecrypt("");

            // Assert
            Assert.IsFalse(canDecrypt);
        }

        [TestMethod]
        public void CanDecrypt_NullString_ReturnsFalse()
        {
            // Act
            var canDecrypt = _encryptionService!.CanDecrypt(null!);

            // Assert
            Assert.IsFalse(canDecrypt);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Encrypt_EmptyString_ThrowsArgumentException()
        {
            // Act
            _encryptionService!.Encrypt("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Encrypt_NullString_ThrowsArgumentException()
        {
            // Act
            _encryptionService!.Encrypt(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Decrypt_InvalidFormat_ThrowsInvalidOperationException()
        {
            // Act
            _encryptionService!.Decrypt("Invalid format");
        }

        [TestMethod]
        public void EncryptDecrypt_LongText_WorksCorrectly()
        {
            // Arrange
            var longText = new string('A', 10000) + "End";

            // Act
            var encrypted = _encryptionService!.Encrypt(longText);
            var decrypted = _encryptionService.Decrypt(encrypted);

            // Assert
            Assert.AreEqual(longText, decrypted);
        }

        [TestMethod]
        public void EncryptDecrypt_SpecialCharacters_WorksCorrectly()
        {
            // Arrange
            var specialText = "Special characters: 日本語 🎉 €£¥ \n\t\r";

            // Act
            var encrypted = _encryptionService!.Encrypt(specialText);
            var decrypted = _encryptionService.Decrypt(encrypted);

            // Assert
            Assert.AreEqual(specialText, decrypted);
        }
    }
}
