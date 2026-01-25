using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class RescueRequestSessionConfiguration : IEntityTypeConfiguration<RescueRequestSession>
    {
        public void Configure(EntityTypeBuilder<RescueRequestSession> builder)
        {
            builder.ToTable("RescueRequestSessions");

            // Enum conversions
            builder.Property(s => s.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(s => s.TriggerType)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Session -> RescuerRequests (1-N)
            builder.HasMany(s => s.Requests)
                .WithOne(r => r.Session)
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(s => s.IncidentId)
                .HasDatabaseName("IX_RescueRequestSessions_IncidentId");

            builder.HasIndex(s => s.Status)
                .HasDatabaseName("IX_RescueRequestSessions_Status");

            builder.HasIndex(s => new { s.IncidentId, s.SessionNumber })
                .IsUnique()
                .HasDatabaseName("IX_RescueRequestSessions_IncidentId_SessionNumber");
        }
    }
}
