namespace App.Domain;

/// <summary>
/// Report of style standardization changes made to a document.
/// </summary>
public record StyleReport
{
    /// <summary>
    /// Number of paragraphs that had their style updated to Normal.
    /// </summary>
    public int NormalStylesApplied { get; init; }

    /// <summary>
    /// Number of paragraphs that had their style updated to Heading 1.
    /// </summary>
    public int Heading1StylesApplied { get; init; }

    /// <summary>
    /// Number of paragraphs that had their style updated to Heading 2.
    /// </summary>
    public int Heading2StylesApplied { get; init; }

    /// <summary>
    /// Number of hyperlink character styles that were updated.
    /// </summary>
    public int HyperlinkStylesApplied { get; init; }

    /// <summary>
    /// Whether the Normal paragraph style was created or updated.
    /// </summary>
    public bool NormalStyleCreated { get; init; }

    /// <summary>
    /// Whether the Heading 1 paragraph style was created or updated.
    /// </summary>
    public bool Heading1StyleCreated { get; init; }

    /// <summary>
    /// Whether the Heading 2 paragraph style was created or updated.
    /// </summary>
    public bool Heading2StyleCreated { get; init; }

    /// <summary>
    /// Whether the Hyperlink character style was created or updated.
    /// </summary>
    public bool HyperlinkStyleCreated { get; init; }

    /// <summary>
    /// Whether the document's StylesPart was created (didn't exist before).
    /// </summary>
    public bool StylesPartCreated { get; init; }

    /// <summary>
    /// Total number of style applications made.
    /// </summary>
    public int TotalStyleChanges =>
        NormalStylesApplied + Heading1StylesApplied + Heading2StylesApplied + HyperlinkStylesApplied;

    /// <summary>
    /// List of warnings encountered during style processing.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates an empty style report.
    /// </summary>
    public static StyleReport Empty => new();

    /// <summary>
    /// Creates a style report with warnings.
    /// </summary>
    public static StyleReport WithWarnings(params string[] warnings)
    {
        return new StyleReport { Warnings = warnings };
    }
}