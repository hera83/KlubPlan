using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class InfoScreenMediaConfiguration : IEntityTypeConfiguration<InfoScreenMedia>
{
    public void Configure(EntityTypeBuilder<InfoScreenMedia> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PublicId)
            .IsRequired();

        builder.Property(e => e.Kind)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Screen)
            .WithMany(s => s.Media)
            .HasForeignKey(e => e.InfoScreenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FileMetadata)
            .WithMany()
            .HasForeignKey(e => e.FileMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PublicId)
            .IsUnique();
    }
}
