using System.Text.Json;
using System.Text.Json.Serialization;

namespace Example.Api.Tests.Component.Shared;

public static class Json
{
    public static bool IsValid(string value) => TryParse(value, out _);

    public static T? Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, SerializerOptions);

    private static JsonSerializerOptions SerializerOptions => new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static bool TryParse(string json, out JsonDocument? jsonDocument)
    {
        try
        {
            jsonDocument = JsonDocument.Parse(json);
        }
        catch (Exception)
        {
            jsonDocument = null;
            return false;
        }

        return true;
    }
}