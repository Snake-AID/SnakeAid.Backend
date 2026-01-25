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

            // Indexes
            builder.HasIndex(m => m.Status)
                .HasDatabaseName("IX_RescueMissions_Status");

            builder.HasIndex(m => m.RescuerId)
                .HasDatabaseName("IX_RescueMissions_RescuerId");

            builder.HasIndex(m => m.IncidentId)
                .IsUnique()
                .HasDatabaseName("IX_RescueMissions_IncidentId");
        }
    }
}
