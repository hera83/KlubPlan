using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for Activity ↔ Form many-to-many</summary>
public class ActivityFormConfiguration : IEntityTypeConfiguration<ActivityForm>
{
    public void Configure(EntityTypeBuilder<ActivityForm> builder)
    {
        builder.HasKey(e => new { e.ActivityId, e.FormId });

        builder.HasOne(e => e.Activity)
            .WithMany(a => a.LinkedForms)
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Form)
            .WithMany()
            .HasForeignKey(e => e.FormId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.FormId);
    }
}
