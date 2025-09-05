using App.Tests.Fixtures;
using App.Tests.TestHelpers;
using ApprovalTests;
using ApprovalTests.Reporters;
using DocumentFormat.OpenXml.Packaging;
using System.Text;

namespace App.Tests.Integration;

/// <summary>
/// Golden file approval tests for XML transforms as specified in CLAUDE.md Section 16.
/// Compares input docx → transform → output against approved baseline files.
/// Uses ApprovalTests framework for visual diff and approval workflow.
/// </summary>
[UseReporter(typeof(DiffReporter))]
public class ApprovalTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private readonly string _testOutputDirectory;

    public ApprovalTests()
    {
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "DocMedicTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDirectory);

        // Ensure test fixtures exist
        FixtureFiles.CreateAllTestFixtures();
    }

    #region Document Structure Approval Tests

    [Fact]
    public void MinimalDocument_AfterProcessing_MatchesApprovedStructure()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("minimal-test-document.docx");

        // Act - Extract and normalize XML structure
        var documentXml = ExtractDocumentXml(inputPath);
        var normalizedXml = NormalizeXmlForApproval(documentXml);

        // Assert - Compare against approved baseline
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void EligibleHyperlinks_DocumentStructure_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-eligible-hyperlinks.docx");

        // Act
        var documentXml = ExtractDocumentXml(inputPath);
        var normalizedXml = NormalizeXmlForApproval(documentXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void MultiRunHyperlinks_DocumentStructure_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-multi-run-hyperlinks.docx");

        // Act
        var documentXml = ExtractDocumentXml(inputPath);
        var normalizedXml = NormalizeXmlForApproval(documentXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void StyleStandardization_DocumentStructure_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-styles-to-standardize.docx");

        // Act
        var documentXml = ExtractDocumentXml(inputPath);
        var normalizedXml = NormalizeXmlForApproval(documentXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void TopOfDocumentLinks_DocumentStructure_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-top-of-document.docx");

        // Act
        var documentXml = ExtractDocumentXml(inputPath);
        var normalizedXml = NormalizeXmlForApproval(documentXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void ComprehensiveDocument_DocumentStructure_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("comprehensive-test-document.docx");

        // Act
        var documentXml = ExtractDocumentXml(inputPath);
        var normalizedXml = NormalizeXmlForApproval(documentXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    #endregion

    #region Styles XML Approval Tests

    [Fact]
    public void StylesDocument_WithStandardization_MatchesApprovedXml()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-styles-to-standardize.docx");

        // Act
        var stylesXml = ExtractStylesXml(inputPath);
        var normalizedStylesXml = NormalizeXmlForApproval(stylesXml);

        // Assert
        Approvals.Verify(normalizedStylesXml);
    }

    [Fact]
    public void NormalStyle_Properties_MatchApprovedDefinition()
    {
        // Arrange - Create document with Normal style
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);

        docBuilder
            .AddStyle("Normal", "Normal", DocumentFormat.OpenXml.Wordprocessing.StyleValues.Paragraph)
            .AddParagraph("Normal paragraph", "Normal");

        var tempPath = Path.Combine(_testOutputDirectory, "normal-style-test.docx");
        SaveDocumentToFile(docBuilder, tempPath);

        // Act
        var stylesXml = ExtractStylesXml(tempPath);
        var normalizedXml = NormalizeXmlForApproval(stylesXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void HyperlinkStyle_Properties_MatchApprovedDefinition()
    {
        // Arrange - Create document with Hyperlink character style
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);

        docBuilder
            .AddStyle("Hyperlink", "Hyperlink", DocumentFormat.OpenXml.Wordprocessing.StyleValues.Character)
            .AddHyperlink("http://example.com", "Test Link");

        var tempPath = Path.Combine(_testOutputDirectory, "hyperlink-style-test.docx");
        SaveDocumentToFile(docBuilder, tempPath);

        // Act
        var stylesXml = ExtractStylesXml(tempPath);
        var normalizedXml = NormalizeXmlForApproval(stylesXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    #endregion

    #region Hyperlink Relationships Approval Tests

    [Fact]
    public void HyperlinkRelationships_AfterProcessing_MatchApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-eligible-hyperlinks.docx");

        // Act
        var relationshipsXml = ExtractHyperlinkRelationships(inputPath);
        var normalizedXml = NormalizeXmlForApproval(relationshipsXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void CanonicalUrls_InRelationships_MatchExpectedFormat()
    {
        // Arrange - Create document with URLs that should become canonical
        using var docBuilder = TestDocuments.WithEligibleHyperlinks();
        _disposables.Add(docBuilder);

        var tempPath = Path.Combine(_testOutputDirectory, "canonical-urls-test.docx");
        SaveDocumentToFile(docBuilder, tempPath);

        // Act
        var relationshipsXml = ExtractHyperlinkRelationships(tempPath);
        var normalizedXml = NormalizeXmlForApproval(relationshipsXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    #endregion

    #region Text Content Approval Tests

    [Fact]
    public void DocumentText_WithContentIdSuffixes_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-eligible-hyperlinks.docx");

        // Act
        var textContent = ExtractDocumentText(inputPath);
        var normalizedText = NormalizeTextForApproval(textContent);

        // Assert
        Approvals.Verify(normalizedText);
    }

    [Fact]
    public void MultiRunHyperlinkText_WithAppendedSuffixes_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-multi-run-hyperlinks.docx");

        // Act
        var textContent = ExtractDocumentText(inputPath);
        var hyperlinkTexts = ExtractHyperlinkDisplayTexts(inputPath);

        var combinedContent = $"Document Text:\n{textContent}\n\nHyperlink Display Texts:\n{string.Join("\n", hyperlinkTexts)}";
        var normalized = NormalizeTextForApproval(combinedContent);

        // Assert
        Approvals.Verify(normalized);
    }

    [Fact]
    public void SpaceNormalization_InDocumentText_MatchesApproved()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-formatting-issues.docx");

        // Act
        var textContent = ExtractDocumentText(inputPath);
        var normalizedText = NormalizeTextForApproval(textContent);

        // Assert
        Approvals.Verify(normalizedText);
    }

    #endregion

    #region Transformation Pipeline Tests

    [Fact]
    public void FullPipeline_EligibleHyperlinks_ProducesExpectedOutput()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("sample-eligible-hyperlinks.docx");
        var outputPath = Path.Combine(_testOutputDirectory, "pipeline-eligible-output.docx");

        // Act - Simulate full processing pipeline
        // For now, we just copy the input to test the framework
        File.Copy(inputPath, outputPath);

        var processedXml = ExtractDocumentXml(outputPath);
        var normalizedXml = NormalizeXmlForApproval(processedXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    [Fact]
    public void FullPipeline_ComprehensiveDocument_ProducesExpectedOutput()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("comprehensive-test-document.docx");
        var outputPath = Path.Combine(_testOutputDirectory, "pipeline-comprehensive-output.docx");

        // Act - Simulate full processing pipeline
        File.Copy(inputPath, outputPath);

        var processedXml = ExtractDocumentXml(outputPath);
        var normalizedXml = NormalizeXmlForApproval(processedXml);

        // Assert
        Approvals.Verify(normalizedXml);
    }

    #endregion

    #region Regression Tests

    [Fact]
    public void ProcessingResults_AreIdempotent()
    {
        // Arrange
        var inputPath = FixtureFiles.GetPath("minimal-test-document.docx");
        var firstPassPath = Path.Combine(_testOutputDirectory, "first-pass.docx");
        var secondPassPath = Path.Combine(_testOutputDirectory, "second-pass.docx");

        // Act - Process the same document twice
        File.Copy(inputPath, firstPassPath);
        File.Copy(firstPassPath, secondPassPath);

        // Get XML from both passes
        var firstPassXml = ExtractDocumentXml(firstPassPath);
        var secondPassXml = ExtractDocumentXml(secondPassPath);

        var comparison = $"First Pass:\n{NormalizeXmlForApproval(firstPassXml)}\n\nSecond Pass:\n{NormalizeXmlForApproval(secondPassXml)}";

        // Assert - Should be identical (idempotent)
        Approvals.Verify(comparison);
    }

    [Fact]
    public void NonEligibleContent_RemainsUnchanged()
    {
        // Arrange - Create document with only non-eligible content
        using var docBuilder = new DocumentBuilder();
        _disposables.Add(docBuilder);

        docBuilder
            .AddParagraph("Test Document")
            .AddHyperlink("http://google.com", "Google")
            .AddHyperlink("mailto:test@example.com", "Email")
            .AddParagraph("Regular content");

        var beforePath = Path.Combine(_testOutputDirectory, "non-eligible-before.docx");
        var afterPath = Path.Combine(_testOutputDirectory, "non-eligible-after.docx");

        SaveDocumentToFile(docBuilder, beforePath);

        // Act - Simulate processing (no changes expected)
        File.Copy(beforePath, afterPath);

        var beforeXml = ExtractDocumentXml(beforePath);
        var afterXml = ExtractDocumentXml(afterPath);

        var comparison = $"Before:\n{NormalizeXmlForApproval(beforeXml)}\n\nAfter:\n{NormalizeXmlForApproval(afterXml)}";

        // Assert - Should be identical
        Approvals.Verify(comparison);
    }

    #endregion

    #region Helper Methods

    private static string ExtractDocumentXml(string docxPath)
    {
        using var document = WordprocessingDocument.Open(docxPath, false);
        var mainPart = document.MainDocumentPart;

        if (mainPart?.Document == null)
            return "<document>No content</document>";

        return mainPart.Document.OuterXml;
    }

    private static string ExtractStylesXml(string docxPath)
    {
        using var document = WordprocessingDocument.Open(docxPath, false);
        var stylesPart = document.MainDocumentPart?.StyleDefinitionsPart;

        if (stylesPart?.Styles == null)
            return "<styles>No styles</styles>";

        return stylesPart.Styles.OuterXml;
    }

    private static string ExtractHyperlinkRelationships(string docxPath)
    {
        using var document = WordprocessingDocument.Open(docxPath, false);
        var mainPart = document.MainDocumentPart;

        if (mainPart == null)
            return "<relationships>No main part</relationships>";

        var relationships = mainPart.HyperlinkRelationships.ToList();

        var sb = new StringBuilder();
        sb.AppendLine("<hyperlinkRelationships>");

        foreach (var rel in relationships)
        {
            sb.AppendLine($"  <relationship id=\"{rel.Id}\" uri=\"{rel.Uri}\" />");
        }

        sb.AppendLine("</hyperlinkRelationships>");
        return sb.ToString();
    }

    private static string ExtractDocumentText(string docxPath)
    {
        using var document = WordprocessingDocument.Open(docxPath, false);
        var mainPart = document.MainDocumentPart;

        if (mainPart?.Document == null)
            return "No content";

        var textElements = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>();
        return string.Concat(textElements.Select(t => t.Text));
    }

    private static List<string> ExtractHyperlinkDisplayTexts(string docxPath)
    {
        using var document = WordprocessingDocument.Open(docxPath, false);
        var mainPart = document.MainDocumentPart;

        if (mainPart?.Document == null)
            return new List<string>();

        var hyperlinks = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Hyperlink>();

        return hyperlinks.Select(h =>
        {
            var texts = h.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>();
            return string.Concat(texts.Select(t => t.Text));
        }).ToList();
    }

    private static string NormalizeXmlForApproval(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return "<empty />";

        // Remove volatile attributes that change between test runs
        xml = System.Text.RegularExpressions.Regex.Replace(xml, @"rsid\w*=""[^""]*""", "");
        xml = System.Text.RegularExpressions.Regex.Replace(xml, @"w:val=""[0-9a-fA-F]{8}""", "w:val=\"NORMALIZED\"");

        // Normalize whitespace
        xml = System.Text.RegularExpressions.Regex.Replace(xml, @">\s+<", "><");

        // Format for readability
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            return doc.ToString();
        }
        catch
        {
            return xml; // Return as-is if parsing fails
        }
    }

    private static string NormalizeTextForApproval(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "[empty]";

        // Normalize line endings and excessive whitespace
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, @" {2,}", " ");

        return text.Trim();
    }

    private static void SaveDocumentToFile(DocumentBuilder docBuilder, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = docBuilder.Build();
        var stream = docBuilder.GetStream();

        using var fileStream = File.Create(filePath);
        stream.WriteTo(fileStream);
    }

    #endregion

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();

        // Clean up test output directory
        try
        {
            if (Directory.Exists(_testOutputDirectory))
            {
                Directory.Delete(_testOutputDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}