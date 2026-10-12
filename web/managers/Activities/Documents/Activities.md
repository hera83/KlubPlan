# Activities (manager)

**Formål:** Aktivitetshandlinger på tværs af `Activities`, `PersonGroups`, `People`, `Users`, `Forms`, `ActivityFiles` og `Files`.
**Brug det når:** aktivitetslisten, formularen eller aktivitetssiden skal vises, en formular skal linkes (evt. som ny version), eller en aktivitet skal slettes med sine filer.
**Brug det ikke til:** arbejdsgruppe, opgaver, gem og fjern formular-link. Kald `IActivityRepository` direkte.
**Bruger:** `IActivityRepository`, `IPersonGroupRepository`, `IPersonRepository`, `IUserRepository`, `IFormRepository`, `IActivityFileRepository`, `IFileRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetListAsync(SearchActivitiesDto search)` | Tabellen plus antal grupper til "Alle". |
| `GetFormAsync(int? id)` | Opret- (`null`) eller ret-formularen med gruppevælgeren. `null`, hvis aktiviteten ikke findes. |
| `GetDetailsAsync(int id)` | Aktiviteten, antal grupper, antal personer i målgrupperne, aktive administratorer/udviklere og formularer, der ikke er linket endnu. |
| `LinkFormAsync(int activityId, int formId, bool createNewVersion, string? userId)` | Linker formularen. Med `createNewVersion` lukkes kilden for svar (hvis åben), der oprettes en ny tom version, og den linkes. Udfald: `ActivityNotFound`, `FormNotFound`, `StillAcceptingResponses`, `NotLatestVersion`. |
| `DeleteAsync(int id)` | Sletter aktivitetens filer, mappen `app_files/activities/<id>/` og så aktiviteten. |

## Faldgruber
- Aktivitetssidens øvrige faner (formularsvar, kommunikation, filer, lister) hentes af `ActivitiesController` fra deres egne områder, fordi managers ikke kalder hinanden.
