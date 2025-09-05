using App.Domain;
using DocumentFormat.OpenXml.Packaging;

namespace App.Core;

/// <summary>
/// Service for building an index of hyperlinks found in Word documents.
/// Handles both external links and internal anchors, including field codes.
/// </summary>
public interface IHyperlinkIndexService
{
    /// <summary>
    /// Builds a comprehensive index of all hyperlinks in the document.
    /// </summary>
    /// <param name="document">The Word document to scan for hyperlinks.</param>
    /// <returns>Index containing all found hyperlinks with metadata for repair operations.</returns>
    HyperlinkIndex Build(WordprocessingDocument document);

    /// <summary>
    /// Validates that a hyperlink index is consistent with the current document state.
    /// </summary>
    /// <param name="document">The Word document to validate against.</param>
    /// <param name="index">The hyperlink index to validate.</param>
    /// <returns>True if the index is still valid, false if it needs to be rebuilt.</returns>
    bool ValidateIndex(WordprocessingDocument document, HyperlinkIndex index);
}