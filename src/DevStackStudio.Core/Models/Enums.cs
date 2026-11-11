namespace DevStackStudio.Core.Models;

public enum DatabaseEngine
{
    PostgreSql = 0,
    MySql = 1,
    SqlServer = 2,
    Oracle = 3
}

public enum BackendFramework
{
    AspNetCoreCqrs = 0
}

public enum FrontendFramework
{
    NextJs = 0,
    ReactVite = 1
}

public enum FrontendMaturity
{
    Complete = 0,
    Experimental = 1,
    Planned = 2
}

public enum RelationshipKind
{
    OneToOne = 0,
    OneToMany = 1,
    ManyToMany = 2
}

public enum ForeignKeyDeleteBehavior
{
    Restrict = 0,
    Cascade = 1,
    SetNull = 2,
    NoAction = 3
}

public enum IdentityStrategy
{
    None = 0,
    DatabaseIdentity = 1,
    Uuid = 2
}

public enum DockerReadinessKind
{
    Ready = 0,
    CliMissing = 1,
    DaemonStopped = 2,
    PermissionDenied = 3,
    VersionIncompatible = 4,
    ComposeMissing = 5,
    Unknown = 6
}

public enum OperatingSystemFamily
{
    Windows = 0,
    Linux = 1,
    MacOs = 2,
    Unknown = 3
}

public enum CpuArchitectureKind
{
    X64 = 0,
    Arm64 = 1,
    X86 = 2,
    Arm = 3,
    Unknown = 4
}

public enum ValidationSeverity
{
    Error = 0,
    Warning = 1,
    Info = 2
}

public enum PortBindingMode
{
    Localhost = 0,
    AllInterfaces = 1
}
