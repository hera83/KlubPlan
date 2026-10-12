# Logs (repository)

**Formål:** Søger read-only i Serilogs log-database.
**Brug det når:** opgaven handler om at vise eller søge i appens egne logs (Indstillinger → Logs).
**Brug det ikke til:** at skrive logs (brug `ILogger<T>`) eller at rydde op i log-databasen (det gør `LogCleanupWorker`).
**Tabeller/filer:** tabellen `Logs` i `app_dbs/log.db` (ikke `app.db`). Sti og tabelnavn kommer fra `LogDatabaseSettings`.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchLogsDto search)` | Filtrerer på level, delstreng i besked/exception/properties og UTC-tidsinterval. Nyeste først, med paging (10-500 pr. side, ellers 10). Returnerer en tom side, hvis databasen eller tabellen ikke findes endnu. |

## Faldgruber
- Bruger ADO.NET (`Microsoft.Data.Sqlite`), ikke EF Core, så der er ingen entity. Mapperen har `ToDto(this SqliteDataReader)` og konstanten `Columns`.
- `Timestamp` gemmes som tekst (`yyyy-MM-ddTHH:mm:ss.fff`, UTC), så tidsfiltrene sammenligner tekst i det format.
- Forbindelsen åbnes med `Mode=ReadOnly`. Skriv aldrig til `log.db` herfra.
