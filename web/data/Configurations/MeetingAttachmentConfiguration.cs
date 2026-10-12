using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class MeetingAttachmentConfiguration : IEntityTypeConfiguration<MeetingAttachment>
{
    public void Configure(EntityTypeBuilder<MeetingAttachment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.TranscriptionStatus)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.HasOne(e => e.Meeting)
            .WithMany(m => m.Attachments)
            .HasForeignKey(e => e.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FileMetadata)
            .WithMany()
            .HasForeignKey(e => e.FileMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.MeetingId);
    }
}
