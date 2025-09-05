using App.Domain;

namespace App.Core;

/// <summary>
/// Service for resolving document metadata via the Power Automate API.
/// Handles HTTP requests to lookup document information by ID.
/// </summary>
public interface ILookupService
{
    /// <summary>
    /// Resolves document metadata for the specified lookup IDs.
    /// </summary>
    /// <param name="lookupIds">Collection of Document_IDs and Content_IDs to resolve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of resolved document metadata.</returns>
    Task<IReadOnlyList<LookupResult>> ResolveAsync(IReadOnlyCollection<string> lookupIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the Power Automate API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the connection is successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current API configuration status.
    /// </summary>
    /// <returns>True if the service is properly configured, false otherwise.</returns>
    bool IsConfigured { get; }
}