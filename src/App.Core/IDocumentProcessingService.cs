using App.Domain;

namespace App.Core;

/// <summary>
/// High-level service for orchestrating the complete document processing pipeline.
/// Coordinates hyperlink repair, formatting, and style operations.
/// </summary>
public interface IDocumentProcessingService
{
    /// <summary>
    /// Processes a collection of Word documents with the specified options.
    /// </summary>
    /// <param name="paths">Paths to the .docx files to process.</param>
    /// <param name="options">Processing options controlling which transformations to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Summary of processing results including counts and warnings.</returns>
    Task<ProcessSummary> ProcessAsync(IEnumerable<string> paths, ProcessOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a single document with the specified options.
    /// </summary>
    /// <param name="path">Path to the .docx file to process.</param>
    /// <param name="options">Processing options controlling which transformations to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Summary of processing results for the single file.</returns>
    Task<ProcessSummary> ProcessSingleAsync(string path, ProcessOptions options, CancellationToken cancellationToken = default);
}