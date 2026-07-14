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
using System.Text.RegularExpressions;

namespace Electron2D.ProjectSystem;

internal static class TaskBoardV3Compatibility
{
    public static JsonObject UpgradeBoard(JsonObject board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var attachmentPolicy = board["attachmentPolicy"] as JsonObject;
        if (attachmentPolicy is not null && !attachmentPolicy.ContainsKey("perTaskByteLimit"))
        {
            attachmentPolicy["perTaskByteLimit"] = attachmentPolicy["boardByteLimit"]?.DeepClone();
        }

        if (!board.ContainsKey("validationContract"))
        {
            board["validationContract"] = TaskBoardV3Migration.CreateValidationContract();
        }

        if (board["migration"] is JsonObject migration && !migration.ContainsKey("reportPath"))
        {
            migration["reportPath"] = ".taskboard/.migration/v2/report.json";
        }

        return board;
    }

    public static JsonObject UpgradeTask(JsonObject task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var wasPreReleaseDraft = !task.ContainsKey("lastActivitySequence") || !task.ContainsKey("workspaceChanges");
        if (!task.ContainsKey("assignee"))
        {
            task["assignee"] = null;
        }

        if (!task.ContainsKey("conversation"))
        {
            task["conversation"] = ExtractConversation(task);
        }

        if (!task.ContainsKey("contextSnapshot"))
        {
            task["contextSnapshot"] = null;
        }
        else if (task["contextSnapshot"] is JsonObject snapshot &&
            snapshot["hashProfile"]?.GetValue<string>() != AgentContextBuilderV3.HashProfile)
        {
            task["contextSnapshot"] = null;
        }

        if (!task.ContainsKey("workspaceChanges"))
        {
            task["workspaceChanges"] = new JsonObject
            {
                ["baseRevision"] = null,
                ["currentRevision"] = null,
                ["files"] = new JsonArray()
            };
        }

        if (!task.ContainsKey("auditRuns"))
        {
            task["auditRuns"] = new JsonArray();
        }

        if (task["status"]?.GetValue<string>() == "Done" && task["submittedAt"] is null)
        {
            task["submittedAt"] = task["completedAt"]?.DeepClone() ?? task["acceptedAt"]?.DeepClone() ?? task["updatedAt"]?.DeepClone();
        }

        if (task["executionContract"] is JsonObject contract)
        {
            if (contract["externalAudit"] is JsonValue legacyAudit && legacyAudit.TryGetValue<string>(out var text))
            {
                PreserveLegacyExternalAuditContract(task, text);
                contract["externalAudit"] = task["status"]?.GetValue<string>() == "Done" &&
                    task["acceptanceState"]?.GetValue<string>() == "Accepted"
                    ? TaskBoardV3Migration.ConvertExternalAudit(null)
                    : TaskBoardV3Migration.ConvertExternalAudit(text);
            }

            if (contract["commands"] is JsonArray commands)
            {
                foreach (var command in commands.OfType<JsonObject>().Where(command => command["kind"]?.GetValue<string>() == "Process"))
                {
                    if (!command.ContainsKey("requestedCapabilities") &&
                        command["requiredAccess"] is JsonValue accessValue && accessValue.TryGetValue<string>(out var access))
                    {
                        command["requestedCapabilities"] = ConvertAccess(access);
                        command.Remove("requiredAccess");
                    }
                }
            }
        }

        if (task["attachments"] is JsonArray attachments)
        {
            foreach (var attachment in attachments.OfType<JsonObject>())
            {
                if (!attachment.ContainsKey("derivatives"))
                {
                    attachment["derivatives"] = new JsonArray();
                }

                UpgradeDerivatives(attachment);
            }
        }

        if (task["acceptanceCriteria"] is JsonArray criteria)
        {
            if (wasPreReleaseDraft && criteria.Count == 0 && task["status"]?.GetValue<string>() == "Done")
            {
                var acceptanceId = (task["activity"] as JsonArray)?.OfType<JsonObject>()
                    .LastOrDefault(entry => entry["kind"]?.GetValue<string>() == "AcceptanceResult")?["activityEntryId"]?.GetValue<string>() ?? "legacy-acceptance";
                var taskUid = task["taskUid"]?.GetValue<string>() ?? "unknown";
                criteria.Add(new JsonObject
                {
                    ["criterionId"] = "legacy-accepted-result",
                    ["description"] = "Результат был принят до введения обязательных критериев TaskBoard v3.",
                    ["state"] = "Passed",
                    ["evidence"] = new JsonArray(new JsonObject
                    {
                        ["kind"] = "Uri",
                        ["uri"] = $"e2d-task://{taskUid}/activity/{acceptanceId}"
                    })
                });
            }

            foreach (var criterion in criteria.OfType<JsonObject>().Where(criterion =>
                criterion["state"]?.GetValue<string>() == "Passed" &&
                criterion["evidence"] is JsonArray evidence && evidence.Count == 0))
            {
                if (task["status"]?.GetValue<string>() != "Done")
                {
                    criterion["state"] = "Open";
                    continue;
                }

                var acceptanceId = (task["activity"] as JsonArray)?.OfType<JsonObject>()
                    .LastOrDefault(entry => entry["kind"]?.GetValue<string>() == "AcceptanceResult")?["activityEntryId"]?.GetValue<string>() ?? "legacy-acceptance";
                var taskUid = task["taskUid"]?.GetValue<string>() ?? "unknown";
                ((JsonArray)criterion["evidence"]!).Add(new JsonObject
                {
                    ["kind"] = "Uri",
                    ["uri"] = $"e2d-task://{taskUid}/activity/{acceptanceId}"
                });
            }
        }

        if (task["links"] is JsonArray links)
        {
            foreach (var link in links.OfType<JsonObject>().Where(link =>
                link["kind"]?.GetValue<string>() is "File" or "Directory"))
            {
                var value = link["value"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                value = value.Replace('\\', '/');
                if (link["kind"]?.GetValue<string>() == "Directory")
                {
                    value = value.TrimEnd('/');
                    if (value.Length == 0)
                    {
                        value = ".";
                    }
                }

                if (value.StartsWith("./", StringComparison.Ordinal))
                {
                    value = value[2..];
                }

                link["value"] = value;
                var allowRoot = link["kind"]?.GetValue<string>() == "Directory";
                if (!IsCanonicalProjectPath(value, allowRoot))
                {
                    link["kind"] = "Resource";
                }
            }

            foreach (var link in links.OfType<JsonObject>().Where(link => link["kind"]?.GetValue<string>() == "Uri"))
            {
                var value = link["value"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value, UriKind.Absolute, out _))
                {
                    link["kind"] = "Resource";
                }
            }
        }

        if (task["activity"] is JsonArray activity)
        {
            foreach (var entry in activity.OfType<JsonObject>().Where(entry => entry["kind"]?.GetValue<string>() == "AcceptanceResult"))
            {
                if (entry["payload"] is not JsonObject payload || !payload.ContainsKey("humanActorReference"))
                {
                    continue;
                }

                var actorId = payload["humanActorReference"]?.GetValue<string>() ?? entry["actorId"]?.GetValue<string>();
                payload.Remove("humanActorReference");
                payload["authorityActorId"] = actorId;
                payload["authorityRole"] = "Owner";
                entry["actorKind"] = "Human";
            }

            foreach (var entry in activity.OfType<JsonObject>().Where(entry => entry["kind"]?.GetValue<string>() == "AcceptanceResult"))
            {
                if (entry["payload"] is JsonObject payload && !payload.ContainsKey("auditRunId"))
                {
                    payload["auditRunId"] = null;
                }
            }
        }

        NormalizeLegacyConversationActivity(task);
        NormalizeDraftTaskPatchActivity(task);
        NormalizeActivitySequences(task);

        if (task["conversation"] is JsonObject conversation && conversation["contextCheckpoints"] is JsonArray checkpoints)
        {
            foreach (var checkpoint in checkpoints.OfType<JsonObject>())
            {
                if (!checkpoint.ContainsKey("lastActivitySequence"))
                {
                    checkpoint["lastActivitySequence"] = task["lastActivitySequence"]!.DeepClone();
                }
            }
        }

        return task;
    }

