using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
    {
        public void Configure(EntityTypeBuilder<AppNotification> builder)
        {
            builder.ToTable("AppNotifications");

            // Relationship: AppNotification -> Account (User)
            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_AppNotifications_UserId");

            builder.HasIndex(n => n.IsRead)
                .HasDatabaseName("IX_AppNotifications_IsRead");

            builder.HasIndex(n => new { n.UserId, n.IsRead })
                .HasDatabaseName("IX_AppNotifications_UserId_IsRead");
        }
    }
}
