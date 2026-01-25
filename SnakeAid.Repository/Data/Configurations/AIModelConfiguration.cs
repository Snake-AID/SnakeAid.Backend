using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
    {
        public void Configure(EntityTypeBuilder<AIModel> builder)
        {
            builder.ToTable("AIModels");

            // Indexes
            builder.HasIndex(m => m.Version)
                .IsUnique()
                .HasDatabaseName("IX_AIModels_Version");

            builder.HasIndex(m => m.IsActive)
                .HasDatabaseName("IX_AIModels_IsActive");

            builder.HasIndex(m => m.IsDefault)
                .HasDatabaseName("IX_AIModels_IsDefault");
        }
    }
}
