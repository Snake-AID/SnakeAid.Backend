using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakeCatchingRequestConfiguration : IEntityTypeConfiguration<SnakeCatchingRequest>
    {
        public void Configure(EntityTypeBuilder<SnakeCatchingRequest> builder)
        {
            builder.ToTable("SnakeCatchingRequests");

            // Enum conversions
            builder.Property(r => r.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(r => r.Priority)
                .HasConversion<int>()
                .IsRequired();

            // Relationship với MemberProfile (User) đã config tại MemberProfileConfiguration

            // Relationship: Request -> RescuerProfile (AssignedRescuer)
            builder.HasOne(r => r.AssignedRescuer)
                .WithMany()
                .HasForeignKey(r => r.AssignedRescuerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relationship: Request -> Mission (1-1)
            builder.HasOne(r => r.Mission)
                .WithOne(m => m.SnakeCatchingRequest)
                .HasForeignKey<SnakeCatchingMission>(m => m.SnakeCatchingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(r => r.Status)
                .HasDatabaseName("IX_SnakeCatchingRequests_Status");

            builder.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_SnakeCatchingRequests_UserId");

            builder.HasIndex(r => r.AssignedRescuerId)
                .HasDatabaseName("IX_SnakeCatchingRequests_AssignedRescuerId");

            builder.HasIndex(r => r.RequestDate)
                .HasDatabaseName("IX_SnakeCatchingRequests_RequestDate");
        }
    }
}
