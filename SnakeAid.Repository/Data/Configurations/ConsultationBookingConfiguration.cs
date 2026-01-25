using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ConsultationBookingConfiguration : IEntityTypeConfiguration<ConsultationBooking>
    {
        public void Configure(EntityTypeBuilder<ConsultationBooking> builder)
        {
            builder.ToTable("ConsultationBookings");

            // Enum conversion
            builder.Property(b => b.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Booking -> Account (User)
            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Booking -> Account (Expert)
            builder.HasOne(b => b.Expert)
                .WithMany()
                .HasForeignKey(b => b.ExpertId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Booking -> ExpertTimeSlot
            builder.HasOne(b => b.TimeSlot)
                .WithMany(t => t.RescueMissions)
                .HasForeignKey(b => b.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Booking -> Consultation (optional)
            builder.HasOne(b => b.Consultation)
                .WithMany()
                .HasForeignKey(b => b.ConsultationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(b => b.Status)
                .HasDatabaseName("IX_ConsultationBookings_Status");

            builder.HasIndex(b => b.UserId)
                .HasDatabaseName("IX_ConsultationBookings_UserId");

            builder.HasIndex(b => b.ExpertId)
                .HasDatabaseName("IX_ConsultationBookings_ExpertId");

            builder.HasIndex(b => b.TimeSlotId)
                .HasDatabaseName("IX_ConsultationBookings_TimeSlotId");
        }
    }
}
