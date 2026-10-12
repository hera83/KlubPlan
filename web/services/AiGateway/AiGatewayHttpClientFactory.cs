using System.Net;

namespace web.Services.AiGateway;

public class AiGatewayHttpClientFactory(IHttpClientFactory httpClientFactory)
{
    public HttpClient Create(string baseUrl, string? apiKey, int requestTimeoutSeconds)
    {
        // appsettings.json har "BaseUrl": "" - uden dette tjek ville new Uri(...) kaste en
        // UriFormatException, som ingen controller fanger.
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new AiGatewayException(HttpStatusCode.InternalServerError,
                "AiGateway:BaseUrl er ikke konfigureret. Sæt miljøvariablen AiGateway__BaseUrl (eller " +
                "AiGateway:BaseUrl i appsettings.Production.json) til AiGatewayens URL.");
        }

        var client = httpClientFactory.CreateClient("AiGateway");
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds > 0 ? requestTimeoutSeconds : 300);

        client.DefaultRequestHeaders.Remove("X-Api-Key");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        return client;
    }
}
