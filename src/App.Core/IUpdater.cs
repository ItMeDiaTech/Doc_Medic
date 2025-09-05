namespace App.Core;

/// <summary>
/// Interface for handling application updates via GitHub Releases and Velopack.
/// </summary>
public interface IUpdater
{
    /// <summary>
    /// Checks for available updates from GitHub Releases.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result of the update check operation.</returns>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and applies an available update.
    /// </summary>
    /// <param name="updateInfo">Information about the update to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the update was successfully applied, false otherwise.</returns>
    Task<bool> ApplyAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    Version CurrentVersion { get; }

    /// <summary>
    /// Gets whether the updater is properly configured.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Result of an update check operation.
/// </summary>
public record UpdateCheckResult
{
    /// <summary>
    /// Whether an update is available.
    /// </summary>
    public bool UpdateAvailable { get; init; }

    /// <summary>
    /// Information about the available update, if any.
    /// </summary>
    public UpdateInfo? UpdateInfo { get; init; }

    /// <summary>
    /// Error message if the check failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether the check was successful.
    /// </summary>
    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Creates a successful result with no update available.
    /// </summary>
    public static UpdateCheckResult NoUpdate => new() { UpdateAvailable = false };

    /// <summary>
    /// Creates a successful result with an available update.
    /// </summary>
    public static UpdateCheckResult WithUpdate(UpdateInfo updateInfo) => new()
    {
        UpdateAvailable = true,
        UpdateInfo = updateInfo
    };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static UpdateCheckResult WithError(string errorMessage) => new()
    {
        UpdateAvailable = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Information about an available update.
/// </summary>
public record UpdateInfo
{
    /// <summary>
    /// Version number of the update.
    /// </summary>
    public Version Version { get; init; } = new(1, 0, 0);

    /// <summary>
    /// Release notes for the update.
    /// </summary>
    public string ReleaseNotes { get; init; } = string.Empty;

    /// <summary>
    /// Download URL for the update package.
    /// </summary>
    public string DownloadUrl { get; init; } = string.Empty;

    /// <summary>
    /// Size of the update package in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// SHA-256 hash of the update package for verification.
    /// </summary>
    public string Hash { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is a pre-release version.
    /// </summary>
    public bool IsPrerelease { get; init; }

    /// <summary>
    /// Release date and time.
    /// </summary>
    public DateTime ReleaseDate { get; init; }
}