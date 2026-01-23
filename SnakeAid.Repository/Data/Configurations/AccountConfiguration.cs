using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            // Table name đã được config trong SnakeAidDbContext
            // builder.ToTable("Accounts");

            // Index cho Role để query performance tốt hơn
            builder.HasIndex(a => a.Role)
                .HasDatabaseName("IX_Accounts_Role");

            builder.HasIndex(a => a.IsActive)
                .HasDatabaseName("IX_Accounts_IsActive");
        }
    }
}