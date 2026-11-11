using DevStackStudio.Core.Models;
using DevStackStudio.Core.Providers;
using DevStackStudio.Core.Samples;
using DevStackStudio.Core.Serialization;
using DevStackStudio.Core.Validation;

namespace DevStackStudio.Core.Tests;

public class SchemaValidationTests
{
    [Fact]
    public void Sample_catalog_is_valid()
    {
        var result = new ProjectConfigurationValidator().Validate(SampleProjects.CatalogPostgres());
        Assert.True(result.IsValid, string.Join(System.Environment.NewLine, result.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void Duplicate_entity_names_fail()
    {
        var config = SampleProjects.CatalogPostgres();
        config.Entities.Add(new EntityDefinition
        {
            Name = "Product",
            Columns =
            [
                new ColumnDefinition { Name = "Id", LogicalType = "uuid", IsPrimaryKey = true, Identity = IdentityStrategy.Uuid }
            ]
        });
        var result = new SchemaValidator().Validate(config);
        Assert.Contains(result.Issues, i => i.Message.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Missing_foreign_key_target_fails()
    {
        var config = SampleProjects.CatalogPostgres();
        config.Entities[1].Columns[3].ForeignKeyTargetEntityId = Guid.NewGuid();
        var result = new SchemaValidator().Validate(config);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetNull_requires_nullable_fk()
    {
        var config = SampleProjects.CatalogPostgres();
        var fk = config.Entities[1].Columns.Single(c => c.Name == "CategoryId");
        fk.OnDelete = ForeignKeyDeleteBehavior.SetNull;
        fk.IsNullable = false;
        var result = new SchemaValidator().Validate(config);
        Assert.Contains(result.Issues, i => i.Path.Contains("onDelete", StringComparison.Ordinal));
    }

    [Fact]
    public void React_vite_is_not_complete()
    {
        var config = SampleProjects.CatalogPostgres();
        config.Frontend = FrontendFramework.ReactVite;
        var result = new ProjectConfigurationValidator().Validate(config);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Round_trips_json()
    {
        var json = ProjectConfigurationSerializer.Serialize(SampleProjects.CatalogPostgres());
        var copy = ProjectConfigurationSerializer.Deserialize(json);
        Assert.Equal("CatalogDemo", copy.ProjectName);
        Assert.Equal(2, copy.Entities.Count);
    }

    [Fact]
    public void Future_schema_version_is_rejected()
    {
        var json = """{ "schemaVersion": 99, "projectName": "X" }""";
        Assert.Throws<InvalidOperationException>(() => ProjectConfigurationSerializer.Deserialize(json));
    }
}

public class ProviderMappingTests
{
    [Theory]
    [InlineData(DatabaseEngine.PostgreSql)]
    [InlineData(DatabaseEngine.MySql)]
    [InlineData(DatabaseEngine.SqlServer)]
    [InlineData(DatabaseEngine.Oracle)]
    public void Every_provider_has_core_types(DatabaseEngine engine)
    {
        var provider = DatabaseProviderCatalog.Default.Get(engine);
        foreach (var type in new[] { "uuid", "int", "string", "decimal", "boolean", "timestamp", "json" })
        {
            Assert.NotNull(provider.GetType(type));
        }

        Assert.DoesNotContain("latest", provider.Metadata.ImageTag, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(provider.BuildConnectionString(new DatabaseSettings { DatabaseName = "db", Username = "u", Password = "p" }, "db", provider.Metadata.DefaultPort)));
    }

    [Fact]
    public void SqlServer_and_oracle_require_license_and_x64()
    {
        var sql = DatabaseProviderCatalog.Default.Get(DatabaseEngine.SqlServer);
        var oracle = DatabaseProviderCatalog.Default.Get(DatabaseEngine.Oracle);
        Assert.True(sql.Metadata.RequiresLicenseAcceptance);
        Assert.True(oracle.Metadata.RequiresLicenseAcceptance);
        Assert.Equal([CpuArchitectureKind.X64], sql.Metadata.SupportedArchitectures);
        Assert.Equal([CpuArchitectureKind.X64], oracle.Metadata.SupportedArchitectures);
    }

    [Fact]
    public void Oracle_is_not_silently_substituted()
    {
        var config = SampleProjects.CatalogPostgres();
        config.Database.Engine = DatabaseEngine.Oracle;
        config.Database.AcceptProviderLicense = true;
        config.Database.HostPort = 1521;
        var env = new HostEnvironmentInfo
        {
            OsFamily = OperatingSystemFamily.Linux,
            OsArchitecture = CpuArchitectureKind.Arm64,
            ProcessArchitecture = CpuArchitectureKind.Arm64,
            RuntimeIdentifier = "linux-arm64"
        };
        var result = new SchemaValidator().Validate(config, env);
        Assert.Contains(result.Issues, i => i.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(DatabaseEngine.Oracle, config.Database.Engine);
    }
}
