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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Electron2D.ProjectSystem;

internal sealed class TaskBoardV3ValidationException : FormatException
{
    public TaskBoardV3ValidationException(string code, string message)
        : base($"{code}: {message}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    public string Code { get; }
}

[Flags]
internal enum TaskBoardV3Capability
{
    None = 0,
    EditTask = 1 << 0,
    EditBoard = 1 << 1,
    ChangeStatus = 1 << 2,
    SubmitForAcceptance = 1 << 3,
    AcceptanceDecision = 1 << 4,
    HumanAcceptance = AcceptanceDecision,
    Archive = 1 << 5,
    TrustedReopen = 1 << 6,
    Migrate = 1 << 7
}

internal enum TaskBoardV3Role
{
    Worker,
    Auditor,
    Owner
}

internal sealed record TaskBoardV3MutationContext(
    string ActorId,
    TaskBoardV3Capability Capabilities,
    TaskBoardV3Role Role = TaskBoardV3Role.Worker,
    string ActorKind = "Agent",
    string SessionId = "local",
    string Origin = "internal",
    long? ExpectedRevision = null,
    long? ExpectedLastMessageSequence = null,
    long? ExpectedLastActivitySequence = null)
{
    public bool Has(TaskBoardV3Capability capability)
    {
        return (Capabilities & capability) == capability;
    }
}

internal static class TaskBoardV3SchemaValidator
{
    private const string CodePrefix = "E2D-TASK-V3-SCHEMA-";

    private static readonly string[] TaskProperties =
    [
        "format", "version", "boardId", "taskUid", "revision", "taskId", "legacyAliases", "title", "description",
        "status", "acceptanceState", "priority", "tagIds", "deadline", "createdBy", "assignee", "parentTaskUid", "relations",
        "acceptanceCriteria", "blockers", "lastActivitySequence", "activity", "auditRuns", "conversation", "contextSnapshot", "workspaceChanges", "links", "executionContract", "attachments", "previewAttachmentId",
        "legacySourceFragments", "createdAt", "updatedAt", "submittedAt", "completedAt", "acceptedAt", "acceptedBy",
        "cancelledAt", "cancellationReason", "archivedAt", "archivedBy"
    ];

    private static readonly string[] BoardProperties =
    [
        "format", "version", "boardId", "revision", "idPolicy", "attachmentPolicy", "validationContract", "migration", "tags", "groups", "placements"
    ];

    public static void ValidateTask(JsonObject task)
    {
        ArgumentNullException.ThrowIfNull(task);
        RequireClosedObject(task, TaskProperties);
        RequireConst(task, "format", "Electron2D.TaskFile");
        RequireInteger(task, "version", 3);
        RequirePositiveInteger(task, "revision");
        RequireBoundedString(task, "boardId", 128, nonBlank: true);
        RequirePattern(task, "taskUid", "^task-[A-Za-z0-9][A-Za-z0-9._-]{0,127}$");
        RequireBoundedString(task, "taskId", 128, nonBlank: true);
        RequireBoundedString(task, "title", 512, nonBlank: true);
        RequireBoundedString(task, "description", 262144, nonBlank: false);
        RequireEnum(task, "status", "Ready", "InProgress", "Blocked", "Review", "Done", "Cancelled");
        RequireEnum(task, "acceptanceState", "NotSubmitted", "InternalReview", "Submitted", "Accepted", "ChangesRequested", "Cancelled");
        RequireBoundedString(task, "priority", 64, nonBlank: true);
        RequireBoundedString(task, "createdBy", 16384, nonBlank: true);
        RequireNullableBoundedString(task, "assignee", 16384);
        RequireArray(task, "legacyAliases", 256);
        RequireArray(task, "tagIds", 1024);
        RequireArray(task, "relations", 1024);
        RequireArray(task, "acceptanceCriteria", 256);
        RequireArray(task, "blockers", 256);
        if (ReadInteger(task, "lastActivitySequence") < 0)
        {
            throw Error("ACTIVITY-SEQUENCE", "lastActivitySequence cannot be negative.");
        }
        RequireArray(task, "activity", int.MaxValue);
        RequireArray(task, "auditRuns", int.MaxValue);
        RequireArray(task, "links", 1024);
        RequireArray(task, "attachments", 1024);
        RequireArray(task, "legacySourceFragments", 32);
        ValidateStringArray(task, "legacyAliases", 128, 256);
        ValidateStringArray(task, "tagIds", 128, 1024);
        RequireUniqueStrings(task, "legacyAliases");
        RequireUniqueStrings(task, "tagIds");
        ValidateRelations(task);
        ValidateCriteria(task);
        ValidateBlockers(task);
        ValidateActivity(task);
        ValidateAuditRuns(task);
        ValidateConversation(task);
        ValidateContextSnapshot(task);
        ValidateWorkspaceChanges(task);
        ValidateLinks(task);
        ValidateExecutionContract(RequireObject(task, "executionContract"));
        ValidateAttachments(task);
        ValidateLegacyFragments(task);
        RequireNullableDate(task, "deadline");
        RequireNullablePattern(task, "parentTaskUid", "^task-[A-Za-z0-9][A-Za-z0-9._-]{0,127}$");
        RequireNullableBoundedString(task, "previewAttachmentId", 128);
        RequireDateTime(task, "createdAt", nullable: false);
        RequireDateTime(task, "updatedAt", nullable: false);
        RequireDateTime(task, "submittedAt", nullable: true);
        RequireDateTime(task, "completedAt", nullable: true);
        RequireDateTime(task, "acceptedAt", nullable: true);
        RequireNullableBoundedString(task, "acceptedBy", 16384);
        RequireDateTime(task, "cancelledAt", nullable: true);
        RequireNullableBoundedString(task, "cancellationReason", 16384);
        RequireDateTime(task, "archivedAt", nullable: true);
        RequireNullableBoundedString(task, "archivedBy", 16384);
        ValidateLifecycle(task);
    }

    public static void ValidateBoard(JsonObject board)
    {
        ArgumentNullException.ThrowIfNull(board);
        RequireClosedObject(board, BoardProperties);
        RequireConst(board, "format", "Electron2D.TaskBoard");
        RequireInteger(board, "version", 3);
        RequirePositiveInteger(board, "revision");
        RequireBoundedString(board, "boardId", 128, nonBlank: true);
        RequireArray(board, "tags", 4096);
        RequireArray(board, "groups", 4096);
        RequireArray(board, "placements", 20000);
        var idPolicy = RequireObject(board, "idPolicy");
        RequireClosedObject(idPolicy, ["prefix", "padding", "nextNumber"]);
        RequirePattern(idPolicy, "prefix", "^[A-Za-z0-9][A-Za-z0-9._-]{0,31}$");
        RequirePositiveInteger(idPolicy, "padding");
        RequirePositiveInteger(idPolicy, "nextNumber");
        var attachmentPolicy = RequireObject(board, "attachmentPolicy");
        RequireClosedObject(attachmentPolicy, ["perFileByteLimit", "perTaskByteLimit", "boardByteLimit"]);
        RequirePositiveInteger(attachmentPolicy, "perFileByteLimit");
        RequirePositiveInteger(attachmentPolicy, "perTaskByteLimit");
        RequirePositiveInteger(attachmentPolicy, "boardByteLimit");
        ValidateValidationContract(board);
        ValidateMigration(board);
        ValidateBoardCatalog(board);
    }

    private static void ValidateMigration(JsonObject board)
    {
        if (board["migration"] is null)
        {
            return;
        }

        var migration = RequireObject(board, "migration");
        RequireClosedObject(migration, ["sourceVersion", "reportPath", "reportSha256", "sourceBoardRevision", "sourceDigests", "migratedAt", "finalized"]);
        RequireInteger(migration, "sourceVersion", 2);
        ValidateProjectRelativePath(RequireString(migration, "reportPath"), "migration.reportPath");
        RequirePattern(migration, "reportSha256", "^[a-f0-9]{64}$");
        RequirePositiveInteger(migration, "sourceBoardRevision");
        RequireDateTime(migration, "migratedAt", nullable: false);
        if (migration["finalized"] is not JsonValue finalized || !finalized.TryGetValue<bool>(out _))
        {
            throw Error("TYPE", "migration.finalized must be a boolean.");
        }

        var digests = RequireObject(migration, "sourceDigests");
        if (digests.Count > 20000)
        {
            throw Error("LIMIT", "migration.sourceDigests exceeds 20000 entries.");
        }

        foreach (var digest in digests)
        {
            if (string.IsNullOrWhiteSpace(digest.Key) || digest.Key.Length > 4096 || !IsProjectRelativePath(digest.Key) || digest.Value is not JsonValue hash ||
                !hash.TryGetValue<string>(out var text) || !Regex.IsMatch(text, "^[a-f0-9]{64}$", RegexOptions.CultureInvariant))
            {
                throw Error("TYPE", "migration.sourceDigests contains an invalid path or SHA-256 value.");
            }
        }
    }

    private static void ValidateValidationContract(JsonObject board)
    {
        var contract = RequireObject(board, "validationContract");
        RequireClosedObject(contract, ["semanticValidator", "transitionValidator", "contextBuilder", "executionPolicy", "formatAssertions"]);
        RequireConst(contract, "semanticValidator", "TaskBoardSemanticValidatorV3");
        RequireConst(contract, "transitionValidator", "TaskTransitionValidatorV3");
        RequireConst(contract, "contextBuilder", "AgentContextBuilderV3");
        RequireConst(contract, "executionPolicy", "TaskExecutionPolicyV3");
        if (contract["formatAssertions"] is not JsonValue formatAssertions ||
            !formatAssertions.TryGetValue<bool>(out var enabled) || !enabled)
        {
            throw Error("FORMAT-ASSERTIONS", "validationContract.formatAssertions must be true.");
        }
    }

    private static void ValidateRelations(JsonObject task)
    {
        foreach (var relation in RequireArrayValue(task, "relations"))
        {
            var value = RequireObject(relation, "relations item");
            RequireClosedObject(value, ["relationId", "kind", "targetTaskUid"]);
            RequireBoundedString(value, "relationId", 128, nonBlank: true);
            RequireEnum(value, "kind", "DependsOn", "RelatedTo", "Duplicates", "Supersedes");
            RequirePattern(value, "targetTaskUid", "^task-[A-Za-z0-9][A-Za-z0-9._-]{0,127}$");
        }
    }

    private static void ValidateCriteria(JsonObject task)
    {
        foreach (var criterion in RequireArrayValue(task, "acceptanceCriteria"))
        {
            var value = RequireObject(criterion, "acceptanceCriteria item");
            RequireClosedObject(value, ["criterionId", "description", "state", "evidence"]);
            RequireBoundedString(value, "criterionId", 128, nonBlank: true);
            RequireBoundedString(value, "description", 16384, nonBlank: true);
            RequireEnum(value, "state", "Open", "Passed", "Failed");
            if (value["evidence"] is not JsonArray evidence || evidence.Count > 256)
            {
                throw Error("TYPE", "acceptanceCriteria evidence must be an array of at most 256 items.");
            }

            foreach (var node in evidence)
            {
                var item = RequireObject(node, "evidence item");
                var kind = RequireEnum(item, "kind", "File", "Uri", "Attachment");
                var field = kind switch { "File" => "path", "Uri" => "uri", _ => "attachmentId" };
                RequireClosedObject(item, ["kind", field]);
                RequireBoundedString(item, field, 4096, nonBlank: true);
                if (field == "path")
                {
                    ValidateProjectRelativePath(RequireString(item, field), "criterion evidence path");
                }
                else if (field == "uri" && !Uri.TryCreate(RequireString(item, field), UriKind.Absolute, out _))
                {
                    throw Error("URI", "Criterion evidence URI must be absolute.");
                }
            }

            if (RequireString(value, "state") == "Passed" && evidence.Count == 0)
            {
                throw Error("EVIDENCE-REQUIRED", "Passed acceptance criterion requires at least one evidence link.");
            }
        }
    }

    private static void ValidateBlockers(JsonObject task)
    {
        foreach (var blocker in RequireArrayValue(task, "blockers"))
        {
            var value = RequireObject(blocker, "blockers item");
            RequireClosedObject(value, ["blockerId", "kind", "reason", "state", "createdAt", "createdBy", "resolvedAt", "resolvedBy"]);
            RequireBoundedString(value, "blockerId", 128, nonBlank: true);
            RequireEnum(value, "kind", "Environment", "Decision", "External", "Manual");
            RequireBoundedString(value, "reason", 16384, nonBlank: true);
            RequireEnum(value, "state", "Active", "Resolved");
            RequireDateTime(value, "createdAt", nullable: false);
            RequireBoundedString(value, "createdBy", 16384, nonBlank: true);
            RequireDateTime(value, "resolvedAt", nullable: true);
            RequireNullableBoundedString(value, "resolvedBy", 16384);
            var state = RequireString(value, "state");
            var resolutionPair = value["resolvedAt"] is not null && value["resolvedBy"] is not null;
            if (state == "Active" && (value["resolvedAt"] is not null || value["resolvedBy"] is not null) ||
                state == "Resolved" && !resolutionPair)
            {
                throw Error("BLOCKER-RESOLUTION", "Active blocker has no resolution fields; Resolved blocker requires both fields.");
            }

            if (state == "Resolved" &&
                DateTimeOffset.Parse(RequireString(value, "resolvedAt"), CultureInfo.InvariantCulture) <
                DateTimeOffset.Parse(RequireString(value, "createdAt"), CultureInfo.InvariantCulture))
            {
                throw Error("TIMESTAMP-ORDER", "Blocker resolution cannot precede blocker creation.");
            }
        }
    }

