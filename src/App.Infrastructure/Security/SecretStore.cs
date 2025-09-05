using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using App.Core;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Security;

/// <summary>
/// Windows DPAPI-based secure storage for sensitive data like API keys.
/// Uses Data Protection API to encrypt secrets with current user's credentials.
/// </summary>
public class SecretStore : ISecretStore
{
    private readonly ILogger<SecretStore> _logger;
    private readonly string _secretsPath;
    private readonly object _lockObject = new();

    public SecretStore(ILogger<SecretStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var docMedicPath = Path.Combine(appDataPath, "Doc_Medic");
        Directory.CreateDirectory(docMedicPath);
        
        _secretsPath = Path.Combine(docMedicPath, "secrets.dat");
        
        _logger.LogDebug("SecretStore initialized with path: {SecretsPath}", _secretsPath);
    }

    /// <summary>
    /// Stores a secret value encrypted with DPAPI.
    /// </summary>
    public void Set(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace", nameof(name));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        lock (_lockObject)
        {
            try
            {
                var secrets = LoadSecrets();
                var encryptedValue = EncryptString(value);
                secrets[name] = encryptedValue;
                SaveSecrets(secrets);

                _logger.LogDebug("Secret stored: {SecretName}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store secret: {SecretName}", name);
                throw;
            }
        }
    }

    /// <summary>
    /// Retrieves and decrypts a secret value using DPAPI.
    /// </summary>
    public string? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        lock (_lockObject)
        {
            try
            {
                var secrets = LoadSecrets();
                
                if (!secrets.TryGetValue(name, out var encryptedValue))
                {
                    _logger.LogDebug("Secret not found: {SecretName}", name);
                    return null;
                }

                var decryptedValue = DecryptString(encryptedValue);
                _logger.LogDebug("Secret retrieved: {SecretName}", name);
                return decryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret: {SecretName}", name);
                return null;
            }
        }
    }

    /// <summary>
    /// Removes a stored secret.
    /// </summary>
    public void Remove(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        lock (_lockObject)
        {
            try
            {
                var secrets = LoadSecrets();
                
                if (secrets.Remove(name))
                {
                    SaveSecrets(secrets);
                    _logger.LogDebug("Secret removed: {SecretName}", name);
                }
                else
                {
                    _logger.LogDebug("Secret not found to remove: {SecretName}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove secret: {SecretName}", name);
                throw;
            }
        }
    }

    /// <summary>
    /// Checks if a secret exists.
    /// </summary>
    public bool Exists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        lock (_lockObject)
        {
            try
            {
                var secrets = LoadSecrets();
                var exists = secrets.ContainsKey(name);
                _logger.LogDebug("Secret exists check: {SecretName} = {Exists}", name, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if secret exists: {SecretName}", name);
                return false;
            }
        }
    }

    /// <summary>
    /// Clears all stored secrets.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            try
            {
                if (File.Exists(_secretsPath))
                {
                    File.Delete(_secretsPath);
                    _logger.LogInformation("All secrets cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear secrets");
                throw;
            }
        }
    }

    /// <summary>
    /// Loads the encrypted secrets dictionary from disk.
    /// </summary>
    private Dictionary<string, string> LoadSecrets()
    {
        if (!File.Exists(_secretsPath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var encryptedJson = File.ReadAllText(_secretsPath);
            
            if (string.IsNullOrWhiteSpace(encryptedJson))
            {
                return new Dictionary<string, string>();
            }

            // The entire JSON is encrypted as one blob
            var decryptedJson = DecryptString(encryptedJson);
            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedJson);
            
            return secrets ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load secrets file, returning empty dictionary");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Saves the encrypted secrets dictionary to disk.
    /// </summary>
    private void SaveSecrets(Dictionary<string, string> secrets)
    {
        try
        {
            var json = JsonSerializer.Serialize(secrets, new JsonSerializerOptions { WriteIndented = false });
            var encryptedJson = EncryptString(json);
            
            var tempPath = _secretsPath + ".tmp";
            File.WriteAllText(tempPath, encryptedJson);
            
            if (File.Exists(_secretsPath))
            {
                File.Delete(_secretsPath);
            }
            File.Move(tempPath, _secretsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save secrets file");
            throw;
        }
    }

    /// <summary>
    /// Encrypts a string using Windows DPAPI (Data Protection API).
    /// </summary>
    private static string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts a string using Windows DPAPI (Data Protection API).
    /// </summary>
    private static string DecryptString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return string.Empty;
        }

        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}