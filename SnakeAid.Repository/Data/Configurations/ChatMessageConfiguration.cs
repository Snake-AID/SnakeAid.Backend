using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");

            // Relationship: ChatMessage -> Consultation
            builder.HasOne(cm => cm.Consultation)
                .WithMany()
                .HasForeignKey(cm => cm.ConsultationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: ChatMessage -> Account (Sender)
            builder.HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(cm => cm.ConsultationId)
                .HasDatabaseName("IX_ChatMessages_ConsultationId");

            builder.HasIndex(cm => cm.SenderId)
                .HasDatabaseName("IX_ChatMessages_SenderId");

            builder.HasIndex(cm => cm.SentAt)
                .HasDatabaseName("IX_ChatMessages_SentAt");
        }
    }
}
