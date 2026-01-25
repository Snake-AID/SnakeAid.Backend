using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ExpertTimeSlotConfiguration : IEntityTypeConfiguration<ExpertTimeSlot>
    {
        public void Configure(EntityTypeBuilder<ExpertTimeSlot> builder)
        {
            builder.ToTable("ExpertTimeSlots");

            // Enum conversion
            builder.Property(t => t.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: TimeSlot -> Account (Expert)
            builder.HasOne(t => t.Expert)
                .WithMany()
                .HasForeignKey(t => t.ExpertId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(t => t.Status)
                .HasDatabaseName("IX_ExpertTimeSlots_Status");

            builder.HasIndex(t => t.ExpertId)
                .HasDatabaseName("IX_ExpertTimeSlots_ExpertId");

            builder.HasIndex(t => new { t.ExpertId, t.StartTime })
                .HasDatabaseName("IX_ExpertTimeSlots_ExpertId_StartTime");
        }
    }
}
