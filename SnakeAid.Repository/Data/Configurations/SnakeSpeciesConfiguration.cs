using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;
using System.Text.Json;

namespace SnakeAid.Repository.Data.Configurations
{
    public class SnakeSpeciesConfiguration : IEntityTypeConfiguration<SnakeSpecies>
    {
        public void Configure(EntityTypeBuilder<SnakeSpecies> builder)
        {
            builder.ToTable("SnakeSpecies");

            // Enum conversion
            builder.Property(s => s.PrimaryVenomType)
                .HasConversion<int?>();

            // JSON conversions
            builder.Property(s => s.Identification)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<IdentificationFeature>(v, (JsonSerializerOptions)null))
                .HasColumnType("jsonb");

            builder.Property(s => s.SymptomsByTime)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<SymptomTimeline>>(v, (JsonSerializerOptions)null))
                .HasColumnType("jsonb");

            builder.Property(s => s.FirstAidGuidelineOverride)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<FirstAidOverride>(v, (JsonSerializerOptions)null))
                .HasColumnType("jsonb");

            // One-to-Many: SnakeSpecies -> LibraryMedias
            builder.HasMany(s => s.LibraryMedias)
                .WithOne(m => m.SnakeSpecies)
                .HasForeignKey(m => m.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many: SnakeSpecies -> AlternativeNames
            builder.HasMany(s => s.AlternativeNames)
                .WithOne(n => n.SnakeSpecies)
                .HasForeignKey(n => n.SnakeSpeciesId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(s => s.ScientificName)
                .IsUnique()
                .HasDatabaseName("IX_SnakeSpecies_ScientificName");

            builder.HasIndex(s => s.Slug)
                .IsUnique()
                .HasDatabaseName("IX_SnakeSpecies_Slug");

            builder.HasIndex(s => s.IsVenomous)
                .HasDatabaseName("IX_SnakeSpecies_IsVenomous");

            builder.HasIndex(s => s.IsActive)
                .HasDatabaseName("IX_SnakeSpecies_IsActive");
        }
    }
}
