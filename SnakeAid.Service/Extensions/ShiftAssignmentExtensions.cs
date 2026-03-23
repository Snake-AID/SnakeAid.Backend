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

            return assignment.ShiftStartLocal <= nowLocal && nowLocal <= assignment.ShiftEndLocal;
        }
    }
}
