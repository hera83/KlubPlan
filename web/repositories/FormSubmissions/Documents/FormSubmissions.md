# FormSubmissions (repository)

**Formål:** Besvarelser af formularer og deres svar: gem, tæl og vis.
**Brug det når:** der skal gemmes en (allerede valideret) besvarelse, eller besvarelserne skal vises/eksporteres.
**Brug det ikke til:** validering af svar eller adgangskontrol for offentlige links (det gør `FormManager`).
**Tabeller/filer:** `FormSubmissions`, `FormAnswers` (læser `AspNetUsers` og `People` for navne)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CreateAsync(CreateFormSubmissionDto dto)` | Gemmer besvarelsen med svar og tidspunkt. Returnerer id'et. |
| `CountAsync(int formId)` | Antal besvarelser på formularen. |
| `GetPageAsync(int formId, int page, int pageSize)` | Besvarelser, nyeste først. "Besvaret af" er brugerens navn, personens navn eller "Anonym" ("Ukendt bruger/person", hvis de er slettet). |
| `HasPersonSubmittedAsync(int formId, int personId)` | Har personen allerede svaret via et offentligt link? |

## Faldgruber
- `SubmittedAtUtc` er besvarelsens forretningstidspunkt og sættes ved oprettelse (ud over `CreatedAt` fra `EntityBase`).
- Højst én af `SubmittedByUserId`/`SubmittedByPersonId` er sat. Ingen af dem på anonyme formularer.
