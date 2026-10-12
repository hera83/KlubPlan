# CommunicationEmails (manager)

**Formål:** Sender køen af udgående e-mails fra Kommunikation med vedhæftninger.
**Brug det når:** `CommunicationEmailWorker` skal sende køen.
**Brug det ikke til:** at lægge e-mails i kø (det sker, når en besked sendes via `ICommunicationManager`).
**Bruger:** `ICommunicationEmailMessageRepository`, `IFileRepository`, `IMailService`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SendPendingAsync()` | Sender alle ventende e-mails én ad gangen og markerer hver som `Sent` eller `Failed` (med fejltekst). Hver vedhæftet fil læses én gang pr. gennemløb. |

## Faldgruber
- Vedhæftede filer, der mangler på disk, udelades uden fejl.
- Adressen logges ikke ved fejl (persondata), kun e-mailens id.
