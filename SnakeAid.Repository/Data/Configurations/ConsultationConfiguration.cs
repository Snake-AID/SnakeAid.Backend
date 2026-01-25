using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
    {
        public void Configure(EntityTypeBuilder<Consultation> builder)
        {
            builder.ToTable("Consultations");

            // Enum conversions
            builder.Property(c => c.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(c => c.Type)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Consultation -> Account (Caller)
            builder.HasOne(c => c.Caller)
                .WithMany()
                .HasForeignKey(c => c.CallerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Consultation -> Account (Callee)
            builder.HasOne(c => c.Callee)
                .WithMany()
                .HasForeignKey(c => c.CalleeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(c => c.Status)
                .HasDatabaseName("IX_Consultations_Status");

            builder.HasIndex(c => c.CallerId)
                .HasDatabaseName("IX_Consultations_CallerId");

            builder.HasIndex(c => c.CalleeId)
                .HasDatabaseName("IX_Consultations_CalleeId");

            builder.HasIndex(c => c.RoomId)
                .HasDatabaseName("IX_Consultations_RoomId");
        }
    }
}
