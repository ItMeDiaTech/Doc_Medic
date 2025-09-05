using App.Domain;
using App.Infrastructure.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Moq;

namespace App.Tests.Unit;

/// <summary>
/// Unit tests for HyperlinkRepairService focusing on display text updates and TextRange handling
/// as specified in CLAUDE.md Section 16. Tests display text spanning multiple runs and
/// correct (XXXXXX) insertion logic for 6/5 digits with zero-padding.
/// </summary>
public class HyperlinkRepairServiceTests
{
    private readonly Mock<ILogger<HyperlinkRepairService>> _mockLogger;
    private readonly HyperlinkRepairService _service;

    public HyperlinkRepairServiceTests()
    {
        _mockLogger = new Mock<ILogger<HyperlinkRepairService>>();
        _service = new HyperlinkRepairService(_mockLogger.Object);
    }

    public class DisplaySuffixTests
    {
        [Theory]
        [InlineData("CMS-TEST-123456", "123456")]
        [InlineData("TSRC-PROJECT-789012", "789012")]
        [InlineData("CMS-A1B2C3-999888", "999888")]
        [InlineData("TSRC-LONG-NAME-WITH-DASHES-654321", "654321")]
        public void GetDisplaySuffix_SixDigits_ReturnsLastSixDigits(string contentId, string expectedSuffix)
        {
            // Arrange
            var lookupResult = new LookupResult("Test Title", "Released", contentId, "DOC123");

            // Act
            var result = lookupResult.GetDisplaySuffix();

            // Assert
            result.Should().Be(expectedSuffix);
        }

        [Theory]
        [InlineData("CMS-TEST-12345", "012345")] // Prefix with 0 for 5 digits
        [InlineData("TSRC-PROJECT-98765", "098765")]
        [InlineData("CMS-SHORT-11111", "011111")]
        public void GetDisplaySuffix_FiveDigits_PrefixesWithZero(string contentId, string expectedSuffix)
        {
            // Arrange
            var lookupResult = new LookupResult("Test Title", "Released", contentId, "DOC123");

            // Act
            var result = lookupResult.GetDisplaySuffix();

            // Assert
            result.Should().Be(expectedSuffix);
        }

        [Theory]
        [InlineData("CMS-TEST-1234")] // Only 4 digits
        [InlineData("TSRC-NO-DIGITS")]
        [InlineData("")]
        [InlineData(null)]
        public void GetDisplaySuffix_InvalidContentId_ReturnsZeros(string contentId)
        {
            // Arrange
            var lookupResult = new LookupResult("Test Title", "Released", contentId, "DOC123");

            // Act
            var result = lookupResult.GetDisplaySuffix();

            // Assert
            result.Should().Be("000000");
        }
    }

    public class NeedsRepairTests
    {
        private readonly Mock<ILogger<HyperlinkRepairService>> _mockLogger;
        private readonly HyperlinkRepairService _service;

        public NeedsRepairTests()
        {
            _mockLogger = new Mock<ILogger<HyperlinkRepairService>>();
            _service = new HyperlinkRepairService(_mockLogger.Object);
        }

