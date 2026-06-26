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
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class RepositoryBuildToolTests
{
    [Fact]
    public async Task UnknownCommandReturnsNonZeroStructuredDiagnostic()
    {
        var result = await RunBuildToolAsync("__unknown__");

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("__unknown__", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("__unknown__", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-CLI-UNKNOWN-COMMAND", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.Contains("__unknown__", diagnostic.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("verify", "verify")]
    [InlineData("verify readme", "verify", "readme")]
    [InlineData("verify docs", "verify", "docs")]
    [InlineData("update wiki --check", "update", "wiki", "--check")]
    public async Task SkeletonCommandsRouteToStableDiagnosticShape(string expectedStep, params string[] arguments)
    {
        var result = await RunBuildToolAsync(arguments);

        Assert.Equal(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal(arguments[0], diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal(expectedStep, diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("info", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-ROUTED", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.False(diagnostic.RootElement.TryGetProperty("processExitCode", out _));
        Assert.False(diagnostic.RootElement.TryGetProperty("timedOut", out _));
    }

    [Fact]
    public async Task ReleaseVerifyFailsClosedWithoutArtifacts()
    {
        using var workspace = TemporaryDirectory.Create("release-verify");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "release", "verify");

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("release", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("release verify", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-RELEASE-VERIFY-BLOCKED", diagnostic.RootElement.GetProperty("code").GetString());
        AssertNoReleaseArtifacts(workspace.Root);
    }

    [Fact]
    public async Task PackageWithRidFailsClosedWithoutArtifacts()
    {
        using var workspace = TemporaryDirectory.Create("package-rid");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "package", "--rid", "win-x64");

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("package", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("package", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-PACKAGE-BLOCKED", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.Equal("win-x64", diagnostic.RootElement.GetProperty("runtimeIdentifier").GetString());
        AssertNoReleaseArtifacts(workspace.Root);
    }

    [Theory]
    [InlineData("missing rid flag", "package")]
    [InlineData("missing rid value", "package", "--rid")]
    [InlineData("blank rid value", "package", "--rid", "")]
    [InlineData("misplaced rid flag", "package", "win-x64", "--rid")]
    [InlineData("extra argument", "package", "--rid", "win-x64", "--extra")]
    [InlineData("duplicate rid flag", "package", "--rid", "win-x64", "--rid", "linux-x64")]
    [InlineData("unknown flag", "package", "--runtime", "win-x64")]
    public async Task PackageRejectsInvalidArgumentShape(string caseName, params string[] arguments)
    {
        var result = await RunBuildToolAsync(arguments);

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("package", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("package", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-CLI-INVALID-ARGUMENTS", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.False(
            diagnostic.RootElement.TryGetProperty("runtimeIdentifier", out _),
            $"Invalid package arguments should not accept a runtime identifier for case '{caseName}'.");
    }

    [Fact]
    public async Task ProcessRunnerCapturesOutputErrorAndChildExitCode()
    {
        using var child = await CreateBuiltChildProjectAsync(
            "exit-code",
            """
            Console.WriteLine("child stdout");
            Console.Error.WriteLine("child stderr");
            Environment.ExitCode = 37;
            """);

        var result = await RunProcessRunnerAsync(
            "child-exit",
            DotnetExecutable,
            [child.AssemblyPath],
            Path.GetDirectoryName(child.AssemblyPath)!,
            TimeSpan.FromSeconds(10));

        Assert.Equal(37, GetProperty<int?>(result, "ExitCode"));
        Assert.False(GetProperty<bool>(result, "TimedOut"));
        Assert.Contains("child stdout", GetProperty<string>(result, "StandardOutput"), StringComparison.Ordinal);
        Assert.Contains("child stderr", GetProperty<string>(result, "StandardError"), StringComparison.Ordinal);

        var diagnostic = Assert.Single(GetDiagnostics(result));
        Assert.Equal("process", GetProperty<string>(diagnostic, "Command"));
        Assert.Equal("child-exit", GetProperty<string>(diagnostic, "Step"));
        Assert.Equal("error", GetProperty<string>(diagnostic, "Severity"));
        Assert.Equal("E2D-BUILD-PROCESS-EXITED", GetProperty<string>(diagnostic, "Code"));
        Assert.Equal(37, GetProperty<int?>(diagnostic, "ProcessExitCode"));
        Assert.False(GetProperty<bool>(diagnostic, "TimedOut"));
    }

    [Fact]
    public async Task ProcessRunnerTimeoutCancelsChildAndReturnsTimeoutDiagnostic()
    {
        using var child = await CreateBuiltChildProjectAsync(
            "timeout",
            """
            using System.Threading;

            Console.WriteLine("timeout child started");
            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);

        var result = await RunProcessRunnerAsync(
            "child-timeout",
            DotnetExecutable,
            [child.AssemblyPath],
            Path.GetDirectoryName(child.AssemblyPath)!,
            TimeSpan.FromMilliseconds(250));

        Assert.Null(GetProperty<int?>(result, "ExitCode"));
        Assert.True(GetProperty<bool>(result, "TimedOut"));
        Assert.Contains("timeout child started", GetProperty<string>(result, "StandardOutput"), StringComparison.Ordinal);

        var diagnostic = Assert.Single(GetDiagnostics(result));
        Assert.Equal("process", GetProperty<string>(diagnostic, "Command"));
        Assert.Equal("child-timeout", GetProperty<string>(diagnostic, "Step"));
        Assert.Equal("error", GetProperty<string>(diagnostic, "Severity"));
        Assert.Equal("E2D-BUILD-PROCESS-TIMEOUT", GetProperty<string>(diagnostic, "Code"));
        Assert.Null(GetProperty<int?>(diagnostic, "ProcessExitCode"));
        Assert.True(GetProperty<bool>(diagnostic, "TimedOut"));
    }

    [Fact]
    public async Task ProcessRunnerExternalCancellationKillsChildAndReturnsCancellationDiagnostic()
    {
        using var child = await CreateBuiltChildProjectAsync(
            "external-cancel",
            """
            using System.Threading;

            Console.WriteLine("external cancel child started");
            Console.Out.Flush();
            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);
        var assembly = await BuildAndLoadBuildToolAssemblyAsync();
        using var cancellation = new CancellationTokenSource();

        var runTask = RunProcessRunnerAsync(
            assembly,
            "child-external-cancel",
            DotnetExecutable,
            [child.AssemblyPath],
            Path.GetDirectoryName(child.AssemblyPath)!,
            TimeSpan.FromSeconds(30),
            cancellation.Token);
        cancellation.CancelAfter(TimeSpan.FromSeconds(1));

        var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.Same(runTask, completed);

        object? result = null;
        var exception = await Record.ExceptionAsync(async () => result = await runTask.ConfigureAwait(false));

        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.Null(GetProperty<int?>(result, "ExitCode"));
        Assert.False(GetProperty<bool>(result, "TimedOut"));

        var diagnostic = Assert.Single(GetDiagnostics(result));
        Assert.Equal("process", GetProperty<string>(diagnostic, "Command"));
        Assert.Equal("child-external-cancel", GetProperty<string>(diagnostic, "Step"));
        Assert.Equal("error", GetProperty<string>(diagnostic, "Severity"));
        Assert.Equal("E2D-BUILD-PROCESS-CANCELED", GetProperty<string>(diagnostic, "Code"));
        Assert.Null(GetProperty<int?>(diagnostic, "ProcessExitCode"));
        Assert.False(GetProperty<bool>(diagnostic, "TimedOut"));
    }

    private static async Task<object> RunProcessRunnerAsync(
        string stepName,
        string fileName,
        string[] arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var assembly = await BuildAndLoadBuildToolAssemblyAsync();
        return await RunProcessRunnerAsync(assembly, stepName, fileName, arguments, workingDirectory, timeout, cancellationToken);
    }

    private static async Task<object> RunProcessRunnerAsync(
        Assembly assembly,
        string stepName,
        string fileName,
        string[] arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var requestType = assembly.GetType("Electron2D.Build.ProcessRunRequest", throwOnError: true)!;
        var runnerType = assembly.GetType("Electron2D.Build.ProcessRunner", throwOnError: true)!;
        var request = Activator.CreateInstance(
            requestType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [stepName, fileName, arguments, workingDirectory, timeout],
            culture: null)!;
        var runner = Activator.CreateInstance(
            runnerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [],
            culture: null)!;
        var method = runnerType.GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(runnerType.FullName, "RunAsync");
        var task = (Task)method.Invoke(runner, [request, cancellationToken])!;

        await task.ConfigureAwait(false);

        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static async Task<Assembly> BuildAndLoadBuildToolAssemblyAsync()
    {
        var root = FindRepositoryRoot();
        var projectPath = BuildToolProjectPath(root);

        Assert.True(File.Exists(projectPath), $"Build tool project was not found: {projectPath}");

        var build = await RunProcessAsync(
            DotnetExecutable,
            ["build", projectPath],
            root,
            TimeSpan.FromSeconds(60));

        Assert.True(
            build.ExitCode == 0,
            $"Build tool project failed to build with exit code {build.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{build.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{build.Stderr}");

        var assemblyPath = Path.Combine(root, "eng", "Electron2D.Build", "bin", "Debug", "net10.0", "Electron2D.Build.dll");
        Assert.True(File.Exists(assemblyPath), $"Build tool assembly was not found: {assemblyPath}");

        return Assembly.LoadFrom(assemblyPath);
    }

    private static async Task<BuiltChildProject> CreateBuiltChildProjectAsync(string name, string program)
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-RepositoryBuildToolTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectRoot);

        File.WriteAllText(
            Path.Combine(projectRoot, "Child.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(projectRoot, "Program.cs"), program);

        var build = await RunProcessAsync(
            DotnetExecutable,
            ["build", Path.Combine(projectRoot, "Child.csproj")],
            projectRoot,
            TimeSpan.FromSeconds(60));

        Assert.True(
            build.ExitCode == 0,
            $"Child project failed to build with exit code {build.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{build.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{build.Stderr}");

        var assemblyPath = Path.Combine(projectRoot, "bin", "Debug", "net10.0", "Child.dll");
        Assert.True(File.Exists(assemblyPath), $"Child assembly was not found: {assemblyPath}");

        return new BuiltChildProject(projectRoot, assemblyPath);
    }

    private static async Task<CommandResult> RunBuildToolAsync(params string[] arguments)
    {
        return await RunBuildToolFromDirectoryAsync(FindRepositoryRoot(), arguments);
    }

    private static async Task<CommandResult> RunBuildToolFromDirectoryAsync(string workingDirectory, params string[] arguments)
    {
        var root = FindRepositoryRoot();
        return await RunProcessAsync(
            DotnetExecutable,
            ["run", "--project", BuildToolProjectPath(root), "--", .. arguments],
            workingDirectory,
            TimeSpan.FromSeconds(30));
    }

    private static async Task<CommandResult> RunProcessAsync(
        string fileName,
        string[] arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            throw new TimeoutException($"Process did not exit within {timeout}: {fileName} {string.Join(" ", arguments)}");
        }

        return new CommandResult(process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
    }

    private static JsonDocument ReadFirstDiagnostic(CommandResult result)
    {
        var line = result.Stdout
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        Assert.False(
            string.IsNullOrWhiteSpace(line),
            $"Expected a structured diagnostic on stdout.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");

        return JsonDocument.Parse(line);
    }

    private static List<object> GetDiagnostics(object result)
    {
        var diagnostics = Assert.IsAssignableFrom<IEnumerable>(GetRawProperty(result, "Diagnostics"));
        return diagnostics.Cast<object>().ToList();
    }

    private static T? GetProperty<T>(object instance, string name)
    {
        return (T?)GetRawProperty(instance, name);
    }

    private static object? GetRawProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(instance.GetType().FullName, name);
        return property.GetValue(instance);
    }

    private static string BuildToolProjectPath(string root)
    {
        return Path.Combine(root, "eng", "Electron2D.Build", "Electron2D.Build.csproj");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
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

    private static void AssertNoReleaseArtifacts(string root)
    {
        var forbiddenFiles = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(IsReleaseArtifactFile)
            .ToArray();
        var forbiddenDirectories = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(IsReleaseArtifactDirectory)
            .ToArray();

        Assert.Empty(forbiddenFiles);
        Assert.Empty(forbiddenDirectories);
    }

    private static bool IsReleaseArtifactFile(string path)
    {
        var name = Path.GetFileName(path);
        return name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "release-manifest.json", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".sha256sum", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("checksums", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReleaseArtifactDirectory(string path)
    {
        var name = Path.GetFileName(path);
        return string.Equals(name, "artifacts", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "release", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "releases", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "release-output", StringComparison.OrdinalIgnoreCase);
    }

    private const string DotnetExecutable = "dotnet";

    private sealed record CommandResult(int ExitCode, string Stdout, string Stderr);

    private sealed record BuiltChildProject(string Root, string AssemblyPath) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed record TemporaryDirectory(string Root) : IDisposable
    {
        public static TemporaryDirectory Create(string name)
        {
            var root = Path.Combine(Path.GetTempPath(), "Electron2D-RepositoryBuildToolTests", name, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TemporaryDirectory(root);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
