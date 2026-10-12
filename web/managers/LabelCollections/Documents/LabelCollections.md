# LabelCollections (manager)

**Formål:** Label-samlingshandlinger, der også rører billedfilerne: kombinerer `LabelCollections` og `Files`.
**Brug det når:** der skal uploades, slettes eller hentes et billede, en hel samling skal slettes, eller PDF'en skal dannes.
**Brug det ikke til:** tabel, oprettelse og designer. Kald `ILabelCollectionRepository` direkte.
**Bruger:** `ILabelCollectionRepository`, `IFileRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `UploadMediaAsync(int id, Stream content, string fileName, long length, string? ownerId)` | Tjekker filtype (content type fra `LabelDesignRules`) og størrelse, gemmer billedet i `app_files/labels/{id}/` og knytter det til samlingen. |
| `DeleteMediaAsync(int id, Guid mediaId)` | Fjerner billedet fra biblioteket og sletter filen. `InUse`/`NotFound` som udfald. |
| `DeleteAsync(int id)` | Sletter samlingen og alle dens billedfiler. `false`, hvis den ikke findes. |
| `GetMediaFileAsync(Guid mediaId)` | Filen bag et billed-id. |
| `BuildPdfAsync(int id, bool cutMarks)` | A4-labelark (`LabelDesignPdf`) med hver label gentaget `Quantity` gange. `LabelPdfResult.Fail` ved ingen labels, for mange labels eller fejl i dannelsen. `null`, hvis samlingen ikke findes. |

## Faldgruber
- Billeder, hvis fil mangler på disken, udelades bare fra PDF'en.
