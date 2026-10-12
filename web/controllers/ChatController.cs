using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Extensions;
using web.Services.AiGateway;
using web.Services.AiGateway.Dtos.Speaches;
using web.Services.AiGateway.Interfaces;
using web.Services.Ollama;
using web.Services.Ollama.Dto;
using web.Services.Ollama.Interfaces;
using web.ViewModels;
using AiGatewayOllamaDto = web.Services.AiGateway.Dtos.Ollama;
using AiGatewayKnowledgeBaseDto = web.Services.AiGateway.Dtos.KnowledgeBase;

namespace web.Controllers;

[Authorize]
public class ChatController(
    IOllamaService ollamaService,
    IOllamaConfigurationProvider ollamaConfigurationProvider,
    IAiGatewayService aiGatewayService,
    IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
    ILogger<ChatController> logger) : Controller
{
    private static readonly JsonSerializerOptions HistoryJsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task StreamMessage(ChatSendMessageViewModel request, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/x-ndjson";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteChunkAsync(new { error = "Besked må ikke være tom." }, cancellationToken);
            return;
        }

        var config = await ollamaConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var modelName = string.IsNullOrWhiteSpace(request.Model) ? config.DefaultChatModel : request.Model;
        if (string.IsNullOrWhiteSpace(modelName))
        {
            await WriteChunkAsync(new { error = "Ingen model valgt, og der er ikke sat en standardmodel." }, cancellationToken);
            return;
        }

        var messages = ParseHistory(request.HistoryJson);
        messages.Add(new OllamaChatMessageDto { Role = "user", Content = request.Message });

        if (!string.IsNullOrWhiteSpace(config.DefaultLanguage))
        {
            messages.Insert(0, new OllamaChatMessageDto
            {
                Role = "system",
                Content = $"Svar altid på {config.DefaultLanguage}, uanset hvilket sprog brugeren skriver på."
            });
        }

        try
        {
            await foreach (var chunk in ollamaService.ChatStreamAsync(
                new OllamaChatRequest { Model = modelName, Messages = messages },
                cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Message.Content))
                    await WriteChunkAsync(new { delta = chunk.Message.Content }, cancellationToken);
            }

            await WriteChunkAsync(new { done = true }, cancellationToken);
        }
        catch (OllamaException ex)
        {
            await WriteChunkAsync(new { error = ex.Message }, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for chat-stream");
            await WriteChunkAsync(new { error = "Kunne ikke kontakte Ollama-serveren." }, cancellationToken);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task StreamMessageAiGateway(ChatSendMessageViewModel request, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/x-ndjson";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteChunkAsync(new { error = "Besked må ikke være tom." }, cancellationToken);
            return;
        }

        var config = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var modelName = string.IsNullOrWhiteSpace(request.Model) ? config.DefaultChatModel : request.Model;
        if (string.IsNullOrWhiteSpace(modelName))
        {
            await WriteChunkAsync(new { error = "Ingen model valgt, og der er ikke sat en standardmodel." }, cancellationToken);
            return;
        }

        var messages = ParseAiGatewayHistory(request.HistoryJson);
        messages.Add(new AiGatewayOllamaDto.OllamaMessageDto { Role = "user", Content = request.Message });

        if (!string.IsNullOrWhiteSpace(config.DefaultLanguage))
        {
            messages.Insert(0, new AiGatewayOllamaDto.OllamaMessageDto
            {
                Role = "system",
                Content = $"Svar altid på {config.DefaultLanguage}, uanset hvilket sprog brugeren skriver på."
            });
        }

        try
        {
            await foreach (var chunk in aiGatewayService.OllamaChatStreamAsync(
                new AiGatewayOllamaDto.ChatRequestDto { Model = modelName, Messages = messages },
                cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Message?.Content))
                    await WriteChunkAsync(new { delta = chunk.Message.Content }, cancellationToken);
            }

            await WriteChunkAsync(new { done = true }, cancellationToken);
        }
        catch (AiGatewayException ex)
        {
            await WriteChunkAsync(new { error = ex.Message }, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for chat");
            await WriteChunkAsync(new { error = "Kunne ikke kontakte AiGateway." }, cancellationToken);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task StreamMessageAiGatewayRag(ChatSendMessageViewModel request, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/x-ndjson";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteChunkAsync(new { error = "Besked må ikke være tom." }, cancellationToken);
            return;
        }

        var config = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var modelName = string.IsNullOrWhiteSpace(request.Model) ? config.DefaultChatModel : request.Model;
        if (string.IsNullOrWhiteSpace(modelName))
        {
            await WriteChunkAsync(new { error = "Ingen model valgt, og der er ikke sat en standardmodel." }, cancellationToken);
            return;
        }

        var messages = ParseAiGatewayRagHistory(request.HistoryJson);
        messages.Add(new AiGatewayKnowledgeBaseDto.ChatMessageDto { Role = "user", Content = request.Message });

        if (!string.IsNullOrWhiteSpace(config.DefaultLanguage))
        {
            messages.Insert(0, new AiGatewayKnowledgeBaseDto.ChatMessageDto
            {
                Role = "system",
                Content = $"Svar altid på {config.DefaultLanguage}, uanset hvilket sprog brugeren skriver på."
            });
        }

        try
        {
            // AiGatewayens RagChat-endpoint har (endnu) ikke en stream-parameter som Chat/Generate,
            // så hele svaret sendes som ét delta-chunk – widgeten håndterer det ens.
            var response = await aiGatewayService.RagChatAsync(
                new AiGatewayKnowledgeBaseDto.RagChatRequestDto { Model = modelName, Messages = messages, TopK = 3 },
                cancellationToken);

            var content = response.Message?.Content;
            if (!string.IsNullOrEmpty(content))
                await WriteChunkAsync(new { delta = content }, cancellationToken);

            await WriteChunkAsync(new { done = true }, cancellationToken);
        }
        catch (AiGatewayException ex)
        {
            await WriteChunkAsync(new { error = ex.Message }, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for RAG-chat");
            await WriteChunkAsync(new { error = "Kunne ikke kontakte AiGateway." }, cancellationToken);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SynthesizeSpeech(ChatSynthesizeSpeechViewModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var text = TextToSpeechHelper.Format(request.Text);
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest();

        var config = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);

        try
        {
            var result = await aiGatewayService.SpeachesSynthesizeSpeechAsync(new SynthesizeRequestDto
            {
                Model = config.DefaultTtsModel,
                Voice = config.DefaultTtsVoice,
                Input = text,
                ResponseFormat = "mp3",
                // Nogle speaches-installationer validerer speed som et påkrævet float upstream,
                // selvom AiGateways eget skema tillader null - send derfor altid en værdi.
                Speed = 1.0
            }, cancellationToken);

            return File(result.Content, result.ContentType, result.FileName ?? "oplaesning.mp3");
        }
        catch (AiGatewayException ex)
        {
            logger.LogWarning(ex, "Kunne ikke generere oplæsning via AiGateway ({StatusCode}): {Message}", ex.StatusCode, ex.Message);
            var statusCode = (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500
                ? StatusCodes.Status422UnprocessableEntity
                : StatusCodes.Status502BadGateway;
            return StatusCode(statusCode, $"Kunne ikke generere oplæsning: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for tekst-til-tale");
            return StatusCode(StatusCodes.Status502BadGateway, "Kunne ikke kontakte AiGateway.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TranscribeSpeech(ChatTranscribeSpeechViewModel request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || request.Audio is null || request.Audio.Length == 0)
            return BadRequest();

        var config = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);

        try
        {
            await using var stream = request.Audio.OpenReadStream();
            var result = await aiGatewayService.SpeachesTranscribeAsync(new TranscribeRequestDto
            {
                Model = config.DefaultSttModel,
                FileContent = stream,
                FileName = string.IsNullOrWhiteSpace(request.Audio.FileName) ? "optagelse.webm" : request.Audio.FileName,
                ContentType = request.Audio.ContentType,
                // Whisper skal bruge en ISO-639-1-kode (fx "da") som sproghint, ikke visningsnavnet
                // fra config.DefaultLanguage ("Dansk") - en ugyldig værdi fejler upstream med 500.
                Language = "da"
            }, cancellationToken);

            return Json(new { text = result.Text?.Trim() ?? string.Empty });
        }
        catch (AiGatewayException ex)
        {
            logger.LogWarning(ex, "Kunne ikke transskribere tale via AiGateway ({StatusCode}): {Message}", ex.StatusCode, ex.Message);
            var statusCode = (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500
                ? StatusCodes.Status422UnprocessableEntity
                : StatusCodes.Status502BadGateway;
            return StatusCode(statusCode, $"Kunne ikke transskribere tale: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for tale-til-tekst");
            return StatusCode(StatusCodes.Status502BadGateway, "Kunne ikke kontakte AiGateway.");
        }
    }

    private async Task WriteChunkAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        await Response.WriteAsync(json + "\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    private static List<OllamaChatMessageDto> ParseHistory(string? historyJson)
    {
        if (string.IsNullOrWhiteSpace(historyJson))
            return new List<OllamaChatMessageDto>();

        try
        {
            var history = JsonSerializer.Deserialize<List<ChatHistoryMessageViewModel>>(historyJson, HistoryJsonOptions) ?? new();
            return history.Select(h => new OllamaChatMessageDto { Role = h.Role, Content = h.Content }).ToList();
        }
        catch (JsonException)
        {
            return new List<OllamaChatMessageDto>();
        }
    }

    private static List<AiGatewayOllamaDto.OllamaMessageDto> ParseAiGatewayHistory(string? historyJson)
    {
        if (string.IsNullOrWhiteSpace(historyJson))
            return new List<AiGatewayOllamaDto.OllamaMessageDto>();

        try
        {
            var history = JsonSerializer.Deserialize<List<ChatHistoryMessageViewModel>>(historyJson, HistoryJsonOptions) ?? new();
            return history.Select(h => new AiGatewayOllamaDto.OllamaMessageDto { Role = h.Role, Content = h.Content }).ToList();
        }
        catch (JsonException)
        {
            return new List<AiGatewayOllamaDto.OllamaMessageDto>();
        }
    }

    private static List<AiGatewayKnowledgeBaseDto.ChatMessageDto> ParseAiGatewayRagHistory(string? historyJson)
    {
        if (string.IsNullOrWhiteSpace(historyJson))
            return new List<AiGatewayKnowledgeBaseDto.ChatMessageDto>();

        try
        {
            var history = JsonSerializer.Deserialize<List<ChatHistoryMessageViewModel>>(historyJson, HistoryJsonOptions) ?? new();
            return history.Select(h => new AiGatewayKnowledgeBaseDto.ChatMessageDto { Role = h.Role, Content = h.Content }).ToList();
        }
        catch (JsonException)
        {
            return new List<AiGatewayKnowledgeBaseDto.ChatMessageDto>();
        }
    }
}
