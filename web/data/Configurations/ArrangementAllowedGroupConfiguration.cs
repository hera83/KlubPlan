using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for Arrangement ↔ PersonGroup many-to-many</summary>
public class ArrangementAllowedGroupConfiguration : IEntityTypeConfiguration<ArrangementAllowedGroup>
{
    public void Configure(EntityTypeBuilder<ArrangementAllowedGroup> builder)
    {
        builder.HasKey(e => new { e.ArrangementId, e.PersonGroupId });

        builder.HasOne(e => e.Arrangement)
            .WithMany(a => a.AllowedGroups)
            .HasForeignKey(e => e.ArrangementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PersonGroup)
            .WithMany()
            .HasForeignKey(e => e.PersonGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonGroupId);
    }
}
