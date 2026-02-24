using Microsoft.Extensions.Logging;
using SnakeAid.Service.Options;
using System.Collections.Concurrent;

namespace SnakeAid.Service.Services.LocationIq;

/// <summary>
/// Manages API key rotation and circuit breaking for LocationIQ
/// </summary>
internal class ApiKeyManager
{
    private readonly ConcurrentDictionary<string, ApiKeyState> _keyStates;
    private readonly LocationIqOptions _options;
    private readonly ILogger _logger;
    private int _currentIndex = 0;
    private readonly object _lock = new();

    public ApiKeyManager(LocationIqOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
        _keyStates = new ConcurrentDictionary<string, ApiKeyState>();

        // Initialize key states
        if (options.ApiKeys != null && options.ApiKeys.Length > 0)
        {
            foreach (var key in options.ApiKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                _keyStates.TryAdd(key, new ApiKeyState(key));
            }
        }

        if (_keyStates.Count > 0)
        {
            _logger.LogInformation("Initialized {Count} LocationIQ API key(s) for rotation", _keyStates.Count);
        }
    }

    public bool HasKeys => _keyStates.Count > 0;

    public int AvailableKeysCount => _keyStates.Values.Count(s => !s.IsInCooldown);

    /// <summary>
    /// Get next available API key using round-robin with circuit breaker
    /// </summary>
    public string? GetNextAvailableKey()
    {
        if (_keyStates.Count == 0)
        {
            return null;
        }

        var keys = _keyStates.Values.ToList();
        var attempts = 0;
        var maxAttempts = keys.Count * 2; // Try all keys at least twice

        while (attempts < maxAttempts)
        {
            lock (_lock)
            {
                _currentIndex = (_currentIndex + 1) % keys.Count;
            }

            var keyState = keys[_currentIndex];

            // Check if key is available (not in cooldown)
            if (!keyState.IsInCooldown)
            {
                _logger.LogDebug(
                    "Selected API key {MaskedKey} (index: {Index}, failures: {Failures})",
                    keyState.MaskedKey, _currentIndex, keyState.ConsecutiveFailures);
                return keyState.Key;
            }

            var remainingCooldown = keyState.CooldownUntil!.Value - DateTimeOffset.UtcNow;
            _logger.LogDebug(
                "API key {MaskedKey} is in cooldown for {RemainingSeconds}s",
                keyState.MaskedKey, remainingCooldown.TotalSeconds);

            attempts++;
        }

        // All keys are in cooldown
        _logger.LogWarning("All {Count} API keys are in cooldown", keys.Count);
        return null;
    }

    /// <summary>
    /// Mark a successful API call
    /// </summary>
    public void MarkSuccess(string apiKey)
    {
        if (_keyStates.TryGetValue(apiKey, out var state))
        {
            state.MarkSuccess();
            _logger.LogDebug("API key {MaskedKey} marked as successful", state.MaskedKey);
        }
    }

    /// <summary>
    /// Mark a failed API call (rate limit or error)
    /// </summary>
    public void MarkFailure(string apiKey, bool isRateLimit = false)
    {
        if (_keyStates.TryGetValue(apiKey, out var state))
        {
            state.MarkFailure(_options.KeyCooldownSeconds);

            _logger.LogWarning(
                "API key {MaskedKey} marked as failed (rate limit: {IsRateLimit}). " +
                "Consecutive failures: {Failures}. Cooldown until: {CooldownUntil}",
                state.MaskedKey,
                isRateLimit,
                state.ConsecutiveFailures,
                state.CooldownUntil);
        }
    }

    /// <summary>
    /// Get status of all keys (for debugging)
    /// </summary>
    public Dictionary<string, object> GetKeysStatus()
    {
        return _keyStates.Values.ToDictionary(
            s => s.MaskedKey,
            s => (object)new
            {
                IsInCooldown = s.IsInCooldown,
                ConsecutiveFailures = s.ConsecutiveFailures,
                CooldownUntil = s.CooldownUntil,
                LastUsedAt = s.LastUsedAt
            });
    }
}
