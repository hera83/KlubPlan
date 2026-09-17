using System.Threading.Channels;

namespace web.BgSerives
{
    /// <summary>
    /// In-memory queue of meeting attachment ids awaiting background transcription. A singleton so
    /// <see cref="TranscriptionWorker"/> (also a singleton, per BackgroundService) can dequeue what
    /// controller requests enqueue.
    /// </summary>
    public interface ITranscriptionQueue
    {
        void Enqueue(int attachmentId);

        IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken);
    }

    public class TranscriptionQueue : ITranscriptionQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

        public void Enqueue(int attachmentId) => _channel.Writer.TryWrite(attachmentId);

        public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
