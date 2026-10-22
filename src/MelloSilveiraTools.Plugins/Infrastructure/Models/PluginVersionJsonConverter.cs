using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// JSON converter for <see cref="PluginVersion"/> supporting both string (e.g. <c>"v1.0.0"</c>)
/// and structured JSON object serialization and deserialization.
/// </summary>
public sealed class PluginVersionJsonConverter : JsonConverter<PluginVersion>
{
    /// <inheritdoc/>
    public override PluginVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? versionString = reader.GetString();
            return string.IsNullOrEmpty(versionString) ? PluginVersion.Default : PluginVersion.Parse(versionString);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.TryGetProperty("Name", out JsonElement nameProp) && nameProp.GetString() is string nameStr)
                return PluginVersion.Parse(nameStr);

            int major = doc.RootElement.TryGetProperty("Major", out JsonElement maj) ? maj.GetInt32() : 0;
            int minor = doc.RootElement.TryGetProperty("Minor", out JsonElement min) ? min.GetInt32() : 0;
            int patch = doc.RootElement.TryGetProperty("Patch", out JsonElement pat) ? pat.GetInt32() : 0;
            return new PluginVersion(major, minor, patch);
        }

        return PluginVersion.Default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, PluginVersion value, JsonSerializerOptions options) => writer.WriteStringValue(value.Name);
}
