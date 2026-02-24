namespace SnakeAid.Service.Services.LocationIq;

/// <summary>
/// Represents the state of an API key
/// </summary>
internal class ApiKeyState
{
    public string Key { get; }
    public DateTimeOffset LastUsedAt { get; set; }
    public DateTimeOffset? CooldownUntil { get; set; }
    public int ConsecutiveFailures { get; set; }
    public bool IsInCooldown => CooldownUntil.HasValue && DateTimeOffset.UtcNow < CooldownUntil.Value;

    public ApiKeyState(string key)
    {
        Key = key;
        LastUsedAt = DateTimeOffset.MinValue;
    }

    public void MarkSuccess()
    {
        ConsecutiveFailures = 0;
        CooldownUntil = null;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailure(int cooldownSeconds)
    {
        ConsecutiveFailures++;
        CooldownUntil = DateTimeOffset.UtcNow.AddSeconds(cooldownSeconds);
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public string MaskedKey =>
        Key.Length <= 8 ? "***" : $"{Key[..4]}***{Key[^4..]}";
}
