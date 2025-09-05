using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using Prism.Commands;
using Serilog;
using App.Core;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace App.UI.ViewModels;

/// <summary>
/// View model for the settings page.
/// </summary>
public class SettingsPageViewModel : ViewModelBase
{
    private readonly ISettingsStore _settingsStore;
    private readonly ILogger _logger;
    private AppSettings _settings = AppSettings.Default;

    // API Settings
    private string _baseUrl = string.Empty;
    private string _apiPath = "/lookup";
    private int _timeoutSeconds = 30;
    private bool _isTestingConnection;
    private string _connectionTestResult = string.Empty;

    // UI Settings  
    private string _selectedTheme = "Light";
    private string _selectedAccent = "Blue";
    private bool _rememberWindowState = true;

    // Processing Settings
    private bool _keepBackup = true;
    private int _maxDegreeOfParallelism = Environment.ProcessorCount - 1;

    // Updater Settings
    private string _updateChannel = "Stable";
    private bool _checkOnStartup = true;

    public SettingsPageViewModel(ISettingsStore settingsStore, ILogger logger)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = "Settings";

        // Initialize collections
        ThemeOptions = new ObservableCollection<string> { "Light", "Dark" };
        AccentOptions = new ObservableCollection<string> 
        { 
            "Red", "Pink", "Purple", "DeepPurple", "Indigo", "Blue", "LightBlue", 
            "Cyan", "Teal", "Green", "LightGreen", "Lime", "Yellow", "Amber", 
            "Orange", "DeepOrange", "Brown", "Grey", "BlueGrey" 
        };
        UpdateChannels = new ObservableCollection<string> { "Stable", "Preview" };

        // Commands
        SaveSettingsCommand = new DelegateCommand(OnSaveSettings);
        ResetSettingsCommand = new DelegateCommand(OnResetSettings);
        TestConnectionCommand = new DelegateCommand(OnTestConnection, CanTestConnection);
        
