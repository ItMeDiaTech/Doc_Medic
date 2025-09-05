using App.Core;
using App.Domain;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.OpenXml;

/// <summary>
/// OpenXML implementation of the hyperlink indexing service.
/// Scans Word documents for external links, internal anchors, and field codes.
/// </summary>
public class HyperlinkIndexService : IHyperlinkIndexService
{
    private readonly ILogger<HyperlinkIndexService> _logger;

    public HyperlinkIndexService(ILogger<HyperlinkIndexService> logger)
    {
        _logger = logger;
    }

    public HyperlinkIndex Build(WordprocessingDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        
        var index = new HyperlinkIndex();
        
        try
        {
            // Index external hyperlinks (w:hyperlink with r:id)
            IndexExternalHyperlinks(document, index);
            
            // Index internal anchor links (w:hyperlink with w:anchor)
            IndexInternalAnchors(document, index);
            
            // Index field code hyperlinks (HYPERLINK field codes)
            IndexFieldCodeHyperlinks(document, index);
            
            _logger.LogInformation("Built hyperlink index with {Count} hyperlinks", index.Count);
            return index;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build hyperlink index");
            throw;
        }
    }

    public bool ValidateIndex(WordprocessingDocument document, HyperlinkIndex index)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(index);

        try
        {
            // Simple validation: check if the number of hyperlinks matches
            var body = document.MainDocumentPart?.Document?.Body;
            if (body == null) return false;

            var currentHyperlinkCount = body.Descendants<Hyperlink>().Count() + 
                                      body.Descendants<SimpleField>().Count(f => IsHyperlinkField(f)) +
                                      CountInstructionTextHyperlinks(body);

            return Math.Abs(currentHyperlinkCount - index.Count) <= 1; // Allow small variance
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate hyperlink index");
            return false;
        }
    }

    private void IndexExternalHyperlinks(WordprocessingDocument document, HyperlinkIndex index)
    {
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        var relationships = document.MainDocumentPart?.HyperlinkRelationships;
        if (relationships == null) return;

        var hyperlinks = body.Descendants<Hyperlink>().Where(h => h.Id?.HasValue == true);

        foreach (var hyperlink in hyperlinks)
        {
            var relationshipId = hyperlink.Id?.Value;
            if (string.IsNullOrEmpty(relationshipId)) continue;

            var relationship = relationships.FirstOrDefault(r => r.Id == relationshipId);
            if (relationship?.Uri == null) continue;

            var target = relationship.Uri.ToString();
            if (!HyperlinkPatterns.IsEligibleUrl(target)) continue;

            var displayTextRange = ExtractDisplayTextRange(hyperlink);
            var hyperlinkRef = HyperlinkRef.ForExternalLink(
                Guid.NewGuid().ToString(),
                relationshipId,
                target,
                displayTextRange
            );

            index.AddHyperlink(hyperlinkRef);
            _logger.LogDebug("Indexed external hyperlink: {Target}", target);
        }
    }

    private void IndexInternalAnchors(WordprocessingDocument document, HyperlinkIndex index)
    {
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        var hyperlinks = body.Descendants<Hyperlink>().Where(h => h.Anchor?.HasValue == true);

        foreach (var hyperlink in hyperlinks)
        {
            var anchor = hyperlink.Anchor?.Value;
            if (string.IsNullOrEmpty(anchor)) continue;

            var displayTextRange = ExtractDisplayTextRange(hyperlink);
            var hyperlinkRef = HyperlinkRef.ForInternalAnchor(
                Guid.NewGuid().ToString(),
                anchor,
                displayTextRange
            );

            index.AddHyperlink(hyperlinkRef);
            _logger.LogDebug("Indexed internal anchor: #{Anchor}", anchor);
        }
    }

    private void IndexFieldCodeHyperlinks(WordprocessingDocument document, HyperlinkIndex index)
    {
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        // Index simple field codes (w:fldSimple)
        var simpleFields = body.Descendants<SimpleField>()
            .Where(f => IsHyperlinkField(f));

        foreach (var field in simpleFields)
        {
            var instruction = field.Instruction?.Value;
            if (string.IsNullOrEmpty(instruction)) continue;

            var target = ExtractUrlFromFieldCode(instruction);
            if (string.IsNullOrEmpty(target) || !HyperlinkPatterns.IsEligibleUrl(target)) continue;

            var displayTextRange = ExtractFieldDisplayTextRange(field);
            var hyperlinkRef = HyperlinkRef.ForExternalLink(
                Guid.NewGuid().ToString(),
                null!, // Field codes don't have relationship IDs
                target,
                displayTextRange
            );

            index.AddHyperlink(hyperlinkRef);
            _logger.LogDebug("Indexed field code hyperlink: {Target}", target);
        }

        // Index complex field codes (w:instrText)
        var complexFields = body.Descendants<FieldCode>()
            .Where(fc => fc.Text?.Contains("HYPERLINK", StringComparison.OrdinalIgnoreCase) == true);

        foreach (var fieldCode in complexFields)
        {
            var instruction = fieldCode.Text;
            if (string.IsNullOrEmpty(instruction)) continue;

            var target = ExtractUrlFromFieldCode(instruction);
            if (string.IsNullOrEmpty(target) || !HyperlinkPatterns.IsEligibleUrl(target)) continue;

            var displayTextRange = ExtractComplexFieldDisplayTextRange(fieldCode);
            var hyperlinkRef = HyperlinkRef.ForExternalLink(
                Guid.NewGuid().ToString(),
                null!,
                target,
                displayTextRange
            );

            index.AddHyperlink(hyperlinkRef);
            _logger.LogDebug("Indexed complex field code hyperlink: {Target}", target);
        }
    }

    private static bool IsHyperlinkField(SimpleField field)
    {
        return field.Instruction?.Value?.StartsWith("HYPERLINK", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static int CountInstructionTextHyperlinks(Body body)
    {
        return body.Descendants<FieldCode>()
            .Count(fc => fc.Text?.Contains("HYPERLINK", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string? ExtractUrlFromFieldCode(string instruction)
    {
        // Parse field code like: HYPERLINK "http://example.com" \o "Optional tooltip"
        var match = System.Text.RegularExpressions.Regex.Match(
            instruction, 
            @"HYPERLINK\s+""([^""]+)""", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value : null;
    }

    private static TextRange ExtractDisplayTextRange(Hyperlink hyperlink)
    {
        // Find the paragraph containing this hyperlink
        var paragraph = hyperlink.Ancestors<Paragraph>().FirstOrDefault();
        if (paragraph == null)
            return new TextRange();

        var runs = paragraph.Descendants<Run>().ToList();
        var hyperlinkRuns = hyperlink.Descendants<Run>().ToList();
        
        if (hyperlinkRuns.Count == 0)
            return new TextRange();

        var firstRun = hyperlinkRuns.First();
        var lastRun = hyperlinkRuns.Last();
        
        var startIndex = runs.IndexOf(firstRun);
        var endIndex = runs.IndexOf(lastRun);
        
        if (startIndex == -1 || endIndex == -1)
            return new TextRange();

        var displayText = string.Join("", hyperlinkRuns.SelectMany(r => r.Descendants<Text>()).Select(t => t.Text));

        return TextRange.ForMultipleRuns(0, startIndex, endIndex, displayText);
    }

    private static TextRange ExtractFieldDisplayTextRange(SimpleField field)
    {
        var paragraph = field.Ancestors<Paragraph>().FirstOrDefault();
        if (paragraph == null)
            return new TextRange();

        var runs = paragraph.Descendants<Run>().ToList();
        var fieldRun = field.Ancestors<Run>().FirstOrDefault();
        
        if (fieldRun == null)
            return new TextRange();

        var runIndex = runs.IndexOf(fieldRun);
        if (runIndex == -1)
            return new TextRange();

        var displayText = field.Descendants<Text>().FirstOrDefault()?.Text ?? "";

        return TextRange.ForSingleRun(0, runIndex, displayText);
    }

    private static TextRange ExtractComplexFieldDisplayTextRange(FieldCode fieldCode)
    {
        // For complex fields, we need to find the result text between field begin/end markers
        var paragraph = fieldCode.Ancestors<Paragraph>().FirstOrDefault();
        if (paragraph == null)
            return new TextRange();

        var runs = paragraph.Descendants<Run>().ToList();
        var fieldRun = fieldCode.Ancestors<Run>().FirstOrDefault();
        
        if (fieldRun == null)
            return new TextRange();

        var runIndex = runs.IndexOf(fieldRun);
        if (runIndex == -1)
            return new TextRange();

        // This is a simplified approach - complex field text extraction would need more sophisticated logic
        var displayText = "";
        return TextRange.ForSingleRun(0, runIndex, displayText);
    }
}