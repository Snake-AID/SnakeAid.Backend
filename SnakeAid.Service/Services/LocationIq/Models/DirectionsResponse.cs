using System.Text.Json.Serialization;

namespace SnakeAid.Service.Services.LocationIq.Models;

/// <summary>
/// Response from LocationIQ Directions API
/// </summary>
public class DirectionsResponse
{
    /// <summary>
    /// Response code (e.g., "Ok", "InvalidInput")
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Array of routes
    /// </summary>
    [JsonPropertyName("routes")]
    public List<DirectionsRoute>? Routes { get; set; }

    /// <summary>
    /// Waypoints used for routing
    /// </summary>
    [JsonPropertyName("waypoints")]
    public List<DirectionsWaypoint>? Waypoints { get; set; }
}

public class DirectionsRoute
{
    /// <summary>
    /// Total distance in meters
    /// </summary>
    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    /// <summary>
    /// Total duration in seconds
    /// </summary>
    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    /// <summary>
    /// Route geometry
    /// </summary>
    [JsonPropertyName("geometry")]
    public string? Geometry { get; set; }

    /// <summary>
    /// Array of legs (segments between waypoints)
    /// </summary>
    [JsonPropertyName("legs")]
    public List<DirectionsLeg>? Legs { get; set; }
}

public class DirectionsLeg
{
    /// <summary>
    /// Distance in meters
    /// </summary>
    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    /// <summary>
    /// Summary description
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public class DirectionsWaypoint
{
    /// <summary>
    /// Waypoint location [lng, lat]
    /// </summary>
    [JsonPropertyName("location")]
    public double[]? Location { get; set; }

    /// <summary>
    /// Name of the location
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Distance from input coordinate (snapping distance)
    /// </summary>
    [JsonPropertyName("distance")]
    public double Distance { get; set; }
}
