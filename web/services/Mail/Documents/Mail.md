# Mail (service)

**Formål:** Sender mail via SMTP og læser/sletter mail via IMAP (MailKit).
**Brug det når:** opgaven skal sende en mail (fx Kommunikation) eller vise/rydde op i postkassen (Indstillinger → Mail).
**Brug det ikke til:** at gemme mails i appens database. Den udgående kø (`CommunicationEmailMessages`) håndteres af `CommunicationEmailWorker`.
**Eksternt system og konfiguration:** en SMTP- og en IMAP-server. Sektionerne `Mail:Smtp` (`Host`, `Port`, `SecureSocketOptions`, `Username`, `Password`, `FromAddress`, `FromName`) og `Mail:Imap` (`Host`, `Port`, `SecureSocketOptions`, `Username`, `Password`) i `appsettings.json`. Passwords er secrets. Light pattern uden `HttpClient`: konfigurationen læses ved hvert kald.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SendMailAsync(SendMailRequestDto request)` | Sender en mail (tekst og/eller HTML, med vedhæftninger) via SMTP. |
| `ReadMailAsync(ReadMailRequestDto? request)` | Lister mails i en IMAP-mappe (standard `INBOX`). |
| `GetMailAsync(uint uid, string folder, bool markAsSeen)` | Henter én mail inkl. indhold og vedhæftninger, eller `null`, hvis den ikke findes. |
| `GetFolderStatusAsync(string folder)` | Antal mails og ulæste i en mappe. |
| `DeleteMailAsync(uint uid, string folder)` | Sletter én mail. `false`, hvis den ikke findes. |

## Faldgruber
- Mangler `Mail:Smtp:Host`/`FromAddress` eller `Mail:Imap:Host`, kastes `InvalidOperationException` ved kaldet. `CommunicationEmailWorker` registreres kun, når SMTP er konfigureret.
- `SecureSocketOptions` parses som MailKits enum (`None`, `Auto`, `SslOnConnect`, `StartTls`, `StartTlsWhenAvailable`).