    private static void ValidateActivity(JsonObject task)
    {
        long expectedSequence = 1;
        foreach (var activity in RequireArrayValue(task, "activity"))
        {
            var value = RequireObject(activity, "activity item");
            RequireClosedObject(value, ["activityEntryId", "sequence", "actorId", "actorKind", "createdAt", "kind", "payload"]);
            RequireBoundedString(value, "activityEntryId", 128, nonBlank: true);
            if (ReadInteger(value, "sequence") != expectedSequence++)
            {
                throw Error("ACTIVITY-SEQUENCE", "Activity sequences must be contiguous and start at one.");
            }
            RequireBoundedString(value, "actorId", 16384, nonBlank: true);
            RequireEnum(value, "actorKind", "Human", "Agent", "Cli", "ExternalFile", "System", "Test");
            RequireDateTime(value, "createdAt", nullable: false);
            var storedKind = RequireString(value, "kind");
            if (storedKind is "Comment" or "AgentSummary")
            {
                throw Error("LEGACY-CONVERSATION-ACTIVITY", "Conversation content is stored only in conversation; legacy activity must use kind Legacy.");
            }

            var kind = RequireEnum(value, "kind", "StatusChange", "TestResult", "Blocker", "Decision", "AcceptanceResult", "TaskPatched", "WorkspaceChangesUpdated", "Investigation", "Legacy");
            var payload = RequireObject(value, "payload");
            switch (kind)
            {
                case "StatusChange":
                    RequireClosedObject(payload, ["previous", "next", "reason"]);
                    RequireEnum(payload, "previous", "Ready", "InProgress", "Blocked", "Review", "Done", "Cancelled");
                    RequireEnum(payload, "next", "Ready", "InProgress", "Blocked", "Review", "Done", "Cancelled");
                    RequireBoundedString(payload, "reason", 16384, nonBlank: true);
                    break;
                case "AcceptanceResult":
                    RequireClosedObject(payload, ["decision", "reason", "authorityActorId", "authorityRole", "auditRunId"]);
                    RequireEnum(payload, "decision", "Accepted", "ChangesRequested");
                    RequireBoundedString(payload, "reason", 16384, nonBlank: true);
                    RequireBoundedString(payload, "authorityActorId", 16384, nonBlank: true);
                    RequireEnum(payload, "authorityRole", "Auditor", "Owner");
                    RequireNullableBoundedString(payload, "auditRunId", 128);
                    if (RequireString(value, "actorId") != RequireString(payload, "authorityActorId"))
                    {
                        throw Error("ACTIVITY", "AcceptanceResult actor and authority identity must match.");
                    }
                    break;
                case "TaskPatched":
                    RequireClosedObject(payload, ["fromRevision", "toRevision", "activitySequence", "hashProfile", "patch", "beforeTaskCoreDigest", "afterTaskCoreDigest"]);
                    var fromRevision = ReadInteger(payload, "fromRevision");
                    var toRevision = ReadInteger(payload, "toRevision");
                    if (fromRevision < 1 || toRevision != fromRevision + 1 || ReadInteger(payload, "activitySequence") != ReadInteger(value, "sequence"))
                    {
                        throw Error("TASK-PATCH", "TaskPatched revision and activity sequence metadata are inconsistent.");
                    }
                    RequireConst(payload, "hashProfile", AgentContextBuilderV3.HashProfile);
                    if (payload["patch"] is not JsonArray patch || patch.Count is < 1 or > 32)
                    {
                        throw Error("TASK-PATCH", "TaskPatched requires between 1 and 32 JSON Patch operations.");
                    }

                    foreach (var operationNode in patch)
                    {
                        var operation = RequireObject(operationNode, "JSON Patch operation");
                        RequireClosedObject(operation, ["op", "path", "oldValue", "value"]);
                        RequireConst(operation, "op", "replace");
                        var path = RequireEnum(operation, "path", "/title", "/description", "/priority", "/tagIds", "/deadline", "/assignee", "/parentTaskUid", "/relations", "/acceptanceCriteria", "/links", "/executionContract");
                        ValidatePatchValue(path, operation["oldValue"], "oldValue");
                        ValidatePatchValue(path, operation["value"], "value");
                    }

                    RequirePattern(payload, "beforeTaskCoreDigest", "^[a-f0-9]{64}$");
                    RequirePattern(payload, "afterTaskCoreDigest", "^[a-f0-9]{64}$");
                    break;
                case "WorkspaceChangesUpdated":
                    RequireClosedObject(payload, ["beforeDigest", "afterDigest", "workspaceChanges"]);
                    RequirePattern(payload, "beforeDigest", "^[a-f0-9]{64}$");
                    RequirePattern(payload, "afterDigest", "^[a-f0-9]{64}$");
                    ValidateWorkspaceChangesObject(RequireObject(payload, "workspaceChanges"));
                    break;
                case "Legacy":
                    RequireClosedObject(payload, ["sourceKind", "text"]);
                    RequireBoundedString(payload, "sourceKind", 128, nonBlank: true);
                    RequireBoundedString(payload, "text", 262144, nonBlank: true);
                    break;
                default:
                    ValidateNonLegacyActivityPayload(kind, payload);
                    break;
            }
        }

        if (ReadInteger(task, "lastActivitySequence") != expectedSequence - 1)
        {
            throw Error("ACTIVITY-SEQUENCE", "lastActivitySequence must equal the final activity sequence.");
        }
    }

    private static void ValidatePatchValue(string path, JsonNode? node, string field)
    {
        var property = path[1..];
        var probe = new JsonObject { [property] = node?.DeepClone() };
        switch (path)
        {
            case "/title": RequireBoundedString(probe, property, 512, nonBlank: true); break;
            case "/description": RequireBoundedString(probe, property, 262144, nonBlank: false); break;
            case "/priority": RequireBoundedString(probe, property, 64, nonBlank: true); break;
            case "/tagIds": ValidateStringArray(probe, property, 128, 1024); RequireUniqueStrings(probe, property); break;
            case "/deadline": RequireNullableDate(probe, property); break;
            case "/assignee": RequireNullableBoundedString(probe, property, 16384); break;
            case "/parentTaskUid": RequireNullablePattern(probe, property, "^task-[A-Za-z0-9][A-Za-z0-9._-]{0,127}$"); break;
            case "/relations": ValidateRelations(new JsonObject { ["relations"] = node?.DeepClone() }); break;
            case "/acceptanceCriteria": ValidateCriterionDefinitions(node); break;
            case "/links": ValidateLinks(new JsonObject { ["links"] = node?.DeepClone() }); break;
            case "/executionContract": ValidateExecutionContract(RequireObject(node, $"patch {field}")); break;
            default: throw Error("TASK-PATCH", $"Unsupported patch path '{path}'.");
        }
    }

    private static void ValidateCriterionDefinitions(JsonNode? node)
    {
        if (node is not JsonArray criteria || criteria.Count > 256)
        {
            throw Error("TASK-PATCH", "Patched acceptance criterion definitions must be an array of at most 256 items.");
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var criterionNode in criteria)
        {
            var criterion = RequireObject(criterionNode, "patched criterion definition");
            RequireClosedObject(criterion, ["criterionId", "description"]);
            RequireBoundedString(criterion, "criterionId", 128, nonBlank: true);
            RequireBoundedString(criterion, "description", 16384, nonBlank: true);
            if (!ids.Add(RequireString(criterion, "criterionId"))) throw Error("TASK-PATCH", "Patched criterion ids must be unique.");
        }
    }

    private static void ValidateAuditRuns(JsonObject task)
    {
        foreach (var runNode in RequireArrayValue(task, "auditRuns"))
        {
            var run = RequireObject(runNode, "audit run");
            RequireClosedObject(run, ["runId", "stage", "auditorIdentity", "createdAt", "taskRevision", "recordedAtRevision", "contextDigest", "workspaceChangesDigest", "packageManifest", "controlContext", "packageDigest", "reportAttachmentId", "decision", "previousVerdictChain"]);
            RequireBoundedString(run, "runId", 128, nonBlank: true);
            RequireEnum(run, "stage", "Primary", "Control");
            var identity = RequireObject(run, "auditorIdentity");
            ValidateAuthor(identity);
            RequireEnum(identity, "role", "Auditor", "Owner");
            RequireDateTime(run, "createdAt", nullable: false);
            if (ReadInteger(run, "taskRevision") < 1)
            {
                throw Error("AUDIT-RUN", "Audit run taskRevision must be positive.");
            }
            if (ReadInteger(run, "recordedAtRevision") != ReadInteger(run, "taskRevision") + 1)
            {
                throw Error("AUDIT-RUN", "Audit run must be recorded by the revision immediately following its audited snapshot.");
            }

            RequirePattern(run, "contextDigest", "^[a-f0-9]{64}$");
            RequirePattern(run, "workspaceChangesDigest", "^[a-f0-9]{64}$");
            var packageManifest = RequireObject(run, "packageManifest");
            RequireClosedObject(packageManifest, ["taskRevision", "contextDigest", "workspaceChangesDigest", "inputAttachmentIds", "excludedAttachmentIds"]);
            if (ReadInteger(packageManifest, "taskRevision") != ReadInteger(run, "taskRevision") ||
                RequireString(packageManifest, "contextDigest") != RequireString(run, "contextDigest") ||
                RequireString(packageManifest, "workspaceChangesDigest") != RequireString(run, "workspaceChangesDigest"))
            {
                throw Error("AUDIT-PACKAGE", "Audit package manifest must bind the run revision and context digests.");
            }
            foreach (var field in new[] { "contextDigest", "workspaceChangesDigest" }) RequirePattern(packageManifest, field, "^[a-f0-9]{64}$");
            foreach (var field in new[] { "inputAttachmentIds", "excludedAttachmentIds" })
            {
                ValidateStringArray(packageManifest, field, 128, 1024);
                RequireUniqueStrings(packageManifest, field);
            }
            var included = RequireArrayValue(packageManifest, "inputAttachmentIds").Select(node => node!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
            if (RequireArrayValue(packageManifest, "excludedAttachmentIds").Select(node => node!.GetValue<string>()).Any(included.Contains))
            {
                throw Error("AUDIT-PACKAGE", "Audit package input and excluded attachment sets must be disjoint.");
            }

            JsonObject? controlContext = null;
            if (RequireString(run, "stage") == "Primary")
            {
                if (run["controlContext"] is not null) throw Error("AUDIT-CONTROL", "Primary run cannot declare control context.");
            }
            else
            {
                controlContext = RequireObject(run, "controlContext");
                RequireClosedObject(controlContext, ["primaryRunId", "excludedRunIds", "excludedReportAttachmentIds"]);
                RequireBoundedString(controlContext, "primaryRunId", 128, nonBlank: true);
                foreach (var field in new[] { "excludedRunIds", "excludedReportAttachmentIds" })
                {
                    ValidateStringArray(controlContext, field, 128, 64);
                    RequireUniqueStrings(controlContext, field);
                    if (RequireArrayValue(controlContext, field).Count == 0) throw Error("AUDIT-CONTROL", "Clean control exclusion lists cannot be empty.");
                }
            }
            RequirePattern(run, "packageDigest", "^[a-f0-9]{64}$");
            var expectedPackageDigest = AgentContextBuilderV3.HashCanonical(new JsonObject
            {
                ["packageManifest"] = packageManifest.DeepClone(),
                ["controlContext"] = controlContext?.DeepClone()
            });
            if (RequireString(run, "packageDigest") != expectedPackageDigest)
            {
                throw Error("AUDIT-PACKAGE", "packageDigest does not match the canonical package/control manifest.");
            }
            RequireBoundedString(run, "reportAttachmentId", 128, nonBlank: true);
            RequireEnum(run, "decision", "Accepted", "NeedsFixes");
            ValidateStringArray(run, "previousVerdictChain", 128, 64);
            RequireUniqueStrings(run, "previousVerdictChain");
        }
    }

    private static void ValidateConversation(JsonObject task)
    {
        var conversation = RequireObject(task, "conversation");
        RequireClosedObject(conversation, ["lastMessageSequence", "messages", "contextCheckpoints"]);
        var lastSequence = ReadInteger(conversation, "lastMessageSequence");
        if (lastSequence < 0)
        {
            throw Error("CONVERSATION-SEQUENCE", "conversation.lastMessageSequence cannot be negative.");
        }

        if (conversation["messages"] is not JsonArray messages || conversation["contextCheckpoints"] is not JsonArray checkpoints)
        {
            throw Error("TYPE", "Conversation messages and contextCheckpoints must be arrays.");
        }

        long expectedSequence = 1;
        var messageIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var messageNode in messages)
        {
            var message = RequireObject(messageNode, "conversation message");
            RequireClosedObject(message, ["messageId", "sequence", "author", "createdAt", "replyToMessageId", "agentRunId", "content"]);
            RequireBoundedString(message, "messageId", 128, nonBlank: true);
            if (!messageIds.Add(RequireString(message, "messageId")))
            {
                throw Error("CONVERSATION-SEQUENCE", "Conversation messageId values must be unique.");
            }
            if (ReadInteger(message, "sequence") != expectedSequence++)
            {
                throw Error("CONVERSATION-SEQUENCE", "Conversation message sequences must be contiguous and start at one.");
            }

            var author = RequireObject(message, "author");
            ValidateAuthor(author);
            RequireDateTime(message, "createdAt", nullable: false);
            RequireNullableBoundedString(message, "replyToMessageId", 128);
            RequireNullableBoundedString(message, "agentRunId", 128);
            var actorKind = RequireString(author, "actorKind");
            var agentRunId = message["agentRunId"] is null ? null : RequireString(message, "agentRunId");
            if ((actorKind == "Agent") != (agentRunId is not null))
            {
                throw Error("CONVERSATION-AUTHOR", "Agent messages require agentRunId and non-agent messages cannot claim an agent run.");
            }
            if (message["content"] is not JsonArray content || content.Count == 0)
            {
                throw Error("CONVERSATION-CONTENT", "Conversation message requires content.");
            }

            foreach (var blockNode in content)
            {
                var block = RequireObject(blockNode, "conversation content block");
                var kind = RequireEnum(block, "kind", "Markdown", "Attachment");
                var field = kind == "Markdown" ? "markdown" : "attachmentId";
                RequireClosedObject(block, ["kind", field]);
                RequireBoundedString(block, field, kind == "Markdown" ? 262144 : 128, nonBlank: true);
            }
        }

        if (lastSequence != messages.Count)
        {
            throw Error("CONVERSATION-SEQUENCE", "conversation.lastMessageSequence must equal the final message sequence.");
        }

        foreach (var checkpointNode in checkpoints)
        {
            var checkpoint = RequireObject(checkpointNode, "context checkpoint");
            RequireClosedObject(checkpoint, ["checkpointId", "agentRunId", "actorId", "role", "createdAt", "taskRevision", "lastMessageSequence", "lastActivitySequence", "contextDigest", "rebaseOfCheckpointId"]);
            foreach (var field in new[] { "checkpointId", "agentRunId", "actorId" })
            {
                RequireBoundedString(checkpoint, field, 128, nonBlank: true);
            }

            RequireEnum(checkpoint, "role", "Worker", "Auditor", "Owner");
            RequireDateTime(checkpoint, "createdAt", nullable: false);
            if (ReadInteger(checkpoint, "taskRevision") < 1 || ReadInteger(checkpoint, "lastMessageSequence") < 0 ||
                ReadInteger(checkpoint, "lastActivitySequence") < 0)
            {
                throw Error("CONTEXT-CHECKPOINT", "Checkpoint revision and sequence are invalid.");
            }

            RequirePattern(checkpoint, "contextDigest", "^[a-f0-9]{64}$");
            RequireNullableBoundedString(checkpoint, "rebaseOfCheckpointId", 128);
            if (ReadInteger(checkpoint, "taskRevision") > ReadInteger(task, "revision") ||
                ReadInteger(checkpoint, "lastMessageSequence") > lastSequence ||
                ReadInteger(checkpoint, "lastActivitySequence") > ReadInteger(task, "lastActivitySequence"))
            {
                throw Error("CONTEXT-CHECKPOINT", "Checkpoint cannot reference a future task revision or message sequence.");
            }
        }
    }

    private static void ValidateAuthor(JsonObject author)
    {
        RequireClosedObject(author, ["actorId", "actorKind", "role"]);
        RequireBoundedString(author, "actorId", 16384, nonBlank: true);
        RequireEnum(author, "actorKind", "Human", "Agent", "Cli", "ExternalFile", "System", "Test");
        RequireEnum(author, "role", "Worker", "Auditor", "Owner");
        if (RequireString(author, "actorKind") == "System" && RequireString(author, "role") == "Owner")
        {
            throw Error("CONVERSATION-AUTHOR", "System identity cannot claim Owner role.");
        }
    }

