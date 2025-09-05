using System.Reflection;
using System.Windows.Input;
using Prism.Commands;
using Serilog;
using App.Core;

namespace App.UI.ViewModels;

/// <summary>
/// View model for the About page.
/// </summary>
public class AboutPageViewModel : ViewModelBase
{
    private readonly IUpdater _updater;
    private readonly ILogger _logger;
    private bool _isCheckingForUpdates;
    private string _updateStatus = string.Empty;

    public AboutPageViewModel(IUpdater updater, ILogger logger)
    {
        _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = "About";
        CheckForUpdatesCommand = new DelegateCommand(OnCheckForUpdates, CanCheckForUpdates);

        // Get version from assembly
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        Version = version?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets whether the update check is in progress.
    /// </summary>
    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        private set
        {
            if (SetProperty(ref _isCheckingForUpdates, value))
            {
                CheckForUpdatesCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the current update status message.
    /// </summary>
    public string UpdateStatus
    {
        get => _updateStatus;
        private set => SetProperty(ref _updateStatus, value);
    }

    /// <summary>
    /// Command to check for updates.
    /// </summary>
    public DelegateCommand CheckForUpdatesCommand { get; }

    private async void OnCheckForUpdates()
    {
        if (IsCheckingForUpdates) return;

        IsCheckingForUpdates = true;
        UpdateStatus = "Checking for updates...";

        try
        {
            _logger.Information("Checking for application updates");

            var result = await _updater.CheckAsync();

            if (result.UpdateAvailable && result.UpdateInfo != null)
            {
                UpdateStatus = $"Update available: v{result.UpdateInfo.Version}";
                _logger.Information("Update available: v{LatestVersion}", result.UpdateInfo.Version);

                // TODO: Show update dialog with release notes
                // For now, just show a message
                System.Windows.MessageBox.Show(
                    $"An update is available: v{result.UpdateInfo.Version}\n\nRestart the application to apply the update.",
                    "Update Available",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                UpdateStatus = "You are using the latest version";
                _logger.Information("Application is up to date");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update check failed: {ex.Message}";
            _logger.Error(ex, "Failed to check for updates");
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private bool CanCheckForUpdates() => !IsCheckingForUpdates;
}