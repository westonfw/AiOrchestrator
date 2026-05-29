using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Nodes;

namespace AiOrchestrator.Application;

public static class JsonSupport
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);

    public static JsonObject ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }

    public static JsonNode? ParseNode(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json);
    }

    public static string ToJson(JsonNode? node) => node?.ToJsonString(SerializerOptions) ?? "{}";

    public static JsonNode? CloneNode(JsonNode? node) => node is null ? null : JsonNode.Parse(node.ToJsonString());

    public static JsonObject CloneObject(JsonObject node) => JsonNode.Parse(node.ToJsonString())!.AsObject();
}

public sealed class BasicJsonSchemaValidator : IJsonSchemaValidator
{
    public SchemaValidationResult Validate(JsonNode? schema, JsonNode? payload)
    {
        if (schema is null)
        {
            return new SchemaValidationResult(true, null);
        }

        if (payload is not JsonObject payloadObject)
        {
            return new SchemaValidationResult(false, "Payload is not a JSON object.");
        }

        var schemaObject = schema.AsObject();
        if (schemaObject.TryGetPropertyValue("required", out var requiredNode) && requiredNode is JsonArray required)
        {
            foreach (var item in required)
            {
                var property = item?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(property) && !payloadObject.ContainsKey(property))
                {
                    return new SchemaValidationResult(false, $"Required property '{property}' is missing.");
                }
            }
        }

        if (!schemaObject.TryGetPropertyValue("properties", out var propertiesNode) || propertiesNode is not JsonObject properties)
        {
            return new SchemaValidationResult(true, null);
        }

        foreach (var (name, propertySchema) in properties)
        {
            if (!payloadObject.TryGetPropertyValue(name, out var value) || value is null || propertySchema is not JsonObject propertySchemaObject)
            {
                continue;
            }

            if (!propertySchemaObject.TryGetPropertyValue("type", out var typeNode))
            {
                continue;
            }

            var expectedType = typeNode?.GetValue<string>();
            if (!MatchesType(value, expectedType))
            {
                return new SchemaValidationResult(false, $"Property '{name}' should be '{expectedType}'.");
            }
        }

        return new SchemaValidationResult(true, null);
    }

    private static bool MatchesType(JsonNode value, string? expectedType)
    {
        return expectedType switch
        {
            "object" => value is JsonObject,
            "array" => value is JsonArray,
            "string" => value is JsonValue && value.GetValueKind() == JsonValueKind.String,
            "number" => value is JsonValue && (value.GetValueKind() == JsonValueKind.Number),
            "integer" => value is JsonValue && value.GetValueKind() == JsonValueKind.Number,
            "boolean" => value is JsonValue && (value.GetValueKind() is JsonValueKind.True or JsonValueKind.False),
            _ => true
        };
    }
}
