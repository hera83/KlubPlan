using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.StoredFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Optional: Relationship to ApplicationUser (owner)
        // builder.HasOne<ApplicationUser>()
        //     .WithMany()
        //     .HasForeignKey(e => e.OwnerId)
        //     .OnDelete(DeleteBehavior.SetNull);
    }
}
