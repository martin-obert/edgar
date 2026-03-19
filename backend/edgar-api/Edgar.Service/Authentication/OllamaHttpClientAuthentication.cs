using System.Net.Http.Headers;

namespace Edgar.Service.Authentication;

public class OllamaAuthenticationSettings
{
    public string? CfAccessClientId { get; init; }
    public string? CfAccessClientSecret { get; init; }

    
    public string? BearerToken { get; init; }
    
    public void EnrichHeaders(HttpRequestHeaders defaultRequestHeaders)
    {
        if (!string.IsNullOrEmpty(CfAccessClientId))
            defaultRequestHeaders.Add("CF-Access-Client-ID", CfAccessClientId);
        if (!string.IsNullOrEmpty(CfAccessClientSecret))
            defaultRequestHeaders.Add("CF-Access-Client-Secret", CfAccessClientSecret);
        if (!string.IsNullOrEmpty(BearerToken))
            defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
    }
}