using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ReputationRawEventConfiguration : IEntityTypeConfiguration<ReputationRawEvent>
    {
        public void Configure(EntityTypeBuilder<ReputationRawEvent> builder)
        {
            builder.ToTable("ReputationRawEvents");

            // Relationship: ReputationRawEvent -> Account (User)
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_ReputationRawEvents_UserId");

            builder.HasIndex(e => e.IsProcessed)
                .HasDatabaseName("IX_ReputationRawEvents_IsProcessed");

            builder.HasIndex(e => e.EventType)
                .HasDatabaseName("IX_ReputationRawEvents_EventType");

            builder.HasIndex(e => new { e.ReferenceId, e.ReferenceType })
                .HasDatabaseName("IX_ReputationRawEvents_ReferenceId_ReferenceType");
        }
    }
}
