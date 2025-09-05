using App.Domain;

namespace App.Core;

/// <summary>
/// Persistent storage interface for application settings.
/// Handles loading and saving configuration to %AppData%.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Loads application settings from persistent storage.
    /// </summary>
    /// <returns>The loaded settings, or default settings if none exist.</returns>
    AppSettings Load();

    /// <summary>
    /// Saves application settings to persistent storage.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    void Save(AppSettings settings);

    /// <summary>
    /// Resets settings to their default values.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets the path where settings are stored.
    /// </summary>
    string SettingsPath { get; }
}

/// <summary>
/// Application settings model containing all configurable options.
/// </summary>
public record AppSettings
{
    /// <summary>
    /// API-related settings for Power Automate integration.
    /// </summary>
    public ApiSettings Api { get; init; } = new();

    /// <summary>
    /// Updater configuration settings.
    /// </summary>
    public UpdaterSettings Updater { get; init; } = new();

    /// <summary>
    /// UI theme and appearance settings.
    /// </summary>
    public UiSettings UI { get; init; } = new();

    /// <summary>
    /// Document processing behavior settings.
    /// </summary>
    public ProcessingSettings Processing { get; init; } = new();

    /// <summary>
    /// Default application settings.
    /// </summary>
    public static AppSettings Default => new();
}

/// <summary>
/// API configuration settings.
/// </summary>
public record ApiSettings
{
    /// <summary>
    /// Base URL for the Power Automate API.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// API endpoint path for lookup requests.
    /// </summary>
    public string Path { get; init; } = "/lookup";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
}

/// <summary>
/// Updater configuration settings.
/// </summary>
public record UpdaterSettings
{
    /// <summary>
    /// Update channel (Stable or Preview).
    /// </summary>
    public string Channel { get; init; } = "Stable";

    /// <summary>
    /// Whether to check for updates on startup.
    /// </summary>
    public bool CheckOnStartup { get; init; } = true;
}

/// <summary>
/// UI theme and appearance settings.
/// </summary>
public record UiSettings
{
    /// <summary>
    /// Theme name (Light or Dark).
    /// </summary>
    public string Theme { get; init; } = "Light";

    /// <summary>
    /// Accent color name.
    /// </summary>
    public string Accent { get; init; } = "Blue";

    /// <summary>
    /// Whether to remember window size and position.
    /// </summary>
    public bool RememberWindowState { get; init; } = true;
}

/// <summary>
/// Document processing behavior settings.
/// </summary>
public record ProcessingSettings
{
    /// <summary>
    /// Whether to keep backup copies of processed files.
    /// </summary>
    public bool KeepBackup { get; init; } = true;

    /// <summary>
    /// Maximum degree of parallelism for batch processing.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount - 1;

    /// <summary>
    /// Default processing options for new sessions.
    /// </summary>
    public ProcessOptions DefaultOptions { get; init; } = ProcessOptions.Default;
}