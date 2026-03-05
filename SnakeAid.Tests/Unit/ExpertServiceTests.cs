using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Expert;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;

namespace SnakeAid.Tests.Unit;

public class ExpertServiceTests
{
    [Fact]
    public async Task CreateBulkTimeSlotsAsync_ShouldDeduplicateOverlappingBlocksInPayload()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var request = new BulkTimeSlotRequest
        {
            WeekStartDate = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
            Days =
            {
                new DayBlockRequest
                {
                    DayOfWeek = DayOfWeek.Monday,
                    TimeBlocks =
                    {
                        new TimeBlockRequest { StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9) },
                        new TimeBlockRequest { StartTime = TimeSpan.FromHours(8.5), EndTime = TimeSpan.FromHours(10) }
                    }
                }
            }
        };

        await service.CreateBulkTimeSlotsAsync(expertId, request);

        var slots = db.ExpertTimeSlots.Where(s => s.ExpertId == expertId).OrderBy(s => s.StartTime).ToList();
        Assert.Equal(4, slots.Count);
        Assert.Equal(4, slots.Select(s => (s.StartTime, s.EndTime)).Distinct().Count());
    }

    [Fact]
    public async Task CreateBulkTimeSlotsAsync_ShouldSkipExistingSlots()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);

        db.ExpertTimeSlots.Add(new ExpertTimeSlot
        {
            Id = Guid.NewGuid(),
            ExpertId = expertId,
            StartTime = new DateTime(2026, 3, 9, 8, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 3, 9, 8, 30, 0, DateTimeKind.Utc),
            Status = TimeSlotStatus.Available,
            Version = 0
        });
        await db.SaveChangesAsync();

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var request = new BulkTimeSlotRequest
        {
            WeekStartDate = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
            Days =
            {
                new DayBlockRequest
                {
                    DayOfWeek = DayOfWeek.Monday,
                    TimeBlocks =
                    {
                        new TimeBlockRequest { StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9) }
                    }
                }
            }
        };

        await service.CreateBulkTimeSlotsAsync(expertId, request);

        var slots = db.ExpertTimeSlots.Where(s => s.ExpertId == expertId).OrderBy(s => s.StartTime).ToList();
        Assert.Equal(2, slots.Count);
    }

    [Fact]
    public async Task CreateBulkTimeSlotsAsync_ShouldThrowValidationException_WhenWeekStartIsNotUtc()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var request = new BulkTimeSlotRequest
        {
            WeekStartDate = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Unspecified),
            Days =
            {
                new DayBlockRequest
                {
                    DayOfWeek = DayOfWeek.Monday,
                    TimeBlocks =
                    {
                        new TimeBlockRequest { StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9) }
                    }
                }
            }
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateBulkTimeSlotsAsync(expertId, request));
    }

    [Fact]
    public void ExpertTimeSlot_Model_ShouldHaveUniqueCompositeIndex()
    {
        using var db = CreateDbContext();
        var entityType = db.Model.FindEntityType(typeof(ExpertTimeSlot));
        Assert.NotNull(entityType);

        var hasUniqueCompositeIndex = entityType!
            .GetIndexes()
            .Any(index =>
                index.IsUnique &&
                index.Properties.Count == 3 &&
                index.Properties.Any(p => p.Name == nameof(ExpertTimeSlot.ExpertId)) &&
                index.Properties.Any(p => p.Name == nameof(ExpertTimeSlot.StartTime)) &&
                index.Properties.Any(p => p.Name == nameof(ExpertTimeSlot.EndTime)));

        Assert.True(hasUniqueCompositeIndex);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseInMemoryDatabase($"ExpertServiceTests_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new SnakeAidDbContext(options);
    }

    private static async Task SeedExpertAsync(SnakeAidDbContext db, Guid expertId)
    {
        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Unit Test Expert",
            UserName = "unit.expert",
            NormalizedUserName = "UNIT.EXPERT",
            Email = "unit.expert@test.local",
            NormalizedEmail = "UNIT.EXPERT@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Unit test bio",
            ConsultationFee = 120_000m,
            IsOnline = true
        });

        await db.SaveChangesAsync();
    }
}
