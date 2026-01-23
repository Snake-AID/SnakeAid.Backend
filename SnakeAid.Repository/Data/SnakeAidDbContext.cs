using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("SnakeAidDb");

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