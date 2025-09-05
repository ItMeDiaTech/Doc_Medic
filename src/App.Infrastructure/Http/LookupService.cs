using System.Text.Json;
using App.Core;
using App.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace App.Infrastructure.Http;

/// <summary>
/// HTTP client implementation for the Power Automate lookup API.
/// Provides document metadata resolution with resilience policies.
/// </summary>
public class LookupService : ILookupService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LookupService> _logger;
    private readonly LookupServiceOptions _options;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    // In-memory cache for API results (valid for 1 hour or until app restart)
    private readonly Dictionary<string, CachedLookupResult> _cache = new();
    private readonly object _cacheLock = new();
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromHours(1);

    public LookupService(
        HttpClient httpClient, 
        IOptions<LookupServiceOptions> options, 
        ILogger<LookupService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ConfigureHttpClient();
        _resiliencePipeline = CreateResiliencePipeline();
    }

    public bool IsConfigured => 
        !string.IsNullOrEmpty(_options.BaseUrl) && 
        !string.IsNullOrEmpty(_options.Path);

    public async Task<IReadOnlyList<LookupResult>> ResolveAsync(
        IReadOnlyCollection<string> lookupIds, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lookupIds);

        if (!IsConfigured)
        {
            _logger.LogWarning("LookupService is not properly configured");
            return Array.Empty<LookupResult>();
        }

        if (!lookupIds.Any())
        {
            _logger.LogDebug("No lookup IDs provided");
            return Array.Empty<LookupResult>();
        }

        _logger.LogInformation("Resolving {Count} lookup IDs via Power Automate API", lookupIds.Count);

        // Check cache first and filter out cached results
        var uncachedIds = new List<string>();
        var cachedResults = new List<LookupResult>();

        lock (_cacheLock)
        {
            foreach (var id in lookupIds)
            {
                if (_cache.TryGetValue(id, out var cached) && !cached.IsExpired)
                {
                    cachedResults.Add(cached.Result);
                    _logger.LogTrace("Found cached result for ID: {Id}", id);
                }
                else
                {
                    uncachedIds.Add(id);
                    // Remove expired entries
                    if (cached?.IsExpired == true)
                    {
                        _cache.Remove(id);
                    }
                }
            }
        }

        _logger.LogDebug("Found {CachedCount} cached results, need to fetch {UncachedCount} from API", 
            cachedResults.Count, uncachedIds.Count);

        // If all results are cached, return them
        if (!uncachedIds.Any())
        {
            return cachedResults;
        }

        try
        {
            // Make API request for uncached IDs
            var apiResults = await MakeApiRequest(uncachedIds, cancellationToken);
            
            // Cache the new results
            CacheResults(apiResults);

            // Combine cached and new results
            var allResults = cachedResults.Concat(apiResults).ToList();

            _logger.LogInformation("Successfully resolved {ResultCount} of {RequestCount} lookup IDs", 
                allResults.Count, lookupIds.Count);

            return allResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve lookup IDs via API");
            
            // Return only cached results if API fails
            if (cachedResults.Any())
            {
                _logger.LogWarning("Returning {CachedCount} cached results due to API failure", cachedResults.Count);
                return cachedResults;
            }

            return Array.Empty<LookupResult>();
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Cannot test connection - service is not configured");
            return false;
        }

        try
        {
            _logger.LogDebug("Testing connection to Power Automate API");

            // Use a minimal test request with empty array
            var testIds = Array.Empty<string>();
            await MakeApiRequest(testIds, cancellationToken);

            _logger.LogInformation("Connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed");
            return false;
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Doc_Medic/1.0");
    }

    private ResiliencePipeline<HttpResponseMessage> CreateResiliencePipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnRetry = args =>
                {
                    _logger.LogWarning("Retry attempt {AttemptNumber} for API request due to: {Exception}", 
                        args.AttemptNumber, args.Outcome.Exception?.Message ?? "HTTP error");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(60),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => !response.IsSuccessStatusCode),
                OnOpened = args =>
                {
                    _logger.LogError("Circuit breaker opened due to failures");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed - service recovered");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(_options.TimeoutSeconds))
            .Build();
    }

    private async Task<IReadOnlyList<LookupResult>> MakeApiRequest(
        IReadOnlyCollection<string> lookupIds, 
        CancellationToken cancellationToken)
    {
        var requestPayload = new LookupRequest { Lookup_IDs = lookupIds.ToArray() };
        var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogTrace("Making API request with payload: {Payload}", jsonPayload);

        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _resiliencePipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.PostAsync(_options.Path, content, token);
        }, cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogTrace("API response received: {Response}", responseContent);

        var apiResponse = JsonSerializer.Deserialize<LookupResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });

        if (apiResponse?.Results == null)
        {
            _logger.LogWarning("API response was null or missing results array");
            return Array.Empty<LookupResult>();
        }

        // Convert API response to domain models
        var results = apiResponse.Results
            .Where(r => !string.IsNullOrEmpty(r.Document_ID) && !string.IsNullOrEmpty(r.Content_ID))
            .Select(r => new LookupResult(
                r.Title ?? string.Empty,
                r.Status ?? string.Empty,
                r.Content_ID,
                r.Document_ID))
            .ToList();

        _logger.LogDebug("Converted {Count} API results to domain models", results.Count);

        return results;
    }

    private void CacheResults(IReadOnlyList<LookupResult> results)
    {
        lock (_cacheLock)
        {
            var now = DateTime.UtcNow;
            foreach (var result in results)
            {
                // Cache by both Document_ID and Content_ID for dual lookup capability
                _cache[result.DocumentId] = new CachedLookupResult(result, now);
                _cache[result.ContentId] = new CachedLookupResult(result, now);
                
                _logger.LogTrace("Cached result for Document_ID: {DocumentId} and Content_ID: {ContentId}", 
                    result.DocumentId, result.ContentId);
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    // Internal data models for JSON serialization
    private record LookupRequest
    {
        public string[] Lookup_IDs { get; init; } = Array.Empty<string>();
    }

    private record LookupResponse
    {
        public LookupApiResult[]? Results { get; init; }
    }

    private record LookupApiResult
    {
        public string? Title { get; init; }
        public string? Status { get; init; }
        public string Content_ID { get; init; } = string.Empty;
        public string Document_ID { get; init; } = string.Empty;
    }

    private record CachedLookupResult(LookupResult Result, DateTime CachedAt)
    {
        public bool IsExpired => DateTime.UtcNow - CachedAt > TimeSpan.FromHours(1);
    }
}

/// <summary>
/// Configuration options for the LookupService.
/// </summary>
public class LookupServiceOptions
{
    /// <summary>
    /// Base URL for the Power Automate API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API path for the lookup endpoint.
    /// </summary>
    public string Path { get; set; } = "/lookup";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}