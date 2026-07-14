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
using System.Text.Json.Nodes;
using System.Globalization;
using System.Text;

namespace Electron2D.ProjectSystem;

internal sealed record TaskBoardV3Snapshot(
    JsonObject Board,
    IReadOnlyList<JsonObject> ActiveTasks,
    IReadOnlyList<JsonObject> CompletedTasks);

internal sealed partial class TaskBoardV3DiskStore
{
    private readonly string projectRoot;
    private readonly TaskBoardWriteOptions writeOptions;

    public TaskBoardV3DiskStore(string projectRoot, TaskBoardWriteOptions? writeOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        this.projectRoot = Path.GetFullPath(projectRoot);
        this.writeOptions = writeOptions ?? TaskBoardWriteOptions.Default;
        _ = new TaskBoardDiskStore(this.projectRoot, this.writeOptions);
    }

    private TaskBoardDiskStore CreateTransactionStore()
    {
        return new TaskBoardDiskStore(projectRoot, writeOptions);
    }

    public static bool IsV3(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var boardPath = Path.Combine(
            Path.GetFullPath(projectRoot),
            ProjectTaskStorage.BoardDocumentPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(boardPath))
        {
            return false;
        }

        try
        {
            var board = JsonNode.Parse(ReadAllTextShared(boardPath)) as JsonObject;
            return board?["format"]?.GetValue<string>() == "Electron2D.TaskBoard" &&
                board["version"]?.GetValue<int>() == 3;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    public static JsonObject CreateNativeBoard(string boardId = "main")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);
        var board = new JsonObject
        {
            ["format"] = "Electron2D.TaskBoard",
            ["version"] = 3,
            ["boardId"] = boardId,
            ["revision"] = 1,
            ["idPolicy"] = new JsonObject
            {
                ["prefix"] = "T-",
                ["padding"] = 4,
                ["nextNumber"] = 1
            },
            ["attachmentPolicy"] = new JsonObject
            {
                ["perFileByteLimit"] = 25L * 1024 * 1024,
                ["perTaskByteLimit"] = 100L * 1024 * 1024,
                ["boardByteLimit"] = 250L * 1024 * 1024
            },
            ["validationContract"] = TaskBoardV3Migration.CreateValidationContract(),
            ["migration"] = null,
            ["tags"] = new JsonArray(),
            ["groups"] = new JsonArray(),
            ["placements"] = new JsonArray()
        };
        TaskBoardV3SemanticValidator.Validate(Environment.CurrentDirectory, board, [], [], validateAttachmentBlobs: false);
        return board;
    }

    public TaskBoardV3Snapshot LoadSnapshot()
    {
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        return LoadSnapshotCore();
    }

    private TaskBoardV3Snapshot LoadSnapshotCore()
    {
        var board = TaskBoardV3Compatibility.UpgradeBoard(ReadObject(ProjectTaskStorage.BoardDocumentPath));
        if (board["format"]?.GetValue<string>() != "Electron2D.TaskBoard" || board["version"]?.GetValue<int>() != 3)
        {
            throw new FormatException("TaskBoard v3 reader requires an Electron2D.TaskBoard version 3 document.");
        }

        var active = ReadTasks(ProjectTaskStorage.ActiveTasksDirectory).Select(TaskBoardV3Compatibility.UpgradeTask).ToArray();
        var completed = ReadTasks(ProjectTaskStorage.CompletedTasksDirectory).Select(TaskBoardV3Compatibility.UpgradeTask).ToArray();
        return new TaskBoardV3Snapshot(board, active, completed);
    }

    internal TaskBoardV3Snapshot LoadSnapshotUnderExistingWriteLock()
    {
        return LoadSnapshotCore();
    }

    public TaskBoardV3Snapshot Verify()
    {
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        TaskBoardV3SemanticValidator.Validate(
            projectRoot,
            snapshot.Board,
            snapshot.ActiveTasks,
            snapshot.CompletedTasks);
        return snapshot;
    }

