using System.Runtime.InteropServices;

namespace SnakeAid.Core.Utils
{
    public static class AppTime
    {
        private static readonly Lazy<TimeZoneInfo> _businessTimeZone = new(ResolveBusinessTimeZone);

        public static TimeZoneInfo BusinessTimeZone => _businessTimeZone.Value;

        public static DateTime UtcNow => DateTime.UtcNow;

        public static DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, BusinessTimeZone);

        public static DateOnly TodayLocalDate => DateOnly.FromDateTime(NowLocal);

        private static TimeZoneInfo ResolveBusinessTimeZone()
        {
            var configured = Environment.GetEnvironmentVariable("SNAKEAID_TIMEZONE");
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(configured))
            {
                candidates.Add(configured);
            }

            candidates.Add(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh");

            foreach (var id in candidates.Distinct())
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch
                {
                    // Fallback to next candidate.
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}