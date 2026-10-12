using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

public class ArrangementRegistrationAnswerConfiguration : IEntityTypeConfiguration<ArrangementRegistrationAnswer>
{
    public void Configure(EntityTypeBuilder<ArrangementRegistrationAnswer> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.ArrangementRegistration)
            .WithMany(r => r.Answers)
            .HasForeignKey(e => e.ArrangementRegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ArrangementFormField)
            .WithMany(f => f.Answers)
            .HasForeignKey(e => e.ArrangementFormFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ArrangementRegistrationId);
        builder.HasIndex(e => e.ArrangementFormFieldId);
    }
}
