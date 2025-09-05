using App.Domain;
using DocumentFormat.OpenXml.Packaging;

namespace App.Core;

/// <summary>
/// Service for applying document formatting operations including spacing,
/// style standardization, image centering, and "Top of Document" link fixes.
/// </summary>
public interface IFormattingService
{
    /// <summary>
    /// Normalizes whitespace by collapsing multiple consecutive spaces into single spaces.
    /// Preserves non-breaking spaces and proper punctuation spacing.
    /// </summary>
    /// <param name="document">The Word document to normalize.</param>
    /// <returns>Number of spacing normalizations performed.</returns>
    int NormalizeSpaces(WordprocessingDocument document);

    /// <summary>
    /// Fixes "Top of Document" links to use internal anchors and applies proper styling.
    /// Ensures a bookmark exists at the document start and updates links to reference it.
    /// </summary>
    /// <param name="document">The Word document to fix links in.</param>
    /// <returns>Number of "Top of Document" links that were fixed.</returns>
    int FixTopOfDocumentLinks(WordprocessingDocument document);

    /// <summary>
    /// Ensures standard styles exist in the document and applies them where appropriate.
    /// Creates or updates Normal, Heading 1, Heading 2, and Hyperlink styles.
    /// </summary>
    /// <param name="document">The Word document to standardize styles in.</param>
    /// <returns>Report of style changes made to the document.</returns>
    StyleReport EnsureStyles(WordprocessingDocument document);

    /// <summary>
    /// Centers all images in the document by setting paragraph justification.
    /// Handles both inline and floating images.
    /// </summary>
    /// <param name="document">The Word document to center images in.</param>
    /// <returns>Number of images that were centered.</returns>
    int CenterImages(WordprocessingDocument document);

    /// <summary>
    /// Applies specific style to paragraphs that match the given criteria.
    /// </summary>
    /// <param name="document">The Word document to apply styles to.</param>
    /// <param name="styleId">The style ID to apply.</param>
    /// <param name="predicate">Function to determine which paragraphs should receive the style.</param>
    /// <returns>Number of paragraphs that had the style applied.</returns>
    int ApplyStyleSelectively(WordprocessingDocument document, string styleId, Func<string, bool> predicate);
}