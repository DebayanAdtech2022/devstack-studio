namespace DevStackStudio.Core.Models;

public sealed class DockerSettings
{
    public string NetworkName { get; set; } = string.Empty;
    public string ComposeProjectName { get; set; } = string.Empty;
    public int BackendHostPort { get; set; } = 8080;
    public int FrontendHostPort { get; set; } = 3000;
    public bool MountSourceForDevelopment { get; set; } = true;
    public bool EnableProductionProfile { get; set; } = true;
}
