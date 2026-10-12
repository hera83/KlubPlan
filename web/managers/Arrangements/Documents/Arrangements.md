# Arrangements (manager)

**Formål:** Arrangementshandlinger på tværs af `Arrangements`, `ArrangementRegistrations`, `People` og `PersonGroups`.
**Brug det når:** builderen skal have valglister, det offentlige `/Tilmelding`-link skal vises eller indsendes, eller tilmeldingerne skal vises/eksporteres.
**Brug det ikke til:** tabel, gem, tvangsåbning og sletning. Kald `IArrangementRepository` direkte.
**Bruger:** `IArrangementRepository`, `IArrangementRegistrationRepository`, `IPersonRepository`, `IPersonGroupRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetBuilderAsync(int? id)` | Arrangementet (`null` id = opret) plus alle grupper og personer til Adgang-fanen. `null`, hvis id'et ikke findes. |
| `GetPublicAsync(Guid arrangementPublicId, Guid? personPublicId)` | Adgangskontrol: `NotFound`, `NotYetOpen`, `Closed`, `MissingIdentity`, `InvalidIdentity`, `NotAllowed`, `AlreadyRegistered` eller `Ok`. Ved `Ok` også personens navn og optagne pladser pr. vagt. |
| `SubmitPublicAsync(Guid arrangementPublicId, Guid? personPublicId, SubmitPublicArrangementDto input)` | Tilmelding via linket: adgang, svar, valg af vagter, navngivne pladser, kapacitet og krav tjekkes igen, før der gemmes. |
| `GetRegistrationsAsync(int arrangementId, int page, int pageSize)` | En side tilmeldinger med en kolonne pr. besvarbart felt. Paging 5-500, ellers 10. |
| `GetAllRegistrationsAsync(int arrangementId)` | Alle tilmeldinger på én side (CSV-eksport). |

## Faldgruber
- Tilmelding har ingen anonym tilstand. UId kræves altid.
- Ved `Restricted` er personen tilladt, hvis den står direkte på listen eller er medlem af en tilladt gruppe.
- Navngivne pladser tæller kun, når arrangementet tillader flere navne pr. vagt. Ellers ignoreres de.
- Kapacitet tjekkes mod databasen ved indsendelse, ikke kun i browseren, fordi andre kan have tilmeldt sig i mellemtiden.
