using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ArrangementConfiguration : IEntityTypeConfiguration<Arrangement>
{
    public void Configure(EntityTypeBuilder<Arrangement> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PublicId)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.AccessMode)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.PublicId)
            .IsUnique();
    }
}
