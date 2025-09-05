namespace App.Core;

/// <summary>
/// Secure storage interface for sensitive data like API keys.
/// Uses DPAPI for encryption on Windows.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Stores a secret value encrypted with the current user's credentials.
    /// </summary>
    /// <param name="name">The name/key for the secret.</param>
    /// <param name="value">The secret value to encrypt and store.</param>
    void Set(string name, string value);

    /// <summary>
    /// Retrieves and decrypts a secret value.
    /// </summary>
    /// <param name="name">The name/key for the secret.</param>
    /// <returns>The decrypted secret value, or null if not found.</returns>
    string? Get(string name);

    /// <summary>
    /// Removes a stored secret.
    /// </summary>
    /// <param name="name">The name/key for the secret to remove.</param>
    void Remove(string name);

    /// <summary>
    /// Checks if a secret exists.
    /// </summary>
    /// <param name="name">The name/key for the secret.</param>
    /// <returns>True if the secret exists, false otherwise.</returns>
    bool Exists(string name);

    /// <summary>
    /// Clears all stored secrets.
    /// </summary>
    void Clear();
}