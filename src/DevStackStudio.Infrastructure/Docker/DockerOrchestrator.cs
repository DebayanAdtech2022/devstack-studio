using DevStackStudio.Core.Contracts;

namespace DevStackStudio.Infrastructure.Docker;

public sealed class DockerOrchestrator(IProcessRunner processRunner) : IDockerOrchestrator
{
    public Task<ProcessExecutionResult> ComposeAsync(string projectDirectory, IReadOnlyList<string> composeArgs, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        if (!Directory.Exists(projectDirectory))
        {
            throw new DirectoryNotFoundException(projectDirectory);
        }

        var args = new List<string> { "compose" };
        args.AddRange(composeArgs);
        return processRunner.RunAsync(new ProcessExecutionRequest
        {
            FileName = "docker",
            Arguments = args,
            WorkingDirectory = projectDirectory,
            Timeout = TimeSpan.FromMinutes(30)
        }, cancellationToken);
    }
}
