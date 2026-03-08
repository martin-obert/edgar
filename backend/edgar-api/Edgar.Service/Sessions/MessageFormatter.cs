using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Edgar.Service.Sessions;

public static class MessageFormatter
{
    
    private static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Always,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(object obj) => JsonSerializer.Serialize(obj, JsonSerializerOptions);
    public static byte[] SerializeToBytes(object obj)
    {
        var json = Serialize(obj);
        return Encoding.UTF8.GetBytes(json);
    }
}