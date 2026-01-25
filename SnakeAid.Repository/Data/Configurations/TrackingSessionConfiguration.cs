using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class TrackingSessionConfiguration : IEntityTypeConfiguration<TrackingSession>
    {
        public void Configure(EntityTypeBuilder<TrackingSession> builder)
        {
            builder.ToTable("TrackingSessions");

            // Enum conversion
            builder.Property(t => t.SessionType)
                .HasConversion<int>()
                .IsRequired();

            // Indexes
            builder.HasIndex(t => t.SessionId)
                .HasDatabaseName("IX_TrackingSessions_SessionId");

            builder.HasIndex(t => t.SessionType)
                .HasDatabaseName("IX_TrackingSessions_SessionType");

            builder.HasIndex(t => t.IsActive)
                .HasDatabaseName("IX_TrackingSessions_IsActive");

            builder.HasIndex(t => new { t.SessionId, t.SessionType })
                .IsUnique()
                .HasDatabaseName("IX_TrackingSessions_SessionId_SessionType");
        }
    }
}
