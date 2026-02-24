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

        // Validate and set BaseAddress
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogWarning("LocationIQ BaseUrl is not configured. Service will use fallback prices.");
        }
        else
        {
            try
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
                _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
                _logger.LogDebug("LocationIQ BaseAddress set to: {BaseUrl}", _options.BaseUrl);
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "Invalid LocationIQ BaseUrl: {BaseUrl}", _options.BaseUrl);
            }
        }

        if (_keyManager.HasKeys)
        {
            _logger.LogInformation(
                "LocationIQ service initialized with {KeyCount} API key(s) and rotation enabled",
                _options.ApiKeys.Length);
        }
        else
        {
            _logger.LogWarning("LocationIQ service initialized without API keys. Distance calculations will fail.");
        }
    }

    public async Task<double> CalculateDistanceAsync(
        double sourceLng,
        double sourceLat,
        double destLng,
        double destLat)
    {
        // Check if BaseAddress is configured
        if (_httpClient.BaseAddress == null)
        {
            _logger.LogWarning("LocationIQ BaseAddress is not set. Cannot calculate distance.");
            throw new ExternalServiceException("LocationIQ service is not properly configured (missing BaseUrl).");
        }

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
            // Validate coordinates before making the API call
            ValidateCoordinates(sourceLng, sourceLat, "source");
            ValidateCoordinates(destLng, destLat, "destination");

            // LocationIQ Directions API expects coordinates in format: lng,lat;lng,lat
            // Using Directions API instead of Matrix API due to better reliability
            var coordinates = $"{sourceLng},{sourceLat};{destLng},{destLat}";

            // Build the request URL - using Directions API
            var requestUrl = $"/v1/directions/driving/{coordinates}?key={apiKey}&overview=full";

            _logger.LogInformation(
                "Calling LocationIQ Directions API: Source({SourceLng},{SourceLat}) -> Dest({DestLng},{DestLat}), URL: {Url}",
                sourceLng, sourceLat, destLng, destLat, 
                requestUrl.Replace(apiKey, "***"));

            var response = await _httpClient.GetAsync(requestUrl);
            var jsonContent = await response.Content.ReadAsStringAsync();

            // Always log the response at Information level for debugging
            _logger.LogInformation(
                "LocationIQ Directions API Response: StatusCode={StatusCode}, BodyLength={Length}",
                response.StatusCode, jsonContent.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "LocationIQ API error: StatusCode={StatusCode}, URL={Url}, Content={Content}",
                    response.StatusCode, requestUrl.Replace(apiKey, "***"), jsonContent);

                var exception = new ExternalServiceException(
                    $"LocationIQ API returned error: {response.StatusCode}");
                exception.Data["StatusCode"] = response.StatusCode;
                throw exception;
            }
            
            var directionsResponse = JsonSerializer.Deserialize<DirectionsResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (directionsResponse == null)
            {
                _logger.LogError(
                    "Failed to deserialize LocationIQ Directions response. RawResponse: {Response}",
                    jsonContent);
                throw new ExternalServiceException("Failed to parse LocationIQ Directions API response");
            }

            // Check for routes array
            if (directionsResponse.Routes == null || directionsResponse.Routes.Count == 0)
            {
                _logger.LogError(
                    "LocationIQ returned no routes. " +
                    "Source: ({SourceLng},{SourceLat}), Dest: ({DestLng},{DestLat}), " +
                    "Code: {Code}, RawResponse: {Response}",
                    sourceLng, sourceLat, destLng, destLat,
                    directionsResponse.Code ?? "null",
                    jsonContent);
                    
                throw new ExternalServiceException(
                    "No route available between coordinates - possibly no road connection exists");
            }

            // Get distance from first route
            var route = directionsResponse.Routes[0];
            var distanceInMeters = route.Distance;
            
            // Check for valid distance value
            if (double.IsNaN(distanceInMeters) || double.IsInfinity(distanceInMeters) || distanceInMeters <= 0)
            {
                _logger.LogError(
                    "LocationIQ returned invalid or zero distance. " +
                    "Source: ({SourceLng},{SourceLat}), Dest: ({DestLng},{DestLat}), " +
                    "Distance: {Distance}, Code: {Code}",
                    sourceLng, sourceLat, destLng, destLat,
                    distanceInMeters,
                    directionsResponse.Code ?? "null");
                    
                throw new ExternalServiceException(
                    "No valid route found between the coordinates (distance is zero or invalid)");
            }
            
            var distanceInKm = distanceInMeters / 1000.0;

            _logger.LogInformation(
                "Distance calculated: {DistanceKm} km ({DistanceM} meters)",
                distanceInKm.ToString("F2"), distanceInMeters);

            return distanceInKm;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling LocationIQ Directions API");
            throw new ExternalServiceException("Failed to connect to LocationIQ service", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "LocationIQ API request timeout");
            throw new ExternalServiceException("LocationIQ service timeout", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LocationIQ API response");
            throw new ExternalServiceException("Invalid JSON response from LocationIQ service", ex);
        }
        catch (Exception ex) when (ex is not ExternalServiceException)
        {
            _logger.LogError(ex, "Unexpected error calculating distance");
            throw new ExternalServiceException("Failed to calculate distance", ex);
        }
    }

    private void ValidateCoordinates(double lng, double lat, string locationName)
    {
        if (lng < -180 || lng > 180)
        {
            throw new ArgumentException(
                $"Invalid {locationName} longitude: {lng}. Must be between -180 and 180.",
                nameof(lng));
        }

        if (lat < -90 || lat > 90)
        {
            throw new ArgumentException(
                $"Invalid {locationName} latitude: {lat}. Must be between -90 and 90.",
                nameof(lat));
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
