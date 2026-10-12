using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Self-referencing tree; deleting a folder deletes its sub tree</summary>
public class ActivityFolderConfiguration : IEntityTypeConfiguration<ActivityFolder>
{
    public void Configure(EntityTypeBuilder<ActivityFolder> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Activity)
            .WithMany()
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ParentFolder)
            .WithMany(f => f.SubFolders)
            .HasForeignKey(e => e.ParentFolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ActivityId, e.ParentFolderId });
    }
}
