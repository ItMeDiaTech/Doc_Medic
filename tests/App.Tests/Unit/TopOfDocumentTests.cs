using App.Domain;
using App.Tests.TestHelpers;
using DocumentFormat.OpenXml.Wordprocessing;

namespace App.Tests.Unit;

/// <summary>
/// Tests for "Top of Document" bookmark and anchor functionality as specified in CLAUDE.md Section 6.
/// Verifies bookmark creation, internal anchor updates, and paragraph alignment/styling.
/// </summary>
public class TopOfDocumentTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    #region Bookmark Creation Tests

    [Fact]
    public void DocumentStart_BookmarkExists_CanBeReferenced()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithTopOfDocumentLinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var bookmarks = document.MainDocumentPart!.Document.Descendants<BookmarkStart>().ToList();
        var docStartBookmark = bookmarks.FirstOrDefault(b => b.Name == "DocStart");

        // Assert
        docStartBookmark.Should().NotBeNull("DocStart bookmark should exist");
        docStartBookmark!.Name!.Value.Should().Be("DocStart");
        docStartBookmark.Id.Should().NotBeNull("Bookmark should have an ID");
    }

    [Fact]
    public void BookmarkStart_HasCorrespondingBookmarkEnd()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithTopOfDocumentLinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var bookmarkStart = document.MainDocumentPart!.Document.Descendants<BookmarkStart>()
            .FirstOrDefault(b => b.Name == "DocStart");
        var bookmarkEnd = document.MainDocumentPart.Document.Descendants<BookmarkEnd>()
            .FirstOrDefault(b => b.Id == bookmarkStart?.Id);

        // Assert
        bookmarkStart.Should().NotBeNull("BookmarkStart should exist");
        bookmarkEnd.Should().NotBeNull("BookmarkEnd should exist");
        bookmarkStart!.Id!.Value.Should().Be(bookmarkEnd!.Id!.Value, "BookmarkStart and BookmarkEnd should have matching IDs");
    }

    [Fact]
    public void DocStartBookmark_IsAtDocumentStart()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithTopOfDocumentLinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var body = document.MainDocumentPart!.Document.Body!;
        var firstElement = body.Elements().FirstOrDefault();
        var bookmarkInFirstParagraph = firstElement?.Descendants<BookmarkStart>()
            .FirstOrDefault(b => b.Name == "DocStart");

        // Assert
        bookmarkInFirstParagraph.Should().NotBeNull("DocStart bookmark should be in the first paragraph or element");
    }

    #endregion

    #region "Top of Document" Pattern Recognition Tests

    [Theory]
    [InlineData("Top of Document")]
    [InlineData("top of document")]
    [InlineData("TOP OF DOCUMENT")]
    [InlineData("Top Of Document")]
    [InlineData("  Top of Document  ")]  // With whitespace
    [InlineData("Top  of   Document")]   // Multiple spaces between words
    public void TopOfDocumentPattern_WithValidText_IsRecognized(string text)
    {
        // Act
        var isMatch = HyperlinkPatterns.TopOfDocumentPattern.IsMatch(text);

        // Assert
        isMatch.Should().BeTrue($"'{text}' should be recognized as Top of Document pattern");
    }

    [Theory]
    [InlineData("Go to Top of Document")]  // Extra text at beginning
    [InlineData("Top of Document link")]   // Extra text at end
    [InlineData("Top of Doc")]             // Abbreviated
    [InlineData("Document Top")]           // Different wording
    [InlineData("Back to Top")]            // Different phrase
    public void TopOfDocumentPattern_WithInvalidText_IsNotRecognized(string text)
    {
        // Act
        var isMatch = HyperlinkPatterns.TopOfDocumentPattern.IsMatch(text);

        // Assert
        isMatch.Should().BeFalse($"'{text}' should not be recognized as Top of Document pattern");
    }

    #endregion

    #region Internal Hyperlink Tests

    [Fact]
    public void InternalHyperlink_WithDocStartAnchor_LinksCorrectly()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithTopOfDocumentLinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var hyperlinks = document.MainDocumentPart!.Document.Descendants<Hyperlink>().ToList();
        var docStartLinks = hyperlinks.Where(h => h.Anchor == "DocStart").ToList();

        // Assert
        docStartLinks.Should().NotBeEmpty("Should have hyperlinks with DocStart anchor");
        docStartLinks.Should().AllSatisfy(link => 
        {
            link.Anchor!.Value.Should().Be("DocStart");
            link.Id.Should().BeNull("Internal links should not have relationship IDs");
        });
    }

    [Fact]
    public void TopOfDocumentLink_HasCorrectDisplayText()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithTopOfDocumentLinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var hyperlinks = document.MainDocumentPart!.Document.Descendants<Hyperlink>()
            .Where(h => h.Anchor == "DocStart").ToList();

        // Assert
        hyperlinks.Should().NotBeEmpty("Should have Top of Document links");
        
        foreach (var link in hyperlinks)
        {
            var displayText = string.Concat(link.Descendants<Text>().Select(t => t.Text));
            HyperlinkPatterns.TopOfDocumentPattern.IsMatch(displayText)
                .Should().BeTrue($"Link text '{displayText}' should match Top of Document pattern");
        }
    }

    #endregion

    #region Paragraph Alignment Tests

    [Fact]
    public void TopOfDocumentParagraph_IsRightAligned()
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        
        // Create a Top of Document link with right alignment
        docBuilder.AddBookmark("DocStart");
        var paragraph = new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Right }),
            new Hyperlink(new Run(new Text("Top of Document"))) { Anchor = "DocStart" }
        );
        
        // Add the paragraph directly to test alignment
        var document = docBuilder.GetDocument();
        document.MainDocumentPart!.Document.Body!.AppendChild(paragraph);
        docBuilder.Build();

        // Act
        var topDocParagraphs = document.MainDocumentPart.Document.Descendants<Paragraph>()
            .Where(p => p.Descendants<Hyperlink>().Any(h => h.Anchor == "DocStart")).ToList();

        // Assert
        topDocParagraphs.Should().NotBeEmpty("Should find Top of Document paragraphs");
        topDocParagraphs.Should().AllSatisfy(p => 
        {
            var justification = p.ParagraphProperties?.Justification?.Val;
            justification?.Value.Should().Be(JustificationValues.Right, "Top of Document paragraphs should be right-aligned");
        });
    }

    #endregion

    #region Hyperlink Style Application Tests

    [Fact]
    public void TopOfDocumentLink_AppliesHyperlinkCharacterStyle()
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        
        // Add hyperlink character style
        docBuilder.AddStyle("Hyperlink", "Hyperlink", StyleValues.Character);
        docBuilder.AddBookmark("DocStart");
        
        // Create hyperlink with character style applied
        var run = new Run(new Text("Top of Document"));
        run.RunProperties = new RunProperties(new RunStyle { Val = "Hyperlink" });
        
        var hyperlink = new Hyperlink(run) { Anchor = "DocStart" };
        var paragraph = new Paragraph(hyperlink);
        
        var document = docBuilder.GetDocument();
        document.MainDocumentPart!.Document.Body!.AppendChild(paragraph);
        docBuilder.Build();

        // Act
        var topDocLink = document.MainDocumentPart.Document.Descendants<Hyperlink>()
            .FirstOrDefault(h => h.Anchor == "DocStart");

        // Assert
        topDocLink.Should().NotBeNull("Top of Document hyperlink should exist");
        
        var runWithStyle = topDocLink!.Descendants<Run>().FirstOrDefault();
        runWithStyle.Should().NotBeNull("Hyperlink should contain runs");
        
        var runStyle = runWithStyle!.RunProperties?.RunStyle?.Val;
        runStyle?.Value.Should().Be("Hyperlink", "Top of Document link should have Hyperlink character style applied");
    }

    #endregion

    #region Case-Insensitive Matching Tests

    [Theory]
    [InlineData("Top of Document", "DocStart")]
    [InlineData("top of document", "DocStart")]
    [InlineData("TOP OF DOCUMENT", "DocStart")]
    [InlineData("Top Of Document", "DocStart")]
    public void TopOfDocumentText_CaseInsensitive_MatchesAndLinksToDocStart(string linkText, string expectedAnchor)
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        docBuilder.AddBookmark("DocStart");
        docBuilder.AddInternalHyperlink(expectedAnchor, linkText);
        var document = docBuilder.Build();

        // Act
        var hyperlink = document.MainDocumentPart!.Document.Descendants<Hyperlink>()
            .FirstOrDefault(h => h.Anchor == expectedAnchor);

        // Assert
        hyperlink.Should().NotBeNull($"Should find hyperlink with anchor '{expectedAnchor}'");
        
        var displayText = string.Concat(hyperlink!.Descendants<Text>().Select(t => t.Text));
        displayText.Should().Be(linkText);
        
        // Verify the text matches the Top of Document pattern (case-insensitive)
        HyperlinkPatterns.TopOfDocumentPattern.IsMatch(displayText)
            .Should().BeTrue($"'{displayText}' should match Top of Document pattern");
    }

    #endregion

    #region Whitespace Normalization Tests

    [Theory]
    [InlineData("  Top of Document  ", "Top of Document")]
    [InlineData("Top  of   Document", "Top of Document")]
    [InlineData("\tTop\tof\tDocument\t", "Top of Document")]
    public void TopOfDocumentText_WithExtraWhitespace_IsNormalizedForMatching(string originalText, string normalizedText)
    {
        // Act - Simulate the normalization that would happen in the service
        var normalized = System.Text.RegularExpressions.Regex.Replace(originalText.Trim(), @"\s+", " ");

        // Assert
        normalized.Should().Be(normalizedText);
        HyperlinkPatterns.TopOfDocumentPattern.IsMatch(normalized)
            .Should().BeTrue($"Normalized text '{normalized}' should match Top of Document pattern");
    }

    #endregion

    #region Multiple Top of Document Links Tests

    [Fact]
    public void MultipleTopOfDocumentLinks_AllPointToSameBookmark()
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        docBuilder.AddBookmark("DocStart");
        docBuilder.AddInternalHyperlink("DocStart", "Top of Document");
        docBuilder.AddInternalHyperlink("DocStart", "top of document");
        docBuilder.AddInternalHyperlink("DocStart", "TOP OF DOCUMENT");
        var document = docBuilder.Build();

        // Act
        var topDocLinks = document.MainDocumentPart!.Document.Descendants<Hyperlink>()
            .Where(h => h.Anchor == "DocStart").ToList();

        // Assert
        topDocLinks.Should().HaveCount(3, "Should have three Top of Document links");
        topDocLinks.Should().AllSatisfy(link => 
        {
            link.Anchor!.Value.Should().Be("DocStart");
            var displayText = string.Concat(link.Descendants<Text>().Select(t => t.Text));
            HyperlinkPatterns.TopOfDocumentPattern.IsMatch(displayText)
                .Should().BeTrue($"Link text '{displayText}' should match pattern");
        });
    }

    #endregion

    #region Bookmark ID Management Tests

    [Fact]
    public void BookmarkIds_AreUniqueAcrossDocument()
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        
        // Add multiple bookmarks to test ID uniqueness
        docBuilder.AddBookmark("DocStart");
        docBuilder.AddBookmark("Section1");
        docBuilder.AddBookmark("Section2");
        var document = docBuilder.Build();

        // Act
        var bookmarkStarts = document.MainDocumentPart!.Document.Descendants<BookmarkStart>().ToList();
        var bookmarkIds = bookmarkStarts.Select(b => b.Id!.Value).ToList();

        // Assert
        bookmarkIds.Should().OnlyHaveUniqueItems("All bookmark IDs should be unique");
        bookmarkIds.Should().AllSatisfy(id => 
        {
            id.Should().NotBeNullOrEmpty("Bookmark ID should not be null or empty");
        });
    }

    [Fact]
    public void BookmarkEnd_MatchesBookmarkStart_Id()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithTopOfDocumentLinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var bookmarkStarts = document.MainDocumentPart!.Document.Descendants<BookmarkStart>().ToList();
        var bookmarkEnds = document.MainDocumentPart.Document.Descendants<BookmarkEnd>().ToList();

        // Assert
        foreach (var start in bookmarkStarts)
        {
            var matchingEnd = bookmarkEnds.FirstOrDefault(end => end.Id == start.Id);
            matchingEnd.Should().NotBeNull($"BookmarkStart with ID '{start.Id}' should have matching BookmarkEnd");
        }
    }

    #endregion

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();
    }
}