# Forms (manager)

**Formål:** At besvare formularer og vise svarene: kombinerer `Forms`, `FormSubmissions` og (for offentlige links) `People`.
**Brug det når:** en formular skal besvares (logget ind eller via `/Formular`), eller svarene skal vises/eksporteres.
**Brug det ikke til:** at oprette, rette, versionere eller slette formularer. Kald `IFormRepository` direkte.
**Bruger:** `IFormRepository`, `IFormSubmissionRepository`, `IPersonRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SubmitAsync(int formId, string userId, IReadOnlyList<FormAnswerInputDto> answers)` | Besvarelse fra en logget ind bruger. Udfald: `NotFound`, `Closed` (lukket eller gammel version), `Invalid` (med `FieldErrors`). Brugeren gemmes ikke på anonyme formularer. |
| `GetPublicFormAsync(Guid formPublicId, Guid? personPublicId)` | Adgangskontrol til `/Formular`: `NotFound`, `Closed`, `MissingIdentity`, `InvalidIdentity`, `AlreadySubmitted` eller `Ok`, plus formularen. |
| `SubmitPublicAsync(Guid formPublicId, Guid? personPublicId, IReadOnlyList<FormAnswerInputDto> answers)` | Besvarelse via offentligt link. Adgangen tjekkes igen før gem. |
| `GetResponsesAsync(int formId, int page, int pageSize)` | En side besvarelser med én kolonne pr. besvarbart felt og versionsinfo. Paging 5-500, ellers 10. |
| `GetAllResponsesAsync(int formId)` | Alle besvarelser på én side (CSV-eksport). |

## Faldgruber
- På anonyme formularer slås personen aldrig op, uanset hvad linket indeholder. Identitet gemmes aldrig.
- Validering: påkrævede felter skal udfyldes. Afkrydsningsfelter gemmes som værdierne samlet med ", ".
