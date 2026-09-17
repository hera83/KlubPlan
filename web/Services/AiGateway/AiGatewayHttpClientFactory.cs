using System.Net;

namespace web.Services.AiGateway;

public class AiGatewayHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AiGatewayHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public HttpClient Create(string baseUrl, string? apiKey, int requestTimeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new AiGatewayException(HttpStatusCode.InternalServerError,
                "AiGateway:BaseUrl er ikke konfigureret. Sæt miljøvariablen AiGateway__BaseUrl (eller " +
                "AiGateway:BaseUrl i appsettings.Production.json) til AiGatewayens URL.");
        }

        var client = _httpClientFactory.CreateClient("AiGateway");
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
