using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Api.Controllers;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Meta;
using SnakeAid.Core.Requests.Expert;
using SnakeAid.Core.Responses.Expert;
using SnakeAid.Core.Responses.UserFeedback;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using System.Linq;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class ExpertControllerIntegrationTests
{
    [Fact]
    public async Task CreateBulkTimeSlots_ShouldReturnOk_AndPersistSlots()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var controller = BuildController(service, expertId, "Expert");

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

        var result = await controller.CreateBulkTimeSlots(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.True(payload.IsSuccess);
        var slotCount = await db.ExpertTimeSlots.CountAsync(s => s.ExpertId == expertId);
        Assert.Equal(2, slotCount);
    }

    [Fact]
    public async Task CreateBulkTimeSlots_WithNonUtcWeekStart_ShouldThrowValidationException()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var controller = BuildController(service, expertId, "Expert");

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

        await Assert.ThrowsAsync<ValidationException>(() => controller.CreateBulkTimeSlots(request));
    }

    [Fact]
    public async Task GetExpertReviews_ShouldReturnConsultationFeedbackOnly()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);
        await SeedFeedbackAsync(db, expertId);

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var controller = BuildController(service, Guid.Empty, "User");

        var result = await controller.GetExpertReviews(expertId, new PaginationRequest { PageNumber = 1, PageSize = 10 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<PagingResponse<UserFeedbackResponse>>>(ok.Value);
        Assert.NotNull(payload.Data);
        Assert.Single(payload.Data.Items);
        Assert.All(payload.Data.Items, i => Assert.Equal(FeedbackType.Consultation, i.Type));
    }

    [Fact]
    public async Task GetAvailableTimeSlots_ShouldReturnFutureAvailableSlotsOnly()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);
        db.ExpertTimeSlots.AddRange(
            new ExpertTimeSlot
            {
                Id = Guid.NewGuid(),
                ExpertId = expertId,
                StartTime = DateTime.UtcNow.AddHours(1),
                EndTime = DateTime.UtcNow.AddHours(1.5),
                Status = TimeSlotStatus.Available
            },
            new ExpertTimeSlot
            {
                Id = Guid.NewGuid(),
                ExpertId = expertId,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1.5),
                Status = TimeSlotStatus.Available
            },
            new ExpertTimeSlot
            {
                Id = Guid.NewGuid(),
                ExpertId = expertId,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(2.5),
                Status = TimeSlotStatus.Reserved
            });
        await db.SaveChangesAsync();

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var controller = BuildController(service, Guid.Empty, "User");

        var result = await controller.GetExpertTimeSlots(expertId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<IEnumerable<ExpertTimeSlotResponse>>>(ok.Value);
        Assert.NotNull(payload.Data);
        Assert.Single(payload.Data);
    }

    [Fact]
    public async Task GetExperts_ShouldFilterByIsOnlineAndSpecialization()
    {
        var onlineExpertId = Guid.NewGuid();
        var offlineExpertId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedExpertAsync(db, onlineExpertId, isOnline: true, fee: 150_000m, rating: 4.8m, ratingCount: 100, specializationName: "Ran Doc Viet Nam");
        await SeedExpertAsync(db, offlineExpertId, isOnline: false, fee: 120_000m, rating: 4.9m, ratingCount: 120, specializationName: "Ran Canh");

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var controller = BuildController(service, Guid.Empty, "User");

        var request = new ExpertDirectoryQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            IsOnline = true,
            Specialization = "Doc"
        };

        var result = await controller.GetExperts(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<PagingResponse<ExpertProfileResponse>>>(ok.Value);
        Assert.NotNull(payload.Data);
        var items = payload.Data.Items.ToList();
        Assert.Single(items);
        Assert.Equal(onlineExpertId, items[0].AccountId);
    }

    [Fact]
    public async Task GetExperts_ShouldSortByConsultationFeeAscending()
    {
        var expensiveExpertId = Guid.NewGuid();
        var cheapExpertId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expensiveExpertId, isOnline: true, fee: 300_000m, rating: 4.9m, ratingCount: 100, specializationName: "Rắn Độc");
        await SeedExpertAsync(db, cheapExpertId, isOnline: true, fee: 100_000m, rating: 4.1m, ratingCount: 20, specializationName: "Rắn Độc");

        var service = new ExpertService(new UnitOfWork<SnakeAidDbContext>(db), NullLogger<ExpertService>.Instance);
        var controller = BuildController(service, Guid.Empty, "User");

        var result = await controller.GetExperts(new ExpertDirectoryQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "consultationFee",
            SortOrder = "asc"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<PagingResponse<ExpertProfileResponse>>>(ok.Value);
        Assert.NotNull(payload.Data);
        var items = payload.Data.Items.ToList();
        Assert.True(items.Count >= 2);
        Assert.Equal(cheapExpertId, items[0].AccountId);
        Assert.Equal(expensiveExpertId, items[1].AccountId);
    }

    private static ExpertController BuildController(ExpertService service, Guid userId, string role)
    {
        var controller = new ExpertController(service);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
        return controller;
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ExpertControllerSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedExpertAsync(
        SnakeAidDbContext db,
        Guid expertId,
        bool isOnline = true,
        decimal fee = 150_000m,
        decimal rating = 0m,
        int ratingCount = 0,
        string specializationName = "General")
    {
        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Integration Expert",
            UserName = "integration.expert",
            NormalizedUserName = "INTEGRATION.EXPERT",
            Email = "integration.expert@test.local",
            NormalizedEmail = "INTEGRATION.EXPERT@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Integration bio",
            ConsultationFee = fee,
            IsOnline = isOnline,
            Rating = rating,
            RatingCount = ratingCount
        });

        var specialization = await db.Set<Specialization>().FirstOrDefaultAsync(s => s.Name == specializationName);
        if (specialization == null)
        {
            specialization = new Specialization
            {
                Name = specializationName
            };
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

    private static async Task SeedFeedbackAsync(SnakeAidDbContext db, Guid expertId)
    {
        var raterId = Guid.NewGuid();
        db.Set<Account>().Add(new Account
        {
            Id = raterId,
            FullName = "Rater User",
            UserName = "rater.user",
            NormalizedUserName = "RATER.USER",
            Email = "rater@test.local",
            NormalizedEmail = "RATER@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.User
        });

        db.UserFeedbacks.Add(new UserFeedback
        {
            Id = Guid.NewGuid(),
            RaterId = raterId,
            TargetUserId = expertId,
            ReferenceId = Guid.NewGuid(),
            Type = FeedbackType.Consultation,
            Rating = 5,
            Comments = "Consultation review"
        });

        db.UserFeedbacks.Add(new UserFeedback
        {
            Id = Guid.NewGuid(),
            RaterId = raterId,
            TargetUserId = expertId,
            ReferenceId = Guid.NewGuid(),
            Type = FeedbackType.Catching,
            Rating = 2,
            Comments = "Catching review"
        });

        await db.SaveChangesAsync();
    }

    private sealed class ExpertControllerSqliteDbContext : SnakeAidDbContext
    {
        public ExpertControllerSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
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
                typeof(UserFeedback),
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

            modelBuilder.Entity<UserFeedback>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasOne(f => f.Rater)
                    .WithMany()
                    .HasForeignKey(f => f.RaterId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(f => f.TargetUser)
                    .WithMany()
                    .HasForeignKey(f => f.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
