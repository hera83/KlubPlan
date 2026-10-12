namespace web.Repositories.Meetings.Dtos;

public enum RequestTranscriptionStatus
{
    /// <summary>Marked as queued; the caller puts it on the transcription queue.</summary>
    Queued,
    NotFound,
    NotLatestVersion,

    /// <summary>Only audio files can be transcribed.</summary>
    NotAudio,

    AlreadyInProgress
}
