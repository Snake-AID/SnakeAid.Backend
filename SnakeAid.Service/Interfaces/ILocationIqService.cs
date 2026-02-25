using SnakeAid.Service.Services.LocationIq.Models;

namespace SnakeAid.Service.Interfaces;

/// <summary>
/// Service for calculating distance and price using LocationIQ Matrix API
/// </summary>
public interface ILocationIqService
{
    /// <summary>
    /// Calculate distance between two coordinates
    /// </summary>
    /// <param name="sourceLng">Source longitude (rescuer)</param>
    /// <param name="sourceLat">Source latitude (rescuer)</param>
    /// <param name="destLng">Destination longitude (request)</param>
    /// <param name="destLat">Destination latitude (request)</param>
    /// <returns>Distance in kilometers</returns>
    Task<double> CalculateDistanceAsync(
        double sourceLng,
        double sourceLat,
        double destLng,
        double destLat);

    /// <summary>
    /// Calculate price based on distance
    /// </summary>
    /// <param name="distanceInKm">Distance in kilometers</param>
    /// <returns>Price in VND</returns>
    decimal CalculatePrice(double distanceInKm);

    /// <summary>
    /// Calculate both distance and price
    /// </summary>
    /// <param name="sourceLng">Source longitude (rescuer)</param>
    /// <param name="sourceLat">Source latitude (rescuer)</param>
    /// <param name="destLng">Destination longitude (request)</param>
    /// <param name="destLat">Destination latitude (request)</param>
    /// <returns>Tuple of (distanceInKm, priceInVnd)</returns>
    Task<(double distanceInKm, decimal priceInVnd)> CalculateDistanceAndPriceAsync(
        double sourceLng,
        double sourceLat,
        double destLng,
        double destLat);
}
