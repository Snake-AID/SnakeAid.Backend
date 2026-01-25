using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class LibraryMediaConfiguration : IEntityTypeConfiguration<LibraryMedia>
    {
        public void Configure(EntityTypeBuilder<LibraryMedia> builder)
        {
            builder.ToTable("LibraryMedias");

            // Enum conversion
            builder.Property(m => m.MediaType)
                .HasConversion<int>()
                .IsRequired();

            // Relationship với SnakeSpecies đã config tại SnakeSpeciesConfiguration

            // Relationship: LibraryMedia -> Account (UploadedBy)
            builder.HasOne(m => m.UploadedBy)
                .WithMany()
                .HasForeignKey(m => m.UploadedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(m => m.SnakeSpeciesId)
                .HasDatabaseName("IX_LibraryMedias_SnakeSpeciesId");

            builder.HasIndex(m => m.MediaType)
                .HasDatabaseName("IX_LibraryMedias_MediaType");

            builder.HasIndex(m => m.IsActive)
                .HasDatabaseName("IX_LibraryMedias_IsActive");

            builder.HasIndex(m => m.IsPublic)
                .HasDatabaseName("IX_LibraryMedias_IsPublic");
        }
    }
}
