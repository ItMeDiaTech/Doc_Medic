namespace App.Domain;

/// <summary>
/// Summary of processing results for a batch of documents.
/// </summary>
public record ProcessSummary
{
    /// <summary>
    /// Total number of files processed.
    /// </summary>
    public int FilesProcessed { get; init; }

    /// <summary>
    /// Number of files that were successfully modified.
    /// </summary>
    public int FilesChanged { get; init; }

    /// <summary>
    /// Total number of hyperlinks inspected across all files.
    /// </summary>
    public int HyperlinksInspected { get; init; }

    /// <summary>
    /// Number of hyperlinks that were eligible for repair.
    /// </summary>
    public int HyperlinksEligible { get; init; }

    /// <summary>
    /// Number of hyperlinks that were actually repaired.
    /// </summary>
    public int HyperlinksRepaired { get; init; }

    /// <summary>
    /// Number of style changes made across all files.
    /// </summary>
    public int StyleChanges { get; init; }

    /// <summary>
    /// Number of whitespace normalizations performed.
    /// </summary>
    public int WhitespaceChanges { get; init; }

    /// <summary>
    /// Number of images that were centered.
    /// </summary>
    public int ImagesCentered { get; init; }

    /// <summary>
    /// Number of "Top of Document" links that were fixed.
    /// </summary>
    public int TopOfDocumentLinksFixed { get; init; }

    /// <summary>
    /// List of warnings encountered during processing.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// List of errors encountered during processing.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Processing start time.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Processing end time.
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Total processing duration.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Indicates whether the processing completed successfully without errors.
    /// </summary>
    public bool IsSuccessful => Errors.Count == 0;

    /// <summary>
    /// Creates an empty summary for tracking during processing.
    /// </summary>
    public static ProcessSummary Empty => new()
    {
        StartTime = DateTime.UtcNow
    };

    /// <summary>
    /// Creates a final summary with the end time set.
    /// </summary>
    public ProcessSummary WithEndTime(DateTime endTime)
    {
        return this with { EndTime = endTime };
    }
}