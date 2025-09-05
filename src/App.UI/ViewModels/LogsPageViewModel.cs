using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Prism.Commands;
using Serilog;

namespace App.UI.ViewModels;

/// <summary>
/// View model for the logs page with live log monitoring.
/// </summary>
public class LogsPageViewModel : ViewModelBase, IDisposable
{
    private readonly ILogger _logger;
    private readonly DispatcherTimer _logWatcher;
    private FileSystemWatcher? _fileWatcher;
    private string _logFilePath = string.Empty;
    private string _selectedLogLevel = "All";
    private string _filterText = string.Empty;
    private bool _autoScroll = true;
    private long _lastReadPosition;

    public LogsPageViewModel(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = "Logs";
        LogEntries = new ObservableCollection<LogEntry>();
        LogLevels = new ObservableCollection<string> { "All", "Debug", "Information", "Warning", "Error" };

        // Commands
        ClearLogsCommand = new DelegateCommand(OnClearLogs);
        ExportLogsCommand = new DelegateCommand(OnExportLogs);
        RefreshLogsCommand = new DelegateCommand(OnRefreshLogs);

        // Initialize log monitoring
        _logWatcher = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _logWatcher.Tick += OnLogWatcherTick;

        InitializeLogMonitoring();
    }

    #region Properties

    /// <summary>
    /// Collection of log entries for display.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries { get; }

    /// <summary>
    /// Available log level filter options.
    /// </summary>
    public ObservableCollection<string> LogLevels { get; }

    /// <summary>
    /// Gets or sets the selected log level filter.
    /// </summary>
    public string SelectedLogLevel
    {
        get => _selectedLogLevel;
        set
        {
            if (SetProperty(ref _selectedLogLevel, value))
            {
                OnRefreshLogs();
            }
        }
    }

    /// <summary>
    /// Gets or sets the text filter for log messages.
    /// </summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                OnRefreshLogs();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to automatically scroll to the latest log entry.
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set => SetProperty(ref _autoScroll, value);
    }

    /// <summary>
    /// Gets the current log file path being monitored.
    /// </summary>
    public string LogFilePath
    {
        get => _logFilePath;
        private set => SetProperty(ref _logFilePath, value);
    }

    #endregion

    #region Commands

    public DelegateCommand ClearLogsCommand { get; }
    public DelegateCommand ExportLogsCommand { get; }
    public DelegateCommand RefreshLogsCommand { get; }

    #endregion

    #region Command Handlers

    private void OnClearLogs()
    {
        LogEntries.Clear();
        _lastReadPosition = 0;
        _logger.Information("Log display cleared");
    }

    private void OnExportLogs()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var exportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"DocMedic_Logs_{timestamp}.txt");

            using var writer = new StreamWriter(exportPath);
            
            foreach (var entry in LogEntries)
            {
                writer.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}");
                if (!string.IsNullOrEmpty(entry.Exception))
                {
                    writer.WriteLine(entry.Exception);
                }
            }

            _logger.Information("Logs exported to {ExportPath}", exportPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to export logs");
        }
    }

    private void OnRefreshLogs()
    {
        if (string.IsNullOrEmpty(LogFilePath) || !File.Exists(LogFilePath))
            return;

        try
        {
            LogEntries.Clear();
            _lastReadPosition = 0;
            ReadLogFile();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to refresh logs");
        }
    }

    #endregion

    #region Helper Methods

    private void InitializeLogMonitoring()
    {
        try
        {
            var logsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Doc_Medic", "logs");

            if (!Directory.Exists(logsPath))
                return;

            // Find the most recent log file
            var logFiles = Directory.GetFiles(logsPath, "app-*.log")
                                  .OrderByDescending(File.GetLastWriteTime)
                                  .FirstOrDefault();

            if (logFiles != null)
            {
                LogFilePath = logFiles;
                SetupFileWatcher(logsPath);
                ReadLogFile();
                _logWatcher.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize log monitoring");
        }
    }

    private void SetupFileWatcher(string logsPath)
    {
        _fileWatcher?.Dispose();

        _fileWatcher = new FileSystemWatcher(logsPath)
        {
            Filter = "app-*.log",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _fileWatcher.Changed += OnLogFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        if (string.Equals(e.FullPath, LogFilePath, StringComparison.OrdinalIgnoreCase))
        {
            // Delay to avoid reading while file is being written
            Task.Delay(100).ContinueWith(_ => ReadLogFile());
        }
    }

    private void OnLogWatcherTick(object? sender, EventArgs e)
    {
        ReadLogFile();
    }

    private void ReadLogFile()
    {
        if (string.IsNullOrEmpty(LogFilePath) || !File.Exists(LogFilePath))
            return;

        try
        {
            using var stream = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(_lastReadPosition, SeekOrigin.Begin);

            using var reader = new StreamReader(stream);
            
            string? line;
            var newEntries = new List<LogEntry>();
            
            while ((line = reader.ReadLine()) != null)
            {
                var entry = ParseLogLine(line);
                if (entry != null && ShouldIncludeEntry(entry))
                {
                    newEntries.Add(entry);
                }
            }

            _lastReadPosition = stream.Position;

            // Add new entries to UI thread
            if (newEntries.Count > 0)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var entry in newEntries)
                    {
                        LogEntries.Add(entry);
                    }

                    // Keep only the last 1000 entries for performance
                    while (LogEntries.Count > 1000)
                    {
                        LogEntries.RemoveAt(0);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Avoid logging errors about reading log files to prevent recursion
            System.Diagnostics.Debug.WriteLine($"Error reading log file: {ex.Message}");
        }
    }

    private LogEntry? ParseLogLine(string line)
    {
        try
        {
            // Parse Serilog format: 2024-01-01 12:00:00.123 +00:00 [INF] Message
            if (line.Length < 35) return null;

            var timestampEnd = line.IndexOf("] ");
            if (timestampEnd == -1) return null;

            var timestampPart = line.Substring(0, timestampEnd + 1);
            var messagePart = line.Substring(timestampEnd + 2);

            // Extract timestamp
            var timeEnd = timestampPart.IndexOf(" [");
            if (timeEnd == -1) return null;

            if (!DateTime.TryParse(timestampPart.Substring(0, timeEnd), out var timestamp))
                return null;

            // Extract level
            var levelStart = timestampPart.IndexOf('[') + 1;
            var levelEnd = timestampPart.IndexOf(']');
            var level = timestampPart.Substring(levelStart, levelEnd - levelStart);

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = messagePart,
                RawLine = line
            };
        }
        catch
        {
            return null;
        }
    }

    private bool ShouldIncludeEntry(LogEntry entry)
    {
        // Filter by log level
        if (SelectedLogLevel != "All" && !string.Equals(entry.Level, SelectedLogLevel, StringComparison.OrdinalIgnoreCase))
            return false;

        // Filter by text
        if (!string.IsNullOrEmpty(FilterText))
        {
            return entry.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                   entry.Level.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _logWatcher?.Stop();
        _fileWatcher?.Dispose();
    }

    #endregion
}

/// <summary>
/// Represents a log entry for display in the logs page.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Exception { get; set; } = string.Empty;
    public string RawLine { get; set; } = string.Empty;

    /// <summary>
    /// Gets the display color for the log level.
    /// </summary>
    public string LevelColor => Level.ToUpperInvariant() switch
    {
        "ERR" or "ERROR" => "#F44336", // Red
        "WRN" or "WARNING" => "#FF9800", // Orange
        "INF" or "INFORMATION" => "#4CAF50", // Green
        "DBG" or "DEBUG" => "#2196F3", // Blue
        _ => "#757575" // Grey
    };
}