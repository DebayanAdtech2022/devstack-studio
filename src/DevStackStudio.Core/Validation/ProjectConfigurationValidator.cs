using DevStackStudio.Core.Models;
using DevStackStudio.Core.Providers;
using DevStackStudio.Core.Security;

namespace DevStackStudio.Core.Validation;

public sealed class ProjectConfigurationValidator
{
    public ValidationResult Validate(ProjectConfiguration configuration, HostEnvironmentInfo? environment = null)
    {
        var issues = new List<ValidationIssue>();
        if (!PathSafety.IsSafeProjectName(configuration.ProjectName))
        {
            issues.Add(Error("projectName", "Project name must be 1-64 characters and contain only letters, digits, hyphen, or underscore."));
        }

        if (string.IsNullOrWhiteSpace(configuration.OutputDirectory))
        {
            issues.Add(Error("outputDirectory", "Output directory is required."));
        }
        else if (configuration.OutputDirectory.Contains("..", StringComparison.Ordinal))
        {
            issues.Add(Error("outputDirectory", "Output directory must not contain path traversal segments."));
        }

        if (configuration.Frontend == FrontendFramework.ReactVite)
        {
            issues.Add(new ValidationIssue
            {
                Path = "frontend",
                Message = "React with Vite is registered but not yet a complete generator. Select Next.js for a working frontend.",
                Severity = ValidationSeverity.Error
            });
        }

        if (configuration.Docker.BackendHostPort is < 1 or > 65535)
        {
            issues.Add(Error("docker.backendHostPort", "Backend host port must be between 1 and 65535."));
        }

        if (configuration.Docker.FrontendHostPort is < 1 or > 65535)
        {
            issues.Add(Error("docker.frontendHostPort", "Frontend host port must be between 1 and 65535."));
        }

        if (configuration.Docker.BackendHostPort == configuration.Docker.FrontendHostPort)
        {
            issues.Add(Error("docker.backendHostPort", "Backend and frontend host ports must be different."));
        }

        issues.AddRange(new SchemaValidator().Validate(configuration, environment).Issues);
        return ValidationResult.From(issues);
    }

    private static ValidationIssue Error(string path, string message) => new()
    {
        Path = path,
        Message = message,
        Severity = ValidationSeverity.Error
    };
}
