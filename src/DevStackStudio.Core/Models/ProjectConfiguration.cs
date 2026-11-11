namespace DevStackStudio.Core.Models;

public sealed class ProjectConfiguration
{
    public int SchemaVersion { get; set; } = 1;
    public string ProjectName { get; set; } = "DevStackApp";
    public string OutputDirectory { get; set; } = string.Empty;
    public BackendFramework Backend { get; set; } = BackendFramework.AspNetCoreCqrs;
    public FrontendFramework Frontend { get; set; } = FrontendFramework.NextJs;
    public DatabaseSettings Database { get; set; } = new();
    public DockerSettings Docker { get; set; } = new();
    public List<EntityDefinition> Entities { get; set; } = [];
    public List<RelationshipDefinition> Relationships { get; set; } = [];
    public GenerationOptions Options { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
