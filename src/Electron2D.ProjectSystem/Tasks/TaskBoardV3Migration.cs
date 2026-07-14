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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal sealed record TaskBoardV3BlobMigration(
    string SourcePath,
    string TargetPath,
    string Sha256,
    long ByteLength);

internal sealed record TaskBoardV3MigrationPlan(
    int SourceVersion,
    long SourceBoardRevision,
    JsonObject Board,
    IReadOnlyList<JsonObject> ActiveTasks,
    IReadOnlyList<JsonObject> CompletedTasks,
    IReadOnlyDictionary<string, string> SourceDigests,
    IReadOnlyList<TaskBoardV3BlobMigration> BlobMigrations,
    JsonObject Report,
    string ReportSha256);

internal static class TaskBoardV3Migration
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static TaskBoardV3MigrationPlan BuildPlan(string projectRoot, DateTimeOffset migratedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        var fullRoot = Path.GetFullPath(projectRoot);
        var boardPath = FullPath(fullRoot, ProjectTaskStorage.BoardDocumentPath);
        if (!File.Exists(boardPath))
        {
            throw new InvalidOperationException($"Taskboard '{ProjectTaskStorage.BoardDocumentPath}' was not found.");
        }

        var sourceDigests = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var boardBytes = File.ReadAllBytes(boardPath);
        sourceDigests[ProjectTaskStorage.BoardDocumentPath] = Hash(boardBytes);
        var sourceBoard = ProjectTaskSerializer.DeserializeBoard(
            ProjectTaskStorage.BoardDocumentPath,
            DecodeUtf8(boardBytes, ProjectTaskStorage.BoardDocumentPath));

        var activeSources = ReadTasks(fullRoot, ProjectTaskStorage.ActiveTasksDirectory, sourceDigests);
        var completedSources = ReadTasks(fullRoot, ProjectTaskStorage.CompletedTasksDirectory, sourceDigests);
        var allSources = activeSources.Concat(completedSources).ToArray();
        EnsureUniqueSourceIdentity(allSources);
        var uidByTaskId = allSources.ToDictionary(task => task.TaskId, task => task.TaskUid, StringComparer.Ordinal);

        var blobMigrations = new List<TaskBoardV3BlobMigration>();
        var active = activeSources.Select(task => ConvertTask(task, sourceBoard.BoardId, uidByTaskId, blobMigrations)).ToArray();
        var completed = completedSources.Select(task => ConvertTask(task, sourceBoard.BoardId, uidByTaskId, blobMigrations)).ToArray();
        AddBlobDigests(fullRoot, sourceDigests, blobMigrations);

        var board = ConvertBoard(sourceBoard, uidByTaskId, sourceDigests, migratedAt);
        var uidMapping = new JsonObject();
        foreach (var pair in uidByTaskId.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            uidMapping[pair.Key] = pair.Value;
        }

        var digestObject = new JsonObject();
        foreach (var pair in sourceDigests)
        {
            digestObject[pair.Key] = pair.Value;
        }

        var report = new JsonObject
        {
            ["format"] = "Electron2D.TaskMigrationReport",
            ["version"] = 2,
            ["sourceVersion"] = 2,
            ["targetVersion"] = 3,
            ["sourceBoardRevision"] = sourceBoard.Revision,
            ["boardId"] = sourceBoard.BoardId,
            ["activeTaskCount"] = active.Length,
            ["completedTaskCount"] = completed.Length,
            ["blobCount"] = blobMigrations.Count,
            ["uidMapping"] = uidMapping,
            ["sourceDigests"] = digestObject
        };
        var reportSha256 = ComputeReportDigest(report);
        report["reportSha256"] = reportSha256;
        RequiredObject(board["migration"])["reportSha256"] = reportSha256;

        return new TaskBoardV3MigrationPlan(
            2,
            sourceBoard.Revision,
            board,
            active,
            completed,
            sourceDigests,
            blobMigrations,
            report,
            reportSha256);
    }

    public static string Serialize(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.ToJsonString(IndentedJson).ReplaceLineEndings("\n") + "\n";
    }

    internal static string ComputeReportDigest(JsonObject report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var digestSource = report.DeepClone().AsObject();
        digestSource.Remove("reportSha256");
        return Hash(Encoding.UTF8.GetBytes(digestSource.ToJsonString()));
    }

    private static IReadOnlyList<ProjectTask> ReadTasks(
        string projectRoot,
        string relativeDirectory,
        IDictionary<string, string> sourceDigests)
    {
        var directory = FullPath(projectRoot, relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var tasks = new List<ProjectTask>();
        foreach (var filePath in Directory.EnumerateFiles(directory, "*.e2task", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativePath = NormalizeRelativePath(projectRoot, filePath);
            var bytes = File.ReadAllBytes(filePath);
            sourceDigests[relativePath] = Hash(bytes);
            tasks.Add(ProjectTaskSerializer.DeserializeTask(relativePath, DecodeUtf8(bytes, relativePath)));
        }

        return tasks;
    }

    private static void EnsureUniqueSourceIdentity(IReadOnlyList<ProjectTask> tasks)
    {
        if (tasks.GroupBy(task => task.TaskId, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("v2 migration source contains duplicate taskId values.");
        }

        if (tasks.Any(task => string.IsNullOrWhiteSpace(task.TaskUid)) ||
            tasks.GroupBy(task => task.TaskUid, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("v2 migration source contains missing or duplicate taskUid values.");
        }
    }

    private static JsonObject ConvertBoard(
        TaskBoard source,
        IReadOnlyDictionary<string, string> uidByTaskId,
        IReadOnlyDictionary<string, string> sourceDigests,
        DateTimeOffset migratedAt)
    {
        var nextNumber = Math.Max(
            source.IdPolicy.NextNumber,
            uidByTaskId.Keys
                .Where(taskId => taskId.StartsWith(source.IdPolicy.Prefix, StringComparison.Ordinal))
                .Select(taskId => long.TryParse(taskId.AsSpan(source.IdPolicy.Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                    ? number + 1
                    : 1)
                .DefaultIfEmpty(1)
                .Max());
        var digestObject = new JsonObject();
        foreach (var pair in sourceDigests.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            digestObject[pair.Key] = pair.Value;
        }

        var migratedGroups = new JsonArray();
        foreach (var scope in source.Groups
            .GroupBy(group => group.ParentGroupId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var index = 0;
            foreach (var group in scope.OrderBy(group => NormalizeRank(group.Rank), StringComparer.Ordinal)
                .ThenBy(group => group.GroupId, StringComparer.Ordinal))
            {
                index++;
                migratedGroups.Add(new JsonObject
                {
                    ["groupId"] = group.GroupId,
                    ["kind"] = group.Kind.ToString(),
                    ["title"] = group.Title,
                    ["description"] = group.Description,
                    ["parentGroupId"] = group.ParentGroupId,
                    ["rank"] = checked(index * 1000L).ToString("D12", CultureInfo.InvariantCulture)
                });
            }
        }

        var migratedPlacements = new JsonArray();
        foreach (var scope in source.Placements
            .GroupBy(placement => placement.GroupId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var index = 0;
            foreach (var placement in scope.OrderBy(placement => NormalizeRank(placement.Rank), StringComparer.Ordinal)
                .ThenBy(placement => placement.TaskId, StringComparer.Ordinal))
            {
                index++;
                migratedPlacements.Add(new JsonObject
                {
                    ["taskUid"] = ResolveUid(uidByTaskId, placement.TaskId, "placement"),
                    ["groupId"] = placement.GroupId,
                    ["rank"] = checked(index * 1000L).ToString("D12", CultureInfo.InvariantCulture)
                });
            }
        }

        var board = new JsonObject
        {
            ["format"] = "Electron2D.TaskBoard",
            ["version"] = 3,
            ["boardId"] = source.BoardId,
            ["revision"] = source.Revision,
            ["idPolicy"] = new JsonObject
            {
                ["prefix"] = source.IdPolicy.Prefix,
                ["padding"] = source.IdPolicy.Padding,
                ["nextNumber"] = nextNumber
            },
            ["attachmentPolicy"] = new JsonObject
            {
                ["perFileByteLimit"] = source.AttachmentPolicy.PerFileByteLimit,
                ["perTaskByteLimit"] = source.AttachmentPolicy.BoardByteLimit,
                ["boardByteLimit"] = source.AttachmentPolicy.BoardByteLimit
            },
            ["validationContract"] = CreateValidationContract(),
            ["migration"] = new JsonObject
            {
                ["sourceVersion"] = 2,
                ["reportPath"] = ".taskboard/.migration/v2/report.json",
                ["reportSha256"] = new string('0', 64),
                ["sourceBoardRevision"] = source.Revision,
                ["sourceDigests"] = digestObject,
                ["migratedAt"] = FormatDate(migratedAt),
                ["finalized"] = false
            },
            ["tags"] = new JsonArray(source.Tags
                .OrderBy(tag => tag.TagId, StringComparer.Ordinal)
                .Select(tag => (JsonNode)new JsonObject
                {
                    ["tagId"] = tag.TagId,
                    ["name"] = tag.Name,
                    ["color"] = tag.Color.ToString()
                }).ToArray()),
            ["groups"] = migratedGroups,
            ["placements"] = migratedPlacements
        };
        return board;
    }

    internal static JsonObject CreateValidationContract()
    {
        return new JsonObject
        {
            ["semanticValidator"] = "TaskBoardSemanticValidatorV3",
            ["transitionValidator"] = "TaskTransitionValidatorV3",
            ["contextBuilder"] = "AgentContextBuilderV3",
            ["executionPolicy"] = "TaskExecutionPolicyV3",
            ["formatAssertions"] = true
        };
    }

    private static JsonObject ConvertTask(
        ProjectTask source,
        string boardId,
        IReadOnlyDictionary<string, string> uidByTaskId,
        ICollection<TaskBoardV3BlobMigration> blobMigrations)
    {
        var (timelineStart, timelineEnd) = GetTimelineBounds(source);
        var targetStatus = source.Status == ProjectTaskStatus.Blocked &&
            source.BlockingReasons.All(reason => reason == TaskBlockingReason.Dependency)
            ? ProjectTaskStatus.Ready
            : source.Status;
        var relations = new JsonArray();
        for (var index = 0; index < source.Dependencies.Count; index++)
        {
            relations.Add(new JsonObject
            {
                ["relationId"] = $"relation-{index + 1:D4}",
                ["kind"] = "DependsOn",
                ["targetTaskUid"] = ResolveUid(uidByTaskId, source.Dependencies[index], $"dependency of {source.TaskId}")
            });
        }

        var blockers = new JsonArray();
        for (var index = 0; index < source.BlockingReasons.Count; index++)
        {
            var reason = source.BlockingReasons[index];
            if (reason == TaskBlockingReason.Dependency)
            {
                continue;
            }

            blockers.Add(new JsonObject
            {
                ["blockerId"] = $"blocker-{index + 1:D4}",
                ["kind"] = reason.ToString(),
                ["reason"] = $"Перенесено из v2: {reason}",
                ["state"] = targetStatus == ProjectTaskStatus.Blocked ? "Active" : "Resolved",
                ["createdAt"] = FormatDate(timelineStart),
                ["createdBy"] = source.CreatedBy,
                ["resolvedAt"] = targetStatus == ProjectTaskStatus.Blocked ? null : FormatDate(timelineEnd),
                ["resolvedBy"] = targetStatus == ProjectTaskStatus.Blocked ? null : "migration"
            });
        }

        var task = new JsonObject
        {
            ["format"] = "Electron2D.TaskFile",
            ["version"] = 3,
            ["boardId"] = boardId,
            ["taskUid"] = source.TaskUid,
            ["revision"] = source.Revision,
            ["taskId"] = source.TaskId,
            ["legacyAliases"] = StringArray(source.LegacyAliases),
            ["title"] = source.Title,
            ["description"] = source.Description,
            ["status"] = targetStatus.ToString(),
            ["acceptanceState"] = ConvertAcceptanceState(source, targetStatus),
            ["priority"] = source.Priority,
            ["tagIds"] = StringArray(source.Labels),
            ["deadline"] = source.Deadline?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["createdBy"] = source.CreatedBy,
            ["assignee"] = source.Assignee,
            ["parentTaskUid"] = source.ParentTaskId is null ? null : ResolveUid(uidByTaskId, source.ParentTaskId, $"parent of {source.TaskId}"),
            ["relations"] = relations,
            ["acceptanceCriteria"] = ConvertCriteria(source.AcceptanceCriteria, source.Attachments, targetStatus == ProjectTaskStatus.Done),
            ["blockers"] = blockers,
            ["activity"] = ConvertActivity(source),
            ["auditRuns"] = new JsonArray(),
            ["conversation"] = ConvertConversation(source),
            ["contextSnapshot"] = null,
            ["links"] = ConvertLinks(source),
            ["executionContract"] = ConvertExecutionContract(source.ExecutionContract),
            ["attachments"] = ConvertAttachments(source, blobMigrations),
            ["previewAttachmentId"] = source.PreviewAttachmentId,
            ["legacySourceFragments"] = ConvertLegacyFragments(source.LegacySourceFragments),
            ["createdAt"] = FormatDate(timelineStart),
            ["updatedAt"] = FormatDate(timelineEnd),
            ["submittedAt"] = source.Status == ProjectTaskStatus.Done
                ? FormatNullableDate(source.SubmittedAt ?? source.CompletedAt ?? source.UpdatedAt)
                : source.Status == ProjectTaskStatus.Review && source.AcceptanceState == ProjectTaskAcceptanceState.Submitted
                    ? FormatNullableDate(source.SubmittedAt ?? source.UpdatedAt)
                    : null,
            ["completedAt"] = source.Status == ProjectTaskStatus.Done ? FormatNullableDate(source.CompletedAt ?? source.UpdatedAt) : null,
            ["acceptedAt"] = source.Status == ProjectTaskStatus.Done ? FormatNullableDate(source.AcceptedAt ?? source.CompletedAt ?? source.UpdatedAt) : null,
            ["acceptedBy"] = source.Status == ProjectTaskStatus.Done ? source.AcceptedBy ?? "legacy-human" : null,
            ["cancelledAt"] = source.Status == ProjectTaskStatus.Cancelled ? FormatDate(source.UpdatedAt) : null,
            ["cancellationReason"] = source.Status == ProjectTaskStatus.Cancelled ? source.CancellationReason ?? "Перенесено из v2 без причины" : null,
            ["archivedAt"] = source.ArchivedAt is null ? null : FormatDate(source.ArchivedAt.Value),
            ["archivedBy"] = source.ArchivedAt is null ? null : source.ArchivedBy ?? "migration"
        };
        EnsureMigratedTerminalActivity(task, source);
        return TaskBoardV3Compatibility.UpgradeTask(task);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) GetTimelineBounds(ProjectTask source)
    {
        var values = new List<DateTimeOffset>
        {
            source.CreatedAt,
            source.UpdatedAt
        };
        values.AddRange(source.Activity.Select(entry => entry.CreatedAt));
        AddIfPresent(values, source.SubmittedAt);
        AddIfPresent(values, source.CompletedAt);
        AddIfPresent(values, source.AcceptedAt);
        AddIfPresent(values, source.ArchivedAt);
        return (values.Min(), values.Max());
    }

    private static void AddIfPresent(ICollection<DateTimeOffset> values, DateTimeOffset? value)
    {
        if (value is not null)
        {
            values.Add(value.Value);
        }
    }

    private static JsonArray ConvertCriteria(
        IEnumerable<AcceptanceCriterion> criteria,
        IReadOnlyCollection<TaskAttachment> attachments,
        bool forcePassed)
    {
        var attachmentIds = attachments.Select(attachment => attachment.AttachmentId).ToHashSet(StringComparer.Ordinal);
        return new JsonArray(criteria.Select(criterion =>
        {
            var evidence = new JsonArray(criterion.EvidenceLinks.Select(value => ConvertEvidence(value, attachmentIds)).ToArray());
            if (forcePassed && evidence.Count == 0)
            {
                evidence.Add(new JsonObject { ["kind"] = "File", ["path"] = ".taskboard/.migration/v2/report.json" });
            }

            return (JsonNode)new JsonObject
            {
                ["criterionId"] = criterion.CriterionId,
                ["description"] = criterion.Description,
                ["state"] = forcePassed ? "Passed" : criterion.State.ToString(),
                ["evidence"] = evidence
            };
        }).ToArray());
    }

    private static JsonNode ConvertEvidence(string evidence, IReadOnlySet<string> attachmentIds)
    {
        if (attachmentIds.Contains(evidence))
        {
            return new JsonObject { ["kind"] = "Attachment", ["attachmentId"] = evidence };
        }

        if (Uri.TryCreate(evidence, UriKind.Absolute, out var uri))
        {
            return new JsonObject { ["kind"] = "Uri", ["uri"] = uri.AbsoluteUri };
        }

        return new JsonObject { ["kind"] = "File", ["path"] = NormalizeProjectPath(evidence) };
    }

    private static JsonArray ConvertActivity(ProjectTask source)
    {
        var activity = new JsonArray();
        foreach (var entry in source.Activity.OrderBy(entry => entry.CreatedAt).ThenBy(entry => entry.ActivityEntryId, StringComparer.Ordinal))
        {
            JsonObject payload;
            string kind;
            if (entry.Kind == TaskActivityKind.Comment)
            {
                continue;
            }

            if (entry.Kind == TaskActivityKind.AgentSummary)
            {
                kind = entry.Kind.ToString();
                payload = new JsonObject { ["markdown"] = entry.Payload };
            }
            else if (entry.Kind == TaskActivityKind.AcceptanceResult &&
                source.Status == ProjectTaskStatus.Done &&
                entry.ActorKind == PrincipalKind.Human)
            {
                kind = "AcceptanceResult";
                payload = new JsonObject
                {
                    ["decision"] = "Accepted",
                    ["reason"] = entry.Payload,
                    ["authorityActorId"] = entry.ActorId,
                    ["authorityRole"] = "Owner",
                    ["auditRunId"] = null
                };
            }
            else
            {
                kind = "Legacy";
                payload = new JsonObject
                {
                    ["sourceKind"] = entry.Kind.ToString(),
                    ["text"] = entry.Payload
                };
            }

            activity.Add(new JsonObject
            {
                ["activityEntryId"] = entry.ActivityEntryId,
                ["actorId"] = entry.ActorId,
                ["actorKind"] = entry.ActorKind.ToString(),
                ["createdAt"] = FormatDate(entry.CreatedAt),
                ["kind"] = kind,
                ["payload"] = payload
            });
        }

        return activity;
    }

    private static JsonObject ConvertConversation(ProjectTask source)
    {
        var messages = new JsonArray();
        long sequence = 0;
        foreach (var entry in source.Activity.Where(entry => entry.Kind == TaskActivityKind.Comment)
            .OrderBy(entry => entry.CreatedAt).ThenBy(entry => entry.ActivityEntryId, StringComparer.Ordinal))
        {
            sequence++;
            messages.Add(new JsonObject
            {
                ["messageId"] = entry.ActivityEntryId,
                ["sequence"] = sequence,
                ["author"] = new JsonObject
                {
                    ["actorId"] = entry.ActorId,
                    ["actorKind"] = entry.ActorKind.ToString(),
                    ["role"] = "Worker"
                },
                ["createdAt"] = FormatDate(entry.CreatedAt),
                ["replyToMessageId"] = null,
                ["agentRunId"] = null,
                ["content"] = new JsonArray(new JsonObject { ["kind"] = "Markdown", ["markdown"] = entry.Payload })
            });
        }

        return new JsonObject
        {
            ["lastMessageSequence"] = sequence,
            ["messages"] = messages,
            ["contextCheckpoints"] = new JsonArray()
        };
    }

    private static void EnsureMigratedTerminalActivity(JsonObject task, ProjectTask source)
    {
        if (source.Status != ProjectTaskStatus.Done)
        {
            return;
        }

        var activity = RequiredArray(task, "activity");
        if (activity.Select(RequiredObject).Any(entry =>
            RequiredString(entry, "kind") == "AcceptanceResult" &&
            RequiredString(entry, "actorKind") == "Human"))
        {
            return;
        }

        var actor = source.AcceptedBy ?? "legacy-human";
        activity.Add(new JsonObject
        {
            ["activityEntryId"] = "migration-acceptance",
            ["actorId"] = actor,
            ["actorKind"] = "Human",
            ["createdAt"] = FormatDate(source.AcceptedAt ?? source.CompletedAt ?? source.UpdatedAt),
            ["kind"] = "AcceptanceResult",
            ["payload"] = new JsonObject
            {
                ["decision"] = "Accepted",
                ["reason"] = "Человеческая приёмка перенесена из terminal state v2.",
                ["authorityActorId"] = actor,
                ["authorityRole"] = "Owner",
                ["auditRunId"] = null
            }
        });
    }

    private static JsonArray ConvertLinks(ProjectTask source)
    {
        var values = new List<(string Kind, string Value)>();
        values.AddRange(source.LinkedTransactions.Select(value => ("Transaction", value)));
        values.AddRange(source.LinkedJobs.Select(value => ("Job", value)));
        values.AddRange(source.LinkedDiagnostics.Select(value => ("Diagnostic", value)));
        values.AddRange(source.LinkedArtifacts.Select(value => (ClassifyArtifact(value), value)));
        values.AddRange(source.LinkedScenesResourcesAndNodes.Select(value => ("Node", value)));
        return new JsonArray(values.Select((item, index) => (JsonNode)new JsonObject
        {
            ["linkId"] = $"link-{index + 1:D4}",
            ["kind"] = item.Kind,
            ["value"] = item.Value
        }).ToArray());
    }

    private static string ClassifyArtifact(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return "Uri";
        }

        if (value.EndsWith("/", StringComparison.Ordinal) || value.EndsWith('\\'))
        {
            return "Directory";
        }

        return "File";
    }

    private static JsonObject ConvertExecutionContract(TaskExecutionContract source)
    {
        return new JsonObject
        {
            ["taskType"] = source.TaskType,
            ["readyToStart"] = StringArray(source.ReadyToStart),
            ["stopConditions"] = StringArray(source.StopConditions),
            ["allowedChanges"] = StringArray(source.AllowedChanges),
            ["forbiddenChanges"] = StringArray(source.ForbiddenChanges),
            ["requiredOutputs"] = StringArray(source.RequiredOutputs),
            ["commands"] = new JsonArray(source.RequiredCommands.Select((command, index) => (JsonNode)new JsonObject
            {
                ["commandId"] = $"command-{index + 1:D4}",
                ["kind"] = "LegacyShell",
                ["text"] = command,
                ["execution"] = "ForbiddenUntilReviewed"
            }).ToArray()),
            ["externalAudit"] = ConvertExternalAudit(source.ExternalAudit)
        };
    }

    internal static JsonObject ConvertExternalAudit(string? legacyValue)
    {
        if (string.IsNullOrWhiteSpace(legacyValue) ||
            legacyValue.Contains("not required", StringComparison.OrdinalIgnoreCase) ||
            legacyValue.Contains("not-required", StringComparison.OrdinalIgnoreCase) ||
            legacyValue.Contains("не требуется", StringComparison.OrdinalIgnoreCase))
        {
            return new JsonObject
            {
                ["mode"] = "None",
                ["independence"] = "NotRequired",
                ["instructions"] = null,
                ["requiredVerdicts"] = new JsonArray()
            };
        }

        return new JsonObject
        {
            ["mode"] = "Single",
            ["independence"] = "DifferentActor",
            ["instructions"] = legacyValue,
            ["requiredVerdicts"] = new JsonArray("Primary")
        };
    }

    private static JsonArray ConvertAttachments(
        ProjectTask source,
        ICollection<TaskBoardV3BlobMigration> blobMigrations)
    {
        var result = new JsonArray();
        foreach (var attachment in source.Attachments.OrderBy(attachment => attachment.AttachmentId, StringComparer.Ordinal))
        {
            var originalName = Path.GetFileName(attachment.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var extension = Path.GetExtension(originalName).ToLowerInvariant();
            var safeName = attachment.Sha256 + extension;
            var targetPath = $".taskboard/attachments/{source.TaskUid}/{attachment.AttachmentId}/{safeName}";
            blobMigrations.Add(new TaskBoardV3BlobMigration(
                attachment.RelativePath,
                targetPath,
                attachment.Sha256,
                attachment.ByteLength));
            result.Add(new JsonObject
            {
                ["attachmentId"] = attachment.AttachmentId,
                ["displayName"] = attachment.DisplayName,
                ["relativePath"] = targetPath,
                ["mediaType"] = attachment.MediaType,
                ["byteLength"] = attachment.ByteLength,
                ["sha256"] = attachment.Sha256,
                ["addedAt"] = FormatDate(attachment.AddedAt),
                ["addedBy"] = attachment.AddedBy,
                ["derivatives"] = new JsonArray()
            });
        }

        return result;
    }

    private static JsonArray ConvertLegacyFragments(IEnumerable<LegacySourceFragment> fragments)
    {
        return new JsonArray(fragments.Select(fragment => (JsonNode)new JsonObject
        {
            ["sourcePath"] = NormalizeProjectPath(fragment.SourcePath),
            ["byteOffset"] = fragment.ByteOffset,
            ["byteLength"] = fragment.ByteLength,
            ["encoding"] = fragment.Encoding,
            ["hasBom"] = fragment.HasBom,
            ["lineEnding"] = fragment.LineEnding,
            ["sha256"] = fragment.Sha256,
            ["markdown"] = fragment.Markdown
        }).ToArray());
    }

    private static void AddBlobDigests(
        string projectRoot,
        IDictionary<string, string> sourceDigests,
        IReadOnlyList<TaskBoardV3BlobMigration> blobMigrations)
    {
        foreach (var blob in blobMigrations.OrderBy(blob => blob.SourcePath, StringComparer.Ordinal))
        {
            var fullPath = FullPath(projectRoot, blob.SourcePath);
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Attachment blob '{blob.SourcePath}' was not found.");
            }

            var bytes = File.ReadAllBytes(fullPath);
            var digest = Hash(bytes);
            if (bytes.LongLength != blob.ByteLength || !string.Equals(digest, blob.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Attachment blob '{blob.SourcePath}' does not match v2 metadata.");
            }

            sourceDigests[blob.SourcePath] = digest;
        }
    }

    private static string ConvertAcceptanceState(ProjectTask source, ProjectTaskStatus targetStatus)
    {
        return targetStatus switch
        {
            ProjectTaskStatus.Ready => "NotSubmitted",
            ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked =>
                source.AcceptanceState == ProjectTaskAcceptanceState.ChangesRequested ? "ChangesRequested" : "NotSubmitted",
            ProjectTaskStatus.Review =>
                source.AcceptanceState == ProjectTaskAcceptanceState.Submitted ? "Submitted" : "InternalReview",
            ProjectTaskStatus.Done => "Accepted",
            ProjectTaskStatus.Cancelled => "Cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };
    }

    private static string NormalizeRank(string rank)
    {
        if (rank.Length > 12 || rank.Any(character => character is < '0' or > '9'))
        {
            throw new InvalidOperationException($"v2 rank '{rank}' cannot be represented as a canonical v3 rank.");
        }

        return rank.PadLeft(12, '0');
    }

    private static string ResolveUid(
        IReadOnlyDictionary<string, string> uidByTaskId,
        string taskId,
        string relation)
    {
        if (!uidByTaskId.TryGetValue(taskId, out var taskUid))
        {
            throw new InvalidOperationException($"v2 {relation} references unknown taskId '{taskId}'.");
        }

        return taskUid;
    }

    private static JsonArray StringArray(IEnumerable<string> values)
    {
        return new JsonArray(values.Select(value => (JsonNode)JsonValue.Create(value)!).ToArray());
    }

    private static string NormalizeProjectPath(string value)
    {
        return value.Replace('\\', '/').TrimStart('/');
    }

    private static string DecodeUtf8(byte[] bytes, string relativePath)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidOperationException($"Migration source '{relativePath}' is not valid UTF-8.", exception);
        }
    }

    private static string FullPath(string projectRoot, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string NormalizeRelativePath(string projectRoot, string fullPath)
    {
        return Path.GetRelativePath(projectRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string Hash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string? FormatNullableDate(DateTimeOffset? value)
    {
        return value is null ? null : FormatDate(value.Value);
    }

    private static JsonObject RequiredObject(JsonNode? value)
    {
        return value as JsonObject ?? throw new InvalidOperationException("Expected object while building v3 migration.");
    }

    private static JsonArray RequiredArray(JsonObject value, string propertyName)
    {
        return value[propertyName] as JsonArray ?? throw new InvalidOperationException($"Expected array '{propertyName}' while building v3 migration.");
    }

    private static string RequiredString(JsonObject value, string propertyName)
    {
        return value[propertyName]?.GetValue<string>() ?? throw new InvalidOperationException($"Expected string '{propertyName}' while building v3 migration.");
    }
}
