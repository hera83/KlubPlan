using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using web.BgWorkers;
using web.Constants;
using web.Data;
using web.Data.Models;
using web.Data.Seeds;
using web.Extensions;
using web.Infrastructure.ErrorHandling;
using web.Infrastructure.Logging;
using web.Infrastructure.Setup;
using web.Infrastructure.Transcription;
using web.Infrastructure.Transcription.Interfaces;
using web.Managers.Arrangements;
using web.Managers.Arrangements.Interfaces;
using web.Managers.Forms;
using web.Managers.Forms.Interfaces;
using web.Managers.InfoScreens;
using web.Managers.InfoScreens.Interfaces;
using web.Managers.LabelCollections;
using web.Managers.LabelCollections.Interfaces;
using web.Managers.Meetings;
using web.Managers.Meetings.Interfaces;
using web.Managers.LabelCollections.Interfaces;
using web.Managers.People;
using web.Managers.People.Interfaces;
using web.Managers.Profile;
using web.Managers.Profile.Interfaces;
using web.Repositories.Activities;
using web.Repositories.Activities.Interfaces;
using web.Managers.Activities;
using web.Managers.Activities.Interfaces;
using web.Managers.ActivityFiles;
using web.Managers.ActivityLists;
using web.Managers.ActivityLists.Interfaces;
using web.Managers.ActivityFiles.Interfaces;
using web.Repositories.ActivityFiles;
using web.Repositories.ActivityFiles.Interfaces;
using web.Repositories.ActivityLists;
using web.Repositories.ActivityLists.Interfaces;
using web.Managers.Communication;
using web.Managers.Communication.Interfaces;
using web.Managers.CommunicationEmails;
using web.Managers.CommunicationEmails.Interfaces;
using web.Repositories.CommunicationEmailMessages;
using web.Repositories.CommunicationEmailMessages.Interfaces;
using web.Repositories.CommunicationMessages;
using web.Repositories.CommunicationMessages.Interfaces;
using web.Repositories.Files;
using web.Managers.SmsMessages;
using web.Managers.SmsMessages.Interfaces;
using web.Repositories.AppSettings;
using web.Repositories.AppSettings.Interfaces;
using web.Repositories.SmsMessages;
using web.Repositories.SmsMessages.Interfaces;
using web.Repositories.ThemeSettings;
using web.Repositories.ThemeSettings.Interfaces;
using web.Repositories.Files.Interfaces;
using web.Repositories.Forms;
using web.Repositories.Forms.Interfaces;
using web.Repositories.FormSubmissions;
using web.Repositories.FormSubmissions.Interfaces;
using web.Repositories.InfoScreens;
using web.Repositories.InfoScreens.Interfaces;
using web.Repositories.LabelCollections;
using web.Repositories.LabelCollections.Interfaces;
using web.Repositories.Logs;
using web.Repositories.Logs.Interfaces;
using web.Repositories.Meetings;
using web.Repositories.Meetings.Interfaces;
using web.Repositories.People;
using web.Repositories.People.Interfaces;
using web.Repositories.PersonGroups;
using web.Repositories.PersonGroups.Interfaces;
using web.Repositories.ArrangementRegistrations;
using web.Repositories.ArrangementRegistrations.Interfaces;
using web.Repositories.Arrangements;
using web.Repositories.Arrangements.Interfaces;
using web.Repositories.Users;
using web.Repositories.Users.Interfaces;
using web.Services.AiGateway;
using web.Services.AiGateway.Interfaces;
using web.Services.Mail;
using web.Services.Mail.Interfaces;
using web.Services.Ollama;
using web.Services.Ollama.Interfaces;
using web.Services.Sms;
using web.Services.Sms.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Serilog og log-database (app_dbs/log.db). Niveauerne sættes her, ikke i Logging:LogLevel.
var logDatabaseSettings = new LogDatabaseSettings
{
    DatabasePath = Path.Combine(builder.Environment.ContentRootPath, "app_dbs", "log.db")
};
Directory.CreateDirectory(Path.GetDirectoryName(logDatabaseSettings.DatabasePath)!);
builder.Services.AddSingleton(logDatabaseSettings);

builder.Host.UseSerilog((_, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.SQLite(
        sqliteDbPath: logDatabaseSettings.DatabasePath,
        tableName: logDatabaseSettings.TableName,
        storeTimestampInUtc: true,
        // Sinkens eget loft skal ligge over LogDatabaseSettings.MaxSizeBytes, så det altid er
        // LogCleanupWorker (og ikke sinken), der holder filen nede.
        maxDatabaseSize: 2048,
        rollOver: false));

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpClient();

// app.db: Data Source i appsettings er relativ og gøres absolut ud fra ContentRootPath.
var connectionStringBuilder = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Default"));
connectionStringBuilder.DataSource = Path.Combine(builder.Environment.ContentRootPath, connectionStringBuilder.DataSource);
Directory.CreateDirectory(Path.GetDirectoryName(connectionStringBuilder.DataSource)!);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, AppFilesHelper.FolderName));
builder.Services.AddDbContext<WebDbContext>(options => options.UseSqlite(connectionStringBuilder.ToString()));

// Authentication (ASP.NET Core Identity med cookies) + authorization
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<WebDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DeveloperOnly", policy =>
        policy.RequireRole(AppRoles.Developer));

    options.AddPolicy("AdminOrDeveloper", policy =>
        policy.RequireRole(AppRoles.Developer, AppRoles.Administrator));
});