    private static void ValidateContextSnapshot(JsonObject task)
    {
        if (task["contextSnapshot"] is null)
        {
            return;
        }

        var snapshot = RequireObject(task, "contextSnapshot");
        RequireClosedObject(snapshot, ["contextRevision", "builderProfile", "hashProfile", "throughTaskRevision", "throughMessageSequence", "throughActivitySequence", "contextDigest", "sourceManifest", "summaryMarkdown", "createdAt", "createdBy", "model"]);
        RequireConst(snapshot, "builderProfile", AgentContextBuilderV3.BuilderProfile);
        RequireConst(snapshot, "hashProfile", AgentContextBuilderV3.HashProfile);
        if (ReadInteger(snapshot, "contextRevision") < 1 || ReadInteger(snapshot, "throughTaskRevision") < 1 ||
            ReadInteger(snapshot, "throughMessageSequence") < 0 || ReadInteger(snapshot, "throughActivitySequence") < 0)
        {
            throw Error("CONTEXT-SNAPSHOT", "Context snapshot revision and covered sequence are invalid.");
        }

        RequirePattern(snapshot, "contextDigest", "^[a-f0-9]{64}$");
        var manifest = RequireObject(snapshot, "sourceManifest");
        RequireClosedObject(manifest, ["builderProfile", "hashProfile", "throughTaskRevision", "throughMessageSequence", "throughActivitySequence", "taskCoreDigest", "conversationDigest", "checkpointDigest", "activityDigest", "auditRunsDigest", "workspaceChangesDigest", "attachmentManifestDigest", "attachmentManifest"]);
        RequireConst(manifest, "builderProfile", AgentContextBuilderV3.BuilderProfile);
        RequireConst(manifest, "hashProfile", AgentContextBuilderV3.HashProfile);
        if (ReadInteger(manifest, "throughTaskRevision") != ReadInteger(snapshot, "throughTaskRevision") ||
            ReadInteger(manifest, "throughMessageSequence") != ReadInteger(snapshot, "throughMessageSequence") ||
            ReadInteger(manifest, "throughActivitySequence") != ReadInteger(snapshot, "throughActivitySequence"))
        {
            throw Error("CONTEXT-SNAPSHOT", "Snapshot and source manifest watermarks must match.");
        }
        foreach (var field in new[] { "taskCoreDigest", "conversationDigest", "checkpointDigest", "activityDigest", "auditRunsDigest", "workspaceChangesDigest", "attachmentManifestDigest" })
        {
            RequirePattern(manifest, field, "^[a-f0-9]{64}$");
        }

        if (manifest["attachmentManifest"] is not JsonArray attachmentManifest)
        {
            throw Error("CONTEXT-SNAPSHOT", "Context snapshot attachment manifest must be an array.");
        }

        foreach (var entryNode in attachmentManifest)
        {
            var entry = RequireObject(entryNode, "attachment manifest entry");
            RequireClosedObject(entry, ["attachmentId", "derivativeId", "kind", "byteLength", "sha256"]);
            RequireBoundedString(entry, "attachmentId", 128, nonBlank: true);
            RequireNullableBoundedString(entry, "derivativeId", 128);
            var kind = RequireEnum(entry, "kind", "Original", "ExtractedText", "Ocr", "Preview");
            if ((kind == "Original") != (entry["derivativeId"] is null))
            {
                throw Error("CONTEXT-SNAPSHOT", "Original manifest entries cannot have derivativeId and derivative entries require it.");
            }
            if (ReadInteger(entry, "byteLength") < 0) throw Error("CONTEXT-SNAPSHOT", "Attachment manifest length cannot be negative.");
            RequirePattern(entry, "sha256", "^[a-f0-9]{64}$");
        }
        if (RequireString(snapshot, "contextDigest") != AgentContextBuilderV3.HashCanonical(manifest) ||
            RequireString(manifest, "attachmentManifestDigest") != AgentContextBuilderV3.HashCanonical(attachmentManifest))
        {
            throw Error("CONTEXT-SNAPSHOT", "Snapshot context or attachment manifest digest is not reproducible.");
        }

        RequireBoundedString(snapshot, "summaryMarkdown", 262144, nonBlank: true);
        RequireDateTime(snapshot, "createdAt", nullable: false);
        ValidateAuthor(RequireObject(snapshot, "createdBy"));
        RequireBoundedString(snapshot, "model", 256, nonBlank: true);
        var lastSequence = ReadInteger(RequireObject(task, "conversation"), "lastMessageSequence");
        if (ReadInteger(snapshot, "throughTaskRevision") > ReadInteger(task, "revision") ||
            ReadInteger(snapshot, "throughMessageSequence") > lastSequence ||
            ReadInteger(snapshot, "throughActivitySequence") > ReadInteger(task, "lastActivitySequence"))
        {
            throw Error("CONTEXT-SNAPSHOT", "Context snapshot cannot cover sources that do not exist.");
        }
    }

    private static void ValidateWorkspaceChanges(JsonObject task)
    {
        ValidateWorkspaceChangesObject(RequireObject(task, "workspaceChanges"));
    }

    private static void ValidateWorkspaceChangesObject(JsonObject workspaceChanges)
    {
        RequireClosedObject(workspaceChanges, ["baseRevision", "currentRevision", "files"]);
        RequireNullablePattern(workspaceChanges, "baseRevision", "^(?:git:[a-f0-9]{40,64}|workspace:[a-f0-9]{64})$");
        RequireNullablePattern(workspaceChanges, "currentRevision", "^(?:git:[a-f0-9]{40,64}|workspace:[a-f0-9]{64})$");
        var files = RequireArrayValue(workspaceChanges, "files");
        if (files.Count > 20000)
        {
            throw Error("WORKSPACE-CHANGES", "workspaceChanges.files exceeds 20000 entries.");
        }

        if ((workspaceChanges["baseRevision"] is null) != (workspaceChanges["currentRevision"] is null) ||
            files.Count > 0 && workspaceChanges["baseRevision"] is null)
        {
            throw Error("WORKSPACE-CHANGES", "Workspace revisions form a pair and are required for a non-empty file list.");
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fileNode in files)
        {
            var file = RequireObject(fileNode, "workspace change file");
            RequireClosedObject(file, ["path", "changeKind", "previousPath", "baseSha256", "currentSha256", "firstChangedAt", "lastChangedAt", "agentRunIds"]);
            var path = RequireString(file, "path");
            ValidateProjectRelativePath(path, "workspace change path");
            if (path.StartsWith(".taskboard/", StringComparison.OrdinalIgnoreCase) || path.Equals(".taskboard", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(".git/", StringComparison.OrdinalIgnoreCase) || path.Equals(".git", StringComparison.OrdinalIgnoreCase))
            {
                throw Error("WORKSPACE-CHANGES", "Taskboard, Git and attachment internals cannot be claimed as workspace changes.");
            }
            if (!paths.Add(path)) throw Error("WORKSPACE-CHANGES", $"Workspace change path '{path}' is duplicated.");
            var kind = RequireEnum(file, "changeKind", "Added", "Modified", "Deleted", "Renamed", "Copied", "TypeChanged");
            RequireNullableBoundedString(file, "previousPath", 4096);
            if (file["previousPath"] is not null) ValidateProjectRelativePath(RequireString(file, "previousPath"), "workspace previous path");
            RequireNullablePattern(file, "baseSha256", "^[a-f0-9]{64}$");
            RequireNullablePattern(file, "currentSha256", "^[a-f0-9]{64}$");
            RequireDateTime(file, "firstChangedAt", nullable: false);
            RequireDateTime(file, "lastChangedAt", nullable: false);
            if (DateTimeOffset.Parse(RequireString(file, "lastChangedAt"), CultureInfo.InvariantCulture) <
                DateTimeOffset.Parse(RequireString(file, "firstChangedAt"), CultureInfo.InvariantCulture))
            {
                throw Error("WORKSPACE-CHANGES", "Workspace change lastChangedAt cannot precede firstChangedAt.");
            }
            ValidateStringArray(file, "agentRunIds", 128, 256);
            RequireUniqueStrings(file, "agentRunIds");

            var hasPreviousPath = file["previousPath"] is not null;
            var hasBase = file["baseSha256"] is not null;
            var hasCurrent = file["currentSha256"] is not null;
            var valid = kind switch
            {
                "Added" => !hasPreviousPath && !hasBase && hasCurrent,
                "Deleted" => !hasPreviousPath && hasBase && !hasCurrent,
                "Renamed" or "Copied" => hasPreviousPath && hasBase && hasCurrent,
                _ => !hasPreviousPath && hasBase && hasCurrent
            };
            if (!valid) throw Error("WORKSPACE-CHANGES", $"Workspace change metadata is inconsistent with kind '{kind}'.");
        }
    }

    private static void ValidateNonLegacyActivityPayload(string kind, JsonObject payload)
    {
        switch (kind)
        {
            case "TestResult":
                RequireClosedObject(payload, ["commandId", "outcome", "exitCode", "summary", "evidence"]);
                RequireBoundedString(payload, "commandId", 128, nonBlank: true);
                RequireEnum(payload, "outcome", "Passed", "Failed", "Skipped");
                if (payload["exitCode"] is not null) _ = ReadInteger(payload, "exitCode");
                RequireBoundedString(payload, "summary", 16384, nonBlank: true);
                if (payload["evidence"] is not JsonArray) throw Error("TYPE", "TestResult evidence must be an array.");
                break;
            case "Blocker":
                RequireClosedObject(payload, ["blockerId", "kind", "reason", "state"]);
                RequireBoundedString(payload, "blockerId", 128, nonBlank: true);
                RequireEnum(payload, "kind", "Environment", "Decision", "External", "Manual");
                RequireBoundedString(payload, "reason", 16384, nonBlank: true);
                RequireEnum(payload, "state", "Active", "Resolved");
                break;
            case "Decision":
                RequireClosedObject(payload, ["decision", "rationale", "authority"]);
                RequireBoundedString(payload, "decision", 16384, nonBlank: true);
                RequireBoundedString(payload, "rationale", 16384, nonBlank: true);
                RequireBoundedString(payload, "authority", 16384, nonBlank: true);
                break;
            case "Investigation":
                RequireClosedObject(payload, ["summary", "evidence"]);
                RequireBoundedString(payload, "summary", 16384, nonBlank: true);
                if (payload["evidence"] is not JsonArray) throw Error("TYPE", "Investigation evidence must be an array.");
                break;
        }
    }

    private static void ValidateLinks(JsonObject task)
    {
        foreach (var link in RequireArrayValue(task, "links"))
        {
            var value = RequireObject(link, "links item");
            RequireClosedObject(value, ["linkId", "kind", "value"]);
            RequireBoundedString(value, "linkId", 128, nonBlank: true);
            var kind = RequireEnum(value, "kind", "File", "Directory", "Uri", "Transaction", "Job", "Diagnostic", "Scene", "Resource", "Node");
            RequireBoundedString(value, "value", 4096, nonBlank: true);
            var linkValue = RequireString(value, "value");
            if (kind is "File" or "Directory")
            {
                ValidateProjectRelativePath(linkValue, $"{kind} link", allowProjectRoot: kind == "Directory");
            }
            else if (kind == "Uri" && !Uri.TryCreate(linkValue, UriKind.Absolute, out _))
            {
                throw Error("URI", "Typed Uri link must contain an absolute URI.");
            }
        }
    }

    private static void ValidateExecutionContract(JsonObject contract)
    {
        RequireClosedObject(contract, ["taskType", "readyToStart", "stopConditions", "allowedChanges", "forbiddenChanges", "requiredOutputs", "commands", "externalAudit"]);
        RequireBoundedString(contract, "taskType", 128, nonBlank: true);
        foreach (var field in new[] { "readyToStart", "stopConditions", "allowedChanges", "forbiddenChanges", "requiredOutputs" })
        {
            ValidateStringArray(contract, field, 16384, 256);
        }

        if (contract["commands"] is not JsonArray commands || commands.Count > 256)
        {
            throw Error("TYPE", "executionContract.commands must be an array of at most 256 items.");
        }

        foreach (var commandNode in commands)
        {
            var command = RequireObject(commandNode, "commands item");
            var kind = RequireEnum(command, "kind", "Process", "LegacyShell");
            if (kind == "LegacyShell")
            {
                RequireClosedObject(command, ["commandId", "kind", "text", "execution"]);
                RequireBoundedString(command, "commandId", 128, nonBlank: true);
                RequireBoundedString(command, "text", 262144, nonBlank: true);
                RequireEnum(command, "execution", "ForbiddenUntilReviewed");
                continue;
            }

            RequireClosedObject(command, ["commandId", "kind", "executable", "arguments", "workingDirectory", "platforms", "timeoutSeconds", "expectedExitCodes", "requestedCapabilities", "confirmation"]);
            RequireBoundedString(command, "commandId", 128, nonBlank: true);
            RequireBoundedString(command, "executable", 1024, nonBlank: true);
            ValidateStringArray(command, "arguments", 8192, 256, allowBlank: true);
            ValidateProjectRelativePath(RequireString(command, "workingDirectory"), "command workingDirectory", allowProjectRoot: true);
            ValidateEnumArray(command, "platforms", 1, "Any", "Windows", "Linux", "macOS");
            var platforms = RequireArrayValue(command, "platforms").Select(item => item!.GetValue<string>()).ToArray();
            if (platforms.Length > 1 && platforms.Contains("Any", StringComparer.Ordinal))
            {
                throw Error("PLATFORMS", "Platform Any cannot be combined with a specific platform.");
            }
            var timeout = ReadInteger(command, "timeoutSeconds");
            if (timeout is < 1 or > 86400) throw Error("LIMIT", "Command timeoutSeconds must be between 1 and 86400.");
            ValidateIntegerArray(command, "expectedExitCodes", 1, 32);
            ValidateEnumArray(command, "requestedCapabilities", 1, "WorkspaceRead", "WorkspaceWrite", "Network", "ExternalEffect");
            RequireEnum(command, "confirmation", "PolicyDecides", "HumanRequired");
        }

        var audit = RequireObject(contract, "externalAudit");
        RequireClosedObject(audit, ["mode", "independence", "instructions", "requiredVerdicts"]);
        var mode = RequireEnum(audit, "mode", "None", "Single", "PrimaryControl");
        var independence = RequireEnum(audit, "independence", "NotRequired", "DifferentActor", "CleanControlContext");
        RequireNullableBoundedString(audit, "instructions", 16384);
        ValidateEnumArray(audit, "requiredVerdicts", 0, "Primary", "Control");
        var verdicts = RequireArrayValue(audit, "requiredVerdicts").Select(item => item!.GetValue<string>()).ToArray();
        var validAudit = mode switch
        {
            "None" => independence == "NotRequired" && audit["instructions"] is null && verdicts.Length == 0,
            "Single" => verdicts.SequenceEqual(["Primary"], StringComparer.Ordinal),
            "PrimaryControl" => independence != "NotRequired" && verdicts.SequenceEqual(["Primary", "Control"], StringComparer.Ordinal),
            _ => false
        };
        if (!validAudit)
        {
            throw Error("EXTERNAL-AUDIT", "Typed externalAudit fields are inconsistent with its mode.");
        }
    }

    private static void ValidateAttachments(JsonObject task)
    {
        foreach (var attachment in RequireArrayValue(task, "attachments"))
        {
            var value = RequireObject(attachment, "attachments item");
            RequireClosedObject(value, ["attachmentId", "displayName", "relativePath", "mediaType", "byteLength", "sha256", "addedAt", "addedBy", "derivatives"]);
            RequireBoundedString(value, "attachmentId", 128, nonBlank: true);
            RequireBoundedString(value, "displayName", 255, nonBlank: true);
            RequireBoundedString(value, "relativePath", 4096, nonBlank: true);
            ValidateAttachmentRelativePath(RequireString(value, "relativePath"), "Attachment relativePath");
            RequireBoundedString(value, "mediaType", 255, nonBlank: true);
            if (ReadInteger(value, "byteLength") < 0) throw Error("TYPE", "Attachment byteLength cannot be negative.");
            RequirePattern(value, "sha256", "^[a-f0-9]{64}$");
            RequireDateTime(value, "addedAt", nullable: false);
            RequireBoundedString(value, "addedBy", 16384, nonBlank: true);
            if (value["derivatives"] is not JsonArray derivatives || derivatives.Count != 3)
            {
                throw Error("DERIVATIVE-STATUS", "Attachment derivatives must contain exactly ExtractedText, Ocr and Preview lifecycle entries.");
            }

            var derivativeKinds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var derivativeNode in derivatives)
            {
                var derivative = RequireObject(derivativeNode, "attachment derivative");
                RequireClosedObject(derivative, ["derivativeId", "kind", "status", "failureReason", "relativePath", "mediaType", "byteLength", "sha256", "sourceSha256", "extractor", "createdAt"]);
                RequireBoundedString(derivative, "derivativeId", 128, nonBlank: true);
                var kind = RequireEnum(derivative, "kind", "ExtractedText", "Ocr", "Preview");
                if (!derivativeKinds.Add(kind)) throw Error("DERIVATIVE-STATUS", $"Derivative kind '{kind}' is duplicated.");
                var status = RequireEnum(derivative, "status", "Pending", "Ready", "Failed", "Unsupported", "NotRequired");
                RequireNullableBoundedString(derivative, "failureReason", 16384);
                RequirePattern(derivative, "sourceSha256", "^[a-f0-9]{64}$");
                RequireDateTime(derivative, "createdAt", nullable: false);
                if (status == "Ready")
                {
                    RequireBoundedString(derivative, "relativePath", 4096, nonBlank: true);
                    ValidateAttachmentRelativePath(RequireString(derivative, "relativePath"), "Derivative relativePath");
                    RequireBoundedString(derivative, "mediaType", 255, nonBlank: true);
                    if (ReadInteger(derivative, "byteLength") < 0) throw Error("TYPE", "Derivative byteLength cannot be negative.");
                    RequirePattern(derivative, "sha256", "^[a-f0-9]{64}$");
                    var extractor = RequireObject(derivative, "extractor");
                    RequireClosedObject(extractor, ["name", "version"]);
                    RequireBoundedString(extractor, "name", 256, nonBlank: true);
                    RequireBoundedString(extractor, "version", 128, nonBlank: true);
                    if (derivative["failureReason"] is not null) throw Error("DERIVATIVE-STATUS", "Ready derivative cannot have a failure reason.");
                }
                else
                {
                    if (new[] { "relativePath", "mediaType", "byteLength", "sha256", "extractor" }.Any(field => derivative[field] is not null))
                    {
                        throw Error("DERIVATIVE-STATUS", "A non-ready derivative cannot expose blob metadata.");
                    }

                    var hasReason = derivative["failureReason"] is not null;
                    if ((status is "Failed" or "Unsupported") != hasReason)
                    {
                        throw Error("DERIVATIVE-STATUS", "Failed and Unsupported derivatives require a reason; Pending and NotRequired do not.");
                    }
                }
            }
        }
    }

