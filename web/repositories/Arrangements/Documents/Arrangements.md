# Arrangements (repository)

**Formål:** Arrangementer (Tilmeldinger): et arrangement med egen tilmeldingsformular, vagter med krav pr. vagt, et tilmeldingsvindue og en adgangsliste (personer/grupper).
**Brug det når:** opgaven handler om selve arrangementet: oprette, rette, (tvangs)åbne tilmelding, slette eller vise det.
**Brug det ikke til:** tilmeldingerne (det er `ArrangementRegistrations`) eller det offentlige `/Tilmelding`-link (det er `ArrangementManager`).
**Tabeller/filer:** `Arrangements`, `ArrangementFormFields`, `ArrangementShifts`, `ArrangementShiftRequirements`, `ArrangementAllowedPersons`, `ArrangementAllowedGroups`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchArrangementsDto search)` | Side med arrangementer, nyeste først, med første vagts start, antal pladser og tilmeldte. Paging 5-200, ellers 10. |
| `GetLinkOptionsAsync()` | Alle arrangementer sorteret efter titel med adgangsliste og tilmeldingsperiode. Til link-vælgeren i Kommunikation. |
| `GetByIdAsync(int id)` / `GetByPublicIdAsync(Guid publicId)` | Arrangementet med felter (i rækkefølge), vagter (efter start, så rækkefølge) med krav og adgangsliste, eller `null`. |
| `SaveAsync(SaveArrangementDto dto)` | Opretter (`Id` 0) eller retter og afstemmer felter, vagter og krav mod de eksisterende ud fra id. Adgangslisten genopbygges (kun ved `Restricted`). `NotFound` eller `Invalid` (titel, sluttid efter start, labels, muligheder, vagttitler). |
| `ToggleRegistrationForcedOpenAsync(int id)` | Lukket → midlertidigt tvangsåben (overstyrer datoerne) → tilbage til datoerne. Returnerer den nye tilstand, eller `null`. |
| `DeleteAsync(int id)` | Sletter arrangementet med alt under det, også tilmeldinger. |

## Faldgruber
- Alle tider er UTC. Builderens `datetime-local`-felter er lokal tid og konverteres i `RegistrationsMapper` (`FromLocalToUtc`/`FromUtcToLocal`).
- Slettes en vagt, forsvinder tilmeldingernes pladser på den vagt (cascade).
- Krav med tom tekst springes over ved gem.
