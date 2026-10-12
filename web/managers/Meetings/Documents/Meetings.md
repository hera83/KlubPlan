# Meetings (manager)

**Formål:** Mødehandlinger på tværs af `Meetings`, `Files`, `PersonGroups`, `Users`, transskriptionskøen og AiGateway.
**Brug det når:** mødeoversigten eller mødesiden skal vises (med valglister), bilag skal uploades/slettes/hentes, et møde skal slettes med sine filer, eller der skal transskriberes eller laves AI-referat.
**Brug det ikke til:** almindelige ændringer af mødet, fremmøde, noter og beslutninger. Kald `IMeetingRepository` direkte.
**Bruger:** `IMeetingRepository`, `IFileRepository`, `IPersonGroupRepository`, `IUserRepository`, `ITranscriptionQueue`, `IAiGatewayService`, `IAiGatewayConfigurationProvider`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetOverviewAsync(SearchMeetingsDto search)` | Mødetabellen plus alle grupper og aktive administratorer/udviklere. |
| `GetDetailsAsync(int id)` | Mødet plus grupper og administratorer til dialogerne. |
| `AddAttachmentAsync(...)` | Gemmer filen i `app_files/meetings/` og knytter den til mødet. `null` på ukendt/gammel version. |
| `DeleteAttachmentAsync(int attachmentId)` / `GetAttachmentFileAsync(int attachmentId)` | Fjerner bilaget og sletter filen, eller henter filen til download. |
| `DeleteAsync(int id)` | Sletter mødet og alle dets bilagsfiler. |
| `RequestTranscriptionAsync(int attachmentId)` | Markerer lydbilaget og lægger det i køen (`TranscriptionWorker` tager det). |
| `TranscribeAsync(int attachmentId)` | Baggrundsjobbet: Speaches-transskription → chat-model renser teksten (falder tilbage til råteksten) → tilføjes referatet. Fejl gemmes på bilaget og kastes ikke. |
| `GenerateSummaryAsync(int meetingId)` | Struktureret referat ud fra dagsorden og noter. Udfald: `NotFound`, `NoNotes`, `NoModel`, `EmptyAnswer`, `Failed` (med besked). |

## Faldgruber
- `GenerateSummaryAsync` fanger `AiGatewayException`/netværksfejl og returnerer `Failed`, så brugeren får en toast (kendt afvigelse fra `errorhandling.md` regel 4).