    private static void ValidateLegacyFragments(JsonObject task)
    {
        foreach (var fragmentNode in RequireArrayValue(task, "legacySourceFragments"))
        {
            var fragment = RequireObject(fragmentNode, "legacySourceFragments item");
            RequireClosedObject(fragment, ["sourcePath", "byteOffset", "byteLength", "encoding", "hasBom", "lineEnding", "sha256", "markdown"]);
            RequireBoundedString(fragment, "sourcePath", 4096, nonBlank: true);
            if (ReadInteger(fragment, "byteOffset") < 0) throw Error("TYPE", "legacy source byteOffset cannot be negative.");
            var length = ReadInteger(fragment, "byteLength");
            if (length is < 0 or > 1048576) throw Error("LIMIT", "legacy source byteLength must be between 0 and 1048576.");
            RequireBoundedString(fragment, "encoding", 128, nonBlank: true);
            if (fragment["hasBom"] is not JsonValue hasBom || !hasBom.TryGetValue<bool>(out _))
            {
                throw Error("TYPE", "legacy source hasBom must be a boolean.");
            }

            RequireEnum(fragment, "lineEnding", "lf", "crlf", "cr", "mixed");
            RequirePattern(fragment, "sha256", "^[a-f0-9]{64}$");
            RequireBoundedString(fragment, "markdown", 1048576, nonBlank: false);
        }
    }

    private static void ValidateBoardCatalog(JsonObject board)
    {
        foreach (var tagNode in RequireArrayValue(board, "tags"))
        {
            var tag = RequireObject(tagNode, "tags item");
            RequireClosedObject(tag, ["tagId", "name", "color"]);
            RequireBoundedString(tag, "tagId", 128, nonBlank: true);
            RequireBoundedString(tag, "name", 128, nonBlank: true);
            RequireTagColor(tag);
        }

        foreach (var groupNode in RequireArrayValue(board, "groups"))
        {
            var group = RequireObject(groupNode, "groups item");
            RequireClosedObject(group, ["groupId", "kind", "title", "description", "parentGroupId", "rank"]);
            RequireBoundedString(group, "groupId", 128, nonBlank: true);
            RequireEnum(group, "kind", "Epoch", "Milestone");
            RequireBoundedString(group, "title", 512, nonBlank: true);
            RequireBoundedString(group, "description", 262144, nonBlank: false);
            RequireNullableBoundedString(group, "parentGroupId", 128);
            RequirePattern(group, "rank", "^[0-9]{12}$");
        }

        foreach (var placementNode in RequireArrayValue(board, "placements"))
        {
            var placement = RequireObject(placementNode, "placements item");
            RequireClosedObject(placement, ["taskUid", "groupId", "rank"]);
            RequirePattern(placement, "taskUid", "^task-[A-Za-z0-9][A-Za-z0-9._-]{0,127}$");
            RequireNullableBoundedString(placement, "groupId", 128);
            RequirePattern(placement, "rank", "^[0-9]{12}$");
        }
    }

    private static void RequireTagColor(JsonObject tag)
    {
        var color = RequireString(tag, "color");
        if (color is "Gray" or "Blue" or "Green" or "Yellow" or "Orange" or "Red" or "Purple" ||
            Regex.IsMatch(color, "^#[0-9A-F]{6}$", RegexOptions.CultureInvariant))
        {
            return;
        }

        throw Error("ENUM", "Property 'color' must be a legacy tag color or canonical #RRGGBB value.");
    }

    private static void ValidateProjectRelativePath(string value, string description, bool allowProjectRoot = false)
    {
        if (!IsProjectRelativePath(value, allowProjectRoot))
        {
            throw Error("PATH", $"{description} value '{value}' must be a canonical project-relative path.");
        }
    }

    private static bool IsProjectRelativePath(string value, bool allowProjectRoot = false)
    {
        if (allowProjectRoot && value == ".")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value) || value.Length > 4096 || Path.IsPathRooted(value) ||
            value.Contains('\\') || value.Contains(':'))
        {
            return false;
        }

        try
        {
            foreach (var segment in value.Split('/'))
            {
                ValidateSafeFileName(segment);
            }
            return true;
        }
        catch (TaskBoardV3ValidationException)
        {
            return false;
        }
    }

    private static void ValidateAttachmentRelativePath(string value, string description)
    {
        ValidateProjectRelativePath(value, description);
        foreach (var segment in value.Split('/'))
        {
            ValidateSafeFileName(segment);
        }
    }

    private static void ValidateSafeFileName(string name)
    {
        var stem = Path.GetFileNameWithoutExtension(name);
        var reserved = stem.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(stem, "^(COM|LPT)[1-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." ||
            name.EndsWith(' ') || name.EndsWith('.') || name.Any(char.IsControl) ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.IndexOfAny(['<', '>', ':', '"', '/', '\\', '|', '?', '*']) >= 0 || reserved)
        {
            throw Error("SAFE-NAME", $"File name '{name}' is not safe on supported platforms.");
        }
    }

    private static void ValidateLifecycle(JsonObject task)
    {
        var status = RequireString(task, "status");
        var acceptance = RequireString(task, "acceptanceState");
        var valid = status switch
        {
            "Ready" => acceptance == "NotSubmitted" && AllNull(task, "submittedAt", "completedAt", "acceptedAt", "acceptedBy", "cancelledAt", "cancellationReason", "archivedAt", "archivedBy"),
            "InProgress" => acceptance is "NotSubmitted" or "ChangesRequested" && AllNull(task, "submittedAt", "completedAt", "acceptedAt", "acceptedBy", "cancelledAt", "cancellationReason", "archivedAt", "archivedBy"),
            "Blocked" => acceptance is "NotSubmitted" or "ChangesRequested" && AllNull(task, "submittedAt", "completedAt", "acceptedAt", "acceptedBy", "cancelledAt", "cancellationReason", "archivedAt", "archivedBy"),
            "Review" => acceptance switch
            {
                "InternalReview" => task["submittedAt"] is null,
                "Submitted" => task["submittedAt"] is JsonValue,
                _ => false
            } && AllNull(task, "completedAt", "acceptedAt", "acceptedBy", "cancelledAt", "cancellationReason", "archivedAt", "archivedBy"),
            "Done" => acceptance == "Accepted" && task["submittedAt"] is JsonValue && task["completedAt"] is JsonValue && task["acceptedAt"] is JsonValue && task["acceptedBy"] is JsonValue && AllNull(task, "cancelledAt", "cancellationReason"),
            "Cancelled" => acceptance == "Cancelled" && task["cancelledAt"] is JsonValue && task["cancellationReason"] is JsonValue && AllNull(task, "completedAt", "acceptedAt", "acceptedBy"),
            _ => false
        };
        if (!valid)
        {
            throw Error("LIFECYCLE", $"Status '{status}' and acceptance state '{acceptance}' have inconsistent audit fields.");
        }

        if ((task["archivedAt"] is null) != (task["archivedBy"] is null) ||
            task["archivedAt"] is not null && status is not ("Done" or "Cancelled"))
        {
            throw Error("LIFECYCLE", "Archive fields must form a pair and are allowed only for terminal tasks.");
        }

        var hasActiveBlocker = RequireArrayValue(task, "blockers").Select(blocker => RequireObject(blocker, "blocker"))
            .Any(blocker => RequireString(blocker, "state") == "Active");
        if (status == "Blocked" && !hasActiveBlocker || status != "Blocked" && hasActiveBlocker)
        {
            throw Error("LIFECYCLE", "Only Blocked tasks may contain active explicit blockers, and Blocked requires one.");
        }

        var criteria = RequireArrayValue(task, "acceptanceCriteria");
        if (status == "Done" && (criteria.Count == 0 || criteria.Select(criterion => RequireObject(criterion, "criterion"))
            .Any(criterion => RequireString(criterion, "state") != "Passed" || RequireArrayValue(criterion, "evidence").Count == 0)))
        {
            throw Error("LIFECYCLE", "Done requires every criterion to be Passed with evidence.");
        }

        var createdAt = DateTimeOffset.Parse(RequireString(task, "createdAt"), CultureInfo.InvariantCulture);
        var timestamps = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        foreach (var field in new[] { "updatedAt", "submittedAt", "completedAt", "acceptedAt", "cancelledAt", "archivedAt" })
        {
            if (task[field] is JsonValue timestamp && timestamp.TryGetValue<string>(out var text) &&
                DateTimeOffset.Parse(text, CultureInfo.InvariantCulture) is var value)
            {
                if (value < createdAt)
                {
                    throw Error("TIMESTAMP-ORDER", $"Timestamp '{field}' cannot precede createdAt.");
                }

                timestamps[field] = value;
            }
        }

        EnsureTimestampOrder("submittedAt", "completedAt");
        EnsureTimestampOrder("completedAt", "acceptedAt");
        EnsureTimestampOrder("submittedAt", "acceptedAt");
        if (timestamps.TryGetValue("archivedAt", out var archivedAt))
        {
            var terminalAt = status == "Done"
                ? timestamps.GetValueOrDefault("acceptedAt", createdAt)
                : timestamps.GetValueOrDefault("cancelledAt", createdAt);
            if (archivedAt < terminalAt)
            {
                throw Error("TIMESTAMP-ORDER", "Archive timestamp cannot precede the terminal decision.");
            }
        }

        void EnsureTimestampOrder(string earlier, string later)
        {
            if (timestamps.TryGetValue(earlier, out var first) && timestamps.TryGetValue(later, out var second) && second < first)
            {
                throw Error("TIMESTAMP-ORDER", $"Timestamp '{later}' cannot precede '{earlier}'.");
            }
        }
    }

    private static bool AllNull(JsonObject value, params string[] properties)
    {
        return properties.All(property => value[property] is null);
    }

    private static void RequireClosedObject(JsonObject value, IReadOnlyCollection<string> properties)
    {
        var allowed = properties.ToHashSet(StringComparer.Ordinal);
        var unknown = value.Select(property => property.Key).FirstOrDefault(property => !allowed.Contains(property));
        if (unknown is not null)
        {
            throw Error("UNKNOWN-PROPERTY", $"Property '{unknown}' is not allowed by v3 schema.");
        }

        var missing = properties.FirstOrDefault(property => !value.ContainsKey(property));
        if (missing is not null)
        {
            throw Error("REQUIRED", $"Required property '{missing}' is missing.");
        }
    }

    private static void RequireConst(JsonObject value, string propertyName, string expected)
    {
        if (!string.Equals(RequireString(value, propertyName), expected, StringComparison.Ordinal))
        {
            throw Error("CONST", $"Property '{propertyName}' must equal '{expected}'.");
        }
    }

    private static void RequireInteger(JsonObject value, string propertyName, long expected)
    {
        if (ReadInteger(value, propertyName) != expected)
        {
            throw Error("CONST", $"Property '{propertyName}' must equal {expected}.");
        }
    }

    private static void RequirePositiveInteger(JsonObject value, string propertyName)
    {
        if (ReadInteger(value, propertyName) < 1)
        {
            throw Error("TYPE", $"Property '{propertyName}' must be a positive integer.");
        }
    }

    private static long ReadInteger(JsonObject value, string propertyName)
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

        throw Error("TYPE", $"Property '{propertyName}' must be an integer.");
    }

    private static void RequireBoundedString(JsonObject value, string propertyName, int maximum, bool nonBlank)
    {
        var text = RequireString(value, propertyName, allowEmpty: !nonBlank);
        if (text.Length > maximum)
        {
            throw Error("LIMIT", $"Property '{propertyName}' exceeds {maximum} characters.");
        }

        if (nonBlank && string.IsNullOrWhiteSpace(text))
        {
            throw Error("STRING", $"Property '{propertyName}' cannot be blank.");
        }
    }

    private static string RequireString(JsonObject value, string propertyName, bool allowEmpty = false)
    {
        if (value[propertyName] is not JsonValue text || !text.TryGetValue<string>(out var result) || (!allowEmpty && result.Length == 0))
        {
            throw Error("STRING", $"Property '{propertyName}' must be a string.");
        }

        return result;
    }

    private static void RequirePattern(JsonObject value, string propertyName, string pattern)
    {
        if (!Regex.IsMatch(RequireString(value, propertyName), pattern, RegexOptions.CultureInvariant))
        {
            throw Error("PATTERN", $"Property '{propertyName}' has an invalid canonical form.");
        }
    }

    private static void RequireArray(JsonObject value, string propertyName, int maximum)
    {
        if (value[propertyName] is not JsonArray array)
        {
            throw Error("TYPE", $"Property '{propertyName}' must be an array.");
        }

        if (array.Count > maximum)
        {
            throw Error("LIMIT", $"Property '{propertyName}' exceeds {maximum} items.");
        }
    }

    private static JsonArray RequireArrayValue(JsonObject value, string propertyName)
    {
        return value[propertyName] as JsonArray ??
            throw Error("TYPE", $"Property '{propertyName}' must be an array.");
    }

    private static JsonObject RequireObject(JsonObject value, string propertyName)
    {
        return value[propertyName] as JsonObject ??
            throw Error("TYPE", $"Property '{propertyName}' must be an object.");
    }

    private static JsonObject RequireObject(JsonNode? value, string description)
    {
        return value as JsonObject ??
            throw Error("TYPE", $"{description} must be an object.");
    }

    private static string RequireEnum(JsonObject value, string propertyName, params string[] allowed)
    {
        var result = RequireString(value, propertyName);
        if (!allowed.Contains(result, StringComparer.Ordinal))
        {
            throw Error("ENUM", $"Property '{propertyName}' has unsupported value '{result}'.");
        }

        return result;
    }

    private static void RequireNullableBoundedString(JsonObject value, string propertyName, int maximum)
    {
        if (!value.ContainsKey(propertyName))
        {
            throw Error("REQUIRED", $"Required property '{propertyName}' is missing.");
        }

        if (value[propertyName] is null)
        {
            return;
        }

        RequireBoundedString(value, propertyName, maximum, nonBlank: true);
    }

    private static void RequireNullablePattern(JsonObject value, string propertyName, string pattern)
    {
        if (!value.ContainsKey(propertyName))
        {
            throw Error("REQUIRED", $"Required property '{propertyName}' is missing.");
        }

        if (value[propertyName] is null)
        {
            return;
        }

        RequirePattern(value, propertyName, pattern);
    }

    private static void RequireNullableDate(JsonObject value, string propertyName)
    {
        if (!value.ContainsKey(propertyName))
        {
            throw Error("REQUIRED", $"Required property '{propertyName}' is missing.");
        }

        if (value[propertyName] is null)
        {
            return;
        }

        var text = RequireString(value, propertyName);
        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            throw Error("DATE", $"Property '{propertyName}' must use yyyy-MM-dd.");
        }
    }

    private static void RequireDateTime(JsonObject value, string propertyName, bool nullable)
    {
        if (!value.ContainsKey(propertyName))
        {
            throw Error("REQUIRED", $"Required property '{propertyName}' is missing.");
        }

        if (value[propertyName] is null && nullable)
        {
            return;
        }

        var text = RequireString(value, propertyName);
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
        {
            throw Error("DATETIME", $"Property '{propertyName}' must be an RFC 3339 timestamp.");
        }
    }

    private static void ValidateStringArray(
        JsonObject value,
        string propertyName,
        int maximumLength,
        int maximumItems,
        bool allowBlank = false)
    {
        if (value[propertyName] is not JsonArray array || array.Count > maximumItems)
        {
            throw Error("TYPE", $"Property '{propertyName}' must be an array of at most {maximumItems} strings.");
        }

        foreach (var item in array)
        {
            if (item is not JsonValue text || !text.TryGetValue<string>(out var result) ||
                result.Length > maximumLength || !allowBlank && string.IsNullOrWhiteSpace(result))
            {
                throw Error("TYPE", $"Property '{propertyName}' must contain strings of at most {maximumLength} characters.");
            }
        }
    }

    private static void ValidateEnumArray(JsonObject value, string propertyName, int minimumItems, params string[] allowed)
    {
        if (value[propertyName] is not JsonArray array || array.Count < minimumItems)
        {
            throw Error("TYPE", $"Property '{propertyName}' must contain at least {minimumItems} item(s).");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array)
        {
            if (item is not JsonValue text || !text.TryGetValue<string>(out var result) ||
                !allowed.Contains(result, StringComparer.Ordinal) || !seen.Add(result))
            {
                throw Error("ENUM", $"Property '{propertyName}' contains an unsupported or duplicate value.");
            }
        }
    }

    private static void RequireUniqueStrings(JsonObject value, string propertyName)
    {
        var strings = RequireArrayValue(value, propertyName).Select(node => node!.GetValue<string>()).ToArray();
        if (strings.Distinct(StringComparer.Ordinal).Count() != strings.Length)
        {
            throw Error("UNIQUE", $"Property '{propertyName}' must contain unique values.");
        }
    }

    private static void ValidateIntegerArray(JsonObject value, string propertyName, int minimumItems, int maximumItems)
    {
        if (value[propertyName] is not JsonArray array || array.Count < minimumItems || array.Count > maximumItems)
        {
            throw Error("TYPE", $"Property '{propertyName}' must contain between {minimumItems} and {maximumItems} integers.");
        }

        var seen = new HashSet<long>();
        foreach (var item in array)
        {
            if (item is not JsonValue number)
            {
                throw Error("TYPE", $"Property '{propertyName}' must contain unique integers.");
            }

            long result;
            if (number.TryGetValue<long>(out var longValue))
            {
                result = longValue;
            }
            else if (number.TryGetValue<int>(out var intValue))
            {
                result = intValue;
            }
            else
            {
                throw Error("TYPE", $"Property '{propertyName}' must contain unique integers.");
            }

            if (!seen.Add(result)) throw Error("TYPE", $"Property '{propertyName}' must contain unique integers.");
        }
    }

    private static TaskBoardV3ValidationException Error(string suffix, string message)
    {
        return new TaskBoardV3ValidationException(CodePrefix + suffix, message);
    }
}

