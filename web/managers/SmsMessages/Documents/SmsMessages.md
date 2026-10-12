# SmsMessages (manager)

**Formål:** Holder tabellen `SmsMessages` i takt med SMS-gatewayen (`ISmsService`).
**Brug det når:** `SmsWorker` skal sende, opdatere status eller hente indgående SMS'er, eller en SMS i kø skal slettes (og annulleres i gatewayen).
**Brug det ikke til:** at lægge SMS'er i kø eller vise loggen. Kald `ISmsMessageRepository` direkte.
**Bruger:** `ISmsMessageRepository`, `ISmsService`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SendOutboundAsync()` | Sender alle udgående SMS'er uden gateway-id. Fejl markeres `Failed` og forsøges igen næste gang. |
| `PollOutboundStatusesAsync()` | Opdaterer status for afsendte SMS'er fra de sidste 7 dage, som ikke er endelige endnu. |
| `FetchInboundAsync()` | Henter og gemmer nye indgående SMS'er. |
| `DeleteAsync(int id)` | Sletter en SMS med status `Queued` eller `Failed`. En SMS i gatewayens kø annulleres først. Udfald: `NotFound`, `NotDeletable`, `GatewayFailed`. |

## Faldgruber
- Telefonnumre logges ikke (persondata), kun SMS'ens id.
