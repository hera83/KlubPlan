# InfoScreens (repository)

**Formål:** Værktøjer → Infoskærme: skærme (slideshows) med slides fra designeren, et mediebibliotek pr. skærm og data til den offentlige afspiller.
**Brug det når:** opgaven handler om infoskærme, deres slides eller hvilke medier der hører til en skærm.
**Brug det ikke til:** selve mediefilerne. De er rækker i `FileMetadata` og ejes af `Files`. Upload/sletning af filer går gennem `InfoScreenManager`.
**Tabeller/filer:** `InfoScreens`, `InfoScreenSlides`, `InfoScreenMedia` (peger på `FileMetadata`)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchInfoScreensDto search)` | Side med skærme sorteret efter titel, filtreret på tekst og aktiv/inaktiv. |
| `ExistsAsync(int id)` | Findes skærmen? |
| `CreateAsync(CreateInfoScreenDto dto)` | Opretter skærmen med én startside, der viser titlen. `Invalid` ved tom titel. |
| `SetActiveAsync(int id, bool isActive)` | (De)aktiverer og bumper `UpdatedAt`, så kørende skærme opdaterer. |
| `DeleteAsync(int id)` | Sletter skærm, slides og medierækker. Returnerer de fil-id'er, der skal slettes, eller `null`. |
| `GetDesignerAsync(int id)` | Det gemte design og mediebiblioteket (nyeste først), eller `null`. |
| `SaveDesignAsync(int id, InfoScreenDesignDto design)` | Validerer/normaliserer (`InfoScreenDesignNormalizer`) og erstatter indstillinger og slides. `Invalid` med en dansk fejltekst ved regelbrud. |
| `AddMediaAsync(int id, int fileId, string kind)` | Knytter en gemt fil til skærmens bibliotek. `null`, hvis skærmen ikke findes. |
| `RemoveMediaAsync(int id, Guid mediaId)` | Fjerner en fil fra biblioteket (`InUse`, hvis en gemt side bruger den). Returnerer fil-id'et, der skal slettes. |
| `GetMediaFileIdAsync(Guid mediaId)` | Fil-id bag et medie-id. |
| `GetPlayerAsync(Guid publicId, bool preview)` | Afspillerens data: status (`ok`/`inactive`/`notfound`), version (`UpdatedAt.Ticks`) og synlige slides. |
| `GetTitleAsync(Guid publicId)` | Titel til afspillersidens `<title>`. |

## Faldgruber
- Slides gemmes som JSON (`ElementsJson`) i camelCase med `InfoScreenMapper.JsonOptions`. Designeren, afspilleren og `Designer.cshtml` bruger samme options.
- `Version` i afspillerdata er `UpdatedAt.Ticks`. Alt, der skal få kørende skærme til at genindlæse, skal markere skærmen som ændret.
- "I brug"-tjekket ser efter medie-id'et som tekst i de gemte slides' JSON.
