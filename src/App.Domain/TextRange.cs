namespace App.Domain;

/// <summary>
/// Captures paragraph and run indices to safely update hyperlink display text
/// without losing formatting when text spans multiple runs.
/// </summary>
public record TextRange
{
    /// <summary>
    /// Index of the paragraph containing the text.
    /// </summary>
    public int ParagraphIndex { get; init; }

    /// <summary>
    /// Index of the starting run within the paragraph.
    /// </summary>
    public int StartRunIndex { get; init; }

    /// <summary>
    /// Index of the ending run within the paragraph (inclusive).
    /// </summary>
    public int EndRunIndex { get; init; }

    /// <summary>
    /// Character offset within the starting run.
    /// </summary>
    public int StartCharOffset { get; init; }

    /// <summary>
    /// Character offset within the ending run (exclusive).
    /// </summary>
    public int EndCharOffset { get; init; }

    /// <summary>
    /// The complete text content across all runs in this range.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Creates a TextRange for a single run.
    /// </summary>
    public static TextRange ForSingleRun(int paragraphIndex, int runIndex, string text, int startOffset = 0)
    {
        return new TextRange
        {
            ParagraphIndex = paragraphIndex,
            StartRunIndex = runIndex,
            EndRunIndex = runIndex,
            StartCharOffset = startOffset,
            EndCharOffset = text.Length,
            Text = text
        };
    }

    /// <summary>
    /// Creates a TextRange spanning multiple runs.
    /// </summary>
    public static TextRange ForMultipleRuns(int paragraphIndex, int startRunIndex, int endRunIndex, 
        string text, int startCharOffset = 0, int endCharOffset = 0)
    {
        return new TextRange
        {
            ParagraphIndex = paragraphIndex,
            StartRunIndex = startRunIndex,
            EndRunIndex = endRunIndex,
            StartCharOffset = startCharOffset,
            EndCharOffset = endCharOffset,
            Text = text
        };
    }
}