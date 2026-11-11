using DevStackStudio.Core.Models;
using DevStackStudio.Core.Naming;

namespace DevStackStudio.Core.Samples;

public static class SampleProjects
{
    public static ProjectConfiguration CatalogPostgres()
    {
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var categoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var productPk = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var categoryPk = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var productCategoryFk = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var category = new EntityDefinition
        {
            Id = categoryId,
            Name = "Category",
            Description = "Product category",
            Columns =
            [
                new ColumnDefinition { Id = categoryPk, Name = "Id", LogicalType = "uuid", IsPrimaryKey = true, Identity = IdentityStrategy.Uuid, Ordinal = 0 },
                new ColumnDefinition { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"), Name = "Name", LogicalType = "string", Length = 128, IsNullable = false, IsUnique = true, Ordinal = 1 }
            ]
        };
        var product = new EntityDefinition
        {
            Id = productId,
            Name = "Product",
            Description = "Catalog product",
            Columns =
            [
                new ColumnDefinition { Id = productPk, Name = "Id", LogicalType = "uuid", IsPrimaryKey = true, Identity = IdentityStrategy.Uuid, Ordinal = 0 },
                new ColumnDefinition { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd2"), Name = "Name", LogicalType = "string", Length = 256, IsNullable = false, HasIndex = true, Ordinal = 1 },
                new ColumnDefinition { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd3"), Name = "Price", LogicalType = "decimal", Precision = 18, Scale = 2, IsNullable = false, DefaultValue = "0", Ordinal = 2 },
                new ColumnDefinition
                {
                    Id = productCategoryFk,
                    Name = "CategoryId",
                    LogicalType = "uuid",
                    IsNullable = false,
                    ForeignKeyTargetEntityId = categoryId,
                    ForeignKeyTargetColumnId = categoryPk,
                    OnDelete = ForeignKeyDeleteBehavior.Restrict,
                    Ordinal = 3
                }
            ]
        };

        var configuration = new ProjectConfiguration
        {
            ProjectName = "CatalogDemo",
            OutputDirectory = "generated",
            Database = new DatabaseSettings
            {
                Engine = DatabaseEngine.PostgreSql,
                DatabaseName = "catalogdb",
                Username = "appuser",
                Password = "LocalDevPassword1!",
                HostPort = 5432
            },
            Entities = [category, product],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Name = "ProductCategory",
                    Kind = RelationshipKind.OneToMany,
                    FromEntityId = productId,
                    FromColumnId = productCategoryFk,
                    ToEntityId = categoryId,
                    ToColumnId = categoryPk
                }
            ]
        };
        NameFactory.ApplyDefaults(configuration);
        return configuration;
    }
}
