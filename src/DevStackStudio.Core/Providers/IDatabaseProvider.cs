using DevStackStudio.Core.Models;

namespace DevStackStudio.Core.Providers;

public interface IDatabaseProvider
{
    DatabaseProviderMetadata Metadata { get; }
    IReadOnlyList<DataTypeDefinition> Types { get; }
    DataTypeDefinition? GetType(string logicalId);
    DataTypeDefinition MapOrWarn(string logicalId, out string? warning);
    string QuoteIdentifier(string identifier);
    string FormatDefault(ColumnDefinition column, DataTypeDefinition type);
    string BuildConnectionString(DatabaseSettings settings, string host, int port);
    IReadOnlyDictionary<string, string> BuildEnvironmentVariables(DatabaseSettings settings);
    string BuildHealthcheckTest(DatabaseSettings settings);
    string ComposeServiceName => "db";
}

public sealed class DatabaseProviderCatalog
{
    private readonly IReadOnlyDictionary<DatabaseEngine, IDatabaseProvider> _providers;

    public static DatabaseProviderCatalog Default { get; } = new(
        new PostgresProvider(),
        new MySqlProvider(),
        new SqlServerProvider(),
        new OracleProvider());

    public DatabaseProviderCatalog(params IDatabaseProvider[] providers)
    {
        _providers = providers.ToDictionary(p => p.Metadata.Engine);
    }

    public IReadOnlyCollection<IDatabaseProvider> All => _providers.Values.ToList();

    public IDatabaseProvider Get(DatabaseEngine engine) =>
        _providers.TryGetValue(engine, out var provider)
            ? provider
            : throw new InvalidOperationException($"No database provider registered for {engine}.");
}