internal static class TaskTransitionValidatorV3
{
    private const string CodePrefix = "E2D-TASK-V3-TRANSITION-";

    public static void ValidateTask(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);
        ValidateContext(context);
        RequireCapability(context, TaskBoardV3Capability.EditTask, "EDIT-CAPABILITY", "Task mutation requires EditTask capability.");
        RequireRevision(previous, next);
        ValidateExpectedWatermarks(previous, context);

        foreach (var propertyName in new[] { "format", "version", "taskUid", "boardId", "createdAt", "createdBy" })
        {
            if (!JsonNode.DeepEquals(previous[propertyName], next[propertyName]))
            {
                throw Error("IMMUTABLE-FIELD", $"Field '{propertyName}' is immutable.");
            }
        }

        ValidateActivityPrefix(previous, next);
        ValidateConversationPrefix(previous, next);
        ValidateAttachmentHistory(previous, next);
        ValidateArrayPrefix(RequiredArray(previous, "auditRuns"), RequiredArray(next, "auditRuns"), "AUDIT-RUN-PREFIX", "Audit runs");
        ValidateAppendedConversationIdentity(previous, next, context);
        ValidateAppendedAuditRuns(previous, next, context);
        ValidateTaskPatch(previous, next, context);
        ValidateWorkspaceChangesTransition(previous, next, context);
        ValidateUpdatedAt(previous, next);
        ValidateLifecycleTransition(previous, next, context);
        ValidateAppendedActivityIdentity(previous, next, context);
        ValidateAcceptanceEntries(previous, next, context);
        ValidatePrivilegedAudit(previous, next, context);
    }

    private static void ValidateExpectedWatermarks(JsonObject previous, TaskBoardV3MutationContext context)
    {
        if (context.ExpectedRevision is { } expectedRevision && expectedRevision != RequiredLong(previous, "revision") ||
            context.ExpectedLastMessageSequence is { } expectedMessage && expectedMessage != RequiredLong(RequiredObject(previous["conversation"]), "lastMessageSequence") ||
            context.ExpectedLastActivitySequence is { } expectedActivity && expectedActivity != RequiredLong(previous, "lastActivitySequence"))
        {
            throw Error("CAS-WATERMARK", "Task revision, message sequence and activity sequence must match one atomic read snapshot.");
        }
    }

    public static void ValidateBoard(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);
        ValidateContext(context);
        RequireCapability(context, TaskBoardV3Capability.EditBoard, "EDIT-BOARD-CAPABILITY", "Board mutation requires EditBoard capability.");
        RequireRevision(previous, next);

        foreach (var propertyName in new[] { "format", "version", "boardId" })
        {
            if (!JsonNode.DeepEquals(previous[propertyName], next[propertyName]))
            {
                throw Error("IMMUTABLE-FIELD", $"Board field '{propertyName}' is immutable.");
            }
        }

        var previousNextNumber = RequiredLong(RequiredObject(previous["idPolicy"]), "nextNumber");
        var nextNextNumber = RequiredLong(RequiredObject(next["idPolicy"]), "nextNumber");
        if (nextNextNumber < previousNextNumber)
        {
            throw Error("ALLOCATOR-REGRESSION", "Board nextNumber cannot move backwards.");
        }

        if (!JsonNode.DeepEquals(previous["migration"], next["migration"]) && !context.Has(TaskBoardV3Capability.Migrate))
        {
            throw Error("MIGRATION-CAPABILITY", "Migration metadata can change only through a trusted migration command.");
        }
    }

    private static void ValidateLifecycleTransition(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var previousStatus = RequiredString(previous, "status");
        var nextStatus = RequiredString(next, "status");
        var statusChanged = !string.Equals(previousStatus, nextStatus, StringComparison.Ordinal);
        var previousAcceptance = RequiredString(previous, "acceptanceState");
        var nextAcceptance = RequiredString(next, "acceptanceState");

        if (statusChanged)
        {
            RequireCapability(context, TaskBoardV3Capability.ChangeStatus, "STATUS-CAPABILITY", "Status transition requires ChangeStatus capability.");
            if (previousStatus is "Done" or "Cancelled")
            {
                if (nextStatus != "Ready")
                {
                    throw Error("STATUS", $"Terminal status '{previousStatus}' can transition only to Ready through reopen.");
                }

                RequireCapability(context, TaskBoardV3Capability.TrustedReopen, "TRUSTED-REOPEN-REQUIRED", "Reopening a terminal task requires TrustedReopen capability.");
            }
            else if (!AllowedTransitions.TryGetValue(previousStatus, out var allowed) || !allowed.Contains(nextStatus))
            {
                throw Error("STATUS", $"Status transition '{previousStatus}' -> '{nextStatus}' is not allowed.");
            }
        }

        if (nextStatus == "Review" && nextAcceptance == "Submitted" && previousAcceptance != "Submitted")
        {
            RequireCapability(context, TaskBoardV3Capability.SubmitForAcceptance, "SUBMIT-CAPABILITY", "Submitting for acceptance requires SubmitForAcceptance capability.");
        }

        if (statusChanged && nextStatus == "Done")
        {
            RequireCapability(context, TaskBoardV3Capability.AcceptanceDecision, "ACCEPTANCE-CAPABILITY", "Acceptance decision capability is required to produce Done.");
            if (context.Role is not (TaskBoardV3Role.Auditor or TaskBoardV3Role.Owner))
            {
                throw Error("ACCEPTANCE-ROLE", "Only a trusted Auditor or Owner role may accept a task.");
            }

            if (IsSelfAcceptance(previous, context.ActorId))
            {
                throw Error("SELF-ACCEPTANCE", "The task creator, assignee or worker in the current attempt cannot accept their own result.");
            }

            if (!string.Equals(NullableString(next, "acceptedBy"), context.ActorId, StringComparison.Ordinal))
            {
                throw Error("ACCEPTANCE-ACTOR", "acceptedBy must match the trusted acceptance actor.");
            }

            var previousActivityCount = RequiredArray(previous, "activity").Count;
            var hasMatchingAcceptance = RequiredArray(next, "activity").Skip(previousActivityCount).Select(RequiredObject).Any(entry =>
                RequiredString(entry, "kind") == "AcceptanceResult" &&
                RequiredString(entry, "actorKind") == context.ActorKind &&
                RequiredString(entry, "actorId") == context.ActorId &&
                RequiredObject(entry["payload"])["decision"]?.GetValue<string>() == "Accepted" &&
                RequiredObject(entry["payload"])["authorityActorId"]?.GetValue<string>() == context.ActorId &&
                RequiredObject(entry["payload"])["authorityRole"]?.GetValue<string>() == context.Role.ToString());
            if (!hasMatchingAcceptance)
            {
                throw Error("ACCEPTANCE-ACTIVITY", "Done requires a typed AcceptanceResult matching trusted identity, kind and role.");
            }
        }
    }

    private static void ValidatePrivilegedAudit(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var archiveChanged = !JsonNode.DeepEquals(previous["archivedAt"], next["archivedAt"]) ||
            !JsonNode.DeepEquals(previous["archivedBy"], next["archivedBy"]);
        if (archiveChanged)
        {
            RequireCapability(context, TaskBoardV3Capability.Archive, "ARCHIVE-CAPABILITY", "Archive audit fields require Archive capability.");
        }

        var acceptanceChanged = new[] { "acceptedAt", "acceptedBy", "completedAt" }
            .Any(propertyName => !JsonNode.DeepEquals(previous[propertyName], next[propertyName]));
        if (acceptanceChanged && !context.Has(TaskBoardV3Capability.AcceptanceDecision))
        {
            throw Error("ACCEPTANCE-CAPABILITY", "Acceptance audit fields require AcceptanceDecision capability.");
        }

        if (NullableString(next, "archivedAt") is not null && NullableString(next, "archivedBy") is null)
        {
            throw Error("ARCHIVE-PAIR", "archivedAt and archivedBy must be changed as one pair.");
        }
    }

    private static void ValidateAcceptanceEntries(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var previousCount = RequiredArray(previous, "activity").Count;
        var appended = RequiredArray(next, "activity").Skip(previousCount).Select(RequiredObject)
            .Where(entry => RequiredString(entry, "kind") == "AcceptanceResult").ToArray();
        if (appended.Length == 0)
        {
            if (RequiredString(next, "acceptanceState") == "ChangesRequested" &&
                RequiredString(previous, "acceptanceState") != "ChangesRequested")
            {
                throw Error("ACCEPTANCE-ACTIVITY", "ChangesRequested requires an appended trusted AcceptanceResult.");
            }

            return;
        }

        RequireCapability(context, TaskBoardV3Capability.AcceptanceDecision, "ACCEPTANCE-CAPABILITY", "AcceptanceResult requires AcceptanceDecision capability.");
        if (context.Role is not (TaskBoardV3Role.Auditor or TaskBoardV3Role.Owner))
        {
            throw Error("ACCEPTANCE-ROLE", "AcceptanceResult requires a trusted Auditor or Owner role.");
        }

        if (IsSelfAcceptance(previous, context.ActorId))
        {
            throw Error("SELF-ACCEPTANCE", "The task creator, assignee or worker cannot decide acceptance for their own result.");
        }

        foreach (var entry in appended)
        {
            var payload = RequiredObject(entry["payload"]);
            if (RequiredString(entry, "actorId") != context.ActorId ||
                RequiredString(entry, "actorKind") != context.ActorKind ||
                RequiredString(payload, "authorityActorId") != context.ActorId ||
                RequiredString(payload, "authorityRole") != context.Role.ToString())
            {
                throw Error("ACCEPTANCE-ACTOR", "AcceptanceResult must match trusted identity, actor kind and role.");
            }
        }
    }

    private static void ValidateActivityPrefix(JsonObject previous, JsonObject next)
    {
        var previousActivity = RequiredArray(previous, "activity");
        var nextActivity = RequiredArray(next, "activity");
        if (nextActivity.Count < previousActivity.Count)
        {
            throw Error("ACTIVITY-PREFIX", "Activity is append-only and cannot be truncated.");
        }

        for (var index = 0; index < previousActivity.Count; index++)
        {
            if (!JsonNode.DeepEquals(previousActivity[index], nextActivity[index]))
            {
                throw Error("ACTIVITY-PREFIX", $"Existing activity entry at index {index} is immutable.");
            }
        }

        var previousSequence = RequiredLong(previous, "lastActivitySequence");
        var nextSequence = RequiredLong(next, "lastActivitySequence");
        if (nextSequence != previousSequence + nextActivity.Count - previousActivity.Count)
        {
            throw Error("ACTIVITY-SEQUENCE", "Activity append must advance lastActivitySequence exactly once per appended entry.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        DateTimeOffset? lastTimestamp = null;
        foreach (var entry in nextActivity.Select(RequiredObject))
        {
            var id = RequiredString(entry, "activityEntryId");
            if (!ids.Add(id))
            {
                throw Error("ACTIVITY-ID", $"Activity entry id '{id}' is duplicated.");
            }
            if (RequiredLong(entry, "sequence") != ids.Count)
            {
                throw Error("ACTIVITY-SEQUENCE", "Activity sequence must match its immutable append position.");
            }

            var timestamp = DateTimeOffset.Parse(RequiredString(entry, "createdAt"), CultureInfo.InvariantCulture);
            if (lastTimestamp is not null && timestamp < lastTimestamp)
            {
                throw Error("ACTIVITY-ORDER", "Activity timestamps must be nondecreasing.");
            }

            lastTimestamp = timestamp;
        }
    }

    private static void ValidateConversationPrefix(JsonObject previous, JsonObject next)
    {
        var previousConversation = RequiredObject(previous["conversation"]);
        var nextConversation = RequiredObject(next["conversation"]);
        ValidateArrayPrefix(
            RequiredArray(previousConversation, "messages"),
            RequiredArray(nextConversation, "messages"),
            "CONVERSATION-PREFIX",
            "Conversation messages");
        ValidateArrayPrefix(
            RequiredArray(previousConversation, "contextCheckpoints"),
            RequiredArray(nextConversation, "contextCheckpoints"),
            "CONTEXT-CHECKPOINT-PREFIX",
            "Context checkpoints");
    }

    private static void ValidateAppendedConversationIdentity(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var previousConversation = RequiredObject(previous["conversation"]);
        var nextConversation = RequiredObject(next["conversation"]);
        var previousMessageCount = RequiredArray(previousConversation, "messages").Count;
        foreach (var message in RequiredArray(nextConversation, "messages").Skip(previousMessageCount).Select(RequiredObject))
        {
            var author = RequiredObject(message["author"]);
            if (RequiredString(author, "actorId") != context.ActorId ||
                RequiredString(author, "actorKind") != context.ActorKind ||
                RequiredString(author, "role") != context.Role.ToString())
            {
                throw Error("CONVERSATION-AUTHOR", "Appended conversation author must come from the trusted mutation context.");
            }
        }

        var previousCheckpointCount = RequiredArray(previousConversation, "contextCheckpoints").Count;
        foreach (var checkpoint in RequiredArray(nextConversation, "contextCheckpoints").Skip(previousCheckpointCount).Select(RequiredObject))
        {
            if (RequiredString(checkpoint, "actorId") != context.ActorId ||
                RequiredString(checkpoint, "role") != context.Role.ToString() ||
                RequiredLong(checkpoint, "taskRevision") != RequiredLong(next, "revision") ||
                !string.Equals(RequiredString(checkpoint, "contextDigest"), AgentContextBuilderV3.ComputeDigest(next), StringComparison.Ordinal))
            {
                throw Error("CONTEXT-CHECKPOINT-IDENTITY", "Appended checkpoint must match trusted identity and the final task context manifest.");
            }
        }
    }

    private static void ValidateAppendedActivityIdentity(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var previousCount = RequiredArray(previous, "activity").Count;
        foreach (var entry in RequiredArray(next, "activity").Skip(previousCount).Select(RequiredObject))
        {
            if (RequiredString(entry, "actorId") != context.ActorId || RequiredString(entry, "actorKind") != context.ActorKind)
            {
                throw Error("ACTIVITY-ACTOR", "Appended activity identity must come from the trusted mutation context.");
            }
        }
    }

    private static void ValidateAttachmentHistory(JsonObject previous, JsonObject next)
    {
        var previousAttachments = RequiredArray(previous, "attachments").Select(RequiredObject).ToArray();
        var nextAttachments = RequiredArray(next, "attachments").Select(RequiredObject).ToArray();
        if (nextAttachments.Length < previousAttachments.Length)
        {
            throw Error("ATTACHMENT-PREFIX", "Original attachments are append-only and cannot be removed.");
        }

        var immutableFields = new[] { "attachmentId", "displayName", "relativePath", "mediaType", "byteLength", "sha256", "addedAt", "addedBy" };
        for (var index = 0; index < previousAttachments.Length; index++)
        {
            var oldAttachment = previousAttachments[index];
            var newAttachment = nextAttachments[index];
            if (immutableFields.Any(field => !JsonNode.DeepEquals(oldAttachment[field], newAttachment[field])))
            {
                throw Error("ATTACHMENT-PREFIX", $"Original attachment at index {index} is immutable.");
            }

            var oldDerivatives = RequiredArray(oldAttachment, "derivatives").Select(RequiredObject).ToArray();
            var newDerivatives = RequiredArray(newAttachment, "derivatives").Select(RequiredObject).ToArray();
            if (oldDerivatives.Length != newDerivatives.Length)
            {
                throw Error("ATTACHMENT-PREFIX", "Derivative lifecycle slots cannot be added or removed after attachment creation.");
            }

            for (var derivativeIndex = 0; derivativeIndex < oldDerivatives.Length; derivativeIndex++)
            {
                var oldDerivative = oldDerivatives[derivativeIndex];
                var newDerivative = newDerivatives[derivativeIndex];
                if (new[] { "derivativeId", "kind", "sourceSha256", "createdAt" }
                    .Any(field => !JsonNode.DeepEquals(oldDerivative[field], newDerivative[field])))
                {
                    throw Error("ATTACHMENT-PREFIX", "Derivative identity and source are immutable.");
                }

                var oldStatus = RequiredString(oldDerivative, "status");
                var newStatus = RequiredString(newDerivative, "status");
                var allowed = oldStatus == newStatus ||
                    oldStatus == "Pending" && newStatus is "Ready" or "Failed" or "Unsupported" or "NotRequired" ||
                    oldStatus == "Failed" && newStatus is "Pending" or "Ready";
                if (!allowed || oldStatus == "Ready" && !JsonNode.DeepEquals(oldDerivative, newDerivative))
                {
                    throw Error("ATTACHMENT-PREFIX", $"Derivative transition '{oldStatus}' -> '{newStatus}' is not lossless.");
                }
            }
        }
    }

    private static void ValidateAppendedAuditRuns(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var previousCount = RequiredArray(previous, "auditRuns").Count;
        var auditedRevision = RequiredLong(previous, "revision");
        var auditedContextDigest = AgentContextBuilderV3.ComputeDigest(previous);
        var auditedWorkspaceDigest = WorkspaceChangesBuilderV3.ComputeSnapshotDigest(RequiredObject(previous["workspaceChanges"]));
        var previousAttachmentIds = RequiredArray(previous, "attachments").Select(RequiredObject)
            .Select(attachment => RequiredString(attachment, "attachmentId")).ToHashSet(StringComparer.Ordinal);
        foreach (var run in RequiredArray(next, "auditRuns").Skip(previousCount).Select(RequiredObject))
        {
            RequireCapability(context, TaskBoardV3Capability.AcceptanceDecision, "AUDIT-RUN-CAPABILITY", "Appending an audit run requires AcceptanceDecision capability.");
            if (context.Role is not (TaskBoardV3Role.Auditor or TaskBoardV3Role.Owner))
            {
                throw Error("AUDIT-RUN-ROLE", "Appending an audit run requires trusted Auditor or Owner role.");
            }
            if (IsSelfAcceptance(previous, context.ActorId))
            {
                throw Error("AUDIT-RUN-SELF", "Task creator, assignee or worker cannot audit their own task.");
            }

            var identity = RequiredObject(run["auditorIdentity"]);
            if (RequiredString(identity, "actorId") != context.ActorId ||
                RequiredString(identity, "actorKind") != context.ActorKind ||
                RequiredString(identity, "role") != context.Role.ToString())
            {
                throw Error("AUDIT-RUN-ACTOR", "Audit run identity must match trusted mutation context.");
            }

            if (RequiredLong(run, "taskRevision") != auditedRevision ||
                RequiredLong(run, "recordedAtRevision") != RequiredLong(next, "revision") ||
                !string.Equals(RequiredString(run, "contextDigest"), auditedContextDigest, StringComparison.Ordinal) ||
                !string.Equals(RequiredString(run, "workspaceChangesDigest"), auditedWorkspaceDigest, StringComparison.Ordinal) ||
                !previousAttachmentIds.Contains(RequiredString(run, "reportAttachmentId")))
            {
                throw Error(
                    "AUDIT-RUN-CONTEXT",
                    "Audit run must reference the exact previous task revision, its canonical context digest and an already immutable report attachment.");
            }

            var package = RequiredObject(run["packageManifest"]);
            var packageAttachmentIds = RequiredArray(package, "inputAttachmentIds").Select(node => node!.GetValue<string>())
                .Concat(RequiredArray(package, "excludedAttachmentIds").Select(node => node!.GetValue<string>())).ToArray();
            if (packageAttachmentIds.Any(id => !previousAttachmentIds.Contains(id)) ||
                RequiredArray(package, "inputAttachmentIds").Select(node => node!.GetValue<string>()).Contains(RequiredString(run, "reportAttachmentId"), StringComparer.Ordinal))
            {
                throw Error("AUDIT-RUN-PACKAGE", "Audit package manifest references unknown inputs or includes its own report output.");
            }
        }
    }

    private static void ValidateTaskPatch(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var expectedPatch = TaskPatchV3.BuildPatch(previous, next);
        var previousActivityCount = RequiredArray(previous, "activity").Count;
        var appended = RequiredArray(next, "activity").Skip(previousActivityCount).Select(RequiredObject)
            .Where(entry => RequiredString(entry, "kind") == "TaskPatched").ToArray();
        if (expectedPatch.Count == 0)
        {
            if (appended.Length != 0) throw Error("TASK-PATCH-UNEXPECTED", "TaskPatched cannot be appended without a definition change.");
            return;
        }

        if (appended.Length != 1)
        {
            throw Error("TASK-PATCH-REQUIRED", "Definition changes require exactly one append-only TaskPatched event.");
        }

        var entry = appended[0];
        var payload = RequiredObject(entry["payload"]);
        if (RequiredString(entry, "actorId") != context.ActorId || RequiredString(entry, "actorKind") != context.ActorKind ||
            !JsonNode.DeepEquals(payload["patch"], expectedPatch) ||
            RequiredLong(payload, "fromRevision") != RequiredLong(previous, "revision") ||
            RequiredLong(payload, "toRevision") != RequiredLong(next, "revision") ||
            RequiredLong(payload, "activitySequence") != RequiredLong(entry, "sequence") ||
            RequiredString(payload, "hashProfile") != AgentContextBuilderV3.HashProfile ||
            RequiredString(payload, "beforeTaskCoreDigest") != TaskPatchV3.ComputeTaskCoreDigest(previous) ||
            RequiredString(payload, "afterTaskCoreDigest") != TaskPatchV3.ComputeTaskCoreDigest(next))
        {
            throw Error("TASK-PATCH-MISMATCH", "TaskPatched must exactly describe the trusted definition change and canonical before/after digests.");
        }
    }

    private static void ValidateWorkspaceChangesTransition(
        JsonObject previous,
        JsonObject next,
        TaskBoardV3MutationContext context)
    {
        var changed = !JsonNode.DeepEquals(previous["workspaceChanges"], next["workspaceChanges"]);
        var previousActivityCount = RequiredArray(previous, "activity").Count;
        var appended = RequiredArray(next, "activity").Skip(previousActivityCount).Select(RequiredObject)
            .Where(entry => RequiredString(entry, "kind") == "WorkspaceChangesUpdated").ToArray();
        if (!changed)
        {
            if (appended.Length != 0) throw Error("WORKSPACE-CHANGES-UNEXPECTED", "WorkspaceChangesUpdated cannot be appended without a trusted snapshot change.");
            return;
        }

        if (appended.Length != 1)
        {
            throw Error("WORKSPACE-CHANGES-REQUIRED", "Changing workspaceChanges requires exactly one append-only WorkspaceChangesUpdated event.");
        }

        var entry = appended[0];
        var payload = RequiredObject(entry["payload"]);
        if (RequiredString(entry, "actorId") != context.ActorId || RequiredString(entry, "actorKind") != context.ActorKind ||
            RequiredString(payload, "beforeDigest") != WorkspaceChangesBuilderV3.ComputeSnapshotDigest(RequiredObject(previous["workspaceChanges"])) ||
            RequiredString(payload, "afterDigest") != WorkspaceChangesBuilderV3.ComputeSnapshotDigest(RequiredObject(next["workspaceChanges"])) ||
            !JsonNode.DeepEquals(payload["workspaceChanges"], next["workspaceChanges"]))
        {
            throw Error("WORKSPACE-CHANGES-MISMATCH", "WorkspaceChangesUpdated must match the trusted before/after snapshots and identity.");
        }
    }

    private static void ValidateArrayPrefix(JsonArray previous, JsonArray next, string code, string description)
    {
        if (next.Count < previous.Count)
        {
            throw Error(code, $"{description} are append-only and cannot be truncated.");
        }

        for (var index = 0; index < previous.Count; index++)
        {
            if (!JsonNode.DeepEquals(previous[index], next[index]))
            {
                throw Error(code, $"Existing {description.ToLowerInvariant()} entry at index {index} is immutable.");
            }
        }
    }

    private static bool IsSelfAcceptance(JsonObject previous, string actorId)
    {
        if (string.Equals(NullableString(previous, "assignee"), actorId, StringComparison.Ordinal) ||
            string.Equals(NullableString(previous, "createdBy"), actorId, StringComparison.Ordinal))
        {
            return true;
        }

        var messages = RequiredArray(RequiredObject(previous["conversation"]), "messages").Select(RequiredObject);
        if (messages.Any(message =>
        {
            var author = RequiredObject(message["author"]);
            return RequiredString(author, "actorId") == actorId && RequiredString(author, "role") == "Worker";
        }))
        {
            return true;
        }

        return RequiredArray(previous, "activity").Select(RequiredObject).Any(entry =>
            RequiredString(entry, "actorId") == actorId &&
            RequiredString(entry, "actorKind") is "Agent" or "Cli" &&
            RequiredString(entry, "kind") != "AcceptanceResult");
    }

    private static void ValidateUpdatedAt(JsonObject previous, JsonObject next)
    {
        var previousUpdatedAt = DateTimeOffset.Parse(RequiredString(previous, "updatedAt"), CultureInfo.InvariantCulture);
        var nextUpdatedAt = DateTimeOffset.Parse(RequiredString(next, "updatedAt"), CultureInfo.InvariantCulture);
        if (nextUpdatedAt < previousUpdatedAt)
        {
            throw Error("UPDATED-AT", "updatedAt cannot move backwards.");
        }
    }

    private static void RequireRevision(JsonObject previous, JsonObject next)
    {
        var previousRevision = RequiredLong(previous, "revision");
        var nextRevision = RequiredLong(next, "revision");
        if (nextRevision != previousRevision + 1)
        {
            throw Error("REVISION", $"Revision must increase exactly by one; expected {previousRevision + 1}, actual {nextRevision}.");
        }
    }

    private static void ValidateContext(TaskBoardV3MutationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.SessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.Origin);
        if (context.ActorKind is not ("Human" or "Agent" or "Cli" or "ExternalFile" or "System" or "Test"))
        {
            throw Error("ACTOR-KIND", "Mutation context actor kind is not supported.");
        }
    }

    private static void RequireCapability(
        TaskBoardV3MutationContext context,
        TaskBoardV3Capability capability,
        string code,
        string message)
    {
        if (!context.Has(capability))
        {
            throw Error(code, message);
        }
    }

    private static string RequiredString(JsonObject value, string propertyName)
    {
        var text = value[propertyName]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw Error("SHAPE", $"Required string '{propertyName}' is missing or blank.");
        }

        return text;
    }

    private static string? NullableString(JsonObject value, string propertyName)
    {
        return value[propertyName]?.GetValue<string>();
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

        throw Error("SHAPE", $"Required integer '{propertyName}' is missing.");
    }

    private static JsonArray RequiredArray(JsonObject value, string propertyName)
    {
        return value[propertyName] as JsonArray ?? throw Error("SHAPE", $"Required array '{propertyName}' is missing.");
    }

    private static JsonObject RequiredObject(JsonNode? value)
    {
        return value as JsonObject ?? throw Error("SHAPE", "Required object is missing.");
    }

    private static TaskBoardV3ValidationException Error(string suffix, string message)
    {
        return new TaskBoardV3ValidationException(CodePrefix + suffix, message);
    }

    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedTransitions =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["Ready"] = ["InProgress", "Blocked", "Cancelled"],
            ["InProgress"] = ["Review", "Blocked", "Cancelled"],
            ["Blocked"] = ["Ready", "InProgress", "Cancelled"],
            ["Review"] = ["InProgress", "Done", "Cancelled"]
        };
}

