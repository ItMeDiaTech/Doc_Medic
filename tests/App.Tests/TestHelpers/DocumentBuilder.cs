using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace App.Tests.TestHelpers;

/// <summary>
/// Helper class for building test Word documents using OpenXML SDK.
/// Provides fluent API for creating documents with hyperlinks, styles, and content.
/// </summary>
public class DocumentBuilder : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly WordprocessingDocument _document;
    private readonly Body _body;
    private bool _disposed = false;

    public DocumentBuilder()
    {
        _stream = new MemoryStream();
        _document = WordprocessingDocument.Create(_stream, WordprocessingDocumentType.Document);

        // Create main document part
        var mainPart = _document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        _body = mainPart.Document.Body!;
    }

    /// <summary>
    /// Adds a paragraph with the specified text.
    /// </summary>
    public DocumentBuilder AddParagraph(string text, string? styleId = null)
    {
        var paragraph = new Paragraph();

        if (!string.IsNullOrEmpty(styleId))
        {
            paragraph.AppendChild(new ParagraphProperties(
                new ParagraphStyleId { Val = styleId }));
        }

        paragraph.AppendChild(new Run(new Text(text)));
        _body.AppendChild(paragraph);

        return this;
    }

    /// <summary>
    /// Adds a paragraph with a hyperlink containing the specified URL and display text.
    /// </summary>
    public DocumentBuilder AddHyperlink(string url, string displayText, string? relationshipId = null)
    {
        // Add relationship for external hyperlink
        var mainPart = _document.MainDocumentPart!;
        var hyperlinkRelId = relationshipId ?? mainPart.AddHyperlinkRelationship(new Uri(url), true).Id;

        var hyperlink = new Hyperlink(new Run(new Text(displayText)))
        {
            Id = hyperlinkRelId
        };

        var paragraph = new Paragraph(hyperlink);
        _body.AppendChild(paragraph);

        return this;
    }

    /// <summary>
    /// Adds a hyperlink with display text split across multiple runs (for testing TextRange scenarios).
    /// </summary>
    public DocumentBuilder AddMultiRunHyperlink(string url, params string[] textParts)
    {
        if (textParts.Length == 0) throw new ArgumentException("At least one text part required.");

        var mainPart = _document.MainDocumentPart!;
        var hyperlinkRelId = mainPart.AddHyperlinkRelationship(new Uri(url), true).Id;

        var hyperlink = new Hyperlink { Id = hyperlinkRelId };

        // Add each text part as a separate run
        foreach (var textPart in textParts)
        {
            hyperlink.AppendChild(new Run(new Text(textPart)));
        }

        var paragraph = new Paragraph(hyperlink);
        _body.AppendChild(paragraph);

        return this;
    }

    /// <summary>
    /// Adds an internal hyperlink (anchor link).
    /// </summary>
    public DocumentBuilder AddInternalHyperlink(string anchorName, string displayText)
    {
        var hyperlink = new Hyperlink(new Run(new Text(displayText)))
        {
            Anchor = anchorName
        };

        var paragraph = new Paragraph(hyperlink);
        _body.AppendChild(paragraph);

        return this;
    }

    /// <summary>
    /// Adds a bookmark at the current position.
    /// </summary>
    public DocumentBuilder AddBookmark(string bookmarkName, string? bookmarkText = null)
    {
        var bookmarkStart = new BookmarkStart
        {
            Id = "1",
            Name = bookmarkName
        };

        var bookmarkEnd = new BookmarkEnd { Id = "1" };

        var paragraph = new Paragraph();
        paragraph.AppendChild(bookmarkStart);

        if (!string.IsNullOrEmpty(bookmarkText))
        {
            paragraph.AppendChild(new Run(new Text(bookmarkText)));
        }

        paragraph.AppendChild(bookmarkEnd);
        _body.AppendChild(paragraph);

        return this;
    }

    /// <summary>
    /// Adds a style to the document's styles part.
    /// </summary>
    public DocumentBuilder AddStyle(string styleId, string styleName, StyleValues styleType)
    {
        var mainPart = _document.MainDocumentPart!;

        // Create styles part if it doesn't exist
        StyleDefinitionsPart? stylesPart = mainPart.StyleDefinitionsPart;
        if (stylesPart == null)
        {
            stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles();
        }

        var style = new Style
        {
            StyleId = styleId,
            Type = styleType
        };

        style.AppendChild(new StyleName { Val = styleName });
        stylesPart.Styles!.AppendChild(style);

        return this;
    }

    /// <summary>
    /// Adds style properties to a style.
    /// </summary>
    public DocumentBuilder SetStyleProperties(string styleId, Action<StyleProperties> configure)
    {
        var stylesPart = _document.MainDocumentPart!.StyleDefinitionsPart;
        var style = stylesPart?.Styles?.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);

        if (style != null)
        {
            var properties = new StyleProperties();
            configure(properties);

            if (style.Type?.Value == StyleValues.Paragraph)
            {
                var paragraphProps = style.StyleParagraphProperties ?? new StyleParagraphProperties();
                style.StyleParagraphProperties = paragraphProps;
            }
            else if (style.Type?.Value == StyleValues.Character)
            {
                var runProps = style.StyleRunProperties ?? new StyleRunProperties();
                style.StyleRunProperties = runProps;
            }
        }

        return this;
    }

    /// <summary>
    /// Adds an image to the document (centered paragraph).
    /// </summary>
    public DocumentBuilder AddCenteredImage(string imageName = "test-image")
    {
        // Create a paragraph with center alignment
        var paragraph = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center }
            ),
            new Run(
                new Text($"[{imageName}]") // Placeholder for actual image
            )
        );

        _body.AppendChild(paragraph);
        return this;
    }

    /// <summary>
    /// Gets the built document as a MemoryStream.
    /// </summary>
    public MemoryStream GetStream()
    {
        _document.Save();
        _stream.Position = 0;
        return _stream;
    }

    /// <summary>
    /// Gets the WordprocessingDocument for direct manipulation.
    /// </summary>
    public WordprocessingDocument GetDocument() => _document;

    /// <summary>
    /// Saves the document and returns it for testing.
    /// </summary>
    public WordprocessingDocument Build()
    {
        _document.Save();
        return _document;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _document?.Dispose();
            _stream?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Helper class for configuring style properties in tests.
/// </summary>
public class StyleProperties
{
    public string? FontName { get; set; }
    public int? FontSize { get; set; }
    public bool? Bold { get; set; }
    public string? Color { get; set; }
    public bool? Underline { get; set; }
    public int? SpaceBefore { get; set; }
    public int? SpaceAfter { get; set; }
    public string? LineSpacing { get; set; }
}

/// <summary>
/// Static helper methods for common test document scenarios.
/// </summary>
public static class TestDocuments
{
    /// <summary>
    /// Creates a document with eligible hyperlinks for testing.
    /// </summary>
    public static DocumentBuilder WithEligibleHyperlinks()
    {
        return new DocumentBuilder()
            .AddHyperlink("http://example.com?docid=12345", "Document Link")
            .AddHyperlink("http://site.com/CMS-1-123456/path", "CMS Document")
            .AddHyperlink("http://test.com/TSRC-abc123-987654", "TSRC Reference");
    }

    /// <summary>
    /// Creates a document with multi-run hyperlink display text.
    /// </summary>
    public static DocumentBuilder WithMultiRunHyperlinks()
    {
        return new DocumentBuilder()
            .AddMultiRunHyperlink("http://example.com?docid=12345", "Document", " Link", " Title")
            .AddMultiRunHyperlink("http://site.com/CMS-1-123456/path", "CMS ", "Document ", "(123456)");
    }

    /// <summary>
    /// Creates a document with Top of Document links.
    /// </summary>
    public static DocumentBuilder WithTopOfDocumentLinks()
    {
        return new DocumentBuilder()
            .AddBookmark("DocStart")
            .AddParagraph("Document content here...")
            .AddInternalHyperlink("DocStart", "Top of Document")
            .AddInternalHyperlink("DocStart", "TOP OF DOCUMENT");
    }

    /// <summary>
    /// Creates a document with various styles for testing standardization.
    /// </summary>
    public static DocumentBuilder WithStylesToStandardize()
    {
        return new DocumentBuilder()
            .AddStyle("Normal", "Normal", StyleValues.Paragraph)
            .AddStyle("Heading1", "Heading 1", StyleValues.Paragraph)
            .AddStyle("Heading2", "Heading 2", StyleValues.Paragraph)
            .AddStyle("Hyperlink", "Hyperlink", StyleValues.Character)
            .AddParagraph("Normal paragraph", "Normal")
            .AddParagraph("First Heading", "Heading1")
            .AddParagraph("Second Heading", "Heading2");
    }

    /// <summary>
    /// Creates a document with multiple spaces for normalization testing.
    /// </summary>
    public static DocumentBuilder WithMultipleSpaces()
    {
        return new DocumentBuilder()
            .AddParagraph("Text  with   multiple    spaces")
            .AddParagraph("Another   paragraph  with    spaces");
    }

    /// <summary>
    /// Creates a document with images for centering tests.
    /// </summary>
    public static DocumentBuilder WithImages()
    {
        return new DocumentBuilder()
            .AddCenteredImage("image1")
            .AddParagraph("Some text content")
            .AddCenteredImage("image2");
    }
}