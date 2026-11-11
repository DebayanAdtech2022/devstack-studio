using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Environment;

public sealed class DockerInstallOption
{
    public required string Title { get; init; }
    public required string Instructions { get; init; }
    public bool AutomatedInstallAvailable { get; init; }
    public string? OfficialDownloadUrl { get; init; }
}

public static class DockerInstallGuidance
{
    public static DockerInstallOption For(OperatingSystemFamily os) => os switch
    {
        OperatingSystemFamily.Windows => new DockerInstallOption
        {
            Title = "Docker Desktop for Windows",
            OfficialDownloadUrl = "https://docs.docker.com/desktop/setup/install/windows-install/",
            AutomatedInstallAvailable = false,
            Instructions =
                "1. Review Docker Desktop licensing for your organization.\n" +
                "2. Download Docker Desktop from Docker's official Windows install documentation.\n" +
                "3. Enable WSL 2 if prompted.\n" +
                "4. Start Docker Desktop and wait until the engine is running.\n" +
                "5. Recheck from DevStack Studio. The application will not collect an administrator password; use Windows UAC if elevation is required."
        },
        OperatingSystemFamily.MacOs => new DockerInstallOption
        {
            Title = "Docker Desktop for Mac",
            OfficialDownloadUrl = "https://docs.docker.com/desktop/setup/install/mac-install/",
            AutomatedInstallAvailable = false,
            Instructions =
                "1. Review Docker Desktop licensing for your organization.\n" +
                "2. Download the installer that matches Apple Silicon or Intel from Docker's official Mac install documentation.\n" +
                "3. Complete installation and start Docker Desktop.\n" +
                "4. Recheck from DevStack Studio. Browser and installer architecture detection can disagree on Apple Silicon; prefer the manual choice that matches your Mac."
        },
        OperatingSystemFamily.Linux => new DockerInstallOption
        {
            Title = "Docker Engine on Linux",
            OfficialDownloadUrl = "https://docs.docker.com/engine/install/",
            AutomatedInstallAvailable = false,
            Instructions =
                "1. Install Docker Engine using your distribution's official Docker documentation. Do not paste untrusted curl | sh scripts.\n" +
                "2. Enable and start the docker service.\n" +
                "3. Add your user to the docker group, then log out and back in. DevStack Studio will not silently change security settings.\n" +
                "4. Recheck CLI, daemon, and Compose v2 from the wizard."
        },
        _ => new DockerInstallOption
        {
            Title = "Unsupported platform",
            AutomatedInstallAvailable = false,
            Instructions = "Install Docker using vendor documentation for this operating system, then recheck."
        }
    };
}
