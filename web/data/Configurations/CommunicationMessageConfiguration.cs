using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class CommunicationMessageConfiguration : IEntityTypeConfiguration<CommunicationMessage>
{
    public void Configure(EntityTypeBuilder<CommunicationMessage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Body)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.LinkType)
            .HasMaxLength(20);

        builder.Property(e => e.RecipientSummary)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Form)
            .WithMany()
            .HasForeignKey(e => e.FormId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Arrangement)
            .WithMany()
            .HasForeignKey(e => e.ArrangementId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Activity)
            .WithMany()
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.CreatedAt);
    }
}
