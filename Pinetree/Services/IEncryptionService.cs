using System;

namespace Pinetree.Services
{
    /// <summary>
    /// Interface for encryption and decryption services
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts plain text using AES-256 with random IV per encryption
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Encrypted text with version prefix (e.g., "ENC_V1:base64data")</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts encrypted text that was created by this service
        /// </summary>
        /// <param name="encryptedText">Encrypted text with version prefix</param>
        /// <returns>Original plain text</returns>
        string Decrypt(string encryptedText);

        /// <summary>
        /// Checks if the given text can be decrypted by this service
        /// </summary>
        /// <param name="encryptedText">Text to check</param>
        /// <returns>True if the text can be decrypted</returns>
        bool CanDecrypt(string encryptedText);
    }
}
