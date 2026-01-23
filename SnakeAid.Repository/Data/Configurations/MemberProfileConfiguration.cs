using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SnakeAid.Repository.Data.Configurations
{
    public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
    {
        public void Configure(EntityTypeBuilder<MemberProfile> builder)
        {
            builder.ToTable("MemberProfiles");

            // Inherit from Account
            builder.HasBaseType<Account>();

            builder.Property(mp => mp.Rating)
                .HasColumnType("decimal(3,2)")
                .HasDefaultValue(0.0f);

            builder.Property(mp => mp.RatingCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(mp => mp.HasUnderlyingDisease)
                .IsRequired()
                .HasDefaultValue(false);

            // Convert List<string> to JSON or delimited string
            builder.Property(mp => mp.EmergencyContacts)
                .HasConversion(
                    v => string.Join(';', v ?? new List<string>()),
                    v => string.IsNullOrEmpty(v)
                        ? new List<string>()
                        : v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .HasMaxLength(1000);
        }
    }
}