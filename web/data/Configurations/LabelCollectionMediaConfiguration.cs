using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class LabelCollectionMediaConfiguration : IEntityTypeConfiguration<LabelCollectionMedia>
{
    public void Configure(EntityTypeBuilder<LabelCollectionMedia> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PublicId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Collection)
            .WithMany(c => c.Media)
            .HasForeignKey(e => e.LabelCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FileMetadata)
            .WithMany()
            .HasForeignKey(e => e.FileMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PublicId)
            .IsUnique();
    }
}
