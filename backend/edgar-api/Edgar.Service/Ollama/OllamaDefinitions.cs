namespace Edgar.Service.Ollama;

public static class OllamaDefinitions
{
    public static readonly OllamaModelDefinition DefaultModel = new()
    {
        Model = "qwen2.5:7b",
        SystemPrompt = "You are a helpful assistant.",
        Options = new()
        {
            Seed = 0,
            Temperature = 0.7f,
            TopP = 0.8f,
            TopK = 20,
            NumPredict = 8192,
            NumCtx = 32768,
            Stop = null,
            MinP = 0f,
        },
        AllTools =
        [
            new OllamaToolDefinition
            {
                Function = new OllamaFunctionDefinition
                {
                    Name = "list_doors",
                    Definition = "List all the doors in the room.",
                    Parameters = null
                }
            },
            new OllamaToolDefinition
            {
                Function = new OllamaFunctionDefinition
                {
                    Name = "set_door_state",
                    Definition = "Set the state of the door.",
                    Parameters = new OllamaFunctionParametersDefinition
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OllamaFunctionParameterDefinition>
                        {
                            {
                                "id",
                                new OllamaFunctionParameterDefinition
                                    { Type = "string", Definition = "The id of the door." }
                            },
                            {
                                "state",
                                new OllamaFunctionParameterDefinition
                                {
                                    Type = "string",
                                    Enum = ["open", "closed", "jammed", "locked"],
                                    Definition = "The state of the door. Can be open, closed, jammed or locked."
                                }
                            }
                        },
                        Required = ["id", "state"]
                    }
                }
            }
        ]
    };
}