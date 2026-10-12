using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web.Data.Models;

namespace web.Data.Configurations;

/// <summary>Join entity for CommunicationMessage ↔ Person many-to-many</summary>
public class CommunicationMessageRecipientPersonConfiguration : IEntityTypeConfiguration<CommunicationMessageRecipientPerson>
{
    public void Configure(EntityTypeBuilder<CommunicationMessageRecipientPerson> builder)
    {
        builder.HasKey(e => new { e.CommunicationMessageId, e.PersonId });

        builder.HasOne(e => e.CommunicationMessage)
            .WithMany(m => m.DirectPersons)
            .HasForeignKey(e => e.CommunicationMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Person)
            .WithMany()
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.PersonId);
    }
}
