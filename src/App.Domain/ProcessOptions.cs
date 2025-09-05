namespace App.Domain;

/// <summary>
/// Configuration options for document processing operations.
/// </summary>
public record ProcessOptions
{
    /// <summary>
    /// Whether to collapse multiple consecutive spaces into single spaces.
    /// </summary>
    public bool CollapseDoubleSpaces { get; init; }

    /// <summary>
    /// Whether to fix "Top of Document" links to use internal anchors.
    /// </summary>
    public bool FixTopOfDocLinks { get; init; }

    /// <summary>
    /// Whether to standardize document styles (Normal, Heading 1, Heading 2, Hyperlink).
    /// </summary>
    public bool StandardizeStyles { get; init; }

    /// <summary>
    /// Whether to center all images in the document.
    /// </summary>
    public bool CenterImages { get; init; }

    /// <summary>
    /// Default processing options with all features enabled.
    /// </summary>
    public static ProcessOptions Default => new()
    {
        CollapseDoubleSpaces = true,
        FixTopOfDocLinks = true,
        StandardizeStyles = true,
        CenterImages = true
    };

    /// <summary>
    /// Processing options with no transformations enabled.
    /// </summary>
    public static ProcessOptions None => new()
    {
        CollapseDoubleSpaces = false,
        FixTopOfDocLinks = false,
        StandardizeStyles = false,
        CenterImages = false
    };
}