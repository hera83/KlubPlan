namespace web.Repositories.Communication.Dtos
{
    /// <summary>
    /// One resolved, deduped send target: a single (Channel, Address) pair attributed to the
    /// target Person it was resolved for (used for {{Navn}} personalization and personal links,
    /// even when Address belongs to one of that Person's guardians).
    /// </summary>
    public record ResolvedRecipientDto(int PersonId, string DisplayName, string Channel, string Address);
}
