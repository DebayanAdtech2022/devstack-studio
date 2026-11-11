namespace DevStackStudio.Core.Models;

public sealed class RelationshipDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public RelationshipKind Kind { get; set; } = RelationshipKind.OneToMany;
    public Guid FromEntityId { get; set; }
    public Guid FromColumnId { get; set; }
    public Guid ToEntityId { get; set; }
    public Guid ToColumnId { get; set; }
    public ForeignKeyDeleteBehavior OnDelete { get; set; } = ForeignKeyDeleteBehavior.Restrict;
    public string? JoinTableName { get; set; }
}
