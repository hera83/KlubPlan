using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SubmittedAtUtc)
            .IsRequired();

        builder.HasOne(e => e.Form)
            .WithMany(f => f.Submissions)
            .HasForeignKey(e => e.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.FormId);
    }
}
