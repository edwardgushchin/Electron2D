/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Electron2D.Build;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var sink = new JsonDiagnosticSink(Console.Out);
        var app = new RepositoryBuildApplication(sink);

        try
        {
            return await app.RunAsync(args, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sink.Write(new BuildDiagnostic(
                "root",
                "fatal",
                "error",
                "E2D-BUILD-UNHANDLED-ERROR",
                $"Unhandled repository build tool error: {ex.Message}"));
            return RepositoryBuildExitCodes.Failed;
        }
    }
}

internal sealed class RepositoryBuildApplication(JsonDiagnosticSink diagnostics)
{
    public Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Length == 0)
        {
            diagnostics.Write(new BuildDiagnostic(
                "root",
                "usage",
                "error",
                "E2D-BUILD-CLI-USAGE",
                "Expected one of: test, verify, verify readme, verify docs, update wiki --check, update docs --check, update docs, package --rid <rid>, release verify, audit package."));
            return Task.FromResult(RepositoryBuildExitCodes.Failed);
        }

        return args[0] switch
        {
            "test" => RouteExactAsync(args, "test", "test"),
            "verify" => RouteVerifyAsync(args, cancellationToken),
            "update" => RouteUpdateAsync(args, cancellationToken),
            "package" => RoutePackageAsync(args),
            "release" => RouteReleaseAsync(args),
            "audit" => RouteAuditAsync(args, cancellationToken),
            _ => UnknownCommandAsync(args[0])
        };
    }

    private Task<int> RouteAuditAsync(string[] args, CancellationToken cancellationToken)
    {
        return new AuditPackageCommand(diagnostics).RunAsync(args, cancellationToken);
    }

    private Task<int> RouteVerifyAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 1)
        {
            return RouteAsync("verify", "verify");
        }

        if (args is ["verify", "readme"])
        {
            return VerifyReadmeAsync();
        }

        if (args is ["verify", "docs"])
        {
            return VerifyDocsAsync(cancellationToken);
        }

        return InvalidArgumentsAsync("verify", "verify", "Expected: verify, verify readme, or verify docs.");
    }

    private Task<int> RouteUpdateAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args is ["update", "wiki", "--check"])
        {
            return RouteAsync("update", "update wiki --check");
        }

        if (args is ["update", "docs", "--check"])
        {
            return RunDocumentationIndexUpdateAsync(check: true, cancellationToken);
        }

        if (args is ["update", "docs"])
        {
            return RunDocumentationIndexUpdateAsync(check: false, cancellationToken);
        }

        return InvalidArgumentsAsync("update", "update", "Expected: update wiki --check, update docs --check, or update docs.");
    }

    private Task<int> RoutePackageAsync(string[] args)
    {
        if (args.Length != 3 || args[1] != "--rid" || string.IsNullOrWhiteSpace(args[2]))
        {
            return InvalidArgumentsAsync("package", "package", "Expected: package --rid <rid>.");
        }

        var rid = args[2];
        diagnostics.Write(new BuildDiagnostic(
            "package",
            "package",
            "error",
            "E2D-BUILD-PACKAGE-BLOCKED",
            $"Package creation for '{rid}' is not implemented yet; no archives were created.",
            RuntimeIdentifier: rid));
        return Task.FromResult(RepositoryBuildExitCodes.Blocked);
    }

    private Task<int> RouteReleaseAsync(string[] args)
    {
        if (args is ["release", "verify"])
        {
            diagnostics.Write(new BuildDiagnostic(
                "release",
                "release verify",
                "error",
                "E2D-BUILD-RELEASE-VERIFY-BLOCKED",
                "Release verification internals are not implemented yet; no tags, archives, or GitHub Release entries were created."));
            return Task.FromResult(RepositoryBuildExitCodes.Blocked);
        }

        return InvalidArgumentsAsync("release", "release", "Expected: release verify.");
    }

    private Task<int> RouteExactAsync(string[] args, string command, string step)
    {
        if (args.Length == 1)
        {
            return RouteAsync(command, step);
        }

        return InvalidArgumentsAsync(command, step, $"The {command} command does not accept additional arguments yet.");
    }

    private Task<int> RouteAsync(string command, string step)
    {
        diagnostics.Write(new BuildDiagnostic(
            command,
            step,
            "info",
            "E2D-BUILD-ROUTED",
            $"Routed repository build command '{step}'."));
        return Task.FromResult(RepositoryBuildExitCodes.Success);
    }

    private Task<int> VerifyReadmeAsync()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot(Directory.GetCurrentDirectory());
        if (repositoryRoot is null)
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify readme",
                "error",
                "E2D-BUILD-REPOSITORY-ROOT-NOT-FOUND",
                "Repository root was not found from the current working directory."));
            return Task.FromResult(RepositoryBuildExitCodes.Failed);
        }

        var verifier = new RepositoryReadmeVerifier(repositoryRoot);
        var verifierDiagnostics = verifier.Verify();
        foreach (var diagnostic in verifierDiagnostics)
        {
            diagnostics.Write(diagnostic);
        }

        return Task.FromResult(verifierDiagnostics.Any(diagnostic => string.Equals(diagnostic.Severity, "error", StringComparison.Ordinal))
            ? RepositoryBuildExitCodes.Failed
            : RepositoryBuildExitCodes.Success);
    }

    private Task<int> VerifyDocsAsync(CancellationToken cancellationToken)
    {
        var verifier = CreateLocalDocumentationVerifier("verify", "verify docs");
        return verifier is null
            ? Task.FromResult(RepositoryBuildExitCodes.Failed)
            : verifier.VerifyAsync(cancellationToken);
    }

    private Task<int> RunDocumentationIndexUpdateAsync(bool check, CancellationToken cancellationToken)
    {
        var step = check ? "update docs --check" : "update docs";
        var verifier = CreateLocalDocumentationVerifier("update", step);
        return verifier is null
            ? Task.FromResult(RepositoryBuildExitCodes.Failed)
            : verifier.RunGeneratedIndexCommandAsync(check, cancellationToken);
    }

    private LocalDocumentationVerifier? CreateLocalDocumentationVerifier(string command, string step)
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot(Directory.GetCurrentDirectory());
        if (repositoryRoot is null)
        {
            diagnostics.Write(new BuildDiagnostic(
                command,
                step,
                "error",
                "E2D-BUILD-REPOSITORY-ROOT-NOT-FOUND",
                "Repository root was not found from the current working directory."));
            return null;
        }

        return new LocalDocumentationVerifier(repositoryRoot, diagnostics, new ProcessRunner());
    }

    private Task<int> UnknownCommandAsync(string command)
    {
        diagnostics.Write(new BuildDiagnostic(
            command,
            command,
            "error",
            "E2D-BUILD-CLI-UNKNOWN-COMMAND",
            $"Unknown repository build command '{command}'."));
        return Task.FromResult(RepositoryBuildExitCodes.Failed);
    }

    private Task<int> InvalidArgumentsAsync(string command, string step, string message)
    {
        diagnostics.Write(new BuildDiagnostic(
            command,
            step,
            "error",
            "E2D-BUILD-CLI-INVALID-ARGUMENTS",
            message));
        return Task.FromResult(RepositoryBuildExitCodes.Failed);
    }

}

