using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Models;
using web.Extensions;
using web.Managers.SmsMessages.Dtos;
using web.Managers.SmsMessages.Interfaces;
using web.Repositories.AppSettings.Interfaces;
using web.Repositories.Logs.Interfaces;
using web.Repositories.SmsMessages.Dtos;
using web.Repositories.SmsMessages.Interfaces;
using web.Repositories.ThemeSettings.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Services.AiGateway;
using web.Services.AiGateway.Dtos.KnowledgeBase;
using web.Services.AiGateway.Interfaces;
using web.Services.Mail.Dtos;
using web.Services.Mail.Interfaces;
using web.Services.Ollama;
using web.Services.Ollama.Dto;
using web.Services.Ollama.Interfaces;
using web.Services.Sms.Dtos.Sms;
using web.Services.Sms.Dtos.Subscriptions;
using web.Services.Sms.Interfaces;
using web.ViewModels;
using web.ViewModels.Mappers;

namespace web.Controllers;

[Authorize(Policy = "AdminOrDeveloper")]
public class SettingsController(
    UserManager<ApplicationUser> userManager,
    IUserRepository userRepository,
    IAppSettingRepository appSettingRepository,
    IThemeSettingRepository themeSettingRepository,
    ISmsMessageRepository smsMessageRepository,
    ISmsMessageManager smsMessageManager,
    ILogRepository logRepository,
    IOllamaService ollamaService,
    IOllamaConfigurationProvider ollamaConfigurationProvider,
    IAiGatewayService aiGatewayService,
    IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
    ISmsService smsService,
    IMailService mailService,
    IConfiguration configuration,
    ILogger<SettingsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Kun Brugere-fanen (standardfanen) indlæses med det samme. De øvrige faner
        // indlæser deres indhold via AJAX, første gang de vises (se settingsTabs-scriptet
        // i Index.cshtml), så Indstillinger-siden ikke skal vente på Ollama/SMS-gatewayen mv.
        var ollamaConfig = await ollamaConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var aiGatewayConfig = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);

        var model = new SettingsIndexViewModel
        {
            Users = await GetUsersAsync(new UserFilterViewModel(), cancellationToken),
            ShowOllamaTab = !string.IsNullOrWhiteSpace(ollamaConfig.BaseUrl),
            ShowAiGatewayTab = !string.IsNullOrWhiteSpace(aiGatewayConfig.BaseUrl),
            ShowSmsTab = !string.IsNullOrWhiteSpace(configuration["Sms:BaseUrl"])
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> UsersTabContent(CancellationToken cancellationToken)
    {
        var model = await GetUsersAsync(new UserFilterViewModel(), cancellationToken);
        return PartialView("_UsersTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> RegistrationTabContent(CancellationToken cancellationToken)
    {
        var model = await GetRegistrationSettingsAsync(cancellationToken);
        return PartialView("_RegistrationTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> ThemeTabContent(CancellationToken cancellationToken)
    {
        var model = await GetThemeSettingsAsync(cancellationToken);
        return PartialView("_ThemeTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> OllamaTabContent(CancellationToken cancellationToken)
    {
        var model = await GetOllamaStatusAsync(cancellationToken);
        return PartialView("_OllamaTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> AiGatewayTabContent(CancellationToken cancellationToken)
    {
        var model = await GetAiGatewayStatusAsync(cancellationToken);
        return PartialView("_AiGatewayTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> SmsTabContent(CancellationToken cancellationToken)
    {
        var model = await GetSmsStatusAsync(cancellationToken);
        return PartialView("_SmsTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> MailTabContent(CancellationToken cancellationToken)
    {
        var model = await GetMailStatusAsync(cancellationToken);
        return PartialView("_MailTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> MailStatus(CancellationToken cancellationToken)
    {
        var model = await GetMailFolderStatusAsync(cancellationToken);
        return PartialView("_MailStatusCards", model);
    }

    [HttpGet]
    public async Task<IActionResult> LogsTabContent(LogFilterViewModel filter, CancellationToken cancellationToken)
    {
        var result = await logRepository.SearchAsync(filter.ToSearchLogsDto(), cancellationToken);
        var model = result.ToLogFilterViewModel(filter);
        return PartialView("_LogsTab", model);
    }

    [HttpGet]
    public async Task<IActionResult> UsersTable(UserFilterViewModel filter, CancellationToken cancellationToken)
    {
        var model = await GetUsersAsync(filter, cancellationToken);
        return PartialView("_UsersTableBody", model);
    }

    [HttpGet]
    public async Task<IActionResult> LogsTable(LogFilterViewModel filter, CancellationToken cancellationToken)
    {
        var result = await logRepository.SearchAsync(filter.ToSearchLogsDto(), cancellationToken);
        var model = result.ToLogFilterViewModel(filter);
        return PartialView("_LogsTableBody", model);
    }

    [HttpGet]
    public async Task<IActionResult> OllamaTable(CancellationToken cancellationToken)
    {
        var model = await GetOllamaStatusAsync(cancellationToken);
        return PartialView("_OllamaTabBody", model);
    }

    [HttpGet]
    public async Task<IActionResult> OllamaRunningModelsTable(CancellationToken cancellationToken)
    {
        var model = await GetOllamaRunningStatusAsync(cancellationToken);
        return PartialView("_OllamaRunningModelsTableBody", model);
    }

    [HttpGet]
    public async Task<IActionResult> AiGatewayTable(CancellationToken cancellationToken)
    {
        var model = await GetAiGatewayStatusAsync(cancellationToken);
        return PartialView("_AiGatewayTabBody", model);
    }

    [HttpGet]
    public async Task<IActionResult> SmsTable(SmsFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await smsMessageRepository.SearchAsync(filter.ToSearchSmsMessagesDto(), cancellationToken);
        return PartialView("_SmsTableBody", page.ApplyTo(filter));
    }

    [HttpGet]
    public async Task<IActionResult> MailTable(MailFilterViewModel filter, CancellationToken cancellationToken)
    {
        var model = await GetMailMessagesAsync(filter, cancellationToken);
        return PartialView("_MailTableBody", model);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadMailAttachment(uint uid, string fileName, CancellationToken cancellationToken)
    {
        ReadMailResponseDto? message;
        try
        {
            message = await mailService.GetMailAsync(uid, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke forbinde til mailboksen for at hente vedhæftet fil");
            return this.ToastErrorJson("Kunne ikke forbinde til mailboksen.");
        }

        var attachment = message?.Attachments.FirstOrDefault(a => a.FileName == fileName);
        if (attachment is null)
        {
            return NotFound();
        }

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> MailMessage(uint uid, CancellationToken cancellationToken)
    {
        ReadMailResponseDto? message;
        try
        {
            message = await mailService.GetMailAsync(uid, markAsSeen: true, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke forbinde til mailboksen for at hente mail {Uid}", uid);
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        if (message is null)
            return NotFound();

        return Json(new MailMessageViewModel
        {
            Uid = message.Uid,
            From = message.From,
            To = message.To,
            Subject = message.Subject,
            ReceivedAtUtc = message.ReceivedAtUtc,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            Attachments = message.Attachments
                .Select(a => new MailAttachmentListItemViewModel { FileName = a.FileName, SizeBytes = a.Content.LongLength })
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMail(uint uid, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await mailService.DeleteMailAsync(uid, cancellationToken: cancellationToken);
            return deleted
                ? this.ToastSuccessJson("Mail slettet.")
                : this.ToastErrorJson("Mailen blev ikke fundet.");
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke slette mail {Uid}", uid);
            return this.ToastErrorJson("Kunne ikke forbinde til mailboksen.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMail(SendMailViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Angiv mindst én modtager, et emne og en besked.");

        var recipients = model.To
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (recipients.Count == 0)
            return this.ToastErrorJson("Angiv mindst én modtager.");

        try
        {
            await mailService.SendMailAsync(new SendMailRequestDto
            {
                To = recipients,
                Subject = model.Subject.Trim(),
                TextBody = model.Body.Trim(),
            }, cancellationToken);

            return this.ToastSuccessJson("Mail sendt.");
        }
        catch (FormatException)
        {
            return this.ToastErrorJson("En eller flere modtageradresser er ugyldige.");
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke forbinde til mailserveren for at sende mail");
            return this.ToastErrorJson("Kunne ikke forbinde til mailserveren.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Brugeren kunne ikke oprettes. Kontroller felterne.");

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return this.ToastErrorJson(string.Join(" ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, model.Role);
        return this.ToastSuccessJson("Bruger oprettet.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
            return this.ToastErrorJson("Brugeren blev ikke fundet.");

        if (User.Identity?.Name == user.UserName)
            return this.ToastWarningJson("Du kan ikke redigere din egen bruger her. Brug 'Min profil' i stedet.");

        if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length < 6)
            return this.ToastErrorJson("Password skal være mindst 6 tegn.");

        user.Email = model.Email;
        user.UserName = model.Email;
        user.DisplayName = model.DisplayName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber;
        user.IsActive = model.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return this.ToastErrorJson(string.Join(" ", result.Errors.Select(e => e.Description)));

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, model.Role);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, token, model.Password);
            if (!passwordResult.Succeeded)
                return this.ToastErrorJson(string.Join(" ", passwordResult.Errors.Select(e => e.Description)));
        }

        return this.ToastSuccessJson("Bruger opdateret.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return this.ToastErrorJson("Brugeren blev ikke fundet.");

        if (User.Identity?.Name == user.UserName)
            return this.ToastWarningJson("Du kan ikke slette din egen bruger.");

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded
            ? this.ToastSuccessJson("Bruger slettet.")
            : this.ToastErrorJson(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRegistration(RegistrationSettingsViewModel model, CancellationToken cancellationToken)
    {
        await SetSettingAsync(AppSettingKeys.AllowPublicRegistration, model.AllowPublicRegistration.ToString().ToLowerInvariant(), cancellationToken);
        return this.ToastSuccessJson("Registreringsindstillinger gemt.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateThemeSelection(ThemeSelectionViewModel model, CancellationToken cancellationToken)
    {
        var light = await themeSettingRepository.GetAsync(model.ActiveThemeName, ThemeMode.Light, cancellationToken);
        var dark = await themeSettingRepository.GetAsync(model.ActiveThemeName, ThemeMode.Dark, cancellationToken);
        if (light is null || dark is null)
            return this.ToastErrorJson("Det valgte farvetema findes ikke.");

        await SetSettingAsync(AppSettingKeys.ActiveThemeMode, model.ActiveThemeMode, cancellationToken);
        await SetSettingAsync(AppSettingKeys.ActiveThemeName, model.ActiveThemeName, cancellationToken);

        return this.ToastSuccessJson("Tema gemt.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PullOllamaModel(PullOllamaModelViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Angiv et gyldigt modelnavn.");

        try
        {
            var result = await ollamaService.PullModelAsync(
                new OllamaPullRequest { Model = model.ModelName.Trim() },
                cancellationToken);

            return this.ToastSuccessJson($"Model '{model.ModelName}' hentet ({result.Status}).");
        }
        catch (OllamaException ex)
        {
            return this.ToastErrorJson($"Kunne ikke hente model: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at hente model {ModelName}", model.ModelName);
            return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOllamaModel(string modelName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return this.ToastErrorJson("Modelnavn mangler.");

        try
        {
            await ollamaService.DeleteModelAsync(
                new OllamaDeleteRequest { Model = modelName },
                cancellationToken);

            return this.ToastSuccessJson($"Model '{modelName}' slettet.");
        }
        catch (OllamaException ex)
        {
            return this.ToastErrorJson($"Kunne ikke slette model: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at slette model {ModelName}", modelName);
            return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadOllamaModel(string modelName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return this.ToastErrorJson("Modelnavn mangler.");

        try
        {
            await ollamaService.GenerateAsync(
                new OllamaGenerateRequest { Model = modelName },
                cancellationToken);

            return this.ToastSuccessJson($"Model '{modelName}' indlæst i hukommelsen.");
        }
        catch (OllamaException ex)
        {
            return this.ToastErrorJson($"Kunne ikke indlæse model: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at indlæse model {ModelName}", modelName);
            return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnloadOllamaModel(string modelName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return this.ToastErrorJson("Modelnavn mangler.");

        try
        {
            await ollamaService.GenerateAsync(
                new OllamaGenerateRequest { Model = modelName, KeepAlive = JsonSerializer.SerializeToElement(0) },
                cancellationToken);

            return this.ToastSuccessJson($"Model '{modelName}' fjernet fra hukommelsen.");
        }
        catch (OllamaException ex)
        {
            return this.ToastErrorJson($"Kunne ikke fjerne model: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at fjerne model {ModelName}", modelName);
            return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAiGatewayGroup(CreateAiGatewayGroupViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Angiv et gyldigt gruppenavn.");

        try
        {
            await aiGatewayService.CreateGroupAsync(new CreateGroupRequestDto { Name = model.Name.Trim() }, cancellationToken);
            return this.ToastSuccessJson($"Gruppen '{model.Name}' er oprettet.");
        }
        catch (AiGatewayException ex)
        {
            return this.ToastErrorJson($"Kunne ikke oprette gruppe: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at oprette gruppen {GroupName}", model.Name);
            return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAiGatewayGroup(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await aiGatewayService.DeleteGroupAsync(id, cancellationToken);
            return this.ToastSuccessJson("Gruppen er slettet.");
        }
        catch (AiGatewayException ex)
        {
            return this.ToastErrorJson($"Kunne ikke slette gruppe: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at slette gruppen {GroupId}", id);
            return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAiGatewayDocument(UploadAiGatewayDocumentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.File is null || model.File.Length == 0)
            return this.ToastErrorJson("Vælg en gyldig fil og gruppe.");

        try
        {
            await using var stream = model.File.OpenReadStream();
            await aiGatewayService.UploadDocumentAsync(new UploadDocumentRequestDto
            {
                GroupId = model.GroupId,
                FileContent = stream,
                FileName = model.File.FileName,
                ContentType = model.File.ContentType
            }, cancellationToken);

            return this.ToastSuccessJson($"Dokumentet '{model.File.FileName}' er uploadet.");
        }
        catch (AiGatewayException ex)
        {
            return this.ToastErrorJson($"Kunne ikke uploade dokument: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at uploade dokumentet {FileName}", model.File?.FileName);
            return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAiGatewayDocument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await aiGatewayService.DeleteDocumentAsync(id, cancellationToken);
            return this.ToastSuccessJson("Dokumentet er slettet.");
        }
        catch (AiGatewayException ex)
        {
            return this.ToastErrorJson($"Kunne ikke slette dokument: {ex.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at slette dokumentet {DocumentId}", id);
            return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAiGatewayDocument(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await aiGatewayService.DownloadDocumentAsync(id, cancellationToken);
            return File(result.Content, result.ContentType, result.FileName ?? "document");
        }
        catch (AiGatewayException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke hente dokumentet {DocumentId} fra AiGateway", id);
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSms(SendSmsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Angiv et gyldigt telefonnummer og en besked.");

        await smsMessageRepository.QueueAsync([new QueueSmsMessageDto { PhoneNumber = model.To.Trim(), Body = model.Message.Trim() }], cancellationToken);
        return this.ToastSuccessJson("SMS lagt i kø til afsendelse.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSms(int id, CancellationToken cancellationToken)
    {
        return await smsMessageManager.DeleteAsync(id, cancellationToken) switch
        {
            DeleteSmsMessageStatus.Deleted => this.ToastSuccessJson("Sms slettet."),
            DeleteSmsMessageStatus.NotDeletable => this.ToastErrorJson("Kun sms'er med status Queued eller Failed kan slettes."),
            DeleteSmsMessageStatus.GatewayFailed => this.ToastErrorJson("Kunne ikke annullere sms'en i SMS-gatewayen. Den er måske allerede sendt."),
            _ => this.ToastErrorJson("Sms'en blev ikke fundet.")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetrySms(int id, CancellationToken cancellationToken)
    {
        return await smsMessageRepository.RequeueAsync(id, cancellationToken) switch
        {
            RequeueSmsMessageStatus.Requeued => this.ToastSuccessJson("Sms lagt i kø til afsendelse igen."),
            RequeueSmsMessageStatus.NotFailed => this.ToastErrorJson("Kun sms'er med status Failed kan sendes igen."),
            _ => this.ToastErrorJson("Sms'en blev ikke fundet.")
        };
    }

    [HttpGet]
    public async Task<IActionResult> SmsSubscriberNumbers(CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await smsService.GetAllSubscriptionsAsync(cancellationToken: cancellationToken);

            return Json(new SmsSubscriberNumbersViewModel
            {
                Subscriptions = subscriptions
                    .Select(s => new SmsSubscriptionViewModel
                    {
                        SubscriptionId = s.Id,
                        PhoneNumbers = s.PhoneNumbers,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        WebhookUrl = s.WebhookUrl,
                        IsActive = s.IsActive
                    })
                    .OrderByDescending(s => s.IsActive)
                    .ThenByDescending(s => s.StartDate)
                    .ToList()
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke hente modtagernumre fra SMS-gatewayen");
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSmsSubscriberNumbers(SaveSmsSubscriberNumbersViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ToastErrorJson("Angiv en gyldig startdato og slutdato.");

        if (model.EndDate < model.StartDate)
            return this.ToastErrorJson("Slutdato skal være efter startdato.");

        var phoneNumbers = model.PhoneNumbers
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct()
            .ToList();

        try
        {
            if (model.SubscriptionId.HasValue && phoneNumbers.Count == 0)
            {
                await smsService.DeleteSubscriptionAsync(model.SubscriptionId.Value, cancellationToken: cancellationToken);
                return this.ToastSuccessJson("Alle modtagernumre er fjernet.");
            }

            if (phoneNumbers.Count == 0)
                return this.ToastErrorJson("Tilføj mindst ét nummer.");

            var webhookUrl = string.IsNullOrWhiteSpace(model.WebhookUrl) ? null : model.WebhookUrl.Trim();

            if (model.SubscriptionId.HasValue)
            {
                await smsService.UpdateSubscriptionAsync(model.SubscriptionId.Value, new UpdateSubscriptionsRequestDto
                {
                    PhoneNumbers = phoneNumbers,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    WebhookUrl = webhookUrl
                }, cancellationToken: cancellationToken);
            }
            else
            {
                await smsService.CreateSubscriptionAsync(new CreateSubscriptionsRequestDto
                {
                    PhoneNumbers = phoneNumbers,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    WebhookUrl = webhookUrl
                }, cancellationToken: cancellationToken);
            }

            return this.ToastSuccessJson("Modtagernumre gemt.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Kunne ikke gemme modtagernumre i SMS-gatewayen");
            var message = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Conflict => "Et eller flere numre er allerede registreret til en anden nøgle i SMS-gatewayen. Fjern dem fra listen og forsøg igen.",
                System.Net.HttpStatusCode.BadRequest => "SMS-gatewayen afviste numrene. Kontroller at de er angivet korrekt (f.eks. med landekode).",
                _ => "Kunne ikke gemme modtagernumre i SMS-gatewayen."
            };
            return this.ToastErrorJson(message);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Tidsudløb ved kontakt til SMS-gatewayen for at gemme modtagernumre");
            return this.ToastErrorJson("Kunne ikke forbinde til SMS-gatewayen.");
        }
    }

    private async Task<RegistrationSettingsViewModel> GetRegistrationSettingsAsync(CancellationToken cancellationToken)
    {
        return new RegistrationSettingsViewModel
        {
            AllowPublicRegistration = await GetBoolSettingAsync(AppSettingKeys.AllowPublicRegistration, cancellationToken)
        };
    }

    private async Task<ThemeSettingsViewModel> GetThemeSettingsAsync(CancellationToken cancellationToken)
    {
        return new ThemeSettingsViewModel
        {
            ActiveThemeMode = await GetSettingAsync(AppSettingKeys.ActiveThemeMode, ThemeMode.System, cancellationToken),
            ActiveThemeName = await GetSettingAsync(AppSettingKeys.ActiveThemeName, "Orange", cancellationToken),
            Themes = (await themeSettingRepository.GetAllAsync(cancellationToken)).ToThemeSettingViewModels()
        };
    }

    private async Task<UserFilterViewModel> GetUsersAsync(UserFilterViewModel filter, CancellationToken cancellationToken)
    {
        var page = await userRepository.SearchAsync(filter.ToSearchUsersDto(), cancellationToken);
        return page.ApplyTo(filter, userManager.GetUserId(User));
    }

    private async Task<OllamaSettingsViewModel> GetOllamaRunningStatusAsync(CancellationToken cancellationToken)
    {
        var config = await ollamaConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var model = new OllamaSettingsViewModel
        {
            BaseUrl = config.BaseUrl,
            DefaultChatModel = config.DefaultChatModel
        };

        try
        {
            var version = await ollamaService.GetVersionAsync(cancellationToken);
            model.Version = version.Version;
            model.IsOnline = true;

            var running = await ollamaService.ListRunningModelsAsync(cancellationToken);
            model.RunningModels = running.Models
                .Select(m => new OllamaRunningModelViewModel
                {
                    Name = m.Name,
                    SizeVram = m.SizeVram ?? m.Size,
                    ExpiresAt = m.ExpiresAt
                })
                .ToList();
        }
        catch (Exception ex) when (ex is OllamaException or HttpRequestException or TaskCanceledException)
        {
            model.IsOnline = false;
            model.ErrorMessage = "Kunne ikke forbinde til Ollama-serveren.";
            logger.LogWarning(ex, "Ollama-serveren på {BaseUrl} svarede ikke", config.BaseUrl);
        }

        return model;
    }

    private async Task<OllamaSettingsViewModel> GetOllamaStatusAsync(CancellationToken cancellationToken)
    {
        var model = await GetOllamaRunningStatusAsync(cancellationToken);
        if (!model.IsOnline)
            return model;

        try
        {
            var tags = await ollamaService.ListModelsAsync(cancellationToken);
            var runningNames = model.RunningModels.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            model.InstalledModels = tags.Models
                .Select(m => new OllamaInstalledModelViewModel
                {
                    Name = m.Name,
                    Size = m.Size,
                    ParameterSize = m.Details?.ParameterSize,
                    QuantizationLevel = m.Details?.QuantizationLevel,
                    Family = m.Details?.Family,
                    ModifiedAt = m.ModifiedAt,
                    IsRunning = runningNames.Contains(m.Name)
                })
                .OrderBy(m => m.Name)
                .ToList();
        }
        catch (Exception ex) when (ex is OllamaException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke hente modeloversigt fra Ollama-serveren på {BaseUrl}", model.BaseUrl);
        }

        return model;
    }

    private async Task<AiGatewaySettingsViewModel> GetAiGatewayStatusAsync(CancellationToken cancellationToken)
    {
        var config = await aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
        var model = new AiGatewaySettingsViewModel { BaseUrl = config.BaseUrl, DefaultChatModel = config.DefaultChatModel };

        try
        {
            var health = await aiGatewayService.CheckHealthAsync(cancellationToken);
            model.IsOnline = true;
            model.Status = health.Status;
            model.HealthChecks = (health.Checks ?? [])
                .Select(c => new AiGatewayHealthCheckViewModel { Name = c.Name, Status = c.Status, Description = c.Description })
                .ToList();
        }
        catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
        {
            model.IsOnline = false;
            model.ErrorMessage = "Kunne ikke forbinde til AiGateway.";
            logger.LogWarning(ex, "AiGateway på {BaseUrl} svarede ikke", config.BaseUrl);
            return model;
        }

        try
        {
            var groups = await aiGatewayService.ListGroupsAsync(cancellationToken);
            var documents = await aiGatewayService.ListDocumentsAsync(null, cancellationToken);

            var groupNameById = groups.ToDictionary(g => g.Id, g => g.Name ?? string.Empty);
            var documentCountByGroup = documents
                .GroupBy(d => d.GroupId)
                .ToDictionary(g => g.Key, g => g.Count());

            model.Groups = groups
                .Select(g => new AiGatewayGroupViewModel
                {
                    Id = g.Id,
                    Name = g.Name ?? string.Empty,
                    CreatedAtUtc = g.CreatedAt,
                    DocumentCount = documentCountByGroup.GetValueOrDefault(g.Id)
                })
                .OrderBy(g => g.Name)
                .ToList();

            model.Documents = documents
                .Select(d => new AiGatewayDocumentViewModel
                {
                    Id = d.Id,
                    GroupId = d.GroupId,
                    GroupName = groupNameById.GetValueOrDefault(d.GroupId),
                    FileName = d.FileName ?? string.Empty,
                    ContentType = d.ContentType,
                    SizeBytes = d.SizeBytes,
                    Status = d.Status,
                    UploadedAtUtc = d.UploadedAt
                })
                .OrderByDescending(d => d.UploadedAtUtc)
                .ToList();
        }
        catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke hente KnowledgeBase-data fra AiGateway på {BaseUrl}", config.BaseUrl);
        }

        try
        {
            var installed = (await aiGatewayService.OllamaListModelsAsync(cancellationToken)).Models ?? [];
            var running = (await aiGatewayService.OllamaListRunningModelsAsync(cancellationToken)).Models ?? [];
            var runningNames = running
                .Where(r => r.Name is not null)
                .Select(r => r.Name!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            model.OllamaModels = installed
                .Select(m => new AiGatewayOllamaModelViewModel
                {
                    Name = m.Name ?? string.Empty,
                    Size = m.Size,
                    ParameterSize = m.Details?.ParameterSize,
                    QuantizationLevel = m.Details?.QuantizationLevel,
                    Family = m.Details?.Family,
                    ModifiedAt = m.ModifiedAt,
                    IsRunning = m.Name is not null && runningNames.Contains(m.Name)
                })
                .OrderBy(m => m.Name)
                .ToList();
        }
        catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke hente Ollama-modeloversigt fra AiGateway på {BaseUrl}", config.BaseUrl);
        }

        try
        {
            var models = (await aiGatewayService.SpeachesListAudioModelsAsync(cancellationToken)).Models ?? [];
            var running = (await aiGatewayService.SpeachesListRunningModelsAsync(cancellationToken)).Models ?? [];
            var runningIds = running.ToHashSet(StringComparer.OrdinalIgnoreCase);

            model.SpeachesModels = models
                .Select(m => new AiGatewaySpeachesModelViewModel
                {
                    Id = m.Id ?? string.Empty,
                    OwnedBy = m.OwnedBy,
                    Task = m.Task,
                    Language = m.Language ?? [],
                    IsRunning = m.Id is not null && runningIds.Contains(m.Id)
                })
                .OrderBy(m => m.Id)
                .ToList();
        }
        catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke hente Speaches-modeloversigt fra AiGateway på {BaseUrl}", config.BaseUrl);
        }

        return model;
    }

    private async Task<SmsSettingsViewModel> GetSmsStatusAsync(CancellationToken cancellationToken)
    {
        var model = new SmsSettingsViewModel
        {
            Messages = (await smsMessageRepository.SearchAsync(new SearchSmsMessagesDto(), cancellationToken)).ApplyTo(new SmsFilterViewModel())
        };

        try
        {
            var balance = await smsService.GetBalanceCostAsync(cancellationToken: cancellationToken);
            if (balance is not null)
            {
                model.BalanceDkk = balance.Balance;
                model.BalanceUpdatedAtUtc = balance.UpdatedAt;
            }

            var currentCost = await smsService.GetCurrentCostAsync(cancellationToken: cancellationToken);
            if (currentCost is not null)
            {
                model.CurrentPriceDkk = currentCost.SmsPriceDkk;
                model.CurrentPriceUpdatedAtUtc = currentCost.UpdatedAt;
            }

            model.IsOnline = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            model.IsOnline = false;
            model.ErrorMessage = "Kunne ikke forbinde til SMS-gatewayen.";
            logger.LogWarning(ex, "SMS-gatewayen svarede ikke");
        }

        return model;
    }

    private async Task<MailSettingsViewModel> GetMailStatusAsync(CancellationToken cancellationToken)
    {
        var model = await GetMailFolderStatusAsync(cancellationToken);
        model.Messages = await GetMailMessagesAsync(new MailFilterViewModel(), cancellationToken);
        return model;
    }

    private async Task<MailSettingsViewModel> GetMailFolderStatusAsync(CancellationToken cancellationToken)
    {
        var model = new MailSettingsViewModel();

        try
        {
            var status = await mailService.GetFolderStatusAsync(cancellationToken: cancellationToken);
            model.TotalMessageCount = status.TotalCount;
            model.UnseenMessageCount = status.UnseenCount;
            model.IsOnline = true;
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
        {
            model.IsOnline = false;
            model.ErrorMessage = "Kunne ikke forbinde til mailboksen.";
            logger.LogWarning(ex, "Mailboksen svarede ikke");
        }

        return model;
    }

    private async Task<MailFilterViewModel> GetMailMessagesAsync(MailFilterViewModel filter, CancellationToken cancellationToken)
    {
        filter.Page = filter.Page < 1 ? 1 : filter.Page;
        filter.PageSize = filter.PageSize is < 5 or > 200 ? 10 : filter.PageSize;

        List<MailListItemViewModel> items;
        try
        {
            var messages = await mailService.ReadMailAsync(
                new ReadMailRequestDto { Folder = "INBOX", MaxCount = 50 },
                cancellationToken);

            items = messages
                .Select(m => new MailListItemViewModel
                {
                    Uid = m.Uid,
                    From = m.From,
                    Subject = m.Subject,
                    ReceivedAtUtc = m.ReceivedAtUtc,
                    IsSeen = m.IsSeen,
                    Attachments = m.Attachments
                        .Select(a => new MailAttachmentListItemViewModel { FileName = a.FileName, SizeBytes = a.Content.LongLength })
                        .ToList()
                })
                .OrderByDescending(m => m.ReceivedAtUtc)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Kunne ikke hente mails fra indbakken");
            items = [];
        }

        var query = items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(m =>
                m.From.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.Subject.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();
        filter.TotalCount = filtered.Count;
        filter.Items = filtered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

        return filter;
    }

    private async Task<string> GetSettingAsync(string key, string defaultValue, CancellationToken cancellationToken) =>
        await appSettingRepository.GetValueAsync(key, cancellationToken) ?? defaultValue;

    private async Task<bool> GetBoolSettingAsync(string key, CancellationToken cancellationToken)
    {
        var value = await GetSettingAsync(key, "false", cancellationToken);
        return bool.TryParse(value, out var result) && result;
    }

    private Task SetSettingAsync(string key, string value, CancellationToken cancellationToken) =>
        appSettingRepository.SetValueAsync(key, value, cancellationToken);
}
