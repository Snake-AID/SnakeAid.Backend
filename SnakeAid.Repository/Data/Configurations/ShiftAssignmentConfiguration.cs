using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
    {
        public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
        {
            builder.ToTable("ShiftAssignments");

            builder.Property(a => a.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.HasOne(a => a.Shift)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Rescuer)
                .WithMany()
                .HasForeignKey(a => a.RescuerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.Status)
                .HasDatabaseName("IX_ShiftAssignments_Status");

            builder.HasIndex(a => a.RescuerId)
                .HasDatabaseName("IX_ShiftAssignments_RescuerId");

            builder.HasIndex(a => a.ShiftId)
                .HasDatabaseName("IX_ShiftAssignments_ShiftId");

            builder.HasIndex(a => a.ShiftStartLocal)
                .HasDatabaseName("IX_ShiftAssignments_ShiftStartLocal");

            builder.HasIndex(a => a.ShiftEndLocal)
                .HasDatabaseName("IX_ShiftAssignments_ShiftEndLocal");

            builder.HasIndex(a => new { a.RescuerId, a.ShiftId, a.ShiftStartLocal })
                .IsUnique()
                .HasDatabaseName("UX_ShiftAssignments_Rescuer_Shift_ShiftStartLocal");
        }
    }
}
