using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Contracts;

public sealed class ProcessExecutionRequest
{
    public required string FileName { get; init; }
    public IReadOnlyList<string> Arguments { get; init; } = [];
    public string? WorkingDirectory { get; init; }
    public TimeSpan? Timeout { get; init; }
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
}

public sealed class ProcessExecutionResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
    public bool TimedOut { get; init; }
    public bool Succeeded => ExitCode == 0 && !TimedOut;
}

public interface IProcessRunner
{
    Task<ProcessExecutionResult> RunAsync(ProcessExecutionRequest request, CancellationToken cancellationToken = default);
}

public interface IEnvironmentDetector
{
    Task<HostEnvironmentInfo> DetectAsync(CancellationToken cancellationToken = default);
}

public interface IDockerProbe
{
    Task<DockerReadiness> ProbeAsync(CancellationToken cancellationToken = default);
}

public interface IDockerOrchestrator
{
    Task<ProcessExecutionResult> ComposeAsync(string projectDirectory, IReadOnlyList<string> composeArgs, CancellationToken cancellationToken = default);
}

public sealed class GeneratedFile
{
    public required string RelativePath { get; init; }
    public required string Contents { get; init; }
    public bool IsSecret { get; init; }
    public bool IsUserOwned { get; init; }
}

public sealed class GenerationPlan
{
    public required ProjectConfiguration Configuration { get; init; }
    public required string TemplateVersion { get; init; }
    public IReadOnlyList<string> Steps { get; init; } = [];
}

public sealed class GenerationResult
{
    public bool Succeeded { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public IReadOnlyList<GeneratedFile> Files { get; init; } = [];
    public IReadOnlyList<string> Conflicts { get; init; } = [];
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
    public string ManifestPath { get; init; } = string.Empty;
    public bool DryRun { get; init; }
}

public interface IProjectGenerator
{
    Task<GenerationResult> GenerateAsync(ProjectConfiguration configuration, CancellationToken cancellationToken = default);
}

public sealed class ManifestFileEntry
{
    public required string Path { get; init; }
    public required string Sha256 { get; init; }
    public bool UserOwned { get; init; }
}

public sealed class ProjectManifest
{
    public string GeneratorVersion { get; init; } = "0.1.0";
    public string TemplateVersion { get; init; } = TemplateVersions.Current;
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string ProjectName { get; init; } = string.Empty;
    public List<ManifestFileEntry> Files { get; init; } = [];
}
