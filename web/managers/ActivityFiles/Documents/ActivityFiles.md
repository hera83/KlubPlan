# ActivityFiles (manager)

**Formål:** De handlinger på fanen "Filer", der rører de gemte filer: upload, gendannelse af versioner, download, zip og sletning.
**Brug det når:** en fil skal uploades eller hentes, en version gendannes, eller et udvalg zippes eller slettes. (Sletning af en hel aktivitet med filer ligger i `IActivityManager.DeleteAsync`.)
**Brug det ikke til:** visning, mapper, omdøbning og flytning. Kald `IActivityFileRepository` direkte.
**Bruger:** `IActivityFileRepository`, `IFileRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `UploadAsync(UploadActivityFileDto upload, Stream content)` | Validerer, gemmer filen i `app_files/activities/<id>/` og tilføjer den som ny fil eller ny version. Content type udledes af filendelsen. |
| `RestoreVersionAsync(int activityId, int versionId, string? userId)` | Gemmer en kopi af en ældre version som ny nyeste version. Historikken bevares. |
| `GetDownloadAsync(int activityId, int fileId, int? versionId)` | Sti, content type, navn og om filen kan vises i browseren. `null`, hvis den ikke findes eller mangler på disk. |
| `GetZipAsync(int activityId, fileIds, folderIds)` | Filerne og de tomme mapper, der skal i en zip. Filer, der mangler på disk, udelades. `null`, når der ikke er noget at zippe. |
| `DeleteAsync(int activityId, fileIds, folderIds)` | Sletter udvalget permanent inkl. alle versioner og de gemte filer. |

## Faldgruber
- Selve zip-filen bygges i controlleren (`ActivityFilesController.DownloadZip`) i `app_files/temp/` og slettes, når svaret er sendt.
- Fejler gemningen af versionen, slettes den netop gemte fil igen.
