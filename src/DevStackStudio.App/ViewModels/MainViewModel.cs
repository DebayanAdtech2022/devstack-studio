using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevStackStudio.Core.Contracts;
using DevStackStudio.Core.Environment;
using DevStackStudio.Core.Models;
using DevStackStudio.Core.Naming;
using DevStackStudio.Core.Providers;
using DevStackStudio.Core.Samples;
using DevStackStudio.Core.Serialization;
using DevStackStudio.Core.Validation;
using DevStackStudio.Infrastructure.Network;

namespace DevStackStudio.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEnvironmentDetector _environmentDetector;
    private readonly IDockerProbe _dockerProbe;
    private readonly IDockerOrchestrator _dockerOrchestrator;
    private readonly IProjectGenerator _projectGenerator;

    [ObservableProperty]
    private int _currentStepIndex;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private HostEnvironmentInfo _environment = new();

    [ObservableProperty]
    private DockerReadiness _docker = new();

    [ObservableProperty]
    private ProjectConfiguration _configuration = SampleProjects.CatalogPostgres();

    [ObservableProperty]
    private bool _forceOverwriteExistingFiles = true;

    [ObservableProperty]
    private EntityDefinition? _selectedEntity;

    [ObservableProperty]
    private string _newEntityName = string.Empty;

    [ObservableProperty]
    private string _newColumnName = string.Empty;

    [ObservableProperty]
    private string _newColumnType = "string";

    [ObservableProperty]
    private string _newColumnLength = "256";

    public bool IsCurrentColumnLengthVisible =>
        SelectedDatabaseProvider.GetType(NewColumnType)?.SupportsLength == true;

    partial void OnNewColumnTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCurrentColumnLengthVisible));
        var typeDef = SelectedDatabaseProvider.GetType(value);
        if (typeDef?.SupportsLength == true)
        {
            NewColumnLength = (typeDef.DefaultLength ?? 256).ToString();
        }
        else
        {
            NewColumnLength = string.Empty;
        }
    }

    [ObservableProperty]
    private bool _newColumnNullable = false;

    [ObservableProperty]
    private bool _newColumnPrimaryKey = false;

    [ObservableProperty]
    private EntityDefinition? _newColumnForeignKeyTarget;

    public IEnumerable<EntityDefinition?> AvailableForeignKeyTargets
    {
        get
        {
            yield return null;
            foreach (var e in Configuration.Entities)
            {
                if (e != SelectedEntity)
                {
                    yield return e;
                }
            }
        }
    }

    [ObservableProperty]
    private string _generationOutput = string.Empty;

    [ObservableProperty]
    private string _orchestrationLogs = string.Empty;

    [ObservableProperty]
    private bool _servicesRunning;

    public string EnvironmentSummary => $"{Environment.OsDescription} ({Environment.RuntimeIdentifier})";
    public string DotNetSummary => ".NET 10.0 LTS (host)";

    public DatabaseEngine SelectedDatabaseEngine => Configuration.Database.Engine;
    public string ProjectName
    {
        get => Configuration.ProjectName;
        set
        {
            if (Configuration.ProjectName != value)
            {
                Configuration.ProjectName = value;
                NameFactory.ApplyDefaults(Configuration);
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(Configuration));
            }
        }
    }
    public BackendFramework BackendFramework => Configuration.Backend;
    public FrontendFramework FrontendFramework => Configuration.Frontend;

    public bool IsStep0 => CurrentStepIndex == 0;
    public bool IsStep1 => CurrentStepIndex == 1;
    public bool IsStep2 => CurrentStepIndex == 2;
    public bool IsStep3 => CurrentStepIndex == 3;
    public bool IsStep4 => CurrentStepIndex == 4;
    public bool IsStep5 => CurrentStepIndex == 5;
    public bool IsStep6 => CurrentStepIndex == 6;
    public bool IsStep7 => CurrentStepIndex == 7;

    public bool CanGoPrevious => CurrentStepIndex > 0;
    public bool CanGoNext => CurrentStepIndex < 7 &&
        !(CurrentStepIndex == 2 && RequiresLicenseAcceptance && !AcceptProviderLicense);

    partial void OnCurrentStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(IsStep5));
        OnPropertyChanged(nameof(IsStep6));
        OnPropertyChanged(nameof(IsStep7));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        PreviousStepCommand.NotifyCanExecuteChanged();
        NextStepCommand.NotifyCanExecuteChanged();
    }

    public ObservableCollection<EntityDefinition> Entities { get; } = new();
    public ObservableCollection<ColumnDefinition> SelectedEntityColumns { get; } = new();

    partial void OnSelectedEntityChanged(EntityDefinition? value)
    {
        SelectedEntityColumns.Clear();
        if (value?.Columns != null)
        {
            foreach (var col in value.Columns)
            {
                SelectedEntityColumns.Add(col);
            }
        }
        NewColumnForeignKeyTarget = null;
        OnPropertyChanged(nameof(AvailableForeignKeyTargets));
    }

    public int HostPort
    {
        get => Configuration.Database.HostPort;
        set
        {
            if (Configuration.Database.HostPort != value)
            {
                Configuration.Database.HostPort = value;
                OnPropertyChanged(nameof(HostPort));
                OnPropertyChanged(nameof(IsHostPortInUse));
                OnPropertyChanged(nameof(PortConflictNotice));
            }
        }
    }

    public bool IsHostPortInUse => PortDetector.IsPortInUse(HostPort);

    public string PortConflictNotice =>
        IsHostPortInUse
            ? $"Port {HostPort} is currently in use on your host (e.g. by an existing database service). An available port was auto-assigned."
            : string.Empty;

    public void ResolveAvailableDatabasePort()
    {
        var defaultPort = SelectedDatabaseProvider.Metadata.DefaultPort;
        if (PortDetector.IsPortInUse(defaultPort))
        {
            HostPort = PortDetector.FindNextAvailablePort(defaultPort + 1, Configuration.Docker.BackendHostPort, Configuration.Docker.FrontendHostPort);
        }
        else
        {
            HostPort = defaultPort;
        }
    }

    public IReadOnlyList<IDatabaseProvider> DatabaseProviders => DatabaseProviderCatalog.Default.All.ToList();

    public IDatabaseProvider SelectedDatabaseProvider
    {
        get => DatabaseProviderCatalog.Default.Get(Configuration.Database.Engine);
        set
        {
            if (value != null && Configuration.Database.Engine != value.Metadata.Engine)
            {
                Configuration.Database.Engine = value.Metadata.Engine;
                Configuration.Database.Username = value.Metadata.DefaultUsername;
                NameFactory.ApplyDefaults(Configuration);
                ResolveAvailableDatabasePort();
                OnPropertyChanged(nameof(SelectedDatabaseProvider));
                OnPropertyChanged(nameof(SelectedDatabaseEngine));
                OnPropertyChanged(nameof(Configuration));
                OnPropertyChanged(nameof(HostPort));
                OnPropertyChanged(nameof(IsHostPortInUse));
                OnPropertyChanged(nameof(PortConflictNotice));
                OnPropertyChanged(nameof(DatabaseTypes));
                OnPropertyChanged(nameof(ProviderLimitationWarning));
                OnPropertyChanged(nameof(IsCurrentColumnLengthVisible));
                OnPropertyChanged(nameof(RequiresLicenseAcceptance));
                OnPropertyChanged(nameof(AcceptProviderLicense));
                OnPropertyChanged(nameof(CanGoNext));
                NextStepCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<DataTypeDefinition> DatabaseTypes => SelectedDatabaseProvider.Types;

    public bool RequiresLicenseAcceptance => SelectedDatabaseProvider.Metadata.RequiresLicenseAcceptance;

    public bool AcceptProviderLicense
    {
        get => Configuration.Database.AcceptProviderLicense;
        set
        {
            if (Configuration.Database.AcceptProviderLicense != value)
            {
                Configuration.Database.AcceptProviderLicense = value;
                OnPropertyChanged(nameof(AcceptProviderLicense));
                OnPropertyChanged(nameof(CanGoNext));
                NextStepCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ProviderLimitationWarning =>
        SelectedDatabaseProvider.Metadata.GenerationLimitation ??
        (SelectedDatabaseProvider.Metadata.RequiresLicenseAcceptance ? SelectedDatabaseProvider.Metadata.LicenseSummary : string.Empty);

    public MainViewModel(
        IEnvironmentDetector environmentDetector,
        IDockerProbe dockerProbe,
        IDockerOrchestrator dockerOrchestrator,
        IProjectGenerator projectGenerator)
    {
        _environmentDetector = environmentDetector;
        _dockerProbe = dockerProbe;
        _dockerOrchestrator = dockerOrchestrator;
        _projectGenerator = projectGenerator;

        SyncEntitiesFromConfiguration();
        ResolveAvailableDatabasePort();

        _ = RefreshEnvironmentAsync();
    }

    private void SyncEntitiesFromConfiguration()
    {
        Entities.Clear();
        foreach (var entity in Configuration.Entities)
        {
            Entities.Add(entity);
        }

        SelectedEntity = Entities.FirstOrDefault();
    }

    [RelayCommand]
    public async Task RefreshEnvironmentAsync()
    {
        IsBusy = true;
        StatusMessage = "Detecting environment and probing Docker...";
        try
        {
            Environment = await _environmentDetector.DetectAsync();
            Docker = await _dockerProbe.ProbeAsync();
            OnPropertyChanged(nameof(EnvironmentSummary));
            OnPropertyChanged(nameof(DotNetSummary));
            StatusMessage = Docker.Summary;
        }
        catch (Exception ex)
        {
            StatusMessage = "Environment check error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    public void NextStep()
    {
        if (CurrentStepIndex < 7)
        {
            CurrentStepIndex++;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    public void PreviousStep()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
        }
    }

    [RelayCommand]
    public void GoToStep(object? parameter)
    {
        int stepIndex;
        if (parameter is int i)
        {
            stepIndex = i;
        }
        else if (parameter is string s && int.TryParse(s, out var parsed))
        {
            stepIndex = parsed;
        }
        else if (parameter is IConvertible convertible)
        {
            try
            {
                stepIndex = convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (stepIndex >= 0 && stepIndex <= 7)
        {
            CurrentStepIndex = stepIndex;
        }
    }

    [RelayCommand]
    public void AddEntity()
    {
        var name = NewEntityName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "NewEntity" + (Configuration.Entities.Count + 1);
        }

        if (!IdentifierRules.IsValidIdentifier(name))
        {
            StatusMessage = $"'{name}' is not a valid identifier.";
            return;
        }

        if (Configuration.Entities.Any(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"Entity '{name}' already exists.";
            return;
        }

        var entity = new EntityDefinition
        {
            Name = name,
            Columns =
            [
                new ColumnDefinition
                {
                    Name = "Id",
                    LogicalType = "uuid",
                    IsPrimaryKey = true,
                    Identity = IdentityStrategy.Uuid
                }
            ]
        };

        Configuration.Entities.Add(entity);
        Entities.Add(entity);
        NewEntityName = string.Empty;
        SelectedEntity = entity;
        StatusMessage = $"Added entity '{name}'.";
    }

    [RelayCommand]
    public void RemoveEntity(EntityDefinition? entity)
    {
        entity ??= SelectedEntity;
        if (entity is null) return;

        // Check if any other entity has a foreign key referencing this entity
        var referencingColumns = Configuration.Entities
            .Where(e => e.Id != entity.Id)
            .SelectMany(e => e.Columns.Where(c => c.ForeignKeyTargetEntityId == entity.Id).Select(c => new { Entity = e, Column = c }))
            .ToList();

        if (referencingColumns.Count > 0)
        {
            var refs = string.Join(", ", referencingColumns.Select(r => $"{r.Entity.Name}.{r.Column.Name}"));
            StatusMessage = $"Cannot delete '{entity.Name}' because it is referenced by foreign key ({refs}). Remove the foreign key column first.";
            return;
        }

        Configuration.Entities.Remove(entity);
        Entities.Remove(entity);

        // Remove any relationships that involved this entity
        Configuration.Relationships.RemoveAll(r => r.FromEntityId == entity.Id || r.ToEntityId == entity.Id);

        if (SelectedEntity == entity)
        {
            SelectedEntity = Entities.FirstOrDefault();
        }
        OnPropertyChanged(nameof(AvailableForeignKeyTargets));
        StatusMessage = $"Removed entity '{entity.Name}'.";
    }

    [RelayCommand]
    public void SelectEntity(EntityDefinition entity)
    {
        SelectedEntity = entity;
    }

    [RelayCommand]
    public void AddColumn()
    {
        if (SelectedEntity is null) return;

        var name = NewColumnName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Column" + (SelectedEntity.Columns.Count + 1);
        }

        if (!IdentifierRules.IsValidIdentifier(name))
        {
            StatusMessage = $"'{name}' is not a valid column name.";
            return;
        }

        if (SelectedEntity.Columns.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = $"Column '{name}' already exists on '{SelectedEntity.Name}'.";
            return;
        }

        if (NewColumnPrimaryKey && SelectedEntity.Columns.Any(c => c.IsPrimaryKey))
        {
            StatusMessage = $"Entity '{SelectedEntity.Name}' already has a primary key column. Only one primary key is allowed.";
            return;
        }

        var logicalType = string.IsNullOrWhiteSpace(NewColumnType) ? "string" : NewColumnType;
        Guid? fkEntityId = null;
        Guid? fkColumnId = null;

        if (NewColumnForeignKeyTarget is not null)
        {
            fkEntityId = NewColumnForeignKeyTarget.Id;
            var targetPk = NewColumnForeignKeyTarget.Columns.FirstOrDefault(c => c.IsPrimaryKey);
            if (targetPk is not null)
            {
                fkColumnId = targetPk.Id;
                logicalType = targetPk.LogicalType; // Inherit exact type from target PK
            }
        }

        var typeDef = SelectedDatabaseProvider.GetType(logicalType);

        int? length = null;
        if (typeDef?.SupportsLength == true && fkEntityId is null)
        {
            if (int.TryParse(NewColumnLength, out var parsedLength) && parsedLength > 0)
            {
                length = parsedLength;
            }
            else
            {
                length = typeDef.DefaultLength ?? 256;
            }
        }

        var col = new ColumnDefinition
        {
            Name = name,
            LogicalType = logicalType,
            Length = length,
            IsNullable = NewColumnNullable,
            IsPrimaryKey = NewColumnPrimaryKey,
            ForeignKeyTargetEntityId = fkEntityId,
            ForeignKeyTargetColumnId = fkColumnId,
            Ordinal = SelectedEntity.Columns.Count
        };

        SelectedEntity.Columns.Add(col);
        SelectedEntityColumns.Add(col);

        // If this column is a foreign key, register the relationship in Configuration.Relationships
        if (fkEntityId is not null && fkColumnId is not null)
        {
            Configuration.Relationships.Add(new RelationshipDefinition
            {
                Name = $"{SelectedEntity.Name}{NewColumnForeignKeyTarget?.Name}",
                Kind = RelationshipKind.OneToMany,
                FromEntityId = SelectedEntity.Id,
                FromColumnId = col.Id,
                ToEntityId = fkEntityId.Value,
                ToColumnId = fkColumnId.Value
            });
        }

        NewColumnName = string.Empty;
        NewColumnLength = typeDef?.SupportsLength == true ? (typeDef.DefaultLength ?? 256).ToString() : string.Empty;
        NewColumnNullable = false;
        NewColumnPrimaryKey = false;
        NewColumnForeignKeyTarget = null;
        StatusMessage = $"Added column '{name}' to '{SelectedEntity.Name}'.";
    }

    [RelayCommand]
    public void RemoveColumn(ColumnDefinition column)
    {
        if (SelectedEntity is null || column is null) return;

        if (column.IsPrimaryKey && SelectedEntity.Columns.Count(c => c.IsPrimaryKey) <= 1)
        {
            StatusMessage = $"Cannot delete primary key column '{column.Name}'. Every entity must have a primary key.";
            return;
        }

        // If other entities have foreign keys referencing this column, block deletion
        var referencing = Configuration.Entities
            .SelectMany(e => e.Columns.Where(c => c.ForeignKeyTargetColumnId == column.Id).Select(c => new { Entity = e, Column = c }))
            .ToList();

        if (referencing.Count > 0)
        {
            var refs = string.Join(", ", referencing.Select(r => $"{r.Entity.Name}.{r.Column.Name}"));
            StatusMessage = $"Cannot delete column '{column.Name}' because it is referenced by foreign key ({refs}).";
            return;
        }

        // Remove any relationships involving this column
        Configuration.Relationships.RemoveAll(r => r.FromColumnId == column.Id || r.ToColumnId == column.Id);

        SelectedEntity.Columns.Remove(column);
        SelectedEntityColumns.Remove(column);
        StatusMessage = $"Removed column '{column.Name}'.";
    }

    [RelayCommand]
    public async Task SaveConfigurationAsync(string filePath)
    {
        try
        {
            var json = ProjectConfigurationSerializer.Serialize(Configuration);
            await File.WriteAllTextAsync(filePath, json);
            StatusMessage = $"Configuration saved to {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save error: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task LoadConfigurationAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var loaded = ProjectConfigurationSerializer.Deserialize(json);
            if (loaded != null)
            {
                Configuration = loaded;
                SyncEntitiesFromConfiguration();
                OnPropertyChanged(nameof(SelectedDatabaseProvider));
                OnPropertyChanged(nameof(SelectedDatabaseEngine));
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(BackendFramework));
                OnPropertyChanged(nameof(FrontendFramework));
                StatusMessage = $"Configuration loaded from {filePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load error: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task GenerateProjectAsync()
    {
        IsBusy = true;
        StatusMessage = "Validating and generating project...";
        try
        {
            if (string.IsNullOrWhiteSpace(Configuration.OutputDirectory))
            {
                Configuration.OutputDirectory = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "DevStackProjects",
                    Configuration.ProjectName);
            }

            Configuration.Options.FailOnUserFileConflict = !ForceOverwriteExistingFiles;
            Configuration.Options.OverwriteUnchangedGeneratedFiles = true;
            var result = await _projectGenerator.GenerateAsync(Configuration);
            if (result.Succeeded)
            {
                GenerationOutput = $"Success! {result.Files.Count} files generated in:\n{result.OutputDirectory}\n\n" +
                                   $"Manifest: {result.ManifestPath}\n" +
                                   string.Join("\n", result.Files.Take(12).Select(f => " + " + f.RelativePath)) +
                                   (result.Files.Count > 12 ? $"\n... and {result.Files.Count - 12} more files" : "");
                StatusMessage = "Project generated successfully!";
            }
            else
            {
                GenerationOutput = "Generation Failed:\n" +
                                   string.Join("\n", result.Issues.Select(i => $"[{i.Severity}] {i.Message}")) +
                                   "\nConflicts:\n" + string.Join("\n", result.Conflicts);
                StatusMessage = "Project generation failed. Check errors.";
            }
        }
        catch (Exception ex)
        {
            GenerationOutput = "Error: " + ex.Message;
            StatusMessage = "Generation error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task StartServicesAsync()
    {
        if (string.IsNullOrWhiteSpace(Configuration.OutputDirectory) || !Directory.Exists(Configuration.OutputDirectory))
        {
            StatusMessage = "Output directory does not exist. Generate project first.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Starting Docker Compose stack (up --build -d)...";
        try
        {
            var res = await _dockerOrchestrator.ComposeAsync(Configuration.OutputDirectory, ["up", "--build", "-d"]);
            OrchestrationLogs = res.StandardOutput + "\n" + res.StandardError;
            ServicesRunning = res.Succeeded;
            StatusMessage = res.Succeeded ? "Services started successfully." : "Failed to start services.";
        }
        catch (Exception ex)
        {
            OrchestrationLogs = "Exception: " + ex.Message;
            StatusMessage = "Docker error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task StopServicesAsync()
    {
        if (string.IsNullOrWhiteSpace(Configuration.OutputDirectory) || !Directory.Exists(Configuration.OutputDirectory)) return;

        IsBusy = true;
        StatusMessage = "Stopping Docker Compose stack...";
        try
        {
            var res = await _dockerOrchestrator.ComposeAsync(Configuration.OutputDirectory, ["down"]);
            OrchestrationLogs = res.StandardOutput + "\n" + res.StandardError;
            ServicesRunning = false;
            StatusMessage = res.Succeeded ? "Services stopped." : "Stop failed.";
        }
        catch (Exception ex)
        {
            OrchestrationLogs = "Exception: " + ex.Message;
            StatusMessage = "Docker error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
