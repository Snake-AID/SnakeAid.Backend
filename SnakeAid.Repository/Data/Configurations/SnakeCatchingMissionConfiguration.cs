using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakeCatchingMissionConfiguration : IEntityTypeConfiguration<SnakeCatchingMission>
    {
        public void Configure(EntityTypeBuilder<SnakeCatchingMission> builder)
        {
            builder.ToTable("SnakeCatchingMissions");

            // Enum conversion
            builder.Property(m => m.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship với RescuerProfile đã config tại RescuerProfileConfiguration

            // Indexes
            builder.HasIndex(m => m.Status)
                .HasDatabaseName("IX_SnakeCatchingMissions_Status");

            builder.HasIndex(m => m.RescuerId)
                .HasDatabaseName("IX_SnakeCatchingMissions_RescuerId");

            builder.HasIndex(m => m.SnakeCatchingRequestId)
                .IsUnique()
                .HasDatabaseName("IX_SnakeCatchingMissions_RequestId");
        }
    }
}
