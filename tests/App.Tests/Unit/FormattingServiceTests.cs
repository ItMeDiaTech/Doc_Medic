using App.Domain;
using App.Infrastructure.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace App.Tests.Unit;

/// <summary>
/// Unit tests for FormattingService focusing on style standardization as specified in CLAUDE.md Section 16.
/// Tests Verdana fonts, sizes, and spacing values per specification.
/// Also tests space normalization and Top of Document link handling.
/// </summary>
public class FormattingServiceTests
{
    private readonly Mock<ILogger<FormattingService>> _mockLogger;
    private readonly FormattingService _service;

    public FormattingServiceTests()
    {
        _mockLogger = new Mock<ILogger<FormattingService>>();
        _service = new FormattingService(_mockLogger.Object);
    }

    public class StyleCreationTests
    {
        private readonly FormattingService _service;

        public StyleCreationTests()
        {
            var mockLogger = new Mock<ILogger<FormattingService>>();
            _service = new FormattingService(mockLogger.Object);
        }

        [Fact]
        public void EnsureStyles_EmptyDocument_CreatesAllRequiredStyles()
        {
            // Arrange
            using var document = CreateEmptyWordDocument();

            // Act
            var result = _service.EnsureStyles(document);

            // Assert
            result.Should().NotBeNull();
            result.NormalStyleCreated.Should().BeTrue();
            result.Heading1StyleCreated.Should().BeTrue();
            result.Heading2StyleCreated.Should().BeTrue();
            result.HyperlinkStyleCreated.Should().BeTrue();

            // Verify styles exist in the document
            var stylesPart = document.MainDocumentPart?.StyleDefinitionsPart;
            stylesPart.Should().NotBeNull();
            
            var styles = stylesPart!.Styles!.Descendants<Style>().ToList();
            styles.Should().Contain(s => s.StyleId != null && s.StyleId.Value == "Normal");
            styles.Should().Contain(s => s.StyleId != null && s.StyleId.Value == "Heading1");
            styles.Should().Contain(s => s.StyleId != null && s.StyleId.Value == "Heading2");
            styles.Should().Contain(s => s.StyleId != null && s.StyleId.Value == "Hyperlink");
        }

        [Fact]
        public void NormalStyle_HasCorrectProperties()
        {
            // Arrange
            using var document = CreateEmptyWordDocument();

            // Act
            _service.EnsureStyles(document);

            // Assert
            var normalStyle = GetStyleById(document, "Normal");
            normalStyle.Should().NotBeNull();
            normalStyle!.Type!.Value.Should().Be(StyleValues.Paragraph);

            // Check font properties
            var runProps = normalStyle.StyleRunProperties;
            runProps.Should().NotBeNull();
            runProps!.RunFonts!.Ascii!.Value.Should().Be("Verdana");
            runProps.RunFonts.HighAnsi!.Value.Should().Be("Verdana");
            runProps.RunFonts.ComplexScript!.Value.Should().Be("Verdana");
            runProps.FontSize!.Val!.Value.Should().Be("24"); // 12pt = 24 half-points
            runProps.Color!.Val!.Value.Should().Be("000000");

            // Check paragraph properties
            var paraProps = normalStyle.StyleParagraphProperties;
            paraProps.Should().NotBeNull();
            paraProps!.SpacingBetweenLines!.Before!.Value.Should().Be("120"); // 6pt = 120 twips
            paraProps.SpacingBetweenLines.Line!.Value.Should().Be("240"); // Single spacing
            paraProps.SpacingBetweenLines.LineRule!.Value.Should().Be(LineSpacingRuleValues.Auto);
        }

