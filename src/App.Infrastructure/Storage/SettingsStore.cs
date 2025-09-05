using System.Text.Json;
using App.Core;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Storage;

/// <summary>
/// JSON-based persistent storage for application settings.
/// Stores settings in %AppData%/Doc_Medic/settings.json.
/// </summary>
public class SettingsStore : ISettingsStore
{
    private readonly ILogger<SettingsStore> _logger;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsStore(ILogger<SettingsStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var docMedicPath = Path.Combine(appDataPath, "Doc_Medic");
        Directory.CreateDirectory(docMedicPath);
        
        _settingsPath = Path.Combine(docMedicPath, "settings.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _logger.LogDebug("SettingsStore initialized with path: {SettingsPath}", _settingsPath);
    }

    public string SettingsPath => _settingsPath;

    /// <summary>
    /// Loads application settings from JSON file.
    /// Returns default settings if file doesn't exist or cannot be parsed.
    /// </summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("Settings file not found, using defaults: {SettingsPath}", _settingsPath);
                return AppSettings.Default;
            }

            var jsonContent = File.ReadAllText(_settingsPath);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Settings file is empty, using defaults: {SettingsPath}", _settingsPath);
                return AppSettings.Default;
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(jsonContent, _jsonOptions);
            
            if (settings == null)
            {
                _logger.LogWarning("Failed to deserialize settings, using defaults: {SettingsPath}", _settingsPath);
                return AppSettings.Default;
            }

            _logger.LogDebug("Settings loaded successfully from: {SettingsPath}", _settingsPath);
            return settings;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error loading settings from: {SettingsPath}", _settingsPath);
            return AppSettings.Default;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error loading settings from: {SettingsPath}", _settingsPath);
            return AppSettings.Default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading settings from: {SettingsPath}", _settingsPath);
            return AppSettings.Default;
        }
    }

    /// <summary>
    /// Saves application settings to JSON file with atomic write operation.
    /// </summary>
    public void Save(AppSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var tempPath = _settingsPath + ".tmp";
        
        try
        {
            // Serialize to JSON
            var jsonContent = JsonSerializer.Serialize(settings, _jsonOptions);
            
            // Write to temporary file first (atomic operation)
            File.WriteAllText(tempPath, jsonContent);
            
            // Replace original file with temp file
            if (File.Exists(_settingsPath))
            {
                File.Delete(_settingsPath);
            }
            File.Move(tempPath, _settingsPath);
            
            _logger.LogDebug("Settings saved successfully to: {SettingsPath}", _settingsPath);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error saving settings to: {SettingsPath}", _settingsPath);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error saving settings to: {SettingsPath}", _settingsPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving settings to: {SettingsPath}", _settingsPath);
            throw;
        }
        finally
        {
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary settings file: {TempPath}", tempPath);
                }
            }
        }
    }

    /// <summary>
    /// Resets settings to default values by deleting the settings file.
    /// </summary>
    public void Reset()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                File.Delete(_settingsPath);
                _logger.LogInformation("Settings file deleted, reset to defaults: {SettingsPath}", _settingsPath);
            }
            else
            {
                _logger.LogDebug("No settings file to delete: {SettingsPath}", _settingsPath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error resetting settings file: {SettingsPath}", _settingsPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resetting settings file: {SettingsPath}", _settingsPath);
            throw;
        }
    }
}