// Repositories
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IPersonGroupRepository, PersonGroupRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IInfoScreenRepository, InfoScreenRepository>();
builder.Services.AddScoped<ILabelCollectionRepository, LabelCollectionRepository>();
builder.Services.AddScoped<IFormRepository, FormRepository>();
builder.Services.AddScoped<IFormSubmissionRepository, FormSubmissionRepository>();
builder.Services.AddScoped<IArrangementRepository, ArrangementRepository>();
builder.Services.AddScoped<IArrangementRegistrationRepository, ArrangementRegistrationRepository>();
builder.Services.AddScoped<IMeetingRepository, MeetingRepository>();
builder.Services.AddScoped<IActivityFileRepository, ActivityFileRepository>();
builder.Services.AddScoped<ICommunicationMessageRepository, CommunicationMessageRepository>();
builder.Services.AddScoped<ICommunicationEmailMessageRepository, CommunicationEmailMessageRepository>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityListRepository, ActivityListRepository>();
builder.Services.AddScoped<ISmsMessageRepository, SmsMessageRepository>();
builder.Services.AddScoped<IAppSettingRepository, AppSettingRepository>();
builder.Services.AddScoped<IThemeSettingRepository, ThemeSettingRepository>();

// Managers
builder.Services.AddScoped<IPeopleManager, PeopleManager>();
builder.Services.AddScoped<IProfileManager, ProfileManager>();
builder.Services.AddScoped<IInfoScreenManager, InfoScreenManager>();
builder.Services.AddScoped<ILabelCollectionManager, LabelCollectionManager>();
builder.Services.AddScoped<IFormManager, FormManager>();
builder.Services.AddScoped<IArrangementManager, ArrangementManager>();
builder.Services.AddScoped<IMeetingManager, MeetingManager>();
builder.Services.AddScoped<IActivityFileManager, ActivityFileManager>();
builder.Services.AddScoped<ICommunicationManager, CommunicationManager>();
builder.Services.AddScoped<ICommunicationEmailManager, CommunicationEmailManager>();
builder.Services.AddScoped<IActivityManager, ActivityManager>();
builder.Services.AddScoped<IActivityListManager, ActivityListManager>();
builder.Services.AddScoped<ISmsMessageManager, SmsMessageManager>();

// Eksterne services
builder.Services.AddScoped<IOllamaConfigurationProvider, OllamaConfigurationProvider>();
builder.Services.AddScoped<OllamaHttpClientFactory>();
builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddScoped<IAiGatewayConfigurationProvider, AiGatewayConfigurationProvider>();
builder.Services.AddScoped<AiGatewayHttpClientFactory>();
builder.Services.AddScoped<IAiGatewayService, AiGatewayService>();
var smsBaseUrl = builder.Configuration["Sms:BaseUrl"];
builder.Services.AddHttpClient<ISmsService, SmsService>(client =>
{
    // Falder tilbage til en dummy-adresse ligesom Ollama/AiGateway, så DI-opløsning af
    // ISmsService (som SettingsController altid injicerer) ikke fejler når SMS-gatewayen
    // ikke er konfigureret; faktiske kald fejler så blot med HttpRequestException.
    var baseUrl = string.IsNullOrWhiteSpace(smsBaseUrl) ? "http://localhost/" : smsBaseUrl;
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
});
builder.Services.AddScoped<IMailService, MailService>();

// QuestPDF (labels på Aktiviteter → Lister og Værktøjer → Labels): Community-licensen dækker privat/forenings-brug.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Workers
builder.Services.AddHostedService<LogCleanupWorker>();
builder.Services.AddSingleton<ITranscriptionQueue, TranscriptionQueue>();
builder.Services.AddHostedService<TranscriptionWorker>();

var smsWorkerEnabled = !string.IsNullOrWhiteSpace(builder.Configuration["Sms:ApiKey"]) && !string.IsNullOrWhiteSpace(smsBaseUrl);
if (smsWorkerEnabled)
{
    builder.Services.AddHostedService<SmsWorker>();
}

var communicationEmailWorkerEnabled = !string.IsNullOrWhiteSpace(builder.Configuration["Mail:Smtp:Host"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["Mail:Smtp:FromAddress"]);
if (communicationEmailWorkerEnabled)
{
    builder.Services.AddHostedService<CommunicationEmailWorker>();
}

var app = builder.Build();

if (!smsWorkerEnabled)
{
    app.Logger.LogWarning("Sms:BaseUrl eller Sms:ApiKey er ikke konfigureret; SmsWorker kører ikke.");
}

if (!communicationEmailWorkerEnabled)
{
    app.Logger.LogWarning("Mail:Smtp:Host eller Mail:Smtp:FromAddress er ikke konfigureret; CommunicationEmailWorker kører ikke.");
}

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<WebDbContext>().Database.MigrateAsync();
    await DataSeeder.SeedAsync(scope.ServiceProvider);
}

// GlobalExceptionHandler logger og svarer ProblemDetails på script-kald. Almindelige
// side-requests sendes videre til fejlsiden /Home/Error. Middlewaren logger ikke selv,
// så hver exception kun logges én gang.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/Home/Error",
    SuppressDiagnosticsCallback = _ => true
});

// Containeren står bag en reverse proxy, der terminerer TLS, så Kestrel ser kun HTTP. Ved at
// stole på X-Forwarded-Proto/-For afspejler Request.Scheme/Host det, klienten faktisk brugte
// (https i drift), hvilket absolutte links bygget ud fra Request.Scheme (fx Kommunikations
// formular-links) afhænger af. Proxyens adresse er ikke kendt på forhånd, så known-proxy/
// network-listerne ryddes. I udvikling er der ingen proxy, og intet ændres.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<FirstUserSetupMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