        [Fact]
        public void Heading1Style_HasCorrectProperties()
        {
            // Arrange
            using var document = CreateEmptyWordDocument();

            // Act
            _service.EnsureStyles(document);

            // Assert
            var heading1Style = GetStyleById(document, "Heading1");
            heading1Style.Should().NotBeNull();
            heading1Style!.Type!.Value.Should().Be(StyleValues.Paragraph);

            // Check font properties
            var runProps = heading1Style.StyleRunProperties;
            runProps.Should().NotBeNull();
            runProps!.RunFonts!.Ascii!.Value.Should().Be("Verdana");
            runProps.FontSize!.Val!.Value.Should().Be("36"); // 18pt = 36 half-points
            runProps.Bold.Should().NotBeNull();
            runProps.Color!.Val!.Value.Should().Be("000000");

            // Check paragraph properties
            var paraProps = heading1Style.StyleParagraphProperties;
            paraProps.Should().NotBeNull();
            paraProps!.Justification!.Val!.Value.Should().Be(JustificationValues.Left);
            paraProps.SpacingBetweenLines!.Before!.Value.Should().Be("0"); // 0pt before
            paraProps.SpacingBetweenLines.After!.Value.Should().Be("240"); // 12pt after = 240 twips
            paraProps.SpacingBetweenLines.Line!.Value.Should().Be("240"); // Single spacing
        }

        [Fact]
        public void Heading2Style_HasCorrectProperties()
        {
            // Arrange
            using var document = CreateEmptyWordDocument();

            // Act
            _service.EnsureStyles(document);

            // Assert
            var heading2Style = GetStyleById(document, "Heading2");
            heading2Style.Should().NotBeNull();
            heading2Style!.Type!.Value.Should().Be(StyleValues.Paragraph);

            // Check font properties
            var runProps = heading2Style.StyleRunProperties;
            runProps.Should().NotBeNull();
            runProps!.RunFonts!.Ascii!.Value.Should().Be("Verdana");
            runProps.FontSize!.Val!.Value.Should().Be("28"); // 14pt = 28 half-points
            runProps.Bold.Should().NotBeNull();
            runProps.Color!.Val!.Value.Should().Be("000000");

            // Check paragraph properties
            var paraProps = heading2Style.StyleParagraphProperties;
            paraProps.Should().NotBeNull();
            paraProps!.Justification!.Val!.Value.Should().Be(JustificationValues.Left);
            paraProps.SpacingBetweenLines!.Before!.Value.Should().Be("120"); // 6pt before = 120 twips
            paraProps.SpacingBetweenLines.After!.Value.Should().Be("120"); // 6pt after = 120 twips
            paraProps.SpacingBetweenLines.Line!.Value.Should().Be("240"); // Single spacing
        }

        [Fact]
        public void HyperlinkStyle_HasCorrectProperties()
        {
            // Arrange
            using var document = CreateEmptyWordDocument();

            // Act
            _service.EnsureStyles(document);

            // Assert
            var hyperlinkStyle = GetStyleById(document, "Hyperlink");
            hyperlinkStyle.Should().NotBeNull();
            hyperlinkStyle!.Type!.Value.Should().Be(StyleValues.Character);

            // Check run properties (character style)
            var runProps = hyperlinkStyle.StyleRunProperties;
            runProps.Should().NotBeNull();
            runProps!.RunFonts!.Ascii!.Value.Should().Be("Verdana");
            runProps.FontSize!.Val!.Value.Should().Be("24"); // 12pt = 24 half-points
            runProps.Color!.Val!.Value.Should().Be("0000FF"); // Blue #0000FF
            runProps.Underline!.Val!.Value.Should().Be(UnderlineValues.Single);
        }

        [Fact]
        public void EnsureStyles_ExistingStyles_UpdatesInsteadOfCreating()
        {
            // Arrange
            using var document = CreateDocumentWithExistingStyles();

            // Act
            var result = _service.EnsureStyles(document);

            // Assert
            // Should update existing styles, not create new ones
            result.NormalStyleCreated.Should().BeFalse();
            result.Heading1StyleCreated.Should().BeFalse();
            result.Heading2StyleCreated.Should().BeFalse();
            result.HyperlinkStyleCreated.Should().BeFalse();

            // Verify styles still have correct properties after update
            var normalStyle = GetStyleById(document, "Normal");
            normalStyle!.StyleRunProperties!.RunFonts!.Ascii!.Value.Should().Be("Verdana");
        }

