using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Pinetree.Data;

namespace Pinetree.Services;

/// <summary>
/// Server-side encryption service for protecting user content at rest
/// Uses password-based key derivation (PBKDF2) with User ID and Password Hash for enhanced security
/// Each user gets a unique encryption key derived from their password hash and user ID
/// </summary>
public class EncryptionService(UserManager<ApplicationUser> userManager)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // PBKDF2 parameters
    private const int KeySize = 32; // 256 bits
    private const int SaltSize = 16; // 128 bits
    private const int Iterations = 100000;

    /// <summary>
    /// Derives a user-specific encryption key using PBKDF2 with User ID and Password Hash
    /// Throws exception if user or password hash is not found for security
    /// </summary>
    private async Task<byte[]> DeriveUserKeyAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User not found for encryption: {userId}");
        var passwordHash = user.PasswordHash;
        if (string.IsNullOrEmpty(passwordHash))
        {
            throw new InvalidOperationException($"Password hash not found for user: {userId}");
        }

        // Create a deterministic salt based on user ID
        // This ensures the same user always gets the same key, even across sessions
        var saltMaterial = $"PinetreeEncryption_{user.Id}_v1";
        var salt = SHA256.HashData(Encoding.UTF8.GetBytes(saltMaterial))[..SaltSize];

        // Derive key using PBKDF2 with password hash as input material
        using var pbkdf2 = new Rfc2898DeriveBytes(passwordHash, salt, Iterations, HashAlgorithmName.SHA256);
        var derivedKey = pbkdf2.GetBytes(KeySize);

        return derivedKey;
    }

    /// <summary>
    /// Encrypts content if it's private
    /// Uses user-specific key derived from password hash and user ID
    /// </summary>
    public async Task<string?> EncryptContentAsync(string? content, bool isPublic, string userId)
    {
        // Only encrypt private content
        if (isPublic || string.IsNullOrEmpty(content))
        {
            return content;
        }

        try
        {
            var key = await DeriveUserKeyAsync(userId);
            return Encrypt(content, key);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    /// <summary>
    /// Decrypts content if it's private
    /// Uses user-specific key derived from password hash and user ID
    /// Falls back to returning original content if decryption fails (for legacy data compatibility)
    /// </summary>
    public async Task<string?> DecryptContentAsync(string? encryptedContent, bool isPublic, string userId)
    {
        // Only decrypt private content
        if (isPublic || string.IsNullOrEmpty(encryptedContent))
        {
            return encryptedContent;
        }

        try
        {
            var key = await DeriveUserKeyAsync(userId);
            return Decrypt(encryptedContent, key);
        }
        catch (Exception)
        {
            // If decryption fails, it might be legacy unencrypted data
            // Log the error but return the original content for backward compatibility
            return encryptedContent;
        }
    }

    /// <summary>
    /// Encrypts plaintext using AES-256-CBC with the provided key
    /// </summary>
    private static string Encrypt(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var writer = new StreamWriter(cs);

        writer.Write(plainText);
        writer.Flush();
        cs.FlushFinalBlock();

        var encrypted = ms.ToArray();
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts ciphertext using AES-256-CBC with the provided key
    /// </summary>
    private static string Decrypt(string cipherText, byte[] key)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return reader.ReadToEnd();
    }
}
