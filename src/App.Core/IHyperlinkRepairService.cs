using App.Domain;
using DocumentFormat.OpenXml.Packaging;

namespace App.Core;

/// <summary>
/// Service for repairing hyperlinks in Word documents based on API lookup results.
/// Handles URL updates and display text formatting according to the specification.
/// </summary>
public interface IHyperlinkRepairService
{
    /// <summary>
    /// Repairs eligible hyperlinks in the document using the provided lookup results.
    /// </summary>
    /// <param name="document">The Word document containing hyperlinks to repair.</param>
    /// <param name="index">Index of hyperlinks in the document.</param>
    /// <param name="lookupMap">Map of lookup IDs to resolved document metadata.</param>
    /// <returns>Number of hyperlinks that were successfully repaired.</returns>
    int Repair(WordprocessingDocument document, HyperlinkIndex index, IReadOnlyDictionary<string, LookupResult> lookupMap);

    /// <summary>
    /// Repairs a single hyperlink with the provided metadata.
    /// </summary>
    /// <param name="document">The Word document containing the hyperlink.</param>
    /// <param name="hyperlink">The hyperlink reference to repair.</param>
    /// <param name="metadata">The resolved document metadata for the hyperlink.</param>
    /// <returns>True if the hyperlink was successfully repaired, false otherwise.</returns>
    bool RepairSingle(WordprocessingDocument document, HyperlinkRef hyperlink, LookupResult metadata);

    /// <summary>
    /// Validates that a hyperlink needs repair based on current target and metadata.
    /// </summary>
    /// <param name="hyperlink">The hyperlink to validate.</param>
    /// <param name="metadata">The resolved document metadata.</param>
    /// <returns>True if the hyperlink needs repair, false if it's already correct.</returns>
    bool NeedsRepair(HyperlinkRef hyperlink, LookupResult metadata);
}