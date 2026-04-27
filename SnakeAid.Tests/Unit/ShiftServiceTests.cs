using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using Xunit;

namespace SnakeAid.Tests.Unit
{
    public class ShiftServiceTests
    {
        [Fact]
        public async Task GetAssignmentsByDateAsync_ShouldIncludeOvernightShiftFromPreviousDay()
        {
            await using var db = CreateDbContext();

            var shift = new WorkShift
            {
                Id = Guid.NewGuid(),
                Name = "Night",
                StartTime = TimeSpan.FromHours(22),
                EndTime = TimeSpan.FromHours(6),
                RequiredRescuers = 1,
                IsActive = true
            };

            var rescuerId = Guid.NewGuid();
            var account = new Account
            {
                Id = rescuerId,
                UserName = "rescue.user",
                NormalizedUserName = "RESCUE.USER",
                Email = "rescue.user@example.com",
                NormalizedEmail = "RESCUE.USER@EXAMPLE.COM",
                FullName = "Rescue User",
                Role = AccountRole.Rescuer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var rescuer = new RescuerProfile
            {
                AccountId = rescuerId,
                IsOnline = true,
                Rating = 4.5m,
                RatingCount = 1,
                Type = RescuerType.Emergency,
                IsAvailable = true,
                TotalMissions = 0,
                CompletedMissions = 0,
                Account = account
            };

            var overnightAssignment = new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                RescuerId = rescuerId,
                ShiftId = shift.Id,
                ShiftStartLocal = new DateTime(2026, 3, 22, 22, 0, 0, DateTimeKind.Unspecified),
                ShiftEndLocal = new DateTime(2026, 3, 23, 6, 0, 0, DateTimeKind.Unspecified),
                Status = ShiftAssignmentStatus.Scheduled,
                Notes = "Overnight assignment"
            };

            var dayAssignment = new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                RescuerId = rescuerId,
                ShiftId = shift.Id,
                ShiftStartLocal = new DateTime(2026, 3, 23, 8, 0, 0, DateTimeKind.Unspecified),
                ShiftEndLocal = new DateTime(2026, 3, 23, 16, 0, 0, DateTimeKind.Unspecified),
                Status = ShiftAssignmentStatus.Scheduled,
                Notes = "Day assignment"
            };

            db.Add(account);
            db.Add(rescuer);
            db.Add(shift);
            db.Add(overnightAssignment);
            db.Add(dayAssignment);
            await db.SaveChangesAsync();

            var service = new ShiftService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ShiftService>.Instance);

            var result = await service.GetAssignmentsByDateAsync(new DateOnly(2026, 3, 23));

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == overnightAssignment.Id);
            Assert.Contains(result, r => r.Id == dayAssignment.Id);
        }

        [Fact]
        public async Task CloneAssignmentsToNextWeekAsync_ShouldCopyScheduledAssignmentsAndSkipExistingTargetAssignments()
        {
            await using var db = CreateDbContext();

            var shift = new WorkShift
            {
                Id = Guid.NewGuid(),
                Name = "Day",
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(16),
                RequiredRescuers = 1,
                IsActive = true
            };

            var rescuerId = Guid.NewGuid();
            var account = new Account
            {
                Id = rescuerId,
                UserName = "rescue.user",
                NormalizedUserName = "RESCUE.USER",
                Email = "rescue.user@example.com",
                NormalizedEmail = "RESCUE.USER@EXAMPLE.COM",
                FullName = "Rescue User",
                Role = AccountRole.Rescuer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var rescuer = new RescuerProfile
            {
                AccountId = rescuerId,
                IsOnline = true,
                Rating = 4.5m,
                RatingCount = 1,
                Type = RescuerType.Emergency,
                IsAvailable = true,
                TotalMissions = 0,
                CompletedMissions = 0,
                Account = account
            };

            var sourceAssignment = new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                RescuerId = rescuerId,
                ShiftId = shift.Id,
                ShiftStartLocal = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Unspecified),
                ShiftEndLocal = new DateTime(2026, 4, 20, 16, 0, 0, DateTimeKind.Unspecified),
                Status = ShiftAssignmentStatus.Scheduled,
                Notes = "Source assignment"
            };

            var existingTargetAssignment = new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                RescuerId = rescuerId,
                ShiftId = shift.Id,
                ShiftStartLocal = new DateTime(2026, 4, 27, 8, 0, 0, DateTimeKind.Unspecified),
                ShiftEndLocal = new DateTime(2026, 4, 27, 16, 0, 0, DateTimeKind.Unspecified),
                Status = ShiftAssignmentStatus.Scheduled,
                Notes = "Already exists in target week"
            };

            db.Add(account);
            db.Add(rescuer);
            db.Add(shift);
            db.Add(sourceAssignment);
            db.Add(existingTargetAssignment);
            await db.SaveChangesAsync();

            var service = new ShiftService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ShiftService>.Instance);
            var result = await service.CloneAssignmentsToNextWeekAsync(new DateOnly(2026, 4, 22));

            Assert.Single(result);
            Assert.Equal(new DateTime(2026, 4, 29, 8, 0, 0, DateTimeKind.Unspecified), result[0].ShiftStartLocal);
            Assert.Equal(ShiftAssignmentStatus.Scheduled, result[0].Status);
            Assert.Null(result[0].CheckInAtUtc);
            Assert.Null(result[0].CheckOutAtUtc);
        }

        private static SnakeAidDbContext CreateDbContext()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new SnakeAidDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}
