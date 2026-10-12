namespace web.Infrastructure.Transcription.Interfaces;

/// <summary>
/// In-memory queue of meeting attachment ids awaiting background transcription. A singleton so
/// TranscriptionWorker (also a singleton, per BackgroundService) can dequeue what controller
/// requests enqueue.
/// </summary>
public interface ITranscriptionQueue
{
    void Enqueue(int attachmentId);

    IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken);
}
