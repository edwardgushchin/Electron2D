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
namespace Electron2D.Build;

internal sealed class TestCommand(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const int DefaultTimeoutSeconds = 3600;
    private const string BaselineFilter = "Category!=Baseline";

    private static readonly string[] TestProjects =
    [
        "tests/Electron2D.Tests.Unit/Electron2D.Tests.Unit.csproj",
        "tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj",
        "tests/Electron2D.Tests.RuntimeSmoke/Electron2D.Tests.RuntimeSmoke.csproj",
        "tests/Electron2D.Tests.GoldenData/Electron2D.Tests.GoldenData.csproj"
    ];

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var options = ParseOptions(args);
        if (options is null)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "test",
            "test",
            "info",
            "E2D-BUILD-TEST-STARTED",
            $"Running {TestProjects.Length} test projects.",
            TimeoutSeconds: options.TimeoutSeconds));

        foreach (var project in TestProjects)
        {
            var step = $"test {project}";
            var arguments = CreateDotnetTestArguments(project, options.IncludeBaseline);
            diagnostics.Write(new BuildDiagnostic(
                "test",
                step,
                "info",
                "E2D-BUILD-TEST-PROJECT-STARTED",
                $"Running dotnet test for '{project}'.",
                ProjectPath: project,
                TimeoutSeconds: options.TimeoutSeconds));

            var result = await processRunner.RunAsync(
                new ProcessRunRequest(
                    step,
                    "dotnet",
                    arguments,
                    repositoryRoot,
                    TimeSpan.FromSeconds(options.TimeoutSeconds)),
                cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in result.Diagnostics)
            {
                diagnostics.Write(diagnostic);
            }

            if (result.TimedOut)
            {
                diagnostics.Write(new BuildDiagnostic(
                    "test",
                    step,
                    "error",
                    "E2D-BUILD-TEST-PROJECT-TIMEOUT",
                    $"dotnet test timed out for '{project}' after {options.TimeoutSeconds} seconds.",
                    TimedOut: true,
                    ProjectPath: project,
                    TimeoutSeconds: options.TimeoutSeconds));
                return RepositoryBuildExitCodes.Failed;
            }

            if (result.ExitCode != 0)
            {
                diagnostics.Write(new BuildDiagnostic(
                    "test",
                    step,
                    "error",
                    "E2D-BUILD-TEST-PROJECT-FAILED",
                    $"dotnet test failed for '{project}' with exit code {result.ExitCode}.",
                    ProcessExitCode: result.ExitCode,
                    TimedOut: false,
                    ProjectPath: project,
                    TimeoutSeconds: options.TimeoutSeconds));
                return result.ExitCode ?? RepositoryBuildExitCodes.Failed;
            }

            diagnostics.Write(new BuildDiagnostic(
                "test",
                step,
                "info",
                "E2D-BUILD-TEST-PROJECT-PASSED",
                $"dotnet test passed for '{project}'.",
                ProcessExitCode: 0,
                TimedOut: false,
                ProjectPath: project,
                TimeoutSeconds: options.TimeoutSeconds));
        }

        diagnostics.Write(new BuildDiagnostic(
            "test",
            "test",
            "info",
            "E2D-BUILD-TEST-PASSED",
            "All configured test projects passed.",
            TimeoutSeconds: options.TimeoutSeconds));
        return RepositoryBuildExitCodes.Success;
    }

    private TestCommandOptions? ParseOptions(string[] args)
    {
        var includeBaseline = false;
        var timeoutSeconds = DefaultTimeoutSeconds;

        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument == "--include-baseline")
            {
                includeBaseline = true;
                continue;
            }

            if (argument == "--timeout-seconds")
            {
                if (index + 1 >= args.Length ||
                    !int.TryParse(args[index + 1], out timeoutSeconds) ||
                    timeoutSeconds <= 0)
                {
                    WriteInvalidArguments();
                    return null;
                }

                index++;
                continue;
            }

            WriteInvalidArguments();
            return null;
        }

        return new TestCommandOptions(includeBaseline, timeoutSeconds);
    }

    private void WriteInvalidArguments()
    {
        diagnostics.Write(new BuildDiagnostic(
            "test",
            "test",
            "error",
            "E2D-BUILD-CLI-INVALID-ARGUMENTS",
            "Expected: test [--include-baseline] [--timeout-seconds <n>]."));
    }

    private static string[] CreateDotnetTestArguments(string project, bool includeBaseline)
    {
        return includeBaseline
            ? ["test", project]
            : ["test", project, "--filter", BaselineFilter];
    }

    private sealed record TestCommandOptions(bool IncludeBaseline, int TimeoutSeconds);
}
