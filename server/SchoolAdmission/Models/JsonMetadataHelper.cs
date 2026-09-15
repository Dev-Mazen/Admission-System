using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace SchoolAdmission.Models;

public static class JsonMetadataHelper
{
    // .NET 8 requires TypeInfoResolver to be set on JsonSerializerOptions before it is frozen.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    /// <summary>
    /// Safely gets a string property value from a JSON metadata string.
    /// Handles null, empty, or malformed JSON gracefully.
    /// </summary>
    public static string? GetProperty(string? rawJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            var node = JsonNode.Parse(rawJson);
            if (node is JsonObject obj && obj.TryGetPropertyValue(propertyName, out var propNode) && propNode != null)
            {
                return propNode.ToString();
            }
        }
        catch
        {
            // Defensive: invalid JSON returns null without throwing exception
        }

        return null;
    }

    /// <summary>
    /// Safely merges a property key/value into an existing JSON metadata string,
    /// preserving all existing or future JSON properties.
    /// </summary>
    public static string MergeProperty(string? rawJson, string propertyName, object? propertyValue)
    {
        JsonObject jsonObj;

        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            try
            {
                var parsedNode = JsonNode.Parse(rawJson);
                jsonObj = parsedNode as JsonObject ?? new JsonObject();
            }
            catch
            {
                jsonObj = new JsonObject();
            }
        }
        else
        {
            jsonObj = new JsonObject();
        }

        if (propertyValue == null)
        {
            jsonObj.Remove(propertyName);
        }
        else
        {
            jsonObj[propertyName] = JsonValue.Create(propertyValue);
        }

        return jsonObj.ToJsonString(JsonOpts);
    }
}
