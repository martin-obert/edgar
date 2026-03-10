using System.Net.Http.Headers;

namespace Edgar.Service.Authentication;

public class OllamaAuthenticationSettings
{
    public string? CfAccessClientId { get; init; }
    public string? CfAccessClientSecret { get; init; }

    public void EnrichHeaders(HttpRequestHeaders defaultRequestHeaders)
    {
        if (!string.IsNullOrEmpty(CfAccessClientId))
            defaultRequestHeaders.Add("CF-Access-Client-ID", CfAccessClientId);
        if (!string.IsNullOrEmpty(CfAccessClientSecret))
            defaultRequestHeaders.Add("CF-Access-Client-Secret", CfAccessClientSecret);
    }
}

public sealed class OllamaSettings
{
    public required string BaseUrl { get; set; }
    public required OllamaAuthenticationSettings Authentication { get; set; }
}