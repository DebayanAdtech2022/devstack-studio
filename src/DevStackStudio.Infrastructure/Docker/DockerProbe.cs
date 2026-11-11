using DevStackStudio.Core.Contracts;
using DevStackStudio.Core.Models;

namespace DevStackStudio.Infrastructure.Docker;

public sealed class DockerProbe(IProcessRunner processRunner) : IDockerProbe
{
    public async Task<DockerReadiness> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var cli = await processRunner.RunAsync(new ProcessExecutionRequest
        {
            FileName = "docker",
            Arguments = ["version", "--format", "{{.Client.Version}}"],
            Timeout = TimeSpan.FromSeconds(20)
        }, cancellationToken).ConfigureAwait(false);

        if (cli.ExitCode == 127 || LooksLikeMissingCli(cli.StandardError) || LooksLikeMissingCli(cli.StandardOutput))
        {
            return new DockerReadiness
            {
                Kind = DockerReadinessKind.CliMissing,
                CliAvailable = false,
                DaemonRunning = false,
                Summary = "Docker CLI is not installed or is not on PATH.",
                Detail = FirstNonEmpty(cli.StandardError, cli.StandardOutput)
            };
        }

        if (!cli.Succeeded)
        {
            var combined = cli.StandardError + cli.StandardOutput;
            if (LooksLikePermission(combined))
            {
                return new DockerReadiness
                {
                    Kind = DockerReadinessKind.PermissionDenied,
                    CliAvailable = true,
                    DaemonRunning = false,
                    CliVersion = cli.StandardOutput.Trim(),
                    Summary = "Docker CLI is installed but the current user cannot access the Docker engine (permission denied).",
                    Detail = combined.Trim()
                };
            }

            if (LooksLikeDaemonStopped(combined))
            {
                return new DockerReadiness
                {
                    Kind = DockerReadinessKind.DaemonStopped,
                    CliAvailable = true,
                    DaemonRunning = false,
                    CliVersion = NullIfEmpty(cli.StandardOutput.Trim()),
                    Summary = "Docker CLI is installed but the Docker daemon is not running.",
                    Detail = combined.Trim()
                };
            }
        }

        var info = await processRunner.RunAsync(new ProcessExecutionRequest
        {
            FileName = "docker",
            Arguments = ["info", "--format", "{{.ServerVersion}}"],
            Timeout = TimeSpan.FromSeconds(25)
        }, cancellationToken).ConfigureAwait(false);

        if (!info.Succeeded)
        {
            var combined = info.StandardError + info.StandardOutput;
            if (LooksLikePermission(combined))
            {
                return new DockerReadiness
                {
                    Kind = DockerReadinessKind.PermissionDenied,
                    CliAvailable = true,
                    CliVersion = NullIfEmpty(cli.StandardOutput.Trim()),
                    Summary = "Docker is installed but inaccessible due to permissions.",
                    Detail = combined.Trim()
                };
            }

            return new DockerReadiness
            {
                Kind = DockerReadinessKind.DaemonStopped,
                CliAvailable = true,
                CliVersion = NullIfEmpty(cli.StandardOutput.Trim()),
                Summary = "Docker CLI is installed but the Docker daemon is not running.",
                Detail = combined.Trim()
            };
        }

        var compose = await processRunner.RunAsync(new ProcessExecutionRequest
        {
            FileName = "docker",
            Arguments = ["compose", "version"],
            Timeout = TimeSpan.FromSeconds(20)
        }, cancellationToken).ConfigureAwait(false);

        if (!compose.Succeeded)
        {
            return new DockerReadiness
            {
                Kind = DockerReadinessKind.ComposeMissing,
                CliAvailable = true,
                DaemonRunning = true,
                CliVersion = NullIfEmpty(cli.StandardOutput.Trim()),
                EngineVersion = NullIfEmpty(info.StandardOutput.Trim()),
                Summary = "Docker is running but Compose v2 (`docker compose`) was not found.",
                Detail = compose.StandardError.Trim()
            };
        }

        return new DockerReadiness
        {
            Kind = DockerReadinessKind.Ready,
            CliAvailable = true,
            DaemonRunning = true,
            ComposeV2Available = true,
            CliVersion = NullIfEmpty(cli.StandardOutput.Trim()),
            EngineVersion = NullIfEmpty(info.StandardOutput.Trim()),
            ComposeVersion = NullIfEmpty(compose.StandardOutput.Trim()),
            Summary = "Docker is running and ready."
        };
    }

    private static bool LooksLikeMissingCli(string text) =>
        text.Contains("not recognized", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("No such file", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("command not found", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("The system cannot find the file", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeDaemonStopped(string text) =>
        text.Contains("error during connect", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("Is the docker daemon running", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("dockerDesktopLinuxEngine", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("The docker daemon is not running", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikePermission(string text) =>
        text.Contains("permission denied", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("operation not permitted", StringComparison.OrdinalIgnoreCase);

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static string? FirstNonEmpty(params string[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
