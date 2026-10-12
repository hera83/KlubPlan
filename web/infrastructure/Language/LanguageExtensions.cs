using System.Net;
using System.Text.RegularExpressions;
using web.Services.AiGateway;
using web.Services.AiGateway.Dtos.Ollama;
using web.Services.AiGateway.Interfaces;

namespace web.Infrastructure.Language;

// Indgangspunkt: saml AiGateway-klienten og dens konfiguration i ét LanguageTools-objekt,
// typisk én gang i en controllers konstruktør, hvor begge allerede er injiceret via DI:
//
//   _language = aiGatewayService.Language(aiGatewayConfigurationProvider);
//
// Alle sprogfunktioner kaldes derefter direkte på _language uden at skulle sende
// configurationProvider med hver gang - se LanguageTools.cs.
public static class LanguageExtensions
{
    public static LanguageTools Language(this IAiGatewayService aiGateway, IAiGatewayConfigurationProvider configurationProvider)
        => new(aiGateway, configurationProvider);
}