        private Style? GetStyleById(WordprocessingDocument document, string styleId)
        {
            return document.MainDocumentPart?.StyleDefinitionsPart?.Styles?
                .Descendants<Style>()
                .FirstOrDefault(s => string.Equals(s.StyleId?.Value, styleId, StringComparison.OrdinalIgnoreCase));
        }

        private WordprocessingDocument CreateEmptyWordDocument()
        {
            var stream = new MemoryStream();
            var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
            
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            mainPart.Document.AppendChild(new Body());

            return document;
        }

        private WordprocessingDocument CreateDocumentWithExistingStyles()
        {
            var document = CreateEmptyWordDocument();
            var mainPart = document.MainDocumentPart!;
            
            // Add styles part with minimal existing styles
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles();
            
            // Add minimal existing styles that need updating
            stylesPart.Styles.AppendChild(new Style 
            { 
                StyleId = "Normal", 
                Type = StyleValues.Paragraph,
                StyleName = new StyleName { Val = "Normal" }
            });
            
            stylesPart.Styles.AppendChild(new Style 
            { 
                StyleId = "Heading1", 
                Type = StyleValues.Paragraph,
                StyleName = new StyleName { Val = "Heading 1" }
            });
            
            stylesPart.Styles.AppendChild(new Style 
            { 
                StyleId = "Heading2", 
                Type = StyleValues.Paragraph,
                StyleName = new StyleName { Val = "Heading 2" }
            });
            
            stylesPart.Styles.AppendChild(new Style 
            { 
                StyleId = "Hyperlink", 
                Type = StyleValues.Character,
                StyleName = new StyleName { Val = "Hyperlink" }
            });

            return document;
        }
    }

    public class SpaceNormalizationTests
    {
        private readonly FormattingService _service;

        public SpaceNormalizationTests()
        {
            var mockLogger = new Mock<ILogger<FormattingService>>();
            _service = new FormattingService(mockLogger.Object);
        }

        [Fact]
        public void NormalizeSpaces_MultipleSpaces_CollapsesToSingle()
        {
            // Arrange
            using var document = CreateDocumentWithText("Text  with   multiple    spaces");

            // Act
            var result = _service.NormalizeSpaces(document);

            // Assert
            result.Should().Be(1);
            var text = GetDocumentText(document);
            text.Should().Be("Text with multiple spaces");
        }

        [Fact]
        public void NormalizeSpaces_PreservesNonBreakingSpaces()
        {
            // Arrange
            var textWithNbsp = "Text\u00A0with\u00A0non-breaking\u00A0spaces";
            using var document = CreateDocumentWithText(textWithNbsp);

            // Act
            var result = _service.NormalizeSpaces(document);

            // Assert
            result.Should().Be(0); // No changes made
            var text = GetDocumentText(document);
            text.Should().Be(textWithNbsp); // Unchanged
        }

        [Fact]
        public void NormalizeSpaces_PreservesLeadingTrailingSpaces()
        {
            // Arrange
            using var document = CreateDocumentWithText("  Leading  and  trailing  ");

            // Act
            var result = _service.NormalizeSpaces(document);

            // Assert
            result.Should().Be(1);
            var text = GetDocumentText(document);
            text.Should().Be("  Leading and trailing  ");

            // Verify xml:space is preserved on text elements with leading/trailing spaces
            var textElements = document.MainDocumentPart!.Document.Descendants<Text>().ToList();
            var modifiedText = textElements.First(t => t.Text == "  Leading and trailing  ");
            modifiedText.Space!.Value.Should().Be(SpaceProcessingModeValues.Preserve);
        }

        [Fact]
        public void NormalizeSpaces_EmptyDocument_ReturnsZero()
        {
            // Arrange
            using var document = CreateEmptyWordDocument();

            // Act
            var result = _service.NormalizeSpaces(document);

            // Assert
            result.Should().Be(0);
        }

