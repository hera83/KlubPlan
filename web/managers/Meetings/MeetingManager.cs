using web.Constants;
using web.Infrastructure.Transcription.Interfaces;
using web.Managers.Meetings.Dtos;
using web.Managers.Meetings.Interfaces;
using web.Repositories.Files.Dtos;
using web.Repositories.Files.Interfaces;
using web.Repositories.Meetings.Dtos;
using web.Repositories.Meetings.Interfaces;
using web.Repositories.PersonGroups.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Services.AiGateway;
using web.Services.AiGateway.Dtos.Ollama;
using web.Services.AiGateway.Dtos.Speaches;
using web.Services.AiGateway.Interfaces;

namespace web.Managers.Meetings;

public class MeetingManager(
    IMeetingRepository meetingRepository,
    IFileRepository fileRepository,
    IPersonGroupRepository personGroupRepository,
    IUserRepository userRepository,
    ITranscriptionQueue transcriptionQueue,
    IAiGatewayService aiGatewayService,
    IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
    ILogger<MeetingManager> logger) : IMeetingManager
{
    private static readonly string[] AdminRoles = [AppRoles.Administrator, AppRoles.Developer];

    public async Task<MeetingsOverviewDto> GetOverviewAsync(SearchMeetingsDto search, CancellationToken cancellationToken = default) => new()
    {
        Meetings = await meetingRepository.SearchAsync(search, cancellationToken),
        Groups = await personGroupRepository.GetAllAsync(cancellationToken),
        Admins = await userRepository.GetActiveInRolesAsync(AdminRoles, cancellationToken)
    };

    public async Task<MeetingDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(id, cancellationToken);
        if (meeting is null)
            return null;

        return new MeetingDetailsDto
        {
            Meeting = meeting,
            Groups = await personGroupRepository.GetAllAsync(cancellationToken),
            Admins = await userRepository.GetActiveInRolesAsync(AdminRoles, cancellationToken)
        };
    }

    public async Task<MeetingAttachmentDto?> AddAttachmentAsync(int meetingId, Stream content, string originalFileName, string contentType, string? uploaderId, bool isRecording, CancellationToken cancellationToken = default)
    {
        if (!await meetingRepository.IsLatestVersionAsync(meetingId, cancellationToken))
            return null;

        var file = await fileRepository.CreateAsync(new CreateStoredFileDto
        {
            OriginalFileName = originalFileName,
            ContentType = contentType,
            Category = FileCategories.Meetings,
            OwnerId = uploaderId
        }, content, cancellationToken);

        var attachment = await meetingRepository.AddAttachmentAsync(meetingId, file.Id, isRecording, cancellationToken);
        if (attachment is null)
        {
            // The meeting was deleted or versioned while the file uploaded.
            await fileRepository.DeleteAsync(file.Id, cancellationToken);
            return null;
        }

        logger.LogInformation("Meeting {MeetingId} got attachment {FileId} (recording: {IsRecording})", meetingId, file.Id, isRecording);
        return attachment;
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var fileId = await meetingRepository.RemoveAttachmentAsync(attachmentId, cancellationToken);
        if (fileId is null)
            return false;

        await fileRepository.DeleteAsync(fileId.Value, cancellationToken);
        return true;
    }

    public async Task<StoredFileDto?> GetAttachmentFileAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var fileId = await meetingRepository.GetAttachmentFileIdAsync(attachmentId, cancellationToken);
        return fileId is null ? null : await fileRepository.GetByIdAsync(fileId.Value, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var fileIds = await meetingRepository.DeleteAsync(id, cancellationToken);
        if (fileIds is null)
            return false;

        foreach (var fileId in fileIds)
            await fileRepository.DeleteAsync(fileId, cancellationToken);
        return true;
    }

    public async Task<RequestTranscriptionStatus> RequestTranscriptionAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var status = await meetingRepository.RequestTranscriptionAsync(attachmentId, cancellationToken);
        if (status == RequestTranscriptionStatus.Queued)
            transcriptionQueue.Enqueue(attachmentId);
        return status;
    }

    public async Task TranscribeAsync(int attachmentId, CancellationToken cancellationToken = default)
    {
        var job = await meetingRepository.StartTranscriptionAsync(attachmentId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Transcription job for attachment {AttachmentId} skipped: attachment not found", attachmentId);
            return;
        }

        try
        {
            var file = await fileRepository.GetByIdAsync(job.FileId, cancellationToken)
                ?? throw new InvalidOperationException("Lydfilen blev ikke fundet.");
            var config = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);

            string rawText;
            await using (var stream = File.OpenRead(file.FullPath))
            {
                var transcription = await aiGatewayService.SpeachesTranscribeAsync(new TranscribeRequestDto
                {
                    Model = config.DefaultSttModel,
                    FileContent = stream,
                    FileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    Language = "da"
                }, cancellationToken);
                rawText = transcription.Text?.Trim() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(rawText))
                throw new InvalidOperationException("Transskriptionen var tom.");

            var cleanedText = await CleanTranscriptAsync(config.DefaultChatModel, rawText, cancellationToken);
            await meetingRepository.CompleteTranscriptionAsync(attachmentId, cleanedText, cancellationToken);

            logger.LogInformation("Transcription completed for attachment {AttachmentId} (meeting {MeetingId})", attachmentId, job.MeetingId);
        }
        catch (Exception ex)
        {
            // Persist the failure even if the job's own token caused the exception (e.g. app shutdown).
            await meetingRepository.FailTranscriptionAsync(attachmentId, ex.Message, CancellationToken.None);
            logger.LogWarning(ex, "Transcription failed for attachment {AttachmentId}", attachmentId);
        }
    }

    public async Task<GenerateSummaryResultDto> GenerateSummaryAsync(int meetingId, CancellationToken cancellationToken = default)
    {
        var meeting = await meetingRepository.GetByIdAsync(meetingId, cancellationToken);
        if (meeting is null)
            return new GenerateSummaryResultDto { Status = GenerateSummaryStatus.NotFound };
        if (string.IsNullOrWhiteSpace(meeting.AgendaNotes) && string.IsNullOrWhiteSpace(meeting.MinutesNotes))
            return new GenerateSummaryResultDto { Status = GenerateSummaryStatus.NoNotes };

        var config = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.DefaultChatModel))
            return new GenerateSummaryResultDto { Status = GenerateSummaryStatus.NoModel };

        var prompt = $"""
            Du hjælper med at strukturere mødereferater for en dansk forening. Ud fra dagsorden og de
            rå noter nedenfor, skriv et pænt struktureret referat på dansk med korte afsnit/punkter,
            og afslut med en liste over konkrete beslutninger/opgaver, hvis nogen fremgår af noterne.
            Opfind ikke indhold der ikke fremgår af noterne.

            Titel: {meeting.Title}

            Dagsorden:
            {(string.IsNullOrWhiteSpace(meeting.AgendaNotes) ? "(ingen)" : meeting.AgendaNotes)}

            Rå noter:
            {(string.IsNullOrWhiteSpace(meeting.MinutesNotes) ? "(ingen)" : meeting.MinutesNotes)}
            """;

        try
        {
            var response = await aiGatewayService.OllamaChatAsync(new ChatRequestDto
            {
                Model = config.DefaultChatModel,
                Messages = [new() { Role = "user", Content = prompt }]
            }, cancellationToken);

            var summary = response.Message?.Content?.Trim();
            return string.IsNullOrWhiteSpace(summary)
                ? new GenerateSummaryResultDto { Status = GenerateSummaryStatus.EmptyAnswer }
                : new GenerateSummaryResultDto { Status = GenerateSummaryStatus.Generated, Summary = summary };
        }
        catch (AiGatewayException ex)
        {
            logger.LogWarning(ex, "Kunne ikke generere mødereferat via AiGateway ({StatusCode}): {Message}", ex.StatusCode, ex.Message);
            return new GenerateSummaryResultDto { Status = GenerateSummaryStatus.Failed, ErrorMessage = $"Kunne ikke generere referat: {ex.Message}" };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for referat-generering");
            return new GenerateSummaryResultDto { Status = GenerateSummaryStatus.Failed, ErrorMessage = "Kunne ikke kontakte AiGateway." };
        }
    }

    /// <summary>
    /// Runs the raw transcript through the chat model to fix spelling/punctuation and lay it out in
    /// natural paragraphs, without changing its meaning. Falls back to the raw transcript if no chat
    /// model is configured or the cleanup call fails, so a working transcript is never lost.
    /// </summary>
    private async Task<string> CleanTranscriptAsync(string? chatModel, string rawText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chatModel))
            return rawText;

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
            var response = await aiGatewayService.OllamaChatAsync(new ChatRequestDto
            {
                Model = chatModel,
                Messages = [new() { Role = "user", Content = prompt }]
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
