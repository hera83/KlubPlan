using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class SmsMessageConfiguration : IEntityTypeConfiguration<SmsMessage>
{
    public void Configure(EntityTypeBuilder<SmsMessage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Direction)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Body)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.FailureReason)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.GatewayMessageId);
        builder.HasIndex(e => e.PhoneNumber);
        builder.HasIndex(e => e.CreatedAt);
    }
}
