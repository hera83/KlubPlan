using System.Text.Json.Serialization;

namespace web.Constants
{
    /// <summary>
    /// Status of a queued CommunicationEmailMessage. Stored as a string (see ApplicationDbContext)
    /// so values stay readable in the database and stable across enum reordering.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CommunicationEmailMessageStatus
    {
        Pending,
        Sent,
        Failed
    }
}
