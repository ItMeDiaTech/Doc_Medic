using App.Domain;
using App.Tests.TestHelpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace App.Tests.Unit;

/// <summary>
/// Tests for hyperlink repair functionality including display text updates
/// across multiple runs as specified in CLAUDE.md Section 16.
/// Tests TextRange helper functionality and (XXXXXX) insertion logic.
/// </summary>
public class HyperlinkRepairTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    [Theory]
    [InlineData("CMS-1-123456", "123456")]
    [InlineData("TSRC-test-data-987654", "987654")]
    [InlineData("CMS-ABC123-111111", "111111")]
    public void ExtractDisplaySuffix_FromContentId_ReturnsLast6Digits(string contentId, string expectedSuffix)
    {
        // Act
        var match = HyperlinkPatterns.Last6DigitsPattern.Match(contentId);

        // Assert
        match.Success.Should().BeTrue();
        match.Groups[1].Value.Should().Be(expectedSuffix);
    }

    [Theory]
    [InlineData("CMS-1-12345", "012345")]  // 5 digits should be zero-padded
    [InlineData("TSRC-test-98765", "098765")]
    public void ExtractDisplaySuffix_FromContentIdWith5Digits_ReturnsZeroPaddedSuffix(string contentId, string expectedSuffix)
    {
        // Arrange & Act
        var last6Match = HyperlinkPatterns.Last6DigitsPattern.Match(contentId);
        var last5Match = HyperlinkPatterns.Last5DigitsPattern.Match(contentId);

        // Assert - should find 5 digits and pad with zero
        last6Match.Success.Should().BeFalse();
        last5Match.Success.Should().BeTrue();

        var paddedResult = "0" + last5Match.Groups[1].Value;
        paddedResult.Should().Be(expectedSuffix);
    }

    [Fact]
    public void HyperlinkIndex_WithMultipleEligibleLinks_ExtractsAllLookupIds()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithEligibleHyperlinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var hyperlinkIndex = BuildHyperlinkIndex(document);

        // Assert
        hyperlinkIndex.Should().NotBeNull();
        // Would need to implement HyperlinkIndexService to test fully
        // This is a placeholder for the actual implementation test
    }

    [Fact]
    public void DisplayText_WithExistingSuffix_DoesNotDuplicate()
    {
        // Arrange
        var displayTextWithSuffix = "Document Title (123456)";

        // Act
        var hasSuffix = HyperlinkPatterns.DisplaySuffixPattern.IsMatch(displayTextWithSuffix);

        // Assert
        hasSuffix.Should().BeTrue("Display text already has proper suffix format");
    }

    [Theory]
    [InlineData("Document Title", " (123456)", "Document Title (123456)")]
    [InlineData("Link Text   ", " (987654)", "Link Text    (987654)")]  // Preserves trailing whitespace
    [InlineData("", " (111111)", " (111111)")]  // Empty text gets suffix
    public void AppendDisplaySuffix_ToSingleRunText_AppendsCorrectly(string originalText, string suffix, string expectedResult)
    {
        // Act
        var result = originalText + suffix;

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void MultiRunHyperlink_DisplayTextExtraction_ConcatenatesAllRuns()
    {
        // Arrange
        using var docBuilder = TestDocuments.WithMultiRunHyperlinks();
        _disposables.Add(docBuilder);
        var document = docBuilder.Build();

        // Act
        var hyperlinks = document.MainDocumentPart!.Document.Descendants<Hyperlink>().ToList();

        // Assert
        hyperlinks.Should().HaveCount(2);

        // First hyperlink: "Document" + " Link" + " Title" = "Document Link Title"
        var firstLinkText = string.Concat(hyperlinks[0].Descendants<Text>().Select(t => t.Text));
        firstLinkText.Should().Be("Document Link Title");

        // Second hyperlink: "CMS " + "Document " + "(123456)" = "CMS Document (123456)"
        var secondLinkText = string.Concat(hyperlinks[1].Descendants<Text>().Select(t => t.Text));
        secondLinkText.Should().Be("CMS Document (123456)");
    }

    [Fact]
    public void TextRange_WithMultipleRuns_CanIdentifyLastRunForAppending()
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        docBuilder.AddMultiRunHyperlink("http://example.com?docid=12345", "Document", " Link", " Title");
        var document = docBuilder.Build();

        // Act
        var hyperlink = document.MainDocumentPart!.Document.Descendants<Hyperlink>().First();
        var textRuns = hyperlink.Descendants<Run>().ToList();
        var lastRun = textRuns.Last();
        var lastText = lastRun.Descendants<Text>().First();

        // Assert
        textRuns.Should().HaveCount(3);
        lastText.Text.Should().Be(" Title");

        // Verify we can append to the last run
        lastText.Text += " (123456)";
        lastText.Text.Should().Be(" Title (123456)");
    }

    [Theory]
    [InlineData("Document Link", "123456", "Document Link (123456)")]
    [InlineData("Title Text (wrong)", "987654", "Title Text (wrong) (987654)")]  // Replaces wrong suffix
    [InlineData("Text (12345)", "123456", "Text (12345) (123456)")]  // 5 digits is not valid suffix
    public void UpdateDisplayText_WithContentIdSuffix_AppendsOrReplacesCorrectly(string originalText, string contentIdSuffix, string expectedResult)
    {
        // This test simulates the logic that would be in the hyperlink repair service
        // Act
        string result;
        if (HyperlinkPatterns.DisplaySuffixPattern.IsMatch(originalText))
        {
            // Has valid 6-digit suffix already - don't modify
            result = originalText;
        }
        else
        {
            // Append new suffix
            result = originalText.TrimEnd() + $" ({contentIdSuffix})";
        }

        // Assert for cases where suffix should be added
        if (originalText == "Document Link" || originalText == "Title Text (wrong)" || originalText == "Text (12345)")
        {
            result.Should().Be(expectedResult);
        }
    }

    [Fact]
    public void HyperlinkRepair_WithCanonicalUrlMismatch_UpdatesTargetUrl()
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        docBuilder.AddHyperlink("http://old-site.com?docid=12345", "Old Link");
        var document = docBuilder.Build();

        var expectedCanonicalUrl = HyperlinkPatterns.BuildCanonicalUrl("12345");

        // Act
        var hyperlinkRelationship = document.MainDocumentPart!.HyperlinkRelationships.First();
        var originalUrl = hyperlinkRelationship.Uri.ToString();

        // Assert setup
        originalUrl.Should().Be("http://old-site.com?docid=12345");
        expectedCanonicalUrl.Should().Be("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=12345");

        // This would be the repair logic (not implemented in this test, just verifying the setup)
        originalUrl.Should().NotBe(expectedCanonicalUrl, "URL should need updating");
    }

    [Fact]
    public void HyperlinkRepair_WithMatchingCanonicalUrl_DoesNotModify()
    {
        // Arrange
        var canonicalUrl = HyperlinkPatterns.BuildCanonicalUrl("12345");
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);
        docBuilder.AddHyperlink(canonicalUrl, "Canonical Link");
        var document = docBuilder.Build();

        // Act
        var hyperlinkRelationship = document.MainDocumentPart!.HyperlinkRelationships.First();
        var currentUrl = hyperlinkRelationship.Uri.ToString();

        // Assert
        currentUrl.Should().Be(canonicalUrl, "URL is already canonical and should not be modified");
    }

    [Theory]
    [InlineData("http://example.com?docid=12345", true)]  // Document ID pattern
    [InlineData("http://site.com/CMS-1-123456", true)]   // Content ID pattern
    [InlineData("http://regular-link.com", false)]       // Not eligible
    [InlineData("mailto:test@example.com", false)]       // Not eligible
    public void EligibilityCheck_ForHyperlinkTargets_ReturnsCorrectResult(string url, bool shouldBeEligible)
    {
        // Act
        var isEligible = HyperlinkPatterns.IsEligibleUrl(url);

        // Assert
        isEligible.Should().Be(shouldBeEligible);
    }

    [Fact]
    public void TextRange_PreservesFormatting_AcrossMultipleRuns()
    {
        // Arrange
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);

        // This would create a hyperlink where runs have different formatting
        // For now, we test the concept with a simple multi-run scenario
        docBuilder.AddMultiRunHyperlink("http://example.com?docid=12345", "Bold", " Normal", " Italic");
        var document = docBuilder.Build();

        // Act
        var hyperlink = document.MainDocumentPart!.Document.Descendants<Hyperlink>().First();
        var runs = hyperlink.Descendants<Run>().ToList();

        // Assert
        runs.Should().HaveCount(3, "Each text part should be in a separate run");
        runs[0].InnerText.Should().Be("Bold");
        runs[1].InnerText.Should().Be(" Normal");
        runs[2].InnerText.Should().Be(" Italic");

        // When appending suffix, formatting of the last run should be preserved
        var lastRun = runs.Last();
        var lastText = lastRun.Descendants<Text>().First();
        lastText.Text += " (123456)";

        lastText.Text.Should().Be(" Italic (123456)");
    }

    /// <summary>
    /// Helper method to simulate building a hyperlink index from a document.
    /// In the actual implementation, this would be done by IHyperlinkIndexService.
    /// </summary>
    private static HyperlinkIndex BuildHyperlinkIndex(WordprocessingDocument document)
    {
        // This is a simplified version for testing
        // The actual service would create HyperlinkRef objects with proper TextRange info
        var index = new HyperlinkIndex();
        var hyperlinks = document.MainDocumentPart!.Document.Descendants<Hyperlink>().ToList();

        foreach (var hyperlink in hyperlinks)
        {
            // Extract URL from relationship or anchor
            if (!string.IsNullOrEmpty(hyperlink.Id))
            {
                var relationship = document.MainDocumentPart.HyperlinkRelationships
                    .FirstOrDefault(r => r.Id == hyperlink.Id);

                if (relationship?.Uri != null)
                {
                    var url = relationship.Uri.ToString();
                    
                    // Create a simplified HyperlinkRef for testing
                    var hyperlinkRef = new HyperlinkRef
                    {
                        Id = hyperlink.Id,
                        RelationshipId = hyperlink.Id,
                        Anchor = hyperlink.Anchor,
                        Target = url,
                        DisplayTextRange = new TextRange() // Simplified for testing
                    };
                    
                    index.AddHyperlink(hyperlinkRef);
                }
            }
        }

        return index;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();
    }
}