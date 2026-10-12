# ActivityLists (manager)

**Formål:** Arbejdslister ud over list-repositoriet: Excel-import og -eksport og udsendelse af de personlige `/Arbejdsliste`-links.
**Brug det når:** en liste skal oprettes fra et Excel-ark, eksporteres til Excel, eller links skal sendes til eksterne kontakter.
**Brug det ikke til:** alt andet på listerne. Kald `IActivityListRepository` direkte.
**Bruger:** `IActivityListRepository`, `IActivityRepository`, `ICommunicationEmailMessageRepository`, `ISmsMessageRepository` og `ActivityListExcel` (ClosedXML)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CreateFromExcelAsync(CreateActivityListFromExcelDto dto)` | Tjekker aktiviteten og filtypen (.xlsx), læser arket (første række er overskrifter) og opretter listen. |
| `ExportAsync(SearchActivityListItemsDto search, string? userId)` | Hele listen eller linjerne i søgningen som .xlsx med statusfarver. Filnavnet får personens navn, "mine", "uden tilknytning" eller "filtreret". |
| `SendLinksAsync(SendActivityListLinksDto dto, string baseUrl)` | Lægger en e-mail og/eller SMS med hver valgt ekstern kontakts personlige link i kø. Kontakter uden e-mail/mobil nævnes i svaret. |

## Faldgruber
- Excel-import og -eksport ligger her, selvom de kun bruger ét repository, så fil-logikken ikke ligger i controlleren (bevidst afvigelse fra hybrid-reglen).
- E-mails og SMS'er lægges i kø i to gemninger. Fejler den anden, er e-mailene allerede i kø.
- Content-strømmen ved import skal kunne søges i (ClosedXML læser den som zip). Controlleren kopierer uploaden til en `MemoryStream`.
