# People (manager)

**Formål:** Logik på tværs af personer og persongrupper.
**Brug det når:** en handling skal bruge både `IPersonRepository` og `IPersonGroupRepository`.
**Brug det ikke til:** almindelig CRUD på én person eller én gruppe. Kald repositoriet direkte.
**Bruger:** `IPersonRepository`, `IPersonGroupRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetOverviewAsync(SearchPeopleDto search)` | En side personer plus alle grupper (til filtre og gruppevedligehold). |
| `ImportToGroupAsync(int groupId, IReadOnlyList<string> rawNames)` | Importerer én person pr. linje som spiller i gruppen. Udfald: `Imported` (evt. med `Errors` pr. linje), `GroupNotFound`, `NoNames` eller `Failed`. |

## Faldgruber
- Importen renser hver linje (trim, også af kommaer) og laver navne om til "Titel Case" med dansk kultur. Linjer over 200 tegn afvises og kommer med i `Errors`.
- `Imported` med `ImportedCount = 0` betyder, at alle linjer blev afvist. Det er ikke det samme som `NoNames`.
