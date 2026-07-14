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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal sealed record TaskExecutionGrantV3(
    IReadOnlyList<string> GrantedCapabilities,
    bool AllowShellInterpreter,
    bool HumanConfirmed,
    IReadOnlyList<string>? AllowedExecutables = null);

internal static class TaskExecutionPolicyV3
{
    private const string CodePrefix = "E2D-TASK-V3-EXECUTION-";

    private static readonly HashSet<string> ShellInterpreters = new(StringComparer.OrdinalIgnoreCase)
    {
        "bash", "bash.exe", "sh", "sh.exe", "zsh", "zsh.exe",
        "cmd", "cmd.exe", "powershell", "powershell.exe", "pwsh", "pwsh.exe"
    };

    public static void Authorize(JsonObject command, TaskExecutionGrantV3 trustedGrant)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(trustedGrant);
        if (command["kind"]?.GetValue<string>() == "LegacyShell")
        {
            throw Error("LEGACY-SHELL-DENIED", "LegacyShell commands are never executable automatically.");
        }

        if (command["kind"]?.GetValue<string>() != "Process")
        {
            throw Error("COMMAND-KIND", "Only a validated Process command can be considered by the execution policy.");
        }

        var executable = command["executable"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw Error("EXECUTABLE", "Process executable is missing.");
        }

        var executableName = Path.GetFileName(executable);
        if (ShellInterpreters.Contains(executableName) &&
            (!trustedGrant.AllowShellInterpreter || !trustedGrant.HumanConfirmed))
        {
            throw Error("SHELL-DENIED", "Shell interpreters require a separate trusted grant and human confirmation.");
        }

        if (trustedGrant.AllowedExecutables is null ||
            !trustedGrant.AllowedExecutables.Contains(executableName, StringComparer.OrdinalIgnoreCase))
        {
            throw Error("EXECUTABLE-DENIED", $"Executable '{executableName}' is not allowed by the trusted policy.");
        }

        var requested = command["requestedCapabilities"] as JsonArray ??
            throw Error("CAPABILITY-SHAPE", "Process command must declare requestedCapabilities.");
        foreach (var capability in requested)
        {
            var value = capability?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(value) || !trustedGrant.GrantedCapabilities.Contains(value))
            {
                throw Error("CAPABILITY-DENIED", $"Capability '{value}' was requested by the task but not granted by trusted policy.");
            }
        }

        if (command["confirmation"]?.GetValue<string>() == "HumanRequired" && !trustedGrant.HumanConfirmed)
        {
            throw Error("CONFIRMATION-REQUIRED", "The command requires current human confirmation.");
        }
    }

    private static TaskBoardV3ValidationException Error(string suffix, string message)
    {
        return new TaskBoardV3ValidationException(CodePrefix + suffix, message);
    }
}

internal static class AgentContextBuilderV3
{
    internal const string BuilderProfile = "AgentContextBuilderV3";
    internal const string HashProfile = "sha256-jcs-rfc8785-v1";

