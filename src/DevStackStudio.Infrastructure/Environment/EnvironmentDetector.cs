using System.Runtime.InteropServices;
using DevStackStudio.Core.Contracts;
using DevStackStudio.Core.Environment;
using DevStackStudio.Core.Models;

namespace DevStackStudio.Infrastructure.Environment;

public sealed class EnvironmentDetector : IEnvironmentDetector
{
    public Task<HostEnvironmentInfo> DetectAsync(CancellationToken cancellationToken = default)
    {
        var os = OperatingSystem.IsWindows()
            ? OSPlatform.Windows
            : OperatingSystem.IsLinux()
                ? OSPlatform.Linux
                : OperatingSystem.IsMacOS()
                    ? OSPlatform.OSX
                    : OSPlatform.Create("unknown");

        var family = RuntimePlatformMapper.MapOs(os);
        var osArch = RuntimePlatformMapper.MapArchitecture(RuntimeInformation.OSArchitecture);
        var processArch = RuntimePlatformMapper.MapArchitecture(RuntimeInformation.ProcessArchitecture);
        long? disk = null;
        try
        {
            var root = Path.GetPathRoot(System.Environment.CurrentDirectory);
            if (!string.IsNullOrEmpty(root))
            {
                var info = new DriveInfo(root);
                disk = info.AvailableFreeSpace;
            }
        }
        catch (Exception)
        {
            disk = null;
        }

        var infoResult = new HostEnvironmentInfo
        {
            OsFamily = family,
            OsDescription = RuntimeInformation.OSDescription,
            OsVersion = System.Environment.OSVersion.VersionString,
            OsArchitecture = osArch,
            ProcessArchitecture = processArch,
            RuntimeIdentifier = RuntimePlatformMapper.ToRid(family, processArch),
            AvailableDiskBytes = disk
        };
        return Task.FromResult(infoResult);
    }
}
