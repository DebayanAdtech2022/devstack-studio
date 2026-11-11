using System.Text.Json;
using System.Text.Json.Serialization;
using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Serialization;

public static class ProjectConfigurationSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(ProjectConfiguration configuration)
    {
        configuration.UpdatedAtUtc = DateTimeOffset.UtcNow;
        return JsonSerializer.Serialize(configuration, Options);
    }

    public static ProjectConfiguration Deserialize(string json)
    {
        var configuration = JsonSerializer.Deserialize<ProjectConfiguration>(json, Options)
            ?? throw new InvalidOperationException("Project configuration JSON was empty.");

        if (configuration.SchemaVersion < 1)
        {
            configuration.SchemaVersion = 1;
        }

        if (configuration.SchemaVersion > 1)
        {
            throw new InvalidOperationException($"Unsupported configuration schema version {configuration.SchemaVersion}.");
        }

        return configuration;
    }

    public static ProjectConfiguration Clone(ProjectConfiguration configuration)
    {
        return Deserialize(Serialize(configuration));
    }
}
