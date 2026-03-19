namespace Edgar.Service.Ollama;

public static class OllamaModelDefinitionExtensions
{
    public static int GetToolIndex(this OllamaToolDefinition[] ollamaToolDefinitions, string functionName)
    {
        for (int i = 0; i < ollamaToolDefinitions.Length; i++)
        {
            if (ollamaToolDefinitions[i].Function.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}