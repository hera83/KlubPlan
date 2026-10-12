# ActivityFiles (repository)

**Formål:** Fanen "Filer" på en aktivitet: mapper, filer og versioner. Hver version peger på en gemt fil (`FileMetadataId`), som ejes af `Files`-repositoriet.
**Brug det når:** mapper og filer skal vises, oprettes, omdøbes, flyttes eller tælles, eller en upload skal valideres og placeres.
**Brug det ikke til:** at gemme, hente eller slette de fysiske filer. Det sker i `IActivityFileManager`, som kombinerer dette repository med `IFileRepository`.
**Tabeller/filer:** `ActivityFolders`, `ActivityFiles`, `ActivityFileVersions`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetFolderAsync(int activityId, int? folderId, string? searchText)` | Indholdet af en mappe (`null` = roden) med brødkrummer og totaler, eller søgeresultater på tværs af mapper. `null`, hvis aktiviteten ikke findes. |
| `GetFolderTreeAsync(int activityId)` | Alle mapper som et indrykket træ (til "Flyt til"). |
| `GetVersionsAsync(int activityId, int fileId)` | Filens versioner, nyeste først (`IsCurrent` på den første). `null`, hvis filen ikke findes. |
| `GetVersionRefAsync(int activityId, int fileId, int? versionId)` | Den version, der skal hentes (nyeste, når `versionId` er `null`), med downloadnavn. |
| `GetZipPlanAsync(int activityId, fileIds, folderIds)` | Stierne i en zip af udvalget (nyeste version af hver fil) og tomme mapper. |
| `ResolveUploadTargetAsync(...)` | Validerer navn, blokerede filtyper og størrelse og finder målet: filen (ny version), eller mappen, hvor mapperne fra en trukket mappes sti oprettes. `ErrorMessage` er sat ved fejl. |
| `AddVersionAsync(...)` | Tilføjer en gemt fil som næste version, og opretter filen først, hvis den ikke findes i mappen. |
| `GetRestoreSourceAsync(int activityId, int versionId)` | Den version, der skal gendannes, og om den allerede er den nyeste. |
| `CreateFolderAsync` / `RenameFolderAsync` / `RenameFileAsync` | Opretter eller omdøber. Navne er unikke pr. mappe uden forskel på store og små bogstaver. |
| `MoveAsync(int activityId, fileIds, folderIds, int? targetFolderId)` | Flytter udvalget og returnerer antal flyttede og navnene på dem, der blev sprunget over (navnekonflikt, mappe ind i sig selv). |
| `GetDeleteSummaryAsync(...)` | Antal mapper, filer, versioner og bytes, som en sletning vil fjerne. |
| `DeleteAsync(...)` | Sletter udvalget inkl. undermapper og versioner i én transaktion. Returnerer opsummeringen og de gemte filer, kalderen skal slette. |
| `DeleteAllForActivityAsync(int activityId)` | Sletter alle mapper og filer på aktiviteten og returnerer de gemte filer, kalderen skal slette. |

## Faldgruber
- Alle metoder er afgrænset af `activityId`, så et id fra en anden aktivitet aldrig bliver rørt.
- `DeleteAsync`/`DeleteAllForActivityAsync` sletter kun rækkerne. Brug `IActivityFileManager.DeleteAsync` eller `IActivityManager.DeleteAsync`, ellers bliver filerne i `app_files/activities/<id>/` liggende.
- Navnesammenligning bruger dansk kultur uden forskel på store og små bogstaver (`æ/ø/å`).
