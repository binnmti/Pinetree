using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pinetree.Data;

namespace Pinetree.Services;

/// <summary>
/// Server-side encryption service for protecting user content at rest
/// Uses Data Protection API for secure server-side key management combined with AES-256-GCM for content encryption
/// Provides robust security against database compromise while supporting all user types (password and SNS users)
/// Each user gets a unique encryption key that persists across password changes and is independent of authentication method
/// </summary>
public class EncryptionService
{
    private readonly IDataProtector _keyProtector;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    // Encryption parameters for AES-256-GCM
    private const int KeySize = 32; // 256 bits
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16; // 128 bits for GCM
    
    /// <summary>
    /// Constructor - initializes the Data Protector for user key encryption
    /// </summary>
    public EncryptionService(IDataProtectionProvider dataProtectionProvider, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _keyProtector = dataProtectionProvider.CreateProtector("Pinetree.UserEncryptionKeys.v2");
        _userManager = userManager;
        _context = context;
    }    
    /// <summary>
    /// Gets or creates a persistent encryption key for a user
    /// This key is secured by Data Protection API and persists across password changes
    /// Works for all user types (password and SNS users)
    /// </summary>
    public async Task<byte[]> GetUserEncryptionKeyAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User not found for encryption: {userId}");

        // Check if user already has an encryption key stored
        var existingKeyData = await _context.UserClaims
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ClaimType == "EncryptionKeyData_V2");
        
        if (existingKeyData != null && !string.IsNullOrEmpty(existingKeyData.ClaimValue))
        {            
            try
            {
                // Decrypt the stored key using Data Protection API
                var protectedKey = existingKeyData.ClaimValue;
                var keyBase64 = _keyProtector.Unprotect(protectedKey);
                var keyBytes = Convert.FromBase64String(keyBase64);
                return keyBytes;
            }
            catch (Exception ex)
            {
                // If decryption fails (e.g., key rotation, corruption),
                // generate a new key and lose old data (acceptable trade-off for security)
                Console.WriteLine($"Warning: Failed to decrypt stored encryption key for user {userId}. Error: {ex.Message}");
                return await GenerateAndStoreNewKeyAsync(user);
            }
        }

        // Generate new master key for the user
        return await GenerateAndStoreNewKeyAsync(user);
    }
    
    /// <summary>
    /// Encrypts content if it's private and not already encrypted
    /// Uses encryption marker to identify encrypted content
    /// </summary>
    public async Task<string?> EncryptContentAsync(string? content, bool isPublic, string userId)
    {
        // Only encrypt private content
        if (isPublic || string.IsNullOrEmpty(content))
        {
            return content;
        }

        // Skip encryption if already encrypted
        if (IsEncryptedContent(content))
        {
            return content;
        }        try
        {
            var key = await GetUserEncryptionKeyAsync(userId);
            var encryptedData = Encrypt(content, key);
            return AddEncryptionMarker(encryptedData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }
    
    /// <summary>
    /// Decrypts content if it's private and encrypted
    /// Handles legacy plain text data gracefully by returning it as-is
    /// </summary>
    public async Task<string?> DecryptContentAsync(string? encryptedContent, bool isPublic, string userId)
    {
        // Only decrypt private content
        if (isPublic || string.IsNullOrEmpty(encryptedContent))
        {
            return encryptedContent;
        }

        // Check if content is encrypted (has our encryption marker)
        if (!IsEncryptedContent(encryptedContent))
        {
            // This is legacy plain text data - return as is
            Console.WriteLine($"Found legacy plain text data for user {userId}");
            return encryptedContent;
        }        
        try
        {
            var key = await GetUserEncryptionKeyAsync(userId);
            var actualEncryptedContent = RemoveEncryptionMarker(encryptedContent);
            return Decrypt(actualEncryptedContent, key);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decryption failed for user {userId}", ex);
        }
    }

    /// <summary>
    /// Checks if content is encrypted by looking for encryption marker
    /// </summary>
    private static bool IsEncryptedContent(string content)
    {
        return content.StartsWith("ENC_V1:");
    }

    /// <summary>
    /// Adds encryption marker to encrypted content
    /// </summary>
    private static string AddEncryptionMarker(string encryptedContent)
    {
        return "ENC_V1:" + encryptedContent;
    }

    /// <summary>
    /// Removes encryption marker from encrypted content
    /// </summary>
    private static string RemoveEncryptionMarker(string markedContent)
    {
        return markedContent.StartsWith("ENC_V1:") 
            ? markedContent.Substring(7) 
            : markedContent;
    }

    /// <summary>
    /// Migrates legacy plain text data to encrypted format on-demand
    /// </summary>
    public async Task<bool> TryMigrateLegacyDataAsync(string userId, string itemId, string plainTextContent)
    {
        if (IsEncryptedContent(plainTextContent))
        {
            return false; // Already encrypted
        }

        try
        {
            var encryptedContent = await EncryptContentAsync(plainTextContent, false, userId);
            
            Console.WriteLine($"Migrated legacy data for item {itemId}, user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to migrate legacy data for item {itemId}, user {userId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Encrypts plaintext using AES-256-GCM (AEAD) with the provided key
    /// Provides both confidentiality and authenticity/integrity
    /// </summary>
    private static string Encrypt(string plainText, byte[] key)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        
        // Generate random nonce (12 bytes is standard for GCM)
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        
        // Prepare buffers
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize]; // 128-bit authentication tag        // Encrypt with AES-GCM
        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        
        // Combine nonce + ciphertext + tag for storage
        var result = new byte[nonce.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + cipherBytes.Length, tag.Length);
        
        return Convert.ToBase64String(result);
    }
    
    /// <summary>
    /// Decrypts ciphertext using AES-256-GCM (AEAD) with the provided key
    /// Automatically verifies authenticity and throws exception if tampered
    /// </summary>
    private static string Decrypt(string cipherText, byte[] key)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        
        // Check minimum length (nonce + tag = 28 bytes minimum)
        var minimumLength = NonceSize + TagSize;
        if (fullCipher.Length < minimumLength)
        {
            // Might be legacy CBC data, try legacy decryption
            return DecryptLegacyCBC(cipherText, key);
        }
        
        // Extract components
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[fullCipher.Length - nonce.Length - tag.Length];
        
        Buffer.BlockCopy(fullCipher, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(fullCipher, nonce.Length, cipherBytes, 0, cipherBytes.Length);
        Buffer.BlockCopy(fullCipher, nonce.Length + cipherBytes.Length, tag, 0, tag.Length);
          // Decrypt and verify with AES-GCM
        var plainBytes = new byte[cipherBytes.Length];
        using var aesGcm = new AesGcm(key, TagSize);
        
        try
        {
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (AuthenticationTagMismatchException)
        {
            throw new InvalidOperationException("Ciphertext has been tampered with or corrupted");
        }
    }
    
    /// <summary>
    /// Legacy CBC decryption for backward compatibility
    /// Should only be used for existing data migration
    /// </summary>
    private static string DecryptLegacyCBC(string cipherText, byte[] key)
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
    
    /// <summary>
    /// Generates a new master key and stores it protected with Data Protection API
    /// This method works for all user types (password and SNS users)
    /// </summary>
    private async Task<byte[]> GenerateAndStoreNewKeyAsync(ApplicationUser user)
    {
        // Generate random master key
        var masterKey = new byte[KeySize];
        RandomNumberGenerator.Fill(masterKey);

        // Protect with Data Protection API
        var protectedKey = _keyProtector.Protect(Convert.ToBase64String(masterKey));

        // Store protected key as user claim (using V2 to distinguish from old password-based keys)
        var existingClaim = await _context.UserClaims
            .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ClaimType == "EncryptionKeyData_V2");

        if (existingClaim != null)
        {
            existingClaim.ClaimValue = protectedKey;
        }
        else
        {
            _context.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = user.Id,
                ClaimType = "EncryptionKeyData_V2",
                ClaimValue = protectedKey
            });
        }

        await _context.SaveChangesAsync();
        return masterKey;
    }}
