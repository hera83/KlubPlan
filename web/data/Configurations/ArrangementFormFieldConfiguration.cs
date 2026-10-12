using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ArrangementFormFieldConfiguration : IEntityTypeConfiguration<ArrangementFormField>
{
    public void Configure(EntityTypeBuilder<ArrangementFormField> builder)
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

        builder.HasOne(e => e.Arrangement)
            .WithMany(a => a.FormFields)
            .HasForeignKey(e => e.ArrangementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ArrangementId, e.Order });
    }
}
