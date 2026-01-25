using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakeCatchingTariffConfiguration : IEntityTypeConfiguration<SnakeCatchingTariff>
    {
        public void Configure(EntityTypeBuilder<SnakeCatchingTariff> builder)
        {
            builder.ToTable("SnakeCatchingTariffs");

            // Enum conversion
            builder.Property(t => t.SizeCategory)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Tariff -> SnakeSpecies
            builder.HasOne(t => t.SnakeSpecies)
                .WithMany(s => s.SnakeCatchingTariffs)
                .HasForeignKey(t => t.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint - mỗi species/size chỉ có 1 tariff
            builder.HasIndex(t => new { t.SnakeSpeciesId, t.SizeCategory })
                .IsUnique()
                .HasDatabaseName("IX_SnakeCatchingTariffs_SnakeSpeciesId_SizeCategory");

            builder.HasIndex(t => t.IsActive)
                .HasDatabaseName("IX_SnakeCatchingTariffs_IsActive");
        }
    }
}
