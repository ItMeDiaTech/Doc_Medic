using App.Core;
using App.Domain;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.OpenXml;

/// <summary>
/// Implementation of document formatting service using OpenXML SDK.
/// Handles spacing, style standardization, image centering, and "Top of Document" link fixes.
/// </summary>
public class FormattingService : IFormattingService
{
    private readonly ILogger<FormattingService> _logger;

    // Style constants following the specification
    private const string NormalStyleId = "Normal";
    private const string Heading1StyleId = "Heading1";
    private const string Heading2StyleId = "Heading2";
    private const string HyperlinkStyleId = "Hyperlink";
    private const string DocStartBookmarkName = "DocStart";

    public FormattingService(ILogger<FormattingService> logger)
    {
        _logger = logger;
    }

    public int NormalizeSpaces(WordprocessingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _logger.LogDebug("Starting space normalization");

        var normalizations = 0;
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return 0;

        // Process all text runs in the document
        var textElements = body.Descendants<Text>().ToList();

        foreach (var textElement in textElements)
        {
            if (string.IsNullOrEmpty(textElement.Text)) continue;

            var originalText = textElement.Text;
            
            // Skip non-breaking spaces (preserve them)
            if (originalText.Contains('\u00A0')) continue;

            // Collapse multiple spaces using the pattern from HyperlinkPatterns
            var normalizedText = HyperlinkPatterns.MultipleSpacesPattern.Replace(originalText, " ");

            if (!string.Equals(originalText, normalizedText, StringComparison.Ordinal))
            {
                textElement.Text = normalizedText;
                
                // Preserve xml:space if there are leading/trailing spaces
                if (normalizedText.StartsWith(' ') || normalizedText.EndsWith(' '))
                {
                    textElement.Space = SpaceProcessingModeValues.Preserve;
                }

                normalizations++;
                _logger.LogTrace("Normalized spaces in text: '{OriginalText}' -> '{NormalizedText}'", 
                    originalText, normalizedText);
            }
        }

        _logger.LogDebug("Completed space normalization: {NormalizationCount} text elements updated", normalizations);
        return normalizations;
    }

    public int FixTopOfDocumentLinks(WordprocessingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _logger.LogDebug("Starting Top of Document links fix");

        var fixedLinks = 0;

        // Ensure DocStart bookmark exists at document start
        EnsureDocStartBookmark(document);

        // Find and fix "Top of Document" links
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return 0;

        var paragraphs = body.Descendants<Paragraph>().ToList();

        foreach (var paragraph in paragraphs)
        {
            var paragraphText = GetParagraphText(paragraph);
            
            if (HyperlinkPatterns.TopOfDocumentPattern.IsMatch(paragraphText))
            {
                if (ConvertToTopOfDocumentLink(paragraph))
                {
                    fixedLinks++;
                    _logger.LogTrace("Fixed Top of Document link in paragraph with text: '{Text}'", paragraphText);
                }
            }
        }

        _logger.LogDebug("Completed Top of Document links fix: {FixedCount} links updated", fixedLinks);
        return fixedLinks;
    }

