# AppSettings (repository)

**Formål:** Appens indstillinger som nøgle/værdi (tabel `AppSettings`, nøglerne står i `AppSettingKeys`).
**Brug det når:** en indstilling fra Indstillinger skal læses eller gemmes (offentlig registrering, aktivt tema og tilstand).
**Brug det ikke til:** konfiguration fra `appsettings.json`. Brug `IConfiguration`.
**Tabeller/filer:** `AppSettings`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetValueAsync(string key)` | Den gemte værdi, eller `null`, hvis nøglen ikke findes. Kalderen giver standardværdien. |
| `SetValueAsync(string key, string value)` | Opretter eller retter indstillingen. |

## Faldgruber
- `AppSetting` har en tekstnøgle og arver ikke `EntityBase`, så `CreatedAtUtc`/`UpdatedAtUtc` sættes her.
- Repositoriet har ingen `Dtos/` eller `Mappers/`, fordi metoderne kun bruger simple typer.
