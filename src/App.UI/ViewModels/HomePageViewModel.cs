using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Prism.Commands;
using Serilog;
using App.Core;
using App.Domain;
using App.UI.Models;

namespace App.UI.ViewModels;

/// <summary>
/// View model for the main file processing page.
/// </summary>
public class HomePageViewModel : ViewModelBase
{
    private readonly IDocumentProcessingService _processingService;
    private readonly ILogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    private bool _collapseDoubleSpaces = true;
    private bool _fixTopOfDocLinks = true;
    private bool _standardizeStyles = true;
    private bool _centerImages = true;
    private bool _isProcessing;
    private int _overallProgress;
    private string _processingStatus = string.Empty;

    public HomePageViewModel(IDocumentProcessingService processingService, ILogger logger)
    {
        _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Title = "Process Files";
        Files = new ObservableCollection<DocumentFile>();

        // Commands
        SelectFilesCommand = new DelegateCommand(OnSelectFiles);
        ClearFilesCommand = new DelegateCommand(OnClearFiles, CanClearFiles);
        ProcessFilesCommand = new DelegateCommand(OnProcessFiles, CanProcessFiles);
        CancelProcessingCommand = new DelegateCommand(OnCancelProcessing, CanCancelProcessing);
        RemoveSelectedCommand = new DelegateCommand<DocumentFile>(OnRemoveSelected);

        // Wire up property changed notifications
        Files.CollectionChanged += (_, _) =>
        {
            ClearFilesCommand.RaiseCanExecuteChanged();
            ProcessFilesCommand.RaiseCanExecuteChanged();
        };
    }

    #region Properties

    /// <summary>
    /// Collection of files to process.
    /// </summary>
    public ObservableCollection<DocumentFile> Files { get; }

    /// <summary>
    /// Gets or sets whether to collapse multiple spaces.
    /// </summary>
    public bool CollapseDoubleSpaces
    {
        get => _collapseDoubleSpaces;
        set => SetProperty(ref _collapseDoubleSpaces, value);
    }

    /// <summary>
    /// Gets or sets whether to fix "Top of Document" links.
    /// </summary>
    public bool FixTopOfDocLinks
    {
        get => _fixTopOfDocLinks;
        set => SetProperty(ref _fixTopOfDocLinks, value);
    }

    /// <summary>
    /// Gets or sets whether to standardize styles.
    /// </summary>
    public bool StandardizeStyles
    {
        get => _standardizeStyles;
        set => SetProperty(ref _standardizeStyles, value);
    }

    /// <summary>
    /// Gets or sets whether to center images.
    /// </summary>
    public bool CenterImages
    {
        get => _centerImages;
        set => SetProperty(ref _centerImages, value);
    }

    /// <summary>
    /// Gets whether processing is currently running.
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                ProcessFilesCommand.RaiseCanExecuteChanged();
                CancelProcessingCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the overall processing progress (0-100).
    /// </summary>
    public int OverallProgress
    {
        get => _overallProgress;
        private set => SetProperty(ref _overallProgress, value);
    }

    /// <summary>
    /// Gets the current processing status message.
    /// </summary>
    public string ProcessingStatus
    {
        get => _processingStatus;
        private set => SetProperty(ref _processingStatus, value);
    }

    #endregion

    #region Commands

    public DelegateCommand SelectFilesCommand { get; }
    public DelegateCommand ClearFilesCommand { get; }
    public DelegateCommand ProcessFilesCommand { get; }
    public DelegateCommand CancelProcessingCommand { get; }
    public DelegateCommand<DocumentFile> RemoveSelectedCommand { get; }

    #endregion

    #region Command Handlers

