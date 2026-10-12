# Ollama (service)

**Formål:** Direkte klient til en Ollama-server: generering, chat (også streaming), embeddings og modelstyring.
**Brug det når:** opgaven skal tale direkte med en Ollama-server uden om AiGateway (Chat-widget og Indstillinger → Ollama).
**Brug det ikke til:** AI gennem gatewayen. Det er `AiGateway`-servicen, som også har Ollama-endpoints og tale/lyd.
**Eksternt system og konfiguration:** Ollamas HTTP-API (`/api/...`). Sektionen `Ollama` i `appsettings.json`: `BaseUrl`, `ApiKey` (secret, sendes som `Authorization: Bearer`, når den er sat), `RequestTimeoutSeconds` og standardværdierne `DefaultChatModel`, `DefaultGenerateModel`, `DefaultEmbeddingModel`, `DefaultKeepAlive`, `DefaultLanguage` og `UseStreamingByDefault`. Full pattern.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GenerateAsync` | Tekstgenerering ud fra en prompt. |
| `ChatAsync` / `ChatStreamAsync` | Chat med historik. `ChatStreamAsync` returnerer `IAsyncEnumerable`. |
| `EmbedAsync` | Embeddings. |
| `ListModelsAsync` / `ListRunningModelsAsync` | Installerede og indlæste modeller. |
| `ShowModelAsync`, `CreateModelAsync`, `CopyModelAsync`, `PullModelAsync`, `PushModelAsync`, `DeleteModelAsync` | Modelstyring. `CopyModelAsync`/`DeleteModelAsync` returnerer `bool`. |
| `GetVersionAsync` | Ollama-serverens version. |

## Faldgruber
- Fejl kastes som `OllamaException` med Ollamas statuskode. `GlobalExceptionHandler` giver samme status videre.
- DTO'erne ligger stadig i `Dto/` med `Ollama`-præfiks og uden `Dto`-suffiks (se kendte afvigelser i projektets `.claude/CLAUDE.md`).
