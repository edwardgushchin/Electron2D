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

internal sealed class LineEndingVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private static readonly string[] CrlfWorkingTreeAllowlist =
    [
        "docs/verdicts/",
        ".bat",
        ".cmd"
    ];

    public async Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        var result = await processRunner.RunAsync(
            new ProcessRunRequest(
                "verify line-endings",
                "git",
                ["-c", "core.quotepath=false", "ls-files", "--eol"],
                repositoryRoot,
                TimeSpan.FromSeconds(30)),
            cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify line-endings",
                "error",
                "E2D-BUILD-LINE-ENDINGS-GIT-FAILED",
                $"git ls-files --eol failed: {result.StandardError.Trim()}"));
            return RepositoryBuildExitCodes.Failed;
        }

        var violations = ParseEolRecords(result.StandardOutput)
            .Where(record => IsUnexpectedCrlf(record))
            .Select(record => record.Path)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (violations.Length > 0)
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                "verify line-endings",
                "error",
                "E2D-BUILD-LINE-ENDINGS-CRLF",
                $"Unexpected CRLF working-tree line endings in tracked text files: {string.Join(", ", violations)}."));
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify line-endings",
            "info",
            "E2D-BUILD-LINE-ENDINGS-PASSED",
            "Tracked text files use the repository line-ending policy."));
        return RepositoryBuildExitCodes.Success;
    }

    private static IEnumerable<EolRecord> ParseEolRecords(string output)
    {
        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.TrimEnd('\r');
            var tabIndex = line.IndexOf('\t', StringComparison.Ordinal);
            if (tabIndex < 0)
            {
                continue;
            }

            var metadata = line[..tabIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (metadata.Length < 3 ||
                !metadata[0].StartsWith("i/", StringComparison.Ordinal) ||
                !metadata[1].StartsWith("w/", StringComparison.Ordinal) ||
                !metadata[2].StartsWith("attr/", StringComparison.Ordinal))
            {
                continue;
            }

            var attribute = string.Join(" ", metadata.Skip(2)).Replace("attr/", string.Empty, StringComparison.Ordinal);
            yield return new EolRecord(
                metadata[0][2..],
                metadata[1][2..],
                attribute,
                line[(tabIndex + 1)..].Replace('\\', '/'));
        }
    }

    private static bool IsUnexpectedCrlf(EolRecord record)
    {
        return string.Equals(record.WorkingTree, "crlf", StringComparison.Ordinal) &&
            !IsBinary(record) &&
            !IsAllowedCrlfPath(record.Path);
    }

    private static bool IsBinary(EolRecord record)
    {
        return string.Equals(record.Index, "-text", StringComparison.Ordinal) ||
            string.Equals(record.WorkingTree, "-text", StringComparison.Ordinal) ||
            record.Attribute.Contains("-text", StringComparison.Ordinal) ||
            record.Attribute.Contains("binary", StringComparison.Ordinal);
    }

    private static bool IsAllowedCrlfPath(string path)
    {
        return CrlfWorkingTreeAllowlist.Any(pattern =>
            pattern.EndsWith("/", StringComparison.Ordinal)
                ? path.StartsWith(pattern, StringComparison.Ordinal)
                : path.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record EolRecord(string Index, string WorkingTree, string Attribute, string Path);
}
