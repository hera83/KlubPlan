using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Name uniqueness per folder is enforced case-insensitively in ActivityFileRepository</summary>
public class ActivityFileConfiguration : IEntityTypeConfiguration<ActivityFile>
{
    public void Configure(EntityTypeBuilder<ActivityFile> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Activity)
            .WithMany()
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Folder)
            .WithMany(f => f.Files)
            .HasForeignKey(e => e.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ActivityId, e.FolderId });
    }
}
