using System.Diagnostics;
using System.Text;
using DevStackStudio.Core.Contracts;
using DevStackStudio.Core.Security;
using Microsoft.Extensions.Logging;

namespace DevStackStudio.Infrastructure.Process;

public sealed class ProcessRunner(ILogger<ProcessRunner>? logger = null) : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProcessRunner>.Instance;

    public async Task<ProcessExecutionResult> RunAsync(ProcessExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);

        var fileName = ResolveExecutablePath(request.FileName);

        var start = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = request.WorkingDirectory ?? System.Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in request.Arguments)
        {
            start.ArgumentList.Add(argument);
        }

        if (request.Environment is not null)
        {
            foreach (var pair in request.Environment)
            {
                start.Environment[pair.Key] = pair.Value;
            }
        }

        _logger.LogInformation("Executing {File} in {Dir}", request.FileName, start.WorkingDirectory);

        using var process = new System.Diagnostics.Process { StartInfo = start, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdout.AppendLine(SecretRedactor.Redact(e.Data));
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderr.AppendLine(SecretRedactor.Redact(e.Data));
            }
        };

        try
        {
            if (!process.Start())
            {
                return new ProcessExecutionResult { ExitCode = -1, StandardError = "Failed to start process." };
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or System.ComponentModel.Win32Exception)
        {
            return new ProcessExecutionResult { ExitCode = 127, StandardError = SecretRedactor.Redact(ex.Message) };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = request.Timeout is { } timeout ? new CancellationTokenSource(timeout) : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            TryKill(process);
            return new ProcessExecutionResult
            {
                ExitCode = -1,
                TimedOut = true,
                StandardOutput = stdout.ToString(),
                StandardError = stderr + System.Environment.NewLine + "Process timed out."
            };
        }

        return new ProcessExecutionResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout.ToString(),
            StandardError = stderr.ToString()
        };
    }

    private static void TryKill(System.Diagnostics.Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string ResolveExecutablePath(string fileName)
    {
        if (Path.IsPathRooted(fileName) || fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            return fileName;
        }

        // On macOS / Linux, standard paths might not be in PATH if launched outside a full login shell
        if (!OperatingSystem.IsWindows() && (fileName == "docker" || fileName == "docker-compose"))
        {
            string[] searchPaths =
            [
                "/usr/local/bin",
                "/opt/homebrew/bin",
                "/usr/bin",
                "/bin",
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".docker", "bin"),
                "/Applications/Docker.app/Contents/Resources/bin"
            ];

            foreach (var dir in searchPaths)
            {
                var candidate = Path.Combine(dir, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return fileName;
    }
}
