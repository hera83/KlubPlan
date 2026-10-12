# Communication (manager)

**Formål:** Kommunikation på tværs af `CommunicationMessages`, `People`, `PersonGroups`, `Forms`, `Arrangements` og `Files`: skrive, sende og følge op på beskeder via e-mail og SMS.
**Brug det når:** compose-dialogen skal have valglister, beskeder skal vises med modtagere, gemmes, sendes, sendes igen eller slettes, eller en vedhæftning skal hentes.
**Brug det ikke til:** at sende køen af e-mails. Det gør `ICommunicationEmailManager` (og `SmsWorker` for SMS).
**Bruger:** `ICommunicationMessageRepository`, `IPersonRepository`, `IPersonGroupRepository`, `IFormRepository`, `IArrangementRepository`, `IFileRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetComposeOptionsAsync()` | Grupper, personer, formularer (seneste åbne version) og arrangementer, der ikke er lukket for tilmelding. |
| `GetMessagesAsync(int? activityId)` | Beskedlisten (alle eller én aktivitets) plus antal grupper til "Alle"-badget. |
| `GetDetailsAsync(int id)` | Beskeden, antal grupper og første side af modtagertabellen, eller `null`. |
| `GetRecipientsAsync(int id, SearchCommunicationRecipientsDto search)` | Hele målgruppen (også dem uden kontaktoplysninger) med leveringsstatus, filtreret på navn og `MissingEmail`/`MissingSms`/`Sent`. Paging 5-200, ellers 10. |
| `GetResendTargetsAsync(int id)` | Målgruppen til "Send igen", markeret med hvem der har fået den før. |
| `SaveAsync(SaveCommunicationDto dto, string baseUrl)` | Validerer, gemmer kladden og vedhæftninger (kun ved e-mail, i `app_files/communication/`) og sender, hvis `Send` er sat. Udfald: se `SendCommunicationStatus`. |
| `SendExistingAsync(int id, string baseUrl, recipientPersonIds)` | Sender en gemt besked til hele målgruppen eller kun de valgte personer inden for målgruppen. |
| `DeleteAsync(int id)` | Sletter beskeden og dens vedhæftede filer. |
| `GetAttachmentFileAsync(int attachmentId)` | Den gemte fil til download, eller `null`. |

## Faldgruber
- Modtagere findes ud fra personernes egne og pårørendes kontaktoplysninger. Personer behandles efter id, og den første, der "ejer" en adresse, får den. En fælles forælder får derfor kun én besked.
- Ved et begrænset arrangement filtreres grupper og personer mod adgangslisten på serveren, uanset hvad klienten sender.
- Ikke-anonyme formularer får altid et personligt link. Tilmeldingslinks er altid personlige.
- Et delvist "Send igen" ændrer ikke beskedens antal modtagere eller opsummering.
- `{{Navn}}` i teksten erstattes med modtagerens navn (den person, adressen er fundet for).
