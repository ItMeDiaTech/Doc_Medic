using App.Domain;

namespace App.Tests.Unit;

/// <summary>
/// Tests for regex patterns and URL processing logic as defined in CLAUDE.md Section 16.
/// Covers Document_ID extraction, Content_ID patterns, and edge cases.
/// </summary>
public class HyperlinkPatternsTests
{
    #region Document_ID Extraction Tests

    [Theory]
    [InlineData("http://example.com?docid=12345", "12345")]
    [InlineData("https://site.com?docid=ABC123DEF", "ABC123DEF")]
    [InlineData("http://test.com?docid=doc-with-dashes", "doc-with-dashes")]
    [InlineData("https://example.com?docid=document_with_underscores", "document_with_underscores")]
    [InlineData("http://site.com?docid=MixedCase123", "MixedCase123")]
    public void ExtractDocumentId_WithValidDocumentIds_ReturnsCorrectId(string url, string expectedId)
    {
        // Act
        var result = HyperlinkPatterns.ExtractDocumentId(url);

        // Assert
        result.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("http://example.com?docid=12345&param=value", "12345")]
    [InlineData("https://site.com?docid=ABC123#anchor", "ABC123")]
    [InlineData("http://test.com?docid=doc123&other=test&more=params", "doc123")]
    [InlineData("https://example.com?docid=document#section1", "document")]
    [InlineData("http://site.com?other=value&docid=extracted&final=param", "extracted")]
    public void ExtractDocumentId_WithDelimiters_ExtractsUpToDelimiter(string url, string expectedId)
    {
        // Act
        var result = HyperlinkPatterns.ExtractDocumentId(url);

        // Assert
        result.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("http://example.com?docid=doc%20with%20spaces", "doc%20with%20spaces")]
    [InlineData("https://site.com?docid=url%2Dencoded%2Dvalue", "url%2Dencoded%2Dvalue")]
    [InlineData("http://test.com?docid=special%21chars%40here", "special%21chars%40here")]
    public void ExtractDocumentId_WithUrlEncodedValues_ReturnsEncodedValue(string url, string expectedId)
    {
        // Act
        var result = HyperlinkPatterns.ExtractDocumentId(url);

        // Assert
        result.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://site.com?other=value")]
    [InlineData("http://test.com?doc=12345")]  // Wrong parameter name
    [InlineData("https://example.com?docids=12345")]  // Wrong parameter name (plural)
    [InlineData("")]
    [InlineData(null)]
    public void ExtractDocumentId_WithInvalidUrls_ReturnsNull(string url)
    {
        // Act
        var result = HyperlinkPatterns.ExtractDocumentId(url);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Content_ID Pattern Tests

    [Theory]
    [InlineData("CMS-1-123456", "CMS-1-123456")]
    [InlineData("TSRC-asd2121jkla-123456", "TSRC-asd2121jkla-123456")]
    [InlineData("CMS-ABC123-987654", "CMS-ABC123-987654")]
    [InlineData("TSRC-test-data-456789", "TSRC-test-data-456789")]
    [InlineData("CMS-a1b2c3-111111", "CMS-a1b2c3-111111")]
    public void ExtractContentId_WithValidContentIds_ReturnsCorrectId(string url, string expectedId)
    {
        // Act
        var result = HyperlinkPatterns.ExtractContentId(url);

        // Assert
        result.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("Check this CMS-1-123456 document", "CMS-1-123456")]
    [InlineData("Reference: TSRC-asd2121jkla-123456 for details", "TSRC-asd2121jkla-123456")]
    [InlineData("http://example.com/path/CMS-ABC123-987654/more", "CMS-ABC123-987654")]
    [InlineData("The document TSRC-test-data-456789 contains important info", "TSRC-test-data-456789")]
    public void ExtractContentId_FromTextContainingContentId_FindsPattern(string text, string expectedId)
    {
        // Act
        var result = HyperlinkPatterns.ExtractContentId(text);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedId);
        result.Should().MatchRegex(@"(CMS|TSRC)-[A-Za-z0-9\-]+-\d{6}");
    }

    [Theory]
    [InlineData("CMS-1-12345")]  // Only 5 digits
    [InlineData("TSRC-test-1234567")]  // 7 digits
    [InlineData("cms-test-123456")]  // Lowercase
    [InlineData("CMS_test_123456")]  // Underscores instead of dashes
    [InlineData("XMS-test-123456")]  // Wrong prefix
    [InlineData("CMS-123456")]  // Missing middle section
    [InlineData("")]
    [InlineData(null)]
    public void ExtractContentId_WithInvalidPatterns_ReturnsNull(string text)
    {
        // Act
        var result = HyperlinkPatterns.ExtractContentId(text);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region URL Eligibility Tests

    [Theory]
    [InlineData("http://example.com?docid=12345")]
    [InlineData("https://site.com/path?docid=ABC123")]
    [InlineData("http://test.com?other=value&docid=doc123")]
    [InlineData("http://example.com/CMS-1-123456/path")]
    [InlineData("https://site.com/content/TSRC-test-data-456789")]
    [InlineData("Check this CMS-ABC123-987654 reference")]
    public void IsEligibleUrl_WithValidPatterns_ReturnsTrue(string url)
    {
        // Act
        var result = HyperlinkPatterns.IsEligibleUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://site.com/path")]
    [InlineData("http://test.com?other=value")]
    [InlineData("mailto:test@example.com")]
    [InlineData("file:///C:/path/document.docx")]
    [InlineData("")]
    [InlineData(null)]
    public void IsEligibleUrl_WithInvalidPatterns_ReturnsFalse(string url)
    {
        // Act
        var result = HyperlinkPatterns.IsEligibleUrl(url);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Display Text Suffix Tests

    [Theory]
    [InlineData("CMS-1-123456", "123456")]
    [InlineData("TSRC-test-data-987654", "987654")]
    [InlineData("CMS-ABC123-111111", "111111")]
    public void Last6DigitsPattern_WithValidContentId_ExtractsLast6Digits(string contentId, string expectedDigits)
    {
        // Act
        var match = HyperlinkPatterns.Last6DigitsPattern.Match(contentId);

        // Assert
        match.Success.Should().BeTrue();
        match.Groups[1].Value.Should().Be(expectedDigits);
    }

    [Theory]
    [InlineData("CMS-1-12345", "12345")]  // 5 digits case
    [InlineData("TSRC-test-98765", "98765")]
    public void Last5DigitsPattern_WithFiveDigitSuffix_ExtractsLast5Digits(string contentId, string expectedDigits)
    {
        // Act
        var match = HyperlinkPatterns.Last5DigitsPattern.Match(contentId);

        // Assert
        match.Success.Should().BeTrue();
        match.Groups[1].Value.Should().Be(expectedDigits);
    }

    [Theory]
    [InlineData("Document Title (123456)", true)]
    [InlineData("Some Text (987654)", true)]
    [InlineData("Link Text (012345)", true)]  // With leading zero
    [InlineData("Title (000123)", true)]  // Multiple leading zeros
    [InlineData("Text  (123456)  ", true)]  // With surrounding whitespace
    public void DisplaySuffixPattern_WithExistingSuffix_MatchesCorrectly(string displayText, bool shouldMatch)
    {
        // Act
        var match = HyperlinkPatterns.DisplaySuffixPattern.IsMatch(displayText);

        // Assert
        match.Should().Be(shouldMatch);
    }

    [Theory]
    [InlineData("Document Title")]  // No suffix
    [InlineData("Some Text (12345)")]  // Only 5 digits, no leading zero
    [InlineData("Link Text (1234567)")]  // 7 digits
    [InlineData("Title [123456]")]  // Wrong brackets
    public void DisplaySuffixPattern_WithInvalidSuffix_DoesNotMatch(string displayText)
    {
        // Act
        var match = HyperlinkPatterns.DisplaySuffixPattern.IsMatch(displayText);

        // Assert
        match.Should().BeFalse();
    }

    #endregion

    #region Canonical URL Building Tests

    [Theory]
    [InlineData("12345", "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=12345")]
    [InlineData("ABC123DEF", "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=ABC123DEF")]
    [InlineData("document-with-dashes", "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=document-with-dashes")]
    public void BuildCanonicalUrl_WithValidDocumentId_ReturnsCorrectUrl(string documentId, string expectedUrl)
    {
        // Act
        var result = HyperlinkPatterns.BuildCanonicalUrl(documentId);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildCanonicalUrl_WithInvalidDocumentId_ThrowsArgumentException(string documentId)
    {
        // Act & Assert
        FluentActions.Invoking(() => HyperlinkPatterns.BuildCanonicalUrl(documentId))
            .Should().Throw<ArgumentException>()
            .WithParameterName("documentId");
    }

    #endregion

    #region Space Normalization Tests

    [Theory]
    [InlineData("Text  with  double  spaces", "Text with double spaces")]
    [InlineData("Multiple   spaces    here", "Multiple spaces here")]
    [InlineData("Text\t\twith\t\ttabs", "Text\t\twith\t\ttabs")]  // Should not affect tabs
    [InlineData("Normal text", "Normal text")]  // Single spaces unchanged
    [InlineData("", "")]  // Empty string
    public void MultipleSpacesPattern_WhenReplaced_CollapsesToSingleSpaces(string input, string expectedOutput)
    {
        // Act
        var result = HyperlinkPatterns.MultipleSpacesPattern.Replace(input, " ");

        // Assert
        result.Should().Be(expectedOutput);
    }

    #endregion

    #region Top of Document Pattern Tests

    [Theory]
    [InlineData("Top of Document")]
    [InlineData("top of document")]
    [InlineData("TOP OF DOCUMENT")]
    [InlineData("Top Of Document")]
    [InlineData("  Top of Document  ")]  // With whitespace
    [InlineData("Top  of   Document")]   // Multiple spaces between words
    public void TopOfDocumentPattern_WithValidText_Matches(string text)
    {
        // Act
        var match = HyperlinkPatterns.TopOfDocumentPattern.IsMatch(text);

        // Assert
        match.Should().BeTrue();
    }

    [Theory]
    [InlineData("Go to Top of Document")]  // Extra text at beginning
    [InlineData("Top of Document link")]   // Extra text at end
    [InlineData("Top of Doc")]             // Abbreviated
    [InlineData("Document Top")]           // Different wording
    [InlineData("")]                       // Empty string
    public void TopOfDocumentPattern_WithInvalidText_DoesNotMatch(string text)
    {
        // Act
        var match = HyperlinkPatterns.TopOfDocumentPattern.IsMatch(text);

        // Assert
        match.Should().BeFalse();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void DocumentIdPattern_IsCaseInsensitive()
    {
        // Arrange
        var urls = new[]
        {
            "http://example.com?DOCID=test123",
            "http://example.com?DocId=test123",
            "http://example.com?docid=test123"
        };

        // Act & Assert
        foreach (var url in urls)
        {
            var result = HyperlinkPatterns.ExtractDocumentId(url);
            result.Should().Be("test123", $"URL pattern should be case insensitive for: {url}");
        }
    }

    [Fact]
    public void ContentIdPattern_IsCaseSensitive()
    {
        // Act & Assert
        HyperlinkPatterns.ExtractContentId("CMS-test-123456").Should().NotBeNull();
        HyperlinkPatterns.ExtractContentId("cms-test-123456").Should().BeNull();
        HyperlinkPatterns.ExtractContentId("TSRC-test-123456").Should().NotBeNull();
        HyperlinkPatterns.ExtractContentId("tsrc-test-123456").Should().BeNull();
    }

    [Theory]
    [InlineData("http://example.com?docid=", "")]  // Empty docid value
    [InlineData("http://example.com?docid=&other=value", "")]  // Empty docid with other params
    public void ExtractDocumentId_WithEmptyDocumentId_ReturnsEmptyString(string url, string expectedResult)
    {
        // Act
        var result = HyperlinkPatterns.ExtractDocumentId(url);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ContainsDocumentId_IsCaseInsensitive()
    {
        // Arrange
        var testCases = new[]
        {
            ("http://example.com?DOCID=123", true),
            ("http://example.com?DocId=123", true),
            ("http://example.com?docid=123", true),
            ("http://example.com?doc=123", false)
        };

        // Act & Assert
        foreach (var (url, expected) in testCases)
        {
            HyperlinkPatterns.ContainsDocumentId(url).Should().Be(expected);
        }
    }

    #endregion
}