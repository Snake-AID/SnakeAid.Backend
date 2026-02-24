using System.Text.Json.Serialization;

namespace SnakeAid.Service.Services.LocationIq.Models;

/// <summary>
/// Response from LocationIQ Matrix API
/// </summary>
public class MatrixResponse
{
    /// <summary>
    /// Array of distances in meters
    /// </summary>
    [JsonPropertyName("distances")]
    public double[][]? Distances { get; set; }

    /// <summary>
    /// Array of durations in seconds
    /// </summary>
    [JsonPropertyName("durations")]
    public double[][]? Durations { get; set; }

    /// <summary>
    /// Sources used for calculation
    /// </summary>
    [JsonPropertyName("sources")]
    public List<MatrixLocation>? Sources { get; set; }

    /// <summary>
    /// Destinations used for calculation
    /// </summary>
    [JsonPropertyName("destinations")]
    public List<MatrixLocation>? Destinations { get; set; }
}

public class MatrixLocation
{
    [JsonPropertyName("location")]
    public double[]? Location { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
