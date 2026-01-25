using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class BlogConfiguration : IEntityTypeConfiguration<Blog>
    {
        public void Configure(EntityTypeBuilder<Blog> builder)
        {
            builder.ToTable("Blogs");

            // Enum conversion
            builder.Property(b => b.Status)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Blog -> Account (Author)
            builder.HasOne(b => b.Author)
                .WithMany()
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(b => b.Status)
                .HasDatabaseName("IX_Blogs_Status");

            builder.HasIndex(b => b.AuthorId)
                .HasDatabaseName("IX_Blogs_AuthorId");
        }
    }
}
