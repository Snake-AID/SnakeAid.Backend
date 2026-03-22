using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Service.Extensions
{
    public static class ShiftAssignmentExtensions
    {
        public static bool IsOnDutyNow(this ShiftAssignment assignment, DateTime nowUtc, DateOnly targetDate)
        {
            // `nowUtc` should be converted to local timezone before calling for shift comparisons.
            // This avoids incorrect results for night shifts when server time is UTC and shifts are local clock periods.
            if (assignment == null)
                return false;

            if (assignment.Status == ShiftAssignmentStatus.Completed
                || assignment.Status == ShiftAssignmentStatus.Cancelled
                || assignment.Status == ShiftAssignmentStatus.NoShow)
            {
                return false;
            }

            if (assignment.Shift == null)
            {
                return false;
            }

            var nowTime = nowUtc.TimeOfDay;
            var isOvernight = assignment.Shift.EndTime < assignment.Shift.StartTime;

            if (assignment.Date == targetDate)
            {
                return IsTimeWithinShiftWindow(nowTime, assignment.Shift.StartTime, assignment.Shift.EndTime);
            }

            // Support overnight shifts crossing midnight: e.g. 22:00 (day N) -> 06:00 (day N+1)
            if (isOvernight && assignment.Date == targetDate.AddDays(-1))
            {
                // On the next day, we only care about the time after midnight until shift end.
                return nowTime <= assignment.Shift.EndTime;
            }

            return false;
        }

        public static bool IsTimeWithinShiftWindow(TimeSpan current, TimeSpan start, TimeSpan end)
        {
            if (start == end)
            {
                return true;
            }

            if (end < start)
            {
                return current >= start || current <= end;
            }

            return current >= start && current <= end;
        }
    }
}
