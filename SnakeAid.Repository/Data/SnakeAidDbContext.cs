namespace SnakeAid.Repository.Data
{
    public class SnakeAidDbContext : DbContext
    {
        public SnakeAidDbContext(DbContextOptions<SnakeAidDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<MemberProfile> MemberProfiles { get; set; }
        public DbSet<ExpertProfile> ExpertProfiles { get; set; }
        public DbSet<RescuerProfile> RescuerProfiles { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("SnakeAidDb");

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
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var now = DateTime.UtcNow;
                var entity = (BaseEntity)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added) entity.CreatedAt = now;

                entity.UpdatedAt = now;
            }
        }
    }
}