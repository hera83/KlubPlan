# Activities (repository)

**Formål:** Aktiviteter med målgrupper, arbejdsgruppe, opgaver og links til formularer.
**Brug det når:** en aktivitet, et arbejdsgruppe-medlem, en opgave eller et formular-link skal oprettes, rettes, slettes eller vises.
**Brug det ikke til:** sletning af en hel aktivitet (filerne skal med, brug `IActivityManager.DeleteAsync`), aktivitetens filer (`ActivityFiles`) eller arbejdslister (`ActivityLists`).
**Tabeller/filer:** `Activities`, `ActivityTargetGroups`, `ActivityWorkgroupMembers`, `ActivityTasks`, `ActivityForms`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchActivitiesDto search)` | Side med aktiviteter (søgning i titel, sted og kategori) sorteret efter start eller oprettelse, nyeste først, med målgruppenavne og antal (løste) opgaver. Sidestørrelse 10, hvis den ikke er sat. |
| `GetByIdAsync(int id)` | Aktiviteten med målgrupper, arbejdsgruppe, opgaver og linkede formularer (usorteret), eller `null`. |
| `ExistsAsync(int id)` | Findes aktiviteten? |
| `SaveAsync(SaveActivityDto dto)` | Opretter (`Id` 0) eller retter og erstatter målgrupperne. Tekster trimmes, tomme bliver `null`. Udfald: `TitleRequired`, `EndNotAfterStart`, `NotFound`. |
| `DeleteAsync(int id)` | Sletter aktiviteten (alt under den cascade, formularer og beskeder bevares). Slet filerne først. |
| `SaveWorkgroupMemberAsync(SaveActivityWorkgroupMemberDto dto)` | Opretter (sidst i rækkefølgen) eller retter et medlem, enten koblet til en administrator eller som ekstern kontakt. Udfald: `NameRequired`, `NotFound`, `ActivityNotFound`, `UserNotFound`, `AlreadyMember`. |
| `DeleteWorkgroupMemberAsync(int id)` | Fjerner medlemmet. Opgaver, det var ansvarlig for, bliver uden ansvarlig. |
| `SaveTaskAsync(SaveActivityTaskDto dto)` | Opretter (sidst i rækkefølgen) eller retter en opgave. Udfald: `TitleRequired`, `NotFound`, `ActivityNotFound`, `AssigneeNotFound`. |
| `ToggleTaskCompletedAsync(int id, string? userId)` | Skifter mellem løst og åben og gemmer hvem/hvornår. |
| `DeleteTaskAsync(int id)` | Sletter opgaven. |
| `LinkFormAsync(int activityId, int formId)` | Linker formularen, hvis den ikke allerede er linket. `false`, hvis aktiviteten ikke findes. |
| `UnlinkFormAsync(int activityId, int formId)` | Fjerner linket (formularen bevares). `false`, hvis den ikke er linket. |

## Faldgruber
- Et medlem koblet til en administrator har hverken navn, e-mail eller mobil selv. `ActivityWorkgroupMemberDto.Email`/`Mobile` kommer fra kontoen.
- Sortering til visning (målgrupper, arbejdsgruppe, opgaver, formularer) sker i ViewModel-mapperen.
- Tidspunkter er UTC. Konvertering fra/til `datetime-local` sker i mapperen.