    private static void NormalizeDraftTaskPatchActivity(JsonObject task)
    {
        if (task["activity"] is not JsonArray activity) return;
        foreach (var entry in activity.OfType<JsonObject>().Where(entry =>
            entry["kind"]?.GetValue<string>() == "TaskPatched" &&
            entry["payload"] is JsonObject payload && !payload.ContainsKey("oldValue") && !payload.ContainsKey("fromRevision")))
        {
            var original = entry["payload"]!.ToJsonString();
            entry["kind"] = "Legacy";
            entry["payload"] = new JsonObject
            {
                ["sourceKind"] = "TaskPatchedV3Draft",
                ["text"] = original
            };
        }
    }

    private static void NormalizeActivitySequences(JsonObject task)
    {
        var activity = task["activity"] as JsonArray ?? new JsonArray();
        task["activity"] = activity;
        long sequence = 0;
        foreach (var entry in activity.OfType<JsonObject>())
        {
            entry["sequence"] = ++sequence;
        }
        task["lastActivitySequence"] = sequence;
    }

    private static void PreserveLegacyExternalAuditContract(JsonObject task, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var activity = task["activity"] as JsonArray ?? new JsonArray();
        task["activity"] = activity;
        if (activity.OfType<JsonObject>().Any(entry =>
            entry["kind"]?.GetValue<string>() == "Legacy" &&
            (entry["payload"] as JsonObject)?["sourceKind"]?.GetValue<string>() == "ExternalAuditContract" &&
            (entry["payload"] as JsonObject)?["text"]?.GetValue<string>() == text))
        {
            return;
        }

        const string baseId = "legacy-external-audit-contract";
        var existingIds = activity.OfType<JsonObject>()
            .Select(entry => entry["activityEntryId"]?.GetValue<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var activityEntryId = baseId;
        for (var suffix = 2; existingIds.Contains(activityEntryId); suffix++)
        {
            activityEntryId = $"{baseId}-{suffix}";
        }

        activity.Add(new JsonObject
        {
            ["activityEntryId"] = activityEntryId,
            ["actorId"] = "legacy-compatibility",
            ["actorKind"] = "ExternalFile",
            ["createdAt"] = task["updatedAt"]?.DeepClone() ?? task["createdAt"]?.DeepClone(),
            ["kind"] = "Legacy",
            ["payload"] = new JsonObject
            {
                ["sourceKind"] = "ExternalAuditContract",
                ["text"] = text
            }
        });
    }

    private static void UpgradeDerivatives(JsonObject attachment)
    {
        var derivatives = attachment["derivatives"] as JsonArray ?? new JsonArray();
        attachment["derivatives"] = derivatives;
        var sourceSha256 = attachment["sha256"]?.GetValue<string>() ?? new string('0', 64);
        var addedAt = attachment["addedAt"]?.GetValue<string>() ?? DateTimeOffset.UnixEpoch.ToString("O");
        foreach (var derivative in derivatives.OfType<JsonObject>())
        {
            if (!derivative.ContainsKey("status"))
            {
                derivative["status"] = "Ready";
                derivative["failureReason"] = null;
            }
        }

        foreach (var kind in new[] { "ExtractedText", "Ocr", "Preview" })
        {
            if (derivatives.OfType<JsonObject>().Any(item => item["kind"]?.GetValue<string>() == kind))
            {
                continue;
            }

            derivatives.Add(new JsonObject
            {
                ["derivativeId"] = $"derivative-{kind.ToLowerInvariant()}",
                ["kind"] = kind,
                ["status"] = "Pending",
                ["failureReason"] = null,
                ["relativePath"] = null,
                ["mediaType"] = null,
                ["byteLength"] = null,
                ["sha256"] = null,
                ["sourceSha256"] = sourceSha256,
                ["extractor"] = null,
                ["createdAt"] = addedAt
            });
        }
    }

    private static void NormalizeLegacyConversationActivity(JsonObject task)
    {
        var conversation = task["conversation"] as JsonObject ?? ExtractConversation(task);
        task["conversation"] = conversation;
        var messages = conversation["messages"] as JsonArray ?? new JsonArray();
        conversation["messages"] = messages;
        foreach (var message in messages.OfType<JsonObject>())
        {
            var actorKind = (message["author"] as JsonObject)?["actorKind"]?.GetValue<string>();
            var messageId = message["messageId"]?.GetValue<string>() ?? $"legacy-message-{Guid.NewGuid():N}";
            if (actorKind == "Agent" && message["agentRunId"] is null)
            {
                message["agentRunId"] = $"legacy-run-{messageId}";
            }
            else if (actorKind != "Agent")
            {
                message["agentRunId"] = null;
            }

            if (actorKind == "System" && (message["author"] as JsonObject)?["role"]?.GetValue<string>() == "Owner")
            {
                message["author"]!["role"] = "Worker";
            }
        }

        var existingIds = messages.OfType<JsonObject>()
            .Select(message => message["messageId"]?.GetValue<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var activity = task["activity"] as JsonArray ?? new JsonArray();
        foreach (var entry in activity.OfType<JsonObject>().ToArray())
        {
            var kind = entry["kind"]?.GetValue<string>();
            if (kind == "AgentSummary")
            {
                var markdown = (entry["payload"] as JsonObject)?["markdown"]?.DeepClone() ?? "Перенесённая сводка";
                entry["kind"] = "Legacy";
                entry["payload"] = new JsonObject { ["sourceKind"] = "AgentSummary", ["text"] = markdown };
                continue;
            }

            if (kind != "Comment")
            {
                continue;
            }

            var messageId = entry["activityEntryId"]?.GetValue<string>() ?? $"legacy-message-{Guid.NewGuid():N}";
            if (!existingIds.Contains(messageId))
            {
                var actorKind = entry["actorKind"]?.GetValue<string>() ?? "ExternalFile";
                messages.Add(new JsonObject
                {
                    ["messageId"] = messageId,
                    ["sequence"] = messages.Count + 1,
                    ["author"] = new JsonObject
                    {
                        ["actorId"] = entry["actorId"]?.DeepClone() ?? "legacy",
                        ["actorKind"] = actorKind,
                        ["role"] = "Worker"
                    },
                    ["createdAt"] = entry["createdAt"]?.DeepClone(),
                    ["replyToMessageId"] = null,
                    ["agentRunId"] = actorKind == "Agent" ? $"legacy-run-{messageId}" : null,
                    ["content"] = new JsonArray(new JsonObject
                    {
                        ["kind"] = "Markdown",
                        ["markdown"] = (entry["payload"] as JsonObject)?["markdown"]?.DeepClone() ?? "Перенесённый комментарий"
                    })
                });
                existingIds.Add(messageId);
            }

            activity.Remove(entry);
        }

        conversation["lastMessageSequence"] = messages.Count;
    }

    private static JsonObject ExtractConversation(JsonObject task)
    {
        var messages = new JsonArray();
        var activity = task["activity"] as JsonArray ?? new JsonArray();
        long sequence = 0;
        foreach (var entry in activity.OfType<JsonObject>()
            .Where(entry => entry["kind"]?.GetValue<string>() == "Comment").ToArray())
        {
            sequence++;
            var payload = entry["payload"] as JsonObject;
            messages.Add(new JsonObject
            {
                ["messageId"] = entry["activityEntryId"]?.DeepClone(),
                ["sequence"] = sequence,
                ["author"] = new JsonObject
                {
                    ["actorId"] = entry["actorId"]?.DeepClone(),
                    ["actorKind"] = entry["actorKind"]?.DeepClone(),
                    ["role"] = "Worker"
                },
                ["createdAt"] = entry["createdAt"]?.DeepClone(),
                ["replyToMessageId"] = null,
                ["agentRunId"] = entry["actorKind"]?.GetValue<string>() == "Agent"
                    ? $"legacy-run-{entry["activityEntryId"]?.GetValue<string>() ?? sequence.ToString()}"
                    : null,
                ["content"] = new JsonArray(new JsonObject
                {
                    ["kind"] = "Markdown",
                    ["markdown"] = payload?["markdown"]?.DeepClone() ?? "Перенесённый комментарий"
                })
            });
            activity.Remove(entry);
        }

        return new JsonObject
        {
            ["lastMessageSequence"] = sequence,
            ["messages"] = messages,
            ["contextCheckpoints"] = new JsonArray()
        };
    }

    private static JsonArray ConvertAccess(string access)
    {
        return access switch
        {
            "ReadOnly" => new JsonArray("WorkspaceRead"),
            "WorkspaceWrite" => new JsonArray("WorkspaceRead", "WorkspaceWrite"),
            "Network" => new JsonArray("WorkspaceRead", "Network"),
            "ExternalEffect" => new JsonArray("WorkspaceRead", "ExternalEffect"),
            _ => new JsonArray("WorkspaceRead")
        };
    }

    private static bool IsCanonicalProjectPath(string value, bool allowRoot)
    {
        if (allowRoot && value == ".")
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(value) && value.Length <= 4096 &&
            !Path.IsPathRooted(value) && !value.Contains('\\') && !value.Contains(':') &&
            value.Split('/').All(IsSafePathSegment);
    }

    private static bool IsSafePathSegment(string segment)
    {
        var stem = Path.GetFileNameWithoutExtension(segment);
        var reserved = stem.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(stem, "^(COM|LPT)[1-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return !string.IsNullOrWhiteSpace(segment) && segment is not ("." or "..") &&
            !segment.EndsWith(' ') && !segment.EndsWith('.') && !segment.Any(char.IsControl) &&
            segment.IndexOfAny(['<', '>', ':', '"', '/', '\\', '|', '?', '*']) < 0 && !reserved;
    }
}