internal static class TaskBoardV3TransitionValidator
{
    public static void ValidateTask(JsonObject previous, JsonObject next, TaskBoardV3MutationContext context)
    {
        TaskTransitionValidatorV3.ValidateTask(previous, next, context);
    }

    public static void ValidateBoard(JsonObject previous, JsonObject next, TaskBoardV3MutationContext context)
    {
        TaskTransitionValidatorV3.ValidateBoard(previous, next, context);
    }
}

internal static partial class TaskBoardSemanticValidatorV3
{
    private const string CodePrefix = "E2D-TASK-V3-SEMANTIC-";

    public static void Validate(
        string projectRoot,
        JsonObject board,
        IEnumerable<JsonObject> activeTasks,
        IEnumerable<JsonObject> completedTasks,
        bool validateAttachmentBlobs = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(activeTasks);
        ArgumentNullException.ThrowIfNull(completedTasks);

        try
        {
            ValidateCore(
                Path.GetFullPath(projectRoot),
                board,
                activeTasks.ToArray(),
                completedTasks.ToArray(),
                validateAttachmentBlobs);
        }
        catch (TaskBoardV3ValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or FormatException)
        {
            throw Error("SHAPE", $"Malformed v3 document: {exception.Message}");
        }
    }

    private static void ValidateCore(
        string projectRoot,
        JsonObject board,
        IReadOnlyList<JsonObject> activeTasks,
        IReadOnlyList<JsonObject> completedTasks,
        bool validateAttachmentBlobs)
    {
        TaskBoardV3SchemaValidator.ValidateBoard(board);
        RequireFormat(board, "Electron2D.TaskBoard");
        RequireVersion(board);
        var boardId = RequiredString(board, "boardId");
        var allTasks = activeTasks.Concat(completedTasks).ToArray();

        foreach (var task in allTasks)
        {
            TaskBoardV3SchemaValidator.ValidateTask(task);
            RequireFormat(task, "Electron2D.TaskFile");
            RequireVersion(task);
            if (!string.Equals(RequiredString(task, "boardId"), boardId, StringComparison.Ordinal))
            {
                throw Error("FOREIGN-BOARD", $"Task '{RequiredString(task, "taskId")}' belongs to another board.");
            }
        }

        EnsureUnique(allTasks, "taskUid", "DUPLICATE-TASK-UID");
        EnsureUnique(allTasks, "taskId", "DUPLICATE-TASK-ID");

        var tasksByUid = allTasks.ToDictionary(task => RequiredString(task, "taskUid"), StringComparer.Ordinal);
        var activeByUid = activeTasks.ToDictionary(task => RequiredString(task, "taskUid"), StringComparer.Ordinal);
        ValidateBoardCatalogs(board);
        ValidatePlacements(board, activeByUid, completedTasks);
        ValidateTaskChildren(board, allTasks, tasksByUid);
        ValidateGraphs(allTasks, tasksByUid);
        ValidateLifecycle(allTasks);
        ValidateAuditRunChains(allTasks);
        ValidateAttachments(projectRoot, board, allTasks, validateAttachmentBlobs);
    }

