# CommunicationMessages (repository)

**Formål:** Beskeder fra Kommunikation: kladder med valgte grupper/personer, vedhæftninger og, når de er sendt, modtagerrækkerne med deres SMS'er og e-mails i kø.
**Brug det når:** en besked skal vises, gemmes som kladde, markeres som sendt eller slettes.
**Brug det ikke til:** at finde modtagere, bygge tekster og links eller gemme filer. Det gør `ICommunicationManager`.
**Tabeller/filer:** `CommunicationMessages`, `CommunicationMessageGroups`, `CommunicationMessageRecipientPersons`, `CommunicationMessageRecipients`, `CommunicationMessageAttachments` + nye rækker i `SmsMessages` og `CommunicationEmailMessages`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetListAsync(int? activityId)` | Beskeder, nyeste først, med grupper og direkte valgte personer. Alle, eller kun dem fra én aktivitet. |
| `GetByIdAsync(int id)` | Beskeden med formular-/arrangementstitel, grupper, personer og vedhæftninger, eller `null`. |
| `GetRecipientsAsync(int id)` | Alle adresser, beskeden er sendt til (alle afsendelser), med status fra den tilhørende SMS/e-mail. |
| `SaveDraftAsync(SaveCommunicationMessageDto dto)` | Opretter (`Id` 0) eller retter en kladde og erstatter grupper/personer. Udfald: `Saved`, `NotFound`, `NotDraft`. |
| `AddAttachmentAsync(int messageId, int fileMetadataId)` | Knytter en gemt fil til beskeden. `false`, hvis beskeden ikke findes. |
| `GetAttachmentFileIdAsync(int attachmentId)` | Den gemte fil bag en vedhæftning, eller `null`. |
| `MarkSentAsync(MarkCommunicationMessageSentDto dto)` | Tilføjer modtagerrækker, lægger én SMS eller e-mail i kø pr. modtager og markerer beskeden sendt, i én `SaveChangesAsync`. `false`, hvis beskeden ikke findes. |
| `DeleteAsync(int id)` | Sletter beskeden med valg, modtagerrækker og vedhæftningslinks. Returnerer vedhæftningernes gemte filer, eller `null`, hvis den ikke findes. |

## Faldgruber
- `MarkSentAsync` opretter selv rækkerne i `SmsMessages` og `CommunicationEmailMessages`, så afsendelsen er atomar uden en transaktion på tværs af repositories. Køen sendes af `SmsWorker` og `CommunicationEmailWorker`.
- SMS'er og e-mails i kø bliver liggende, når beskeden slettes (de er leveringsloggen). Filerne skal slettes af kalderen, så brug `ICommunicationManager.DeleteAsync`.
- Sortering af grupper, personnavne og vedhæftninger sker i ViewModel-mapperen, ikke her.
