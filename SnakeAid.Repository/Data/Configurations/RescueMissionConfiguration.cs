using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class RescueMissionConfiguration : IEntityTypeConfiguration<RescueMission>
    {
        public void Configure(EntityTypeBuilder<RescueMission> builder)
        {
            builder.ToTable("RescueMissions");

            // Enum conversion
            builder.Property(m => m.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Mission -> RescuerProfile
            builder.HasOne(m => m.Rescuer)
                .WithMany(r => r.Missions)
                .HasForeignKey(m => m.RescuerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore Media navigation - ReportMedia uses polymorphic pattern (ReferenceId/ReferenceType)
            builder.Ignore(m => m.Media);

            // Indexes
            builder.HasIndex(m => m.Status)
                .HasDatabaseName("IX_RescueMissions_Status");

            builder.HasIndex(m => m.RescuerId)
                .HasDatabaseName("IX_RescueMissions_RescuerId");

            // Not unique anymore - an incident can have multiple missions (due to abort/retry)
            builder.HasIndex(m => m.IncidentId)
                .HasDatabaseName("IX_RescueMissions_IncidentId");
        }
    }
}
