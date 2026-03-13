using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class RescuerRequestConfiguration : IEntityTypeConfiguration<RescuerRequest>
    {
        public void Configure(EntityTypeBuilder<RescuerRequest> builder)
        {
            builder.ToTable("RescuerRequests");

            // Enum conversion
            builder.Property(r => r.Status)
                .HasConversion<int>()
                .IsRequired();

            // Indexes
            builder.HasIndex(r => r.Status)
                .HasDatabaseName("IX_RescuerRequests_Status");

            builder.HasIndex(r => r.IncidentId)
                .HasDatabaseName("IX_RescuerRequests_IncidentId");

            builder.HasIndex(r => r.RescuerId)
                .HasDatabaseName("IX_RescuerRequests_RescuerId");

            builder.HasIndex(r => r.OperatorId)
                .HasDatabaseName("IX_RescuerRequests_OperatorId");

            builder.HasIndex(r => r.DispatchedAt)
                .HasDatabaseName("IX_RescuerRequests_DispatchedAt");
        }
    }
}
