using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SnakeAid.Repository.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("AspNetUsers");

            builder.Property(a => a.FullName)
                .HasMaxLength(100);

            builder.Property(a => a.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(a => a.PhoneVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(a => a.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
