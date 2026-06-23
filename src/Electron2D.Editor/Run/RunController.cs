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
using System.Xml.Linq;

using Electron2D.Editor.Scripting;

namespace Electron2D.Editor.Run;

internal sealed class RunController
{
    public const string CurrentSceneEnvironmentVariable = "ELECTRON2D_CURRENT_SCENE";

    private readonly ProjectBuildRunner buildRunner = new();

    public OutputConsole OutputConsole { get; } = new();

    public FrameTiming FrameTiming { get; } = new();

    public RunDiagnosticStore DiagnosticStore { get; } = new();

    public RunSession? ActiveSession { get; private set; }

    public RunStartResult StartProject(
        string projectPath,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        return Start(RunTarget.Project, projectPath, scenePath: null, environment);
    }

    public RunStartResult StartCurrentScene(
        string projectPath,
        string scenePath,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        return Start(RunTarget.CurrentScene, projectPath, scenePath, environment);
    }

    public async Task<bool> StopActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveSession is null)
        {
            return false;
        }

        var session = ActiveSession;
        var stopped = await session.StopAsync(cancellationToken).ConfigureAwait(false);
        if (!session.IsRunning)
        {
            ActiveSession = null;
        }

        return stopped;
    }

    public async Task<int> WaitForActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveSession is null)
        {
            throw new InvalidOperationException("There is no active run session.");
        }

        var session = ActiveSession;
        var exitCode = await session.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        ActiveSession = null;
        return exitCode;
    }

    public IReadOnlyList<RunDiagnostic> LoadShaderDiagnostics(string projectPath)
    {
        return DiagnosticStore.LoadShaderDiagnostics(projectPath);
    }

    private RunStartResult Start(
        RunTarget target,
        string projectPath,
        string? scenePath,
        IReadOnlyDictionary<string, string>? environment)
    {
        if (ActiveSession is { IsRunning: true })
        {
            throw new InvalidOperationException("A run session is already active.");
        }

        var projectFile = ProjectBuildRunner.ResolveProjectFile(projectPath);
        var projectRoot = Path.GetDirectoryName(projectFile)!;
        var build = buildRunner.Build(projectFile);
        var compilerDiagnostics = build.Diagnostics.Select(RunDiagnostic.FromCompiler).ToArray();
        DiagnosticStore.AddRange(compilerDiagnostics);
        if (!build.Succeeded)
        {
            return new RunStartResult(
                target,
                buildSucceeded: false,
                processStarted: false,
                session: null,
                compilerDiagnostics);
        }

        var targetPath = ResolveProjectTargetPath(projectFile);
        var startInfo = CreateStartInfo(projectRoot, targetPath, environment);
        if (target == RunTarget.CurrentScene)
        {
            startInfo.Environment[CurrentSceneEnvironmentVariable] = ToProjectRelativeScenePath(projectRoot, scenePath);
        }

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start project run process.");
        var session = new RunSession(process, target, OutputConsole);
        ActiveSession = session;
        return new RunStartResult(
            target,
            buildSucceeded: true,
            processStarted: true,
            session,
            Array.Empty<RunDiagnostic>());
    }

    private static ProcessStartInfo CreateStartInfo(
        string projectRoot,
        string targetPath,
        IReadOnlyDictionary<string, string>? environment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(targetPath);
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        return startInfo;
    }

    private static string ResolveProjectTargetPath(string projectFile)
    {
        var projectRoot = Path.GetDirectoryName(projectFile)!;
        var document = XDocument.Load(projectFile);
        var properties = document.Root?.Elements("PropertyGroup") ?? [];
        var targetFramework = GetProperty(properties, "TargetFramework");
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            targetFramework = GetProperty(properties, "TargetFrameworks")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            throw new InvalidOperationException($"Project target framework was not found: {projectFile}");
        }

        var assemblyName = GetProperty(properties, "AssemblyName");
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assemblyName = Path.GetFileNameWithoutExtension(projectFile);
        }

        var outputPath = GetProperty(properties, "OutputPath");
        var outputDirectory = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(projectRoot, "bin", "Debug", targetFramework)
            : Path.GetFullPath(Path.Combine(projectRoot, outputPath));
        var targetPath = Path.Combine(outputDirectory, assemblyName + ".dll");
        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException($"Built project assembly was not found: {targetPath}", targetPath);
        }

        return targetPath;
    }

    private static string GetProperty(IEnumerable<XElement> propertyGroups, string name)
    {
        return propertyGroups
            .Elements(name)
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static string ToProjectRelativeScenePath(string projectRoot, string? scenePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        var fullScenePath = Path.GetFullPath(Path.IsPathRooted(scenePath)
            ? scenePath
            : Path.Combine(fullProjectRoot, scenePath));
        if (!IsSameOrChildOf(fullProjectRoot, fullScenePath))
        {
            throw new ArgumentException("Current scene path must stay inside the project directory.", nameof(scenePath));
        }

        if (!File.Exists(fullScenePath))
        {
            throw new FileNotFoundException($"Current scene file was not found: {fullScenePath}", fullScenePath);
        }

        return Path.GetRelativePath(fullProjectRoot, fullScenePath).Replace('\\', '/');
    }

    private static bool IsSameOrChildOf(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedCandidate = Path.GetFullPath(candidate);
        return normalizedCandidate.Equals(normalizedRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
            normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }
}
