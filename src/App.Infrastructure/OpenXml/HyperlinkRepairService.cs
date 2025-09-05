using App.Core;
using App.Domain;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.OpenXml;

/// <summary>
/// Implementation of hyperlink repair service using OpenXML SDK.
/// Handles URL updates and display text formatting according to the specification.
/// </summary>
public class HyperlinkRepairService : IHyperlinkRepairService
{
    private readonly ILogger<HyperlinkRepairService> _logger;

    public HyperlinkRepairService(ILogger<HyperlinkRepairService> logger)
    {
        _logger = logger;
    }

    public int Repair(WordprocessingDocument document, HyperlinkIndex index, IReadOnlyDictionary<string, LookupResult> lookupMap)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(lookupMap);

        _logger.LogInformation("Starting hyperlink repair for {HyperlinkCount} indexed hyperlinks", index.Count);

        var repairCount = 0;
        var eligibleHyperlinks = index.GetEligibleHyperlinks();

        _logger.LogDebug("Found {EligibleCount} eligible hyperlinks for repair", eligibleHyperlinks.Count);

        foreach (var hyperlink in eligibleHyperlinks)
        {
            try
            {
                var metadata = ResolveMetadata(hyperlink, lookupMap);
                if (metadata == null)
                {
                    _logger.LogDebug("No metadata found for hyperlink {HyperlinkId} with target {Target}", 
                        hyperlink.Id, hyperlink.Target);
                    continue;
                }

                if (RepairSingle(document, hyperlink, metadata))
                {
                    repairCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to repair hyperlink {HyperlinkId} with target {Target}", 
                    hyperlink.Id, hyperlink.Target);
            }
        }

        _logger.LogInformation("Completed hyperlink repair: {RepairedCount} of {EligibleCount} hyperlinks repaired", 
            repairCount, eligibleHyperlinks.Count);

