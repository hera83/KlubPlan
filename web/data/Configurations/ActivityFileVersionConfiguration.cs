using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ActivityFileVersionConfiguration : IEntityTypeConfiguration<ActivityFileVersion>
{
    public void Configure(EntityTypeBuilder<ActivityFileVersion> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.ActivityFile)
            .WithMany(f => f.Versions)
            .HasForeignKey(e => e.ActivityFileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FileMetadata)
            .WithMany()
            .HasForeignKey(e => e.FileMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedByUser)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.ActivityFileId, e.VersionNumber }).IsUnique();
    }
}