        LoadSettings();
    }

    #region Properties

    /// <summary>
    /// Available theme options.
    /// </summary>
    public ObservableCollection<string> ThemeOptions { get; }

    /// <summary>
    /// Available accent color options.
    /// </summary>
    public ObservableCollection<string> AccentOptions { get; }

    /// <summary>
    /// Available update channel options.
    /// </summary>
    public ObservableCollection<string> UpdateChannels { get; }

    // API Settings Properties
    public string BaseUrl
    {
        get => _baseUrl;
        set => SetProperty(ref _baseUrl, value, OnApiSettingsChanged);
    }

    public string ApiPath
    {
        get => _apiPath;
        set => SetProperty(ref _apiPath, value, OnApiSettingsChanged);
    }

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => SetProperty(ref _timeoutSeconds, Math.Max(1, Math.Min(300, value)), OnApiSettingsChanged);
    }

    public bool IsTestingConnection
    {
        get => _isTestingConnection;
        set => SetProperty(ref _isTestingConnection, value);
    }

    public string ConnectionTestResult
    {
        get => _connectionTestResult;
        set => SetProperty(ref _connectionTestResult, value);
    }

    // UI Settings Properties
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                ApplyThemeChange(value);
            }
        }
    }

    public string SelectedAccent
    {
        get => _selectedAccent;
        set
        {
            if (SetProperty(ref _selectedAccent, value))
            {
                ApplyAccentChange(value);
            }
        }
    }

    public bool RememberWindowState
    {
        get => _rememberWindowState;
        set => SetProperty(ref _rememberWindowState, value);
    }

    // Processing Settings Properties
    public bool KeepBackup
    {
        get => _keepBackup;
        set => SetProperty(ref _keepBackup, value);
    }

    public int MaxDegreeOfParallelism
    {
        get => _maxDegreeOfParallelism;
        set => SetProperty(ref _maxDegreeOfParallelism, Math.Max(1, Math.Min(Environment.ProcessorCount * 2, value)));
    }

    /// <summary>
    /// Maximum value for the parallelism slider (processor count * 2).
    /// </summary>
    public int MaxSliderValue => Environment.ProcessorCount * 2;

    // Updater Settings Properties
    public string UpdateChannel
    {
        get => _updateChannel;
        set => SetProperty(ref _updateChannel, value);
    }

    public bool CheckOnStartup
    {
        get => _checkOnStartup;
        set => SetProperty(ref _checkOnStartup, value);
    }

    #endregion

    #region Commands

    public DelegateCommand SaveSettingsCommand { get; }
    public DelegateCommand ResetSettingsCommand { get; }
    public DelegateCommand TestConnectionCommand { get; }

    #endregion

    #region Command Handlers

    private void OnSaveSettings()
    {
        try
        {
            _settings = _settings with
            {
                Api = _settings.Api with
                {
                    BaseUrl = BaseUrl.Trim(),
                    Path = ApiPath.Trim(),
                    TimeoutSeconds = TimeoutSeconds
                },
                UI = _settings.UI with
                {
                    Theme = SelectedTheme,
                    Accent = SelectedAccent,
                    RememberWindowState = RememberWindowState
                },
                Processing = _settings.Processing with
                {
                    KeepBackup = KeepBackup,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism
                },
                Updater = _settings.Updater with
                {
                    Channel = UpdateChannel,
                    CheckOnStartup = CheckOnStartup
                }
            };

            _settingsStore.Save(_settings);
            
            ConnectionTestResult = "Settings saved successfully";
            _logger.Information("Settings saved successfully");
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"Failed to save settings: {ex.Message}";
            _logger.Error(ex, "Failed to save settings");
        }
    }

    private void OnResetSettings()
    {
        try
        {
            _settingsStore.Reset();
            LoadSettings();
            
            ConnectionTestResult = "Settings reset to defaults";
            _logger.Information("Settings reset to defaults");
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"Failed to reset settings: {ex.Message}";
            _logger.Error(ex, "Failed to reset settings");
        }
    }

    private async void OnTestConnection()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            ConnectionTestResult = "Please enter a base URL";
            return;
        }

        await ExecuteWithBusyState(async () =>
        {
            IsTestingConnection = true;
            ConnectionTestResult = "Testing connection...";

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                var fullUrl = BaseUrl.TrimEnd('/') + "/" + ApiPath.TrimStart('/');
                
                // Simple HEAD request to test connectivity
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, fullUrl));
                
                if (response.IsSuccessStatusCode)
                {
                    ConnectionTestResult = "✓ Connection successful";
                }
                else
                {
                    ConnectionTestResult = $"✗ Connection failed: {response.StatusCode} {response.ReasonPhrase}";
                }
            }
            catch (HttpRequestException ex)
            {
                ConnectionTestResult = $"✗ Connection failed: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                ConnectionTestResult = "✗ Connection timed out";
            }
            catch (Exception ex)
            {
                ConnectionTestResult = $"✗ Connection failed: {ex.Message}";
            }
            finally
            {
                IsTestingConnection = false;
            }
        });
    }

    private bool CanTestConnection() => !IsTestingConnection && !string.IsNullOrWhiteSpace(BaseUrl);

    #endregion

    #region Helper Methods

    private void LoadSettings()
    {
        try
        {
            _settings = _settingsStore.Load();

            // Load API settings
            BaseUrl = _settings.Api.BaseUrl;
            ApiPath = _settings.Api.Path;
            TimeoutSeconds = _settings.Api.TimeoutSeconds;

            // Load UI settings
            SelectedTheme = _settings.UI.Theme;
            SelectedAccent = _settings.UI.Accent;
            RememberWindowState = _settings.UI.RememberWindowState;

            // Load processing settings
            KeepBackup = _settings.Processing.KeepBackup;
            MaxDegreeOfParallelism = _settings.Processing.MaxDegreeOfParallelism;

            // Load updater settings
            UpdateChannel = _settings.Updater.Channel;
            CheckOnStartup = _settings.Updater.CheckOnStartup;

            ConnectionTestResult = string.Empty;
            
            _logger.Information("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"Failed to load settings: {ex.Message}";
            _logger.Error(ex, "Failed to load settings");
        }
    }

    private void OnApiSettingsChanged()
    {
        TestConnectionCommand.RaiseCanExecuteChanged();
        ConnectionTestResult = string.Empty;
    }

    private void ApplyThemeChange(string theme)
    {
        try
        {
            var paletteHelper = new PaletteHelper();
            var materialTheme = paletteHelper.GetTheme();

            materialTheme.SetBaseTheme(theme == "Dark" ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(materialTheme);

            _logger.Information("Theme changed to {Theme}", theme);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply theme change: {Theme}", theme);
        }
    }

    private void ApplyAccentChange(string accent)
    {
        try
        {
            var paletteHelper = new PaletteHelper();
            var materialTheme = paletteHelper.GetTheme();

            // Get the primary color from MaterialDesignColors
            var swatches = new SwatchesProvider().Swatches;
            var selectedSwatch = swatches.FirstOrDefault(s => 
                string.Equals(s.Name, accent, StringComparison.OrdinalIgnoreCase));

            if (selectedSwatch != null)
            {
                materialTheme.SetPrimaryColor(selectedSwatch.ExemplarHue.Color);
                paletteHelper.SetTheme(materialTheme);
            }

            _logger.Information("Accent color changed to {Accent}", accent);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply accent change: {Accent}", accent);
        }
    }

    #endregion
}