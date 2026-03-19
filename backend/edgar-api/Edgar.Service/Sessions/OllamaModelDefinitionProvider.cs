using System.Text.Json;
using Edgar.Service.Authentication;
using Edgar.Service.Ollama;
using Microsoft.Extensions.Options;

namespace Edgar.Service.Sessions;

public class OllamaModelDefinitionProvider(
    IOptions<OllamaSettings> options,
    ILogger<OllamaModelDefinitionProvider> logger,
    ISessionRepository sessionRepository) : IOllamaModelDefinitionProvider
{
    public async Task<OllamaModelDefinition> GetDefaultModelDefinitionAsync(
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(options.Value.ModelDefinitionFilePath))
        {
            if (!File.Exists(options.Value.ModelDefinitionFilePath))
            {
                logger.LogWarning("Model definition file not found: {FilePath}", options.Value.ModelDefinitionFilePath);
                return OllamaDefinitions.DefaultModel;
            }

            logger.LogInformation("Loading model definition from file: {FilePath}",
                options.Value.ModelDefinitionFilePath);

            await using var fileStream = File.OpenRead(options.Value.ModelDefinitionFilePath);

            var modelOverride =
                await JsonSerializer.DeserializeAsync<OllamaModelDefinition>(fileStream,
                    cancellationToken: cancellationToken);
            logger.LogInformation("Model definition loaded");

            if (modelOverride is not null) return modelOverride;

            logger.LogError("Failed to deserialize model definition");
        }

        return OllamaDefinitions.DefaultModel;
    }

    public async Task<OllamaModelDefinition> GetSessionModelDefinitionAsync(Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session?.ModelConfiguration is null)
        {
            return await GetDefaultModelDefinitionAsync(cancellationToken);
        }

        return session.ModelConfiguration;
    }
}