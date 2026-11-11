namespace DevStackStudio.Core.Models;

public sealed class ColumnDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Id";
    public string LogicalType { get; set; } = "uuid";
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public IdentityStrategy Identity { get; set; } = IdentityStrategy.None;
    public bool IsUnique { get; set; }
    public bool HasIndex { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public string? CheckConstraint { get; set; }
    public Guid? ForeignKeyTargetEntityId { get; set; }
    public Guid? ForeignKeyTargetColumnId { get; set; }
    public ForeignKeyDeleteBehavior OnDelete { get; set; } = ForeignKeyDeleteBehavior.Restrict;
    public int Ordinal { get; set; }
}
