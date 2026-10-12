# Profile (manager)

**Formål:** Profilbilleder: kombinerer brugeren (`Users`) med selve filen (`Files`).
**Brug det når:** et profilbillede skal sættes, fjernes eller vises.
**Brug det ikke til:** resten af profilen (navn, telefon, brugernavn, password, tema). Kald `IUserRepository` direkte.
**Bruger:** `IUserRepository`, `IFileRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SetAvatarAsync(string userId, Stream image, string contentType, string originalFileName)` | Gemmer billedet i `app_files/avatars/`, peger brugeren på det og sletter det forrige. `false`, hvis brugeren ikke findes. |
| `DeleteAvatarAsync(string userId)` | Fjerner billedet fra brugeren og sletter filen. `false`, hvis brugeren ikke findes eller intet billede har. |
| `GetAvatarAsync(string userId)` | Billedfilen, eller `null`. |

## Faldgruber
- Det nye billede gemmes før det gamle slettes, så brugeren aldrig står uden billede, hvis noget fejler halvvejs.
- Filtype og størrelse valideres i `ProfileController` (JPEG/PNG/WebP, maks. 5 MB), før manageren kaldes.
