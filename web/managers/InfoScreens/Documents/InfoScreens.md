# InfoScreens (manager)

**Formål:** Infoskærm-handlinger, der også rører mediefilerne: kombinerer `InfoScreens` og `Files`.
**Brug det når:** der skal uploades, slettes eller hentes en mediefil, eller en hel skærm skal slettes.
**Brug det ikke til:** tabel, oprettelse, (de)aktivering, designer og afspillerdata. Kald `IInfoScreenRepository` direkte.
**Bruger:** `IInfoScreenRepository`, `IFileRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId)` | Tjekker filtype (content type fra `InfoScreenRules`, aldrig fra browseren) og størrelse, gemmer filen i `app_files/infoscreens/{id}/` og knytter den til skærmen. |
| `DeleteMediaAsync(int id, Guid mediaId)` | Fjerner filen fra biblioteket og sletter den. `InUse`/`NotFound` som udfald. |
| `DeleteAsync(int id)` | Sletter skærmen og alle dens mediefiler. `false`, hvis den ikke findes. |
| `GetMediaFileAsync(Guid mediaId)` | Filen bag et medie-id til afspilleren (range requests i controlleren). |

## Faldgruber
- Bliver skærmen slettet, mens en fil uploades, slettes den nye fil igen, og udfaldet er `NotFound`.
