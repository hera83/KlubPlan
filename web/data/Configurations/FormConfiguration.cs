using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class FormConfiguration : IEntityTypeConfiguration<Form>
{
    public void Configure(EntityTypeBuilder<Form> builder)
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

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.VersionNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasOne<Form>()
            .WithMany()
            .HasForeignKey(e => e.RootFormId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.RootFormId);
        builder.HasIndex(e => e.PublicId)
            .IsUnique();
    }
}
