using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Services.AiGateway.Dtos.Ollama;
using web.Services.AiGateway.Dtos.Speaches;
using web.Services.AiGateway.Interfaces;

namespace web.BgSerives
{
    /// <summary>
    /// Processes queued meeting-attachment transcription jobs one at a time: transcribes the audio
    /// via AiGateway/Speaches, runs the raw transcript through a chat model to clean up wording and
    /// formatting, then appends the cleaned text to the meeting's Referat/Noter. Runs independently
    /// of any HTTP request so a job survives the user navigating away from the meeting page.
    /// </summary>
    public class TranscriptionWorker : BackgroundService
    {
        private readonly ITranscriptionQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TranscriptionWorker> _logger;

        public TranscriptionWorker(
            ITranscriptionQueue queue,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            ILogger<TranscriptionWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var attachmentId in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessAsync(attachmentId, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transcription job for attachment {AttachmentId} failed unexpectedly", attachmentId);
                }
            }
        }

        private async Task ProcessAsync(int attachmentId, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aiGateway = scope.ServiceProvider.GetRequiredService<IAiGatewayService>();
            var configProvider = scope.ServiceProvider.GetRequiredService<IAiGatewayConfigurationProvider>();

            var attachment = await context.MeetingAttachments
                .Include(a => a.FileMetadata)
                .Include(a => a.Meeting)
                .FirstOrDefaultAsync(a => a.Id == attachmentId, stoppingToken);

            if (attachment is null)
            {
                _logger.LogWarning("Transcription job for attachment {AttachmentId} skipped: attachment not found", attachmentId);
                return;
            }

            attachment.TranscriptionStatus = TranscriptionStatus.Processing;
            attachment.TranscriptionStartedAtUtc = DateTime.UtcNow;
            attachment.TranscriptionError = null;
            await context.SaveChangesAsync(stoppingToken);

            try
            {
                var config = await configProvider.GetActiveConfigurationAsync(stoppingToken);
                var fullPath = Path.Combine(_env.ContentRootPath, attachment.FileMetadata.StoredPath);

                string rawText;
                await using (var fileStream = File.OpenRead(fullPath))
                {
                    var transcription = await aiGateway.SpeachesTranscribeAsync(new TranscribeRequestDto
                    {
                        Model = config.DefaultSttModel,
                        FileContent = fileStream,
                        FileName = attachment.FileMetadata.OriginalFileName,
                        ContentType = attachment.FileMetadata.ContentType,
                        Language = "da"
                    }, stoppingToken);

                    rawText = transcription.Text?.Trim() ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    throw new InvalidOperationException("Transskriptionen var tom.");
                }

                var cleanedText = await CleanTranscriptAsync(aiGateway, config.DefaultChatModel, rawText, stoppingToken);

                var meeting = attachment.Meeting;
                meeting.MinutesNotes = string.IsNullOrWhiteSpace(meeting.MinutesNotes)
                    ? cleanedText
                    : $"{meeting.MinutesNotes}\n\n{cleanedText}";
                meeting.UpdatedAtUtc = DateTime.UtcNow;

                attachment.TranscriptionStatus = TranscriptionStatus.Completed;
                attachment.TranscriptionCompletedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Transcription completed for attachment {AttachmentId} (meeting {MeetingId})", attachmentId, attachment.MeetingId);
            }
            catch (Exception ex)
            {
                attachment.TranscriptionStatus = TranscriptionStatus.Failed;
                attachment.TranscriptionError = ex.Message;
                attachment.TranscriptionCompletedAtUtc = DateTime.UtcNow;
                // Persist the failure even if the job's own token was what caused the exception (e.g. app shutdown).
                await context.SaveChangesAsync(CancellationToken.None);

                _logger.LogWarning(ex, "Transcription failed for attachment {AttachmentId}", attachmentId);
            }
        }

        /// <summary>
        /// Runs the raw transcript through the chat model to fix spelling/punctuation and lay it out
        /// in natural paragraphs, without changing its meaning. Falls back to the raw transcript if no
        /// chat model is configured or the cleanup call fails, so a working transcript is never lost.
        /// </summary>
        private async Task<string> CleanTranscriptAsync(IAiGatewayService aiGateway, string? chatModel, string rawText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(chatModel))
            {
                return rawText;
            }

            var prompt = $"""
                Du får en rå tale-til-tekst-transskription af et møde, på dansk. Ret stavefejl og
                sætningsopbygning, tilføj korrekt punktuering, fjern fyldord og gentagelser (fx "øh",
                "altså", stammende gentagelser), og del teksten op i naturlige afsnit. Bevar alt
                indhold og betydning uændret - opfind ikke nyt indhold, og opsummer eller forkort ikke.
                Svar udelukkende med den rensede tekst, uden indledning eller kommentarer.

                Rå transskription:
                {rawText}
                """;

            try
            {
                var response = await aiGateway.OllamaChatAsync(new ChatRequestDto
                {
                    Model = chatModel,
                    Messages = new List<OllamaMessageDto>
                    {
                        new() { Role = "user", Content = prompt }
                    }
                }, cancellationToken);

                var cleaned = response.Message?.Content?.Trim();
                return string.IsNullOrWhiteSpace(cleaned) ? rawText : cleaned;
            }
            catch (Exception)
            {
                // Cleanup is a nice-to-have; a raw-but-correct transcript beats losing the recording's content.
                return rawText;
            }
        }
    }
}
