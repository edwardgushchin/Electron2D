/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <edwardgushchin@yandex.ru>
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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Electron2D.ProjectSystem;

internal sealed record WorkspaceFileStateV3(string Path, string Sha256, string Kind);

internal static class WorkspaceChangesBuilderV3
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".taskboard", ".temp", ".vs", "bin", "obj", "node_modules"
    };

    public static IReadOnlyDictionary<string, WorkspaceFileStateV3> CaptureManifest(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var root = Path.GetFullPath(projectRoot);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        EnsureNotReparse(root, root);

        var result = new SortedDictionary<string, WorkspaceFileStateV3>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var childDirectory in Directory.EnumerateDirectories(directory).OrderBy(value => value, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(childDirectory);
                if (ExcludedDirectoryNames.Contains(name)) continue;
                EnsureContained(root, childDirectory);
                EnsureNotReparse(root, childDirectory);
                pending.Push(childDirectory);
            }

            foreach (var file in Directory.EnumerateFiles(directory).OrderBy(value => value, StringComparer.Ordinal))
            {
                EnsureContained(root, file);
                EnsureNotReparse(root, file);
                var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
                ValidateProjectRelativePath(relativePath);
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                var sha256 = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
                result.Add(relativePath, new WorkspaceFileStateV3(relativePath, sha256, "File"));
            }
        }

        return result;
    }

    public static void EnsureBaseline(string projectRoot, JsonObject task, string actorId, string actorKind, DateTimeOffset now)
    {
        var workspaceChanges = task["workspaceChanges"] as JsonObject ?? throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-WORKSPACE-SHAPE", "Task workspaceChanges is missing.");
        if (workspaceChanges["baseRevision"] is not null) return;
        var manifest = CaptureManifest(projectRoot);
        var digest = ComputeManifestDigest(manifest);
        SaveBaseline(projectRoot, task["taskUid"]!.GetValue<string>(), manifest, digest);
        ApplySnapshot(task, new JsonObject
        {
            ["baseRevision"] = "workspace:" + digest,
            ["currentRevision"] = "workspace:" + digest,
            ["files"] = new JsonArray()
        }, actorId, actorKind, now);
    }

    public static void Refresh(string projectRoot, JsonObject task, string actorId, string actorKind, DateTimeOffset now)
    {
        EnsureBaseline(projectRoot, task, actorId, actorKind, now);
        var workspaceChanges = (JsonObject)task["workspaceChanges"]!;
        var baseRevision = workspaceChanges["baseRevision"]!.GetValue<string>();
        var baseline = LoadBaseline(projectRoot, task["taskUid"]!.GetValue<string>(), baseRevision);
        var current = CaptureManifest(projectRoot);
        var next = Build(task, baseline, current, baseRevision, now);
        ApplySnapshot(task, next, actorId, actorKind, now);
        ValidateCurrentWorkspace(projectRoot, next);
    }

    public static void ApplySnapshot(JsonObject task, JsonObject snapshot, string actorId, string actorKind, DateTimeOffset now)
    {
        var previous = task["workspaceChanges"] as JsonObject ?? throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-WORKSPACE-SHAPE", "Task workspaceChanges is missing.");
        if (JsonNode.DeepEquals(previous, snapshot)) return;
        var beforeDigest = ComputeSnapshotDigest(previous);
        var afterDigest = ComputeSnapshotDigest(snapshot);
        task["workspaceChanges"] = snapshot.DeepClone();
        var sequence = TaskActivitySequenceV3.Next(task);
        TaskActivitySequenceV3.Append(task, new JsonObject
        {
            ["activityEntryId"] = $"activity-{Guid.NewGuid():N}",
            ["sequence"] = sequence,
            ["actorId"] = actorId,
            ["actorKind"] = actorKind,
            ["createdAt"] = now.ToString("O"),
            ["kind"] = "WorkspaceChangesUpdated",
            ["payload"] = new JsonObject
            {
                ["beforeDigest"] = beforeDigest,
                ["afterDigest"] = afterDigest,
                ["workspaceChanges"] = snapshot.DeepClone()
            }
        });
    }

    public static string ComputeManifestDigest(IReadOnlyDictionary<string, WorkspaceFileStateV3> manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return AgentContextBuilderV3.HashCanonical(new JsonArray(manifest.Values
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .Select(file => (JsonNode)new JsonObject
            {
                ["path"] = file.Path,
                ["sha256"] = file.Sha256,
                ["kind"] = file.Kind
            }).ToArray()));
    }

    public static string ComputeSnapshotDigest(JsonObject workspaceChanges)
    {
        ArgumentNullException.ThrowIfNull(workspaceChanges);
        return AgentContextBuilderV3.HashCanonical(workspaceChanges);
    }

    public static JsonObject Build(
        JsonObject task,
        IReadOnlyDictionary<string, WorkspaceFileStateV3> baseline,
        IReadOnlyDictionary<string, WorkspaceFileStateV3> current,
        string baseRevision,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseRevision);

        var timestamp = now.ToString("O");
        var previousRows = (task["workspaceChanges"]?["files"] as JsonArray ?? new JsonArray())
            .OfType<JsonObject>()
            .Where(row => row["path"] is not null)
            .ToDictionary(row => row["path"]!.GetValue<string>(), row => row, StringComparer.Ordinal);
        var agentRunIds = CollectAgentRunIds(task);
        var rows = new List<JsonObject>();
        var added = current.Keys.Except(baseline.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var deleted = baseline.Keys.Except(current.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        foreach (var path in baseline.Keys.Intersect(current.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
        {
            var before = baseline[path];
            var after = current[path];
            if (before.Kind == after.Kind && before.Sha256 == after.Sha256) continue;
            rows.Add(CreateRow(
                path,
                before.Kind == after.Kind ? "Modified" : "TypeChanged",
                previousPath: null,
                before.Sha256,
                after.Sha256,
                FirstChangedAt(previousRows, path, timestamp),
                timestamp,
                agentRunIds));
        }

        foreach (var path in added.OrderBy(value => value, StringComparer.Ordinal).ToArray())
        {
            var after = current[path];
            var renameSource = deleted.OrderBy(value => value, StringComparer.Ordinal)
                .FirstOrDefault(candidate => baseline[candidate].Kind == after.Kind && baseline[candidate].Sha256 == after.Sha256);
            if (renameSource is not null)
            {
                deleted.Remove(renameSource);
                rows.Add(CreateRow(path, "Renamed", renameSource, baseline[renameSource].Sha256, after.Sha256,
                    FirstChangedAt(previousRows, path, timestamp), timestamp, agentRunIds));
                continue;
            }

            var copySource = baseline.Keys.Intersect(current.Keys, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                .FirstOrDefault(candidate => baseline[candidate].Kind == after.Kind && baseline[candidate].Sha256 == after.Sha256);
            rows.Add(copySource is null
                ? CreateRow(path, "Added", null, null, after.Sha256, FirstChangedAt(previousRows, path, timestamp), timestamp, agentRunIds)
                : CreateRow(path, "Copied", copySource, baseline[copySource].Sha256, after.Sha256, FirstChangedAt(previousRows, path, timestamp), timestamp, agentRunIds));
        }

        foreach (var path in deleted.OrderBy(value => value, StringComparer.Ordinal))
        {
            rows.Add(CreateRow(path, "Deleted", null, baseline[path].Sha256, null,
                FirstChangedAt(previousRows, path, timestamp), timestamp, agentRunIds));
        }

        var orderedRows = rows.OrderBy(row => row["path"]!.GetValue<string>(), StringComparer.Ordinal).ToArray();
        ValidateScopes(task, orderedRows);
        return new JsonObject
        {
            ["baseRevision"] = baseRevision,
            ["currentRevision"] = "workspace:" + ComputeManifestDigest(current),
            ["files"] = new JsonArray(orderedRows.Select(row => (JsonNode)row).ToArray())
        };
    }

    public static void ValidateCurrentWorkspace(string projectRoot, JsonObject workspaceChanges)
    {
        ArgumentNullException.ThrowIfNull(workspaceChanges);
        var current = CaptureManifest(projectRoot);
        var expectedRevision = workspaceChanges["currentRevision"]?.GetValue<string>();
        var actualRevision = "workspace:" + ComputeManifestDigest(current);
        if (!string.Equals(expectedRevision, actualRevision, StringComparison.Ordinal))
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-REVISION", "Workspace changed after the authoritative task snapshot was built.");
        }

        foreach (var row in (workspaceChanges["files"] as JsonArray ?? throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-WORKSPACE-SHAPE", "workspaceChanges.files is missing.")).OfType<JsonObject>())
        {
            var path = row["path"]!.GetValue<string>();
            var kind = row["changeKind"]!.GetValue<string>();
            if (kind == "Deleted")
            {
                if (current.ContainsKey(path)) throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-HASH", $"Deleted path '{path}' exists.");
                continue;
            }

            if (!current.TryGetValue(path, out var actual) ||
                !string.Equals(actual.Sha256, row["currentSha256"]?.GetValue<string>(), StringComparison.Ordinal))
            {
                throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-HASH", $"Current file '{path}' does not match workspaceChanges.");
            }
        }
    }

    private static JsonObject CreateRow(
        string path,
        string changeKind,
        string? previousPath,
        string? baseSha256,
        string? currentSha256,
        string firstChangedAt,
        string lastChangedAt,
        JsonArray agentRunIds)
    {
        return new JsonObject
        {
            ["path"] = path,
            ["changeKind"] = changeKind,
            ["previousPath"] = previousPath,
            ["baseSha256"] = baseSha256,
            ["currentSha256"] = currentSha256,
            ["firstChangedAt"] = firstChangedAt,
            ["lastChangedAt"] = lastChangedAt,
            ["agentRunIds"] = agentRunIds.DeepClone()
        };
    }

    private static string FirstChangedAt(IReadOnlyDictionary<string, JsonObject> previousRows, string path, string fallback)
    {
        return previousRows.TryGetValue(path, out var previous) && previous["firstChangedAt"] is JsonValue value
            ? value.GetValue<string>()
            : fallback;
    }

    private static JsonArray CollectAgentRunIds(JsonObject task)
    {
        var values = (task["conversation"]?["messages"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
            .Select(message => message["agentRunId"]?.GetValue<string>())
            .Concat((task["conversation"]?["contextCheckpoints"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .Select(checkpoint => checkpoint["agentRunId"]?.GetValue<string>()))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Select(value => (JsonNode)value!)
            .ToArray();
        return new JsonArray(values);
    }

    private static void ValidateScopes(JsonObject task, IReadOnlyList<JsonObject> rows)
    {
        if (rows.Count == 0) return;
        var contract = task["executionContract"] as JsonObject ?? throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-WORKSPACE-SCOPE", "executionContract is missing.");
        var allowed = MachinePathRules(contract, "allowedChanges");
        var forbidden = MachinePathRules(contract, "forbiddenChanges");
        if (allowed.Count == 0)
        {
            throw new TaskBoardV3ValidationException(
                "E2D-TASK-V3-WORKSPACE-SCOPE",
                "Changed files require at least one machine-readable executionContract.allowedChanges entry in the form path:<glob>.");
        }
        foreach (var row in rows)
        {
            var path = row["path"]!.GetValue<string>();
            if (!allowed.Any(pattern => GlobMatches(pattern, path)) || forbidden.Any(pattern => GlobMatches(pattern, path)))
            {
                throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-SCOPE", $"Changed path '{path}' is outside allowedChanges or matches forbiddenChanges.");
            }
        }
    }

    private static IReadOnlyList<string> MachinePathRules(JsonObject contract, string field)
    {
        return (contract[field] as JsonArray ?? new JsonArray()).OfType<JsonValue>()
            .Select(value => value.GetValue<string>())
            .Where(value => value.StartsWith("path:", StringComparison.Ordinal))
            .Select(value => value[5..])
            .Where(value => !string.IsNullOrWhiteSpace(value) && !value.StartsWith('/') && !value.Contains('\\') && !value.Contains(':'))
            .ToArray();
    }

    private static bool GlobMatches(string pattern, string path)
    {
        var expression = new StringBuilder("^");
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
            {
                expression.Append(".*");
                index++;
            }
            else if (character == '*')
            {
                expression.Append("[^/]*");
            }
            else
            {
                expression.Append(Regex.Escape(character.ToString()));
            }
        }
        expression.Append('$');
        return Regex.IsMatch(path, expression.ToString(), RegexOptions.CultureInvariant);
    }

    private static string BaselinePath(string projectRoot, string taskUid)
    {
        return Path.Combine(projectRoot, ".taskboard", ".cache", "workspace-changes", taskUid + ".json");
    }

    private static void SaveBaseline(
        string projectRoot,
        string taskUid,
        IReadOnlyDictionary<string, WorkspaceFileStateV3> manifest,
        string digest)
    {
        var path = BaselinePath(projectRoot, taskUid);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var document = new JsonObject
        {
            ["digest"] = digest,
            ["files"] = new JsonArray(manifest.Values.OrderBy(file => file.Path, StringComparer.Ordinal).Select(file => (JsonNode)new JsonObject
            {
                ["path"] = file.Path,
                ["sha256"] = file.Sha256,
                ["kind"] = file.Kind
            }).ToArray())
        };
        var temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        File.WriteAllText(temporaryPath, document.ToJsonString());
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static IReadOnlyDictionary<string, WorkspaceFileStateV3> LoadBaseline(string projectRoot, string taskUid, string baseRevision)
    {
        var path = BaselinePath(projectRoot, taskUid);
        if (!File.Exists(path))
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-BASELINE", "Trusted workspace baseline is unavailable; Review/Done is fail-closed.");
        }
        var document = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-WORKSPACE-BASELINE", "Trusted workspace baseline is invalid.");
        var digest = document["digest"]?.GetValue<string>();
        if (baseRevision != "workspace:" + digest)
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-BASELINE", "Trusted workspace baseline digest does not match the task.");
        }
        var result = (document["files"] as JsonArray ?? throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-WORKSPACE-BASELINE", "Trusted workspace baseline files are missing.")).OfType<JsonObject>()
            .Select(file => new WorkspaceFileStateV3(
                file["path"]!.GetValue<string>(),
                file["sha256"]!.GetValue<string>(),
                file["kind"]!.GetValue<string>()))
            .ToDictionary(file => file.Path, file => file, StringComparer.Ordinal);
        if (ComputeManifestDigest(result) != digest)
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-BASELINE", "Trusted workspace baseline content does not match its digest.");
        }
        return result;
    }

    private static void EnsureContained(string root, string path)
    {
        var absolute = Path.GetFullPath(path);
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!absolute.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-CONTAINMENT", $"Workspace path '{path}' escapes project root.");
        }
    }

    private static void EnsureNotReparse(string root, string path)
    {
        if (!string.Equals(Path.GetFullPath(root), Path.GetFullPath(path), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            EnsureContained(root, path);
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-REPARSE", $"Workspace path '{path}' is a symlink or reparse point.");
        }
    }

    private static void ValidateProjectRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Contains('\\') || path.Contains(':') ||
            path.Split('/').Any(segment => !IsSafeSegment(segment)))
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-WORKSPACE-PATH", $"Workspace path '{path}' is not canonical and cross-platform safe.");
        }
    }

    private static bool IsSafeSegment(string segment)
    {
        var stem = Path.GetFileNameWithoutExtension(segment);
        var reserved = stem.Equals("CON", StringComparison.OrdinalIgnoreCase) || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(stem, "^(COM|LPT)[1-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return !string.IsNullOrWhiteSpace(segment) && segment is not ("." or "..") &&
            !segment.EndsWith(' ') && !segment.EndsWith('.') && !segment.Any(char.IsControl) &&
            segment.IndexOfAny(['<', '>', ':', '"', '/', '\\', '|', '?', '*']) < 0 && !reserved;
    }
}