        [Fact]
        public void NeedsRepair_UrlAndDisplayTextMatch_ReturnsFalse()
        {
            // Arrange
            var metadata = new LookupResult("Test Document", "Released", "CMS-TEST-123456", "DOC123");
            var canonicalUrl = "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=DOC123";
            var displayText = "Test Document (123456)";
            var textRange = TextRange.ForSingleRun(0, 0, displayText);
            var hyperlink = HyperlinkRef.ForExternalLink("link1", "rId1", canonicalUrl, textRange);

            // Act
            var result = _service.NeedsRepair(hyperlink, metadata);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void NeedsRepair_WrongUrl_ReturnsTrue()
        {
            // Arrange
            var metadata = new LookupResult("Test Document", "Released", "CMS-TEST-123456", "DOC123");
            var wrongUrl = "http://old-site.com/document";
            var displayText = "Test Document (123456)";
            var textRange = TextRange.ForSingleRun(0, 0, displayText);
            var hyperlink = HyperlinkRef.ForExternalLink("link1", "rId1", wrongUrl, textRange);

            // Act
            var result = _service.NeedsRepair(hyperlink, metadata);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void NeedsRepair_MissingSuffix_ReturnsTrue()
        {
            // Arrange
            var metadata = new LookupResult("Test Document", "Released", "CMS-TEST-123456", "DOC123");
            var canonicalUrl = "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=DOC123";
            var displayText = "Test Document"; // Missing (123456) suffix
            var textRange = TextRange.ForSingleRun(0, 0, displayText);
            var hyperlink = HyperlinkRef.ForExternalLink("link1", "rId1", canonicalUrl, textRange);

            // Act
            var result = _service.NeedsRepair(hyperlink, metadata);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void NeedsRepair_WrongSuffix_ReturnsTrue()
        {
            // Arrange
            var metadata = new LookupResult("Test Document", "Released", "CMS-TEST-123456", "DOC123");
            var canonicalUrl = "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=DOC123";
            var displayText = "Test Document (999999)"; // Wrong suffix
            var textRange = TextRange.ForSingleRun(0, 0, displayText);
            var hyperlink = HyperlinkRef.ForExternalLink("link1", "rId1", canonicalUrl, textRange);

            // Act
            var result = _service.NeedsRepair(hyperlink, metadata);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void NeedsRepair_FiveDigitContentId_ZeroPadded_ReturnsCorrectSuffix()
        {
            // Arrange
            var metadata = new LookupResult("Test Document", "Released", "CMS-TEST-12345", "DOC123");
            var canonicalUrl = "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=DOC123";
            var displayText = "Test Document (012345)"; // Correctly zero-padded
            var textRange = TextRange.ForSingleRun(0, 0, displayText);
            var hyperlink = HyperlinkRef.ForExternalLink("link1", "rId1", canonicalUrl, textRange);

            // Act
            var result = _service.NeedsRepair(hyperlink, metadata);

            // Assert
            result.Should().BeFalse(); // Should not need repair
        }
    }

    public class TextRangeTests
    {
        [Fact]
        public void TextRange_ForSingleRun_CreatesCorrectRange()
        {
            // Arrange
            var text = "Test hyperlink text";
            var paragraphIndex = 1;
            var runIndex = 2;

            // Act
            var textRange = TextRange.ForSingleRun(paragraphIndex, runIndex, text);

            // Assert
            textRange.ParagraphIndex.Should().Be(paragraphIndex);
            textRange.StartRunIndex.Should().Be(runIndex);
            textRange.EndRunIndex.Should().Be(runIndex);
            textRange.StartCharOffset.Should().Be(0);
            textRange.EndCharOffset.Should().Be(text.Length);
            textRange.Text.Should().Be(text);
        }

        [Fact]
        public void TextRange_ForSingleRun_WithOffset_CreatesCorrectRange()
        {
            // Arrange
            var text = "Test hyperlink text";
            var paragraphIndex = 1;
            var runIndex = 2;
            var startOffset = 5;

            // Act
            var textRange = TextRange.ForSingleRun(paragraphIndex, runIndex, text, startOffset);

            // Assert
            textRange.ParagraphIndex.Should().Be(paragraphIndex);
            textRange.StartRunIndex.Should().Be(runIndex);
            textRange.EndRunIndex.Should().Be(runIndex);
            textRange.StartCharOffset.Should().Be(startOffset);
            textRange.EndCharOffset.Should().Be(text.Length);
            textRange.Text.Should().Be(text);
        }

        [Fact]
        public void TextRange_ForMultipleRuns_CreatesCorrectRange()
        {
            // Arrange
            var text = "Hyperlink text spanning multiple runs";
            var paragraphIndex = 0;
            var startRunIndex = 1;
            var endRunIndex = 3;
            var startCharOffset = 5;
            var endCharOffset = 10;

            // Act
            var textRange = TextRange.ForMultipleRuns(paragraphIndex, startRunIndex, endRunIndex, 
                text, startCharOffset, endCharOffset);

            // Assert
            textRange.ParagraphIndex.Should().Be(paragraphIndex);
            textRange.StartRunIndex.Should().Be(startRunIndex);
            textRange.EndRunIndex.Should().Be(endRunIndex);
            textRange.StartCharOffset.Should().Be(startCharOffset);
            textRange.EndCharOffset.Should().Be(endCharOffset);
            textRange.Text.Should().Be(text);
        }
    }

    public class HyperlinkRefTests
    {
        [Fact]
        public void HyperlinkRef_ForExternalLink_CreatesCorrectReference()
        {
            // Arrange
            var id = "link1";
            var relationshipId = "rId1";
            var target = "http://example.com";
            var textRange = TextRange.ForSingleRun(0, 0, "Test Link");

            // Act
            var hyperlinkRef = HyperlinkRef.ForExternalLink(id, relationshipId, target, textRange);

            // Assert
            hyperlinkRef.Id.Should().Be(id);
            hyperlinkRef.RelationshipId.Should().Be(relationshipId);
            hyperlinkRef.Anchor.Should().BeNull();
            hyperlinkRef.Target.Should().Be(target);
            hyperlinkRef.DisplayTextRange.Should().Be(textRange);
            hyperlinkRef.IsExternalLink.Should().BeTrue();
            hyperlinkRef.IsInternalAnchor.Should().BeFalse();
        }

        [Fact]
        public void HyperlinkRef_ForInternalAnchor_CreatesCorrectReference()
        {
            // Arrange
            var id = "anchor1";
            var anchor = "DocStart";
            var textRange = TextRange.ForSingleRun(0, 0, "Top of Document");

            // Act
            var hyperlinkRef = HyperlinkRef.ForInternalAnchor(id, anchor, textRange);

            // Assert
            hyperlinkRef.Id.Should().Be(id);
            hyperlinkRef.RelationshipId.Should().BeNull();
            hyperlinkRef.Anchor.Should().Be(anchor);
            hyperlinkRef.Target.Should().Be("#" + anchor);
            hyperlinkRef.DisplayTextRange.Should().Be(textRange);
            hyperlinkRef.IsExternalLink.Should().BeFalse();
            hyperlinkRef.IsInternalAnchor.Should().BeTrue();
        }
    }

    public class DisplayTextPatternsIntegrationTests
    {
        [Theory]
        [InlineData("Document Title", "CMS-TEST-123456", "Document Title (123456)")]
        [InlineData("Document Title  ", "CMS-TEST-123456", "Document Title (123456)")] // Trims whitespace
        [InlineData("Document Title (999999)", "CMS-TEST-123456", "Document Title (123456)")] // Replaces existing
        [InlineData("Document Title (0999999)", "CMS-TEST-123456", "Document Title (123456)")] // Replaces with leading zeros
        [InlineData("Document Title", "CMS-TEST-12345", "Document Title (012345)")] // Zero pads 5 digits
        public void GenerateExpectedDisplayText_VariousScenarios_ReturnsCorrectText(
            string originalText, string contentId, string expectedResult)
        {
            // Arrange
            var metadata = new LookupResult("Test", "Released", contentId, "DOC123");
            var expectedSuffix = $" ({metadata.GetDisplaySuffix()})";

            // Act
            string result;
            var existingSuffixMatch = HyperlinkPatterns.DisplaySuffixPattern.Match(originalText);
            
            if (existingSuffixMatch.Success)
            {
                // Replace existing suffix
                result = originalText.Substring(0, existingSuffixMatch.Index) + expectedSuffix;
            }
            else
            {
                // Append new suffix (trim trailing whitespace first)
                var trimmedText = originalText.TrimEnd();
                result = trimmedText + expectedSuffix;
            }

            // Assert
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("Document Title (123456)", "CMS-TEST-123456", true)]
        [InlineData("Document Title (012345)", "CMS-TEST-12345", true)]
        [InlineData("Document Title", "CMS-TEST-123456", false)]
        [InlineData("Document Title (999999)", "CMS-TEST-123456", false)]
        public void CheckIfSuffixMatches_VariousScenarios_ReturnsCorrectResult(
            string displayText, string contentId, bool shouldMatch)
        {
            // Arrange
            var metadata = new LookupResult("Test", "Released", contentId, "DOC123");
            var expectedSuffix = $" ({metadata.GetDisplaySuffix()})";

            // Act
            var result = displayText.EndsWith(expectedSuffix, StringComparison.Ordinal);

            // Assert
            result.Should().Be(shouldMatch);
        }
    }
}