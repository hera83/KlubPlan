# ArrangementRegistrations (repository)

**Formål:** Tilmeldinger til arrangementer: tilmeldingen, svarene og de vagtpladser, den tager.
**Brug det når:** der skal gemmes en (allerede valideret) tilmelding, eller tilmeldinger og optagne pladser skal vises/tælles.
**Brug det ikke til:** adgangskontrol og validering af tilmeldinger (det gør `ArrangementManager`).
**Tabeller/filer:** `ArrangementRegistrations`, `ArrangementRegistrationAnswers`, `ArrangementRegistrationShifts`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CreateAsync(CreateArrangementRegistrationDto dto)` | Gemmer tilmeldingen med svar, én række pr. optaget vagtplads og tidspunkt. |
| `CountAsync(int arrangementId)` | Antal tilmeldinger. |
| `GetPageAsync(int arrangementId, int page, int pageSize)` | Tilmeldinger, nyeste først, med personens navn, svar og vagtpladser (vagtens titel og tider). |
| `ExistsForPersonAsync(int arrangementId, int personId)` | Er personen allerede tilmeldt? |
| `GetTakenCountsAsync(IReadOnlyCollection<int> shiftIds)` | Optagne pladser pr. vagt. |

## Faldgruber
- En plads med `CompanionName = null` er den tilmeldte selv. Navngivne pladser ("+"-flowet) erstatter selv-rækken.
- Der er et unikt index på (arrangement, person), så en person kan kun tilmelde sig én gang.
