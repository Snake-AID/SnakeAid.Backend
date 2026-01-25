using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class LocationEventConfiguration : IEntityTypeConfiguration<LocationEvent>
    {
        public void Configure(EntityTypeBuilder<LocationEvent> builder)
        {
            builder.ToTable("LocationEvents");

            // Enum conversions
            builder.Property(e => e.SessionType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(e => e.Role)
                .HasConversion<int>()
                .IsRequired();

            // Indexes
            builder.HasIndex(e => e.SessionId)
                .HasDatabaseName("IX_LocationEvents_SessionId");

            builder.HasIndex(e => e.AccountId)
                .HasDatabaseName("IX_LocationEvents_AccountId");

            builder.HasIndex(e => e.RecordedAt)
                .HasDatabaseName("IX_LocationEvents_RecordedAt");

            builder.HasIndex(e => new { e.SessionId, e.RecordedAt })
                .HasDatabaseName("IX_LocationEvents_SessionId_RecordedAt");
        }
    }
}
