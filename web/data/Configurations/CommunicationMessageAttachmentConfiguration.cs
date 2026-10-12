using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class CommunicationMessageAttachmentConfiguration : IEntityTypeConfiguration<CommunicationMessageAttachment>
{
    public void Configure(EntityTypeBuilder<CommunicationMessageAttachment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.CommunicationMessage)
            .WithMany(m => m.Attachments)
            .HasForeignKey(e => e.CommunicationMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FileMetadata)
            .WithMany()
            .HasForeignKey(e => e.FileMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.CommunicationMessageId);
    }
}
