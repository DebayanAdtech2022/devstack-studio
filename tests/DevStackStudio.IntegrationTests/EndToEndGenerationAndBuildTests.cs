using DevStackStudio.Core.Models;
using DevStackStudio.Core.Samples;
using DevStackStudio.Generators.Pipeline;
using DevStackStudio.Infrastructure.Process;

namespace DevStackStudio.IntegrationTests;

public class EndToEndGenerationAndBuildTests
{
    [Fact]
    public async Task Generate_Postgres_stack_and_verify_backend_compiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DevStackTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var config = SampleProjects.CatalogPostgres();
            config.ProjectName = "CatalogTestApp";
            config.OutputDirectory = tempDir;

            var generator = new ProjectGenerator();
            var genResult = await generator.GenerateAsync(config);

            Assert.True(genResult.Succeeded, string.Join("; ", genResult.Issues.Select(i => i.Message)));
            Assert.True(File.Exists(Path.Combine(tempDir, "docker-compose.yml")));
            Assert.True(File.Exists(Path.Combine(tempDir, "devstack-manifest.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "database", "schema.sql")));

            // Locate generated solution or backend project
            var backendDir = Path.Combine(tempDir, "backend");
            var slnxFile = Directory.GetFiles(backendDir, "*.slnx").FirstOrDefault();
            Assert.NotNull(slnxFile);

            // Test compiling the generated backend using dotnet build
            var runner = new ProcessRunner();
            var buildResult = await runner.RunAsync(new DevStackStudio.Core.Contracts.ProcessExecutionRequest
            {
                FileName = "dotnet",
                Arguments = ["build", slnxFile!, "-m:1", "/p:UseSharedCompilation=false"],
                WorkingDirectory = backendDir,
                Timeout = TimeSpan.FromMinutes(4)
            });

            Assert.True(buildResult.Succeeded, $"Backend build failed:\nSTDOUT:\n{buildResult.StandardOutput}\nSTDERR:\n{buildResult.StandardError}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { /* ignore cleanup errors */ }
            }
        }
    }

    [Fact]
    public async Task Manifest_verification_and_conflict_detection_work()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DevStackConflictTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var config = SampleProjects.CatalogPostgres();
            config.ProjectName = "ConflictDemo";
            config.OutputDirectory = tempDir;

            var generator = new ProjectGenerator();
            var result1 = await generator.GenerateAsync(config);
            Assert.True(result1.Succeeded);

            // Modify a generated file
            var readmePath = Path.Combine(tempDir, "README.md");
            await File.WriteAllTextAsync(readmePath, "# Custom User Edits Here");

            // Re-run with conflict checking enabled
            config.Options.FailOnUserFileConflict = true;
            var result2 = await generator.GenerateAsync(config);

            Assert.False(result2.Succeeded);
            Assert.Contains(result2.Conflicts, c => c == "README.md");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
