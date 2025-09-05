namespace App.Domain;

/// <summary>
/// Represents a hyperlink reference found in a Word document with metadata
/// needed for repair operations.
/// </summary>
public record HyperlinkRef
{
    /// <summary>
    /// Internal key for mapping this hyperlink during processing.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// OpenXML relationship ID for external links (r:id attribute).
    /// Null for internal anchors.
    /// </summary>
    public string? RelationshipId { get; init; }

    /// <summary>
    /// Internal anchor name for document bookmarks.
    /// Null for external links.
    /// </summary>
    public string? Anchor { get; init; }

    /// <summary>
    /// Current target URL or anchor reference.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Location and content of the display text to enable safe updates.
    /// </summary>
    public TextRange DisplayTextRange { get; init; } = new();

    /// <summary>
    /// Indicates whether this is an external hyperlink.
    /// </summary>
    public bool IsExternalLink => !string.IsNullOrEmpty(RelationshipId);

    /// <summary>
    /// Indicates whether this is an internal anchor link.
    /// </summary>
    public bool IsInternalAnchor => !string.IsNullOrEmpty(Anchor);

    /// <summary>
    /// Creates a hyperlink reference for an external link.
    /// </summary>
    public static HyperlinkRef ForExternalLink(string id, string relationshipId, string target, TextRange displayTextRange)
    {
        return new HyperlinkRef
        {
            Id = id,
            RelationshipId = relationshipId,
            Target = target,
            DisplayTextRange = displayTextRange
        };
    }

    /// <summary>
    /// Creates a hyperlink reference for an internal anchor.
    /// </summary>
    public static HyperlinkRef ForInternalAnchor(string id, string anchor, TextRange displayTextRange)
    {
        return new HyperlinkRef
        {
            Id = id,
            Anchor = anchor,
            Target = "#" + anchor,
            DisplayTextRange = displayTextRange
        };
    }
}