    public JsonObject LoadTask(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        var snapshot = LoadSnapshot();
        return snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).SingleOrDefault(task =>
            string.Equals(RequiredString(task, "taskId"), taskId, StringComparison.Ordinal)) ??
            throw new FileNotFoundException($"Task '{taskId}' was not found.");
    }

    public JsonObject RetrieveAttachment(string taskId, string attachmentId, string? derivativeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(attachmentId);
        var snapshot = Verify();
        var task = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).SingleOrDefault(candidate =>
            string.Equals(RequiredString(candidate, "taskId"), taskId, StringComparison.Ordinal)) ??
            throw new FileNotFoundException($"Task '{taskId}' was not found.");
        var attachment = RequiredArray(task, "attachments").Select(RequiredObject).SingleOrDefault(candidate =>
            string.Equals(RequiredString(candidate, "attachmentId"), attachmentId, StringComparison.Ordinal)) ??
            throw new FileNotFoundException($"Attachment '{attachmentId}' was not found on task '{taskId}'.");

        JsonObject blob = attachment;
        if (derivativeId is not null)
        {
            blob = RequiredArray(attachment, "derivatives").Select(RequiredObject).SingleOrDefault(candidate =>
                string.Equals(RequiredString(candidate, "derivativeId"), derivativeId, StringComparison.Ordinal)) ??
                throw new FileNotFoundException($"Derivative '{derivativeId}' was not found on attachment '{attachmentId}'.");
            if (!string.Equals(RequiredString(blob, "status"), "Ready", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Derivative '{derivativeId}' is not retrievable because its status is '{RequiredString(blob, "status")}'.");
            }
        }

        var relativePath = RequiredString(blob, "relativePath");
        var bytes = File.ReadAllBytes(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var expectedLength = RequiredLong(blob, "byteLength");
        var expectedHash = RequiredString(blob, "sha256");
        var actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        if (bytes.LongLength != expectedLength || !string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Attachment changed during retrieval; retry against a stable taskboard revision.");
        }

        return new JsonObject
        {
            ["taskId"] = taskId,
            ["taskRevision"] = RequiredLong(task, "revision"),
            ["attachmentId"] = attachmentId,
            ["derivativeId"] = derivativeId,
            ["kind"] = derivativeId is null ? "Original" : RequiredString(blob, "kind"),
            ["displayName"] = RequiredString(attachment, "displayName"),
            ["mediaType"] = RequiredString(blob, "mediaType"),
            ["byteLength"] = expectedLength,
            ["sha256"] = expectedHash,
            ["contentBase64"] = Convert.ToBase64String(bytes)
        };
    }

    public static JsonObject CreateBoardProjection(TaskBoardV3Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var board = snapshot.Board.DeepClone().AsObject();
        var taskIdByUid = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks)
            .ToDictionary(task => RequiredString(task, "taskUid"), task => RequiredString(task, "taskId"), StringComparer.Ordinal);
        foreach (var placement in RequiredArray(board, "placements").Select(RequiredObject))
        {
            var taskUid = RequiredString(placement, "taskUid");
            placement["taskId"] = taskIdByUid[taskUid];
        }

        return board;
    }

    public static JsonObject CreateTaskProjection(JsonObject task, IReadOnlyList<JsonObject> allTasks)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(allTasks);
        var projected = task.DeepClone().AsObject();
        var taskIdByUid = allTasks.ToDictionary(
            item => RequiredString(item, "taskUid"),
            item => RequiredString(item, "taskId"),
            StringComparer.Ordinal);
        projected["labels"] = projected["tagIds"]!.DeepClone();
        projected["assignee"] = task["assignee"]?.DeepClone();
        var parentUid = NullableString(task, "parentTaskUid");
        projected["parentTaskId"] = parentUid is null ? null : taskIdByUid[parentUid];
        projected["dependencies"] = new JsonArray(DependencyUids(task)
            .Select(uid => (JsonNode)JsonValue.Create(taskIdByUid[uid])!).ToArray());
        projected["subtasks"] = new JsonArray(allTasks
            .Where(candidate => string.Equals(NullableString(candidate, "parentTaskUid"), RequiredString(task, "taskUid"), StringComparison.Ordinal))
            .Select(candidate => (JsonNode)JsonValue.Create(RequiredString(candidate, "taskId"))!).ToArray());
        projected["manualBlockingReasons"] = new JsonArray(RequiredArray(task, "blockers").Select(RequiredObject)
            .Where(blocker => RequiredString(blocker, "state") == "Active")
            .Select(blocker => (JsonNode)JsonValue.Create(RequiredString(blocker, "kind"))!).ToArray());

        projected["acceptanceCriteria"] = new JsonArray(RequiredArray(task, "acceptanceCriteria").Select(RequiredObject)
            .Select(criterion => (JsonNode)new JsonObject
            {
                ["criterionId"] = RequiredString(criterion, "criterionId"),
                ["description"] = RequiredString(criterion, "description"),
                ["state"] = RequiredString(criterion, "state"),
                ["evidenceLinks"] = new JsonArray(RequiredArray(criterion, "evidence").Select(EvidenceText)
                    .Select(value => (JsonNode)JsonValue.Create(value)!).ToArray())
            }).ToArray());
        projected["activity"] = new JsonArray(RequiredArray(task, "activity").Select(RequiredObject)
            .Select(entry => (JsonNode)CreateActivityProjection(entry)).ToArray());
        projected["executionContract"] = CreateExecutionProjection(RequiredObject(task["executionContract"]));
        ProjectLinks(projected, RequiredArray(task, "links").Select(RequiredObject));

        var readiness = ResolveReadiness(task, allTasks);
        projected["readiness"] = readiness;
        projected["boardStatus"] = readiness == "Ready" ? RequiredString(task, "status") : "Blocked";
        return projected;
    }

    public static JsonObject CreateCardProjection(JsonObject task, IReadOnlyList<JsonObject> allTasks)
    {
        var full = CreateTaskProjection(task, allTasks);
        var criteria = RequiredArray(full, "acceptanceCriteria").Select(RequiredObject).ToArray();
        var attachments = RequiredArray(full, "attachments").Select(RequiredObject).ToArray();
        var card = new JsonObject
        {
            ["taskUid"] = RequiredString(full, "taskUid"),
            ["taskId"] = RequiredString(full, "taskId"),
            ["revision"] = RequiredLong(full, "revision"),
            ["title"] = RequiredString(full, "title"),
            ["status"] = RequiredString(full, "status"),
            ["boardStatus"] = RequiredString(full, "boardStatus"),
            ["priority"] = RequiredString(full, "priority"),
            ["labels"] = full["labels"]!.DeepClone(),
            ["deadline"] = full["deadline"]?.DeepClone(),
            ["acceptanceCriteriaProgress"] = new JsonObject
            {
                ["passed"] = criteria.Count(criterion => RequiredString(criterion, "state") == "Passed"),
                ["total"] = criteria.Length
            },
            ["attachmentCount"] = attachments.Length,
            ["assignee"] = task["assignee"]?.DeepClone(),
            ["parentTaskId"] = full["parentTaskId"]?.DeepClone(),
            ["dependencies"] = full["dependencies"]!.DeepClone(),
            ["readiness"] = RequiredString(full, "readiness"),
            ["acceptanceState"] = RequiredString(full, "acceptanceState"),
            ["archivedAt"] = full["archivedAt"]?.DeepClone()
        };

        var previewId = NullableString(full, "previewAttachmentId");
        var preview = previewId is null
            ? attachments.Where(IsRasterAttachment).OrderBy(attachment => RequiredString(attachment, "attachmentId"), StringComparer.Ordinal).FirstOrDefault()
            : attachments.SingleOrDefault(attachment => RequiredString(attachment, "attachmentId") == previewId);
        if (preview is not null && IsRasterAttachment(preview))
        {
            card["cardPreview"] = new JsonObject
            {
                ["attachmentId"] = RequiredString(preview, "attachmentId"),
                ["displayName"] = RequiredString(preview, "displayName"),
                ["relativePath"] = RequiredString(preview, "relativePath"),
                ["mediaType"] = RequiredString(preview, "mediaType")
            };
        }

        return card;
    }

    public static ProjectTask CreateCompatibilityTask(JsonObject task, IReadOnlyList<JsonObject> allTasks)
    {
        var projected = CreateTaskProjection(task, allTasks);
        var result = new ProjectTask
        {
            TaskUid = RequiredString(projected, "taskUid"),
            Revision = RequiredLong(projected, "revision"),
            TaskId = RequiredString(projected, "taskId"),
            Title = RequiredString(projected, "title"),
            Description = projected["description"]?.GetValue<string>() ?? string.Empty,
            Status = Enum.Parse<ProjectTaskStatus>(RequiredString(projected, "status"), ignoreCase: false),
            Readiness = Enum.Parse<TaskReadiness>(RequiredString(projected, "readiness"), ignoreCase: false),
            Priority = RequiredString(projected, "priority"),
            Rank = "000000001000",
            Deadline = projected["deadline"] is null
                ? null
                : DateOnly.ParseExact(RequiredString(projected, "deadline"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Assignee = null,
            CreatedBy = RequiredString(projected, "createdBy"),
            ParentTaskId = NullableString(projected, "parentTaskId"),
            PreviewAttachmentId = NullableString(projected, "previewAttachmentId"),
            CreatedAt = ReadTimestamp(projected, "createdAt"),
            UpdatedAt = ReadTimestamp(projected, "updatedAt"),
            SubmittedAt = ReadOptionalTimestamp(projected, "submittedAt"),
            CompletedAt = ReadOptionalTimestamp(projected, "completedAt"),
            AcceptedAt = ReadOptionalTimestamp(projected, "acceptedAt"),
            AcceptedBy = NullableString(projected, "acceptedBy"),
            AcceptanceState = RequiredString(projected, "acceptanceState") switch
            {
                "Submitted" => ProjectTaskAcceptanceState.Submitted,
                "Accepted" => ProjectTaskAcceptanceState.Accepted,
                "ChangesRequested" => ProjectTaskAcceptanceState.ChangesRequested,
                "Cancelled" => ProjectTaskAcceptanceState.Cancelled,
                _ => ProjectTaskAcceptanceState.Open
            },
            ArchivedAt = ReadOptionalTimestamp(projected, "archivedAt"),
            ArchivedBy = NullableString(projected, "archivedBy"),
            CancellationReason = NullableString(projected, "cancellationReason")
        };
        result.LegacyAliases.AddRange(StringValues(projected, "legacyAliases"));
        result.Labels.AddRange(StringValues(projected, "labels"));
        result.Dependencies.AddRange(StringValues(projected, "dependencies"));
        result.Subtasks.AddRange(StringValues(projected, "subtasks"));
        result.LinkedTransactions.AddRange(StringValues(projected, "linkedTransactions"));
        result.LinkedJobs.AddRange(StringValues(projected, "linkedJobs"));
        result.LinkedDiagnostics.AddRange(StringValues(projected, "linkedDiagnostics"));
        result.LinkedArtifacts.AddRange(StringValues(projected, "linkedArtifacts"));
        result.LinkedScenesResourcesAndNodes.AddRange(StringValues(projected, "linkedScenesResourcesAndNodes"));
        foreach (var reason in RequiredArray(projected, "manualBlockingReasons").Select(RequiredString))
        {
            if (Enum.TryParse<TaskBlockingReason>(reason, ignoreCase: false, out var parsed)) result.BlockingReasons.Add(parsed);
        }

        foreach (var criterionNode in RequiredArray(projected, "acceptanceCriteria"))
        {
            var criterion = RequiredObject(criterionNode);
            result.AcceptanceCriteria.Add(new AcceptanceCriterion(
                RequiredString(criterion, "criterionId"),
                RequiredString(criterion, "description"),
                Enum.Parse<AcceptanceCriterionState>(RequiredString(criterion, "state"), ignoreCase: false),
                StringValues(criterion, "evidenceLinks")));
        }

        foreach (var activityNode in RequiredArray(projected, "activity"))
        {
            var activity = RequiredObject(activityNode);
            var kindText = RequiredString(activity, "kind");
            result.Activity.Add(new TaskActivityEntry(
                RequiredString(activity, "activityEntryId"),
                RequiredString(activity, "actorId"),
                Enum.Parse<PrincipalKind>(RequiredString(activity, "actorKind"), ignoreCase: false),
                ReadTimestamp(activity, "createdAt"),
                Enum.TryParse<TaskActivityKind>(kindText, ignoreCase: false, out var kind) ? kind : TaskActivityKind.Decision,
                RequiredString(activity, "payload")));
        }

        var contract = RequiredObject(projected["executionContract"]);
        result.ExecutionContract.TaskType = RequiredString(contract, "taskType");
        result.ExecutionContract.ReadyToStart.AddRange(StringValues(contract, "readyToStart"));
        result.ExecutionContract.StopConditions.AddRange(StringValues(contract, "stopConditions"));
        result.ExecutionContract.AllowedChanges.AddRange(StringValues(contract, "allowedChanges"));
        result.ExecutionContract.ForbiddenChanges.AddRange(StringValues(contract, "forbiddenChanges"));
        result.ExecutionContract.RequiredOutputs.AddRange(StringValues(contract, "requiredOutputs"));
        result.ExecutionContract.RequiredCommands.AddRange(StringValues(contract, "requiredCommands"));
        result.ExecutionContract.ExternalAudit = RequiredString(contract, "externalAudit");
        foreach (var attachmentNode in RequiredArray(projected, "attachments"))
        {
            var attachment = RequiredObject(attachmentNode);
            result.Attachments.Add(new TaskAttachment
            {
                AttachmentId = RequiredString(attachment, "attachmentId"),
                DisplayName = RequiredString(attachment, "displayName"),
                RelativePath = RequiredString(attachment, "relativePath"),
                MediaType = RequiredString(attachment, "mediaType"),
                ByteLength = RequiredLong(attachment, "byteLength"),
                Sha256 = RequiredString(attachment, "sha256"),
                AddedAt = ReadTimestamp(attachment, "addedAt"),
                AddedBy = RequiredString(attachment, "addedBy")
            });
        }

        return result;
    }

    private static IEnumerable<string> StringValues(JsonObject value, string propertyName)
    {
        return RequiredArray(value, propertyName).Select(RequiredString);
    }

    private static DateTimeOffset ReadTimestamp(JsonObject value, string propertyName)
    {
        return DateTimeOffset.Parse(RequiredString(value, propertyName), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateTimeOffset? ReadOptionalTimestamp(JsonObject value, string propertyName)
    {
        return value[propertyName] is null ? null : ReadTimestamp(value, propertyName);
    }

    private IReadOnlyList<JsonObject> ReadTasks(string relativeDirectory)
    {
        var directory = FullPath(relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.e2task", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => ReadObject(ProjectDocumentPaths.NormalizeRelativePath(Path.GetRelativePath(projectRoot, path))))
            .ToArray();
    }

    private JsonObject ReadObject(string relativePath)
    {
        var path = FullPath(relativePath);
        return JsonNode.Parse(ReadAllTextShared(path)) as JsonObject ??
            throw new FormatException($"Taskboard document '{relativePath}' is not a JSON object.");
    }

    private static string ReadAllTextShared(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private string FullPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Taskboard path '{relativePath}' escapes the project root.");
        }

        return fullPath;
    }

    private static string ResolveReadiness(JsonObject task, IReadOnlyList<JsonObject> allTasks)
    {
        var byUid = allTasks.ToDictionary(item => RequiredString(item, "taskUid"), StringComparer.Ordinal);
        var dependencies = DependencyUids(task).Select(uid => byUid[uid]).ToArray();
        if (dependencies.Any(dependency => RequiredString(dependency, "status") == "Cancelled"))
        {
            return "DependencyCancelled";
        }

        return dependencies.Any(dependency => RequiredString(dependency, "status") != "Done")
            ? "BlockedByDependencies"
            : "Ready";
    }

    private static IEnumerable<string> DependencyUids(JsonObject task)
    {
        return RequiredArray(task, "relations").Select(RequiredObject)
            .Where(relation => RequiredString(relation, "kind") == "DependsOn")
            .Select(relation => RequiredString(relation, "targetTaskUid"));
    }

    private static JsonObject CreateActivityProjection(JsonObject entry)
    {
        var projected = entry.DeepClone().AsObject();
        var payload = RequiredObject(entry["payload"]);
        projected["payload"] = RequiredString(entry, "kind") switch
        {
            "Comment" or "AgentSummary" => RequiredString(payload, "markdown"),
            "Legacy" => RequiredString(payload, "text"),
            _ => payload.ToJsonString()
        };
        return projected;
    }

    private static JsonObject CreateExecutionProjection(JsonObject contract)
    {
        var projected = contract.DeepClone().AsObject();
        var audit = RequiredObject(contract["externalAudit"]);
        projected["externalAudit"] = RequiredString(audit, "mode") == "None"
            ? "not-required"
            : audit["instructions"]?.GetValue<string>() ?? RequiredString(audit, "mode");
        projected["requiredCommands"] = new JsonArray(RequiredArray(contract, "commands").Select(RequiredObject)
            .Select(command => (JsonNode)JsonValue.Create(CommandText(command))!).ToArray());
        return projected;
    }

    private static string CommandText(JsonObject command)
    {
        if (RequiredString(command, "kind") == "LegacyShell")
        {
            return RequiredString(command, "text");
        }

        var arguments = RequiredArray(command, "arguments").Select(RequiredString);
        return string.Join(' ', new[] { RequiredString(command, "executable") }.Concat(arguments));
    }

    private static void ProjectLinks(JsonObject task, IEnumerable<JsonObject> links)
    {
        var all = links.ToArray();
        task["linkedTransactions"] = LinkValues(all, "Transaction");
        task["linkedJobs"] = LinkValues(all, "Job");
        task["linkedDiagnostics"] = LinkValues(all, "Diagnostic");
        task["linkedArtifacts"] = new JsonArray(all.Where(link => RequiredString(link, "kind") is "File" or "Directory" or "Uri")
            .Select(link =>
            {
                var value = RequiredString(link, "value");
                return (JsonNode)JsonValue.Create(RequiredString(link, "kind") == "Directory" ? value.TrimEnd('/', '\\') + "/" : value)!;
            }).ToArray());
        task["linkedScenesResourcesAndNodes"] = new JsonArray(all.Where(link => RequiredString(link, "kind") is "Scene" or "Resource" or "Node")
            .Select(link => (JsonNode)JsonValue.Create(RequiredString(link, "value"))!).ToArray());
    }

    private static JsonArray LinkValues(IEnumerable<JsonObject> links, string kind)
    {
        return new JsonArray(links.Where(link => RequiredString(link, "kind") == kind)
            .Select(link => (JsonNode)JsonValue.Create(RequiredString(link, "value"))!).ToArray());
    }

    private static string EvidenceText(JsonNode? node)
    {
        var evidence = RequiredObject(node);
        return RequiredString(evidence, "kind") switch
        {
            "File" => RequiredString(evidence, "path"),
            "Uri" => RequiredString(evidence, "uri"),
            "Attachment" => RequiredString(evidence, "attachmentId"),
            _ => throw new FormatException("Unknown v3 evidence kind.")
        };
    }

    private static bool IsRasterAttachment(JsonObject attachment)
    {
        return RequiredString(attachment, "mediaType").ToLowerInvariant() is
            "image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp";
    }

    private static string RequiredString(JsonObject value, string propertyName)
    {
        return RequiredString(value[propertyName]);
    }

    private static string RequiredString(JsonNode? value)
    {
        var text = value?.GetValue<string>();
        return string.IsNullOrWhiteSpace(text) ? throw new FormatException("Required v3 string is missing.") : text;
    }

    private static string? NullableString(JsonObject value, string propertyName)
    {
        return value[propertyName]?.GetValue<string>();
    }

    private static long RequiredLong(JsonObject value, string propertyName)
    {
        var node = value[propertyName] as JsonValue ?? throw new FormatException($"Required v3 integer '{propertyName}' is missing.");
        if (node.TryGetValue<long>(out var longValue))
        {
            return longValue;
        }

        if (node.TryGetValue<int>(out var intValue))
        {
            return intValue;
        }

        throw new FormatException($"Required v3 integer '{propertyName}' is invalid.");
    }

    private static JsonArray RequiredArray(JsonObject value, string propertyName)
    {
        return value[propertyName] as JsonArray ?? throw new FormatException($"Required v3 array '{propertyName}' is missing.");
    }

    private static JsonObject RequiredObject(JsonNode? value)
    {
        return value as JsonObject ?? throw new FormatException("Required v3 object is missing.");
    }
}
