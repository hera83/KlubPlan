# SmsMessages (repository)

**Formål:** SMS'er: den udgående kø, som `SmsWorker` sender, de indgående, den henter, og SMS-loggen under Indstillinger.
**Brug det når:** en SMS skal lægges i kø uden en besked fra Kommunikation bag (arbejdsliste-links, test-SMS), loggen skal vises, eller en fejlet SMS skal sendes igen.
**Brug det ikke til:** SMS'er fra Kommunikation (de lægges i kø sammen med beskeden i `ICommunicationMessageRepository.MarkSentAsync`) eller kald til SMS-gatewayen (`ISmsMessageManager`).
**Tabeller/filer:** `SmsMessages`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchSmsMessagesDto search)` | Loggen, nyeste først, filtreret på retning og tekst i nummer, besked og status. Sidestørrelse 5-200, ellers 10. |
| `GetByIdAsync(int id)` | Én SMS, eller `null`. |
| `QueueAsync(messages)` | Lægger udgående SMS'er i kø med status `Pending` i én gemning. |
| `RequeueAsync(int id)` | Lægger en fejlet udgående SMS i kø igen. Udfald: `NotFound`, `NotFailed`. |
| `DeleteAsync(int id)` | Sletter rækken (gatewayen annulleres af `ISmsMessageManager.DeleteAsync`). |
| `GetUnsubmittedAsync()` | Udgående SMS'er uden gateway-id, ældste først, også dem, der fejlede ved afsendelse. |
| `ApplySubmitResultAsync(int id, SmsGatewayStatusDto? result)` | Gemmer gatewayens svar på en afsendelse. |
| `MarkFailedAsync(int id, string failureReason)` | Status `Failed` med fejltekst. |
| `GetInFlightAsync(DateTime createdSinceUtc)` | Afsendte SMS'er, som gatewayen endnu ikke har meldt sendt eller fejlet. |
| `ApplyStatusAsync(int id, SmsGatewayStatusDto status)` | Gemmer en status hentet fra gatewayen. |
| `AddInboundAsync(messages)` | Tilføjer indgående SMS'er, der ikke findes i forvejen (efter gateway-id). |

## Faldgruber
- En SMS, der fejler ved afsendelse, får ikke et gateway-id og forsøges derfor igen ved næste kørsel.
