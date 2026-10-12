using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for CommunicationMessage ↔ PersonGroup many-to-many</summary>
public class CommunicationMessageGroupConfiguration : IEntityTypeConfiguration<CommunicationMessageGroup>
{
    public void Configure(EntityTypeBuilder<CommunicationMessageGroup> builder)
    {
        builder.HasKey(e => new { e.CommunicationMessageId, e.PersonGroupId });

        builder.HasOne(e => e.CommunicationMessage)
            .WithMany(m => m.Groups)
            .HasForeignKey(e => e.CommunicationMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.PersonGroup)
            .WithMany()
            .HasForeignKey(e => e.PersonGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonGroupId);
    }
}
