using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Outbound email queue, mirrors SmsMessage</summary>
public class CommunicationEmailMessageConfiguration : IEntityTypeConfiguration<CommunicationEmailMessage>
{
    public void Configure(EntityTypeBuilder<CommunicationEmailMessage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ToAddress)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Body)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.FailureReason)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.CommunicationMessage)
            .WithMany()
            .HasForeignKey(e => e.CommunicationMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.ToAddress);
        builder.HasIndex(e => e.CommunicationMessageId);
    }
}
