using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class LabelCollectionItemConfiguration : IEntityTypeConfiguration<LabelCollectionItem>
{
    public void Configure(EntityTypeBuilder<LabelCollectionItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Background)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ElementsJson)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Collection)
            .WithMany(c => c.Items)
            .HasForeignKey(e => e.LabelCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.LabelCollectionId, e.Order });
    }
}
