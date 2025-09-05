using App.Core;
using App.Domain;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace App.Services;

/// <summary>
/// High-level service for orchestrating the complete document processing pipeline.
/// Coordinates hyperlink repair, formatting, and style operations with parallel processing.
/// </summary>
public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IHyperlinkIndexService _hyperlinkIndexService;
    private readonly ILookupService _lookupService;
    private readonly IHyperlinkRepairService _hyperlinkRepairService;
    private readonly IFormattingService _formattingService;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        IHyperlinkIndexService hyperlinkIndexService,
        ILookupService lookupService,
        IHyperlinkRepairService hyperlinkRepairService,
        IFormattingService formattingService,
        ILogger<DocumentProcessingService> logger)
    {
        _hyperlinkIndexService = hyperlinkIndexService ?? throw new ArgumentNullException(nameof(hyperlinkIndexService));
        _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
        _hyperlinkRepairService = hyperlinkRepairService ?? throw new ArgumentNullException(nameof(hyperlinkRepairService));
        _formattingService = formattingService ?? throw new ArgumentNullException(nameof(formattingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes a collection of Word documents with the specified options.
    /// Uses parallel processing with bounded concurrency for optimal performance.
    /// </summary>
    public async Task<ProcessSummary> ProcessAsync(IEnumerable<string> paths, ProcessOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var filePaths = paths.ToList();
        
        _logger.LogInformation("Starting batch processing of {FileCount} files", filePaths.Count);

        // Thread-safe collections for aggregating results
        var results = new ConcurrentBag<FileProcessingResult>();
        var warnings = new ConcurrentBag<string>();
        var errors = new ConcurrentBag<string>();

        // Bounded parallelism as specified in CLAUDE.md section 7
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
            CancellationToken = cancellationToken
        };

        try
        {
            // Process files in parallel
            await Parallel.ForEachAsync(filePaths, parallelOptions, async (filePath, ct) =>
            {
                try
                {
                    var result = await ProcessSingleFileAsync(filePath, options, ct);
                    results.Add(result);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Processing cancelled for file: {FilePath}", filePath);
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    var warning = $"Access denied to file: {filePath} - {ex.Message}";
                    warnings.Add(warning);
                    _logger.LogWarning("Access denied to file: {FilePath} - {Message}", filePath, ex.Message);
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    var warning = $"File is locked or in use: {filePath} - {ex.Message}";
                    warnings.Add(warning);
                    _logger.LogWarning("File is locked: {FilePath} - {Message}", filePath, ex.Message);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to process file: {filePath} - {ex.Message}";
                    errors.Add(error);
                    _logger.LogError(ex, "Failed to process file: {FilePath}", filePath);
                }
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Batch processing was cancelled");
            throw;
        }

        // Aggregate results
        var endTime = DateTime.UtcNow;
        var successfulResults = results.ToList();
        
        var summary = new ProcessSummary
        {
            FilesProcessed = successfulResults.Count,
            FilesChanged = successfulResults.Count(r => r.WasModified),
            HyperlinksInspected = successfulResults.Sum(r => r.HyperlinksInspected),
            HyperlinksEligible = successfulResults.Sum(r => r.HyperlinksEligible),
            HyperlinksRepaired = successfulResults.Sum(r => r.HyperlinksRepaired),
            StyleChanges = successfulResults.Sum(r => r.StyleChanges),
            WhitespaceChanges = successfulResults.Sum(r => r.WhitespaceChanges),
            ImagesCentered = successfulResults.Sum(r => r.ImagesCentered),
            TopOfDocumentLinksFixed = successfulResults.Sum(r => r.TopOfDocumentLinksFixed),
            Warnings = warnings.ToList(),
            Errors = errors.ToList(),
            StartTime = startTime,
            EndTime = endTime
        };

        _logger.LogInformation("Batch processing completed. Files processed: {FilesProcessed}, Files changed: {FilesChanged}, " +
                              "Hyperlinks repaired: {HyperlinksRepaired}, Duration: {Duration}",
                              summary.FilesProcessed, summary.FilesChanged, summary.HyperlinksRepaired, summary.Duration);

        return summary;
    }

    /// <summary>
    /// Processes a single document with the specified options.
    /// </summary>
    public async Task<ProcessSummary> ProcessSingleAsync(string path, ProcessOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await ProcessSingleFileAsync(path, options, cancellationToken);
            var endTime = DateTime.UtcNow;

            return new ProcessSummary
            {
                FilesProcessed = 1,
                FilesChanged = result.WasModified ? 1 : 0,
                HyperlinksInspected = result.HyperlinksInspected,
                HyperlinksEligible = result.HyperlinksEligible,
                HyperlinksRepaired = result.HyperlinksRepaired,
                StyleChanges = result.StyleChanges,
                WhitespaceChanges = result.WhitespaceChanges,
                ImagesCentered = result.ImagesCentered,
                TopOfDocumentLinksFixed = result.TopOfDocumentLinksFixed,
                Warnings = Array.Empty<string>(),
                Errors = Array.Empty<string>(),
                StartTime = startTime,
                EndTime = endTime
            };
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            _logger.LogError(ex, "Failed to process single file: {FilePath}", path);
            
            return new ProcessSummary
            {
                FilesProcessed = 0,
                FilesChanged = 0,
                Errors = new[] { $"Failed to process file: {path} - {ex.Message}" },
                StartTime = startTime,
                EndTime = endTime
            };
        }
    }

    /// <summary>
    /// Processes a single file following the exact pipeline from CLAUDE.md section 21.
    /// Pipeline: [Load DOCX] → [Index hyperlinks] → [Extract Lookup_IDs] → [POST to API]
    /// → [Build id→metadata map] → [Repair eligible hyperlinks]
    /// → [Apply options: spaces | TopOfDoc | styles | center images] → [Save] → [Report]
    /// </summary>
    private async Task<FileProcessingResult> ProcessSingleFileAsync(string filePath, ProcessOptions options, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting processing of file: {FilePath}", filePath);

        var result = new FileProcessingResult();
        var tempFilePath = Path.GetTempFileName();
        var backupFilePath = $"{filePath}.bak";

        try
        {
            // [Load DOCX]
            using (var document = WordprocessingDocument.Open(filePath, false)) // Read-only first to check accessibility
            {
                // [Index hyperlinks] 
                var index = _hyperlinkIndexService.Build(document);
                result.HyperlinksInspected = index.Hyperlinks.Count;
                
                // [Extract Lookup_IDs]
                var lookupIds = index.ExtractLookupIds();
                result.HyperlinksEligible = lookupIds.Count;
                
                _logger.LogDebug("File: {FilePath}, Hyperlinks: {Total}, Eligible: {Eligible}", 
                                filePath, result.HyperlinksInspected, result.HyperlinksEligible);

                // [POST to API] - only if we have lookup IDs
                IReadOnlyDictionary<string, LookupResult> lookupMap = new Dictionary<string, LookupResult>();
                if (lookupIds.Count > 0)
                {
                    var lookupResults = await _lookupService.ResolveAsync(lookupIds, cancellationToken);
                    lookupMap = BuildLookupMap(lookupResults);
                }

                // Copy to temp file for modification
                File.Copy(filePath, tempFilePath, overwrite: true);
            }

            // [Repair eligible hyperlinks] and [Apply options] with writable document
            using (var document = WordprocessingDocument.Open(tempFilePath, true))
            {
                var index = _hyperlinkIndexService.Build(document); // Rebuild index for temp file
                
                // [Build id→metadata map] and [Repair eligible hyperlinks]
                if (index.Hyperlinks.Count > 0)
                {
                    var lookupIds = index.ExtractLookupIds();
                    if (lookupIds.Count > 0)
                    {
                        var lookupResults = await _lookupService.ResolveAsync(lookupIds, cancellationToken);
                        var lookupMap = BuildLookupMap(lookupResults);
                        result.HyperlinksRepaired = _hyperlinkRepairService.Repair(document, index, lookupMap);
                    }
                }

                // [Apply options: spaces | TopOfDoc | styles | center images]
                if (options.CollapseDoubleSpaces)
                {
                    result.WhitespaceChanges = _formattingService.NormalizeSpaces(document);
                }

                if (options.FixTopOfDocLinks)
                {
                    result.TopOfDocumentLinksFixed = _formattingService.FixTopOfDocumentLinks(document);
                }

                if (options.StandardizeStyles)
                {
                    var styleReport = _formattingService.EnsureStyles(document);
                    result.StyleChanges = styleReport.TotalStyleChanges;
                }

                if (options.CenterImages)
                {
                    result.ImagesCentered = _formattingService.CenterImages(document);
                }

                // Check if any changes were made
                result.WasModified = result.HyperlinksRepaired > 0 || 
                                   result.WhitespaceChanges > 0 || 
                                   result.TopOfDocumentLinksFixed > 0 || 
                                   result.StyleChanges > 0 || 
                                   result.ImagesCentered > 0;
            }

            // [Save] - Atomic write: save to temp, then replace original
            if (result.WasModified)
            {
                // Create backup if needed (could be configurable)
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, backupFilePath, overwrite: true);
                }

                // Replace original with modified temp file
                File.Move(tempFilePath, filePath, overwrite: true);
                
                _logger.LogDebug("File modified and saved: {FilePath}", filePath);
            }
            else
            {
                _logger.LogDebug("No changes made to file: {FilePath}", filePath);
            }

        }
        finally
        {
            // Clean up temp file if it still exists
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {TempFilePath}", tempFilePath);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Builds a lookup map that can resolve by both Document_ID and Content_ID
    /// as specified in CLAUDE.md section 8D.
    /// </summary>
    private static IReadOnlyDictionary<string, LookupResult> BuildLookupMap(IReadOnlyList<LookupResult> lookupResults)
    {
        var map = new Dictionary<string, LookupResult>(StringComparer.Ordinal); // Case-sensitive as specified

        foreach (var result in lookupResults)
        {
            // Add by Document_ID if present
            if (!string.IsNullOrEmpty(result.DocumentId))
            {
                map[result.DocumentId] = result;
            }

            // Add by Content_ID if present  
            if (!string.IsNullOrEmpty(result.ContentId))
            {
                map[result.ContentId] = result;
            }
        }

        return map;
    }

    /// <summary>
    /// Internal result class for tracking per-file processing results.
    /// </summary>
    private class FileProcessingResult
    {
        public bool WasModified { get; set; }
        public int HyperlinksInspected { get; set; }
        public int HyperlinksEligible { get; set; }
        public int HyperlinksRepaired { get; set; }
        public int StyleChanges { get; set; }
        public int WhitespaceChanges { get; set; }
        public int ImagesCentered { get; set; }
        public int TopOfDocumentLinksFixed { get; set; }
    }
}