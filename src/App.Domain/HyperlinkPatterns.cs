using System.Text.RegularExpressions;

namespace App.Domain;

/// <summary>
/// Contains regex patterns and constants for identifying eligible hyperlinks
/// according to the Doc_Medic specification.
/// </summary>
public static class HyperlinkPatterns
{
    /// <summary>
    /// Canonical base URL for Nuxeo document links.
    /// </summary>
    public const string NuxeoBaseUrl = "http://thesource.cvshealth.com/nuxeo/thesource/#!/view?docid=";

    /// <summary>
    /// Regex pattern to extract Document_ID from URLs containing ?docid=
    /// Captures everything after ?docid= up to #, &, or end of string.
    /// </summary>
    public static readonly Regex DocumentIdPattern = new(@"(?<=\?docid=)[^#&\s]+", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Regex pattern to extract Content_ID from URLs containing CMS or TSRC patterns.
    /// Pattern: (CMS|TSRC)-[A-Za-z0-9\-]+-\d{6}
    /// </summary>
    public static readonly Regex ContentIdPattern = new(@"\b(CMS|TSRC)-[A-Za-z0-9\-]+-\d{6}\b", 
        RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern to extract the last 6 digits from a Content_ID.
    /// </summary>
    public static readonly Regex Last6DigitsPattern = new(@"(\d{6})$", RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern to extract the last 5 digits from a Content_ID (for zero-padding).
    /// </summary>
    public static readonly Regex Last5DigitsPattern = new(@"(\d{5})$", RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern to find existing Content_ID suffix in display text.
    /// Matches patterns like " (123456)" or " (0123456)".
    /// </summary>
    public static readonly Regex DisplaySuffixPattern = new(@"\s*\([0]*\d{5,6}\)\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Regex pattern for collapsing multiple spaces (2 or more) into single spaces.
    /// </summary>
    public static readonly Regex MultipleSpacesPattern = new(@" {2,}", RegexOptions.Compiled);

    /// <summary>
    /// Pattern to identify "Top of Document" text with case-insensitive matching
    /// and optional whitespace normalization.
    /// </summary>
    public static readonly Regex TopOfDocumentPattern = new(@"^\s*top\s+of\s+document\s*$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Determines if a URL contains a Document_ID pattern (?docid=).
    /// </summary>
    public static bool ContainsDocumentId(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return url.Contains("?docid=", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a URL contains a Content_ID pattern (CMS or TSRC).
    /// </summary>
    public static bool ContainsContentId(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return ContentIdPattern.IsMatch(url);
    }

    /// <summary>
    /// Determines if a URL is eligible for hyperlink repair based on the specification.
    /// </summary>
    public static bool IsEligibleUrl(string url)
    {
        return ContainsDocumentId(url) || ContainsContentId(url);
    }

    /// <summary>
    /// Extracts Document_ID from a URL if present.
    /// </summary>
    public static string? ExtractDocumentId(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var match = DocumentIdPattern.Match(url);
        return match.Success ? match.Value : null;
    }

    /// <summary>
    /// Extracts Content_ID from a URL if present.
    /// </summary>
    public static string? ExtractContentId(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var match = ContentIdPattern.Match(url);
        return match.Success ? match.Value : null;
    }

    /// <summary>
    /// Builds canonical Nuxeo URL from a Document_ID.
    /// </summary>
    public static string BuildCanonicalUrl(string documentId)
    {
        if (string.IsNullOrEmpty(documentId))
            throw new ArgumentException("Document ID cannot be null or empty.", nameof(documentId));
        
        return NuxeoBaseUrl + documentId;
    }
}