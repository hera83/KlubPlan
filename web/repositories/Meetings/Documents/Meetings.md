# Meetings (repository)

**Formål:** Møder i versionsserier med grupper, deltagere (brugere), fremmøde, noter, beslutninger og bilag, inklusive transskriptionsstatus på lydbilag.
**Brug det når:** opgaven handler om møder og deres indhold i appens egen database.
**Brug det ikke til:** selve bilagsfilerne (de ejes af `Files`; upload/sletning går gennem `MeetingManager`) eller AI (referat og transskription ligger i `MeetingManager`).
**Tabeller/filer:** `Meetings`, `MeetingGroups`, `MeetingAttendees`, `MeetingDecisions`, `MeetingAttachments` (peger på `FileMetadata`)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchMeetingsDto search)` | Seneste version af hver serie, nyeste mødedato først, filtreret på tekst (titel/sted), status og grupper. Med versionsliste, når der er flere versioner. Paging 10-500, ellers 10. |
| `GetByIdAsync(int id)` | Mødet med alt indhold og versionsinfo, eller `null`. |
| `IsLatestVersionAsync(int id)` | Er det seriens seneste version? |
| `CreateAsync(CreateMeetingDto dto)` | Opretter mødet med grupper og deltagere. Returnerer id'et. |
| `UpdateAsync(int id, UpdateMeetingDto dto)` | Retter felter, grupper og deltagere (fremmøde bevares for dem, der bliver). `NotFound` eller `NotLatestVersion`. |
| `CreateNewVersionAsync(int sourceMeetingId, DateTime meetingDateUtc)` | Ny planlagt version af et afholdt/aflyst møde. Udfald: `NotFound`, `StillPlanned`, `NotLatestVersion`. |
| `DeleteAsync(int id)` | Sletter mødet (indhold cascade). Slettes seriens første version, bliver den næste ny rod. Returnerer bilagenes fil-id'er. |
| `SetAttendanceAsync`, `SaveNotesAsync` | Fremmøde for én deltager og dagsorden/referat. `false` ved ukendt møde, ukendt deltager eller gammel version. |
| `AddDecisionAsync`, `UpdateDecisionAsync`, `SetDecisionCompletionAsync`, `DeleteDecisionAsync` | Beslutninger. Afvises på gamle versioner. |
| `AddAttachmentAsync(int meetingId, int fileId, bool isRecording)` / `RemoveAttachmentAsync(int attachmentId)` / `GetAttachmentFileIdAsync(int attachmentId)` | Knytter en gemt fil til mødet, fjerner den (returnerer fil-id'et) eller slår fil-id'et op. |
| `RequestTranscriptionAsync(int attachmentId)` | Markerer et lydbilag som i kø. Udfald: `NotFound`, `NotLatestVersion`, `NotAudio`, `AlreadyInProgress`. |
| `GetTranscriptionStatusAsync(int attachmentId)` | Status og evt. fejl. |
| `StartTranscriptionAsync`, `CompleteTranscriptionAsync`, `FailTranscriptionAsync` | Baggrundsjobbets trin: i gang → teksten tilføjes referatet som nyt afsnit → færdig, eller fejlet med besked. |

## Faldgruber
- Kun seneste version i en serie kan ændres. Alle skrive-metoder tjekker det.
- `MeetingDateUtc` gemmes, som den kommer fra formularen (`datetime-local` uden konvertering). Det er historisk adfærd og ikke ændret.
- Deltagere er brugere med login (`ApplicationUser`), ikke personer fra personregistret.
