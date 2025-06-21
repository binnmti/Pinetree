using System.Security.Cryptography;
using System.Text;

namespace Pinetree.Services;

/// <summary>
/// Encryption service using AES-256-CBC with environment variable configuration
/// Supports versioning for future algorithm changes and uses random IV per encryption
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const string ENCRYPTION_VERSION = "ENC_V1";
    private const string VERSION_SEPARATOR = ":";
    
    private readonly byte[] _key;

    /// <summary>
    /// Constructor - initializes encryption key from environment variables or configuration
    /// </summary>
    /// <param name="configuration">Configuration provider</param>
    /// <param name="logger">Logger instance</param>
    public EncryptionService(IConfiguration configuration)
    {
        // Read encryption key from environment variable first, then configuration
        var keyString = configuration.GetConnectionString("EncryptionKey") ;

        if (string.IsNullOrEmpty(keyString))
        {
            throw new InvalidOperationException(
                "Encryption key not found. Set ENCRYPTION_KEY environment variable or Encryption:Key in configuration.");
        }

        try
        {
            _key = Convert.FromBase64String(keyString);
            
            if (_key.Length != 32) // 256 bits for AES-256
            {
                throw new InvalidOperationException(
                    $"Invalid encryption key length: {_key.Length} bytes. Expected 32 bytes for AES-256.");
            }
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Invalid encryption key format. Key must be a valid Base64 string.", ex);
        }
    }        
    
    /// <summary>
    /// Encrypts plain text using AES-256-CBC with random IV
    /// Each encryption generates a unique result even for identical input
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <returns>Encrypted text with version prefix</returns>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        }

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // Generate random IV for each encryption
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream msEncrypt = new MemoryStream();
            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                using StreamWriter swEncrypt = new(csEncrypt, Encoding.UTF8);
                swEncrypt.Write(plainText);
            }

            var encryptedBytes = msEncrypt.ToArray();
            var encryptedBase64 = Convert.ToBase64String(encryptedBytes);

            // Add version prefix for future compatibility
            return $"{ENCRYPTION_VERSION}{VERSION_SEPARATOR}{encryptedBase64}";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypts encrypted text that was created by this service
    /// Supports version checking for backward compatibility
    /// </summary>
    /// <param name="encryptedText">Encrypted text with version prefix</param>
    /// <returns>Original plain text</returns>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        }

        try
        {
            // Check if the encrypted text has version prefix
            if (!encryptedText.StartsWith(ENCRYPTION_VERSION + VERSION_SEPARATOR))
            {
                throw new InvalidOperationException(
                    $"Unsupported encryption format. Expected format: {ENCRYPTION_VERSION}{VERSION_SEPARATOR}[encrypted_data]");
            }

            // Extract the encrypted data without version prefix
            var encryptedData = encryptedText.Substring(ENCRYPTION_VERSION.Length + VERSION_SEPARATOR.Length);
            var fullCipher = Convert.FromBase64String(encryptedData);

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV from the beginning of the cipher text
            var ivLength = aes.BlockSize / 8; // 16 bytes for AES
            var iv = new byte[ivLength];
            var cipher = new byte[fullCipher.Length - ivLength];

            Array.Copy(fullCipher, 0, iv, 0, ivLength);
            Array.Copy(fullCipher, ivLength, cipher, 0, cipher.Length);

            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream msDecrypt = new(cipher);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt, Encoding.UTF8);
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    /// <summary>
    /// Checks if the given text can be decrypted by this service
    /// </summary>
    /// <param name="encryptedText">Text to check</param>
    /// <returns>True if the text can be decrypted</returns>
    public bool CanDecrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return false;
        }

        return encryptedText.StartsWith(ENCRYPTION_VERSION + VERSION_SEPARATOR);
    }
}
