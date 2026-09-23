namespace web.Data.Entities
{
    /// <summary>Stored as string. Imported = came from the Excel header row; the others are extra columns added in the app.</summary>
    public enum ActivityListColumnKind
    {
        Imported,
        Text,
        YesNo,
        Choice
    }
}
