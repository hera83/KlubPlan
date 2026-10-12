# LabelCollections (repository)

**Formål:** Værktøjer → Labels: label-samlinger designet i label-designeren (tekst, løbenummer, billeder, QR-koder) med A4-arkets inddeling og et billedbibliotek pr. samling.
**Brug det når:** opgaven handler om label-samlinger, deres labels eller hvilke billeder der hører til en samling.
**Brug det ikke til:** labels på arbejdslister (`ActivityListLabels`, et andet system) eller selve billedfilerne (de ejes af `Files`; upload/sletning/PDF går gennem `LabelCollectionManager`).
**Tabeller/filer:** `LabelCollections`, `LabelCollectionItems`, `LabelCollectionMedia` (peger på `FileMetadata`)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchLabelCollectionsDto search)` | Side med samlinger sorteret efter navn. Søger i navn og i labelsenes tekster (gemt JSON). |
| `ExistsAsync(int id)` | Findes samlingen? |
| `CreateAsync(CreateLabelCollectionDto dto)` | Opretter samlingen med én startlabel, der viser navnet. Kolonner/rækker klemmes til de tilladte grænser. `Invalid` ved tomt navn. |
| `DeleteAsync(int id)` | Sletter samling, labels og billedrækker. Returnerer de fil-id'er, der skal slettes, eller `null`. |
| `GetDesignerAsync(int id)` | Det gemte design og billedbiblioteket (nyeste først), eller `null`. |
| `SaveDesignAsync(int id, LabelCollectionDesignDto design)` | Validerer/normaliserer (`LabelCollectionDesignNormalizer`) og erstatter navn, inddeling og labels. `Invalid` med en dansk fejltekst. |
| `AddMediaAsync(int id, int fileId)` | Knytter en gemt fil til samlingens bibliotek. `null`, hvis samlingen ikke findes. |
| `RemoveMediaAsync(int id, Guid mediaId)` | Fjerner et billede (`InUse`, hvis en gemt label bruger det). Returnerer fil-id'et, der skal slettes. |
| `GetMediaFileIdAsync(Guid mediaId)` | Fil-id bag et billed-id. |
| `GetPrintAsync(int id)` | Design og fil-id pr. billede til PDF'en. |

## Faldgruber
- Labels gemmes som JSON (`ElementsJson`) med `UnsafeRelaxedJsonEscaping` og uden null-felter, så søgningen kan finde æ/ø/å i teksterne. Designeren og `Designer.cshtml` bruger `LabelCollectionMapper.JsonOptions`.
- "I brug"-tjekket ser efter billed-id'et som tekst i de gemte labels' JSON.
