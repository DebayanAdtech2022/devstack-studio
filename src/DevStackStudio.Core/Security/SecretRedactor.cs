using System.Text.RegularExpressions;
using DevStackStudio.Core.Models;
using DevStackStudio.Core.Serialization;

namespace DevStackStudio.Core.Security;

public static partial class SecretRedactor
{
    public static string Redact(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = PasswordAssignment().Replace(value, "$1=***");
        redacted = ConnectionStringPassword().Replace(redacted, "$1=***");
        return redacted;
    }

    public static ProjectConfiguration WithoutSecrets(ProjectConfiguration configuration)
    {
        var copy = ProjectConfigurationSerializer.Clone(configuration);
        if (!string.IsNullOrEmpty(copy.Database.Password))
        {
            copy.Database.Password = "***";
        }

        return copy;
    }

    [GeneratedRegex(@"(?i)(password|pwd|secret|token)\s*=\s*([^\s;,]+)")]
    private static partial Regex PasswordAssignment();

    [GeneratedRegex(@"(?i)(Password|Pwd)\s*=\s*([^;]+)")]
    private static partial Regex ConnectionStringPassword();
}
