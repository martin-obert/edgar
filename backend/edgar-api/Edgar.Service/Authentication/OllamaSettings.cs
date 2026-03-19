namespace Edgar.Service.Authentication;

public sealed class OllamaSettings
{
    public required string BaseUrl { get; set; }
    public required OllamaAuthenticationSettings Authentication { get; set; }
    public string? ModelDefinitionFilePath { get; init; }
}