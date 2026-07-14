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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Electron2D.ProjectSystem;

internal sealed class LegacyTaskboardMigrationTask
{
    public required string TaskId { get; set; }

    public required string OriginalTaskId { get; init; }

    public required string Title { get; init; }

    public required string Priority { get; init; }

    public required bool IsCompleted { get; init; }

    public required LegacySourceFragment Fragment { get; init; }

    public List<string> Dependencies { get; } = [];
}

internal sealed class LegacyTaskboardMigrationPlan
{
    public List<LegacyTaskboardMigrationTask> Tasks { get; } = [];

    public List<LegacySourceFragment> BoardFragments { get; } = [];

    public SortedDictionary<string, string> SourceDigests { get; } = new(StringComparer.Ordinal);

    public List<string> Diagnostics { get; } = [];

    public TaskBoard? Board { get; set; }

    public byte[] ReconstructSource(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var fragments = Tasks.Select(task => task.Fragment)
            .Concat(BoardFragments)
            .Where(fragment => string.Equals(fragment.SourcePath, sourcePath, StringComparison.Ordinal))
            .OrderBy(fragment => fragment.ByteOffset)
            .ToArray();
        if (fragments.Length == 0)
        {
            throw new InvalidOperationException($"Legacy source '{sourcePath}' has no migration fragments.");
        }

        using var stream = new MemoryStream();
        long expectedOffset = 0;
        foreach (var fragment in fragments)
        {
            if (fragment.ByteOffset != expectedOffset)
            {
                throw new InvalidOperationException($"Legacy source '{sourcePath}' fragments have a gap or overlap at byte {expectedOffset}.");
            }

            if (fragment.HasBom)
            {
                stream.Write(Encoding.UTF8.GetPreamble());
            }

            stream.Write(Encoding.UTF8.GetBytes(fragment.Markdown));
            expectedOffset += fragment.ByteLength;
        }

        var result = stream.ToArray();
        var actualDigest = HashBytes(result);
        if (!SourceDigests.TryGetValue(sourcePath, out var expectedDigest) ||
            !string.Equals(actualDigest, expectedDigest, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Legacy source '{sourcePath}' reconstruction digest mismatch.");
        }

        return result;
    }

    public IReadOnlyList<ProjectTask> CreateProjectTasks()
    {
        return LegacyTaskboardMigration.CreateProjectTasks(this);
    }

    private static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

}

internal static partial class LegacyTaskboardMigration
{
    private static readonly Regex TaskHeading = new(
        @"^#{1,2}\s+(?<id>T-\d{4})(?:\s+\[(?<marker>[^\]]+)\])?(?:\s+P(?<priority>\d+))?\s*(?::|-|—)\s*(?<title>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RoadmapHeading = new(
        @"^#{1,6}\s+ROADMAP\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex DependencyId = new(
        @"\bT-\d{4}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RoadmapGroupHeading = new(
        @"^###\s+(?<number>\d+(?:\.\d+)*)\.?(?:\s+)(?<title>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RoadmapPlacement = new(
        @"^-\s+`(?<id>T-\d{4})`\s+-\s+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex IsoTimestamp = new(
        @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DatedActivity = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2}))\s+(?:-|—)\s+(?<payload>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BacktickValue = new(
        @"`(?<value>[^`\r\n]+)`",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static LegacyTaskboardMigrationPlan ParseSources(
        IReadOnlyDictionary<string, byte[]> sources,
        string activeSourcePath)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeSourcePath);
        if (!sources.ContainsKey(activeSourcePath))
        {
            throw new InvalidOperationException($"Active task source '{activeSourcePath}' was not provided.");
        }

        var plan = new LegacyTaskboardMigrationPlan();
        foreach (var pair in sources.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            ParseSource(plan, pair.Key, pair.Value, string.Equals(pair.Key, activeSourcePath, StringComparison.Ordinal));
            plan.SourceDigests[pair.Key] = HashBytes(pair.Value);
        }

        ResolveCollisions(plan.Tasks);
        RepairApprovedDependencyEdges(plan);
        plan.Board = BuildBoard(plan, activeSourcePath);
        return plan;
    }

    internal static IReadOnlyList<ProjectTask> CreateProjectTasks(LegacyTaskboardMigrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var result = new List<ProjectTask>(plan.Tasks.Count);
        var parentByChild = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in plan.Tasks)
        {
            foreach (var childId in ReadIdsFromField(source.Fragment.Markdown, "Дочерние задачи", "Подзадачи", "Subtasks"))
            {
                parentByChild.TryAdd(childId, source.TaskId);
            }
        }

        foreach (var source in plan.Tasks)
        {
            var timestamps = IsoTimestamp.Matches(source.Fragment.Markdown)
                .Select(match => DateTimeOffset.TryParse(
                    match.Value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var value)
                    ? value
                    : (DateTimeOffset?)null)
                .Where(value => value is not null)
                .Select(value => value!.Value)
                .OrderBy(value => value)
                .ToArray();
            if (timestamps.Length == 0)
            {
                throw new InvalidOperationException($"Legacy task '{source.TaskId}' contains no ISO timestamp.");
            }

            var status = ReadStatus(source);
            var awaitingAcceptance = IsAwaitingAcceptance(source);
            var createdAt = timestamps[0];
            var updatedAt = timestamps[^1];
            var task = new ProjectTask
            {
                TaskUid = "task-" + HashText($"{source.Fragment.SourcePath}\n{source.Fragment.ByteOffset}\n{source.OriginalTaskId}\n{source.Title}")[..32],
                TaskId = source.TaskId,
                Title = source.Title,
                Description = source.Fragment.Markdown,
                Status = status,
                Priority = source.Priority,
                CreatedBy = "legacy-migration",
                ParentTaskId = parentByChild.GetValueOrDefault(source.OriginalTaskId),
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                AcceptanceState = status switch
                {
                    ProjectTaskStatus.Done => ProjectTaskAcceptanceState.Accepted,
                    ProjectTaskStatus.Cancelled => ProjectTaskAcceptanceState.Cancelled,
                    _ when awaitingAcceptance => ProjectTaskAcceptanceState.Submitted,
                    _ => ProjectTaskAcceptanceState.Open
                },
                SubmittedAt = awaitingAcceptance ? updatedAt : null,
                CompletedAt = status == ProjectTaskStatus.Done ? updatedAt : null,
                AcceptedAt = status == ProjectTaskStatus.Done ? updatedAt : null,
                AcceptedBy = status == ProjectTaskStatus.Done ? "legacy-human" : null,
                ArchivedAt = source.IsCompleted ? updatedAt : null,
                ArchivedBy = source.IsCompleted ? "legacy-migration" : null,
                CancellationReason = status == ProjectTaskStatus.Cancelled ? "Legacy task was cancelled before migration." : null,
                Revision = 1
            };
            if (!string.Equals(source.TaskId, source.OriginalTaskId, StringComparison.Ordinal))
            {
                task.LegacyAliases.Add(source.OriginalTaskId);
            }

            task.Dependencies.AddRange(source.Dependencies);
            task.LegacySourceFragments.Add(source.Fragment);
            PopulateExecutionContract(task, source.Fragment.Markdown);
            PopulateAcceptanceCriteria(task, source.Fragment.Markdown);
            PopulateActivity(task, source.Fragment.Markdown);
            PopulateLinks(task, source.Fragment.Markdown);
            result.Add(task);
        }

        var byId = result.ToDictionary(task => task.TaskId, StringComparer.Ordinal);
        foreach (var pair in parentByChild)
        {
            if (byId.TryGetValue(pair.Value, out var parent))
            {
                parent.Subtasks.Add(pair.Key);
            }
        }

        return result;
    }

    private static void PopulateExecutionContract(ProjectTask task, string markdown)
    {
        var inContract = false;
        List<string>? target = null;
        var externalAudit = new List<string>();
        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                if (line.Equals("### Execution contract", StringComparison.OrdinalIgnoreCase))
                {
                    inContract = true;
                    target = null;
                    continue;
                }

                if (inContract)
                {
                    break;
                }
            }

            if (!inContract)
            {
                continue;
            }

            var trimmed = line.Trim();
            if (trimmed.StartsWith("- Task type:", StringComparison.OrdinalIgnoreCase))
            {
                task.ExecutionContract.TaskType = trimmed["- Task type:".Length..].Trim().Trim('`');
                target = null;
                continue;
            }

            if (TrySelectContractList(trimmed, task.ExecutionContract, out var selected))
            {
                target = selected;
                continue;
            }

            if (trimmed.StartsWith("- External audit:", StringComparison.OrdinalIgnoreCase))
            {
                target = externalAudit;
                var inline = trimmed["- External audit:".Length..].Trim();
                if (inline.Length > 0)
                {
                    externalAudit.Add(inline);
                }

                continue;
            }

            if (target is not null && line.Length > trimmed.Length && trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var value = trimmed[2..].Trim();
                if (value.Length > 0)
                {
                    target.Add(value);
                }
            }
        }

        if (externalAudit.Count > 0)
        {
            task.ExecutionContract.ExternalAudit = string.Join("\n", externalAudit);
        }
    }

    private static bool TrySelectContractList(
        string line,
        TaskExecutionContract contract,
        out List<string>? target)
    {
        target = line.ToLowerInvariant() switch
        {
            "- ready-to-start:" => contract.ReadyToStart,
            "- stop conditions:" => contract.StopConditions,
            "- may change:" => contract.AllowedChanges,
            "- allowed changes:" => contract.AllowedChanges,
            "- must not change:" => contract.ForbiddenChanges,
            "- forbidden changes:" => contract.ForbiddenChanges,
            "- required outputs:" => contract.RequiredOutputs,
            "- required commands:" => contract.RequiredCommands,
            _ => null
        };
        return target is not null;
    }

    private static void PopulateAcceptanceCriteria(ProjectTask task, string markdown)
    {
        var inCriteria = false;
        var index = 0;
        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                var heading = line.TrimStart('#', ' ');
                var isCriteria = heading.Equals("Критерии приёмки", StringComparison.OrdinalIgnoreCase) ||
                    heading.Equals("Критерии и результат", StringComparison.OrdinalIgnoreCase) ||
                    heading.Equals("Критерии", StringComparison.OrdinalIgnoreCase) ||
                    heading.Equals("Выполненные критерии", StringComparison.OrdinalIgnoreCase);
                if (isCriteria)
                {
                    inCriteria = true;
                    continue;
                }

                if (inCriteria)
                {
                    break;
                }
            }

            if (!inCriteria)
            {
                continue;
            }

            var trimmed = line.Trim();
            if (trimmed.Length < 6 || !trimmed.StartsWith("- [", StringComparison.Ordinal) || trimmed[4] != ']')
            {
                continue;
            }

            var description = trimmed[5..].Trim();
            if (description.Length == 0)
            {
                continue;
            }

            var marker = char.ToLowerInvariant(trimmed[3]);
            var state = marker == 'x' ? AcceptanceCriterionState.Passed : marker == '!' ? AcceptanceCriterionState.Failed : AcceptanceCriterionState.Open;
            task.AcceptanceCriteria.Add(new AcceptanceCriterion(
                $"criterion-{++index:D3}",
                description,
                state,
                []));
        }
    }

    private static void PopulateActivity(ProjectTask task, string markdown)
    {
        var index = 0;
        foreach (var rawLine in markdown.Split('\n'))
        {
            var match = DatedActivity.Match(rawLine.Trim().TrimEnd('\r'));
            if (!match.Success || !DateTimeOffset.TryParse(
                match.Groups["timestamp"].Value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var createdAt))
            {
                continue;
            }

            task.Activity.Add(new TaskActivityEntry(
                $"legacy-activity-{++index:D4}",
                "legacy-migration",
                PrincipalKind.ExternalFile,
                createdAt,
                TaskActivityKind.AgentSummary,
                match.Groups["payload"].Value));
        }
    }

    private static void PopulateLinks(ProjectTask task, string markdown)
    {
        foreach (var value in BacktickValue.Matches(markdown)
            .Select(match => match.Groups["value"].Value.Trim())
            .Where(value => value.Contains('/') || value.Contains("://", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal))
        {
            task.LinkedArtifacts.Add(value);
        }
    }

    private static ProjectTaskStatus ReadStatus(LegacyTaskboardMigrationTask source)
    {
        if (source.IsCompleted)
        {
            return source.OriginalTaskId == "T-0963" ? ProjectTaskStatus.Cancelled : ProjectTaskStatus.Done;
        }

        if (IsAwaitingAcceptance(source))
        {
            return ProjectTaskStatus.Review;
        }

        var value = ReadField(source.Fragment.Markdown, "Состояние", "Статус")?.ToLowerInvariant() ?? string.Empty;

        if (value.Contains("in progress", StringComparison.Ordinal))
        {
            return ProjectTaskStatus.InProgress;
        }

        if (value.Contains("blocked", StringComparison.Ordinal))
        {
            return ProjectTaskStatus.Blocked;
        }

        if (value.Contains("review", StringComparison.Ordinal))
        {
            return ProjectTaskStatus.Review;
        }

        if (value.Contains("ready", StringComparison.Ordinal))
        {
            return ProjectTaskStatus.Ready;
        }

        if (value.Contains("cancel", StringComparison.Ordinal) || value.Contains("отмен", StringComparison.Ordinal))
        {
            return ProjectTaskStatus.Cancelled;
        }

        return ProjectTaskStatus.Ready;
    }

    private static bool IsAwaitingAcceptance(LegacyTaskboardMigrationTask source)
    {
        var value = ReadField(source.Fragment.Markdown, "Состояние", "Статус")?.ToLowerInvariant() ?? string.Empty;
        return value.Contains("ready for acceptance", StringComparison.Ordinal) ||
            value.Contains("awaiting", StringComparison.Ordinal);
    }

    private static string? ReadField(string markdown, params string[] aliases)
    {
        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('\r');
            foreach (var alias in aliases)
            {
                var prefix = $"- {alias}:";
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return line[prefix.Length..].Trim().Trim('`');
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ReadIdsFromField(string markdown, params string[] aliases)
    {
        var value = ReadField(markdown, aliases);
        return value is null
            ? []
            : DependencyId.Matches(value).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
    }

    private static void ParseSource(
        LegacyTaskboardMigrationPlan plan,
        string sourcePath,
        byte[] bytes,
        bool activeSource)
    {
        var hasBom = bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble());
        var contentOffset = hasBom ? Encoding.UTF8.GetPreamble().Length : 0;
        var text = new UTF8Encoding(false, true).GetString(bytes, contentOffset, bytes.Length - contentOffset);
        var lines = ReadLines(text, contentOffset);
        var boundaries = new List<LegacyBoundary>();
        char? fenceCharacter = null;
        var fenceLength = 0;
        foreach (var line in lines)
        {
            var fence = ReadFence(line.Text);
            if (fenceCharacter is not null)
            {
                if (fence is not null && fence.Value.Character == fenceCharacter && fence.Value.Length >= fenceLength)
                {
                    fenceCharacter = null;
                    fenceLength = 0;
                }

                continue;
            }

            if (fence is not null)
            {
                fenceCharacter = fence.Value.Character;
                fenceLength = fence.Value.Length;
                continue;
            }

            var taskMatch = TaskHeading.Match(line.Text);
            if (taskMatch.Success)
            {
                boundaries.Add(new LegacyBoundary(
                    line.ByteStart,
                    LegacyBoundaryKind.Task,
                    taskMatch.Groups["id"].Value,
                    taskMatch.Groups["title"].Value,
                    taskMatch.Groups["priority"].Success ? $"P{taskMatch.Groups["priority"].Value}" : "Unspecified"));
            }
            else if (activeSource && RoadmapHeading.IsMatch(line.Text))
            {
                boundaries.Add(new LegacyBoundary(line.ByteStart, LegacyBoundaryKind.Board, string.Empty, string.Empty, string.Empty));
            }
        }

        if (boundaries.Count == 0)
        {
            plan.BoardFragments.Add(CreateFragment(sourcePath, bytes, 0, bytes.Length));
            return;
        }

        if (boundaries[0].ByteStart > 0)
        {
            plan.BoardFragments.Add(CreateFragment(sourcePath, bytes, 0, boundaries[0].ByteStart));
        }

        for (var index = 0; index < boundaries.Count; index++)
        {
            var boundary = boundaries[index];
            var end = index + 1 < boundaries.Count ? boundaries[index + 1].ByteStart : bytes.LongLength;
            var fragment = CreateFragment(sourcePath, bytes, boundary.ByteStart, end);
            if (boundary.Kind == LegacyBoundaryKind.Board)
            {
                plan.BoardFragments.Add(fragment);
                continue;
            }

            var task = new LegacyTaskboardMigrationTask
            {
                TaskId = boundary.TaskId,
                OriginalTaskId = boundary.TaskId,
                Title = boundary.Title,
                Priority = boundary.Priority,
                IsCompleted = !activeSource,
                Fragment = fragment
            };
            task.Dependencies.AddRange(ReadDependencies(fragment.Markdown));
            plan.Tasks.Add(task);
        }
    }

    private static IReadOnlyList<LegacyLine> ReadLines(string text, int initialByteOffset)
    {
        var lines = new List<LegacyLine>();
        var charOffset = 0;
        long byteOffset = initialByteOffset;
        while (charOffset < text.Length)
        {
            var lineEnd = text.IndexOf('\n', charOffset);
            var contentEnd = lineEnd < 0 ? text.Length : lineEnd;
            if (contentEnd > charOffset && text[contentEnd - 1] == '\r')
            {
                contentEnd--;
            }

            var textEnd = lineEnd < 0 ? text.Length : lineEnd + 1;
            var lineText = text[charOffset..contentEnd];
            var consumed = text[charOffset..textEnd];
            var byteLength = Encoding.UTF8.GetByteCount(consumed);
            lines.Add(new LegacyLine(byteOffset, lineText));
            byteOffset += byteLength;
            charOffset = textEnd;
        }

        return lines;
    }

    private static (char Character, int Length)? ReadFence(string line)
    {
        var trimmed = line.Length >= 3 ? line.TrimStart(' ') : line;
        var indent = line.Length - trimmed.Length;
        if (indent > 3 || trimmed.Length < 3 || (trimmed[0] != '`' && trimmed[0] != '~'))
        {
            return null;
        }

        var character = trimmed[0];
        var length = 0;
        while (length < trimmed.Length && trimmed[length] == character)
        {
            length++;
        }

        return length >= 3 ? (character, length) : null;
    }

    private static LegacySourceFragment CreateFragment(
        string sourcePath,
        byte[] source,
        long start,
        long end)
    {
        var length = checked((int)(end - start));
        var slice = source.AsSpan(checked((int)start), length).ToArray();
        var hasBom = start == 0 && slice.AsSpan().StartsWith(Encoding.UTF8.GetPreamble());
        var markdownOffset = hasBom ? Encoding.UTF8.GetPreamble().Length : 0;
        var markdown = new UTF8Encoding(false, true).GetString(slice, markdownOffset, slice.Length - markdownOffset);
        return new LegacySourceFragment
        {
            SourcePath = sourcePath,
            ByteOffset = start,
            ByteLength = length,
            Encoding = "utf-8",
            HasBom = hasBom,
            LineEnding = DetectLineEnding(markdown),
            Sha256 = HashBytes(slice),
            Markdown = markdown
        };
    }

    private static string DetectLineEnding(string text)
    {
        var crlf = text.Contains("\r\n", StringComparison.Ordinal);
        var withoutCrlf = crlf ? text.Replace("\r\n", string.Empty, StringComparison.Ordinal) : text;
        var lf = withoutCrlf.Contains('\n');
        var cr = withoutCrlf.Contains('\r');
        var count = (crlf ? 1 : 0) + (lf ? 1 : 0) + (cr ? 1 : 0);
        return count > 1 ? "mixed" : crlf ? "crlf" : cr ? "cr" : "lf";
    }

    private static IReadOnlyList<string> ReadDependencies(string markdown)
    {
        foreach (var line in markdown.Split('\n'))
        {
            var normalized = line.Trim();
            if (normalized.StartsWith("- Зависимости:", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("- Dependencies:", StringComparison.OrdinalIgnoreCase))
            {
                return DependencyId.Matches(normalized).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
            }
        }

        return [];
    }

    private static void ResolveCollisions(IReadOnlyList<LegacyTaskboardMigrationTask> tasks)
    {
        foreach (var group in tasks.GroupBy(task => task.OriginalTaskId, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            var ordered = group.OrderBy(task => CollisionOrder(task), StringComparer.Ordinal).ToArray();
            for (var index = 1; index < ordered.Length; index++)
            {
                var suffix = CollisionSuffix(ordered[index], index + 1);
                ordered[index].TaskId = $"{ordered[index].OriginalTaskId}-{suffix}";
            }
        }
    }

    private static string CollisionOrder(LegacyTaskboardMigrationTask task)
    {
        if (task.OriginalTaskId == "T-0129" && task.Title.Contains("UI public API", StringComparison.OrdinalIgnoreCase))
        {
            return "0";
        }

        if (task.OriginalTaskId == "T-0228" && task.Title.Contains("audit package", StringComparison.OrdinalIgnoreCase))
        {
            return "0";
        }

        return "1-" + task.Fragment.SourcePath + "-" + task.Fragment.ByteOffset.ToString("D12", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void RepairApprovedDependencyEdges(LegacyTaskboardMigrationPlan plan)
    {
        Remove("T-1015", "T-0980");
        Remove("T-1016", "T-0980");

        void Remove(string taskId, string dependencyId)
        {
            var task = plan.Tasks.SingleOrDefault(task => string.Equals(task.TaskId, taskId, StringComparison.Ordinal));
            if (task is not null && task.Dependencies.Remove(dependencyId))
            {
                plan.Diagnostics.Add($"Removed approved legacy dependency edge {taskId} -> {dependencyId}.");
            }
        }
    }

    private static TaskBoard BuildBoard(LegacyTaskboardMigrationPlan plan, string activeSourcePath)
    {
        var roadmap = plan.BoardFragments.SingleOrDefault(fragment =>
            string.Equals(fragment.SourcePath, activeSourcePath, StringComparison.Ordinal) &&
            RoadmapHeading.IsMatch(fragment.Markdown.Split('\n', 2)[0].TrimEnd('\r')));
        var groups = new List<TaskBoardGroup>();
        var placements = new List<TaskBoardPlacement>();
        var activeTasks = plan.Tasks.Where(task => !task.IsCompleted).ToArray();
        var activeByOriginalId = activeTasks
            .GroupBy(task => task.OriginalTaskId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.FirstOrDefault(task => task.TaskId == task.OriginalTaskId) ?? group.First(),
                StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? currentGroupId = null;
        if (roadmap is not null)
        {
            foreach (var rawLine in roadmap.Markdown.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                var groupMatch = RoadmapGroupHeading.Match(line);
                if (groupMatch.Success)
                {
                    var number = groupMatch.Groups["number"].Value;
                    if (number == "0")
                    {
                        currentGroupId = null;
                        continue;
                    }

                    var kind = number.Contains('.') ? TaskBoardGroupKind.Milestone : TaskBoardGroupKind.Epoch;
                    if (kind == TaskBoardGroupKind.Milestone && !number.StartsWith("2.", StringComparison.Ordinal))
                    {
                        currentGroupId = null;
                        continue;
                    }

                    currentGroupId = kind == TaskBoardGroupKind.Epoch
                        ? $"epoch-{number}"
                        : "milestone-" + number.Replace('.', '-');
                    groups.Add(new TaskBoardGroup(
                        currentGroupId,
                        kind,
                        groupMatch.Groups["title"].Value,
                        string.Empty,
                        kind == TaskBoardGroupKind.Milestone ? "epoch-2" : null,
                        ((groups.Count + 1) * 1000).ToString("D8", System.Globalization.CultureInfo.InvariantCulture)));
                    continue;
                }

                var placementMatch = RoadmapPlacement.Match(line);
                if (!placementMatch.Success)
                {
                    continue;
                }

                var originalId = placementMatch.Groups["id"].Value;
                if (!activeByOriginalId.TryGetValue(originalId, out var task))
                {
                    plan.Diagnostics.Add($"ROADMAP references stale non-active task '{originalId}'.");
                    continue;
                }

                if (!seen.Add(task.TaskId))
                {
                    plan.Diagnostics.Add($"ROADMAP contains duplicate placement for '{task.TaskId}'.");
                    continue;
                }

                placements.Add(new TaskBoardPlacement(
                    task.TaskId,
                    currentGroupId,
                    ((placements.Count + 1) * 1000).ToString("D8", System.Globalization.CultureInfo.InvariantCulture)));
            }
        }

        foreach (var task in activeTasks.Where(task => !seen.Contains(task.TaskId)).OrderBy(task => task.TaskId, StringComparer.Ordinal))
        {
            plan.Diagnostics.Add($"Active task '{task.TaskId}' is absent from ROADMAP and was placed in Ungrouped.");
            placements.Add(new TaskBoardPlacement(
                task.TaskId,
                groupId: null,
                ((placements.Count + 1) * 1000).ToString("D8", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var board = new TaskBoard("main", revision: 1, groups, placements);
        board.IdPolicy.NextNumber = plan.Tasks
            .Select(task => task.OriginalTaskId)
            .Where(id => id.StartsWith("T-", StringComparison.Ordinal))
            .Select(id => int.TryParse(id.AsSpan(2), out var value) ? value : 0)
            .DefaultIfEmpty()
            .Max() + 1L;
        foreach (var pair in plan.SourceDigests)
        {
            board.Migration.SourceDigests[pair.Key] = pair.Value;
        }

        board.Migration.Diagnostics.AddRange(plan.Diagnostics);
        board.Migration.LegacySourceFragments.AddRange(plan.BoardFragments);
        return board;
    }

    private static string CollisionSuffix(LegacyTaskboardMigrationTask task, int fallback)
    {
        if (task.OriginalTaskId == "T-0129" && task.Title.Contains("dev-diary", StringComparison.OrdinalIgnoreCase))
        {
            return "diary";
        }

        if (task.OriginalTaskId == "T-0129")
        {
            return "public-docs";
        }

        if (task.OriginalTaskId == "T-0228" && task.Title.Contains("Conventional Commits", StringComparison.OrdinalIgnoreCase))
        {
            return "commit-policy";
        }

        return $"legacy-{fallback}";
    }

    private static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string HashText(string text)
    {
        return HashBytes(Encoding.UTF8.GetBytes(text));
    }

    private enum LegacyBoundaryKind
    {
        Task,
        Board
    }

    private sealed record LegacyBoundary(
        long ByteStart,
        LegacyBoundaryKind Kind,
        string TaskId,
        string Title,
        string Priority);

    private sealed record LegacyLine(long ByteStart, string Text);
}
