using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnakeAid.Core.Exceptions;
using SnakeAid.Service.Interfaces;
using SnakeAid.Service.Options;
using SnakeAid.Service.Services.LocationIq;
using SnakeAid.Service.Services.LocationIq.Models;
using System.Net;
using System.Text.Json;

namespace SnakeAid.Service.Implements;

/// <summary>
/// Implementation of LocationIQ service for distance calculation with API key rotation
/// </summary>
public class LocationIqService : ILocationIqService
{
    private readonly HttpClient _httpClient;
    private readonly LocationIqOptions _options;
    private readonly ILogger<LocationIqService> _logger;
    private readonly ApiKeyManager _keyManager;

    public LocationIqService(
        HttpClient httpClient,
        IOptions<LocationIqOptions> options,
        ILogger<LocationIqService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _keyManager = new ApiKeyManager(_options, logger);

        // Allow service to be created even without API keys
        // Will throw ExternalServiceException when called if no keys available
        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
        }

        if (_keyManager.HasKeys)
        {
            _logger.LogInformation(
                "LocationIQ service initialized with {KeyCount} API key(s) and rotation enabled",
                _options.ApiKeys.Length);
        }
    }

    public async Task<double> CalculateDistanceAsync(
        double sourceLng,
        double sourceLat,
        double destLng,
        double destLat)
    {
        // Check if any API keys are configured
        if (!_keyManager.HasKeys)
        {
            _logger.LogWarning("LocationIQ API keys are not configured. Cannot calculate distance.");
            throw new ExternalServiceException("LocationIQ API keys are not configured.");
        }

        var retryCount = 0;
        var maxRetries = Math.Min(_options.MaxRetryAttempts, _keyManager.AvailableKeysCount);
        Exception? lastException = null;

        // Retry with different keys if one fails
        while (retryCount < maxRetries)
        {
            var apiKey = _keyManager.GetNextAvailableKey();
            if (apiKey == null)
            {
                _logger.LogError("All API keys are in cooldown. Cannot calculate distance.");
                throw new ExternalServiceException(
                    "All LocationIQ API keys are temporarily unavailable (rate limited).",
                    lastException);
            }

            try
            {
                var distance = await CallMatrixApiAsync(apiKey, sourceLng, sourceLat, destLng, destLat);
                _keyManager.MarkSuccess(apiKey);
                return distance;
            }
            catch (ExternalServiceException ex) when (ex.Data.Contains("StatusCode"))
            {
                var statusCode = (HttpStatusCode)ex.Data["StatusCode"]!;
                
                if (statusCode == HttpStatusCode.TooManyRequests)
                {
                    // 429 - Rate limit hit, mark key as failed and try next
                    _keyManager.MarkFailure(apiKey, isRateLimit: true);
                    _logger.LogWarning(
                        "API key hit rate limit (429). Trying next available key. Attempt {Attempt}/{MaxRetries}",
                        retryCount + 1, maxRetries);
                    
                    lastException = ex;
                    retryCount++;
                    continue;
                }
                else if ((int)statusCode >= 500)
                {
                    // 5xx - Server error, mark as failed and retry
                    _keyManager.MarkFailure(apiKey, isRateLimit: false);
                    _logger.LogWarning(
                        "LocationIQ server error ({StatusCode}). Trying next available key. Attempt {Attempt}/{MaxRetries}",
                        statusCode, retryCount + 1, maxRetries);
                    
                    lastException = ex;
                    retryCount++;
                    continue;
                }
                
                // Other errors (4xx except 429), don't retry
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // All retries exhausted
        _logger.LogError(
            "Failed to calculate distance after {Attempts} attempts with different API keys",
            retryCount);
        
        throw new ExternalServiceException(
            $"Failed to calculate distance after {retryCount} attempts. All API keys failed or are rate limited.",
            lastException);
    }

    private async Task<double> CallMatrixApiAsync(
        string apiKey,
        double sourceLng,
        double sourceLat,
        double destLng,
        double destLat)
    {
        try
        {
            // LocationIQ Matrix API expects coordinates in format: lng,lat;lng,lat
            var coordinates = $"{sourceLng},{sourceLat};{destLng},{destLat}";

            // Build the request URL
            var requestUrl = $"/v1/matrix/driving/{coordinates}?key={apiKey}";

            _logger.LogDebug(
                "Calling LocationIQ Matrix API: Source({SourceLng},{SourceLat}) -> Dest({DestLng},{DestLat})",
                sourceLng, sourceLat, destLng, destLat);

            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "LocationIQ API error: StatusCode={StatusCode}, Content={Content}",
                    response.StatusCode, errorContent);

                var exception = new ExternalServiceException(
                    $"LocationIQ API returned error: {response.StatusCode}");
                exception.Data["StatusCode"] = response.StatusCode;
                throw exception;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var matrixResponse = JsonSerializer.Deserialize<MatrixResponse>(jsonContent);

            if (matrixResponse?.Distances == null ||
                matrixResponse.Distances.Length == 0 ||
                matrixResponse.Distances[0].Length == 0)
            {
                _logger.LogError("Invalid response from LocationIQ: {Response}", jsonContent);
                throw new ExternalServiceException("Invalid response from LocationIQ Matrix API");
            }

            // Distance is returned in meters, convert to kilometers
            var distanceInMeters = matrixResponse.Distances[0][0];
            var distanceInKm = distanceInMeters / 1000.0;

            _logger.LogInformation(
                "Distance calculated: {DistanceKm} km ({DistanceM} meters)",
                distanceInKm.ToString("F2"), distanceInMeters);

            return distanceInKm;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling LocationIQ Matrix API");
            throw new ExternalServiceException("Failed to connect to LocationIQ service", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "LocationIQ API request timeout");
            throw new ExternalServiceException("LocationIQ service timeout", ex);
        }
        catch (Exception ex) when (ex is not ExternalServiceException)
        {
            _logger.LogError(ex, "Unexpected error calculating distance");
            throw new ExternalServiceException("Failed to calculate distance", ex);
        }
    }

    public decimal CalculatePrice(double distanceInKm)
    {
        if (distanceInKm < 0)
        {
            throw new ArgumentException("Distance cannot be negative", nameof(distanceInKm));
        }

        // Round distance to 2 decimal places before calculating price
        var roundedDistance = Math.Round(distanceInKm, 2);
        var price = (decimal)roundedDistance * _options.PricePerKilometer;

        // Round price to nearest 1000 VND
        var roundedPrice = Math.Round(price / 1000, MidpointRounding.AwayFromZero) * 1000;

        _logger.LogInformation(
            "Price calculated: {Distance} km × {PricePerKm} = {Price} VND",
            roundedDistance, _options.PricePerKilometer, roundedPrice);

        return roundedPrice;
    }

    public async Task<(double distanceInKm, decimal priceInVnd)> CalculateDistanceAndPriceAsync(
        double sourceLng,
        double sourceLat,
        double destLng,
        double destLat)
    {
        var distance = await CalculateDistanceAsync(sourceLng, sourceLat, destLng, destLat);
        var price = CalculatePrice(distance);

        return (distance, price);
    }
}