    public StyleReport EnsureStyles(WordprocessingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _logger.LogDebug("Starting style standardization");

        var warnings = new List<string>();
        var report = new StyleReport();

        try
        {
            // Ensure StylesPart exists
            var stylesPart = EnsureStylesPart(document);
            var stylesPartCreated = stylesPart.Item2;

            // Create or update standard styles
            var (normalCreated, normalApplied) = EnsureNormalStyle(stylesPart.Item1);
            var (heading1Created, heading1Applied) = EnsureHeading1Style(stylesPart.Item1);
            var (heading2Created, heading2Applied) = EnsureHeading2Style(stylesPart.Item1);
            var (hyperlinkCreated, hyperlinkApplied) = EnsureHyperlinkStyle(stylesPart.Item1);

            report = new StyleReport
            {
                StylesPartCreated = stylesPartCreated,
                NormalStyleCreated = normalCreated,
                Heading1StyleCreated = heading1Created,
                Heading2StyleCreated = heading2Created,
                HyperlinkStyleCreated = hyperlinkCreated,
                NormalStylesApplied = normalApplied,
                Heading1StylesApplied = heading1Applied,
                Heading2StylesApplied = heading2Applied,
                HyperlinkStylesApplied = hyperlinkApplied,
                Warnings = warnings.AsReadOnly()
            };

            _logger.LogDebug("Completed style standardization: {TotalChanges} total changes made", 
                report.TotalStyleChanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during style standardization");
            warnings.Add($"Style standardization failed: {ex.Message}");
            report = StyleReport.WithWarnings(warnings.ToArray());
        }

        return report;
    }

    public int CenterImages(WordprocessingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _logger.LogDebug("Starting image centering");

        var centeredImages = 0;
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return 0;

        // Find paragraphs containing drawings (images)
        var paragraphsWithImages = body.Descendants<Paragraph>()
            .Where(p => p.Descendants<Drawing>().Any())
            .ToList();

        foreach (var paragraph in paragraphsWithImages)
        {
            // Set paragraph justification to center
            var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>() ?? 
                paragraph.InsertAt(new ParagraphProperties(), 0);

            var justification = paragraphProperties.GetFirstChild<Justification>() ?? 
                paragraphProperties.AppendChild(new Justification());

            justification.Val = JustificationValues.Center;
            centeredImages++;

            _logger.LogTrace("Centered image in paragraph");
        }

        _logger.LogDebug("Completed image centering: {CenteredCount} images centered", centeredImages);
        return centeredImages;
    }

    public int ApplyStyleSelectively(WordprocessingDocument document, string styleId, Func<string, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(predicate);

        var appliedStyles = 0;
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return 0;

        var paragraphs = body.Descendants<Paragraph>().ToList();

        foreach (var paragraph in paragraphs)
        {
            var paragraphText = GetParagraphText(paragraph);
            
            if (predicate(paragraphText))
            {
                ApplyParagraphStyle(paragraph, styleId);
                appliedStyles++;
            }
        }

        return appliedStyles;
    }

    private void EnsureDocStartBookmark(WordprocessingDocument document)
    {
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        // Check if DocStart bookmark already exists
        var existingBookmark = body.Descendants<BookmarkStart>()
            .FirstOrDefault(b => string.Equals(b.Name?.Value, DocStartBookmarkName, StringComparison.OrdinalIgnoreCase));

        if (existingBookmark != null)
        {
            _logger.LogTrace("DocStart bookmark already exists");
            return;
        }

        // Create bookmark at the start of the first paragraph
        var firstParagraph = body.GetFirstChild<Paragraph>();
        if (firstParagraph == null)
        {
            _logger.LogWarning("No paragraphs found in document body");
            return;
        }

        var bookmarkId = GetNextBookmarkId(document);
        var bookmarkStart = new BookmarkStart { Name = DocStartBookmarkName, Id = bookmarkId };
        var bookmarkEnd = new BookmarkEnd { Id = bookmarkId };

        // Insert at the beginning of the first paragraph
        firstParagraph.InsertAt(bookmarkStart, 0);
        firstParagraph.InsertAt(bookmarkEnd, 1);

        _logger.LogTrace("Created DocStart bookmark with ID {BookmarkId}", bookmarkId);
    }

    private string GetNextBookmarkId(WordprocessingDocument document)
    {
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return "1";

        var existingIds = body.Descendants<BookmarkStart>()
            .Select(b => b.Id?.Value)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => int.TryParse(id, out var parsed) ? parsed : 0)
            .ToList();

        var maxId = existingIds.Any() ? existingIds.Max() : 0;
        return (maxId + 1).ToString();
    }

    private bool ConvertToTopOfDocumentLink(Paragraph paragraph)
    {
        try
        {
            // Right-align the paragraph
            var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>() ?? 
                paragraph.InsertAt(new ParagraphProperties(), 0);

            var justification = paragraphProperties.GetFirstChild<Justification>() ?? 
                paragraphProperties.AppendChild(new Justification());

            justification.Val = JustificationValues.Right;

            // Convert content to internal hyperlink
            var runs = paragraph.Descendants<Run>().ToList();
            
            // Create new hyperlink element
            var hyperlink = new Hyperlink { Anchor = DocStartBookmarkName };
            
            foreach (var run in runs)
            {
                // Apply Hyperlink character style to the run
                var runProperties = run.GetFirstChild<RunProperties>() ?? 
                    run.InsertAt(new RunProperties(), 0);

                var runStyle = runProperties.GetFirstChild<RunStyle>() ?? 
                    runProperties.AppendChild(new RunStyle());

                runStyle.Val = HyperlinkStyleId;

                // Move the run into the hyperlink
                run.Remove();
                hyperlink.AppendChild(run);
            }

            // Replace paragraph content with hyperlink
            paragraph.RemoveAllChildren<Run>();
            paragraph.RemoveAllChildren<Hyperlink>();
            paragraph.AppendChild(hyperlink);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert paragraph to Top of Document link");
            return false;
        }
    }

