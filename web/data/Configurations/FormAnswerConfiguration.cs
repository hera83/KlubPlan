using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class FormAnswerConfiguration : IEntityTypeConfiguration<FormAnswer>
{
    public void Configure(EntityTypeBuilder<FormAnswer> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.FormSubmission)
            .WithMany(s => s.Answers)
            .HasForeignKey(e => e.FormSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FormField)
            .WithMany(f => f.Answers)
            .HasForeignKey(e => e.FormFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.FormSubmissionId);
        builder.HasIndex(e => e.FormFieldId);
    }
}
