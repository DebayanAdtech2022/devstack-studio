namespace DevStackStudio.Core.Models;

public sealed class EntityDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Entity";
    public string? Description { get; set; }
    public List<ColumnDefinition> Columns { get; set; } = [];
}
