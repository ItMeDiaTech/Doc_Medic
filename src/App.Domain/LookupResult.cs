namespace App.Domain;

/// <summary>
/// Result from the Power Automate API lookup service containing document metadata.
/// </summary>
public record LookupResult(
    string Title,
    string Status,
    string ContentId,
    string DocumentId)
{
    /// <summary>
    /// Indicates whether the document is currently released (not expired).
    /// </summary>
    public bool IsReleased => string.Equals(Status, "Released", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts the last 6 digits from the Content ID for display text formatting.
    /// If only 5 digits are present, prefixes with '0'.
    /// </summary>
    public string GetDisplaySuffix()
    {
        if (string.IsNullOrEmpty(ContentId))
            return "000000";

        // Extract last 6 digits
        var match = System.Text.RegularExpressions.Regex.Match(ContentId, @"(\d{6})$");
        if (match.Success)
            return match.Value;

        // If only last 5 digits present, prefix with 0
        var match5 = System.Text.RegularExpressions.Regex.Match(ContentId, @"(\d{5})$");
        if (match5.Success)
            return "0" + match5.Value;

        return "000000";
    }
}