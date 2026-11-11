using System.Runtime.InteropServices;
using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Environment;

public static class RuntimePlatformMapper
{
    public static readonly IReadOnlyList<string> SupportedRids =
    [
        "win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"
    ];

    public static OperatingSystemFamily MapOs(OSPlatform platform)
    {
        if (platform == OSPlatform.Windows) return OperatingSystemFamily.Windows;
        if (platform == OSPlatform.Linux) return OperatingSystemFamily.Linux;
        if (platform == OSPlatform.OSX) return OperatingSystemFamily.MacOs;
        return OperatingSystemFamily.Unknown;
    }

    public static CpuArchitectureKind MapArchitecture(Architecture architecture) => architecture switch
    {
        Architecture.X64 => CpuArchitectureKind.X64,
        Architecture.Arm64 => CpuArchitectureKind.Arm64,
        Architecture.X86 => CpuArchitectureKind.X86,
        Architecture.Arm => CpuArchitectureKind.Arm,
        _ => CpuArchitectureKind.Unknown
    };

    public static string ToRid(OperatingSystemFamily os, CpuArchitectureKind processArchitecture)
    {
        var osPart = os switch
        {
            OperatingSystemFamily.Windows => "win",
            OperatingSystemFamily.Linux => "linux",
            OperatingSystemFamily.MacOs => "osx",
            _ => "unknown"
        };

        var archPart = processArchitecture switch
        {
            CpuArchitectureKind.X64 => "x64",
            CpuArchitectureKind.Arm64 => "arm64",
            CpuArchitectureKind.X86 => "x86",
            CpuArchitectureKind.Arm => "arm",
            _ => "unknown"
        };

        return $"{osPart}-{archPart}";
    }

    public static bool IsSupportedRid(string rid) => SupportedRids.Contains(rid, StringComparer.OrdinalIgnoreCase);

    public static CompatibilityIssue? UnsupportedPlatformIssue(string rid)
    {
        if (IsSupportedRid(rid))
        {
            return null;
        }

        return new CompatibilityIssue
        {
            Severity = ValidationSeverity.Error,
            Code = "unsupported-platform",
            Message = $"Runtime identifier '{rid}' is not a supported DevStack Studio build target. Supported targets: {string.Join(", ", SupportedRids)}."
        };
    }
}
