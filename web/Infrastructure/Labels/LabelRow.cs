namespace web.Infrastructure.Labels
{
    /// <summary>One label to print: its text (line breaks kept) and how many copies.</summary>
    public sealed record LabelRow(string Text, int Quantity);
}
