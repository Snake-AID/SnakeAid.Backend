using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NetTopologySuite.Geometries;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data
{
    public class SnakeAidDbContext : IdentityDbContext<Account, ApplicationRole, Guid>
    {
        public SnakeAidDbContext(DbContextOptions<SnakeAidDbContext> options) : base(options)
        {
        }

        public DbSet<MemberProfile> MemberProfiles { get; set; }
        public DbSet<ExpertProfile> ExpertProfiles { get; set; }
        public DbSet<RescuerProfile> RescuerProfiles { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        public DbSet<AIModel> AIModels { get; set; }
        public DbSet<AISnakeClassMapping> AISnakeClassMappings { get; set; }
        public DbSet<Antivenom> Antivenoms { get; set; }
        public DbSet<AppNotification> AppNotifications { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<CatchingEnvironment> CatchingEnvironments { get; set; }
        public DbSet<CatchingMissionDetail> CatchingMissionDetails { get; set; }
        public DbSet<CatchingRequestDetail> CatchingRequestDetails { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CommunityReport> CommunityReports { get; set; }
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<ConsultationBooking> ConsultationBookings { get; set; }
        public DbSet<ConsultationPingRequest> ConsultationPingRequests { get; set; }
        public DbSet<ExpertCertificate> ExpertCertificates { get; set; }
        public DbSet<ExpertSpecialization> ExpertSpecializations { get; set; }
        public DbSet<ExpertTimeSlot> ExpertTimeSlots { get; set; }
        public DbSet<FilterOption> FilterOptions { get; set; }
        public DbSet<FilterQuestion> FilterQuestions { get; set; }
        public DbSet<FilterSnakeMapping> FilterSnakeMappings { get; set; }
        public DbSet<FirstAidGuideline> FirstAidGuidelines { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LibraryMedia> LibraryMedias { get; set; }
        public DbSet<LocationEvent> LocationEvents { get; set; }
        public DbSet<PaymentCard> PaymentCards { get; set; }
        public DbSet<ReportMedia> ReportMedias { get; set; }
        public DbSet<ReputationRawEvent> ReputationRawEvents { get; set; }
        public DbSet<ReputationRule> ReputationRules { get; set; }
        public DbSet<ReputationTransaction> ReputationTransactions { get; set; }
        public DbSet<RescueMission> RescueMissions { get; set; }
        public DbSet<RescueRequestSession> RescueRequestSessions { get; set; }
        public DbSet<RescuerRequest> RescuerRequests { get; set; }
        public DbSet<SnakeAIRecognitionResult> SnakeAIRecognitionResults { get; set; }
        public DbSet<SnakebiteIncident> SnakebiteIncidents { get; set; }
        public DbSet<SnakeCatchingMission> SnakeCatchingMissions { get; set; }
        public DbSet<SnakeCatchingRequest> SnakeCatchingRequests { get; set; }
        public DbSet<SnakeCatchingTariff> SnakeCatchingTariffs { get; set; }
        public DbSet<SnakeSpecies> SnakeSpecies { get; set; }
        public DbSet<SnakeSpeciesName> SnakeSpeciesNames { get; set; }
        public DbSet<SpeciesAntivenom> SpeciesAntivenoms { get; set; }
        public DbSet<SpeciesVenom> SpeciesVenoms { get; set; }
        public DbSet<SymptomConfig> SymptomConfigs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<TrackingSession> TrackingSessions { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TreatmentFacility> TreatmentFacilities { get; set; }
        public DbSet<UserFeedback> UserFeedbacks { get; set; }
        public DbSet<VenomType> VenomTypes { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletWithdraw> WalletWithdraws { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("SnakeAidDb");

            // Ignore UserData property from NetTopologySuite Point
            modelBuilder.Ignore<NetTopologySuite.Geometries.Point>();

            // Identity tables configuration
            modelBuilder.Entity<Account>().ToTable("Accounts");
            modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            // Apply configurations from assembly - relationships được config trong các file riêng
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SnakeAidDbContext).Assembly);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var now = DateTime.UtcNow;

            // Handle BaseEntity timestamps
            var baseEntityEntries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in baseEntityEntries)
            {
                var entity = (BaseEntity)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                    entity.CreatedAt = now;

                entity.UpdatedAt = now;
            }

            // Handle Account timestamps (IdentityUser doesn't inherit BaseEntity)
            var accountEntries = ChangeTracker
                .Entries<Account>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in accountEntries)
            {
                var entity = entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                    entity.CreatedAt = now;

                entity.UpdatedAt = now;
            }
        }
    }
}