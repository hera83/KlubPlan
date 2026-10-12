using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Resolved, deduped send targets</summary>
public class CommunicationMessageRecipientConfiguration : IEntityTypeConfiguration<CommunicationMessageRecipient>
{
    public void Configure(EntityTypeBuilder<CommunicationMessageRecipient> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Channel)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Address)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.CommunicationMessage)
            .WithMany(m => m.Recipients)
            .HasForeignKey(e => e.CommunicationMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SmsMessage)
            .WithMany()
            .HasForeignKey(e => e.SmsMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CommunicationEmailMessage)
            .WithMany()
            .HasForeignKey(e => e.CommunicationEmailMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.CommunicationMessageId);
        builder.HasIndex(e => new { e.Channel, e.Address });
    }
}
