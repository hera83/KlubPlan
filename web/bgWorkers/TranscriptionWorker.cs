using web.Infrastructure.Transcription.Interfaces;
using web.Managers.Meetings.Interfaces;

namespace web.BgWorkers;

/// <summary>
/// Processes queued meeting-attachment transcription jobs one at a time (IMeetingManager.TranscribeAsync:
/// transcribe via AiGateway/Speaches, clean up the wording with a chat model, append to the meeting's
/// minutes). Runs independently of any HTTP request so a job survives the user navigating away.
/// </summary>
public class TranscriptionWorker(
    ITranscriptionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<TranscriptionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var attachmentId in queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var meetingManager = scope.ServiceProvider.GetRequiredService<IMeetingManager>();
                    await meetingManager.TranscribeAsync(attachmentId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Transcription job for attachment {AttachmentId} failed unexpectedly", attachmentId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // DequeueAllAsync kaster, når stoppingToken annulleres.
        }
    }
}
