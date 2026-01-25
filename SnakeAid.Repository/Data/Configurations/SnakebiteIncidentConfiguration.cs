using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakebiteIncidentConfiguration : IEntityTypeConfiguration<SnakebiteIncident>
    {
        public void Configure(EntityTypeBuilder<SnakebiteIncident> builder)
        {
            builder.ToTable("SnakebiteIncidents");

            // Enum conversion
            builder.Property(i => i.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Incident -> RescuerProfile (AssignedRescuer)
            builder.HasOne(i => i.AssignedRescuer)
                .WithMany()
                .HasForeignKey(i => i.AssignedRescuerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationship với MemberProfile (User) đã config tại MemberProfileConfiguration

            // Relationship: Incident -> RescueMission (1-1)
            builder.HasOne(i => i.RescueMission)
                .WithOne(m => m.Incident)
                .HasForeignKey<RescueMission>(m => m.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Incident -> Sessions (1-N)
            builder.HasMany(i => i.Sessions)
                .WithOne(s => s.Incident)
                .HasForeignKey(s => s.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Incident -> AllRequests (1-N)
            builder.HasMany(i => i.AllRequests)
                .WithOne(r => r.Incident)
                .HasForeignKey(r => r.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(i => i.Status)
                .HasDatabaseName("IX_SnakebiteIncidents_Status");

            builder.HasIndex(i => i.UserId)
                .HasDatabaseName("IX_SnakebiteIncidents_UserId");

            builder.HasIndex(i => i.AssignedRescuerId)
                .HasDatabaseName("IX_SnakebiteIncidents_AssignedRescuerId");
        }
    }
}
