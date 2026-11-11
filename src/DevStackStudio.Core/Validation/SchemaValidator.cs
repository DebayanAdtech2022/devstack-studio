using DevStackStudio.Core.Models;
using DevStackStudio.Core.Providers;

namespace DevStackStudio.Core.Validation;

public sealed class SchemaValidator
{
    public ValidationResult Validate(ProjectConfiguration configuration, HostEnvironmentInfo? environment = null)
    {
        var issues = new List<ValidationIssue>();
        var catalog = DatabaseProviderCatalog.Default;
        var provider = catalog.Get(configuration.Database.Engine);

        // Prune any stale relationships referencing entities that were removed
        configuration.Relationships.RemoveAll(r =>
            !configuration.Entities.Any(e => e.Id == r.FromEntityId) ||
            !configuration.Entities.Any(e => e.Id == r.ToEntityId));

        if (!IdentifierRules.IsValidIdentifier(configuration.Database.DatabaseName) ||
            IdentifierRules.IsReserved(configuration.Database.DatabaseName, configuration.Database.Engine))
        {
            issues.Add(Error("database.databaseName", "Database name is not a valid identifier."));
        }

        if (configuration.Database.HostPort is < 1 or > 65535)
        {
            issues.Add(Error("database.hostPort", "Database port must be between 1 and 65535."));
        }

        if (configuration.Database.HostPort == configuration.Docker.BackendHostPort ||
            configuration.Database.HostPort == configuration.Docker.FrontendHostPort)
        {
            issues.Add(Error("database.hostPort", "Database port conflicts with an application port."));
        }

        if (string.IsNullOrWhiteSpace(configuration.Database.Username))
        {
            issues.Add(Error("database.username", "Database username is required."));
        }

        if (string.IsNullOrWhiteSpace(configuration.Database.Password) || configuration.Database.Password.Length < 12)
        {
            issues.Add(Error("database.password", "Database password must be at least 12 characters."));
        }

        if (provider.Metadata.RequiresLicenseAcceptance && !configuration.Database.AcceptProviderLicense)
        {
            issues.Add(Error("database.acceptProviderLicense", $"{provider.Metadata.DisplayName} requires explicit license acknowledgement. {provider.Metadata.LicenseSummary}"));
        }

        if (environment is not null && !provider.Metadata.SupportedArchitectures.Contains(environment.OsArchitecture) &&
            environment.OsArchitecture != CpuArchitectureKind.Unknown)
        {
            issues.Add(Error("database.engine", $"{provider.Metadata.DisplayName} is not supported on {environment.OsArchitecture}. Choose a compatible database instead of substituting one automatically."));
        }

        if (!provider.Metadata.GenerationSupported)
        {
            issues.Add(Error("database.engine", provider.Metadata.GenerationLimitation ?? "This database engine cannot be generated yet."));
        }

        if (configuration.Entities.Count == 0)
        {
            issues.Add(Error("entities", "Define at least one entity."));
        }

        var entityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in configuration.Entities)
        {
            ValidateEntity(entity, configuration, provider, entityNames, issues);
        }

        foreach (var relationship in configuration.Relationships)
        {
            ValidateRelationship(relationship, configuration, issues);
        }

