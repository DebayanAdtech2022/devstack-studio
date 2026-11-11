namespace DevStackStudio.Core.Models;

public sealed class GenerationOptions
{
    public bool DryRun { get; set; }
    public bool OverwriteUnchangedGeneratedFiles { get; set; } = true;
    public bool FailOnUserFileConflict { get; set; } = true;
    public bool ApplyMigrationsOnStartup { get; set; } = true;
    public bool IncludeBackendTests { get; set; } = true;
    public bool IncludeFrontendTests { get; set; } = true;
    public string TemplateVersion { get; set; } = TemplateVersions.Current;
}

public static class TemplateVersions
{
    public const string Current = "1.0.0";
    public const string ConfigurationSchema = "1";
}