        private WordprocessingDocument CreateDocumentWithText(string text)
        {
            var document = CreateEmptyWordDocument();
            var body = document.MainDocumentPart!.Document.Body!;
            
            var paragraph = new Paragraph();
            var run = new Run();
            var textElement = new Text(text);
            
            run.AppendChild(textElement);
            paragraph.AppendChild(run);
            body.AppendChild(paragraph);

            return document;
        }

        private string GetDocumentText(WordprocessingDocument document)
        {
            return string.Join("", document.MainDocumentPart!.Document.Descendants<Text>().Select(t => t.Text));
        }

        private WordprocessingDocument CreateEmptyWordDocument()
        {
            var stream = new MemoryStream();
            var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
            
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            mainPart.Document.AppendChild(new Body());

            return document;
        }
    }

    public class TopOfDocumentTests
    {
        private readonly FormattingService _service;

        public TopOfDocumentTests()
        {
            var mockLogger = new Mock<ILogger<FormattingService>>();
            _service = new FormattingService(mockLogger.Object);
        }

        [Fact]
        public void FixTopOfDocumentLinks_CreatesDocStartBookmark()
        {
            // Arrange
            using var document = CreateDocumentWithText("Some content");

            // Act
            var result = _service.FixTopOfDocumentLinks(document);

            // Assert
            var bookmarks = document.MainDocumentPart!.Document.Descendants<BookmarkStart>().ToList();
            bookmarks.Should().Contain(b => b.Name != null && b.Name.Value == "DocStart");
            
            var docStartBookmark = bookmarks.First(b => b.Name != null && b.Name.Value == "DocStart");
            docStartBookmark.Id.Should().NotBeNull();
            
            // Verify matching bookmark end exists
            var bookmarkEnds = document.MainDocumentPart.Document.Descendants<BookmarkEnd>().ToList();
            bookmarkEnds.Should().Contain(b => b.Id != null && docStartBookmark.Id != null && b.Id.Value == docStartBookmark.Id.Value);
        }

        [Theory]
        [InlineData("Top of Document")]
        [InlineData("TOP OF DOCUMENT")]
        [InlineData("top of document")]
        [InlineData("  Top of Document  ")]
        [InlineData("Top  of  Document")]
        public void FixTopOfDocumentLinks_MatchesVariousFormats(string topText)
        {
            // Arrange
            using var document = CreateDocumentWithTopOfDocText(topText);

            // Act
            var result = _service.FixTopOfDocumentLinks(document);

            // Assert
            result.Should().Be(1);
            
            // Verify hyperlink was created
            var hyperlinks = document.MainDocumentPart!.Document.Descendants<Hyperlink>().ToList();
            hyperlinks.Should().HaveCount(1);
            hyperlinks[0].Anchor?.Value.Should().Be("DocStart");
            
            // Verify paragraph is right-aligned
            var paragraph = document.MainDocumentPart.Document.Descendants<Paragraph>().First();
            var justification = paragraph.GetFirstChild<ParagraphProperties>()?.GetFirstChild<Justification>();
            justification!.Val!.Value.Should().Be(JustificationValues.Right);
            
            // Verify hyperlink style is applied to runs
            var runs = hyperlinks[0].Descendants<Run>().ToList();
            runs.Should().HaveCountGreaterThan(0);
            runs.All(r => r.RunProperties?.RunStyle?.Val?.Value == "Hyperlink").Should().BeTrue();
        }

        [Fact]
        public void FixTopOfDocumentLinks_ExistingBookmark_DoesNotDuplicate()
        {
            // Arrange
            using var document = CreateDocumentWithExistingDocStartBookmark();

            // Act
            var result = _service.FixTopOfDocumentLinks(document);

            // Assert
            var bookmarks = document.MainDocumentPart!.Document.Descendants<BookmarkStart>()
                .Where(b => b.Name?.Value == "DocStart").ToList();
            bookmarks.Should().HaveCount(1); // Should not duplicate
        }

