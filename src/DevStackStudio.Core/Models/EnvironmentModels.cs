namespace DevStackStudio.Core.Models;

public sealed class HostEnvironmentInfo
{
    public OperatingSystemFamily OsFamily { get; init; }
    public string OsDescription { get; init; } = string.Empty;
    public string OsVersion { get; init; } = string.Empty;
    public CpuArchitectureKind OsArchitecture { get; init; }
    public CpuArchitectureKind ProcessArchitecture { get; init; }
    public string RuntimeIdentifier { get; init; } = string.Empty;
    public long? AvailableDiskBytes { get; init; }
    public bool ArchitecturesMatch => OsArchitecture == ProcessArchitecture;
}

public sealed class DockerReadiness
{
    public DockerReadinessKind Kind { get; init; } = DockerReadinessKind.Unknown;
    public bool CliAvailable { get; init; }
    public bool DaemonRunning { get; init; }
    public bool ComposeV2Available { get; init; }
    public string? CliVersion { get; init; }
    public string? EngineVersion { get; init; }
    public string? ComposeVersion { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? Detail { get; init; }
    public bool IsReady => Kind == DockerReadinessKind.Ready;
}

public sealed class CompatibilityIssue
{
    public ValidationSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class CompatibilityReport
{
    public HostEnvironmentInfo Environment { get; init; } = new();
    public DockerReadiness Docker { get; init; } = new();
    public IReadOnlyList<CompatibilityIssue> Issues { get; init; } = [];
    public bool CanCreateProjects { get; init; }
    public bool CanStartDockerStack { get; init; }
}

public sealed class DataTypeDefinition
{
    public string LogicalId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string ProviderTypeName { get; init; } = string.Empty;
    public string ClrTypeName { get; init; } = string.Empty;
    public string EfStoreType { get; init; } = string.Empty;
    public bool RequiresLength { get; init; }
    public bool SupportsLength { get; init; }
    public bool SupportsPrecision { get; init; }
    public bool SupportsScale { get; init; }
    public bool SupportsIdentity { get; init; }
    public IdentityStrategy DefaultIdentity { get; init; } = IdentityStrategy.None;
    public int? DefaultLength { get; init; }
    public int? DefaultPrecision { get; init; }
    public int? DefaultScale { get; init; }
}

public sealed class DatabaseProviderMetadata
{
    public DatabaseEngine Engine { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string ImageName { get; init; } = string.Empty;
    public string ImageTag { get; init; } = string.Empty;
    public IReadOnlyList<CpuArchitectureKind> SupportedArchitectures { get; init; } = [];
    public int DefaultPort { get; init; }
    public string DefaultUsername { get; init; } = string.Empty;
    public string HealthCheckCommand { get; init; } = string.Empty;
    public bool RequiresLicenseAcceptance { get; init; }
    public string LicenseSummary { get; init; } = string.Empty;
    public string EfCoreProviderPackage { get; init; } = string.Empty;
    public string EfCoreProviderVersion { get; init; } = string.Empty;
    public bool EfCoreSupported { get; init; } = true;
    public string InitScriptExtension { get; init; } = ".sql";
    public bool GenerationSupported { get; init; } = true;
    public string? GenerationLimitation { get; init; }
}

public sealed class ValidationIssue
{
    public string Path { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

public sealed class ValidationResult
{
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
    public bool IsValid => Issues.All(i => i.Severity != ValidationSeverity.Error);

    public static ValidationResult Success() => new();

    public static ValidationResult From(params ValidationIssue[] issues) => new() { Issues = issues };

    public static ValidationResult From(IEnumerable<ValidationIssue> issues) => new() { Issues = issues.ToList() };
}