    private static void ValidateBoardCatalogs(JsonObject board)
    {
        var tags = RequiredArray(board, "tags").Select(RequiredObject).ToArray();
        EnsureUnique(tags, "tagId", "DUPLICATE-TAG-ID");
        EnsureUnique(tags, "name", "DUPLICATE-TAG-NAME", StringComparer.OrdinalIgnoreCase);

        var groups = RequiredArray(board, "groups").Select(RequiredObject).ToArray();
        EnsureUnique(groups, "groupId", "DUPLICATE-GROUP-ID");
        foreach (var group in groups)
        {
            RequireRank(RequiredString(group, "rank"), "group");
        }

        var groupIds = groups.Select(group => RequiredString(group, "groupId")).ToHashSet(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            var parent = NullableString(group, "parentGroupId");
            if (parent is not null && !groupIds.Contains(parent))
            {
                throw Error("DANGLING-GROUP-PARENT", $"Group '{RequiredString(group, "groupId")}' has unknown parent '{parent}'.");
            }
        }

        EnsureScopedRanksUnique(
            groups,
            group => NullableString(group, "parentGroupId") ?? "<root>",
            "DUPLICATE-GROUP-RANK");
        EnsureAcyclic(
            groups.ToDictionary(group => RequiredString(group, "groupId"), group => NullableString(group, "parentGroupId"), StringComparer.Ordinal),
            "GROUP-CYCLE");

        var policy = RequiredObject(board["attachmentPolicy"]);
        var perFile = RequiredLong(policy, "perFileByteLimit");
        var perTask = RequiredLong(policy, "perTaskByteLimit");
        var boardLimit = RequiredLong(policy, "boardByteLimit");
        if (perFile < 1 || perTask < perFile || boardLimit < perTask)
        {
            throw Error("ATTACHMENT-POLICY", "Attachment limits must satisfy 0 < per-file <= per-task <= board.");
        }
    }

    private static void ValidatePlacements(
        JsonObject board,
        IReadOnlyDictionary<string, JsonObject> activeTasks,
        IReadOnlyList<JsonObject> completedTasks)
    {
        var groups = RequiredArray(board, "groups").Select(RequiredObject)
            .Select(group => RequiredString(group, "groupId")).ToHashSet(StringComparer.Ordinal);
        var placements = RequiredArray(board, "placements").Select(RequiredObject).ToArray();
        EnsureUnique(placements, "taskUid", "DUPLICATE-PLACEMENT");

        foreach (var placement in placements)
        {
            var taskUid = RequiredString(placement, "taskUid");
            if (!activeTasks.ContainsKey(taskUid))
            {
                throw Error("ORPHAN-PLACEMENT", $"Placement references non-active task '{taskUid}'.");
            }

            var groupId = NullableString(placement, "groupId");
            if (groupId is not null && !groups.Contains(groupId))
            {
                throw Error("DANGLING-PLACEMENT-GROUP", $"Placement for '{taskUid}' references unknown group '{groupId}'.");
            }

            RequireRank(RequiredString(placement, "rank"), "placement");
        }

        var placementUids = placements.Select(placement => RequiredString(placement, "taskUid")).ToHashSet(StringComparer.Ordinal);
        if (!placementUids.SetEquals(activeTasks.Keys))
        {
            throw Error("PLACEMENT-COVERAGE", "Every active task must have exactly one placement and completed tasks must have none.");
        }

        var completedUids = completedTasks.Select(task => RequiredString(task, "taskUid")).ToHashSet(StringComparer.Ordinal);
        if (placementUids.Overlaps(completedUids))
        {
            throw Error("COMPLETED-PLACEMENT", "Completed task must not have a board placement.");
        }

        EnsureScopedRanksUnique(
            placements,
            placement => NullableString(placement, "groupId") ?? "<root>",
            "DUPLICATE-PLACEMENT-RANK");
    }

    private static void ValidateTaskChildren(
        JsonObject board,
        IReadOnlyList<JsonObject> tasks,
        IReadOnlyDictionary<string, JsonObject> tasksByUid)
    {
        var tagIds = RequiredArray(board, "tags").Select(RequiredObject)
            .Select(tag => RequiredString(tag, "tagId")).ToHashSet(StringComparer.Ordinal);

        foreach (var task in tasks)
        {
            EnsureUnique(RequiredArray(task, "relations").Select(RequiredObject), "relationId", "DUPLICATE-RELATION-ID");
            EnsureUnique(RequiredArray(task, "acceptanceCriteria").Select(RequiredObject), "criterionId", "DUPLICATE-CRITERION-ID");
            EnsureUnique(RequiredArray(task, "blockers").Select(RequiredObject), "blockerId", "DUPLICATE-BLOCKER-ID");
            EnsureUnique(RequiredArray(task, "activity").Select(RequiredObject), "activityEntryId", "DUPLICATE-ACTIVITY-ID");
            EnsureUnique(RequiredArray(task, "auditRuns").Select(RequiredObject), "runId", "DUPLICATE-AUDIT-RUN-ID");
            var conversation = RequiredObject(task["conversation"]);
            EnsureUnique(RequiredArray(conversation, "messages").Select(RequiredObject), "messageId", "DUPLICATE-MESSAGE-ID");
            EnsureUnique(RequiredArray(conversation, "contextCheckpoints").Select(RequiredObject), "checkpointId", "DUPLICATE-CHECKPOINT-ID");
            EnsureUnique(RequiredArray(task, "links").Select(RequiredObject), "linkId", "DUPLICATE-LINK-ID");
            EnsureUnique(RequiredArray(task, "attachments").Select(RequiredObject), "attachmentId", "DUPLICATE-ATTACHMENT-ID");
            foreach (var attachment in RequiredArray(task, "attachments").Select(RequiredObject))
            {
                EnsureUnique(RequiredArray(attachment, "derivatives").Select(RequiredObject), "derivativeId", "DUPLICATE-DERIVATIVE-ID");
                EnsureUnique(RequiredArray(attachment, "derivatives").Select(RequiredObject), "kind", "DUPLICATE-DERIVATIVE-KIND");
            }

            var commands = RequiredArray(RequiredObject(task["executionContract"]), "commands").Select(RequiredObject);
            EnsureUnique(commands, "commandId", "DUPLICATE-COMMAND-ID");

            foreach (var tagId in RequiredArray(task, "tagIds").Select(RequiredString))
            {
                if (!tagIds.Contains(tagId))
                {
                    throw Error("DANGLING-TAG", $"Task '{RequiredString(task, "taskId")}' references unknown tag '{tagId}'.");
                }
            }

            var taskUid = RequiredString(task, "taskUid");
            var attachmentIds = RequiredArray(task, "attachments").Select(RequiredObject)
                .Select(attachment => RequiredString(attachment, "attachmentId")).ToHashSet(StringComparer.Ordinal);
            foreach (var evidence in RequiredArray(task, "acceptanceCriteria").Select(RequiredObject)
                .SelectMany(criterion => RequiredArray(criterion, "evidence").Select(RequiredObject))
                .Where(evidence => RequiredString(evidence, "kind") == "Attachment"))
            {
                var attachmentId = RequiredString(evidence, "attachmentId");
                if (!attachmentIds.Contains(attachmentId))
                {
                    throw Error("DANGLING-EVIDENCE-ATTACHMENT", $"Criterion evidence references unknown attachment '{attachmentId}'.");
                }
            }

            var priorMessageIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var message in RequiredArray(conversation, "messages").Select(RequiredObject))
            {
                var replyTo = NullableString(message, "replyToMessageId");
                if (replyTo is not null && !priorMessageIds.Contains(replyTo))
                {
                    throw Error("DANGLING-MESSAGE-REPLY", $"Message '{RequiredString(message, "messageId")}' replies to an unknown or later message.");
                }

                foreach (var block in RequiredArray(message, "content").Select(RequiredObject)
                    .Where(block => RequiredString(block, "kind") == "Attachment"))
                {
                    var attachmentId = RequiredString(block, "attachmentId");
                    if (!attachmentIds.Contains(attachmentId))
                    {
                        throw Error("DANGLING-MESSAGE-ATTACHMENT", $"Message references unknown attachment '{attachmentId}'.");
                    }
                }

                priorMessageIds.Add(RequiredString(message, "messageId"));
            }

            var taskRevision = RequiredLong(task, "revision");
            var lastMessageSequence = RequiredLong(conversation, "lastMessageSequence");
            var priorCheckpointIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var checkpoint in RequiredArray(conversation, "contextCheckpoints").Select(RequiredObject))
            {
                if (RequiredLong(checkpoint, "taskRevision") > taskRevision ||
                    RequiredLong(checkpoint, "lastMessageSequence") > lastMessageSequence)
                {
                    throw Error("CONTEXT-CHECKPOINT-RANGE", "Context checkpoint references a future task revision or message sequence.");
                }

                var rebaseOf = NullableString(checkpoint, "rebaseOfCheckpointId");
                if (rebaseOf is not null && !priorCheckpointIds.Contains(rebaseOf))
                {
                    throw Error("DANGLING-CONTEXT-REBASE", $"Context checkpoint rebases unknown or later checkpoint '{rebaseOf}'.");
                }

                priorCheckpointIds.Add(RequiredString(checkpoint, "checkpointId"));
            }

            var parentUid = NullableString(task, "parentTaskUid");
            if (string.Equals(parentUid, taskUid, StringComparison.Ordinal))
            {
                throw Error("SELF-PARENT", $"Task '{taskUid}' cannot be its own parent.");
            }

            if (parentUid is not null && !tasksByUid.ContainsKey(parentUid))
            {
                throw Error("DANGLING-PARENT", $"Task '{taskUid}' references unknown parent '{parentUid}'.");
            }

