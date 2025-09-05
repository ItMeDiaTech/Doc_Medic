using System.Reflection;
using App.Core;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace App.Infrastructure.Updates;

/// <summary>
/// Velopack-based updater implementation for GitHub Releases integration.
/// Handles automated update checking and application from GitHub releases.
/// </summary>
public class VelopackUpdater : IUpdater
{
    private readonly ILogger<VelopackUpdater> _logger;
    private readonly UpdateManager? _updateManager;
    private readonly Version _currentVersion;

    public VelopackUpdater(ILogger<VelopackUpdater> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get current version from assembly
        var assembly = Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        _currentVersion = versionAttribute?.InformationalVersion != null 
            ? Version.Parse(versionAttribute.InformationalVersion.Split('+')[0]) // Remove build metadata
            : assembly.GetName().Version ?? new Version(1, 0, 0);

        try
        {
            // Initialize Velopack UpdateManager with GitHub source
            _logger.LogInformation("Initializing Velopack UpdateManager");

            // Configure GitHub releases as update source
            var gitHubSource = new GithubSource("https://github.com/ItMeDiaTech/Doc_Medic", null, false);
            _updateManager = new UpdateManager(gitHubSource);
            _logger.LogDebug("Velopack UpdateManager initialized with GitHub source");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Velopack UpdateManager - updater will be disabled");
            _updateManager = null;
        }
    }

    public Version CurrentVersion => _currentVersion;

    public bool IsConfigured => _updateManager != null;

    /// <summary>
    /// Checks for available updates from GitHub Releases using Velopack.
    /// </summary>
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (_updateManager == null)
        {
            _logger.LogWarning("UpdateManager not configured, cannot check for updates");
            return UpdateCheckResult.WithError("Update manager not configured");
        }

        try
        {
            _logger.LogInformation("Checking for updates... Current version: {CurrentVersion}", _currentVersion);

            // Check for updates
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            
            if (updateInfo == null)
            {
                _logger.LogInformation("No updates available");
                return UpdateCheckResult.NoUpdate;
            }

            _logger.LogInformation("Update available: {Version}", updateInfo.TargetFullRelease.Version);

            var updateDetails = new App.Core.UpdateInfo
            {
                Version = new Version(updateInfo.TargetFullRelease.Version.Major,
                                     updateInfo.TargetFullRelease.Version.Minor,
                                     updateInfo.TargetFullRelease.Version.Patch),
                ReleaseNotes = ExtractReleaseNotes(updateInfo),
                DownloadUrl = updateInfo.TargetFullRelease.FileName ?? string.Empty,
                Size = updateInfo.TargetFullRelease.Size,
                Hash = updateInfo.TargetFullRelease.SHA1 ?? string.Empty,
                IsPrerelease = updateInfo.TargetFullRelease.Version.IsPrerelease,
                ReleaseDate = DateTime.UtcNow
            };

            return UpdateCheckResult.WithUpdate(updateDetails);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update check was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to check for updates: {ex.Message}";
            _logger.LogError(ex, "Error checking for updates");
            return UpdateCheckResult.WithError(errorMessage);
        }
    }

    /// <summary>
    /// Downloads and applies an available update using Velopack.
    /// </summary>
    public async Task<bool> ApplyAsync(App.Core.UpdateInfo updateInfo, CancellationToken cancellationToken = default)
    {
        if (_updateManager == null)
        {
            _logger.LogError("UpdateManager not configured, cannot apply updates");
            return false;
        }

        if (updateInfo == null)
        {
            throw new ArgumentNullException(nameof(updateInfo));
        }

        try
        {
            _logger.LogInformation("Downloading and applying update: {Version}", updateInfo.Version);

            // Check for updates first to get the UpdateInfo object
            var availableUpdate = await _updateManager.CheckForUpdatesAsync();
            
            if (availableUpdate == null)
            {
                _logger.LogWarning("No update available to apply");
                return false;
            }

            // Download the update
            await _updateManager.DownloadUpdatesAsync(availableUpdate);
            
            _logger.LogInformation("Update downloaded successfully, preparing to restart application");

            // Apply and restart - this will terminate the current application
            _updateManager.ApplyUpdatesAndRestart(availableUpdate);
            
            // This line should not be reached as the application restarts
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Update application was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply update: {Version}", updateInfo.Version);
            return false;
        }
    }

    /// <summary>
    /// Extracts release notes from Velopack update information.
    /// </summary>
    private static string ExtractReleaseNotes(Velopack.UpdateInfo updateInfo)
    {
        // Fallback to basic version information since Velopack doesn't expose release notes directly
        return $"Update to version {updateInfo.TargetFullRelease.Version}";
    }
}

/// <summary>
/// Extension methods for Velopack integration.
/// </summary>
public static class VelopackExtensions
{
    /// <summary>
    /// Configures Velopack app lifecycle hooks.
    /// Should be called early in application startup.
    /// </summary>
    public static void ConfigureVelopack()
    {
        // Configure Velopack app lifecycle
        VelopackApp.Build()
            .WithFirstRun(v => 
            {
                // Handle first run after installation
                Console.WriteLine($"Thanks for installing Doc_Medic v{v}!");
            })
            .Run();
    }
}