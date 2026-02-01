using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;
using System.Text.Json;

namespace SnakeAid.Repository.Data.Configurations
{
    public class FirstAidGuidelineConfiguration : IEntityTypeConfiguration<FirstAidGuideline>
    {
        public void Configure(EntityTypeBuilder<FirstAidGuideline> builder)
        {
            builder.ToTable("FirstAidGuidelines");

            // JSON conversion for Content
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            
            builder.Property(g => g.Content)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<FirstAidContent>(v, jsonOptions))
                .HasColumnType("jsonb")
                .IsRequired();

            // Enum conversion
            builder.Property(g => g.Type)
                .HasConversion<int>()
                .IsRequired();

            // Indexes
            builder.HasIndex(g => g.Type)
                .HasDatabaseName("IX_FirstAidGuidelines_Type");

            // Removed VenomTypeId index as FK is now in VenomType
        }
    }
}
