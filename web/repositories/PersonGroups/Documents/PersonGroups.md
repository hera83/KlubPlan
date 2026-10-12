# PersonGroups (repository)

**Formål:** Opretter, henter, omdøber og sletter persongrupper (hold/årgange).
**Brug det når:** opgaven handler om selve grupperne: lister, valg i filtre og vedligehold.
**Brug det ikke til:** hvem der er medlem af en gruppe (det styres via personen i `People`).
**Tabeller/filer:** `PersonGroups` (medlemskaber i `PersonGroupMemberships` slettes cascade med gruppen)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetAllAsync()` | Alle grupper sorteret efter navn, med antal medlemmer. |
| `CountAsync()` | Antal grupper. Bruges til at vise "Alle", når et udvalg dækker alle grupper. |
| `ExistsAsync(int id)` | Findes gruppen? |
| `CreateAsync(string name)` | Opretter en gruppe med det trimmede navn. `NameInUse`, hvis navnet er taget. |
| `RenameAsync(int id, string name)` | Omdøber. `NotFound` eller `NameInUse` som udfald. |
| `DeleteAsync(int id)` | Sletter gruppen og dens medlemskaber, men ikke personerne. `false`, hvis den ikke findes. |

## Faldgruber
- Gruppenavne er unikke (unikt index). Sammenligningen er case-sensitiv i SQLite.
- Mange andre områder (møder, tilmeldinger, aktiviteter, kommunikation) refererer grupper. Slettes en gruppe, forsvinder den også fra dem via cascade.
