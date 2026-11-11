using DevStackStudio.Core.Models;
using DevStackStudio.Core.Providers;

namespace DevStackStudio.Core.Environment;

public sealed class CompatibilityChecker
{
    public CompatibilityReport Evaluate(HostEnvironmentInfo environment, DockerReadiness docker, DatabaseEngine? selectedEngine = null)
    {
        var issues = new List<CompatibilityIssue>();
        var platformIssue = RuntimePlatformMapper.UnsupportedPlatformIssue(environment.RuntimeIdentifier);
        if (platformIssue is not null)
        {
            issues.Add(platformIssue);
        }

        if (!environment.ArchitecturesMatch)
        {
            issues.Add(new CompatibilityIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = "arch-mismatch",
                Message = $"OS architecture ({environment.OsArchitecture}) differs from process architecture ({environment.ProcessArchitecture}). Use the installer that matches the process RID {environment.RuntimeIdentifier}."
            });
        }

        if (environment.AvailableDiskBytes is < 2L * 1024 * 1024 * 1024)
        {
            issues.Add(new CompatibilityIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = "low-disk",
                Message = "Less than 2 GB of free disk space was detected. Docker image pulls may fail."
            });
        }

        if (!docker.IsReady)
        {
            issues.Add(new CompatibilityIssue
            {
                Severity = docker.Kind == DockerReadinessKind.CliMissing ? ValidationSeverity.Warning : ValidationSeverity.Warning,
                Code = "docker-" + docker.Kind.ToString().ToLowerInvariant(),
                Message = docker.Summary
            });
        }

        if (selectedEngine is not null)
        {
            var provider = DatabaseProviderCatalog.Default.Get(selectedEngine.Value);
            if (!provider.Metadata.SupportedArchitectures.Contains(environment.OsArchitecture) &&
                environment.OsArchitecture != CpuArchitectureKind.Unknown)
            {
                issues.Add(new CompatibilityIssue
                {
                    Severity = ValidationSeverity.Error,
                    Code = "db-arch",
                    Message = $"{provider.Metadata.DisplayName} images are not listed as supported on {environment.OsArchitecture}."
                });
            }
        }

        var platformOk = platformIssue is null;
        return new CompatibilityReport
        {
            Environment = environment,
            Docker = docker,
            Issues = issues,
            CanCreateProjects = platformOk,
            CanStartDockerStack = platformOk && docker.IsReady
        };
    }
}
