using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class InfoScreenSlideConfiguration : IEntityTypeConfiguration<InfoScreenSlide>
{
    public void Configure(EntityTypeBuilder<InfoScreenSlide> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Background)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ElementsJson)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasOne(e => e.Screen)
            .WithMany(s => s.Slides)
            .HasForeignKey(e => e.InfoScreenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.InfoScreenId, e.Order });
    }
}
