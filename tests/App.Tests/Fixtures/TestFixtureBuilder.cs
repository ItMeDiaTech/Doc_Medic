using App.Tests.TestHelpers;
using DocumentFormat.OpenXml.Wordprocessing;

namespace App.Tests.Fixtures;

/// <summary>
/// Creates sample .docx test fixtures for integration testing.
/// Builds realistic documents with various scenarios for testing the complete pipeline.
/// </summary>
public static class TestFixtureBuilder
{
    /// <summary>
    /// Creates a sample document with eligible hyperlinks for API testing.
    /// Contains Document_ID and Content_ID patterns that should be processed.
    /// </summary>
    public static void CreateSampleWithEligibleHyperlinks(string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        // Add document content with eligible hyperlinks
        docBuilder
            .AddParagraph("Document Processing Test", "Heading1")
            .AddParagraph("This document contains hyperlinks that should be processed by Doc_Medic.")
            .AddParagraph("")
            
            // Document ID hyperlinks (should be processed)
            .AddHyperlink("http://old-system.com?docid=DOC-12345", "Legacy Document Link")
            .AddHyperlink("https://another-site.org/page?docid=REPORT-67890&param=value", "Report Reference")
            .AddHyperlink("http://example.com/path?other=test&docid=FORM-ABC123#section", "Form Document")
            
            .AddParagraph("")
            .AddParagraph("Content ID References:", "Heading2")
            
            // Content ID hyperlinks (should be processed)
            .AddHyperlink("http://cms.example.com/CMS-Project1-123456/view", "CMS Document (wrong suffix)")
            .AddHyperlink("https://tsrc.company.com/docs/TSRC-DataAnalysis-987654", "TSRC Analysis Report")
            .AddHyperlink("http://site.com/path/CMS-UserGuide-111222", "User Guide Reference")
            
            .AddParagraph("")
            .AddParagraph("Non-eligible Links:", "Heading2")
            
            // Non-eligible hyperlinks (should be ignored)
            .AddHyperlink("http://google.com", "Google Search")
            .AddHyperlink("mailto:support@company.com", "Email Support")
            .AddHyperlink("https://docs.microsoft.com/office", "Microsoft Office Docs")
            
            .AddParagraph("")
            .AddParagraph("Mixed Content with multiple spaces  and   formatting   issues.")
            .AddParagraph("")
            .AddInternalHyperlink("#top", "Top of Document")
            .AddParagraph("")
            .AddCenteredImage("sample-chart");
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    /// <summary>
    /// Creates a document with multi-run hyperlink display text for TextRange testing.
    /// </summary>
    public static void CreateSampleWithMultiRunHyperlinks(string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        docBuilder
            .AddParagraph("Multi-Run Hyperlink Test", "Heading1")
            .AddParagraph("This document tests hyperlinks where display text spans multiple runs.")
            .AddParagraph("")
            
            // Multi-run hyperlinks with complex formatting
            .AddMultiRunHyperlink("http://example.com?docid=SPLIT-12345", 
                "Document", " Title", " Split", " Across", " Runs")
            .AddMultiRunHyperlink("http://site.com/CMS-Complex-987654/path",
                "CMS ", "Document ", "With ", "Multiple ", "Runs")
            .AddMultiRunHyperlink("https://test.com/TSRC-Analysis-456789",
                "TSRC", " ", "Analysis", " (", "existing suffix", ")")
            
            .AddParagraph("")
            .AddParagraph("These links test the TextRange functionality for appending Content_ID suffixes.")
            .AddInternalHyperlink("#start", "top of document");
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    /// <summary>
    /// Creates a document with styles that need standardization.
    /// </summary>
    public static void CreateSampleWithStylesToStandardize(string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        // Add styles that need to be updated
        docBuilder
            .AddStyle("Normal", "Normal", StyleValues.Paragraph)
            .AddStyle("Heading1", "Heading 1", StyleValues.Paragraph) 
            .AddStyle("Heading2", "Heading 2", StyleValues.Paragraph)
            .AddStyle("Hyperlink", "Hyperlink", StyleValues.Character)
            
            .AddParagraph("Style Standardization Test", "Heading1")
            .AddParagraph("This document tests style standardization according to CLAUDE.md spec.", "Normal")
            .AddParagraph("")
            
            .AddParagraph("Section Header", "Heading2")
            .AddParagraph("Normal paragraph with correct formatting should be preserved.", "Normal")
            .AddParagraph("Another normal paragraph with multiple   spaces   to    normalize.", "Normal")
            
            .AddParagraph("")
            .AddParagraph("Subsection", "Heading2")
            .AddParagraph("Paragraph with hyperlinks that should get character style.", "Normal")
            
            .AddHyperlink("http://example.com?docid=STYLE-12345", "Styled Hyperlink")
            
            .AddParagraph("")
            .AddCenteredImage("chart-to-center")
            .AddParagraph("Final paragraph.", "Normal")
            .AddInternalHyperlink("#beginning", "Top of Document");
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    /// <summary>
    /// Creates a document with "Top of Document" links in various formats.
    /// </summary>
    public static void CreateSampleWithTopOfDocumentLinks(string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        docBuilder
            .AddBookmark("DocStart", "Document Start")
            .AddParagraph("Top of Document Test", "Heading1")
            .AddParagraph("This document tests Top of Document link processing.")
            .AddParagraph("")
            
            .AddParagraph("Section 1", "Heading2")
            .AddParagraph("Content goes here...")
            .AddInternalHyperlink("DocStart", "Top of Document")
            .AddParagraph("")
            
            .AddParagraph("Section 2", "Heading2") 
            .AddParagraph("More content...")
            .AddInternalHyperlink("DocStart", "top of document")
            .AddParagraph("")
            
            .AddParagraph("Section 3", "Heading2")
            .AddParagraph("Even more content...")
            .AddInternalHyperlink("DocStart", "TOP OF DOCUMENT")
            .AddParagraph("")
            
            .AddParagraph("Section 4", "Heading2")
            .AddParagraph("Final section...")
            .AddInternalHyperlink("DocStart", "  Top of Document  ");
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    /// <summary>
    /// Creates a document with formatting issues that need cleanup.
    /// </summary>
    public static void CreateSampleWithFormattingIssues(string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        docBuilder
            .AddParagraph("Formatting Issues Test", "Heading1")
            .AddParagraph("This document contains various formatting problems.")
            .AddParagraph("")
            
            // Multiple spaces
            .AddParagraph("Paragraph  with   multiple    spaces     everywhere.")
            .AddParagraph("Another   paragraph  with    spacing   issues.")
            .AddParagraph("")
            
            // Images that need centering
            .AddCenteredImage("uncentered-image-1")
            .AddParagraph("Content between images.")
            .AddCenteredImage("uncentered-image-2")
            .AddParagraph("")
            
            // Mixed hyperlink issues
            .AddHyperlink("http://old.com?docid=FIX-12345", "Link needing URL fix")
            .AddHyperlink("http://site.com/CMS-Format-987654/path", "Link needing suffix (wrong)")
            .AddParagraph("")
            
            .AddInternalHyperlink("#top", "top of document")
            .AddInternalHyperlink("#top", "Top   of    Document");
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    /// <summary>
    /// Creates a comprehensive test document with all processing scenarios.
    /// </summary>
    public static void CreateComprehensiveTestDocument(string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        docBuilder
            .AddBookmark("DocStart")
            .AddStyle("Normal", "Normal", StyleValues.Paragraph)
            .AddStyle("Heading1", "Heading 1", StyleValues.Paragraph)
            .AddStyle("Heading2", "Heading 2", StyleValues.Paragraph)
            .AddStyle("Hyperlink", "Hyperlink", StyleValues.Character)
            
            .AddParagraph("Comprehensive Doc_Medic Test Document", "Heading1")
            .AddParagraph("This document tests all Doc_Medic processing capabilities in a single file.", "Normal")
            .AddParagraph("")
            
            // Eligible hyperlinks section
            .AddParagraph("Eligible Hyperlinks", "Heading2")
            .AddHyperlink("http://legacy.com?docid=DOC-COMP-001", "Legacy Document")
            .AddHyperlink("https://system.org/path?docid=REPORT-COMP-002&other=param", "System Report")
            .AddHyperlink("http://cms.com/CMS-Comprehensive-123456/view", "CMS Document (123456)")
            .AddHyperlink("https://tsrc.com/TSRC-TestData-987654", "TSRC Test Data")
            .AddParagraph("")
            
            // Multi-run hyperlinks
            .AddParagraph("Multi-Run Display Text", "Heading2")
            .AddMultiRunHyperlink("http://multi.com?docid=MULTI-001", 
                "Multi", " Run", " Document", " Link")
            .AddMultiRunHyperlink("http://split.com/CMS-Split-456789/path",
                "CMS ", "Split ", "Link ", "(wrong suffix)")
            .AddParagraph("")
            
            // Formatting issues
            .AddParagraph("Formatting Issues", "Heading2") 
            .AddParagraph("Text  with   multiple    spaces    needs     normalization.", "Normal")
            .AddCenteredImage("comprehensive-chart-1")
            .AddParagraph("Content  between   images.")
            .AddCenteredImage("comprehensive-chart-2")
            .AddParagraph("")
            
            // Non-eligible links (should be ignored)
            .AddParagraph("Non-Eligible Links", "Heading2")
            .AddHyperlink("http://google.com", "Google")
            .AddHyperlink("mailto:test@example.com", "Email Link")
            .AddHyperlink("https://regular-website.com/page", "Regular Website")
            .AddParagraph("")
            
            // Top of document links
            .AddParagraph("Navigation", "Heading2")
            .AddInternalHyperlink("DocStart", "Top of Document")
            .AddInternalHyperlink("DocStart", "top of document")
            .AddInternalHyperlink("DocStart", "TOP OF DOCUMENT")
            .AddParagraph("")
            
            .AddParagraph("End of Test Document", "Normal");
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    /// <summary>
    /// Creates a minimal test document for basic validation.
    /// </summary>
    public static void CreateMinimalTestDocument(string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        docBuilder
            .AddParagraph("Minimal Test", "Heading1")
            .AddHyperlink("http://test.com?docid=MIN-001", "Test Link")
            .AddParagraph("Simple content.")
            .AddInternalHyperlink("#start", "Top of Document");
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    /// <summary>
    /// Creates expected output documents for approval testing.
    /// These represent what the documents should look like after processing.
    /// </summary>
    public static void CreateExpectedOutputDocument(string inputScenario, string filePath)
    {
        using var docBuilder = new DocumentBuilder();
        
        switch (inputScenario.ToLower())
        {
            case "eligible-hyperlinks":
                CreateExpectedEligibleHyperlinksOutput(docBuilder);
                break;
            case "multi-run":
                CreateExpectedMultiRunOutput(docBuilder);
                break;
            case "styles":
                CreateExpectedStylesOutput(docBuilder);
                break;
            case "top-of-document":
                CreateExpectedTopOfDocumentOutput(docBuilder);
                break;
            case "formatting":
                CreateExpectedFormattingOutput(docBuilder);
                break;
            default:
                throw new ArgumentException($"Unknown input scenario: {inputScenario}");
        }
        
        SaveDocumentToFile(docBuilder, filePath);
    }

    private static void CreateExpectedEligibleHyperlinksOutput(DocumentBuilder docBuilder)
    {
        docBuilder
            .AddParagraph("Document Processing Test", "Heading1")
            .AddParagraph("This document contains hyperlinks that should be processed by Doc_Medic.")
            .AddParagraph("")
            
            // Expected: URLs updated to canonical, display text has Content_ID suffixes
            .AddHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=DOC-12345", "Legacy Document Link")
            .AddHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=REPORT-67890", "Report Reference")
            .AddHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=FORM-ABC123", "Form Document")
            
            .AddParagraph("")
            .AddParagraph("Content ID References:", "Heading2")
            
            .AddHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=CMS-Project1-123456", "CMS Document (123456)")
            .AddHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=TSRC-DataAnalysis-987654", "TSRC Analysis Report (987654)")
            .AddHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=CMS-UserGuide-111222", "User Guide Reference (111222)")
            
            .AddParagraph("")
            .AddParagraph("Non-eligible Links:", "Heading2")
            
            // Non-eligible links remain unchanged
            .AddHyperlink("http://google.com", "Google Search")
            .AddHyperlink("mailto:support@company.com", "Email Support")
            .AddHyperlink("https://docs.microsoft.com/office", "Microsoft Office Docs");
    }

    private static void CreateExpectedMultiRunOutput(DocumentBuilder docBuilder)
    {
        // Expected output after processing multi-run hyperlinks
        docBuilder
            .AddParagraph("Multi-Run Hyperlink Test", "Heading1")
            .AddParagraph("This document tests hyperlinks where display text spans multiple runs.")
            .AddParagraph("")
            
            // Multi-run text should have suffixes appended to last run
            .AddMultiRunHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=SPLIT-12345",
                "Document", " Title", " Split", " Across", " Runs")
            .AddMultiRunHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=CMS-Complex-987654",
                "CMS ", "Document ", "With ", "Multiple ", "Runs (987654)")
            .AddMultiRunHyperlink("http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=TSRC-Analysis-456789",
                "TSRC", " ", "Analysis", " (", "456789", ")");
    }

    private static void CreateExpectedStylesOutput(DocumentBuilder docBuilder)
    {
        // Expected output with standardized styles
        // This would include proper style definitions with CLAUDE.md specifications
        docBuilder
            .AddStyle("Normal", "Normal", StyleValues.Paragraph)
            .AddStyle("Heading1", "Heading 1", StyleValues.Paragraph)
            .AddStyle("Heading2", "Heading 2", StyleValues.Paragraph)
            .AddStyle("Hyperlink", "Hyperlink", StyleValues.Character);
        
        // Content with standardized styles applied
    }

    private static void CreateExpectedTopOfDocumentOutput(DocumentBuilder docBuilder)
    {
        // Expected output with Top of Document processing
        docBuilder
            .AddBookmark("DocStart", "Document Start")
            .AddParagraph("Top of Document Test", "Heading1")
            // All "Top of Document" links should point to DocStart and be right-aligned
            .AddInternalHyperlink("DocStart", "Top of Document");
    }

    private static void CreateExpectedFormattingOutput(DocumentBuilder docBuilder)
    {
        // Expected output with formatting cleanup
        docBuilder
            .AddParagraph("Formatting Issues Test", "Heading1")
            .AddParagraph("This document contains various formatting problems.")
            .AddParagraph("")
            
            // Spaces normalized
            .AddParagraph("Paragraph with multiple spaces everywhere.")
            .AddParagraph("Another paragraph with spacing issues.")
            .AddParagraph("")
            
            // Images centered
            .AddCenteredImage("uncentered-image-1")
            .AddParagraph("Content between images.")
            .AddCenteredImage("uncentered-image-2");
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
}

/// <summary>
/// Static helper for creating test fixture files in the Fixtures directory.
/// </summary>
public static class FixtureFiles
{
    public static readonly string FixturesDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Documents");

    static FixtureFiles()
    {
        Directory.CreateDirectory(FixturesDirectory);
    }

    public static string GetPath(string fileName) => Path.Combine(FixturesDirectory, fileName);

    /// <summary>
    /// Creates all standard test fixture files.
    /// </summary>
    public static void CreateAllTestFixtures()
    {
        TestFixtureBuilder.CreateSampleWithEligibleHyperlinks(GetPath("sample-eligible-hyperlinks.docx"));
        TestFixtureBuilder.CreateSampleWithMultiRunHyperlinks(GetPath("sample-multi-run-hyperlinks.docx"));
        TestFixtureBuilder.CreateSampleWithStylesToStandardize(GetPath("sample-styles-to-standardize.docx"));
        TestFixtureBuilder.CreateSampleWithTopOfDocumentLinks(GetPath("sample-top-of-document.docx"));
        TestFixtureBuilder.CreateSampleWithFormattingIssues(GetPath("sample-formatting-issues.docx"));
        TestFixtureBuilder.CreateComprehensiveTestDocument(GetPath("comprehensive-test-document.docx"));
        TestFixtureBuilder.CreateMinimalTestDocument(GetPath("minimal-test-document.docx"));

        // Create expected outputs for approval testing
        TestFixtureBuilder.CreateExpectedOutputDocument("eligible-hyperlinks", GetPath("expected-eligible-hyperlinks-output.docx"));
        TestFixtureBuilder.CreateExpectedOutputDocument("multi-run", GetPath("expected-multi-run-output.docx"));
        TestFixtureBuilder.CreateExpectedOutputDocument("styles", GetPath("expected-styles-output.docx"));
        TestFixtureBuilder.CreateExpectedOutputDocument("top-of-document", GetPath("expected-top-of-document-output.docx"));
        TestFixtureBuilder.CreateExpectedOutputDocument("formatting", GetPath("expected-formatting-output.docx"));
    }
}