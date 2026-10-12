# Forms (repository)

**Formål:** Formularer og deres felter i versionsserier: opret, ret, åbn/luk for svar, ny version og slet.
**Brug det når:** opgaven handler om selve formularen (titel, felter, versioner, om den tager imod svar).
**Brug det ikke til:** besvarelser (det er `FormSubmissions`) eller indsendelse/offentlige links (det er `FormManager`).
**Tabeller/filer:** `Forms`, `FormFields`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchFormsDto search, string userId)` | Seneste version af hver serie, nyeste først. Med antal felter/svar, om brugeren har svaret, og versionslisten, når der er flere versioner. Paging 5-200, ellers 10. |
| `GetByIdAsync(int id)` / `GetByPublicIdAsync(Guid publicId)` | Formularen med felter i rækkefølge (muligheder parset), eller `null`. |
| `GetLatestVersionsAsync(bool acceptingResponsesOnly)` | Seneste version af hver serie sorteret efter titel, evt. kun dem, der tager imod svar. Til link-vælgerne i Kommunikation og på aktiviteter. |
| `GetLatestLinkAsync(int id)` | Seneste version i formularens serie (id, `PublicId`, titel, anonym), så et link altid peger på den aktive version. |
| `GetVersionInfoAsync(int id)` | Versionsnummer, antal versioner, om det er den seneste, og id'er på aktuel/forrige/næste version. |
| `IsLatestVersionAsync(int id)` | Er det seriens seneste version? |
| `SaveAsync(SaveFormDto dto)` | Opretter (`Id` 0) eller retter formular og felter. Felter, der ikke er med, slettes med deres svar. Udfald: `NotFound`, `NotLatestVersion`, `Invalid` (titel, labels, muligheder). |
| `SetAcceptingResponsesAsync(int id, bool isAcceptingResponses)` | Åbner/lukker for svar. Kun seneste version. |
| `CreateNewVersionAsync(int sourceFormId, string? userId)` | Kopierer en lukket, seneste version til en ny, åben version. Udfald: `NotFound`, `StillAcceptingResponses`, `NotLatestVersion`. |
| `DeleteAsync(int id)` | Sletter formularen (felter og svar cascade). Slettes seriens første version, bliver den næste ny rod. |

## Faldgruber
- En serie hænger sammen via `RootFormId` (første versions id). Første version har selv `RootFormId = null`, så "serien" er altid `RootFormId ?? Id`.
- Kun seneste version kan rettes, åbnes/lukkes og besvares. Gamle versioner er skrivebeskyttede.
- `SectionHeading` (Informationstekst) må stå uden label og kan ikke besvares.
- `PublicId` bruges i offentlige `/Formular`-links. Det interne `Id` må aldrig vises offentligt.
