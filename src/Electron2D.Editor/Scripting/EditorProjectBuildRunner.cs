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
using System.Text.RegularExpressions;

namespace Electron2D.Editor.Scripting;

internal sealed partial class EditorProjectBuildRunner
{
    public EditorProjectBuildResult Build(string projectPath)
    {
        var projectFile = ResolveProjectFile(projectPath);
        var result = RunDotNet(
            Path.GetDirectoryName(projectFile)!,
            ["build", projectFile, "--nologo", "/clp:NoSummary"]);
        return new EditorProjectBuildResult(
            result.ExitCode,
            result.Output,
            ParseDiagnostics(result.Output));
    }

    public EditorProjectRunResult RunAfterBuild(string projectPath)
    {
        var projectFile = ResolveProjectFile(projectPath);
        var result = RunDotNet(
            Path.GetDirectoryName(projectFile)!,
            ["run", "--project", projectFile, "--no-build"]);
        return new EditorProjectRunResult(result.ExitCode, result.Output);
    }

    private static ProcessResult RunDotNet(string workingDirectory, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(
            process.ExitCode,
            string.Concat(output, error));
    }

    private static IReadOnlyList<EditorProjectDiagnostic> ParseDiagnostics(string output)
    {
        var result = new List<EditorProjectDiagnostic>();
        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = DiagnosticLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var severity = match.Groups["severity"].Value.Equals("error", StringComparison.OrdinalIgnoreCase)
                ? EditorProjectDiagnosticSeverity.Error
                : EditorProjectDiagnosticSeverity.Warning;
            result.Add(new EditorProjectDiagnostic(
                severity,
                match.Groups["code"].Value,
                match.Groups["file"].Value,
                int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture),
                match.Groups["message"].Value.Trim()));
        }

        return result;
    }

    private static string ResolveProjectFile(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        var fullPath = Path.GetFullPath(projectPath);
        if (File.Exists(fullPath) && Path.GetExtension(fullPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return fullPath;
        }

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Project directory was not found: {fullPath}");
        }

        var projects = Directory.EnumerateFiles(fullPath, "*.csproj", SearchOption.TopDirectoryOnly).ToArray();
        return projects.Length == 1
            ? projects[0]
            : throw new InvalidOperationException($"Expected exactly one C# project file in {fullPath}.");
    }

    [GeneratedRegex("^(?<file>.*\\.cs)\\((?<line>\\d+),(?<column>\\d+)\\): (?<severity>error|warning) (?<code>[A-Z]+\\d+): (?<message>.*?)(?: \\[.*\\])?$")]
    private static partial Regex DiagnosticLineRegex();

    private sealed record ProcessResult(int ExitCode, string Output);
}
