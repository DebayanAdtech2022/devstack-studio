using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Providers;

public sealed class MySqlProvider : IDatabaseProvider
{
    public DatabaseProviderMetadata Metadata { get; } = new()
    {
        Engine = DatabaseEngine.MySql,
        DisplayName = "MySQL",
        ImageName = "mysql",
        ImageTag = "8.4.3",
        SupportedArchitectures = [CpuArchitectureKind.X64, CpuArchitectureKind.Arm64],
        DefaultPort = 3306,
        DefaultUsername = "appuser",
        HealthCheckCommand = "mysqladmin ping -h localhost -p$$MYSQL_ROOT_PASSWORD",
        RequiresLicenseAcceptance = false,
        LicenseSummary = "Oracle MySQL is available under the GPL; confirm the image license for your use.",
        EfCoreProviderPackage = "MySql.EntityFrameworkCore",
        EfCoreProviderVersion = "10.0.12",
        InitScriptExtension = ".sql",
        GenerationSupported = true,
        GenerationLimitation = null
    };

    public IReadOnlyList<DataTypeDefinition> Types { get; } =
    [
        TypeRegistry.Type("uuid", "UUID (char 36)", "char(36)", "Guid", "char(36)", identity: true, defaultIdentity: IdentityStrategy.Uuid),
        TypeRegistry.Type("int", "Integer", "int", "int", "int", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),
        TypeRegistry.Type("bigint", "Big integer", "bigint", "long", "bigint", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),
        TypeRegistry.Type("string", "String", "varchar", "string", "varchar", length: true, requiredLength: true, defaultLength: 256),
        TypeRegistry.Type("text", "Text", "longtext", "string", "longtext"),
        TypeRegistry.Type("decimal", "Decimal", "decimal", "decimal", "decimal", precision: true, defaultPrecision: 18, defaultScale: 2),
        TypeRegistry.Type("boolean", "Boolean", "tinyint(1)", "bool", "tinyint(1)"),
        TypeRegistry.Type("date", "Date", "date", "DateOnly", "date"),
        TypeRegistry.Type("timestamp", "Timestamp", "datetime(6)", "DateTime", "datetime(6)"),
        TypeRegistry.Type("json", "JSON", "json", "string", "json"),
        TypeRegistry.Type("binary", "Binary", "longblob", "byte[]", "longblob")
    ];

    public DataTypeDefinition? GetType(string logicalId) =>
        Types.FirstOrDefault(t => string.Equals(t.LogicalId, logicalId, StringComparison.OrdinalIgnoreCase));

    public DataTypeDefinition MapOrWarn(string logicalId, out string? warning)
    {
        warning = null;
        return GetType(logicalId) ?? Types[3];
    }

    public string QuoteIdentifier(string identifier) => "`" + identifier.Replace("`", "``", StringComparison.Ordinal) + "`";

    public string FormatDefault(ColumnDefinition column, DataTypeDefinition type)
    {
        if (string.IsNullOrWhiteSpace(column.DefaultValue))
        {
            return string.Empty;
        }

        return type.LogicalId switch
        {
            "int" or "bigint" or "decimal" or "boolean" => column.DefaultValue,
            _ => "'" + column.DefaultValue.Replace("'", "''", StringComparison.Ordinal) + "'"
        };
    }

    public string BuildConnectionString(DatabaseSettings settings, string host, int port) =>
        $"Server={host};Port={port};Database={settings.DatabaseName};User={settings.Username};Password={settings.Password}";

    public IReadOnlyDictionary<string, string> BuildEnvironmentVariables(DatabaseSettings settings) => new Dictionary<string, string>
    {
        ["MYSQL_DATABASE"] = settings.DatabaseName,
        ["MYSQL_USER"] = settings.Username,
        ["MYSQL_PASSWORD"] = "${DB_PASSWORD}",
        ["MYSQL_ROOT_PASSWORD"] = "${DB_ROOT_PASSWORD}"
    };

    public string BuildHealthcheckTest(DatabaseSettings settings) =>
        "mysqladmin ping -h 127.0.0.1 -uroot -p$$MYSQL_ROOT_PASSWORD --silent";
}
