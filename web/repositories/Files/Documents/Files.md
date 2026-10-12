# Files (repository)

**Formål:** Ejer tabellen `FileMetadata` og de fysiske filer i `app_files/`: gemmer, slår op og sletter dem sammen, så der aldrig ligger en fil uden række eller en række uden fil (`files.md`).
**Brug det når:** en funktion skal gemme, vise/downloade eller slette en uploadet fil.
**Brug det ikke til:** at knytte filen til noget (bilag, medie, profilbillede). Gem filen her, og gem `Id` i områdets egen tabel. Kombinationen sker i en manager.
**Tabeller/filer:** `FileMetadata` og `app_files/<kategori>[/<undermappe>]/<guid>.<endelse>`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CreateAsync(CreateStoredFileDto dto, Stream content)` | Skriver filen (navn: `Guid` "N" + endelse med små bogstaver) og gemmer rækken. Fejler rækken, slettes filen igen. |
| `GetByIdAsync(int id)` | Metadata + absolut sti, eller `null`, hvis rækken mangler, er markeret slettet, eller filen mangler på disk. |
| `DeleteAsync(int id)` | Sletter rækken og filen. `false`, hvis rækken ikke findes. |
| `DeleteEmptyFolderAsync(string category, string subFolder)` | Fjerner `app_files/<kategori>/<undermappe>/`, hvis den findes og er tom. |

## Faldgruber
- Nye rækker gemmer `StoredPath` relativt til `app_files/` med `/` (fx `avatars/3f2a….png`). Ældre rækker har rodmappen med (`app_files\…` eller `App_files\…`). Brug altid `AppFilesHelper.GetFullPath`, aldrig `Path.Combine` direkte.
- `IsDeleted` er en gammel soft-delete. Ingen kode sætter den længere, men gamle rækker kan have den og behandles som slettede. `DeleteAsync` sletter rækken helt.
- Kategorierne (`FileCategories`) og undermapperne har små bogstaver af historiske grunde (kendt afvigelse fra `files.md` regel 2).
- Andre repositories må gerne læse `FileMetadata` via navigationen (navn, størrelse, type), men kun dette repository skriver i tabellen.
