using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");

            // Enum conversion
            builder.Property(t => t.TransactionType)
                .HasConversion<int>()
                .IsRequired();

            // Relationship: Transaction -> Account (User)
            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(t => t.TransactionType)
                .HasDatabaseName("IX_Transactions_TransactionType");

            builder.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_Transactions_UserId");

            builder.HasIndex(t => t.ReferenceId)
                .HasDatabaseName("IX_Transactions_ReferenceId");

            builder.HasIndex(t => t.CreatedAt)
                .HasDatabaseName("IX_Transactions_CreatedAt");
        }
    }
}
