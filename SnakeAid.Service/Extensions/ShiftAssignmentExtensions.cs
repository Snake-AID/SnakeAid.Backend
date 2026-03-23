using System;
using SnakeAid.Core.Domains;

namespace SnakeAid.Service.Extensions
{
    public static class ShiftAssignmentExtensions
    {
        public static bool IsOnDutyNow(this ShiftAssignment assignment, DateTime nowLocal)
        {
            if (assignment == null)
            {
                return false;
            }

            if (assignment.Status == ShiftAssignmentStatus.Completed
                || assignment.Status == ShiftAssignmentStatus.Cancelled
                || assignment.Status == ShiftAssignmentStatus.NoShow)
            {
                return false;
            }

            // Ensure consistent DateTime.Kind for comparison
            // ShiftStartLocal/ShiftEndLocal are stored with Kind=Unspecified (local time without timezone)
            // nowLocal should also be treated as local time for comparison
            var shiftStart = DateTime.SpecifyKind(assignment.ShiftStartLocal, DateTimeKind.Unspecified);
            var shiftEnd = DateTime.SpecifyKind(assignment.ShiftEndLocal, DateTimeKind.Unspecified);
            var now = DateTime.SpecifyKind(nowLocal, DateTimeKind.Unspecified);

            return shiftStart <= now && now <= shiftEnd;
        }
    }
}
