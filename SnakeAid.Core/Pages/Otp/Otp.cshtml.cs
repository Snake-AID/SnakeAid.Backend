using EzyFix.API.Pages.Shared;
using EzyFix.DAL.Data;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace EzyFix.API.Pages.Admin;

public class OtpCenterModel : PageModel
{
    private readonly EzyFixDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "admin-otp-snapshot";

    [BindProperty(SupportsGet = true)]
    public bool Refresh { get; set; }

    public OtpCenterModel(EzyFixDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public IReadOnlyList<OtpSummary> Otps { get; private set; } = Array.Empty<OtpSummary>();
    public int ActiveCount { get; private set; }
    public int ExpiredCount { get; private set; }
    public DateTime RetrievedAt { get; private set; } = DateTime.UtcNow;

    public async Task<IActionResult> OnGetAsync()
    {
        if (Refresh)
        {
            _cache.Remove(CacheKey);
        }

        var snapshot = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
            var retrievedAt = DateTime.UtcNow;

            var otps = await _dbContext.Otps
                .AsNoTracking()
                .OrderByDescending(o => o.ExpirationTime)
                .Select(o => new OtpSummary(
                    o.Id,
                    o.Email,
                    o.OtpCode,
                    o.ExpirationTime,
                    o.AttemptLeft,
                    o.UserId,
                    o.User != null ? o.User.FirstName : null,
                    o.User != null ? o.User.LastName : null))
                .ToListAsync();

            var active = otps.Count(o => o.ExpirationTime > retrievedAt);
            var expired = otps.Count - active;

            var etag = PageCacheHelper.ComputeETag(new { retrievedAt, active, expired, count = otps.Count });

            return new OtpSnapshot(otps, active, expired, retrievedAt, etag);
        });

        RetrievedAt = snapshot.RetrievedAt;
        Otps = snapshot.Otps;
        ActiveCount = snapshot.ActiveCount;
        ExpiredCount = snapshot.ExpiredCount;

        if (PageCacheHelper.TrySetCacheHeaders(HttpContext, snapshot.ETag, snapshot.RetrievedAt, Refresh, out var cached))
        {
            return cached;
        }

        return Page();
    }

    public string FormatFullName(string? firstName, string? lastName)
    {
        var first = firstName?.Trim();
        var last = lastName?.Trim();
        if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
        {
            return "-";
        }

        if (string.IsNullOrEmpty(first))
        {
            return last!;
        }

        if (string.IsNullOrEmpty(last))
        {
            return first;
        }

        return $"{first} {last}".Trim();
    }

    public string AttemptTone(int attempts) => attempts switch
    {
        >= 3 => "error",
        2 => "warn",
        _ => "info"
    };

    public record OtpSummary(
        Guid Id,
        string Email,
        string OtpCode,
        DateTime ExpirationTime,
        int AttemptLeft,
        Guid? UserId,
        string? UserFirstName,
        string? UserLastName);

    private record OtpSnapshot(
        IReadOnlyList<OtpSummary> Otps,
        int ActiveCount,
        int ExpiredCount,
        DateTime RetrievedAt,
        string ETag);
}