    private static readonly JsonSerializerOptions CanonicalStringOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonObject Build(JsonObject task, int recentMessageCount = 50)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentOutOfRangeException.ThrowIfNegative(recentMessageCount);
        TaskBoardV3SchemaValidator.ValidateTask(task);
        var conversation = task["conversation"]!.AsObject();
        var messages = conversation["messages"]!.AsArray();
        var snapshot = task["contextSnapshot"] as JsonObject;
        var coveredMessages = snapshot is null ? 0 : RequiredLong(snapshot, "throughMessageSequence");
        var coveredActivity = snapshot is null ? 0 : RequiredLong(snapshot, "throughActivitySequence");
        var recent = new JsonArray(messages.OfType<JsonObject>()
            .Where(message => message["sequence"]!.GetValue<long>() > coveredMessages)
            .Select(message => message!.DeepClone()).ToArray());
        var activity = task["activity"]!.AsArray();
        var activityTail = new JsonArray(activity.OfType<JsonObject>()
            .Where(entry => entry["sequence"]!.GetValue<long>() > coveredActivity)
            .Select(entry => entry.DeepClone()).ToArray());
        var definition = task.DeepClone().AsObject();
        definition.Remove("conversation");
        definition.Remove("activity");
        definition.Remove("attachments");
        definition.Remove("contextSnapshot");
        definition.Remove("workspaceChanges");
        var manifest = BuildManifest(task);
        return new JsonObject
        {
            ["taskRevision"] = RequiredLong(task, "revision"),
            ["lastMessageSequence"] = RequiredLong(conversation, "lastMessageSequence"),
            ["lastActivitySequence"] = RequiredLong(task, "lastActivitySequence"),
            ["contextDigest"] = HashCanonical(manifest),
            ["contextManifest"] = manifest,
            ["definition"] = definition,
            ["contextSnapshot"] = task["contextSnapshot"]?.DeepClone(),
            ["summaryMarkdown"] = snapshot?["summaryMarkdown"]?.DeepClone(),
            ["recentMessages"] = recent,
            ["availableMessageRange"] = new JsonObject
            {
                ["fromSequence"] = messages.Count == 0 ? 0 : 1,
                ["toSequence"] = RequiredLong(conversation, "lastMessageSequence")
            },
            ["activity"] = activityTail,
            ["attachments"] = task["attachments"]!.DeepClone(),
            ["workspaceChanges"] = task["workspaceChanges"]!.DeepClone()
        };
    }

    public static JsonObject BuildCheckpoint(
        JsonObject task,
        string agentRunId,
        string actorId,
        TaskBoardV3Role role,
        string? rebaseOfCheckpointId = null,
        long? throughTaskRevision = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentRunId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var revision = throughTaskRevision ?? RequiredLong(task, "revision");
        var conversation = task["conversation"] as JsonObject ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-CONVERSATION", "Task conversation is missing.");
        var lastSequence = RequiredLong(conversation, "lastMessageSequence");
        var lastActivitySequence = RequiredLong(task, "lastActivitySequence");
        var digest = HashCanonical(BuildManifest(task, revision));
        return new JsonObject
        {
            ["checkpointId"] = $"checkpoint-{Guid.NewGuid():N}",
            ["agentRunId"] = agentRunId,
            ["actorId"] = actorId,
            ["role"] = role.ToString(),
            ["createdAt"] = DateTimeOffset.UtcNow.ToString("O"),
            ["taskRevision"] = revision,
            ["lastMessageSequence"] = lastSequence,
            ["lastActivitySequence"] = lastActivitySequence,
            ["contextDigest"] = digest,
            ["rebaseOfCheckpointId"] = rebaseOfCheckpointId
        };
    }

    public static bool RequiresRebase(JsonObject checkpoint, JsonObject currentTask)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        ArgumentNullException.ThrowIfNull(currentTask);
        var conversation = currentTask["conversation"] as JsonObject ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-CONVERSATION", "Task conversation is missing.");
        return RequiredLong(checkpoint, "taskRevision") != RequiredLong(currentTask, "revision") ||
            RequiredLong(checkpoint, "lastMessageSequence") != RequiredLong(conversation, "lastMessageSequence") ||
            RequiredLong(checkpoint, "lastActivitySequence") != RequiredLong(currentTask, "lastActivitySequence") ||
            !string.Equals(checkpoint["contextDigest"]?.GetValue<string>(), ComputeDigest(currentTask), StringComparison.Ordinal);
    }

    public static string ComputeDigest(JsonObject task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return HashCanonical(BuildManifest(task));
    }

    internal static JsonObject BuildManifest(
        JsonObject task,
        long? throughTaskRevision = null,
        long? throughMessageSequence = null,
        long? throughActivitySequence = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        var conversation = task["conversation"] as JsonObject ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-CONVERSATION", "Task conversation is missing.");
        var messages = conversation["messages"] as JsonArray ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-CONVERSATION", "Task messages are missing.");
        var activity = task["activity"] as JsonArray ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-ACTIVITY", "Task activity is missing.");
        var auditRuns = task["auditRuns"] as JsonArray ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-AUDIT", "Task audit runs are missing.");
        var checkpoints = conversation["contextCheckpoints"] as JsonArray ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-CHECKPOINTS", "Task context checkpoints are missing.");
        var workspaceChanges = task["workspaceChanges"] as JsonObject ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-WORKSPACE", "Task workspaceChanges are missing.");
        var currentTaskRevision = RequiredLong(task, "revision");
        var taskRevision = throughTaskRevision ?? currentTaskRevision;
        if (taskRevision != currentTaskRevision)
        {
            throw new TaskBoardV3ValidationException(
                "E2D-TASK-V3-CONTEXT-HISTORICAL-CORE",
                "A live task can build only its current task-core revision; historical manifests are read from immutable snapshots/events.");
        }
        var messageSequence = throughMessageSequence ?? RequiredLong(conversation, "lastMessageSequence");
        var activitySequence = throughActivitySequence ?? RequiredLong(task, "lastActivitySequence");
        var messagePrefix = new JsonArray(messages.OfType<JsonObject>()
            .Where(message => RequiredLong(message, "sequence") <= messageSequence)
            .Select(message => message.DeepClone()).ToArray());
        var checkpointPrefix = new JsonArray(checkpoints.OfType<JsonObject>()
            .Where(checkpoint => RequiredLong(checkpoint, "taskRevision") <= taskRevision && RequiredLong(checkpoint, "lastMessageSequence") <= messageSequence)
            .Select(checkpoint =>
            {
                var projection = checkpoint.DeepClone().AsObject();
                projection.Remove("contextDigest");
                return (JsonNode)projection;
            }).ToArray());
        var activityPrefix = new JsonArray(activity.OfType<JsonObject>()
            .Where(entry => RequiredLong(entry, "sequence") <= activitySequence)
            .Select(entry => entry.DeepClone()).ToArray());
        var auditRunPrefix = new JsonArray(auditRuns.OfType<JsonObject>()
            .Where(run => RequiredLong(run, "recordedAtRevision") <= taskRevision)
            .Select(run => run.DeepClone()).ToArray());

        var taskCore = task.DeepClone().AsObject();
        taskCore.Remove("conversation");
        taskCore.Remove("activity");
        taskCore.Remove("attachments");
        taskCore.Remove("contextSnapshot");
        taskCore.Remove("workspaceChanges");
        taskCore.Remove("auditRuns");
        taskCore["revision"] = taskRevision;

        var attachmentManifest = BuildAttachmentManifest(task);
        return new JsonObject
        {
            ["builderProfile"] = BuilderProfile,
            ["hashProfile"] = HashProfile,
            ["throughTaskRevision"] = taskRevision,
            ["throughMessageSequence"] = messageSequence,
            ["throughActivitySequence"] = activitySequence,
            ["taskCoreDigest"] = HashCanonical(taskCore),
            ["conversationDigest"] = HashCanonical(messagePrefix),
            ["checkpointDigest"] = HashCanonical(checkpointPrefix),
            ["activityDigest"] = HashCanonical(activityPrefix),
            ["auditRunsDigest"] = HashCanonical(auditRunPrefix),
            ["workspaceChangesDigest"] = HashCanonical(workspaceChanges),
            ["attachmentManifestDigest"] = HashCanonical(attachmentManifest),
            ["attachmentManifest"] = attachmentManifest
        };
    }

    internal static string HashCanonical(JsonNode? value)
    {
        var canonical = WriteCanonical(value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static JsonArray BuildAttachmentManifest(JsonObject task)
    {
        var result = new JsonArray();
        var attachments = task["attachments"] as JsonArray ??
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-CONTEXT-ATTACHMENTS", "Task attachments are missing.");
        foreach (var attachment in attachments.OfType<JsonObject>()
            .OrderBy(item => item["attachmentId"]?.GetValue<string>(), StringComparer.Ordinal))
        {
            var attachmentId = attachment["attachmentId"]?.GetValue<string>() ?? string.Empty;
            result.Add(new JsonObject
            {
                ["attachmentId"] = attachmentId,
                ["derivativeId"] = null,
                ["kind"] = "Original",
                ["byteLength"] = RequiredLong(attachment, "byteLength"),
                ["sha256"] = attachment["sha256"]?.GetValue<string>()
            });
            foreach (var derivative in (attachment["derivatives"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .Where(item => item["status"]?.GetValue<string>() == "Ready")
                .OrderBy(item => item["kind"]?.GetValue<string>(), StringComparer.Ordinal))
            {
                result.Add(new JsonObject
                {
                    ["attachmentId"] = attachmentId,
                    ["derivativeId"] = derivative["derivativeId"]?.GetValue<string>(),
                    ["kind"] = derivative["kind"]?.GetValue<string>(),
                    ["byteLength"] = RequiredLong(derivative, "byteLength"),
                    ["sha256"] = derivative["sha256"]?.GetValue<string>()
                });
            }
        }

        return result;
    }

    internal static string WriteCanonical(JsonNode? value)
    {
        var builder = new StringBuilder();
        AppendCanonical(builder, value);
        return builder.ToString();
    }

    private static void AppendCanonical(StringBuilder builder, JsonNode? value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                return;
            case JsonObject objectValue:
                builder.Append('{');
                var firstProperty = true;
                foreach (var property in objectValue.OrderBy(property => property.Key, StringComparer.Ordinal))
                {
                    if (!firstProperty) builder.Append(',');
                    firstProperty = false;
                    builder.Append(JsonSerializer.Serialize(property.Key, CanonicalStringOptions));
                    builder.Append(':');
                    AppendCanonical(builder, property.Value);
                }
                builder.Append('}');
                return;
            case JsonArray arrayValue:
                builder.Append('[');
                for (var index = 0; index < arrayValue.Count; index++)
                {
                    if (index > 0) builder.Append(',');
                    AppendCanonical(builder, arrayValue[index]);
                }
                builder.Append(']');
                return;
            case JsonValue scalar when scalar.TryGetValue<string>(out var text):
                builder.Append(JsonSerializer.Serialize(text, CanonicalStringOptions));
                return;
            case JsonValue scalar when scalar.TryGetValue<bool>(out var boolean):
                builder.Append(boolean ? "true" : "false");
                return;
            case JsonValue scalar when scalar.TryGetValue<long>(out var integer):
                builder.Append(integer.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            case JsonValue scalar when scalar.TryGetValue<int>(out var int32):
                builder.Append(int32.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            case JsonValue scalar when scalar.TryGetValue<uint>(out var uint32):
                builder.Append(uint32.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            case JsonValue scalar when scalar.TryGetValue<ulong>(out var uint64):
                builder.Append(uint64.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            default:
                throw new TaskBoardV3ValidationException(
                    "E2D-TASK-V3-CONTEXT-JCS-NUMBER",
                    "TaskBoard v3 canonical context supports the RFC 8785 integer subset; floating or implementation-specific values are forbidden.");
        }
    }

    private static long RequiredLong(JsonObject value, string propertyName)
    {
        if (value[propertyName] is JsonValue number)
        {
            if (number.TryGetValue<long>(out var longValue))
            {
                return longValue;
            }

            if (number.TryGetValue<int>(out var intValue))
            {
                return intValue;
            }
        }

        throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-CONTEXT-SHAPE",
            $"Required integer '{propertyName}' is missing.");
    }
}

internal static class TaskPatchV3
{
    private static readonly string[] PatchableFields =
    [
        "title", "description", "priority", "tagIds", "deadline", "assignee", "parentTaskUid",
        "relations", "acceptanceCriteria", "links", "executionContract"
    ];

    public static JsonArray BuildPatch(JsonObject previous, JsonObject next)
    {
        var patch = new JsonArray();
        foreach (var field in PatchableFields)
        {
            if (!JsonNode.DeepEquals(DefinitionValue(previous, field), DefinitionValue(next, field)))
            {
                patch.Add(new JsonObject
                {
                    ["op"] = "replace",
                    ["path"] = "/" + field,
                    ["oldValue"] = DefinitionValue(previous, field),
                    ["value"] = DefinitionValue(next, field)
                });
            }
        }

        return patch;
    }

    public static string ComputeTaskCoreDigest(JsonObject task)
    {
        var core = new JsonObject();
        foreach (var field in PatchableFields)
        {
            core[field] = DefinitionValue(task, field);
        }

        return AgentContextBuilderV3.HashCanonical(core);
    }

    public static void AppendIfRequired(
        JsonObject previous,
        JsonObject next,
        string actorId,
        string actorKind,
        DateTimeOffset createdAt)
    {
        var patch = BuildPatch(previous, next);
        if (patch.Count == 0)
        {
            return;
        }

        var sequence = TaskActivitySequenceV3.Next(next);
        TaskActivitySequenceV3.Append(next, new JsonObject
        {
            ["activityEntryId"] = $"activity-{Guid.NewGuid():N}",
            ["sequence"] = sequence,
            ["actorId"] = actorId,
            ["actorKind"] = actorKind,
            ["createdAt"] = createdAt.ToString("O"),
            ["kind"] = "TaskPatched",
            ["payload"] = new JsonObject
            {
                ["fromRevision"] = previous["revision"]!.GetValue<long>(),
                ["toRevision"] = previous["revision"]!.GetValue<long>() + 1,
                ["activitySequence"] = sequence,
                ["hashProfile"] = AgentContextBuilderV3.HashProfile,
                ["patch"] = patch,
                ["beforeTaskCoreDigest"] = ComputeTaskCoreDigest(previous),
                ["afterTaskCoreDigest"] = ComputeTaskCoreDigest(next)
            }
        });
    }

    private static JsonNode? DefinitionValue(JsonObject task, string field)
    {
        var value = task[field]?.DeepClone();
        if (field != "acceptanceCriteria" || value is not JsonArray criteria)
        {
            return value;
        }

        foreach (var criterion in criteria.OfType<JsonObject>())
        {
            criterion.Remove("state");
            criterion.Remove("evidence");
        }

        return criteria;
    }
}

internal static class TaskActivitySequenceV3
{
    public static long Next(JsonObject task)
    {
        if (task["lastActivitySequence"] is not JsonValue value || !value.TryGetValue<long>(out var sequence))
        {
            if (task["lastActivitySequence"] is JsonValue intValue && intValue.TryGetValue<int>(out var intSequence))
            {
                return intSequence + 1L;
            }
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-ACTIVITY-SEQUENCE", "Task lastActivitySequence is missing.");
        }
        return sequence + 1;
    }

    public static void Append(JsonObject task, JsonObject entry)
    {
        var expected = Next(task);
        if (entry["sequence"]?.GetValue<long>() != expected)
        {
            throw new TaskBoardV3ValidationException("E2D-TASK-V3-ACTIVITY-SEQUENCE", "Appended activity sequence is not the next atomic sequence.");
        }
        (task["activity"] as JsonArray ?? throw new TaskBoardV3ValidationException(
            "E2D-TASK-V3-ACTIVITY-SEQUENCE", "Task activity is missing.")).Add(entry);
        task["lastActivitySequence"] = expected;
    }
}
