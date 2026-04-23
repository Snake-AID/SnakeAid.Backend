using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.ExpertCertificate;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Implements;
using SnakeAid.Service.Implements;
using System.Reflection;

namespace SnakeAid.Tests.Integration;

public class ExpertCertificateServiceTests
{
    [Fact]
    public async Task CreateMyAsync_ShouldPersistCertificateAndAttachMedia()
    {
        var expertId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);
        await SeedMediaAsync(db, mediaId);

        var service = CreateService(db);

        var response = await service.CreateMyAsync(expertId, new CreateExpertCertificateRequest
        {
            CertificateName = "Clinical Toxicology",
            IssuingOrganization = "SnakeAid",
            IssueDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiryDate = new DateTime(2028, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportMediaIds = [mediaId]
        });

        Assert.Equal(VerificationStatus.Pending, response.VerificationStatus);
        Assert.Single(response.Media);
        Assert.Equal(mediaId, response.Media[0].Id);

        var persistedCertificate = await db.ExpertCertificates.SingleAsync(c => c.Id == response.Id);
        var persistedMedia = await db.ReportMedias.SingleAsync(m => m.Id == mediaId);
        Assert.Equal(response.Id, persistedMedia.ReferenceId);
        Assert.Equal(MediaReferenceType.ExpertCertificate, persistedMedia.ReferenceType);
        Assert.Equal(persistedMedia.MediaUrl, persistedCertificate.CertificateUrl);
    }

    [Fact]
    public async Task UpdateMyAsync_ShouldResetVerificationToPending_AndProfileToUnverified()
    {
        var expertId = Guid.NewGuid();
        var oldMediaId = Guid.NewGuid();
        var newMediaId = Guid.NewGuid();
        var certificateId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId, isVerified: true);
        await SeedMediaAsync(db, oldMediaId, certificateId);
        await SeedMediaAsync(db, newMediaId);
        db.ExpertCertificates.Add(new ExpertCertificate
        {
            Id = certificateId,
            ExpertId = expertId,
            CertificateName = "Old Cert",
            IssuingOrganization = "SnakeAid",
            IssueDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CertificateUrl = "https://cdn.example.com/old.pdf",
            VerificationStatus = VerificationStatus.Verified
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var response = await service.UpdateMyAsync(expertId, certificateId, new UpdateExpertCertificateRequest
        {
            CertificateName = "Updated Cert",
            IssuingOrganization = "SnakeAid Academy",
            IssueDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiryDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportMediaIds = [newMediaId]
        });

        Assert.Equal(VerificationStatus.Pending, response.VerificationStatus);
        Assert.Empty(response.RejectionReason);
        Assert.Single(response.Media);
        Assert.Equal(newMediaId, response.Media[0].Id);

        var profile = await db.ExpertProfiles.SingleAsync(p => p.AccountId == expertId);
        var oldMedia = await db.ReportMedias.SingleAsync(m => m.Id == oldMediaId);
        var newMedia = await db.ReportMedias.SingleAsync(m => m.Id == newMediaId);

        Assert.False(profile.IsVerified);
        Assert.Null(oldMedia.ReferenceId);
        Assert.Equal(certificateId, newMedia.ReferenceId);
    }

    [Fact]
    public async Task AdminCreateAsync_WithVerifiedCertificate_ShouldMarkExpertVerified()
    {
        var expertId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);
        await SeedMediaAsync(db, mediaId);

        var service = CreateService(db);

        var response = await service.AdminCreateAsync(new AdminCreateExpertCertificateRequest
        {
            ExpertId = expertId,
            CertificateName = "Board Certificate",
            IssuingOrganization = "SnakeAid Board",
            IssueDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportMediaIds = [mediaId],
            VerificationStatus = VerificationStatus.Verified
        });

        Assert.Equal(VerificationStatus.Verified, response.VerificationStatus);

        var profile = await db.ExpertProfiles.SingleAsync(p => p.AccountId == expertId);
        Assert.True(profile.IsVerified);
    }

    [Fact]
    public async Task AdminCreateAsync_WithInvalidVerificationStatus_ShouldThrowValidationException()
    {
        var expertId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        await using var db = CreateDbContext();
        await SeedExpertAsync(db, expertId);
        await SeedMediaAsync(db, mediaId);

        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.AdminCreateAsync(
            new AdminCreateExpertCertificateRequest
            {
                ExpertId = expertId,
                CertificateName = "Board Certificate",
                IssuingOrganization = "SnakeAid Board",
                IssueDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ReportMediaIds = [mediaId],
                VerificationStatus = (VerificationStatus)999
            }));

        Assert.Equal("Invalid VerificationStatus value.", exception.Reason);
    }

    private static ExpertCertificateService CreateService(SnakeAidDbContext db)
    {
        return new ExpertCertificateService(
            new UnitOfWork<SnakeAidDbContext>(db),
            NullLogger<ExpertCertificateService>.Instance);
    }

    private static async Task SeedExpertAsync(SnakeAidDbContext db, Guid expertId, bool isVerified = false)
    {
        db.Set<Account>().Add(new Account
        {
            Id = expertId,
            FullName = "Expert Tester",
            UserName = $"expert.{expertId:N}",
            NormalizedUserName = $"EXPERT.{expertId:N}",
            Email = $"expert-{expertId:N}@test.local",
            NormalizedEmail = $"EXPERT-{expertId:N}@TEST.LOCAL",
            IsActive = true,
            Role = AccountRole.Expert
        });

        db.ExpertProfiles.Add(new ExpertProfile
        {
            AccountId = expertId,
            Biography = "Expert bio",
            ConsultationFee = 100_000m,
            IsVerified = isVerified
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedMediaAsync(SnakeAidDbContext db, Guid mediaId, Guid? referenceId = null)
    {
        db.ReportMedias.Add(new ReportMedia
        {
            Id = mediaId,
            FileName = $"{mediaId}.pdf",
            MediaUrl = $"https://cdn.example.com/{mediaId}.pdf",
            ContentType = "application/pdf",
            FileSize = 1024,
            ReferenceId = referenceId,
            ReferenceType = MediaReferenceType.ExpertCertificate,
            Purpose = MediaPurpose.Evidence,
            RequiresAIProcessing = false
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

        var context = new ExpertCertificateSqliteDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class ExpertCertificateSqliteDbContext : SnakeAidDbContext
    {
        public ExpertCertificateSqliteDbContext(DbContextOptions<SnakeAidDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var keep = new HashSet<Type>
            {
                typeof(Account),
                typeof(ExpertProfile),
                typeof(ExpertCertificate),
                typeof(ReportMedia)
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
                entity.Ignore(a => a.MemberProfile);
                entity.Ignore(a => a.RescuerProfile);
            });

            modelBuilder.Entity<ExpertProfile>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.Ignore(e => e.Specializations);
                entity.HasOne(e => e.Account)
                    .WithOne()
                    .HasForeignKey<ExpertProfile>(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExpertCertificate>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Ignore(c => c.Media);
            });

            modelBuilder.Entity<ReportMedia>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Ignore(m => m.AIRecognitionResults);
            });
        }
    }
}