internal sealed class ProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.StepName))
        {
            throw new ArgumentException("Step name must be provided.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("Executable file name must be provided.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.WorkingDirectory))
        {
            throw new ArgumentException("Working directory must be provided.", nameof(request));
        }

        if (request.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Timeout must be greater than zero.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return CreateCanceledResult(request, string.Empty, string.Empty);
        }

        var startInfo = new ProcessStartInfo(request.FileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = request.WorkingDirectory
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ProcessRunResult(
                request.StepName,
                null,
                TimedOut: false,
                string.Empty,
                ex.Message,
                [
                    new BuildDiagnostic(
                        "process",
                        request.StepName,
                        "error",
                        "E2D-BUILD-PROCESS-START-FAILED",
                        $"Failed to start process '{request.FileName}': {ex.Message}")
                ]);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(request.Timeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            await process.WaitForExitAsync(linkedCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

            return CreateCanceledResult(
                request,
                await ReadProcessStreamSafelyAsync(stdoutTask).ConfigureAwait(false),
                await ReadProcessStreamSafelyAsync(stderrTask).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

            return new ProcessRunResult(
                request.StepName,
                null,
                TimedOut: true,
                await ReadProcessStreamSafelyAsync(stdoutTask).ConfigureAwait(false),
                await ReadProcessStreamSafelyAsync(stderrTask).ConfigureAwait(false),
                [
                    new BuildDiagnostic(
                        "process",
                        request.StepName,
                        "error",
                        "E2D-BUILD-PROCESS-TIMEOUT",
                        $"Process '{request.FileName}' timed out after {request.Timeout}.",
                        TimedOut: true)
                ]);
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        var severity = process.ExitCode == 0 ? "info" : "error";

        return new ProcessRunResult(
            request.StepName,
            process.ExitCode,
            TimedOut: false,
            stdout,
            stderr,
            [
                new BuildDiagnostic(
                    "process",
                    request.StepName,
                    severity,
                    "E2D-BUILD-PROCESS-EXITED",
                    $"Process '{request.FileName}' exited with code {process.ExitCode}.",
                    ProcessExitCode: process.ExitCode,
                    TimedOut: false)
            ]);
    }

    private static ProcessRunResult CreateCanceledResult(ProcessRunRequest request, string stdout, string stderr)
    {
        return new ProcessRunResult(
            request.StepName,
            null,
            TimedOut: false,
            stdout,
            stderr,
            [
                new BuildDiagnostic(
                    "process",
                    request.StepName,
                    "error",
                    "E2D-BUILD-PROCESS-CANCELED",
                    $"Process '{request.FileName}' was canceled by the caller.",
                    TimedOut: false)
            ]);
    }

    private static async Task<string> ReadProcessStreamSafelyAsync(Task<string> readTask)
    {
        try
        {
            return await readTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        catch (IOException)
        {
            return string.Empty;
        }
        catch (ObjectDisposedException)
        {
            return string.Empty;
        }
    }

    private static void TryKillProcessTree(Process process)
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
}

internal sealed class JsonDiagnosticSink(TextWriter writer)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Write(BuildDiagnostic diagnostic)
    {
        writer.WriteLine(JsonSerializer.Serialize(diagnostic, SerializerOptions));
    }
}

internal sealed record ProcessRunRequest(
    string StepName,
    string FileName,
    string[] Arguments,
    string WorkingDirectory,
    TimeSpan Timeout);

internal sealed record ProcessRunResult(
    string StepName,
    int? ExitCode,
    bool TimedOut,
    string StandardOutput,
    string StandardError,
    IReadOnlyList<BuildDiagnostic> Diagnostics);

internal sealed record BuildDiagnostic(
    string Command,
    string Step,
    string Severity,
    string Code,
    string Message,
    int? ProcessExitCode = null,
    bool? TimedOut = null,
    string? RuntimeIdentifier = null,
    string? ZipPath = null,
    bool? Force = null,
    string? Path = null);

internal static class RepositoryBuildExitCodes
{
    public const int Success = 0;
    public const int Failed = 1;
    public const int Blocked = 2;
}
