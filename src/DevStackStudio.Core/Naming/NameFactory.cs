using System.Globalization;
using System.Text;
using DevStackStudio.Core.Models;
using DevStackStudio.Core.Security;

namespace DevStackStudio.Core.Naming;

public static class NameFactory
{
    public static string ToPascalCase(string value)
    {
        var parts = Split(value);
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            builder.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                builder.Append(part[1..].ToLowerInvariant());
            }
        }

        var result = builder.ToString();
        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = "N" + result;
        }

        return result;
    }

    public static string ToCamelCase(string value)
    {
        var pascal = ToPascalCase(value);
        return string.IsNullOrEmpty(pascal) ? pascal : char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    public static string ToKebabCase(string value) => string.Join('-', Split(value));

    public static string ToSnakeCase(string value) => string.Join('_', Split(value));

    public static string Pluralize(string name)
    {
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return name + "es";
        }

        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && name.Length > 1 && !IsVowel(name[^2]))
        {
            return name[..^1] + "ies";
        }

        return name + "s";
    }

    public static string SanitizeContainerName(string projectName)
    {
        var kebab = ToKebabCase(projectName);
        if (string.IsNullOrEmpty(kebab))
        {
            kebab = "devstack-app";
        }

        return kebab.Length > 50 ? kebab[..50] : kebab;
    }

    public static string DefaultNetworkName(string projectName) => SanitizeContainerName(projectName) + "-net";

    public static string DefaultComposeProject(string projectName) => SanitizeContainerName(projectName);

    public static string CSharpNamespace(string projectName) => ToPascalCase(projectName);

    public static void ApplyDefaults(ProjectConfiguration configuration)
    {
        if (!PathSafety.IsSafeProjectName(configuration.ProjectName))
        {
            return;
        }

        var projSlug = SanitizeContainerName(configuration.ProjectName);
        var composeSlug = !string.IsNullOrWhiteSpace(configuration.Docker.ComposeProjectName)
            ? SanitizeContainerName(configuration.Docker.ComposeProjectName)
            : projSlug;

        // Container name aligns with compose/project name
        if (string.IsNullOrWhiteSpace(configuration.Database.ContainerName) ||
            configuration.Database.ContainerName.EndsWith("-db", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Database.ContainerName = composeSlug + "-db";
        }

        var engineSuffix = configuration.Database.Engine switch
        {
            DatabaseEngine.MySql => "-mysql-data",
            DatabaseEngine.SqlServer => "-sqlserver-data",
            DatabaseEngine.Oracle => "-oracle-data",
            _ => "-postgres-data"
        };
        if (string.IsNullOrWhiteSpace(configuration.Database.VolumeName) ||
            configuration.Database.VolumeName.EndsWith("-data", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Database.VolumeName = composeSlug + engineSuffix;
        }

        if (string.IsNullOrWhiteSpace(configuration.Docker.NetworkName) ||
            configuration.Docker.NetworkName.EndsWith("-net", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Docker.NetworkName = composeSlug + "-net";
        }

        if (string.IsNullOrWhiteSpace(configuration.Docker.ComposeProjectName) ||
            configuration.Docker.ComposeProjectName.Equals("catalogdemo", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Docker.ComposeProjectName = projSlug;
        }

        if (string.IsNullOrWhiteSpace(configuration.Database.DatabaseName) ||
            configuration.Database.DatabaseName.Equals("catalogdb", StringComparison.OrdinalIgnoreCase) ||
            configuration.Database.DatabaseName.Equals("appdb", StringComparison.OrdinalIgnoreCase))
        {
            configuration.Database.DatabaseName = projSlug.Replace("-", "", StringComparison.Ordinal) + "db";
        }

        if (string.IsNullOrWhiteSpace(configuration.Database.Password))
        {
            configuration.Database.Password = CredentialGenerator.GeneratePassword();
        }
    }

    private static IEnumerable<string> Split(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (builder.Length > 0)
            {
                yield return builder.ToString();
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static bool IsVowel(char c) => "aeiouAEIOU".Contains(c);
}
