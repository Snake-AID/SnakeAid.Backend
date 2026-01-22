using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Username)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(a => a.FullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(a => a.Role)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(a => a.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            builder.HasIndex(a => a.Username)
                .IsUnique()
                .HasDatabaseName("IX_Accounts_Username");

            builder.HasIndex(a => a.Email)
                .IsUnique()
                .HasDatabaseName("IX_Accounts_Email");

            builder.HasIndex(a => a.PhoneNumber)
                .HasDatabaseName("IX_Accounts_PhoneNumber");
        }
    }
}