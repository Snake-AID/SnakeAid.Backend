using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;
using System.Text.Json;

namespace SnakeAid.Repository.Data.Configurations
{
    public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
    {
        public void Configure(EntityTypeBuilder<MemberProfile> builder)
        {
            builder.ToTable("MemberProfiles");

            builder.Property(mp => mp.HasUnderlyingDisease)
                .IsRequired()
                .HasDefaultValue(false);

            // Convert List<string> to JSON
            builder.Property(mp => mp.EmergencyContacts)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null))
                .HasColumnType("jsonb");

            // One-to-One relationship with Account (configured in SnakeAidDbContext)
            builder.HasOne(mp => mp.Account)
                .WithOne(a => a.MemberProfile)
                .HasForeignKey<MemberProfile>(mp => mp.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}