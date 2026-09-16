# KlubPlan

> [!WARNING]
> **Dette projekt er IKKE færdigt.** Det er under aktiv udvikling, og features, datamodel
> og struktur kan ændre sig uden varsel. Projektet er endnu ikke klar til at blive kopieret,
> genbrugt eller taget i brug — undlad venligst at klone/forke det til andre formål på
> nuværende tidspunkt.

## Hvad er det

Et ASP.NET Core MVC-webprojekt (.NET 10) til klubadministration — brugere/roller,
personer/grupper, formularer, beskeder (SMS/mail), årshjul m.m. Se
[CLAUDE.md](CLAUDE.md) og [.github/copilot-instructions.md](.github/copilot-instructions.md)
for arkitektur og konventioner.

## Kør med Docker Compose

1. Kopier miljø-filen og udfyld de værdier du har brug for:

   ```
   cp .env.example .env
   ```

2. Byg og start:

   ```
   docker compose up --build
   ```

3. Åbn [http://localhost:8080](http://localhost:8080).

Ved allerførste opstart bliver du sendt til `/Setup/FirstUser` for at oprette den første
bruger (som automatisk får rollen "Developer").

### Data og persistens

SQLite-databaserne (`app.db`, `logs.db`) og uploadede filer gemmes i de bind-mountede
mapper `./App_dbs` og `./App_files` i projektroden, så data overlever `docker compose down`
og genopbygning af image'et. Disse mapper er allerede git-ignorerede.

### Kendte begrænsninger i denne fase

- Containeren kører kun på almindelig HTTP på port 8080 — TLS/HTTPS er ikke sat op
  (sæt evt. en reverse proxy for dette).
- Ingen af de eksterne integrationer (AI Gateway, Ollama, SMS, Mail) er obligatoriske —
  lad de tilhørende variabler i `.env` stå tomme, hvis de ikke skal bruges.

## Lokal udvikling (uden Docker)

```
dotnet run --project web
```

Se [CLAUDE.md](CLAUDE.md) for flere kommandoer (migrations, build, m.m.).
