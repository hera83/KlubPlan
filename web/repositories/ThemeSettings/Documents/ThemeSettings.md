# ThemeSettings (repository)

**Formål:** Farvetemaerne: én række pr. temanavn og tilstand (Light/Dark), seedet af `ThemeSettingSeeder`.
**Brug det når:** `/theme.css` skal bygges, eller temavælgeren under Indstillinger skal vises eller valideres.
**Brug det ikke til:** at gemme det aktive tema. Det er en indstilling i `IAppSettingRepository`.
**Tabeller/filer:** `ThemeSettings`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetAllAsync()` | Alle temaer sorteret efter tilstand. |
| `GetAsync(string name, string themeMode)` | Temaet med navnet i tilstanden (`ThemeMode.Light`/`Dark`), eller `null`. |
