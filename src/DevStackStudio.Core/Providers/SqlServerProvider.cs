using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Providers;

public sealed class SqlServerProvider : IDatabaseProvider
{
    public DatabaseProviderMetadata Metadata { get; } = new()
    {
        Engine = DatabaseEngine.SqlServer,
        DisplayName = "Microsoft SQL Server",
        ImageName = "mcr.microsoft.com/mssql/server",
        ImageTag = "2022-CU16-ubuntu-22.04",
        SupportedArchitectures = [CpuArchitectureKind.X64],
        DefaultPort = 1433,
        DefaultUsername = "sa",
        HealthCheckCommand = "/opt/mssql-tools18/bin/sqlcmd -C -Q \"SELECT 1\" -b -o /dev/null",
        RequiresLicenseAcceptance = true,
        LicenseSummary = "SQL Server container images require accepting the Microsoft EULA (ACCEPT_EULA=Y). Confirm licensing for your organization. Official linux/amd64 images are used; linux/arm64 is not treated as supported.",
        EfCoreProviderPackage = "Microsoft.EntityFrameworkCore.SqlServer",
        EfCoreProviderVersion = "10.0.0",
        InitScriptExtension = ".sql",
        GenerationSupported = true
    };

    public IReadOnlyList<DataTypeDefinition> Types { get; } =
    [
        TypeRegistry.Type("uuid", "UNIQUEIDENTIFIER", "uniqueidentifier", "Guid", "uniqueidentifier", identity: true, defaultIdentity: IdentityStrategy.Uuid),
        TypeRegistry.Type("int", "INT", "int", "int", "int", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),
        TypeRegistry.Type("bigint", "BIGINT", "bigint", "long", "bigint", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),
        TypeRegistry.Type("string", "NVARCHAR", "nvarchar", "string", "nvarchar", length: true, requiredLength: true, defaultLength: 256),
        TypeRegistry.Type("text", "NVARCHAR(MAX)", "nvarchar(max)", "string", "nvarchar(max)"),
        TypeRegistry.Type("decimal", "DECIMAL", "decimal", "decimal", "decimal", precision: true, defaultPrecision: 18, defaultScale: 2),
        TypeRegistry.Type("boolean", "BIT", "bit", "bool", "bit"),
        TypeRegistry.Type("date", "DATE", "date", "DateOnly", "date"),
        TypeRegistry.Type("timestamp", "DATETIME2", "datetime2", "DateTime", "datetime2"),
        TypeRegistry.Type("json", "NVARCHAR(MAX) JSON", "nvarchar(max)", "string", "nvarchar(max)"),
        TypeRegistry.Type("binary", "VARBINARY(MAX)", "varbinary(max)", "byte[]", "varbinary(max)")
    ];

    public DataTypeDefinition? GetType(string logicalId) =>
        Types.FirstOrDefault(t => string.Equals(t.LogicalId, logicalId, StringComparison.OrdinalIgnoreCase));

    public DataTypeDefinition MapOrWarn(string logicalId, out string? warning)
    {
        warning = logicalId == "json" ? "SQL Server JSON is stored as nvarchar(max)." : null;
        return GetType(logicalId) ?? Types[3];
    }

    public string QuoteIdentifier(string identifier) => "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";

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
        $"Server={host},{port};Database={settings.DatabaseName};User Id={settings.Username};Password={settings.Password};TrustServerCertificate=True";

    public IReadOnlyDictionary<string, string> BuildEnvironmentVariables(DatabaseSettings settings) => new Dictionary<string, string>
    {
        ["ACCEPT_EULA"] = "Y",
        ["MSSQL_SA_PASSWORD"] = "${DB_PASSWORD}",
        ["MSSQL_PID"] = "Developer"
    };

    public string BuildHealthcheckTest(DatabaseSettings settings) =>
        "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $$MSSQL_SA_PASSWORD -Q \"SELECT 1\" -b -o /dev/null";
}
