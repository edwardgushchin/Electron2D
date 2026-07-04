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

using System.Text;
using System.Text.RegularExpressions;

namespace Electron2D.Build;

internal sealed class AuditFollowupVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const string Step = "verify audit-followups";
    private static readonly string[] ValidClosureStates =
    [
        "tracked-existing",
        "tracked-new",
        "accepted-risk",
        "duplicate",
        "not-actionable",
        "promoted-blocker"
    ];

    public async Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        var tasksPath = Path.Combine(repositoryRoot, "TASKS.md");
        if (!File.Exists(tasksPath))
        {
            diagnostics.Write(Error(
                "E2D-BUILD-AUDIT-FOLLOWUP-TASKS-MISSING",
                "TASKS.md was not found; audit follow-up closure notes cannot be verified.",
                "TASKS.md"));
            return RepositoryBuildExitCodes.Failed;
        }

        var closures = ReadClosureNotes(tasksPath);
        var reportPaths = await GetAuditReportPathsAsync(cancellationToken).ConfigureAwait(false);
        var findings = new List<AuditFollowupFinding>();

        foreach (var reportPath in reportPaths)
        {
            var fullPath = Path.Combine(repositoryRoot, reportPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var report = File.ReadAllText(fullPath, Encoding.UTF8);
            var verdict = FirstNonEmptyLine(report);
            if (!string.Equals(verdict, "VERDICT: ACCEPT", StringComparison.Ordinal) &&
                !string.Equals(verdict, "VERDICT: NEEDS_FIXES", StringComparison.Ordinal))
            {
                continue;
            }

            findings.AddRange(AuditFollowupReportParser.ExtractActionableFindings(reportPath, report));
        }

        var errors = new List<BuildDiagnostic>();
        foreach (var finding in findings)
        {
            var matchingClosures = closures
                .Where(closure => ClosureMatchesFinding(closure, finding))
                .ToArray();

            if (matchingClosures.Length == 0)
            {
                errors.Add(Error(
                    "E2D-BUILD-AUDIT-FOLLOWUP-UNCLOSED",
                    $"Actionable {finding.Kind} {finding.FindingId} from {finding.ReportPath} has no closure note in TASKS.md or completed task archives.",
                    finding.ReportPath,
                    finding.LineNumber));
                continue;
            }

            foreach (var closure in matchingClosures)
            {
                ValidateClosure(finding, closure, errors);
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            Step,
            "info",
            "E2D-BUILD-AUDIT-FOLLOWUPS-PASSED",
            $"Audit follow-up closure verification passed for {findings.Count} actionable findings across {reportPaths.Count} saved audit reports."));
        return RepositoryBuildExitCodes.Success;
    }

    private async Task<IReadOnlyList<string>> GetAuditReportPathsAsync(CancellationToken cancellationToken)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var hasGit = Directory.Exists(Path.Combine(repositoryRoot, ".git")) ||
            File.Exists(Path.Combine(repositoryRoot, ".git"));
        if (hasGit)
        {
            var result = await processRunner.RunAsync(
                new ProcessRunRequest(
                    "verify audit-followups git ls-files",
                    "git",
                    ["ls-files", "--", "TASKS.md", "docs/verdicts"],
                    repositoryRoot,
                    TimeSpan.FromSeconds(30)),
                cancellationToken).ConfigureAwait(false);

            if (result.ExitCode == 0)
            {
                foreach (var path in result.StandardOutput
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(path => path.Replace('\\', '/'))
                    .Where(IsAuditReportPath))
                {
                    paths.Add(path);
                }
            }
        }

        var verdictsRoot = Path.Combine(repositoryRoot, "docs", "verdicts");
        if (Directory.Exists(verdictsRoot))
        {
            foreach (var path in Directory
                .EnumerateFiles(verdictsRoot, "*.md", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
                .Where(IsAuditReportPath))
            {
                paths.Add(path);
            }
        }

        return paths
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<AuditFollowupClosureNote> ReadClosureNotes(string tasksPath)
    {
        var sources = new List<(string RelativePath, string Text)>
        {
            ("TASKS.md", File.ReadAllText(tasksPath, Encoding.UTF8))
        };

        foreach (var archivePath in EnumerateCompletedTaskArchivePaths())
        {
            var fullPath = Path.Combine(repositoryRoot, archivePath.Replace('/', Path.DirectorySeparatorChar));
            sources.Add((archivePath, File.ReadAllText(fullPath, Encoding.UTF8)));
        }

        return sources
            .SelectMany(source => AuditFollowupClosureParser.ExtractClosures(source.Text, source.RelativePath))
            .ToArray();
    }

    private IReadOnlyList<string> EnumerateCompletedTaskArchivePaths()
    {
        return new[]
            {
                Path.Combine(repositoryRoot, "completed-tasks"),
                Path.Combine(repositoryRoot, "data", "completed-tasks")
            }
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsAuditReportPath(string path)
    {
        return path.StartsWith("docs/verdicts/", StringComparison.Ordinal) &&
            path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateClosure(
        AuditFollowupFinding finding,
        AuditFollowupClosureNote closure,
        List<BuildDiagnostic> errors)
    {
        if (string.IsNullOrWhiteSpace(closure.State) ||
            !ValidClosureStates.Contains(closure.State, StringComparer.Ordinal))
        {
            errors.Add(Error(
                "E2D-BUILD-AUDIT-FOLLOWUP-CLOSURE-INVALID",
                $"Closure note for {finding.ReportPath} {finding.FindingId} has invalid state '{closure.State}'.",
                closure.SourcePath,
                closure.LineNumber));
            return;
        }

        if (string.IsNullOrWhiteSpace(closure.Target) || string.IsNullOrWhiteSpace(closure.Rationale))
        {
            errors.Add(Error(
                "E2D-BUILD-AUDIT-FOLLOWUP-CLOSURE-INVALID",
                $"Closure note for {finding.ReportPath} {finding.FindingId} must include target and rationale.",
                closure.SourcePath,
                closure.LineNumber));
            return;
        }

        if (string.Equals(closure.State, "accepted-risk", StringComparison.Ordinal) &&
            !closure.HasAcceptedRiskFields)
        {
            errors.Add(Error(
                "E2D-BUILD-AUDIT-FOLLOWUP-ACCEPTED-RISK",
                $"Accepted-risk closure for {finding.ReportPath} {finding.FindingId} must include affected area, impact, likelihood, mitigation, owner/next decision point and decision state.",
                closure.SourcePath,
                closure.LineNumber));
        }
    }

    private static bool ClosureMatchesFinding(AuditFollowupClosureNote closure, AuditFollowupFinding finding)
    {
        if (!string.Equals(NormalizePath(closure.Source), NormalizePath(finding.ReportPath), StringComparison.Ordinal))
        {
            return false;
        }

        return Regex.IsMatch(
            closure.Id,
            $@"(?<![A-Z0-9]){Regex.Escape(finding.FindingId)}(?![A-Z0-9])",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static string NormalizePath(string value)
    {
        return CleanFieldValue(value).Replace('\\', '/');
    }

    private static string FirstNonEmptyLine(string text)
    {
        return NormalizeNewlines(text)
            .Split('\n', StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)) ?? string.Empty;
    }

    private static string NormalizeNewlines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static string CleanFieldValue(string value)
    {
        return value.Trim().Trim('`', '"', '\'');
    }

    private static BuildDiagnostic Error(string code, string message, string path, int? lineNumber = null)
    {
        return new BuildDiagnostic("verify", Step, "error", code, message, Path: path, LineNumber: lineNumber);
    }
}

internal static class AuditFollowupReportParser
{
    private static readonly Regex StructuredRiskEntryPattern = new(
        @"^\s*(?:[-*]\s*)?(?:[*_]{1,3})?(?<kind>FOLLOW_UP_FINDING|OUT_OF_SCOPE_NOTE|INFO_NOTE)\s+(?<id>[A-Z]\d+)\b(?:[*_]{1,3})?",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ExplicitActionablePattern = new(
        @"(?im)^\s*(?:[-*]\s*)?Actionable\s*:\s*(?:true|yes)\s*$|\[actionable\]",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static IReadOnlyList<AuditFollowupFinding> ExtractActionableFindings(string reportPath, string report)
    {
        var lines = NormalizeNewlines(report).Split('\n');
        var section = ExtractRisksAndNotesSection(lines);
        var findings = new List<AuditFollowupFinding>();

        for (var i = 0; i < section.Count; i++)
        {
            var match = StructuredRiskEntryPattern.Match(section[i].Text);
            if (!match.Success)
            {
                continue;
            }

            var kind = match.Groups["kind"].Value.ToUpperInvariant();
            var id = match.Groups["id"].Value.ToUpperInvariant();
            var block = ExtractEntryBlock(section, i);
            var actionable = string.Equals(kind, "FOLLOW_UP_FINDING", StringComparison.Ordinal) ||
                ExplicitActionablePattern.IsMatch(block);
            if (!actionable)
            {
                continue;
            }

            findings.Add(new AuditFollowupFinding(
                NormalizePath(reportPath),
                id,
                kind,
                section[i].LineNumber,
                block));
        }

        return findings;
    }

    private static IReadOnlyList<AuditReportLine> ExtractRisksAndNotesSection(string[] lines)
    {
        var start = Array.FindIndex(lines, line => string.Equals(line.Trim(), "RISKS_AND_NOTES:", StringComparison.Ordinal));
        if (start < 0)
        {
            return [];
        }

        var end = Array.FindIndex(lines, start + 1, line => string.Equals(line.Trim(), "CLOSURE_DECISION:", StringComparison.Ordinal));
        if (end < 0)
        {
            end = lines.Length;
        }

        var result = new List<AuditReportLine>();
        for (var index = start + 1; index < end; index++)
        {
            result.Add(new AuditReportLine(lines[index], index + 1));
        }

        return result;
    }

    private static string ExtractEntryBlock(IReadOnlyList<AuditReportLine> section, int start)
    {
        var end = section.Count;
        for (var index = start + 1; index < section.Count; index++)
        {
            if (StructuredRiskEntryPattern.IsMatch(section[index].Text))
            {
                end = index;
                break;
            }
        }

        return string.Join('\n', section.Skip(start).Take(end - start).Select(line => line.Text));
    }

    private static string NormalizePath(string value)
    {
        return value.Trim().Trim('`', '"', '\'').Replace('\\', '/');
    }

    private static string NormalizeNewlines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}

internal static class AuditFollowupClosureParser
{
    private static readonly Regex ClosureHeaderPattern = new(
        @"^\s*(?:[-*]\s*)?audit-followup-closure\s*:?\s*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ReportPathPattern = new(
        @"docs/verdicts/[^\s`'""]+\.md",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static IReadOnlyList<AuditFollowupClosureNote> ExtractClosures(string tasksText, string sourcePath = "TASKS.md")
    {
        var lines = NormalizeNewlines(tasksText).Split('\n');
        var closures = new List<AuditFollowupClosureNote>();

        for (var index = 0; index < lines.Length; index++)
        {
            if (!ClosureHeaderPattern.IsMatch(lines[index]))
            {
                continue;
            }

            var end = index + 1;
            while (end < lines.Length &&
                !string.IsNullOrWhiteSpace(lines[end]) &&
                !ClosureHeaderPattern.IsMatch(lines[end]) &&
                !lines[end].StartsWith("## ", StringComparison.Ordinal))
            {
                end++;
            }

            var block = string.Join('\n', lines.Skip(index).Take(end - index));
            closures.Add(new AuditFollowupClosureNote(
                sourcePath,
                ExtractSource(block),
                ExtractField(block, "id", "finding id"),
                ExtractField(block, "state", "closure state").ToLowerInvariant(),
                ExtractField(block, "target"),
                ExtractField(block, "rationale"),
                block,
                index + 1,
                HasAcceptedRiskFields(block)));
        }

        return closures;
    }

    private static string ExtractSource(string block)
    {
        var source = ExtractField(block, "source", "report", "report path");
        if (!string.IsNullOrWhiteSpace(source))
        {
            return source;
        }

        var match = ReportPathPattern.Match(block);
        return match.Success ? match.Value : string.Empty;
    }

    private static bool HasAcceptedRiskFields(string block)
    {
        return !string.IsNullOrWhiteSpace(ExtractField(block, "affected area")) &&
            !string.IsNullOrWhiteSpace(ExtractField(block, "impact")) &&
            !string.IsNullOrWhiteSpace(ExtractField(block, "likelihood")) &&
            !string.IsNullOrWhiteSpace(ExtractField(block, "mitigation")) &&
            !string.IsNullOrWhiteSpace(ExtractField(block, "owner/next decision point")) &&
            !string.IsNullOrWhiteSpace(ExtractField(block, "decision state"));
    }

    private static string ExtractField(string block, params string[] names)
    {
        foreach (var name in names)
        {
            var pattern = $@"(?im)(?:^|[;|])\s*(?:[-*]\s*)?{Regex.Escape(name)}\s*:\s*(?<value>[^\r\n;]+)";
            var match = Regex.Match(block, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return CleanFieldValue(match.Groups["value"].Value);
            }
        }

        return string.Empty;
    }

    private static string CleanFieldValue(string value)
    {
        return value.Trim().Trim('`', '"', '\'');
    }

    private static string NormalizeNewlines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}

internal sealed record AuditFollowupFinding(
    string ReportPath,
    string FindingId,
    string Kind,
    int LineNumber,
    string Block);

internal sealed record AuditFollowupClosureNote(
    string SourcePath,
    string Source,
    string Id,
    string State,
    string Target,
    string Rationale,
    string Block,
    int LineNumber,
    bool HasAcceptedRiskFields);

internal sealed record AuditReportLine(string Text, int LineNumber);
