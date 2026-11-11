using System.Text.Json;
using DevStackStudio.Core.Contracts;
using DevStackStudio.Core.Models;
using DevStackStudio.Core.Naming;
using DevStackStudio.Core.Security;
using DevStackStudio.Core.Serialization;
using DevStackStudio.Core.Validation;
using DevStackStudio.Generators.Backend;
using DevStackStudio.Generators.Docker;
using DevStackStudio.Generators.Frontend;
using DevStackStudio.Generators.Root;
using DevStackStudio.Generators.Schema;
using DevStackStudio.Generators.Support;

namespace DevStackStudio.Generators.Pipeline;

public sealed class ProjectGenerator : IProjectGenerator
{
    public async Task<GenerationResult> GenerateAsync(ProjectConfiguration configuration, CancellationToken cancellationToken = default)
    {
        NameFactory.ApplyDefaults(configuration);
        var validator = new ProjectConfigurationValidator();
        var validation = validator.Validate(configuration);
        if (!validation.IsValid)
        {
            return new GenerationResult
            {
                Succeeded = false,
                Issues = validation.Issues,
                DryRun = configuration.Options.DryRun
            };
        }

        cancellationToken.ThrowIfCancellationRequested();
        var files = new List<GeneratedFile>();
        files.AddRange(new RootFilesGenerator().Generate(configuration));
        files.Add(new SchemaSqlGenerator().Generate(configuration));
        files.AddRange(new ComposeGenerator().Generate(configuration));
        files.AddRange(new BackendCqrsGenerator().Generate(configuration));
        files.AddRange(new NextJsFrontendGenerator().Generate(configuration));
        files.Add(new GeneratedFile
        {
            RelativePath = "devstack-manifest.json",
            Contents = BuildManifestJson(configuration, files)
        });

        if (configuration.Options.DryRun)
        {
            return new GenerationResult
            {
                Succeeded = true,
                DryRun = true,
                Files = files,
                Issues = validation.Issues,
                OutputDirectory = configuration.OutputDirectory
            };
        }

        var output = Path.GetFullPath(configuration.OutputDirectory);
        Directory.CreateDirectory(output);
        var conflicts = new List<string>();
        var existingManifest = LoadManifest(Path.Combine(output, "devstack-manifest.json"));

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destination = PathSafety.CombineUnderRoot(output, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (File.Exists(destination) && configuration.Options.FailOnUserFileConflict)
            {
                var currentHash = Hashing.Sha256(await File.ReadAllTextAsync(destination, cancellationToken).ConfigureAwait(false));
                var recorded = existingManifest?.Files.FirstOrDefault(f => f.Path == file.RelativePath);
                if (recorded is not null && !string.Equals(recorded.Sha256, currentHash, StringComparison.OrdinalIgnoreCase))
                {
                    conflicts.Add(file.RelativePath);
                    continue;
                }
            }

            await File.WriteAllTextAsync(destination, file.Contents, cancellationToken).ConfigureAwait(false);
            if (file.IsSecret && !OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(destination, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }

        // If an existing manifest was present, clean up files from previous generation that are no longer in this generation
        if (existingManifest is not null && conflicts.Count == 0)
        {
            var newFilePaths = new HashSet<string>(files.Select(f => f.RelativePath), StringComparer.OrdinalIgnoreCase);
            foreach (var recordedFile in existingManifest.Files)
            {
                if (!newFilePaths.Contains(recordedFile.Path))
                {
                    var oldDestination = PathSafety.CombineUnderRoot(output, recordedFile.Path);
                    if (File.Exists(oldDestination))
                    {
                        try
                        {
                            File.Delete(oldDestination);
                            // Also clean up empty parent directories if any
                            var parentDir = Path.GetDirectoryName(oldDestination);
                            while (!string.IsNullOrEmpty(parentDir) && !string.Equals(parentDir, output, StringComparison.OrdinalIgnoreCase))
                            {
                                if (Directory.Exists(parentDir) && !Directory.EnumerateFileSystemEntries(parentDir).Any())
                                {
                                    Directory.Delete(parentDir);
                                    parentDir = Path.GetDirectoryName(parentDir);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
            }
        }

        return new GenerationResult
        {
            Succeeded = conflicts.Count == 0,
            OutputDirectory = output,
            Files = files,
            Conflicts = conflicts,
            Issues = validation.Issues,
            ManifestPath = Path.Combine(output, "devstack-manifest.json")
        };
    }

    private static string BuildManifestJson(ProjectConfiguration configuration, IReadOnlyList<GeneratedFile> files)
    {
        var manifest = new ProjectManifest
        {
            TemplateVersion = TemplateCatalog.Version,
            ProjectName = configuration.ProjectName,
            Files = files.Select(f => new ManifestFileEntry
            {
                Path = f.RelativePath,
                Sha256 = Hashing.Sha256(f.Contents),
                UserOwned = f.IsUserOwned
            }).ToList()
        };
        return JsonSerializer.Serialize(manifest, ProjectConfigurationSerializer.Options);
    }

    private static ProjectManifest? LoadManifest(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProjectManifest>(File.ReadAllText(path), ProjectConfigurationSerializer.Options);
    }
}
