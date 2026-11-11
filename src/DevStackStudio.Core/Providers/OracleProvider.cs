using DevStackStudio.Core.Models;



namespace DevStackStudio.Core.Providers;



public sealed class OracleProvider : IDatabaseProvider

{

    public DatabaseProviderMetadata Metadata { get; } = new()

    {

        Engine = DatabaseEngine.Oracle,

        DisplayName = "Oracle Database",

        ImageName = "container-registry.oracle.com/database/free",

        ImageTag = "23.5.0.0",

        SupportedArchitectures = [CpuArchitectureKind.X64],

        DefaultPort = 1521,

        DefaultUsername = "appuser",

        HealthCheckCommand = "healthcheck.sh",

        RequiresLicenseAcceptance = true,

        LicenseSummary = "Oracle Database container images are not assumed to be freely redistributable. Pulling container-registry.oracle.com/database/free requires an Oracle account and acceptance of Oracle's terms. DevStack Studio does not redistribute Oracle binaries.",

        EfCoreProviderPackage = "Oracle.EntityFrameworkCore",

        EfCoreProviderVersion = "10.23.26400",

        EfCoreSupported = true,

        InitScriptExtension = ".sql",

        GenerationSupported = true,

        GenerationLimitation = null

    };



    public IReadOnlyList<DataTypeDefinition> Types { get; } =

    [

        TypeRegistry.Type("uuid", "RAW(16)", "raw(16)", "Guid", "RAW(16)", identity: true, defaultIdentity: IdentityStrategy.Uuid),

        TypeRegistry.Type("int", "NUMBER(10)", "number(10)", "int", "NUMBER(10)", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),

        TypeRegistry.Type("bigint", "NUMBER(19)", "number(19)", "long", "NUMBER(19)", identity: true, defaultIdentity: IdentityStrategy.DatabaseIdentity),

        TypeRegistry.Type("string", "VARCHAR2", "varchar2", "string", "VARCHAR2", length: true, requiredLength: true, defaultLength: 256),

        TypeRegistry.Type("text", "CLOB", "clob", "string", "CLOB"),

        TypeRegistry.Type("decimal", "NUMBER", "number", "decimal", "NUMBER", precision: true, defaultPrecision: 18, defaultScale: 2),

        TypeRegistry.Type("boolean", "NUMBER(1)", "number(1)", "bool", "NUMBER(1)"),

        TypeRegistry.Type("date", "DATE", "date", "DateTime", "DATE"),

        TypeRegistry.Type("timestamp", "TIMESTAMP", "timestamp", "DateTime", "TIMESTAMP"),

        TypeRegistry.Type("json", "JSON", "json", "string", "JSON"),

        TypeRegistry.Type("binary", "BLOB", "blob", "byte[]", "BLOB")

    ];



    public DataTypeDefinition? GetType(string logicalId) =>

        Types.FirstOrDefault(t => string.Equals(t.LogicalId, logicalId, StringComparison.OrdinalIgnoreCase));



    public DataTypeDefinition MapOrWarn(string logicalId, out string? warning)

    {

        warning = logicalId == "boolean" ? "Oracle has no native BOOLEAN in all editions; NUMBER(1) is used." : null;

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

            _ => "'" + column.DefaultValue.Replace("'", "''", StringComparison.Ordinal) + "'"

        };

    }



    public string BuildConnectionString(DatabaseSettings settings, string host, int port)

    {

        var service = ResolveServiceName(settings.DatabaseName);

        return $"User Id={settings.Username};Password={settings.Password};Data Source={host}:{port}/{service}";

    }



    public static string ResolveServiceName(string? databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return "FREEPDB1";
        }

        return databaseName;
    }



    public IReadOnlyDictionary<string, string> BuildEnvironmentVariables(DatabaseSettings settings) => new Dictionary<string, string>

    {

        ["ORACLE_PWD"] = "${DB_PASSWORD}",

        ["ORACLE_CHARACTERSET"] = "AL32UTF8"

    };



    public string BuildHealthcheckTest(DatabaseSettings settings) => "$$ORACLE_BASE/checkDBStatus.sh || exit 1";

}

