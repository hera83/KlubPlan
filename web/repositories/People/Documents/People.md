# People (repository)

**Formål:** Personregistret: opretter, henter, retter og sletter personer med gruppemedlemskaber (rolle pr. gruppe) og pårørende.
**Brug det når:** opgaven handler om personer (medlemmer, spillere, trænere) i appens egen database.
**Brug det ikke til:** grupperne selv (det er `PersonGroups`) eller brugere med login (`ApplicationUser`).
**Tabeller/filer:** `People`, `PersonGroupMemberships`, `PersonGuardians`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchPeopleDto search)` | Side med personer sorteret efter navn, filtreret på tekst (navn/UID/mobil/email), grupper og rolle. Paging 10-500 pr. side, ellers 10. |
| `GetByIdAsync(int id)` | Én person med medlemskaber og pårørende (i rækkefølge), eller `null`. |
| `GetOptionsAsync()` | Alle personer (id, UID, navn) sorteret efter navn, til vælgerlister. |
| `CountInGroupsAsync(groupIds)` | Antal personer, der er medlem af mindst én af grupperne. 0, hvis ingen grupper er givet. |
| `GetContactsAsync(groupIds, personIds)` | Kontaktoplysninger (egne + pårørendes i rækkefølge) for alle i grupperne plus de valgte personer, hver person én gang, sorteret efter id. Tom liste, hvis intet er valgt. |
| `IsInAnyGroupAsync(int personId, IReadOnlyCollection<int> groupIds)` | Er personen medlem af mindst én af grupperne? |
| `GetIdByPublicIdAsync(Guid publicId)` | Personens id ud fra `PublicId` (UId i offentlige links), eller `null`. |
| `CreateAsync(CreatePersonDto dto)` | Opretter en person. Tomt UID giver næste ledige nummer. `UidInUse`, hvis UID'et er taget. |
| `UpdateAsync(int id, UpdatePersonDto dto)` | Erstatter felter, medlemskaber og pårørende. `NotFound` eller `UidInUse` som udfald. |
| `DeleteAsync(int id)` | Sletter personen (medlemskaber og pårørende cascade). `false`, hvis den ikke findes. |
| `RegeneratePublicIdAsync(int id)` | Nyt `PublicId`, så gamle offentlige formular-links holder op med at virke. `false`, hvis den ikke findes. |
| `CreateManyInGroupAsync(int groupId, IReadOnlyList<string> names)` | Masse-oprettelse som spillere i én gruppe med fortløbende UID'er. `null`, hvis gemningen fejler. |

## Faldgruber
- UID er et frit, unikt tekstfelt. Automatiske UID'er er det højeste numeriske UID + 1, zero-padded til 6 cifre (`000036`).
- `UpdateAsync` med tomt UID giver personen et **nyt** automatisk UID (som i den oprindelige UI-adfærd).
- Pårørende med tomt navn springes over. Tomme mobil/email gemmes som `null`.
- Andre områder (Kommunikation, Tilmeldinger, Aktiviteter) læser stadig `People` direkte via deres egne services (kendt afvigelse).
