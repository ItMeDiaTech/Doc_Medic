namespace App.Domain;

/// <summary>
/// Index of hyperlinks found in a document, providing efficient lookup
/// and batch operations for hyperlink repair.
/// </summary>
public class HyperlinkIndex
{
    private readonly Dictionary<string, HyperlinkRef> _hyperlinks = new();
    private readonly Dictionary<string, List<string>> _documentIdToHyperlinks = new();
    private readonly Dictionary<string, List<string>> _contentIdToHyperlinks = new();

    /// <summary>
    /// All hyperlinks in the index.
    /// </summary>
    public IReadOnlyCollection<HyperlinkRef> Hyperlinks => _hyperlinks.Values;

    /// <summary>
    /// Total count of hyperlinks in the index.
    /// </summary>
    public int Count => _hyperlinks.Count;

    /// <summary>
    /// Adds a hyperlink to the index.
    /// </summary>
    public void AddHyperlink(HyperlinkRef hyperlink)
    {
        if (hyperlink == null) return;
        if (string.IsNullOrEmpty(hyperlink.Id)) return;

        _hyperlinks[hyperlink.Id] = hyperlink;

        // Index by Document_ID if present
        var documentId = HyperlinkPatterns.ExtractDocumentId(hyperlink.Target);
        if (!string.IsNullOrEmpty(documentId))
        {
            if (!_documentIdToHyperlinks.ContainsKey(documentId))
                _documentIdToHyperlinks[documentId] = new List<string>();
            _documentIdToHyperlinks[documentId].Add(hyperlink.Id);
        }

        // Index by Content_ID if present
        var contentId = HyperlinkPatterns.ExtractContentId(hyperlink.Target);
        if (!string.IsNullOrEmpty(contentId))
        {
            if (!_contentIdToHyperlinks.ContainsKey(contentId))
                _contentIdToHyperlinks[contentId] = new List<string>();
            _contentIdToHyperlinks[contentId].Add(hyperlink.Id);
        }
    }

    /// <summary>
    /// Gets a hyperlink by its ID.
    /// </summary>
    public HyperlinkRef? GetHyperlink(string id)
    {
        return _hyperlinks.TryGetValue(id, out var hyperlink) ? hyperlink : null;
    }

    /// <summary>
    /// Gets all hyperlinks that reference the specified Document_ID.
    /// </summary>
    public IReadOnlyList<HyperlinkRef> GetHyperlinksByDocumentId(string documentId)
    {
        if (!_documentIdToHyperlinks.TryGetValue(documentId, out var hyperlinkIds))
            return Array.Empty<HyperlinkRef>();

        return hyperlinkIds
            .Select(id => _hyperlinks.TryGetValue(id, out var link) ? link : null)
            .Where(link => link != null)
            .Cast<HyperlinkRef>()
            .ToList();
    }

    /// <summary>
    /// Gets all hyperlinks that reference the specified Content_ID.
    /// </summary>
    public IReadOnlyList<HyperlinkRef> GetHyperlinksByContentId(string contentId)
    {
        if (!_contentIdToHyperlinks.TryGetValue(contentId, out var hyperlinkIds))
            return Array.Empty<HyperlinkRef>();

        return hyperlinkIds
            .Select(id => _hyperlinks.TryGetValue(id, out var link) ? link : null)
            .Where(link => link != null)
            .Cast<HyperlinkRef>()
            .ToList();
    }

    /// <summary>
    /// Extracts all unique lookup IDs (both Document_IDs and Content_IDs) 
    /// for API resolution.
    /// </summary>
    public IReadOnlyCollection<string> ExtractLookupIds()
    {
        var lookupIds = new HashSet<string>(StringComparer.Ordinal);

        // Add all Document_IDs
        foreach (var documentId in _documentIdToHyperlinks.Keys)
        {
            lookupIds.Add(documentId);
        }

        // Add all Content_IDs
        foreach (var contentId in _contentIdToHyperlinks.Keys)
        {
            lookupIds.Add(contentId);
        }

        return lookupIds;
    }

    /// <summary>
    /// Gets all hyperlinks that are eligible for repair (contain eligible patterns).
    /// </summary>
    public IReadOnlyList<HyperlinkRef> GetEligibleHyperlinks()
    {
        return _hyperlinks.Values
            .Where(link => HyperlinkPatterns.IsEligibleUrl(link.Target))
            .ToList();
    }

    /// <summary>
    /// Clears all hyperlinks from the index.
    /// </summary>
    public void Clear()
    {
        _hyperlinks.Clear();
        _documentIdToHyperlinks.Clear();
        _contentIdToHyperlinks.Clear();
    }
}