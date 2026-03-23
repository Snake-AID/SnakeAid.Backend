using SnakeAid.Core.Domains;
using SnakeAid.Service.Extensions;
using System;
using Xunit;

namespace SnakeAid.Tests.Unit
{
    public class ShiftAssignmentExtensionsTests
    {
        [Fact]
        public void IsOnDutyNow_ShouldReturnTrue_ForOvernightShiftAfterMidnight()
        {
            // Arrange
            var assignment = new ShiftAssignment
            {
                Date = new DateOnly(2026, 3, 22),
                Status = ShiftAssignmentStatus.Active,
                Shift = new WorkShift
                {
                    StartTime = TimeSpan.FromHours(22),
                    EndTime = TimeSpan.FromHours(6)
                }
            };

            var nowLocal = new DateTime(2026, 3, 23, 1, 0, 0, DateTimeKind.Local);
            var targetDate = DateOnly.FromDateTime(nowLocal);

            // Act
            var result = assignment.IsOnDutyNow(nowLocal, targetDate);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsOnDutyNow_ShouldReturnFalse_ForOvernightShiftBeforeStartOnSameDay()
        {
            // Arrange
            var assignment = new ShiftAssignment
            {
                Date = new DateOnly(2026, 3, 22),
                Status = ShiftAssignmentStatus.Active,
                Shift = new WorkShift
                {
                    StartTime = TimeSpan.FromHours(22),
                    EndTime = TimeSpan.FromHours(6)
                }
            };

            var nowLocal = new DateTime(2026, 3, 22, 20, 0, 0, DateTimeKind.Local);
            var targetDate = DateOnly.FromDateTime(nowLocal);

            // Act
            var result = assignment.IsOnDutyNow(nowLocal, targetDate);

            // Assert
            Assert.False(result);
        }
    }
}