        return ValidationResult.From(issues);
    }

    private static void ValidateEntity(
        EntityDefinition entity,
        ProjectConfiguration configuration,
        IDatabaseProvider provider,
        HashSet<string> entityNames,
        List<ValidationIssue> issues)
    {
        var path = $"entities.{entity.Name}";
        if (!IdentifierRules.IsValidIdentifier(entity.Name) || IdentifierRules.IsReserved(entity.Name, configuration.Database.Engine))
        {
            issues.Add(Error(path, "Entity name is not a valid identifier or is reserved."));
        }

        if (!entityNames.Add(entity.Name))
        {
            issues.Add(Error(path, "Entity names must be unique."));
        }

        if (entity.Columns.Count == 0)
        {
            issues.Add(Error(path, "Each entity must have at least one column."));
        }

        if (entity.Columns.Count(c => c.IsPrimaryKey) != 1)
        {
            issues.Add(Error(path, "Each entity must have exactly one primary key column."));
        }

        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in entity.Columns)
        {
            ValidateColumn(entity, column, configuration, provider, columnNames, issues);
        }
    }

    private static void ValidateColumn(
        EntityDefinition entity,
        ColumnDefinition column,
        ProjectConfiguration configuration,
        IDatabaseProvider provider,
        HashSet<string> columnNames,
        List<ValidationIssue> issues)
    {
        var path = $"entities.{entity.Name}.{column.Name}";
        if (!IdentifierRules.IsValidIdentifier(column.Name) || IdentifierRules.IsReserved(column.Name, configuration.Database.Engine))
        {
            issues.Add(Error(path, "Column name is not a valid identifier or is reserved."));
        }

        if (!columnNames.Add(column.Name))
        {
            issues.Add(Error(path, "Column names must be unique within an entity."));
        }

        var type = provider.GetType(column.LogicalType);
        if (type is null)
        {
            issues.Add(Error(path + ".logicalType", $"Type '{column.LogicalType}' is not supported by {provider.Metadata.DisplayName}."));
            return;
        }

        if (type.RequiresLength && column.Length is null)
        {
            issues.Add(Error(path + ".length", $"{type.DisplayName} requires a length."));
        }

        if (type.SupportsPrecision && column.Precision is null)
        {
            issues.Add(Error(path + ".precision", $"{type.DisplayName} requires precision."));
        }

        if (column.Identity != IdentityStrategy.None && !type.SupportsIdentity)
        {
            issues.Add(Error(path + ".identity", $"{type.DisplayName} does not support identity generation."));
        }

        if (column.IsPrimaryKey && column.IsNullable)
        {
            issues.Add(Error(path, "Primary key columns cannot be nullable."));
        }

        if (column.IsPrimaryKey && column.Identity == IdentityStrategy.None && type.LogicalId is "uuid" or "int" or "bigint")
        {
            issues.Add(new ValidationIssue
            {
                Path = path + ".identity",
                Message = "Primary keys should usually use an identity or UUID strategy.",
                Severity = ValidationSeverity.Warning
            });
        }

        if (!IdentifierRules.IsSafeDefaultValue(column.DefaultValue, column.LogicalType))
        {
            issues.Add(Error(path + ".defaultValue", "Default value is not a safe literal for this type."));
        }

        if (!IdentifierRules.IsSafeCheckConstraint(column.CheckConstraint))
        {
            issues.Add(Error(path + ".checkConstraint", "Check constraints may only use a simple comparison (column operator value). Raw SQL is not allowed."));
        }

        if (column.ForeignKeyTargetEntityId is not null)
        {
            var targetEntity = configuration.Entities.FirstOrDefault(e => e.Id == column.ForeignKeyTargetEntityId);
            if (targetEntity is null)
            {
                issues.Add(Error(path + ".foreignKey", "Foreign key target entity does not exist."));
            }
            else
            {
                var targetColumn = targetEntity.Columns.FirstOrDefault(c => c.Id == column.ForeignKeyTargetColumnId);
                if (targetColumn is null)
                {
                    issues.Add(Error(path + ".foreignKey", "Foreign key target column does not exist."));
                }
                else if (!string.Equals(targetColumn.LogicalType, column.LogicalType, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(Error(path + ".foreignKey", "Foreign key column types must match."));
                }
            }

            if (column.OnDelete == ForeignKeyDeleteBehavior.SetNull && !column.IsNullable)
            {
                issues.Add(Error(path + ".onDelete", "ON DELETE SET NULL requires a nullable foreign key column."));
            }
        }
    }

    private static void ValidateRelationship(RelationshipDefinition relationship, ProjectConfiguration configuration, List<ValidationIssue> issues)
    {
        var from = configuration.Entities.FirstOrDefault(e => e.Id == relationship.FromEntityId);
        var to = configuration.Entities.FirstOrDefault(e => e.Id == relationship.ToEntityId);
        if (from is null || to is null)
        {
            issues.Add(Error($"relationships.{relationship.Name}", "Relationship entities must exist."));
            return;
        }

        if (from.Columns.All(c => c.Id != relationship.FromColumnId) || to.Columns.All(c => c.Id != relationship.ToColumnId))
        {
            issues.Add(Error($"relationships.{relationship.Name}", "Relationship columns must exist."));
        }

        if (relationship.Kind == RelationshipKind.ManyToMany && string.IsNullOrWhiteSpace(relationship.JoinTableName))
        {
            issues.Add(Error($"relationships.{relationship.Name}", "Many-to-many relationships require a join table name."));
        }

        if (!string.IsNullOrWhiteSpace(relationship.JoinTableName) &&
            (!IdentifierRules.IsValidIdentifier(relationship.JoinTableName) ||
             IdentifierRules.IsReserved(relationship.JoinTableName, configuration.Database.Engine)))
        {
            issues.Add(Error($"relationships.{relationship.Name}.joinTableName", "Join table name is not a valid identifier."));
        }
    }

    private static ValidationIssue Error(string path, string message) => new()
    {
        Path = path,
        Message = message,
        Severity = ValidationSeverity.Error
    };
}
