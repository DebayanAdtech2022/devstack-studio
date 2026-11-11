namespace DevStackStudio.Core.Models;

public sealed class DatabaseSettings
{
    public DatabaseEngine Engine { get; set; } = DatabaseEngine.PostgreSql;
    public string DatabaseName { get; set; } = "appdb";
    public string ContainerName { get; set; } = string.Empty;
    public string Username { get; set; } = "appuser";
    public string Password { get; set; } = string.Empty;
    public int HostPort { get; set; } = 5432;
    public PortBindingMode PortBinding { get; set; } = PortBindingMode.Localhost;
    public string VolumeName { get; set; } = string.Empty;
    public string ImageTag { get; set; } = string.Empty;
    public string? OracleEdition { get; set; }
    public bool AcceptProviderLicense { get; set; }
    public int? MemoryLimitMegabytes { get; set; }
    public string RestartPolicy { get; set; } = "unless-stopped";
}