            foreach (var relation in RequiredArray(task, "relations").Select(RequiredObject))
            {
                var targetUid = RequiredString(relation, "targetTaskUid");
                if (string.Equals(targetUid, taskUid, StringComparison.Ordinal))
                {
                    throw Error("SELF-RELATION", $"Task '{taskUid}' cannot reference itself.");
                }

                if (!tasksByUid.ContainsKey(targetUid))
                {
                    throw Error("DANGLING-RELATION", $"Task '{taskUid}' references unknown task '{targetUid}'.");
                }
            }
        }
    }

    private static void ValidateGraphs(
        IReadOnlyList<JsonObject> tasks,
        IReadOnlyDictionary<string, JsonObject> tasksByUid)
    {
        EnsureAcyclic(
            tasks.ToDictionary(task => RequiredString(task, "taskUid"), task => NullableString(task, "parentTaskUid"), StringComparer.Ordinal),
            "PARENT-CYCLE");

        var dependencyEdges = tasks.ToDictionary(
            task => RequiredString(task, "taskUid"),
            task => RequiredArray(task, "relations").Select(RequiredObject)
                .Where(relation => string.Equals(RequiredString(relation, "kind"), "DependsOn", StringComparison.Ordinal))
                .Select(relation => RequiredString(relation, "targetTaskUid")).ToArray(),
            StringComparer.Ordinal);
        EnsureAcyclic(dependencyEdges, tasksByUid.Keys, "DEPENDENCY-CYCLE");
    }

    private static void ValidateLifecycle(IReadOnlyList<JsonObject> tasks)
    {
        foreach (var task in tasks)
        {
            var status = RequiredString(task, "status");
            var criteria = RequiredArray(task, "acceptanceCriteria").Select(RequiredObject).ToArray();
            var activity = RequiredArray(task, "activity").Select(RequiredObject).ToArray();
            if (status == "Done")
            {
                if (criteria.Any(criterion => RequiredString(criterion, "state") != "Passed" || RequiredArray(criterion, "evidence").Count == 0))
                {
                    throw Error("DONE-CRITERIA", $"Done task '{RequiredString(task, "taskId")}' has a criterion that is not Passed.");
                }

                var acceptedBy = NullableString(task, "acceptedBy");
                var hasTrustedAcceptance = activity.Any(entry =>
                    RequiredString(entry, "kind") == "AcceptanceResult" &&
                    RequiredObject(entry["payload"]).TryGetPropertyValue("decision", out var decision) &&
                    decision?.GetValue<string>() == "Accepted" &&
                    RequiredObject(entry["payload"])["authorityActorId"]?.GetValue<string>() == acceptedBy &&
                    RequiredObject(entry["payload"])["authorityRole"]?.GetValue<string>() is "Auditor" or "Owner");
                if (acceptedBy is null || !hasTrustedAcceptance)
                {
                    throw Error("DONE-ACCEPTANCE", $"Done task '{RequiredString(task, "taskId")}' lacks typed trusted acceptance.");
                }
            }

            var hasActiveBlocker = RequiredArray(task, "blockers").Select(RequiredObject)
                .Any(blocker => RequiredString(blocker, "state") == "Active");
            if (status == "Blocked" && !hasActiveBlocker)
            {
                throw Error("BLOCKED-WITHOUT-BLOCKER", $"Blocked task '{RequiredString(task, "taskId")}' has no active explicit blocker.");
            }

            if (status != "Blocked" && hasActiveBlocker)
            {
                throw Error("ACTIVE-BLOCKER-STATUS", $"Task '{RequiredString(task, "taskId")}' has an active blocker outside Blocked status.");
            }

            if (NullableString(task, "archivedAt") is not null && status is not ("Done" or "Cancelled"))
            {
                throw Error("NONTERMINAL-ARCHIVE", $"Non-terminal task '{RequiredString(task, "taskId")}' cannot be archived.");
            }

            var createdAt = DateTimeOffset.Parse(RequiredString(task, "createdAt"), CultureInfo.InvariantCulture);
            var updatedAt = DateTimeOffset.Parse(RequiredString(task, "updatedAt"), CultureInfo.InvariantCulture);
            var historyTimestamps = activity.Select(entry => RequiredString(entry, "createdAt"))
                .Concat(RequiredArray(RequiredObject(task["conversation"]), "messages").Select(RequiredObject)
                    .Select(message => RequiredString(message, "createdAt")))
                .Concat(RequiredArray(RequiredObject(task["conversation"]), "contextCheckpoints").Select(RequiredObject)
                    .Select(checkpoint => RequiredString(checkpoint, "createdAt")))
                .Concat(RequiredArray(task, "auditRuns").Select(RequiredObject)
                    .Select(run => RequiredString(run, "createdAt")));
            if (historyTimestamps.Select(value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture))
                .Any(value => value < createdAt || value > updatedAt))
            {
                throw Error("HISTORY-TIMESTAMP", $"Task '{RequiredString(task, "taskId")}' has history outside createdAt..updatedAt.");
            }
        }
    }

    private static void ValidateAuditRunChains(IReadOnlyList<JsonObject> tasks)
    {
        foreach (var task in tasks)
        {
            var taskRevision = RequiredLong(task, "revision");
            var attachmentIds = RequiredArray(task, "attachments").Select(RequiredObject)
                .Select(attachment => RequiredString(attachment, "attachmentId")).ToHashSet(StringComparer.Ordinal);
            var runs = RequiredArray(task, "auditRuns").Select(RequiredObject).ToArray();
            var priorRunIds = new HashSet<string>(StringComparer.Ordinal);
            var priorRuns = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            foreach (var run in runs)
            {
                var runId = RequiredString(run, "runId");
                if (!priorRunIds.Add(runId))
                {
                    throw Error("AUDIT-RUNS", $"Audit run id '{runId}' is duplicated.");
                }

                if (RequiredLong(run, "taskRevision") > taskRevision)
                {
                    throw Error("AUDIT-RUNS", $"Audit run '{runId}' references a future task revision.");
                }
                if (RequiredLong(run, "recordedAtRevision") > taskRevision)
                {
                    throw Error("AUDIT-RUNS", $"Audit run '{runId}' was recorded in a future task revision.");
                }

                var reportAttachmentId = RequiredString(run, "reportAttachmentId");
                if (!attachmentIds.Contains(reportAttachmentId))
                {
                    throw Error("AUDIT-RUNS", $"Audit run '{runId}' references missing report attachment '{reportAttachmentId}'.");
                }

                foreach (var previousRunId in RequiredArray(run, "previousVerdictChain").Select(RequiredString))
                {
                    if (!priorRunIds.Contains(previousRunId))
                    {
                        throw Error("AUDIT-RUNS", $"Audit run '{runId}' references an unknown or later verdict '{previousRunId}'.");
                    }
                }

                var package = RequiredObject(run["packageManifest"]);
                foreach (var attachmentId in RequiredArray(package, "inputAttachmentIds").Select(RequiredString)
                    .Concat(RequiredArray(package, "excludedAttachmentIds").Select(RequiredString)))
                {
                    if (!attachmentIds.Contains(attachmentId))
                    {
                        throw Error("AUDIT-RUNS", $"Audit run '{runId}' package references missing attachment '{attachmentId}'.");
                    }
                }

                if (RequiredString(run, "stage") == "Control")
                {
                    var clean = RequiredObject(run["controlContext"]);
                    var primaryRunId = RequiredString(clean, "primaryRunId");
                    if (!priorRuns.TryGetValue(primaryRunId, out var primaryRun) || RequiredString(primaryRun, "stage") != "Primary" ||
                        !RequiredArray(run, "previousVerdictChain").Select(RequiredString).Contains(primaryRunId, StringComparer.Ordinal) ||
                        !RequiredArray(clean, "excludedRunIds").Select(RequiredString).Contains(primaryRunId, StringComparer.Ordinal) ||
                        !RequiredArray(clean, "excludedReportAttachmentIds").Select(RequiredString)
                            .Contains(RequiredString(primaryRun, "reportAttachmentId"), StringComparer.Ordinal))
                    {
                        throw Error("AUDIT-RUNS", $"Control run '{runId}' must chain to and exclude a prior Primary run and its report.");
                    }

                    var primaryIdentity = RequiredObject(primaryRun["auditorIdentity"]);
                    var controlIdentity = RequiredObject(run["auditorIdentity"]);
                    if (RequiredString(primaryIdentity, "actorId") == RequiredString(controlIdentity, "actorId"))
                    {
                        throw Error("AUDIT-RUNS", "Primary and Control audit runs must use independent auditor identities.");
                    }
                }

                priorRuns.Add(runId, run);

            }

            var acceptanceEntries = RequiredArray(task, "activity").Select(RequiredObject)
                .Where(entry => RequiredString(entry, "kind") == "AcceptanceResult").ToArray();
            foreach (var acceptance in acceptanceEntries)
            {
                var auditRunId = NullableString(RequiredObject(acceptance["payload"]), "auditRunId");
                if (auditRunId is not null && !priorRunIds.Contains(auditRunId))
                {
                    throw Error("AUDIT-RUNS", $"AcceptanceResult references missing audit run '{auditRunId}'.");
                }
            }

            if (RequiredString(task, "status") != "Done")
            {
                continue;
            }

            var acceptedBy = NullableString(task, "acceptedBy");
            if (acceptedBy is not null && string.Equals(acceptedBy, NullableString(task, "assignee"), StringComparison.Ordinal))
            {
                throw Error("AUDIT-RUNS", "Task assignee cannot accept their own task.");
            }

            var externalAudit = RequiredObject(RequiredObject(task["executionContract"])["externalAudit"]);
            var mode = RequiredString(externalAudit, "mode");
            if (mode == "None")
            {
                continue;
            }

            var finalRun = runs.LastOrDefault();
            if (finalRun is null || RequiredString(finalRun, "decision") != "Accepted" ||
                RequiredLong(finalRun, "recordedAtRevision") != taskRevision - 1 ||
                RequiredString(finalRun, "workspaceChangesDigest") != WorkspaceChangesBuilderV3.ComputeSnapshotDigest(RequiredObject(task["workspaceChanges"])))
            {
                throw Error("AUDIT-RUNS", "Done requires the latest accepted audit run for the immediately preceding task/workspace snapshot.");
            }

            JsonObject primary;
            if (mode == "Single")
            {
                if (RequiredString(finalRun, "stage") != "Primary")
                {
                    throw Error("AUDIT-RUNS", "Single audit acceptance requires the latest run to be an accepted Primary.");
                }
                primary = finalRun;
            }
            else
            {
                if (RequiredString(finalRun, "stage") != "Control")
                {
                    throw Error("AUDIT-RUNS", "PrimaryControl acceptance requires the latest run to be an accepted Control.");
                }
                var primaryRunId = RequiredString(RequiredObject(finalRun["controlContext"]), "primaryRunId");
                primary = priorRuns[primaryRunId];
                if (RequiredString(primary, "decision") != "Accepted")
                {
                    throw Error("AUDIT-RUNS", "PrimaryControl acceptance requires an accepted chained Primary run.");
                }
            }

            var acceptedEntry = acceptanceEntries.LastOrDefault(entry => RequiredObject(entry["payload"])["decision"]?.GetValue<string>() == "Accepted");
            if (acceptedEntry is null ||
                NullableString(RequiredObject(acceptedEntry["payload"]), "auditRunId") != RequiredString(finalRun, "runId"))
            {
                throw Error("AUDIT-RUNS", "AcceptanceResult must reference the final required audit run.");
            }
        }
    }

    private static void ValidateAttachments(
        string projectRoot,
        JsonObject board,
        IReadOnlyList<JsonObject> tasks,
        bool validateAttachmentBlobs)
    {
        var policy = RequiredObject(board["attachmentPolicy"]);
        var perFileLimit = RequiredLong(policy, "perFileByteLimit");
        var perTaskLimit = RequiredLong(policy, "perTaskByteLimit");
        var boardLimit = RequiredLong(policy, "boardByteLimit");
        long totalLength = 0;

        foreach (var task in tasks)
        {
            var taskUid = RequiredString(task, "taskUid");
            var attachments = RequiredArray(task, "attachments").Select(RequiredObject).ToArray();
            long taskLength = 0;
            foreach (var attachment in attachments)
            {
                var attachmentId = RequiredString(attachment, "attachmentId");
                var relativePath = RequiredString(attachment, "relativePath");
                var expectedPrefix = $".taskboard/attachments/{taskUid}/{attachmentId}/";
                if (!relativePath.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
                    relativePath.Length == expectedPrefix.Length ||
                    relativePath.Contains('\\', StringComparison.Ordinal) ||
                    relativePath.Split('/').Any(segment => segment is "" or "." or ".."))
                {
                    throw Error("ATTACHMENT-OWNERSHIP", $"Attachment '{attachmentId}' is outside its UID-owned path.");
                }

                var metadataLength = RequiredLong(attachment, "byteLength");
                if (metadataLength > perFileLimit)
                {
                    throw Error("ATTACHMENT-FILE-LIMIT", $"Attachment '{attachmentId}' exceeds the current board per-file limit.");
                }

                checked
                {
                    totalLength += metadataLength;
                    taskLength += metadataLength;
                }

                if (validateAttachmentBlobs)
                {
                    ValidateBlob(projectRoot, relativePath, attachmentId, metadataLength, RequiredString(attachment, "sha256"), ".taskboard/attachments");
                }

                foreach (var derivative in RequiredArray(attachment, "derivatives").Select(RequiredObject))
                {
                    var derivativeId = RequiredString(derivative, "derivativeId");
                    if (RequiredString(derivative, "sourceSha256") != RequiredString(attachment, "sha256"))
                    {
                        throw Error("DERIVATIVE-SOURCE", $"Derivative '{derivativeId}' does not reference the current original hash.");
                    }

                    if (RequiredString(derivative, "status") != "Ready")
                    {
                        continue;
                    }

                    var derivativePath = RequiredString(derivative, "relativePath");
                    var derivativePrefix = $".taskboard/derived/{taskUid}/{attachmentId}/{derivativeId}/";
                    if (!derivativePath.StartsWith(derivativePrefix, StringComparison.Ordinal) ||
                        derivativePath.Length == derivativePrefix.Length || derivativePath.Contains('\\') ||
                        derivativePath.Split('/').Any(segment => segment is "" or "." or ".."))
                    {
                        throw Error("DERIVATIVE-OWNERSHIP", $"Derivative '{derivativeId}' is outside its owned path.");
                    }

                    var derivativeLength = RequiredLong(derivative, "byteLength");
                    if (derivativeLength > perFileLimit)
                    {
                        throw Error("ATTACHMENT-FILE-LIMIT", $"Derivative '{derivativeId}' exceeds the current per-file limit.");
                    }

                    checked
                    {
                        totalLength += derivativeLength;
                        taskLength += derivativeLength;
                    }

                    if (validateAttachmentBlobs)
                    {
                        ValidateBlob(projectRoot, derivativePath, derivativeId, derivativeLength, RequiredString(derivative, "sha256"), ".taskboard/derived");
                    }
                }
            }

            if (taskLength > perTaskLimit)
            {
                throw Error("ATTACHMENT-TASK-LIMIT", $"Task '{RequiredString(task, "taskId")}' exceeds the current per-task blob limit.");
            }

            var previewId = NullableString(task, "previewAttachmentId");
            if (previewId is not null)
            {
                var preview = attachments.SingleOrDefault(attachment =>
                    string.Equals(RequiredString(attachment, "attachmentId"), previewId, StringComparison.Ordinal));
                if (preview is null || !IsRasterMediaType(RequiredString(preview, "mediaType")))
                {
                    throw Error("ATTACHMENT-PREVIEW", $"Preview '{previewId}' is not an owned raster attachment.");
                }
            }
        }

        if (totalLength > boardLimit)
        {
            throw Error("ATTACHMENT-BOARD-LIMIT", "Attachment blobs exceed the current board-wide limit.");
        }
    }

    private static void ValidateBlob(
        string projectRoot,
        string relativePath,
        string blobId,
        long metadataLength,
        string metadataHash,
        string relativeRoot)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        EnsureContained(projectRoot, absolutePath, blobId, relativeRoot);
        EnsureNoReparsePoint(projectRoot, absolutePath, blobId);
        if (!File.Exists(absolutePath) || (File.GetAttributes(absolutePath) & FileAttributes.Directory) != 0)
        {
            throw Error("ATTACHMENT-MISSING", $"Blob '{relativePath}' is not an existing regular file.");
        }

        var actualLength = new FileInfo(absolutePath).Length;
        if (actualLength != metadataLength)
        {
            throw Error("ATTACHMENT-LENGTH", $"Blob '{blobId}' byte length does not match metadata.");
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(absolutePath))).ToLowerInvariant();
        if (!string.Equals(actualHash, metadataHash, StringComparison.Ordinal))
        {
            throw Error("ATTACHMENT-HASH", $"Blob '{blobId}' SHA-256 does not match metadata.");
        }
    }

    private static void EnsureContained(string projectRoot, string absolutePath, string attachmentId, string relativeRoot)
    {
        var attachmentRoot = Path.GetFullPath(Path.Combine(projectRoot, relativeRoot.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = attachmentRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!absolutePath.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw Error("ATTACHMENT-CONTAINMENT", $"Attachment '{attachmentId}' escapes the attachment root.");
        }
    }

    private static void EnsureNoReparsePoint(string projectRoot, string absolutePath, string attachmentId)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(absolutePath)!);
        var root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        while (current is not null && current.FullName.Length >= root.Length)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw Error("ATTACHMENT-REPARSE", $"Attachment '{attachmentId}' traverses a reparse point.");
            }

            if (string.Equals(current.FullName.TrimEnd(Path.DirectorySeparatorChar), root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                break;
            }

            current = current.Parent;
        }

        if (File.Exists(absolutePath) && (File.GetAttributes(absolutePath) & FileAttributes.ReparsePoint) != 0)
        {
            throw Error("ATTACHMENT-REPARSE", $"Attachment '{attachmentId}' is a reparse point.");
        }
    }

    private static bool IsRasterMediaType(string mediaType)
    {
        return mediaType.ToLowerInvariant() is "image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp";
    }

    private static void EnsureScopedRanksUnique(
        IEnumerable<JsonObject> values,
        Func<JsonObject, string> scopeSelector,
        string code)
    {
        var duplicate = values
            .GroupBy(value => (Scope: scopeSelector(value), Rank: RequiredString(value, "rank")))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw Error(code, $"Rank '{duplicate.Key.Rank}' is duplicated in scope '{duplicate.Key.Scope}'.");
        }
    }

    private static void EnsureUnique(
        IEnumerable<JsonObject> values,
        string propertyName,
        string code,
        IEqualityComparer<string>? comparer = null)
    {
        var seen = new HashSet<string>(comparer ?? StringComparer.Ordinal);
        foreach (var value in values)
        {
            var identity = RequiredString(value, propertyName);
            if (!seen.Add(identity))
            {
                throw Error(code, $"Value '{identity}' is duplicated for '{propertyName}'.");
            }
        }
    }

    private static void EnsureAcyclic(IReadOnlyDictionary<string, string?> edges, string code)
    {
        EnsureAcyclic(
            edges.ToDictionary(pair => pair.Key, pair => pair.Value is null ? Array.Empty<string>() : [pair.Value], StringComparer.Ordinal),
            edges.Keys,
            code);
    }

    private static void EnsureAcyclic(
        IReadOnlyDictionary<string, string[]> edges,
        IEnumerable<string> vertices,
        string code)
    {
        var states = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var vertex in vertices)
        {
            Visit(vertex);
        }

        void Visit(string vertex)
        {
            if (states.TryGetValue(vertex, out var state))
            {
                if (state == 1)
                {
                    throw Error(code, $"Cycle detected at '{vertex}'.");
                }

                return;
            }

            states[vertex] = 1;
            if (edges.TryGetValue(vertex, out var targets))
            {
                foreach (var target in targets)
                {
                    Visit(target);
                }
            }

            states[vertex] = 2;
        }
    }

    private static void RequireFormat(JsonObject document, string expected)
    {
        if (!string.Equals(RequiredString(document, "format"), expected, StringComparison.Ordinal))
        {
            throw Error("SHAPE", $"Expected format '{expected}'.");
        }
    }

    private static void RequireVersion(JsonObject document)
    {
        if (RequiredLong(document, "version") != 3)
        {
            throw Error("VERSION", "Semantic validation requires a v3 snapshot.");
        }
    }

    private static string RequiredString(JsonObject value, string propertyName)
    {
        return RequiredString(value[propertyName]);
    }

    private static string RequiredString(JsonNode? value)
    {
        var text = value?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw Error("SHAPE", "Required string is missing or blank.");
        }

        return text;
    }

    private static string? NullableString(JsonObject value, string propertyName)
    {
        var node = value[propertyName];
        if (node is null)
        {
            return null;
        }

        return RequiredString(node);
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

        throw Error("SHAPE", $"Required integer '{propertyName}' is missing.");
    }

    private static JsonArray RequiredArray(JsonObject value, string propertyName)
    {
        return value[propertyName] as JsonArray ?? throw Error("SHAPE", $"Required array '{propertyName}' is missing.");
    }

    private static JsonObject RequiredObject(JsonNode? value)
    {
        return value as JsonObject ?? throw Error("SHAPE", "Required object is missing.");
    }

    private static void RequireRank(string rank, string kind)
    {
        if (!RankPattern().IsMatch(rank))
        {
            throw Error("RANK-FORMAT", $"The {kind} rank '{rank}' is not a canonical 12-digit rank.");
        }
    }

    private static TaskBoardV3ValidationException Error(string suffix, string message)
    {
        return new TaskBoardV3ValidationException(CodePrefix + suffix, message);
    }

    [GeneratedRegex("^[0-9]{12}$", RegexOptions.CultureInvariant)]
    private static partial Regex RankPattern();
}

internal static class TaskBoardV3SemanticValidator
{
    public static void Validate(
        string projectRoot,
        JsonObject board,
        IEnumerable<JsonObject> activeTasks,
        IEnumerable<JsonObject> completedTasks,
        bool validateAttachmentBlobs = true)
    {
        TaskBoardSemanticValidatorV3.Validate(projectRoot, board, activeTasks, completedTasks, validateAttachmentBlobs);
    }
}
