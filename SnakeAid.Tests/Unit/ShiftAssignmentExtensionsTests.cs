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
                ShiftStartLocal = new DateTime(2026, 3, 22, 22, 0, 0, DateTimeKind.Unspecified),
                ShiftEndLocal = new DateTime(2026, 3, 23, 6, 0, 0, DateTimeKind.Unspecified),
                Status = ShiftAssignmentStatus.Active,
            };

            var nowLocal = new DateTime(2026, 3, 23, 1, 0, 0, DateTimeKind.Local);

            // Act
            var result = assignment.IsOnDutyNow(nowLocal);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsOnDutyNow_ShouldReturnFalse_ForOvernightShiftBeforeStartOnSameDay()
        {
            // Arrange
            var assignment = new ShiftAssignment
            {
                ShiftStartLocal = new DateTime(2026, 3, 22, 22, 0, 0, DateTimeKind.Unspecified),
                ShiftEndLocal = new DateTime(2026, 3, 23, 6, 0, 0, DateTimeKind.Unspecified),
                Status = ShiftAssignmentStatus.Active,
            };

            var nowLocal = new DateTime(2026, 3, 22, 20, 0, 0, DateTimeKind.Local);

            // Act
            var result = assignment.IsOnDutyNow(nowLocal);

            // Assert
            Assert.False(result);
        }
    }
}