        return repairCount;
    }

    public bool RepairSingle(WordprocessingDocument document, HyperlinkRef hyperlink, LookupResult metadata)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(hyperlink);
        ArgumentNullException.ThrowIfNull(metadata);

        if (!NeedsRepair(hyperlink, metadata))
        {
            _logger.LogDebug("Hyperlink {HyperlinkId} does not need repair", hyperlink.Id);
            return false;
        }

        var wasRepaired = false;

        // Update URL to canonical form
        if (UpdateHyperlinkTarget(document, hyperlink, metadata))
        {
            wasRepaired = true;
            _logger.LogDebug("Updated target URL for hyperlink {HyperlinkId}", hyperlink.Id);
        }

        // Update display text with Content_ID suffix
        if (UpdateDisplayText(document, hyperlink, metadata))
        {
            wasRepaired = true;
            _logger.LogDebug("Updated display text for hyperlink {HyperlinkId}", hyperlink.Id);
        }

        return wasRepaired;
    }

    public bool NeedsRepair(HyperlinkRef hyperlink, LookupResult metadata)
    {
        ArgumentNullException.ThrowIfNull(hyperlink);
        ArgumentNullException.ThrowIfNull(metadata);

        // Check if URL needs updating to canonical form
        var canonicalUrl = HyperlinkPatterns.BuildCanonicalUrl(metadata.DocumentId);
        var needsUrlUpdate = !string.Equals(hyperlink.Target, canonicalUrl, StringComparison.OrdinalIgnoreCase);

        // Check if display text needs Content_ID suffix
        var expectedSuffix = $" ({metadata.GetDisplaySuffix()})";
        var needsDisplayTextUpdate = !hyperlink.DisplayTextRange.Text.EndsWith(expectedSuffix, StringComparison.Ordinal);

        return needsUrlUpdate || needsDisplayTextUpdate;
    }

    private LookupResult? ResolveMetadata(HyperlinkRef hyperlink, IReadOnlyDictionary<string, LookupResult> lookupMap)
    {
        // Try to resolve by Document_ID first
        var documentId = HyperlinkPatterns.ExtractDocumentId(hyperlink.Target);
        if (!string.IsNullOrEmpty(documentId) && lookupMap.TryGetValue(documentId, out var byDocId))
        {
            return byDocId;
        }

        // Try to resolve by Content_ID
        var contentId = HyperlinkPatterns.ExtractContentId(hyperlink.Target);
        if (!string.IsNullOrEmpty(contentId) && lookupMap.TryGetValue(contentId, out var byContentId))
        {
            return byContentId;
        }

        return null;
    }

    private bool UpdateHyperlinkTarget(WordprocessingDocument document, HyperlinkRef hyperlink, LookupResult metadata)
    {
        var canonicalUrl = HyperlinkPatterns.BuildCanonicalUrl(metadata.DocumentId);
        
        // Skip if already canonical
        if (string.Equals(hyperlink.Target, canonicalUrl, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (hyperlink.IsExternalLink)
        {
            return UpdateExternalLinkTarget(document, hyperlink, canonicalUrl);
        }
        else
        {
            // Handle field codes - convert to external hyperlink
            return ConvertFieldCodeToExternalLink(document, hyperlink, canonicalUrl);
        }
    }

    private bool UpdateExternalLinkTarget(WordprocessingDocument document, HyperlinkRef hyperlink, string canonicalUrl)
    {
        if (string.IsNullOrEmpty(hyperlink.RelationshipId))
        {
            return false;
        }

        try
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart == null) return false;

            // Find the existing hyperlink relationship
            var existingRel = mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == hyperlink.RelationshipId);
            if (existingRel == null) return false;

            // Delete old relationship and create new one - use proper method
            // For hyperlink relationships, we need to delete by ID not by part reference
            var oldId = existingRel.Id;
            var newRel = mainPart.AddHyperlinkRelationship(new Uri(canonicalUrl, UriKind.Absolute), true);

            // Update all hyperlink elements that reference this relationship
            var hyperlinkElements = mainPart.Document.Descendants<Hyperlink>()
                .Where(h => h.Id?.Value == hyperlink.RelationshipId);

            foreach (var element in hyperlinkElements)
            {
                element.Id = newRel.Id;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update external hyperlink target for {HyperlinkId}", hyperlink.Id);
            return false;
        }
    }

    private bool ConvertFieldCodeToExternalLink(WordprocessingDocument document, HyperlinkRef hyperlink, string canonicalUrl)
    {
        // This is a complex operation that would require finding field codes and converting them
        // For now, log a warning and return false - this can be implemented in a future iteration
        _logger.LogWarning("Field code to hyperlink conversion not yet implemented for hyperlink {HyperlinkId}", 
            hyperlink.Id);
        return false;
    }

    private bool UpdateDisplayText(WordprocessingDocument document, HyperlinkRef hyperlink, LookupResult metadata)
    {
        var expectedSuffix = $" ({metadata.GetDisplaySuffix()})";
        
        // Check if suffix is already present
        if (hyperlink.DisplayTextRange.Text.EndsWith(expectedSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        // Check if any existing suffix needs replacement
        var currentText = hyperlink.DisplayTextRange.Text;
        var existingSuffixMatch = HyperlinkPatterns.DisplaySuffixPattern.Match(currentText);
        
        if (existingSuffixMatch.Success)
        {
            // Replace existing suffix
            var newText = currentText.Substring(0, existingSuffixMatch.Index) + expectedSuffix;
            return ReplaceDisplayText(document, hyperlink, newText);
        }
        else
        {
            // Append new suffix (trim trailing whitespace first)
            var trimmedText = currentText.TrimEnd();
            var newText = trimmedText + expectedSuffix;
            return ReplaceDisplayText(document, hyperlink, newText);
        }
    }

    private bool ReplaceDisplayText(WordprocessingDocument document, HyperlinkRef hyperlink, string newText)
    {
        try
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart == null) return false;

            // Get all paragraphs in the document
            var paragraphs = mainPart.Document.Body?.Descendants<Paragraph>().ToList();
            if (paragraphs == null || hyperlink.DisplayTextRange.ParagraphIndex >= paragraphs.Count)
            {
                _logger.LogWarning("Cannot find paragraph at index {ParagraphIndex} for hyperlink {HyperlinkId}", 
                    hyperlink.DisplayTextRange.ParagraphIndex, hyperlink.Id);
                return false;
            }

            var paragraph = paragraphs[hyperlink.DisplayTextRange.ParagraphIndex];
            var runs = paragraph.Descendants<Run>().ToList();

            // Handle single run case
            if (hyperlink.DisplayTextRange.StartRunIndex == hyperlink.DisplayTextRange.EndRunIndex)
            {
                return UpdateSingleRunText(runs, hyperlink, newText);
            }
            else
            {
                // Handle multiple runs case
                return UpdateMultipleRunsText(runs, hyperlink, newText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update display text for hyperlink {HyperlinkId}", hyperlink.Id);
            return false;
        }
    }

    private bool UpdateSingleRunText(List<Run> runs, HyperlinkRef hyperlink, string newText)
    {
        if (hyperlink.DisplayTextRange.StartRunIndex >= runs.Count)
        {
            _logger.LogWarning("Run index {RunIndex} out of range for hyperlink {HyperlinkId}", 
                hyperlink.DisplayTextRange.StartRunIndex, hyperlink.Id);
            return false;
        }

        var run = runs[hyperlink.DisplayTextRange.StartRunIndex];
        var textElement = run.GetFirstChild<Text>();
        
        if (textElement != null)
        {
            textElement.Text = newText;
            // Preserve xml:space attribute if there are leading/trailing spaces
            if (newText.StartsWith(' ') || newText.EndsWith(' '))
            {
                textElement.Space = SpaceProcessingModeValues.Preserve;
            }
            return true;
        }

        return false;
    }

    private bool UpdateMultipleRunsText(List<Run> runs, HyperlinkRef hyperlink, string newText)
    {
        // For multiple runs spanning case, put all text in the first run and clear others
        // This preserves formatting of the first run while simplifying the text structure
        
        if (hyperlink.DisplayTextRange.StartRunIndex >= runs.Count || 
            hyperlink.DisplayTextRange.EndRunIndex >= runs.Count)
        {
            _logger.LogWarning("Run indices out of range for hyperlink {HyperlinkId}", hyperlink.Id);
            return false;
        }

        var firstRun = runs[hyperlink.DisplayTextRange.StartRunIndex];
        var firstTextElement = firstRun.GetFirstChild<Text>();
        
        if (firstTextElement != null)
        {
            // Set the complete new text in the first run
            firstTextElement.Text = newText;
            if (newText.StartsWith(' ') || newText.EndsWith(' '))
            {
                firstTextElement.Space = SpaceProcessingModeValues.Preserve;
            }

            // Clear text in subsequent runs that were part of the hyperlink
            for (int i = hyperlink.DisplayTextRange.StartRunIndex + 1; i <= hyperlink.DisplayTextRange.EndRunIndex; i++)
            {
                if (i < runs.Count)
                {
                    var runTextElement = runs[i].GetFirstChild<Text>();
                    if (runTextElement != null)
                    {
                        runTextElement.Text = string.Empty;
                    }
                }
            }

            return true;
        }

        return false;
    }
}