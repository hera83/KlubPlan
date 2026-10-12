# Users (repository)

**Formål:** Brugerne med login (`ApplicationUser`, ASP.NET Core Identity): opslag og ændring af profil, tema og profilbillede.
**Brug det når:** opgaven handler om en brugers egne data (profil, tema, profilbillede).
**Brug det ikke til:** personregistret (det er `People`) eller selve profilbilledfilen (det er `Files`; kombinationen ligger i `ProfileManager`).
**Tabeller/filer:** `AspNetUsers` (Identity). Skriver via `UserManager<ApplicationUser>`, så Identitys validering og security stamp følger med.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetByIdAsync(string id)` | Én bruger, eller `null`. |
| `AnyAsync()` | Findes der nogen brugere? (Ingen = opsætning af første bruger.) |
| `SearchAsync(SearchUsersDto search)` | Brugeradministrationens tabel: brugere efter navn med roller, filtreret på navn/e-mail, rolle og aktiv. Sidestørrelse 5-200, ellers 10. |
| `GetActiveInRolesAsync(IReadOnlyCollection<string> roleNames)` | Aktive brugere med mindst én af rollerne, sorteret efter visningsnavn (fx administratorer til møder). |
| `UpdateProfileAsync(string id, UpdateUserProfileDto dto)` | Visningsnavn og telefon, evt. nyt brugernavn og nyt password. Udfald: `Updated`, `NotFound`, `UserNameTaken` eller `Rejected` (med Identitys fejltekst). |
| `SetThemePreferenceAsync(string id, string themePreference)` | Gemmer brugerens tema (Light/Dark/System). |
| `SetProfilePictureAsync(string id, int? fileId)` | Peger brugerens profilbillede på en fil i `Files` (eller fjerner det). |

## Faldgruber
- `ApplicationUser` arver `IdentityUser` (string-id) og ikke `EntityBase`, så `CreatedAtUtc`/`UpdatedAtUtc` sættes stadig i hånden her.
- `UpdateProfileAsync` stopper ved første fejl. Er visningsnavnet allerede gemt, når brugernavn eller password fejler, bliver det ikke rullet tilbage (som før).
- Account-, Settings- og Setup-controllerne bruger stadig `UserManager`/`WebDbContext` direkte (kendt afvigelse).
