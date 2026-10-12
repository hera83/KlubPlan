using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class InfoScreenConfiguration : IEntityTypeConfiguration<InfoScreen>
{
    public void Configure(EntityTypeBuilder<InfoScreen> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PublicId)
            .IsRequired();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AspectRatio)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Transition)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.PublicId)
            .IsUnique();

        builder.HasIndex(e => e.Title);
    }
}
