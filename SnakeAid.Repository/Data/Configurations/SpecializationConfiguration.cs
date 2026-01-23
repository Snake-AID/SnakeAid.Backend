using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
    {
        public void Configure(EntityTypeBuilder<Specialization> builder)
        {
            builder.ToTable("Specializations");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(s => s.Name)
                .IsUnique()
                .HasDatabaseName("IX_Specializations_Name");
        }
    }
}