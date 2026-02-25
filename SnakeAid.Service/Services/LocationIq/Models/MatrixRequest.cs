namespace SnakeAid.Service.Services.LocationIq.Models;

/// <summary>
/// Request model for LocationIQ Matrix API
/// </summary>
public class MatrixRequest
{
    /// <summary>
    /// Source coordinates (rescuer location): [longitude, latitude]
    /// </summary>
    public double[] Source { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Destination coordinates (request location): [longitude, latitude]
    /// </summary>
    public double[] Destination { get; set; } = Array.Empty<double>();
}
