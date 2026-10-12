# ActivityLists (repository)

**Formål:** Arbejdslister på en aktivitet ("Lister"-fanen): kolonner, statusser, linjer med celleværdier og labels, som der arbejdes på linje for linje (Status, Note, Tilknyttet og ekstra kolonner). Også det offentlige link `/Arbejdsliste` for eksterne kontakter.
**Brug det når:** en liste skal vises, filtreres, rettes, fordeles eller tælles op, eller labels på linjerne skal gemmes eller printes.
**Brug det ikke til:** Excel-import og -eksport og udsendelse af links. Brug `IActivityListManager`.
**Tabeller/filer:** `ActivityLists`, `ActivityListColumns`, `ActivityListStatuses`, `ActivityListItems`, `ActivityListCellValues`, `ActivityListLabels`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetSummariesAsync(int activityId, string? userId)` | Aktivitetens lister, nyeste først, med antal linjer, mine, uden tilknytning og pr. status. |
| `CreateAsync(CreateActivityListDto dto)` | Opretter listen med importerede kolonner, standardstatusser og linjerne (i portioner af 500, én transaktion). |
| `UpdateAsync(int listId, string title, string? description)` / `DeleteAsync(int listId)` | Retter titel/beskrivelse eller sletter listen (alt under den cascade). Slet giver aktivitetens id i `Id`. |
| `GetDetailsAsync(int listId, string? userId)` | Listesiden: listen, skemaet (kolonner, statusser, medlemmer) og første side linjer. |
| `GetCountsAsync(int listId)` | Antal i alt, uden tilknytning, pr. status og pr. medlem. |
| `GetItemsAsync(SearchActivityListItemsDto search, string? userId)` | Én side linjer (1-500, ellers 10) med søgning, status-, tilknytnings- og notefilter og sortering. Er siden efter sidste side, gives sidste side. |
| `UpdateFieldAsync(UpdateActivityListFieldDto dto, string? userId)` | Gemmer ét felt (status, note, tilknyttet eller en kolonne) og returnerer nye antal pr. status. |
| `SaveItemAsync(int listId, int? itemId, values, string? userId)` | Tilføjer eller retter en linjes kolonneværdier. |
| `DeleteItemAsync(int listId, int itemId)` | Sletter en linje. |
| `DistributeAsync(int listId, memberIds, bool onlyUnassigned, string? userId)` | Fordeler linjerne i sammenhængende blokke, så de valgte ender med (næsten) lige mange. |
| `SaveColumnAsync` / `DeleteColumnAsync` / `SetColumnHiddenAsync` | Ekstra kolonner (tekst, ja/nej, valgliste). Importerede kolonner kan ikke slettes. |
| `SetNoteVisibleAsync(int listId, bool visible)` | Slår Note-kolonnen til/fra. Noterne gemmes. |
| `SaveStatusesAsync(SaveActivityListStatusesDto dto)` | Erstatter statusserne. Linjer med en fjernet status får standardstatussen. |
| `GetExportDataAsync(search, userId)` | Alle linjer i søgningen i dens sortering, til Excel-eksporten. |
| `SumColumnAsync(search, int columnId, userId)` / `SetSumColumnAsync(int listId, int? columnId)` | "Optælling": summen af hele tal i en kolonne over linjerne i søgningen, og hvilken kolonne der tælles på. |
| `GetLinkRecipientsAsync(int listId, memberIds)` | De valgte eksterne kontakter med antal linjer, til udsendelse af links. |
| `GetPublicListAsync` / `GetPublicItemsAsync` / `UpdatePublicFieldAsync` | Det offentlige link: kun den eksterne kontakts egne linjer, ingen andres navne, og Tilknyttet kan ikke ændres. |
| `GetItemLabelsAsync` / `SaveItemLabelsAsync` | Labels på én linje. |
| `GetPrintLabelsAsync(search, userId)` | Labels på linjerne i søgningen i linjens og labelens rækkefølge, til "Print labels". |

## Faldgruber
- Alle metoder med et linje- eller kolonne-id tager også `listId` og tjekker, at de hører sammen.
- En linje vises som ændret, når `UpdatedAt > CreatedAt`, eller når den har en `UpdatedBy` (en linje tilføjet i hånden).
- Fejl og succes kommer som danske tekster i `ActivityListActionResultDto` (kendt afvigelse: statusser med tekst i controlleren er reglen i resten af projektet).
- Søgningen bruger `LIKE`, som kun er ufølsom for store og små bogstaver i ASCII i SQLite.