    private void OnSelectFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Word Documents to Process",
            Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var fileName in dialog.FileNames)
            {
                // Check if file is already in the list
                if (Files.Any(f => string.Equals(f.FilePath, fileName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                try
                {
                    var docFile = new DocumentFile(fileName);
                    Files.Add(docFile);
                    _logger.Information("Added file to processing queue: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Could not add file to queue: {FileName}", fileName);
                }
            }
        }
    }

    private void OnClearFiles()
    {
        Files.Clear();
        _logger.Information("Cleared file processing queue");
    }

    private bool CanClearFiles() => Files.Count > 0 && !IsProcessing;

    private async void OnProcessFiles()
    {
        if (IsProcessing || Files.Count == 0) return;

        _cancellationTokenSource = new CancellationTokenSource();
        IsProcessing = true;
        OverallProgress = 0;
        ProcessingStatus = "Starting processing...";

        try
        {
            var options = new ProcessOptions
            {
                CollapseDoubleSpaces = CollapseDoubleSpaces,
                FixTopOfDocLinks = FixTopOfDocLinks,
                StandardizeStyles = StandardizeStyles,
                CenterImages = CenterImages
            };

            var filePaths = Files.Select(f => f.FilePath).ToList();
            
            _logger.Information("Starting batch processing of {FileCount} files", filePaths.Count);

            // Update file statuses to processing
            foreach (var file in Files)
            {
                file.Status = Models.ProcessingStatus.Processing;
                file.StatusMessage = "Processing...";
            }

            // Process files with progress tracking
            await ProcessFilesWithProgress(filePaths, options, _cancellationTokenSource.Token);

            ProcessingStatus = "Processing completed";
            _logger.Information("Batch processing completed successfully");
        }
        catch (OperationCanceledException)
        {
            ProcessingStatus = "Processing cancelled";
            _logger.Information("Batch processing was cancelled");

            // Mark unprocessed files as skipped
            foreach (var file in Files.Where(f => f.Status == Models.ProcessingStatus.Processing))
            {
                file.Status = Models.ProcessingStatus.Skipped;
                file.StatusMessage = "Cancelled";
            }
        }
        catch (Exception ex)
        {
            ProcessingStatus = $"Processing failed: {ex.Message}";
            _logger.Error(ex, "Batch processing failed");

            // Mark processing files as error
            foreach (var file in Files.Where(f => f.Status == Models.ProcessingStatus.Processing))
            {
                file.Status = Models.ProcessingStatus.Error;
                file.StatusMessage = "Error occurred";
            }
        }
        finally
        {
            IsProcessing = false;
            OverallProgress = 100;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private bool CanProcessFiles() => Files.Count > 0 && !IsProcessing;

    private void OnCancelProcessing()
    {
        _cancellationTokenSource?.Cancel();
        ProcessingStatus = "Cancelling...";
    }

    private bool CanCancelProcessing() => IsProcessing && _cancellationTokenSource != null;

    private void OnRemoveSelected(DocumentFile? file)
    {
        if (file != null && !IsProcessing)
        {
            Files.Remove(file);
            _logger.Information("Removed file from processing queue: {FileName}", file.FileName);
        }
    }

    #endregion

    #region Helper Methods

    private async Task ProcessFilesWithProgress(
        List<string> filePaths, 
        ProcessOptions options, 
        CancellationToken cancellationToken)
    {
        var processedCount = 0;
        var totalCount = filePaths.Count;

        for (int i = 0; i < filePaths.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var filePath = filePaths[i];
            var documentFile = Files.FirstOrDefault(f => f.FilePath == filePath);

            if (documentFile == null) continue;

            try
            {
                ProcessingStatus = $"Processing {Path.GetFileName(filePath)}...";
                documentFile.Progress = 0;

                // Process single file
                var summary = await _processingService.ProcessSingleAsync(filePath, options, cancellationToken);

                // Update file status based on results
                if (summary.IsSuccessful)
                {
                    documentFile.Status = Models.ProcessingStatus.Completed;
                    documentFile.Changes = summary.HyperlinksRepaired + summary.StyleChanges + 
                                         summary.WhitespaceChanges + summary.ImagesCentered + 
                                         summary.TopOfDocumentLinksFixed;
                    documentFile.StatusMessage = $"{documentFile.Changes} changes made";
                }
                else
                {
                    documentFile.Status = Models.ProcessingStatus.Error;
                    documentFile.StatusMessage = string.Join("; ", summary.Errors);
                }

                documentFile.Progress = 100;
            }
            catch (Exception ex)
            {
                documentFile.Status = Models.ProcessingStatus.Error;
                documentFile.StatusMessage = ex.Message;
                documentFile.Progress = 0;
                _logger.Error(ex, "Failed to process file: {FilePath}", filePath);
            }

            processedCount++;
            OverallProgress = (processedCount * 100) / totalCount;
        }
    }

    /// <summary>
    /// Handles drag-and-drop operations for files.
    /// </summary>
    public void HandleDroppedFiles(string[] filePaths)
    {
        if (IsProcessing) return;

        foreach (var filePath in filePaths.Where(f => Path.GetExtension(f).Equals(".docx", StringComparison.OrdinalIgnoreCase)))
        {
            // Check if file is already in the list
            if (Files.Any(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                var docFile = new DocumentFile(filePath);
                Files.Add(docFile);
                _logger.Information("Added dropped file to processing queue: {FileName}", filePath);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Could not add dropped file to queue: {FileName}", filePath);
            }
        }
    }

    #endregion
}