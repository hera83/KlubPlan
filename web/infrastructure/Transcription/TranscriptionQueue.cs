using System.Threading.Channels;
using web.Infrastructure.Transcription.Interfaces;

namespace web.Infrastructure.Transcription;

/// <summary>Channel-backed, in-memory implementation of <see cref="ITranscriptionQueue"/>. Registered as a singleton.</summary>
public class TranscriptionQueue : ITranscriptionQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int attachmentId) => _channel.Writer.TryWrite(attachmentId);

    public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
