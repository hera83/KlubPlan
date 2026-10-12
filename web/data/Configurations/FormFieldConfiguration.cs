using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Label)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.HelpText)
            .HasMaxLength(500);

        builder.Property(e => e.FieldType)
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.HasOne(e => e.Form)
            .WithMany(f => f.Fields)
            .HasForeignKey(e => e.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.FormId, e.Order });
    }
}
