using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Providers;

public sealed class PostgresProvider : IDatabaseProvider
{
    public DatabaseProviderMetadata Metadata { get; } = new()
    {
        Engine = DatabaseEngine.PostgreSql,
        DisplayName = "PostgreSQL",
        ImageName = "postgres",
        ImageTag = "16.6",
        SupportedArchitectures = [CpuArchitectureKind.X64, CpuArchitectureKind.Arm64],
        DefaultPort = 5432,
        DefaultUsername = "appuser",
        HealthCheckCommand = "pg_isready -U $$POSTGRES_USER -d $$POSTGRES_DB",
        RequiresLicenseAcceptance = false,
        LicenseSummary = "PostgreSQL is available under the PostgreSQL License.",
        EfCoreProviderPackage = "Npgsql.EntityFrameworkCore.PostgreSQL",
        EfCoreProviderVersion = "10.0.0",
        InitScriptExtension = ".sql",
        GenerationSupported = true
    };

    public IReadOnlyList<DataTypeDefinition> Types { get; } =
    [
        TypeRegistry.Type("uuid", "UUID", "uuid", "Guid", "uuid", identity: true, defaultIdentity: IdentityStrategy.Uuid),
        TypeRegistry.Type("int", "Integer", "integer", "int", "integer", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),
        TypeRegistry.Type("bigint", "Big integer", "bigint", "long", "bigint", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),
        TypeRegistry.Type("string", "String", "varchar", "string", "varchar", length: true, requiredLength: true, defaultLength: 256),
        TypeRegistry.Type("text", "Text", "text", "string", "text"),
        TypeRegistry.Type("decimal", "Decimal", "numeric", "decimal", "numeric", precision: true, defaultPrecision: 18, defaultScale: 2),
        TypeRegistry.Type("boolean", "Boolean", "boolean", "bool", "boolean"),
        TypeRegistry.Type("date", "Date", "date", "DateOnly", "date"),
        TypeRegistry.Type("timestamp", "Timestamp", "timestamptz", "DateTimeOffset", "timestamp with time zone"),
        TypeRegistry.Type("json", "JSON", "jsonb", "string", "jsonb"),
        TypeRegistry.Type("binary", "Binary", "bytea", "byte[]", "bytea")
    ];

    public DataTypeDefinition? GetType(string logicalId) =>
        Types.FirstOrDefault(t => string.Equals(t.LogicalId, logicalId, StringComparison.OrdinalIgnoreCase));

    public DataTypeDefinition MapOrWarn(string logicalId, out string? warning)
    {
        warning = null;
        return GetType(logicalId) ?? Types[3];
    }

    public string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    public string FormatDefault(ColumnDefinition column, DataTypeDefinition type)
    {
        if (string.IsNullOrWhiteSpace(column.DefaultValue))
        {
            return string.Empty;
        }

        return type.LogicalId switch
        {
            "int" or "bigint" or "decimal" or "boolean" => column.DefaultValue,
            "uuid" when Guid.TryParse(column.DefaultValue, out var guid) => "'" + guid + "'",
            _ => "'" + column.DefaultValue.Replace("'", "''", StringComparison.Ordinal) + "'"
        };
    }

    public string BuildConnectionString(DatabaseSettings settings, string host, int port) =>
        $"Host={host};Port={port};Database={settings.DatabaseName};Username={settings.Username};Password={settings.Password}";

    public IReadOnlyDictionary<string, string> BuildEnvironmentVariables(DatabaseSettings settings) => new Dictionary<string, string>
    {
        ["POSTGRES_DB"] = settings.DatabaseName,
        ["POSTGRES_USER"] = settings.Username,
        ["POSTGRES_PASSWORD"] = "${DB_PASSWORD}"
    };

    public string BuildHealthcheckTest(DatabaseSettings settings) =>
        $"pg_isready -U {settings.Username} -d {settings.DatabaseName}";
}
