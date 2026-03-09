using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Expert;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    [Fact]
    public async Task GetExpertsAsync_ShouldFilterByIsOnline()
    {
        var onlineExpertId = Guid.NewGuid();
        var offlineExpertId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedExpertAsync(db, onlineExpertId, isOnline: true, fee: 150_000m, rating: 4.5m, ratingCount: 50);
        await SeedExpertAsync(db, offlineExpertId, isOnline: false, fee: 200_000m, rating: 4.9m, ratingCount: 100);

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var result = await service.GetExpertsAsync(new ExpertDirectoryQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            IsOnline = true
        });

        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(onlineExpertId, items[0].AccountId);
    }

    [Fact]
    public async Task GetExpertsAsync_ShouldSortByConsultationFeeDescending()
    {
        var cheaperExpertId = Guid.NewGuid();
        var expensiveExpertId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedExpertAsync(db, cheaperExpertId, isOnline: true, fee: 90_000m, rating: 4.7m, ratingCount: 30);
        await SeedExpertAsync(db, expensiveExpertId, isOnline: true, fee: 210_000m, rating: 4.2m, ratingCount: 20);

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var result = await service.GetExpertsAsync(new ExpertDirectoryQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "consultationFee",
            SortOrder = "desc"
        });

        var items = result.Items.ToList();
        Assert.True(items.Count >= 2);
        Assert.Equal(expensiveExpertId, items[0].AccountId);
        Assert.Equal(cheaperExpertId, items[1].AccountId);
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ExpertServiceSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedExpertAsync(
        SnakeAidDbContext db,
        Guid expertId,
        bool isOnline = true,
        decimal fee = 120_000m,
        decimal rating = 0m,
        int ratingCount = 0)
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
            ConsultationFee = fee,
            IsOnline = isOnline,
            Rating = rating,
            RatingCount = ratingCount
        });

        var specialization = await db.Set<Specialization>().FirstOrDefaultAsync(s => s.Name == "General");
        if (specialization == null)
        {
            specialization = new Specialization { Name = "General" };
            db.Set<Specialization>().Add(specialization);
            await db.SaveChangesAsync();
        }

        db.Set<ExpertSpecialization>().Add(new ExpertSpecialization
        {
            Id = Guid.NewGuid(),
            ExpertId = expertId,
            SpecializationId = specialization.Id
        });

        await db.SaveChangesAsync();
    }

    private sealed class ExpertServiceSqliteDbContext : SnakeAidDbContext
    {
        public ExpertServiceSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(ExpertProfile),
                typeof(ExpertTimeSlot),
                typeof(Specialization),
                typeof(ExpertSpecialization)
            };

            var dbSetEntityTypes = typeof(SnakeAidDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .Distinct();

            foreach (var type in dbSetEntityTypes)
            {
                if (!keep.Contains(type))
                {
                    modelBuilder.Ignore(type);
                }
            }

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(a => a.Id);
            });

            modelBuilder.Entity<ExpertProfile>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.HasOne(e => e.Account)
                    .WithOne()
                    .HasForeignKey<ExpertProfile>(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(e => e.Specializations)
                    .WithOne(es => es.Expert)
                    .HasForeignKey(es => es.ExpertId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Specialization>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<ExpertSpecialization>(entity =>
            {
                entity.HasKey(es => es.Id);
                entity.HasOne(es => es.Expert)
                    .WithMany(e => e.Specializations)
                    .HasForeignKey(es => es.ExpertId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(es => es.Specialization)
                    .WithMany(s => s.ExpertSpecializations)
                    .HasForeignKey(es => es.SpecializationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExpertTimeSlot>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Version)
                    .IsConcurrencyToken()
                    .ValueGeneratedNever()
                    .HasDefaultValue(0u);
                entity.HasIndex(s => new { s.ExpertId, s.StartTime, s.EndTime }).IsUnique();
                entity.HasOne(s => s.Expert)
                    .WithMany()
                    .HasForeignKey(s => s.ExpertId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
