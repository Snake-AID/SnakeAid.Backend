namespace SnakeAid.Service.Options;

public sealed class LocationIqOptions
{
    public const string SectionName = "LocationIq";

    /// <summary>
    /// Base URL for LocationIQ API. Default: https://us1.locationiq.com
    /// </summary>
    public string BaseUrl { get; set; } = "https://us1.locationiq.com";

    /// <summary>
    /// LocationIQ API keys. Multiple keys enable rotation when one hits quota limit.
    /// </summary>
    public string[] ApiKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// HttpClient timeout in seconds. Default: 30
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Price per kilometer in VND. Default: 10000
    /// </summary>
    public decimal PricePerKilometer { get; set; } = 10000;

    /// <summary>
    /// Cooldown duration in seconds when a key hits rate limit. Default: 60
    /// </summary>
    public int KeyCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum retry attempts per request. Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
