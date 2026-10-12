# CommunicationEmailMessages (repository)

**Formål:** Køen af udgående e-mails fra Kommunikation.
**Brug det når:** køen skal sendes og hver e-mail markeres som sendt eller fejlet.
**Brug det ikke til:** at lægge en besked fra Kommunikation i kø. Det sker sammen med beskeden i `ICommunicationMessageRepository.MarkSentAsync`.
**Tabeller/filer:** `CommunicationEmailMessages` (læser vedhæftninger via `CommunicationMessageAttachments`)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetPendingAsync()` | E-mails med status `Pending`, ældste først, med id'erne på beskedens vedhæftede filer. |
| `QueueAsync(emails)` | Lægger e-mails uden en besked bag (fx arbejdsliste-links) i kø med status `Pending` i én gemning. |
| `MarkSentAsync(int id)` | Status `Sent` og `SentAtUtc`. `false`, hvis e-mailen ikke findes. |
| `MarkFailedAsync(int id, string failureReason)` | Status `Failed`, fejltekst og `FailedAtUtc`. `false`, hvis e-mailen ikke findes. |

## Faldgruber
- En e-mail, hvis besked er slettet, har `CommunicationMessageId = null` og sendes uden vedhæftninger.