    private (StylesPart, bool) EnsureStylesPart(WordprocessingDocument document)
    {
        var mainPart = document.MainDocumentPart;
        if (mainPart == null) 
            throw new InvalidOperationException("MainDocumentPart is null");

        var stylesPart = mainPart.StyleDefinitionsPart;
        bool created = false;

        if (stylesPart == null)
        {
            stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles();
            created = true;
            _logger.LogTrace("Created new StylesPart");
        }

        return (stylesPart, created);
    }

    private (bool created, int applied) EnsureNormalStyle(StylesPart stylesPart)
    {
        var styles = stylesPart.Styles;
        var existingStyle = styles?.Descendants<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId?.Value, NormalStyleId, StringComparison.OrdinalIgnoreCase));

        bool created = existingStyle == null;

        if (created)
        {
            var style = CreateNormalStyle();
            styles?.AppendChild(style);
            _logger.LogTrace("Created Normal paragraph style");
        }
        else
        {
            UpdateNormalStyle(existingStyle!);
            _logger.LogTrace("Updated existing Normal paragraph style");
        }

        // Apply style to paragraphs (conservative approach - only those already marked as Normal)
        var applied = ApplyStyleToExistingStyled(stylesPart.GetParentParts().First() as MainDocumentPart, NormalStyleId);
        
        return (created, applied);
    }

    private (bool created, int applied) EnsureHeading1Style(StylesPart stylesPart)
    {
        var styles = stylesPart.Styles;
        var existingStyle = styles?.Descendants<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId?.Value, Heading1StyleId, StringComparison.OrdinalIgnoreCase));

        bool created = existingStyle == null;

        if (created)
        {
            var style = CreateHeading1Style();
            styles?.AppendChild(style);
            _logger.LogTrace("Created Heading1 paragraph style");
        }
        else
        {
            UpdateHeading1Style(existingStyle!);
            _logger.LogTrace("Updated existing Heading1 paragraph style");
        }

        var applied = ApplyStyleToExistingStyled(stylesPart.GetParentParts().First() as MainDocumentPart, Heading1StyleId);
        
        return (created, applied);
    }

    private (bool created, int applied) EnsureHeading2Style(StylesPart stylesPart)
    {
        var styles = stylesPart.Styles;
        var existingStyle = styles?.Descendants<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId?.Value, Heading2StyleId, StringComparison.OrdinalIgnoreCase));

        bool created = existingStyle == null;

        if (created)
        {
            var style = CreateHeading2Style();
            styles?.AppendChild(style);
            _logger.LogTrace("Created Heading2 paragraph style");
        }
        else
        {
            UpdateHeading2Style(existingStyle!);
            _logger.LogTrace("Updated existing Heading2 paragraph style");
        }

        var applied = ApplyStyleToExistingStyled(stylesPart.GetParentParts().First() as MainDocumentPart, Heading2StyleId);
        
        return (created, applied);
    }

    private (bool created, int applied) EnsureHyperlinkStyle(StylesPart stylesPart)
    {
        var styles = stylesPart.Styles;
        var existingStyle = styles?.Descendants<Style>()
            .FirstOrDefault(s => string.Equals(s.StyleId?.Value, HyperlinkStyleId, StringComparison.OrdinalIgnoreCase));

        bool created = existingStyle == null;

        if (created)
        {
            var style = CreateHyperlinkStyle();
            styles?.AppendChild(style);
            _logger.LogTrace("Created Hyperlink character style");
        }
        else
        {
            UpdateHyperlinkStyle(existingStyle!);
            _logger.LogTrace("Updated existing Hyperlink character style");
        }

        // Character styles are applied to runs, count existing hyperlink styled runs
        var applied = CountHyperlinkStyledRuns(stylesPart.GetParentParts().First() as MainDocumentPart);
        
        return (created, applied);
    }

    private Style CreateNormalStyle()
    {
        return new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = NormalStyleId,
            Default = true,
            StyleName = new StyleName { Val = "Normal" },
            PrimaryStyle = new PrimaryStyle(),
            StyleRunProperties = new StyleRunProperties
            {
                RunFonts = new RunFonts 
                { 
                    Ascii = "Verdana", 
                    HighAnsi = "Verdana", 
                    ComplexScript = "Verdana" 
                },
                FontSize = new FontSize { Val = "24" }, // 12pt = 24 half-points
                Color = new Color { Val = "000000" }
            },
            StyleParagraphProperties = new StyleParagraphProperties
            {
                SpacingBetweenLines = new SpacingBetweenLines 
                { 
                    Line = "240", 
                    LineRule = LineSpacingRuleValues.Auto,
                    Before = "120" // 6pt = 120 twips
                }
            }
        };
    }

    private Style CreateHeading1Style()
    {
        return new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = Heading1StyleId,
            StyleName = new StyleName { Val = "Heading 1" },
            PrimaryStyle = new PrimaryStyle(),
            StyleRunProperties = new StyleRunProperties
            {
                RunFonts = new RunFonts 
                { 
                    Ascii = "Verdana", 
                    HighAnsi = "Verdana", 
                    ComplexScript = "Verdana" 
                },
                FontSize = new FontSize { Val = "36" }, // 18pt = 36 half-points
                Bold = new Bold(),
                Color = new Color { Val = "000000" }
            },
            StyleParagraphProperties = new StyleParagraphProperties
            {
                Justification = new Justification { Val = JustificationValues.Left },
                SpacingBetweenLines = new SpacingBetweenLines 
                { 
                    Line = "240", 
                    LineRule = LineSpacingRuleValues.Auto,
                    Before = "0", // 0pt before
                    After = "240" // 12pt after = 240 twips
                }
            }
        };
    }

    private Style CreateHeading2Style()
    {
        return new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = Heading2StyleId,
            StyleName = new StyleName { Val = "Heading 2" },
            PrimaryStyle = new PrimaryStyle(),
            StyleRunProperties = new StyleRunProperties
            {
                RunFonts = new RunFonts 
                { 
                    Ascii = "Verdana", 
                    HighAnsi = "Verdana", 
                    ComplexScript = "Verdana" 
                },
                FontSize = new FontSize { Val = "28" }, // 14pt = 28 half-points
                Bold = new Bold(),
                Color = new Color { Val = "000000" }
            },
            StyleParagraphProperties = new StyleParagraphProperties
            {
                Justification = new Justification { Val = JustificationValues.Left },
                SpacingBetweenLines = new SpacingBetweenLines 
                { 
                    Line = "240", 
                    LineRule = LineSpacingRuleValues.Auto,
                    Before = "120", // 6pt before = 120 twips
                    After = "120"   // 6pt after = 120 twips
                }
            }
        };
    }

    private Style CreateHyperlinkStyle()
    {
        return new Style
        {
            Type = StyleValues.Character,
            StyleId = HyperlinkStyleId,
            StyleName = new StyleName { Val = "Hyperlink" },
            StyleRunProperties = new StyleRunProperties
            {
                RunFonts = new RunFonts 
                { 
                    Ascii = "Verdana", 
                    HighAnsi = "Verdana", 
                    ComplexScript = "Verdana" 
                },
                FontSize = new FontSize { Val = "24" }, // 12pt = 24 half-points
                Color = new Color { Val = "0000FF" }, // Blue #0000FF
                Underline = new Underline { Val = UnderlineValues.Single }
            }
        };
    }

    private void UpdateNormalStyle(Style style)
    {
        // Update run properties
        if (style.StyleRunProperties == null)
            style.StyleRunProperties = new StyleRunProperties();

        var runProps = style.StyleRunProperties;
        runProps.RunFonts = new RunFonts { Ascii = "Verdana", HighAnsi = "Verdana", ComplexScript = "Verdana" };
        runProps.FontSize = new FontSize { Val = "24" };
        runProps.Color = new Color { Val = "000000" };

        // Update paragraph properties
        if (style.StyleParagraphProperties == null)
            style.StyleParagraphProperties = new StyleParagraphProperties();

        var paraProps = style.StyleParagraphProperties;
        paraProps.SpacingBetweenLines = new SpacingBetweenLines 
        { 
            Line = "240", 
            LineRule = LineSpacingRuleValues.Auto,
            Before = "120"
        };
    }

    private void UpdateHeading1Style(Style style)
    {
        if (style.StyleRunProperties == null)
            style.StyleRunProperties = new StyleRunProperties();

        var runProps = style.StyleRunProperties;
        runProps.RunFonts = new RunFonts { Ascii = "Verdana", HighAnsi = "Verdana", ComplexScript = "Verdana" };
        runProps.FontSize = new FontSize { Val = "36" };
        runProps.Bold = new Bold();
        runProps.Color = new Color { Val = "000000" };

        if (style.StyleParagraphProperties == null)
            style.StyleParagraphProperties = new StyleParagraphProperties();

        var paraProps = style.StyleParagraphProperties;
        paraProps.Justification = new Justification { Val = JustificationValues.Left };
        paraProps.SpacingBetweenLines = new SpacingBetweenLines 
        { 
            Line = "240", 
            LineRule = LineSpacingRuleValues.Auto,
            Before = "0",
            After = "240"
        };
    }

    private void UpdateHeading2Style(Style style)
    {
        if (style.StyleRunProperties == null)
            style.StyleRunProperties = new StyleRunProperties();

        var runProps = style.StyleRunProperties;
        runProps.RunFonts = new RunFonts { Ascii = "Verdana", HighAnsi = "Verdana", ComplexScript = "Verdana" };
        runProps.FontSize = new FontSize { Val = "28" };
        runProps.Bold = new Bold();
        runProps.Color = new Color { Val = "000000" };

        if (style.StyleParagraphProperties == null)
            style.StyleParagraphProperties = new StyleParagraphProperties();

        var paraProps = style.StyleParagraphProperties;
        paraProps.Justification = new Justification { Val = JustificationValues.Left };
        paraProps.SpacingBetweenLines = new SpacingBetweenLines 
        { 
            Line = "240", 
            LineRule = LineSpacingRuleValues.Auto,
            Before = "120",
            After = "120"
        };
    }

    private void UpdateHyperlinkStyle(Style style)
    {
        if (style.StyleRunProperties == null)
            style.StyleRunProperties = new StyleRunProperties();

        var runProps = style.StyleRunProperties;
        runProps.RunFonts = new RunFonts { Ascii = "Verdana", HighAnsi = "Verdana", ComplexScript = "Verdana" };
        runProps.FontSize = new FontSize { Val = "24" };
        runProps.Color = new Color { Val = "0000FF" };
        runProps.Underline = new Underline { Val = UnderlineValues.Single };
    }

    private int ApplyStyleToExistingStyled(MainDocumentPart? mainPart, string styleId)
    {
        if (mainPart?.Document?.Body == null) return 0;

        var appliedCount = 0;
        var paragraphs = mainPart.Document.Body.Descendants<Paragraph>().ToList();

        foreach (var paragraph in paragraphs)
        {
            var currentStyle = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            if (string.Equals(currentStyle, styleId, StringComparison.OrdinalIgnoreCase))
            {
                // Style is already applied, count it
                appliedCount++;
            }
        }

        return appliedCount;
    }

    private int CountHyperlinkStyledRuns(MainDocumentPart? mainPart)
    {
        if (mainPart?.Document?.Body == null) return 0;

        return mainPart.Document.Body.Descendants<Run>()
            .Count(r => r.RunProperties?.RunStyle?.Val?.Value == HyperlinkStyleId);
    }

    private void ApplyParagraphStyle(Paragraph paragraph, string styleId)
    {
        var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>() ?? 
            paragraph.InsertAt(new ParagraphProperties(), 0);

        var paragraphStyle = paragraphProperties.GetFirstChild<ParagraphStyleId>() ?? 
            paragraphProperties.AppendChild(new ParagraphStyleId());

        paragraphStyle.Val = styleId;
    }

    private string GetParagraphText(Paragraph paragraph)
    {
        return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
    }
}