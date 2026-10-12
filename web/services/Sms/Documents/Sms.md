# Sms (service)

**Formål:** Klient til den eksterne SMS-gateway: send og læs SMS, status, forbrug/saldo og abonnementer på indgående SMS.
**Brug det når:** opgaven skal sende eller hente SMS'er, eller vise gatewayens helbred, pris og forbrug.
**Brug det ikke til:** at gemme SMS'er i appens database. Det gør `SmsWorker` (tabellen `SmsMessages`).
**Eksternt system og konfiguration:** SMS-gatewayens HTTP-API. Sektionen `Sms` i `appsettings.json`: `BaseUrl`, `ApiKey` (secret, sendes som `x-api-key`) og `WorkerIntervalSeconds` (bruges af `SmsWorker`). Light pattern: typed `HttpClient` med `BaseAddress` sat i `Program.cs`.

## Metoder
| Gruppe (`Dtos/<Gruppe>/`) | Metoder | Beskrivelse |
|---|---|---|
| Health | `GetHealthAsync` | Gatewayens helbred. |
| Cost | `GetCurrentCostAsync`, `GetCostHistoryAsync`, `GetUsageReportCostAsync`, `GetBalanceCostAsync` | Pris, historik, forbrugsrapport og saldo. |
| Sms | `SendSmsAsync`, `GetSmsStatusAsync`, `ReadSmsAsync`, `DeleteSmsAsync` | Send en SMS, hent leveringsstatus, læs indgående SMS'er (evt. filtreret på nummer) og slet én. |
| Subscriptions | `GetAllSubscriptionsAsync`, `GetSubscriptionByIdAsync`, `CreateSubscriptionAsync`, `UpdateSubscriptionAsync`, `DeleteSubscriptionAsync` | Abonnementer på indgående SMS. |

## Faldgruber
- Alle metoder tager en valgfri `apiKey`. Udelades den, bruges `Sms:ApiKey`.
- Er `Sms:BaseUrl` tom, får klienten en dummy-adresse (`http://localhost/`), så DI ikke fejler. Kaldene fejler så med `HttpRequestException`. `SmsWorker` registreres kun, når både `BaseUrl` og `ApiKey` er sat.
- Fejlsvar kastes som `HttpRequestException` og ikke som en egen exception med statuskode. `GlobalExceptionHandler` giver derfor 500.
- `Delete…`-metoderne returnerer `false`, når gatewayen svarer 404.