        [Fact]
        public void FixTopOfDocumentLinks_NoMatchingText_ReturnsZero()
        {
            // Arrange
            using var document = CreateDocumentWithText("Regular content");

            // Act
            var result = _service.FixTopOfDocumentLinks(document);

            // Assert
            result.Should().Be(0);
            
            // Bookmark should still be created
            var bookmarks = document.MainDocumentPart!.Document.Descendants<BookmarkStart>().ToList();
            bookmarks.Should().Contain(b => b.Name != null && b.Name.Value == "DocStart");
        }

        private WordprocessingDocument CreateDocumentWithTopOfDocText(string text)
        {
            var document = CreateEmptyWordDocument();
            var body = document.MainDocumentPart!.Document.Body!;
            
            var paragraph = new Paragraph();
            var run = new Run();
            var textElement = new Text(text);
            
            run.AppendChild(textElement);
            paragraph.AppendChild(run);
            body.AppendChild(paragraph);

            return document;
        }

        private WordprocessingDocument CreateDocumentWithExistingDocStartBookmark()
        {
            var document = CreateDocumentWithText("Content");
            var firstParagraph = document.MainDocumentPart!.Document.Body!.GetFirstChild<Paragraph>()!;
            
            // Insert existing DocStart bookmark
            var bookmarkStart = new BookmarkStart { Name = "DocStart", Id = "1" };
            var bookmarkEnd = new BookmarkEnd { Id = "1" };
            
            firstParagraph.InsertAt(bookmarkStart, 0);
            firstParagraph.InsertAt(bookmarkEnd, 1);

            return document;
        }

        private WordprocessingDocument CreateDocumentWithText(string text)
        {
            var document = CreateEmptyWordDocument();
            var body = document.MainDocumentPart!.Document.Body!;
            
            var paragraph = new Paragraph();
            var run = new Run();
            var textElement = new Text(text);
            
            run.AppendChild(textElement);
            paragraph.AppendChild(run);
            body.AppendChild(paragraph);

            return document;
        }

        private WordprocessingDocument CreateEmptyWordDocument()
        {
            var stream = new MemoryStream();
            var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
            
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            mainPart.Document.AppendChild(new Body());

            return document;
        }
    }

    public class ImageCenteringTests
    {
        private readonly FormattingService _service;

        public ImageCenteringTests()
        {
            var mockLogger = new Mock<ILogger<FormattingService>>();
            _service = new FormattingService(mockLogger.Object);
        }

        [Fact]
        public void CenterImages_ParagraphWithDrawing_CentersParagraph()
        {
            // Arrange
            using var document = CreateDocumentWithDrawing();

            // Act
            var result = _service.CenterImages(document);

            // Assert
            result.Should().Be(1);
            
            var paragraphsWithDrawings = document.MainDocumentPart!.Document.Descendants<Paragraph>()
                .Where(p => p.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().Any())
                .ToList();
            
            paragraphsWithDrawings.Should().HaveCount(1);
            
            var paragraph = paragraphsWithDrawings.First();
            var justification = paragraph.GetFirstChild<ParagraphProperties>()?.GetFirstChild<Justification>();
            justification!.Val!.Value.Should().Be(JustificationValues.Center);
        }

        private WordprocessingDocument CreateDocumentWithDrawing()
        {
            var document = CreateEmptyWordDocument();
            var body = document.MainDocumentPart!.Document.Body!;
            
            var paragraph = new Paragraph();
            var run = new Run();
            var drawing = new Drawing();
            
            // Create a minimal drawing structure with DocProperties
            var docProps = new DocProperties 
            { 
                Id = 1, 
                Name = "Picture 1" 
            };
            drawing.AppendChild(docProps);
            
            run.AppendChild(drawing);
            paragraph.AppendChild(run);
            body.AppendChild(paragraph);

            return document;
        }

        private WordprocessingDocument CreateEmptyWordDocument()
        {
            var stream = new MemoryStream();
            var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
            
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            mainPart.Document.AppendChild(new Body());

            return document;
        }
    }
}