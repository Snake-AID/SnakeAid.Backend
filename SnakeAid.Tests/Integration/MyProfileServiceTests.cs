using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.MyProfile;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class MyProfileServiceTests
{
    [Fact]
    public async Task UpdateMemberProfileAsync_ShouldUpdateAccountAndMemberFields()
    {
        var memberId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedAccountAsync(db, memberId, AccountRole.User);
        db.MemberProfiles.Add(new MemberProfile
        {
            AccountId = memberId,
            Rating = 4.2f,
            RatingCount = 8,
            EmergencyContacts = new List<string> { "0900000000" },
            HasUnderlyingDisease = false
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var response = await service.UpdateMemberProfileAsync(memberId, new UpdateMemberProfileRequest
        {
            FullName = "Member Edited",
            PhoneNumber = "0912345678",
            AvatarUrl = "https://cdn.example.com/member.png",
            EmergencyContacts = new List<string> { "0987654321", "0900111222" },
            HasUnderlyingDisease = true
        });

        Assert.Equal("Member Edited", response.FullName);
        Assert.Equal("0912345678", response.PhoneNumber);
        Assert.Equal("https://cdn.example.com/member.png", response.AvatarUrl);
        Assert.Equal(new[] { "0987654321", "0900111222" }, response.EmergencyContacts);
        Assert.True(response.HasUnderlyingDisease);
        Assert.Equal(4.2f, response.Rating);
        Assert.Equal(8, response.RatingCount);

        var account = await db.Set<Account>().SingleAsync(a => a.Id == memberId);
        var profile = await db.MemberProfiles.SingleAsync(p => p.AccountId == memberId);
        Assert.Equal("Member Edited", account.FullName);
        Assert.True(profile.HasUnderlyingDisease);
    }

    [Fact]
    public async Task UpdateExpertProfileAsync_ShouldUpdateAccountAndExpertEditableFields()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedAccountAsync(db, expertId, AccountRole.Expert);
        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Old bio",
            ConsultationFee = 100_000m,
            EmergencyConsultationFee = 150_000m,
            IsOnline = true,
            Rating = 4.8m,
            RatingCount = 22
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var response = await service.UpdateExpertProfileAsync(expertId, new UpdateExpertProfileRequest
        {
            FullName = "Expert Edited",
            PhoneNumber = "0922222222",
            AvatarUrl = "https://cdn.example.com/expert.png",
            Biography = "Updated expert bio",
            ScheduledConsultationFee = 250_000m,
            EmergencyConsultationFee = 300_000m
        });

        Assert.Equal("Expert Edited", response.FullName);
        Assert.Equal("Updated expert bio", response.Biography);
        Assert.Equal(250_000m, response.ScheduledConsultationFee);
        Assert.Equal(300_000m, response.EmergencyConsultationFee);
        Assert.True(response.IsOnline);
        Assert.Equal(4.8m, response.Rating);
        Assert.Equal(22, response.RatingCount);

        var profile = await db.ExpertProfiles.SingleAsync(p => p.AccountId == expertId);
        Assert.Equal("Updated expert bio", profile.Biography);
        Assert.Equal(250_000m, profile.ConsultationFee);
        Assert.Equal(300_000m, profile.EmergencyConsultationFee);
    }

    [Fact]
    public async Task UpdateExpertProfileAsync_WithoutScheduledFee_ShouldThrowValidationExceptionAndPreserveFees()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedAccountAsync(db, expertId, AccountRole.Expert);
        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Old bio",
            ConsultationFee = 100_000m,
            EmergencyConsultationFee = 150_000m
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateExpertProfileAsync(expertId, new UpdateExpertProfileRequest
        {
            FullName = "Expert Edited",
            Biography = "Updated expert bio"
        }));

        var profile = await db.ExpertProfiles.SingleAsync(p => p.AccountId == expertId);
        Assert.Equal(100_000m, profile.ConsultationFee);
        Assert.Equal(150_000m, profile.EmergencyConsultationFee);
    }

    [Fact]
    public async Task UpdateExpertProfileAsync_WithNullEmergencyFee_ShouldPersistScheduledFeeFallback()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedAccountAsync(db, expertId, AccountRole.Expert);
        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Old bio",
            ConsultationFee = 100_000m,
            EmergencyConsultationFee = 150_000m
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var response = await service.UpdateExpertProfileAsync(expertId, new UpdateExpertProfileRequest
        {
            FullName = "Expert Edited",
            Biography = "Updated expert bio",
            ScheduledConsultationFee = 250_000m,
            EmergencyConsultationFee = null
        });

        Assert.Equal(250_000m, response.ScheduledConsultationFee);
        Assert.Equal(250_000m, response.EmergencyConsultationFee);

        var profile = await db.ExpertProfiles.SingleAsync(p => p.AccountId == expertId);
        Assert.Equal(250_000m, profile.ConsultationFee);
        Assert.Equal(250_000m, profile.EmergencyConsultationFee);
    }

    [Fact]
    public async Task GetExpertProfileAsync_ShouldExposePersistedIsVerified()
    {
        var expertId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedAccountAsync(db, expertId, AccountRole.Expert);
        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Verified bio",
            ConsultationFee = 100_000m,
            EmergencyConsultationFee = 120_000m,
            IsVerified = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var response = await service.GetExpertProfileAsync(expertId);

        Assert.True(response.IsVerified);
    }

    [Fact]
    public void UpdateExpertProfileRequest_WithoutScheduledFee_ShouldFailModelValidation()
    {
        var request = new UpdateExpertProfileRequest
        {
            FullName = "Expert Edited",
            Biography = "Updated expert bio"
        };
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(UpdateExpertProfileRequest.ScheduledConsultationFee)));
    }

    [Fact]
    public async Task UpdateRescuerProfileAsync_ShouldUpdateAccountFieldsButKeepOperationalFieldsReadOnly()
    {
        var rescuerId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedAccountAsync(db, rescuerId, AccountRole.Rescuer);
        db.RescuerProfiles.Add(new RescuerProfile
        {
            AccountId = rescuerId,
            Type = RescuerType.Both,
            IsOnline = true,
            IsAvailable = true,
            Rating = 4.7m,
            RatingCount = 15,
            TotalMissions = 20,
            CompletedMissions = 18
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var response = await service.UpdateRescuerProfileAsync(rescuerId, new UpdateRescuerProfileRequest
        {
            FullName = "Rescuer Edited",
            PhoneNumber = "0933333333",
            AvatarUrl = "https://cdn.example.com/rescuer.png"
        });

        Assert.Equal("Rescuer Edited", response.FullName);
        Assert.Equal("0933333333", response.PhoneNumber);
        Assert.Equal("https://cdn.example.com/rescuer.png", response.AvatarUrl);
        Assert.Equal(RescuerType.Both, response.Type);
        Assert.True(response.IsOnline);
        Assert.True(response.IsAvailable);
        Assert.Equal(20, response.TotalMissions);
        Assert.Equal(18, response.CompletedMissions);
    }

    [Fact]
    public async Task GetMemberProfileAsync_WhenProfileMissing_ShouldThrowNotFound()
    {
        var memberId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedAccountAsync(db, memberId, AccountRole.User);
        var service = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMemberProfileAsync(memberId));
    }

    private static MyProfileService CreateService(SnakeAidDbContext db)
    {
        return new MyProfileService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<MyProfileService>.Instance);
    }

    private static async Task SeedAccountAsync(SnakeAidDbContext db, Guid accountId, AccountRole role)
    {
        db.Set<Account>().Add(new Account
        {
            Id = accountId,
            FullName = "Original Name",
            UserName = $"{role.ToString().ToLowerInvariant()}.{accountId:N}",
            NormalizedUserName = $"{role.ToString().ToUpperInvariant()}.{accountId:N}",
            Email = $"{role.ToString().ToLowerInvariant()}-{accountId:N}@test.local",
            NormalizedEmail = $"{role.ToString().ToUpperInvariant()}-{accountId:N}@TEST.LOCAL",
            PhoneNumber = "0900000000",
            AvatarUrl = "https://cdn.example.com/original.png",
            IsActive = true,
            Role = role
        });
        await db.SaveChangesAsync();
    }

    private static SnakeAidDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<SnakeAidDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new MyProfileSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class MyProfileSqliteDbContext : SnakeAidDbContext
    {
        public MyProfileSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(MemberProfile),
                typeof(ExpertProfile),
                typeof(RescuerProfile)
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

            modelBuilder.Entity<MemberProfile>(entity =>
            {
                entity.HasKey(p => p.AccountId);
                entity.Ignore(p => p.SnakebiteIncidents);
                entity.Ignore(p => p.SnakeCatchingRequests);
                entity.HasOne(p => p.Account)
                    .WithOne()
                    .HasForeignKey<MemberProfile>(p => p.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExpertProfile>(entity =>
            {
                entity.HasKey(p => p.AccountId);
                entity.Ignore(p => p.Specializations);
                entity.HasOne(p => p.Account)
                    .WithOne()
                    .HasForeignKey<ExpertProfile>(p => p.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RescuerProfile>(entity =>
            {
                entity.HasKey(p => p.AccountId);
                entity.Ignore(p => p.Missions);
                entity.Ignore(p => p.CatchingMissions);
                entity.Ignore(p => p.RescuerRequests);
                entity.Ignore(p => p.LastLocation);
                entity.HasOne(p => p.Account)
                    .WithOne()
                    .HasForeignKey<RescuerProfile>(p => p.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
