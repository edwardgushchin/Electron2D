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
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class TaskBoardV3ContractTests
{
    private const string TaskUidA = "task-11111111111111111111111111111111";
    private const string TaskUidB = "task-22222222222222222222222222222222";

    [Theory]
    [InlineData("task-file-v3.schema.json", "Electron2D task file v3 schema")]
    [InlineData("task-board-v3.schema.json", "Electron2D task board v3 schema")]
    public void V3SchemasArePublishedAsClosedDraft202012Documents(string fileName, string title)
    {
        using var schema = ReadSchema(fileName);

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema.RootElement.GetProperty("$schema").GetString());
        Assert.Equal(title, schema.RootElement.GetProperty("title").GetString());
        Assert.Equal("object", schema.RootElement.GetProperty("type").GetString());
        Assert.False(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
    }

    [Fact]
    public void BoardV3TagColorAcceptsLegacyNamesAndCanonicalHexOnly()
    {
        using var schema = ReadSchema("task-board-v3.schema.json");
        var colorSchema = schema.RootElement.GetProperty("properties").GetProperty("tags")
            .GetProperty("items").GetProperty("properties").GetProperty("color");
        var variants = colorSchema.GetProperty("anyOf").EnumerateArray().ToArray();
        Assert.Contains(variants, variant => variant.TryGetProperty("enum", out _));
        Assert.Contains(variants, variant =>
            variant.TryGetProperty("pattern", out var pattern) && pattern.GetString() == "^#[0-9A-F]{6}$");

        var legacy = CreateBoard();
        legacy["tags"]!.AsArray().Add(new JsonObject
        {
            ["tagId"] = "tag-legacy",
            ["name"] = "Legacy",
            ["color"] = "Blue"
        });
        TaskBoardV3SchemaValidator.ValidateBoard(legacy);

        var custom = CreateBoard();
        custom["tags"]!.AsArray().Add(new JsonObject
        {
            ["tagId"] = "tag-custom",
            ["name"] = "Custom",
            ["color"] = "#A1B2C3"
        });
        TaskBoardV3SchemaValidator.ValidateBoard(custom);

        foreach (var invalidColor in new[] { "#ABC", "#A1B2C3DD", "a1b2c3", "rgba(1,2,3,1)" })
        {
            var invalid = CreateBoard();
            invalid["tags"]!.AsArray().Add(new JsonObject
            {
                ["tagId"] = "tag-invalid",
                ["name"] = "Invalid",
                ["color"] = invalidColor
            });
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateBoard(invalid));
        }
    }

    [Fact]
    public void TaskV3UsesImmutableUidReferencesAndOneHierarchySource()
    {
        using var schema = ReadSchema("task-file-v3.schema.json");
        var properties = schema.RootElement.GetProperty("properties");
        var required = schema.RootElement.GetProperty("required").EnumerateArray()
            .Select(value => value.GetString()).ToArray();

        Assert.Contains("boardId", required);
        Assert.Contains("taskUid", required);
        Assert.True(properties.TryGetProperty("parentTaskUid", out _));
        Assert.True(properties.TryGetProperty("relations", out var relations));
        Assert.False(properties.TryGetProperty("parentTaskId", out _));
        Assert.False(properties.TryGetProperty("dependencies", out _));
        Assert.False(properties.TryGetProperty("subtasks", out _));

        var relationProperties = ResolveLocalReference(schema, relations.GetProperty("items")).GetProperty("properties");
        Assert.True(relationProperties.TryGetProperty("relationId", out _));
        Assert.True(relationProperties.TryGetProperty("targetTaskUid", out _));
    }

    [Fact]
    public void TaskV3RequiresIndependentActivityWatermarkAndAuthoritativeWorkspaceChanges()
    {
        using var schema = ReadSchema("task-file-v3.schema.json");
        var required = schema.RootElement.GetProperty("required").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        Assert.Contains("lastActivitySequence", required);
        Assert.Contains("workspaceChanges", required);

        var task = CreateTask(TaskUidA, "T-0001");
        task["lastActivitySequence"] = 1;
        task["activity"]!.AsArray().Add(CreateStatusActivity("activity-1", 2));

        var error = Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(task));
        Assert.Equal("E2D-TASK-V3-SCHEMA-ACTIVITY-SEQUENCE", error.Code);
    }

    [Fact]
    public void TaskPatchStoresReplayableRevisionOldValueAndActivitySequence()
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        var next = previous.DeepClone().AsObject();
        next["title"] = "Изменённая постановка";

        TaskPatchV3.AppendIfRequired(previous, next, "agent-1", "Agent", DateTimeOffset.Parse("2026-07-13T00:01:00+03:00"));

        var entry = Assert.IsType<JsonObject>(Assert.Single(next["activity"]!.AsArray()));
        Assert.Equal(1, entry["sequence"]!.GetValue<long>());
        Assert.Equal(1, next["lastActivitySequence"]!.GetValue<long>());
        var payload = Assert.IsType<JsonObject>(entry["payload"]);
        Assert.Equal(1, payload["fromRevision"]!.GetValue<long>());
        Assert.Equal(2, payload["toRevision"]!.GetValue<long>());
        Assert.Equal("sha256-jcs-rfc8785-v1", payload["hashProfile"]!.GetValue<string>());
        Assert.Equal(1, payload["activitySequence"]!.GetValue<long>());
        var operation = Assert.IsType<JsonObject>(Assert.Single(payload["patch"]!.AsArray()));
        Assert.Equal("Задача", operation["oldValue"]!.GetValue<string>());
        Assert.Equal("Изменённая постановка", operation["value"]!.GetValue<string>());
    }

    [Fact]
    public void TransitionCasBindsRevisionMessageAndActivityWatermarks()
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next["updatedAt"] = "2026-07-13T00:01:00+03:00";

        var error = Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            next,
            new TaskBoardV3MutationContext(
                "agent-1",
                TaskBoardV3Capability.EditTask,
                ExpectedRevision: 1,
                ExpectedLastMessageSequence: 0,
                ExpectedLastActivitySequence: 9)));

        Assert.Equal("E2D-TASK-V3-TRANSITION-CAS-WATERMARK", error.Code);
    }

    [Fact]
    public void AgentContextManifestHashesActivityWatermarkAndWorkspaceChanges()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        task["lastActivitySequence"] = 1;
        task["activity"]!.AsArray().Add(CreateStatusActivity("activity-1", 1));
        task["workspaceChanges"] = new JsonObject
        {
            ["baseRevision"] = "workspace:" + new string('1', 64),
            ["currentRevision"] = "workspace:" + new string('2', 64),
            ["files"] = new JsonArray(new JsonObject
            {
                ["path"] = "src/Task.cs",
                ["changeKind"] = "Modified",
                ["previousPath"] = null,
                ["baseSha256"] = new string('3', 64),
                ["currentSha256"] = new string('4', 64),
                ["firstChangedAt"] = "2026-07-13T00:00:00+03:00",
                ["lastChangedAt"] = "2026-07-13T00:01:00+03:00",
                ["agentRunIds"] = new JsonArray("run-1")
            })
        };

        var manifest = AgentContextBuilderV3.BuildManifest(task);

        Assert.Equal(1, manifest["throughActivitySequence"]!.GetValue<long>());
        Assert.Matches("^[a-f0-9]{64}$", manifest["workspaceChangesDigest"]!.GetValue<string>());
        var before = AgentContextBuilderV3.ComputeDigest(task);
        task["workspaceChanges"]!["files"]![0]!["currentSha256"] = new string('5', 64);
        Assert.NotEqual(before, AgentContextBuilderV3.ComputeDigest(task));
    }

    [Fact]
    public void DotNetAndJavaScriptShareTheSameJcsGoldenCorpus()
    {
        var fixturePath = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Electron2D.Tests.Integration",
            "Fixtures",
            "taskboard-v3-jcs-golden.json");
        var fixture = JsonNode.Parse(File.ReadAllText(fixturePath))!.AsObject();
        Assert.Equal(AgentContextBuilderV3.HashProfile, fixture["profile"]!.GetValue<string>());
        foreach (var testCase in fixture["cases"]!.AsArray().Select(node => node!.AsObject()))
        {
            Assert.Equal(testCase["canonical"]!.GetValue<string>(), AgentContextBuilderV3.WriteCanonical(testCase["value"]));
            Assert.Equal(testCase["sha256"]!.GetValue<string>(), AgentContextBuilderV3.HashCanonical(testCase["value"]));
        }

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "node",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("""
            const fs=require("fs"),crypto=require("crypto");
            const fixture=JSON.parse(fs.readFileSync(process.argv[1],"utf8"));
            const j=v=>v===null||typeof v!=="object"?JSON.stringify(v):Array.isArray(v)?"["+v.map(j).join(",")+"]":"{"+Object.keys(v).sort().map(k=>JSON.stringify(k)+":"+j(v[k])).join(",")+"}";
            for(const x of fixture.cases){const actual=j(x.value);if(actual!==x.canonical||crypto.createHash("sha256").update(actual,"utf8").digest("hex")!==x.sha256)process.exit(2);}
            """);
        startInfo.ArgumentList.Add(fixturePath);
        using var process = System.Diagnostics.Process.Start(startInfo) ?? throw new InvalidOperationException("Node.js golden verifier did not start.");
        Assert.True(process.WaitForExit(10_000), "Node.js golden verifier timed out.");
        Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
    }

    [Fact]
    public void ContextSnapshotProducesSummaryPlusExactRawTailsWithoutWindowTruncation()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        AddMessage(task, "message-1", 1, "Один");
        AddMessage(task, "message-2", 2, "Два");
        AddMessage(task, "message-3", 3, "Три");
        task["activity"]!.AsArray().Add(CreateStatusActivity("activity-1", 1));
        task["activity"]!.AsArray().Add(CreateStatusActivity("activity-2", 2));
        task["lastActivitySequence"] = 2;
        var manifest = AgentContextBuilderV3.BuildManifest(task, 1, 1, 1);
        task["contextSnapshot"] = new JsonObject
        {
            ["contextRevision"] = 1,
            ["builderProfile"] = AgentContextBuilderV3.BuilderProfile,
            ["hashProfile"] = AgentContextBuilderV3.HashProfile,
            ["throughTaskRevision"] = 1,
            ["throughMessageSequence"] = 1,
            ["throughActivitySequence"] = 1,
            ["contextDigest"] = AgentContextBuilderV3.HashCanonical(manifest),
            ["sourceManifest"] = manifest.DeepClone(),
            ["summaryMarkdown"] = "Сводка через sequence 1.",
            ["createdAt"] = "2026-07-13T00:01:00+03:00",
            ["createdBy"] = new JsonObject { ["actorId"] = "agent-1", ["actorKind"] = "Agent", ["role"] = "Worker" },
            ["model"] = "test"
        };

        var context = AgentContextBuilderV3.Build(task, recentMessageCount: 0);

        Assert.Equal("Сводка через sequence 1.", context["summaryMarkdown"]!.GetValue<string>());
        Assert.Equal(new long[] { 2, 3 }, context["recentMessages"]!.AsArray().Select(node => node!["sequence"]!.GetValue<long>()));
        Assert.Equal(new long[] { 2 }, context["activity"]!.AsArray().Select(node => node!["sequence"]!.GetValue<long>()));
    }

    [Fact]
    public void WorkspaceChangesAreBuiltFromRealFilesAndExcludeTaskboardInternals()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-WorkspaceChanges-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(projectRoot, "src"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard"));
        try
        {
            File.WriteAllText(Path.Combine(projectRoot, "src", "A.cs"), "before");
            File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks"), "ignored");
            var baseline = WorkspaceChangesBuilderV3.CaptureManifest(projectRoot);
            File.WriteAllText(Path.Combine(projectRoot, "src", "A.cs"), "after");
            File.WriteAllText(Path.Combine(projectRoot, "src", "B.cs"), "added");
            var current = WorkspaceChangesBuilderV3.CaptureManifest(projectRoot);

            var task = CreateTask(TaskUidA, "T-0001");
            task["executionContract"]!["allowedChanges"] = new JsonArray("path:src/**");
            var snapshot = WorkspaceChangesBuilderV3.Build(
                task,
                baseline,
                current,
                "workspace:" + WorkspaceChangesBuilderV3.ComputeManifestDigest(baseline),
                DateTimeOffset.Parse("2026-07-13T00:02:00+03:00"));

            var files = snapshot["files"]!.AsArray().Select(node => node!.AsObject()).ToArray();
            Assert.Equal(new[] { "src/A.cs", "src/B.cs" }, files.Select(file => file["path"]!.GetValue<string>()));
            Assert.Equal(new[] { "Modified", "Added" }, files.Select(file => file["changeKind"]!.GetValue<string>()));
            WorkspaceChangesBuilderV3.ValidateCurrentWorkspace(projectRoot, snapshot);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void ReviewTransitionRecomputesAndPersistsTrustedWorkspaceChanges()
    {
        var projectRoot = CreateNativeV3Project();
        Directory.CreateDirectory(Path.Combine(projectRoot, "src"));
        File.WriteAllText(Path.Combine(projectRoot, "src", "Feature.cs"), "before");
        try
        {
            var contract = CreateTask(TaskUidA, "T-0001")["executionContract"]!.DeepClone().AsObject();
            contract["allowedChanges"] = new JsonArray("path:src/**");
            var store = new TaskBoardV3DiskStore(projectRoot);
            var created = store.Create(
                "Изменение workspace",
                string.Empty,
                "P1",
                deadline: null,
                structuredInput: new JsonObject { ["executionContract"] = contract },
                expectedBoardRevision: null,
                actorId: "agent-1",
                now: DateTimeOffset.Parse("2026-07-13T00:00:00+03:00"),
                dryRun: false);
            var taskId = created.Task["taskId"]!.GetValue<string>();
            store.SetStatus(taskId, "InProgress", 1, "agent-1", "Agent", "Начало", DateTimeOffset.Parse("2026-07-13T00:01:00+03:00"), false);
            File.WriteAllText(Path.Combine(projectRoot, "src", "Feature.cs"), "after");

            var review = store.SetStatus(taskId, "Review", 2, "agent-1", "Agent", "На проверку", DateTimeOffset.Parse("2026-07-13T00:02:00+03:00"), false);

            var file = Assert.IsType<JsonObject>(Assert.Single(review.Task["workspaceChanges"]!["files"]!.AsArray()));
            Assert.Equal("src/Feature.cs", file["path"]!.GetValue<string>());
            Assert.Equal("Modified", file["changeKind"]!.GetValue<string>());
            Assert.Equal(4, review.Task["lastActivitySequence"]!.GetValue<long>());
            Assert.Equal("WorkspaceChangesUpdated", review.Task["activity"]![2]!["kind"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void TaskV3DefinesConditionalLifecycleAndTypedPayloads()
    {
        using var schema = ReadSchema("task-file-v3.schema.json");
        var properties = schema.RootElement.GetProperty("properties");

        Assert.True(schema.RootElement.TryGetProperty("allOf", out var lifecycleRules));
        Assert.True(lifecycleRules.GetArrayLength() >= 6);

        var commands = properties.GetProperty("executionContract").GetProperty("properties")
            .GetProperty("commands").GetProperty("items");
        Assert.True(commands.TryGetProperty("oneOf", out var commandKinds));
        Assert.Equal(2, commandKinds.GetArrayLength());

        var activityItem = properties.GetProperty("activity").GetProperty("items");
        Assert.True(activityItem.TryGetProperty("oneOf", out var activityKinds));
        Assert.True(activityKinds.GetArrayLength() >= 8);
    }

    [Fact]
    public void V3ContractNamesMandatoryValidatorsAndFixesSchemaLocalDefects()
    {
        using var boardSchema = ReadSchema("task-board-v3.schema.json");
        using var taskSchema = ReadSchema("task-file-v3.schema.json");

        var boardRequired = boardSchema.RootElement.GetProperty("required").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        Assert.Contains("validationContract", boardRequired);
        var validators = boardSchema.RootElement.GetProperty("properties").GetProperty("validationContract")
            .GetProperty("properties");
        Assert.Equal("TaskBoardSemanticValidatorV3", validators.GetProperty("semanticValidator").GetProperty("const").GetString());
        Assert.Equal("TaskTransitionValidatorV3", validators.GetProperty("transitionValidator").GetProperty("const").GetString());
        Assert.Equal("AgentContextBuilderV3", validators.GetProperty("contextBuilder").GetProperty("const").GetString());
        Assert.Equal("TaskExecutionPolicyV3", validators.GetProperty("executionPolicy").GetProperty("const").GetString());
        Assert.True(validators.GetProperty("formatAssertions").GetProperty("const").GetBoolean());

        var boardProperties = boardSchema.RootElement.GetProperty("properties");
        Assert.StartsWith("^[A-Za-z0-9]", boardProperties.GetProperty("idPolicy").GetProperty("properties")
            .GetProperty("prefix").GetProperty("pattern").GetString(), StringComparison.Ordinal);
        var migrationProperties = boardProperties.GetProperty("migration").GetProperty("oneOf")[1]
            .GetProperty("properties");
        Assert.True(migrationProperties.TryGetProperty("reportPath", out _));

        var blockersContains = taskSchema.RootElement.GetProperty("allOf")[2].GetProperty("then")
            .GetProperty("properties").GetProperty("blockers").GetProperty("contains");
        Assert.Equal("object", blockersContains.GetProperty("type").GetString());
        Assert.False(taskSchema.RootElement.GetProperty("properties").GetProperty("activity").TryGetProperty("maxItems", out _));
    }

    [Fact]
    public void TaskV3CarriesLosslessConversationContextAndTypedExecutionRequests()
    {
        using var schema = ReadSchema("task-file-v3.schema.json");
        var required = schema.RootElement.GetProperty("required").EnumerateArray()
            .Select(value => value.GetString()).ToArray();
        Assert.Contains("assignee", required);
        Assert.Contains("conversation", required);
        Assert.Contains("contextSnapshot", required);

        var task = CreateTask(TaskUidA, "T-0001");
        task["executionContract"]!["commands"]!.AsArray().Add(CreateProcessCommand("command-1", "dotnet", "WorkspaceRead"));
        TaskBoardV3SchemaValidator.ValidateTask(task);

        var command = task["executionContract"]!["commands"]![0]!.AsObject();
        command["platforms"] = new JsonArray("Any", "Windows");
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-PLATFORMS",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(task)).Code);
    }

    [Fact]
    public void LifecycleRejectsMissingEvidenceActiveBlockersAndInconsistentResolution()
    {
        var done = CreateTask(TaskUidA, "T-0001", status: "Done");
        done["acceptanceCriteria"]![0]!["evidence"] = new JsonArray();
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-EVIDENCE-REQUIRED",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(done)).Code);

        var ready = CreateTask(TaskUidA, "T-0001");
        ready["blockers"]!.AsArray().Add(CreateBlocker("Active", null, null));
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-LIFECYCLE",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(ready)).Code);

        var resolved = CreateTask(TaskUidA, "T-0001", status: "Blocked");
        resolved["blockers"]!.AsArray().Add(CreateBlocker("Resolved", null, null));
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-BLOCKER-RESOLUTION",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(resolved)).Code);

        var activeWithResolution = CreateTask(TaskUidA, "T-0001", status: "Blocked");
        activeWithResolution["blockers"]!.AsArray().Add(CreateBlocker(
            "Active",
            "2026-07-13T00:01:00+03:00",
            "worker-1"));
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-BLOCKER-RESOLUTION",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(activeWithResolution)).Code);

        var doneWithoutSubmission = CreateTask(TaskUidA, "T-0001", status: "Done");
        doneWithoutSubmission["submittedAt"] = null;
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-LIFECYCLE",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(doneWithoutSubmission)).Code);

        var unordered = CreateTask(TaskUidA, "T-0001", status: "Done");
        unordered["submittedAt"] = "2026-07-13T00:02:00+03:00";
        unordered["completedAt"] = "2026-07-13T00:01:00+03:00";
        unordered["acceptedAt"] = "2026-07-13T00:03:00+03:00";
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-TIMESTAMP-ORDER",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(unordered)).Code);

        var rejected = CreateTask(TaskUidA, "T-0001", status: "Done");
        rejected["activity"]![0]!["payload"]!["decision"] = "Rejected";
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-ENUM",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(rejected)).Code);
    }

    [Fact]
    public void TransitionRequiresTrustedIndependentAuditorOrOwner()
    {
        var previous = CreateTask(TaskUidA, "T-0001", status: "Review");
        previous["acceptanceState"] = "Submitted";
        previous["submittedAt"] = "2026-07-13T00:00:00+03:00";
        var done = CreateTask(TaskUidA, "T-0001", status: "Done");
        done["revision"] = 2;
        done["archivedAt"] = null;
        done["archivedBy"] = null;

        SetAcceptanceAuthority(done, "worker-1", "Agent", "Auditor");
        var selfError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                done,
                new TaskBoardV3MutationContext(
                    "worker-1",
                    TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus | TaskBoardV3Capability.AcceptanceDecision,
                    TaskBoardV3Role.Auditor,
                    "Agent")));
        Assert.Equal("E2D-TASK-V3-TRANSITION-SELF-ACCEPTANCE", selfError.Code);

        SetAcceptanceAuthority(done, "auditor-2", "Agent", "Auditor");
        var roleError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                done,
                new TaskBoardV3MutationContext(
                    "auditor-2",
                    TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus | TaskBoardV3Capability.AcceptanceDecision,
                    TaskBoardV3Role.Worker,
                    "Agent")));
        Assert.Equal("E2D-TASK-V3-TRANSITION-ACCEPTANCE-ROLE", roleError.Code);

        TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            done,
            new TaskBoardV3MutationContext(
                "auditor-2",
                TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus | TaskBoardV3Capability.AcceptanceDecision,
                TaskBoardV3Role.Auditor,
                "Agent"));
    }

    [Fact]
    public void ExecutionPolicyDeniesShellBypassAndDoesNotTrustRequestedCapabilities()
    {
        var shell = CreateProcessCommand("command-shell", "bash", "WorkspaceRead");
        Assert.Equal(
            "E2D-TASK-V3-EXECUTION-SHELL-DENIED",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskExecutionPolicyV3.Authorize(
                shell,
                new TaskExecutionGrantV3(["WorkspaceRead"], AllowShellInterpreter: false, HumanConfirmed: false, AllowedExecutables: ["bash"]))).Code);

        var write = CreateProcessCommand("command-write", "dotnet", "WorkspaceWrite");
        Assert.Equal(
            "E2D-TASK-V3-EXECUTION-CAPABILITY-DENIED",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskExecutionPolicyV3.Authorize(
                write,
                new TaskExecutionGrantV3(["WorkspaceRead"], AllowShellInterpreter: false, HumanConfirmed: false, AllowedExecutables: ["dotnet"]))).Code);

        TaskExecutionPolicyV3.Authorize(
            CreateProcessCommand("command-read", "dotnet", "WorkspaceRead"),
            new TaskExecutionGrantV3(["WorkspaceRead"], AllowShellInterpreter: false, HumanConfirmed: false, AllowedExecutables: ["dotnet"]));

        Assert.Equal(
            "E2D-TASK-V3-EXECUTION-EXECUTABLE-DENIED",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskExecutionPolicyV3.Authorize(
                CreateProcessCommand("command-unknown", "unknown-tool", "WorkspaceRead"),
                new TaskExecutionGrantV3(["WorkspaceRead"], AllowShellInterpreter: false, HumanConfirmed: false))).Code);
    }

    [Fact]
    public void ConversationIsAppendOnlyAndAgentContextRequiresRebaseAfterNewMessage()
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        AddMessage(previous, "message-1", 1, "Первое сообщение");
        var context = AgentContextBuilderV3.Build(previous, recentMessageCount: 1);
        Assert.Equal(1, context["lastMessageSequence"]!.GetValue<long>());
        Assert.NotNull(context["definition"]!["executionContract"]);
        Assert.Single(context["recentMessages"]!.AsArray());
        Assert.NotNull(context["activity"]);
        Assert.NotNull(context["attachments"]);
        var checkpoint = AgentContextBuilderV3.BuildCheckpoint(previous, "run-1", "worker-1", TaskBoardV3Role.Worker);

        var rewritten = previous.DeepClone().AsObject();
        rewritten["revision"] = 2;
        rewritten["conversation"]!["messages"]![0]!["content"]![0]!["markdown"] = "Переписано";
        Assert.Equal(
            "E2D-TASK-V3-TRANSITION-CONVERSATION-PREFIX",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                rewritten,
                new TaskBoardV3MutationContext("worker-1", TaskBoardV3Capability.EditTask))).Code);

        var advanced = previous.DeepClone().AsObject();
        advanced["revision"] = 2;
        AddMessage(advanced, "message-2", 2, "Новое сообщение");
        Assert.True(AgentContextBuilderV3.RequiresRebase(checkpoint, advanced));
        Assert.False(AgentContextBuilderV3.RequiresRebase(checkpoint, previous));
    }

    [Fact]
    public void CompatibilityUpgradePreservesCanonicalCheckpointActivityWatermark()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        var checkpoint = AgentContextBuilderV3.BuildCheckpoint(task, "run-1", "worker-1", TaskBoardV3Role.Worker);
        task["conversation"]!["contextCheckpoints"]!.AsArray().Add(checkpoint);
        task["activity"]!.AsArray().Add(CreateStatusActivity("activity-1", 1));
        task["lastActivitySequence"] = 1;
        var expectedCheckpoint = checkpoint.ToJsonString();

        TaskBoardV3Compatibility.UpgradeTask(task);

        Assert.Equal(
            expectedCheckpoint,
            Assert.Single(task["conversation"]!["contextCheckpoints"]!.AsArray())!.ToJsonString());
        var firstUpgrade = task.ToJsonString();
        TaskBoardV3Compatibility.UpgradeTask(task);
        Assert.Equal(firstUpgrade, task.ToJsonString());
    }

    [Fact]
    public void CompatibilityUpgradeAddsOnlyMissingLegacyCheckpointActivityWatermark()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        var checkpoint = AgentContextBuilderV3.BuildCheckpoint(task, "run-1", "worker-1", TaskBoardV3Role.Worker);
        checkpoint.Remove("lastActivitySequence");
        task["conversation"]!["contextCheckpoints"]!.AsArray().Add(checkpoint);
        task["activity"]!.AsArray().Add(CreateStatusActivity("activity-1", 1));
        task["lastActivitySequence"] = 1;

        TaskBoardV3Compatibility.UpgradeTask(task);

        Assert.Equal(1, checkpoint["lastActivitySequence"]!.GetValue<long>());
        var firstUpgrade = task.ToJsonString();
        TaskBoardV3Compatibility.UpgradeTask(task);
        Assert.Equal(firstUpgrade, task.ToJsonString());
    }

    [Fact]
    public void ContextCheckpointPrefixRejectsTampering()
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        previous["conversation"]!["contextCheckpoints"]!.AsArray().Add(
            AgentContextBuilderV3.BuildCheckpoint(previous, "run-1", "worker-1", TaskBoardV3Role.Worker));
        previous["conversation"]!["contextCheckpoints"]!.AsArray().Add(
            AgentContextBuilderV3.BuildCheckpoint(previous, "run-2", "worker-1", TaskBoardV3Role.Worker));

        var changed = previous.DeepClone().AsObject();
        changed["conversation"]!["contextCheckpoints"]![0]!["lastActivitySequence"] = 1;
        var removed = previous.DeepClone().AsObject();
        removed["conversation"]!["contextCheckpoints"]!.AsArray().RemoveAt(1);
        var reordered = previous.DeepClone().AsObject();
        var reorderedCheckpoints = reordered["conversation"]!["contextCheckpoints"]!.AsArray();
        var first = reorderedCheckpoints[0]!.DeepClone();
        reorderedCheckpoints[0] = reorderedCheckpoints[1]!.DeepClone();
        reorderedCheckpoints[1] = first;

        foreach (var tampered in new[] { changed, removed, reordered })
        {
            tampered["revision"] = 2;
            Assert.Equal(
                "E2D-TASK-V3-TRANSITION-CONTEXT-CHECKPOINT-PREFIX",
                Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3TransitionValidator.ValidateTask(
                    previous,
                    tampered,
                    new TaskBoardV3MutationContext("worker-1", TaskBoardV3Capability.EditTask))).Code);
        }
    }

    [Fact]
    public void ConversationRejectsUntrustedAuthorAndInvalidAgentRunOwnership()
    {
        var agentWithoutRun = CreateTask(TaskUidA, "T-0001");
        AddMessage(agentWithoutRun, "message-1", 1, "Сообщение агента");
        agentWithoutRun["conversation"]!["messages"]![0]!["agentRunId"] = null;
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-CONVERSATION-AUTHOR",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(agentWithoutRun)).Code);

        var humanWithAgentRun = CreateTask(TaskUidA, "T-0001");
        AddMessage(humanWithAgentRun, "message-1", 1, "Сообщение человека");
        humanWithAgentRun["conversation"]!["messages"]![0]!["author"]!["actorId"] = "human-1";
        humanWithAgentRun["conversation"]!["messages"]![0]!["author"]!["actorKind"] = "Human";
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-CONVERSATION-AUTHOR",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(humanWithAgentRun)).Code);

        var systemOwner = CreateTask(TaskUidA, "T-0001");
        AddMessage(systemOwner, "message-1", 1, "Системное сообщение");
        systemOwner["conversation"]!["messages"]![0]!["author"]!["actorKind"] = "System";
        systemOwner["conversation"]!["messages"]![0]!["author"]!["role"] = "Owner";
        systemOwner["conversation"]!["messages"]![0]!["agentRunId"] = null;
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-CONVERSATION-AUTHOR",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(systemOwner)).Code);

        var previous = CreateTask(TaskUidA, "T-0001");
        var forged = previous.DeepClone().AsObject();
        forged["revision"] = 2;
        AddMessage(forged, "message-1", 1, "Поддельный автор");
        Assert.Equal(
            "E2D-TASK-V3-TRANSITION-CONVERSATION-AUTHOR",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                forged,
                new TaskBoardV3MutationContext("different-agent", TaskBoardV3Capability.EditTask, TaskBoardV3Role.Worker, "Agent"))).Code);
    }

    [Fact]
    public void TransitionPreservesOriginalAttachmentsAndRequiresTaskPatchEvidence()
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        previous["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "attachment-1", 1));

        var removedOriginal = previous.DeepClone().AsObject();
        removedOriginal["revision"] = 2;
        removedOriginal["attachments"] = new JsonArray();
        Assert.Equal(
            "E2D-TASK-V3-TRANSITION-ATTACHMENT-PREFIX",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                removedOriginal,
                new TaskBoardV3MutationContext("worker-1", TaskBoardV3Capability.EditTask))).Code);

        var patchedWithoutEvidence = previous.DeepClone().AsObject();
        patchedWithoutEvidence["revision"] = 2;
        patchedWithoutEvidence["description"] = "Новая постановка";
        Assert.Equal(
            "E2D-TASK-V3-TRANSITION-TASK-PATCH-REQUIRED",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                patchedWithoutEvidence,
                new TaskBoardV3MutationContext("worker-1", TaskBoardV3Capability.EditTask))).Code);
    }

    [Fact]
    public void NewConversationContentCannotBeStoredInActivity()
    {
        foreach (var kind in new[] { "Comment", "AgentSummary" })
        {
            var task = CreateTask(TaskUidA, "T-0001");
            var entry = CreateCommentActivity("activity-legacy-chat");
            entry["kind"] = kind;
            task["activity"]!.AsArray().Add(entry);

            Assert.Equal(
                "E2D-TASK-V3-SCHEMA-LEGACY-CONVERSATION-ACTIVITY",
                Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(task)).Code);
        }
    }

    [Fact]
    public void AgentContextDigestIsCanonicalDerivedAndManifestBound()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        AddMessage(task, "message-1", 1, "Контекст");
        var withoutSnapshot = AgentContextBuilderV3.ComputeDigest(task);
        task["contextSnapshot"] = new JsonObject
        {
            ["derived"] = "cache that must not affect source context"
        };
        Assert.Equal(withoutSnapshot, AgentContextBuilderV3.ComputeDigest(task));

        var reordered = new JsonObject();
        foreach (var property in task.Reverse())
        {
            reordered[property.Key] = property.Value?.DeepClone();
        }

        Assert.Equal(AgentContextBuilderV3.ComputeDigest(task), AgentContextBuilderV3.ComputeDigest(reordered));

        task["contextSnapshot"] = null;
        var context = AgentContextBuilderV3.Build(task);
        var manifest = Assert.IsType<JsonObject>(context["contextManifest"]);
        Assert.Equal("AgentContextBuilderV3", manifest["builderProfile"]!.GetValue<string>());
        Assert.Equal("sha256-jcs-rfc8785-v1", manifest["hashProfile"]!.GetValue<string>());
        Assert.Equal(1, manifest["throughTaskRevision"]!.GetValue<long>());
        Assert.Equal(1, manifest["throughMessageSequence"]!.GetValue<long>());
        Assert.Equal(0, manifest["throughActivitySequence"]!.GetValue<long>());
        Assert.Matches("^[a-f0-9]{64}$", manifest["taskCoreDigest"]!.GetValue<string>());
        Assert.Matches("^[a-f0-9]{64}$", manifest["attachmentManifestDigest"]!.GetValue<string>());
    }

    [Fact]
    public void AttachmentDerivativeLifecycleIsExplicitForEverySupportedKind()
    {
        using var schema = ReadSchema("task-file-v3.schema.json");
        var attachment = schema.RootElement.GetProperty("$defs").GetProperty("attachment");
        var derivatives = attachment.GetProperty("properties").GetProperty("derivatives");
        Assert.True(derivatives.TryGetProperty("minItems", out var minItems));
        Assert.Equal(3, minItems.GetInt32());
        Assert.Equal(3, derivatives.GetProperty("maxItems").GetInt32());

        var derivative = schema.RootElement.GetProperty("$defs").GetProperty("attachmentDerivative");
        var required = derivative.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("status", required);
        Assert.Contains("failureReason", required);
        Assert.True(derivative.GetProperty("properties").GetProperty("status").GetProperty("enum")
            .EnumerateArray().Select(item => item.GetString()).SequenceEqual(
                ["Pending", "Ready", "Failed", "Unsupported", "NotRequired"], StringComparer.Ordinal));
    }

    [Fact]
    public void CompatibilityUpgradeCreatesIndependentDerivativeTimestampNodes()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        var attachment = CreateAttachment(TaskUidA, "attachment-1", 1);
        attachment.Remove("derivatives");
        task["attachments"]!.AsArray().Add(attachment);

        var upgraded = TaskBoardV3Compatibility.UpgradeTask(task);

        Assert.Equal(3, upgraded["attachments"]![0]!["derivatives"]!.AsArray().Count);
        TaskBoardV3SchemaValidator.ValidateTask(upgraded);
    }

    [Fact]
    public void CompatibilityUpgradeReclassifiesLegacyFileLinkContainingColon()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        task["links"] = new JsonArray
        {
            new JsonObject
            {
                ["linkId"] = "link-1",
                ["kind"] = "File",
                ["value"] = "string.Concat(\"C\", \":/...\")"
            }
        };

        var upgraded = TaskBoardV3Compatibility.UpgradeTask(task);

        Assert.Equal("Resource", upgraded["links"]![0]!["kind"]!.GetValue<string>());
        TaskBoardV3SchemaValidator.ValidateTask(upgraded);
    }

    [Fact]
    public void CompatibilityUpgradeDoesNotInventAuditProofForAcceptedLegacyContract()
    {
        var task = CreateTask(TaskUidA, "T-0001", status: "Done");
        task["executionContract"]!["externalAudit"] = "primary external audit required";

        var upgraded = TaskBoardV3Compatibility.UpgradeTask(task);

        Assert.Equal("None", upgraded["executionContract"]!["externalAudit"]!["mode"]!.GetValue<string>());
        var legacyAudit = Assert.Single(upgraded["activity"]!.AsArray().OfType<JsonObject>(), entry =>
            entry["kind"]!.GetValue<string>() == "Legacy" &&
            entry["payload"]!["sourceKind"]!.GetValue<string>() == "ExternalAuditContract");
        Assert.Equal("primary external audit required", legacyAudit["payload"]!["text"]!.GetValue<string>());
        TaskBoardV3SchemaValidator.ValidateTask(upgraded);
        TaskBoardV3SemanticValidator.Validate(
            FindRepositoryRoot(),
            CreateBoard(),
            [],
            [upgraded],
            validateAttachmentBlobs: false);
    }

    [Fact]
    public void CompatibilityUpgradePreservesAcceptedDraftWithoutHistoricalCriteria()
    {
        var draft = CreateTask(TaskUidA, "T-0001", status: "Done");
        draft["acceptanceCriteria"] = new JsonArray();
        draft.Remove("lastActivitySequence");
        draft.Remove("workspaceChanges");

        var upgraded = TaskBoardV3Compatibility.UpgradeTask(draft);

        var criterion = Assert.Single(upgraded["acceptanceCriteria"]!.AsArray());
        Assert.Equal("legacy-accepted-result", criterion!["criterionId"]!.GetValue<string>());
        Assert.Equal("Passed", criterion["state"]!.GetValue<string>());
        Assert.Single(criterion["evidence"]!.AsArray());
        TaskBoardV3SchemaValidator.ValidateTask(upgraded);

        var canonical = CreateTask(TaskUidA, "T-0001", status: "Done");
        canonical["acceptanceCriteria"] = new JsonArray();
        Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(
            TaskBoardV3Compatibility.UpgradeTask(canonical)));
    }

    [Fact]
    public void PrimaryControlAcceptanceRequiresIndependentAuditRunChainAndReports()
    {
        var task = CreateTask(TaskUidA, "T-0001", status: "Done");
        task["executionContract"]!["externalAudit"] = new JsonObject
        {
            ["mode"] = "PrimaryControl",
            ["independence"] = "CleanControlContext",
            ["instructions"] = "Проверить независимо",
            ["requiredVerdicts"] = new JsonArray("Primary", "Control")
        };

        Assert.Equal(
            "E2D-TASK-V3-SEMANTIC-AUDIT-RUNS",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SemanticValidator.Validate(
                FindRepositoryRoot(),
                CreateBoard(),
                [],
                [task],
                validateAttachmentBlobs: false)).Code);
    }

    [Fact]
    public void PrimaryControlAcceptanceUsesIndependentChainedAuditRunsAndFinalReference()
    {
        var task = CreateTask(TaskUidA, "T-0001", status: "Done");
        task["updatedAt"] = "2026-07-13T00:01:00+03:00";
        task["acceptedBy"] = "owner-1";
        task["executionContract"]!["externalAudit"] = new JsonObject
        {
            ["mode"] = "PrimaryControl",
            ["independence"] = "CleanControlContext",
            ["instructions"] = "Проверить независимо",
            ["requiredVerdicts"] = new JsonArray("Primary", "Control")
        };
        task["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "report-primary", 1));
        task["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "report-control", 1));
        task["revision"] = 4;
        task["auditRuns"]!.AsArray().Add(CreateAuditRun(
            "audit-primary-1", "Primary", "auditor-1", 1, new string('b', 64), "report-primary"));
        task["auditRuns"]!.AsArray().Add(CreateAuditRun(
            "audit-control-1", "Control", "auditor-2", 2, new string('c', 64), "report-control", "audit-primary-1"));
        var acceptance = task["activity"]![0]!.AsObject();
        acceptance["actorId"] = "owner-1";
        acceptance["actorKind"] = "Human";
        acceptance["payload"]!["authorityActorId"] = "owner-1";
        acceptance["payload"]!["authorityRole"] = "Owner";
        acceptance["payload"]!["auditRunId"] = "audit-control-1";

        TaskBoardV3SemanticValidator.Validate(
            FindRepositoryRoot(),
            CreateBoard(),
            [],
            [task],
            validateAttachmentBlobs: false);
    }

    [Fact]
    public void AuditRunAppendIsBoundToTheAuditedRevisionAndCanonicalContext()
    {
        var previous = CreateTask(TaskUidA, "T-0001", status: "Review");
        previous["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "report-1", 1));
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next["updatedAt"] = "2026-07-13T00:01:00+03:00";
        next["auditRuns"]!.AsArray().Add(CreateAuditRun(
            "audit-primary-1",
            "Primary",
            "auditor-1",
            taskRevision: 1,
            AgentContextBuilderV3.ComputeDigest(previous),
            "report-1"));
        var trustedAuditor = new TaskBoardV3MutationContext(
            "auditor-1",
            TaskBoardV3Capability.EditTask | TaskBoardV3Capability.AcceptanceDecision,
            TaskBoardV3Role.Auditor,
            "Agent");

        TaskBoardV3TransitionValidator.ValidateTask(previous, next, trustedAuditor);

        var forged = next.DeepClone().AsObject();
        forged["auditRuns"]![0]!["contextDigest"] = new string('f', 64);
        var contextError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(previous, forged, trustedAuditor));
        Assert.Equal("E2D-TASK-V3-TRANSITION-AUDIT-RUN-CONTEXT", contextError.Code);

        forged = next.DeepClone().AsObject();
        forged["auditRuns"]![0]!["taskRevision"] = 2;
        var revisionError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(previous, forged, trustedAuditor));
        Assert.Equal("E2D-TASK-V3-TRANSITION-AUDIT-RUN-CONTEXT", revisionError.Code);
    }

    [Fact]
    public void ControlAuditCannotExistWithoutPriorPrimaryAndPackageDigestIsRecomputed()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        task["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "report-control", 1));
        var control = CreateAuditRun(
            "audit-control-1", "Control", "auditor-2", 1, new string('c', 64), "report-control", "missing-primary");
        task["revision"] = 2;
        task["updatedAt"] = "2026-07-13T00:01:00+03:00";
        task["auditRuns"]!.AsArray().Add(control);

        var chainError = Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SemanticValidator.Validate(
            FindRepositoryRoot(), CreateBoard((TaskUidA, null, "000000001000")), [task], [], validateAttachmentBlobs: false));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-AUDIT-RUNS", chainError.Code);

        control["packageDigest"] = new string('f', 64);
        var packageError = Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(task));
        Assert.Equal("E2D-TASK-V3-SCHEMA-AUDIT-PACKAGE", packageError.Code);
    }

    [Fact]
    public void WorkerCannotAppendTheirOwnAuditRun()
    {
        var previous = CreateTask(TaskUidA, "T-0001", status: "Review");
        previous["assignee"] = "auditor-1";
        previous["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "report-1", 1));
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next["updatedAt"] = "2026-07-13T00:01:00+03:00";
        next["auditRuns"]!.AsArray().Add(CreateAuditRun(
            "audit-primary-1", "Primary", "auditor-1", 1, AgentContextBuilderV3.ComputeDigest(previous), "report-1"));

        var error = Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            next,
            new TaskBoardV3MutationContext(
                "auditor-1",
                TaskBoardV3Capability.EditTask | TaskBoardV3Capability.AcceptanceDecision,
                TaskBoardV3Role.Auditor,
                "Agent")));

        Assert.Equal("E2D-TASK-V3-TRANSITION-AUDIT-RUN-SELF", error.Code);
    }

    [Fact]
    public void AttachmentNamesAndDerivedOwnershipAreFailClosed()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        task["attachments"]!.AsArray().Add(new JsonObject
        {
            ["attachmentId"] = "attachment-1",
            ["displayName"] = "CON.txt",
            ["relativePath"] = $".taskboard/attachments/{TaskUidA}/attachment-1/CON.txt",
            ["mediaType"] = "text/plain",
            ["byteLength"] = 1,
            ["sha256"] = new string('0', 64),
            ["addedAt"] = "2026-07-13T00:00:00+03:00",
            ["addedBy"] = "cli",
            ["derivatives"] = new JsonArray()
        });

        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-PATH",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(task)).Code);
    }

    [Fact]
    public void AttachmentPathsRejectUnsafeSegmentsBeforeFilesystemResolution()
    {
        var unsafePaths = new[]
        {
            "foo\n/../secret",
            $".taskboard/attachments/{TaskUidA}/attachment-1/bad:name/evidence.txt",
            $".taskboard/attachments/{TaskUidA}/attachment-1/CON/evidence.txt",
            $".taskboard/attachments/{TaskUidA}/attachment-1/.."
        };

        foreach (var unsafePath in unsafePaths)
        {
            var task = CreateTask(TaskUidA, "T-0001");
            var attachment = CreateAttachment(TaskUidA, "attachment-1", 1);
            attachment["relativePath"] = unsafePath;
            task["attachments"]!.AsArray().Add(attachment);

            var error = Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(task));
            Assert.Contains(error.Code, new[] { "E2D-TASK-V3-SCHEMA-PATH", "E2D-TASK-V3-SCHEMA-SAFE-NAME" });
        }
    }

    [Fact]
    public void EveryProjectPathRejectsWindowsDevicesAdsInvalidCharactersAndEmptySegments()
    {
        foreach (var unsafePath in new[] { "con.txt", "Nul.txt", "foo//bar", "foo:stream", "src/bad?.cs", "src/bad*.cs", "src/trailing. ", "foo\n/../secret" })
        {
            var task = CreateTask(TaskUidA, "T-0001");
            task["links"]!.AsArray().Add(new JsonObject
            {
                ["linkId"] = "link-1",
                ["kind"] = "File",
                ["value"] = unsafePath
            });

            Assert.Equal(
                "E2D-TASK-V3-SCHEMA-PATH",
                Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(task)).Code);
        }
    }

    [Fact]
    public void V3AttachmentPathIsUidOwnedAndSizeLimitsComeOnlyFromBoardPolicy()
    {
        using var taskSchema = ReadSchema("task-file-v3.schema.json");
        using var boardSchema = ReadSchema("task-board-v3.schema.json");

        var attachmentItem = taskSchema.RootElement.GetProperty("properties").GetProperty("attachments")
            .GetProperty("items");
        var attachment = ResolveLocalReference(taskSchema, attachmentItem).GetProperty("properties");
        var relativePath = attachment.GetProperty("relativePath");
        Assert.Contains("taskUid", relativePath.GetProperty("description").GetString(), StringComparison.Ordinal);
        Assert.False(attachment.GetProperty("byteLength").TryGetProperty("maximum", out _));

        var policy = boardSchema.RootElement.GetProperty("properties").GetProperty("attachmentPolicy")
            .GetProperty("properties");
        Assert.True(policy.TryGetProperty("perFileByteLimit", out _));
        Assert.True(policy.TryGetProperty("boardByteLimit", out _));

        var placement = boardSchema.RootElement.GetProperty("properties").GetProperty("placements")
            .GetProperty("items").GetProperty("properties");
        Assert.True(placement.TryGetProperty("taskUid", out _));
        Assert.False(placement.TryGetProperty("taskId", out _));
    }

    [Fact]
    public void RuntimeSchemaValidatorRejectsUnknownBlankOversizedAndImpossibleLifecycleFields()
    {
        var valid = CreateTask(TaskUidA, "T-0001");
        TaskBoardV3SchemaValidator.ValidateTask(valid);
        TaskBoardV3SchemaValidator.ValidateBoard(CreateBoard((TaskUidA, null, "000000001000")));

        var unknown = valid.DeepClone().AsObject();
        unknown["subtasks"] = new JsonArray();
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-UNKNOWN-PROPERTY",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(unknown)).Code);

        var blank = valid.DeepClone().AsObject();
        blank["title"] = "   ";
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-STRING",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(blank)).Code);

        var oversized = valid.DeepClone().AsObject();
        oversized["title"] = new string('x', 513);
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-LIMIT",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(oversized)).Code);

        var impossible = valid.DeepClone().AsObject();
        impossible["status"] = "Done";
        impossible["acceptanceState"] = "NotSubmitted";
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-LIFECYCLE",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(impossible)).Code);

        var incompleteCommand = valid.DeepClone().AsObject();
        incompleteCommand["executionContract"]!["commands"]!.AsArray().Add(new JsonObject
        {
            ["commandId"] = "command-broken",
            ["kind"] = "Process",
            ["executable"] = "dotnet"
        });
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-REQUIRED",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(incompleteCommand)).Code);

        var untypedActivity = valid.DeepClone().AsObject();
        untypedActivity["activity"]!.AsArray().Add(new JsonObject
        {
            ["activityEntryId"] = "activity-broken",
            ["sequence"] = 1,
            ["actorId"] = "agent",
            ["actorKind"] = "Agent",
            ["createdAt"] = "2026-07-13T00:00:00+03:00",
            ["kind"] = "Comment",
            ["payload"] = "неструктурированный текст"
        });
        untypedActivity["lastActivitySequence"] = 1;
        Assert.Equal(
            "E2D-TASK-V3-SCHEMA-LEGACY-CONVERSATION-ACTIVITY",
            Assert.Throws<TaskBoardV3ValidationException>(() => TaskBoardV3SchemaValidator.ValidateTask(untypedActivity)).Code);
    }

    [Fact]
    public void SemanticValidatorAcceptsAConsistentWholeBoardSnapshot()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        var board = CreateBoard((TaskUidA, null, "000000001000"));

        TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [task], []);
    }

    [Theory]
    [InlineData("taskUid", "E2D-TASK-V3-SEMANTIC-DUPLICATE-TASK-UID")]
    [InlineData("taskId", "E2D-TASK-V3-SEMANTIC-DUPLICATE-TASK-ID")]
    public void SemanticValidatorRejectsDuplicateTaskIdentityAcrossActiveAndCompleted(string propertyName, string expectedCode)
    {
        var active = CreateTask(TaskUidA, "T-0001");
        var completed = CreateTask(TaskUidB, "T-0002", status: "Cancelled");
        completed[propertyName] = active[propertyName]!.GetValue<string>();
        var board = CreateBoard((TaskUidA, null, "000000001000"));

        var error = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [active], [completed]));

        Assert.Equal(expectedCode, error.Code);
    }

    [Fact]
    public void SemanticValidatorRejectsForeignBoardAndDanglingUidReferences()
    {
        var foreign = CreateTask(TaskUidA, "T-0001", boardId: "other");
        var board = CreateBoard((TaskUidA, null, "000000001000"));
        var foreignError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [foreign], []));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-FOREIGN-BOARD", foreignError.Code);

        var dangling = CreateTask(TaskUidA, "T-0001");
        AddRelation(dangling, "relation-1", "DependsOn", TaskUidB);
        var danglingError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [dangling], []));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-DANGLING-RELATION", danglingError.Code);
    }

    [Fact]
    public void SemanticValidatorRejectsParentAndDependencyCycles()
    {
        var first = CreateTask(TaskUidA, "T-0001");
        var second = CreateTask(TaskUidB, "T-0002");
        first["parentTaskUid"] = TaskUidB;
        second["parentTaskUid"] = TaskUidA;
        var board = CreateBoard(
            (TaskUidA, null, "000000001000"),
            (TaskUidB, null, "000000002000"));

        var parentError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [first, second], []));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-PARENT-CYCLE", parentError.Code);

        first["parentTaskUid"] = null;
        second["parentTaskUid"] = null;
        AddRelation(first, "relation-1", "DependsOn", TaskUidB);
        AddRelation(second, "relation-2", "DependsOn", TaskUidA);
        var dependencyError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [first, second], []));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-DEPENDENCY-CYCLE", dependencyError.Code);
    }

    [Fact]
    public void SemanticValidatorRejectsDuplicateChildIdsAndSiblingRanks()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        var criteria = task["acceptanceCriteria"]!.AsArray();
        criteria.Add(criteria[0]!.DeepClone());
        var board = CreateBoard((TaskUidA, null, "000000001000"));

        var childError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [task], []));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-DUPLICATE-CRITERION-ID", childError.Code);

        task = CreateTask(TaskUidA, "T-0001");
        board = CreateBoard(
            (TaskUidA, null, "000000001000"),
            (TaskUidB, null, "000000001000"));
        var second = CreateTask(TaskUidB, "T-0002");
        var rankError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [task, second], []));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-DUPLICATE-PLACEMENT-RANK", rankError.Code);
    }

    [Fact]
    public void SemanticValidatorRejectsDoneWithoutPassedCriteriaAndHumanAcceptance()
    {
        var task = CreateTask(TaskUidA, "T-0001", status: "Done");
        task["acceptanceCriteria"]![0]!["state"] = "Open";
        task["activity"] = new JsonArray();
        task["lastActivitySequence"] = 0;
        var board = CreateBoard();

        var error = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [], [task]));

        Assert.Equal("E2D-TASK-V3-SCHEMA-LIFECYCLE", error.Code);
    }

    [Fact]
    public async Task TaskBoardV3ConcurrentWriterWaitsForLockAndCommitsAfterRelease()
    {
        var projectRoot = CreateNativeV3Project();
        try
        {
            using var heldLock = new TaskBoardDiskStore(projectRoot).AcquireWriteLock();
            var pending = Task.Run(() => new TaskBoardV3DiskStore(
                projectRoot,
                new TaskBoardWriteOptions(
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.FromMilliseconds(40))).Create(
                        "Конкурентная задача",
                        string.Empty,
                        "P1",
                        deadline: null,
                        structuredInput: null,
                        expectedBoardRevision: null,
                        actorId: "agent-1",
                        now: new DateTimeOffset(2026, 7, 14, 3, 0, 0, TimeSpan.FromHours(3)),
                        dryRun: false));

            await Task.Delay(100);
            Assert.False(pending.IsCompleted);
            heldLock.Dispose();

            var result = await pending.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal("T-0001", result.Task["taskId"]!.GetValue<string>());
            Assert.Single(new TaskBoardV3DiskStore(projectRoot).Verify().ActiveTasks);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void TaskBoardV3ConcurrentWriterReportsRetryableTimeoutAndCancellation()
    {
        var projectRoot = CreateNativeV3Project();
        try
        {
            using var heldLock = new TaskBoardDiskStore(projectRoot).AcquireWriteLock();
            var timeout = Assert.Throws<TaskBoardWriteException>(() => new TaskBoardV3DiskStore(
                projectRoot,
                new TaskBoardWriteOptions(
                    TimeSpan.FromMilliseconds(40),
                    TimeSpan.FromMilliseconds(5),
                    TimeSpan.FromMilliseconds(10))));
            Assert.Equal("E2D-TASK-0004", timeout.Code);
            Assert.True(timeout.IsRetryable);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelled = Assert.Throws<TaskBoardWriteException>(() => new TaskBoardV3DiskStore(
                projectRoot,
                new TaskBoardWriteOptions(
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromMilliseconds(5),
                    TimeSpan.FromMilliseconds(10),
                    cancellation.Token)));
            Assert.Equal("E2D-TASK-0005", cancelled.Code);
            Assert.True(cancelled.IsRetryable);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public async Task TaskBoardV3ConcurrentWriterAllocatesUniqueCreateIdsWithoutBoardRevisionGuess()
    {
        var projectRoot = CreateNativeV3Project();
        try
        {
            var writers = Enumerable.Range(1, 12).Select(index => Task.Run(() =>
                new TaskBoardV3DiskStore(projectRoot).Create(
                    $"Задача {index}",
                    string.Empty,
                    "P1",
                    deadline: null,
                    structuredInput: null,
                    expectedBoardRevision: null,
                    actorId: $"agent-{index}",
                    now: new DateTimeOffset(2026, 7, 14, 3, index, 0, TimeSpan.FromHours(3)),
                    dryRun: false))).ToArray();

            var results = await Task.WhenAll(writers).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Equal(12, results.Select(result => result.Task["taskId"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(12, results.Select(result => result.Task["taskUid"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count());
            var snapshot = new TaskBoardV3DiskStore(projectRoot).Verify();
            Assert.Equal(12, snapshot.ActiveTasks.Count);
            Assert.Equal(13, snapshot.Board["revision"]!.GetValue<long>());
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public async Task TaskBoardV3ConcurrentWriterSerializesIndependentCliProcesses()
    {
        var projectRoot = CreateNativeV3Project();
        try
        {
            async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(int index)
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add(typeof(Electron2DCommandLine).Assembly.Location);
                foreach (var argument in new[]
                {
                    "tasks", "create",
                    "--project", projectRoot,
                    "--title", $"Процесс {index}",
                    "--operation-id", $"process-create-{index}",
                    "--format", "json"
                })
                {
                    startInfo.ArgumentList.Add(argument);
                }

                using var process = System.Diagnostics.Process.Start(startInfo) ??
                    throw new InvalidOperationException("e2d process did not start.");
                var standardOutput = process.StandardOutput.ReadToEndAsync();
                var standardError = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await process.WaitForExitAsync(timeout.Token);
                return (process.ExitCode, await standardOutput, await standardError);
            }

            var results = await Task.WhenAll(Enumerable.Range(1, 10).Select(RunProcessAsync));
            Assert.All(results, result => Assert.True(result.ExitCode == 0, result.Output + result.Error));
            var snapshot = new TaskBoardV3DiskStore(projectRoot).Verify();
            Assert.Equal(10, snapshot.ActiveTasks.Count);
            Assert.Equal(10, snapshot.ActiveTasks.Select(task => task["taskId"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(11, snapshot.Board["revision"]!.GetValue<long>());
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public async Task TaskBoardV3ConcurrentWriterConversationAppendKeepsCasAndContinuousSequence()
    {
        var projectRoot = CreateNativeV3Project();
        try
        {
            var created = new TaskBoardV3DiskStore(projectRoot).Create(
                "Диалог",
                string.Empty,
                "P1",
                deadline: null,
                structuredInput: null,
                expectedBoardRevision: null,
                actorId: "agent-1",
                now: new DateTimeOffset(2026, 7, 14, 3, 0, 0, TimeSpan.FromHours(3)),
                dryRun: false);
            var taskId = created.Task["taskId"]!.GetValue<string>();
            var attempts = new[]
            {
                Task.Run(() => new TaskBoardV3DiskStore(projectRoot).AddComment(taskId, "Первое", 1, "agent-1", "Agent", new DateTimeOffset(2026, 7, 14, 3, 1, 0, TimeSpan.FromHours(3)), false)),
                Task.Run(() => new TaskBoardV3DiskStore(projectRoot).AddComment(taskId, "Второе", 1, "agent-2", "Agent", new DateTimeOffset(2026, 7, 14, 3, 1, 1, TimeSpan.FromHours(3)), false))
            };

            await Task.WhenAll(attempts.Select(async attempt =>
            {
                try
                {
                    await attempt;
                }
                catch (TaskBoardWriteException exception) when (exception.Code == "E2D-TASK-0006")
                {
                }
            }));

            var afterRace = new TaskBoardV3DiskStore(projectRoot).LoadTask(taskId);
            Assert.Equal(2, afterRace["revision"]!.GetValue<long>());
            var conflict = Assert.Single(attempts, attempt => attempt.IsFaulted).Exception!.InnerExceptions.Single() as TaskBoardWriteException;
            Assert.NotNull(conflict);
            Assert.Equal(2, conflict!.ActualTaskRevision);
            Assert.False(conflict.IsRetryable);

            new TaskBoardV3DiskStore(projectRoot).AddComment(
                taskId,
                "Повтор проигравшей реплики",
                conflict.ActualTaskRevision!.Value,
                "agent-2",
                "Agent",
                new DateTimeOffset(2026, 7, 14, 3, 2, 0, TimeSpan.FromHours(3)),
                false);
            var final = new TaskBoardV3DiskStore(projectRoot).Verify().ActiveTasks.Single();
            var conversation = final["conversation"]!.AsObject();
            Assert.Equal(2, conversation["lastMessageSequence"]!.GetValue<int>());
            Assert.Equal(new[] { 1, 2 }, conversation["messages"]!.AsArray().Select(message => message!["sequence"]!.GetValue<int>()));
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void SemanticValidatorVerifiesAttachmentOwnershipHashAndDynamicLimits()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-TaskBoardV3-" + Guid.NewGuid().ToString("N"));
        var relativePath = $".taskboard/attachments/{TaskUidA}/attachment-1/evidence.txt";
        var absolutePath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        File.WriteAllText(absolutePath, "evidence");
        try
        {
            var task = CreateTask(TaskUidA, "T-0001");
            var attachment = CreateAttachment(TaskUidA, "attachment-1", new FileInfo(absolutePath).Length);
            attachment["relativePath"] = relativePath;
            task["attachments"]!.AsArray().Add(attachment);
            var board = CreateBoard((TaskUidA, null, "000000001000"));

            var hashError = Assert.Throws<TaskBoardV3ValidationException>(() =>
                TaskBoardV3SemanticValidator.Validate(projectRoot, board, [task], []));
            Assert.Equal("E2D-TASK-V3-SEMANTIC-ATTACHMENT-HASH", hashError.Code);

            var actualHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(absolutePath))).ToLowerInvariant();
            task["attachments"]![0]!["sha256"] = actualHash;
            foreach (var derivative in task["attachments"]![0]!["derivatives"]!.AsArray())
            {
                derivative!["sourceSha256"] = actualHash;
            }
            board["attachmentPolicy"]!["perFileByteLimit"] = 1;
            var limitError = Assert.Throws<TaskBoardV3ValidationException>(() =>
                TaskBoardV3SemanticValidator.Validate(projectRoot, board, [task], []));
            Assert.Equal("E2D-TASK-V3-SEMANTIC-ATTACHMENT-FILE-LIMIT", limitError.Code);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void SemanticValidatorCountsOriginalsAndDerivativesAgainstTaskAndBoardQuotas()
    {
        var task = CreateTask(TaskUidA, "T-0001");
        task["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "attachment-1", 6, derivativeLength: 6));
        var board = CreateBoard((TaskUidA, null, "000000001000"));
        board["attachmentPolicy"]!["perFileByteLimit"] = 10;
        board["attachmentPolicy"]!["perTaskByteLimit"] = 10;
        board["attachmentPolicy"]!["boardByteLimit"] = 100;

        var taskLimitError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [task], [], validateAttachmentBlobs: false));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-ATTACHMENT-TASK-LIMIT", taskLimitError.Code);

        task = CreateTask(TaskUidA, "T-0001");
        task["attachments"]!.AsArray().Add(CreateAttachment(TaskUidA, "attachment-1", 6));
        var second = CreateTask(TaskUidB, "T-0002");
        second["attachments"]!.AsArray().Add(CreateAttachment(TaskUidB, "attachment-2", 6));
        board = CreateBoard(
            (TaskUidA, null, "000000001000"),
            (TaskUidB, null, "000000002000"));
        board["attachmentPolicy"]!["perFileByteLimit"] = 10;
        board["attachmentPolicy"]!["perTaskByteLimit"] = 10;
        board["attachmentPolicy"]!["boardByteLimit"] = 10;

        var boardLimitError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [task, second], [], validateAttachmentBlobs: false));
        Assert.Equal("E2D-TASK-V3-SEMANTIC-ATTACHMENT-BOARD-LIMIT", boardLimitError.Code);
    }

    [Fact]
    public void SemanticValidatorRejectsAttachmentThroughReparsePoint()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "Electron2D-TaskBoardV3-Reparse-" + Guid.NewGuid().ToString("N"));
        var projectRoot = Path.Combine(tempRoot, "project");
        var outsideRoot = Path.Combine(tempRoot, "outside");
        var linkPath = Path.Combine(projectRoot, ".taskboard", "attachments", TaskUidA, "attachment-1");
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        Directory.CreateDirectory(outsideRoot);
        var outsideFile = Path.Combine(outsideRoot, "evidence.txt");
        File.WriteAllText(outsideFile, "evidence");
        CreateDirectoryLink(linkPath, outsideRoot);
        try
        {
            var task = CreateTask(TaskUidA, "T-0001");
            var attachment = CreateAttachment(TaskUidA, "attachment-1", new FileInfo(outsideFile).Length);
            attachment["sha256"] = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(outsideFile))).ToLowerInvariant();
            task["attachments"]!.AsArray().Add(attachment);
            var board = CreateBoard((TaskUidA, null, "000000001000"));

            var error = Assert.Throws<TaskBoardV3ValidationException>(() =>
                TaskBoardV3SemanticValidator.Validate(projectRoot, board, [task], []));

            Assert.Equal("E2D-TASK-V3-SEMANTIC-ATTACHMENT-REPARSE", error.Code);
        }
        finally
        {
            if (Directory.Exists(linkPath))
            {
                Directory.Delete(linkPath);
            }

            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData(".taskboard/attachments/task-11111111111111111111111111111111/attachment-1/../escape.txt", "E2D-TASK-V3-SCHEMA-PATH")]
    [InlineData(".taskboard/attachments/task-22222222222222222222222222222222/attachment-1/evidence.txt", "E2D-TASK-V3-SEMANTIC-ATTACHMENT-OWNERSHIP")]
    [InlineData("../outside/evidence.txt", "E2D-TASK-V3-SCHEMA-PATH")]
    public void ValidatorRejectsAttachmentPathEscapeAndForeignUidOwnership(string relativePath, string expectedCode)
    {
        var task = CreateTask(TaskUidA, "T-0001");
        var attachment = CreateAttachment(TaskUidA, "attachment-1", 1);
        attachment["relativePath"] = relativePath;
        task["attachments"]!.AsArray().Add(attachment);
        var board = CreateBoard((TaskUidA, null, "000000001000"));

        var error = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3SemanticValidator.Validate(FindRepositoryRoot(), board, [task], [], validateAttachmentBlobs: false));

        Assert.Equal(expectedCode, error.Code);
    }

    [Fact]
    public void TransitionValidatorAcceptsOrdinaryRevisionPlusOneWithTaskPatchEvidence()
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next["description"] = "Уточнённое описание";
        next["updatedAt"] = "2026-07-13T00:01:00+03:00";
        TaskPatchV3.AppendIfRequired(
            previous,
            next,
            "agent-1",
            "Agent",
            DateTimeOffset.Parse("2026-07-13T00:01:00+03:00", CultureInfo.InvariantCulture));

        TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            next,
            new TaskBoardV3MutationContext("agent-1", TaskBoardV3Capability.EditTask));
    }

    [Fact]
    public void TransitionValidatorAllowsMetadataEditWhenTaskIsAlreadyDone()
    {
        var previous = CreateTask(TaskUidA, "T-0001", status: "Done");
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next["updatedAt"] = "2026-07-13T00:02:00+03:00";
        next["tagIds"]!.AsArray().Add("tag-0001");
        TaskPatchV3.AppendIfRequired(
            previous,
            next,
            "agent-1",
            "Agent",
            DateTimeOffset.Parse("2026-07-13T00:02:00+03:00", CultureInfo.InvariantCulture));

        TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            next,
            new TaskBoardV3MutationContext("agent-1", TaskBoardV3Capability.EditTask));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void TransitionValidatorRequiresExactRevisionIncrement(long nextRevision)
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        var next = previous.DeepClone().AsObject();
        next["revision"] = nextRevision;

        var error = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                next,
                new TaskBoardV3MutationContext("agent-1", TaskBoardV3Capability.EditTask)));

        Assert.Equal("E2D-TASK-V3-TRANSITION-REVISION", error.Code);
    }

    [Theory]
    [InlineData("taskUid", "task-33333333333333333333333333333333")]
    [InlineData("boardId", "other")]
    [InlineData("createdBy", "other-writer")]
    [InlineData("createdAt", "2026-07-13T00:02:00+03:00")]
    public void TransitionValidatorProtectsImmutableIdentityAndCreationAudit(string propertyName, string replacement)
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next[propertyName] = replacement;

        var error = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                next,
                new TaskBoardV3MutationContext("agent-1", TaskBoardV3Capability.EditTask)));

        Assert.Equal("E2D-TASK-V3-TRANSITION-IMMUTABLE-FIELD", error.Code);
    }

    [Fact]
    public void TransitionValidatorProtectsExistingActivityAsImmutablePrefix()
    {
        var previous = CreateTask(TaskUidA, "T-0001");
        previous["activity"]!.AsArray().Add(CreateCommentActivity("activity-comment-1"));
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next["activity"]![0]!["payload"]!["markdown"] = "Переписано";

        var error = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                next,
                new TaskBoardV3MutationContext("agent-1", TaskBoardV3Capability.EditTask)));

        Assert.Equal("E2D-TASK-V3-TRANSITION-ACTIVITY-PREFIX", error.Code);
    }

    [Fact]
    public void TransitionValidatorRequiresAcceptanceCapabilityForDoneAndTrustedCapabilityForReopen()
    {
        var previous = CreateTask(TaskUidA, "T-0001", status: "Review");
        previous["acceptanceState"] = "Submitted";
        previous["submittedAt"] = "2026-07-13T00:00:00+03:00";
        var done = CreateTask(TaskUidA, "T-0001", status: "Done");
        done["revision"] = 2;
        done["createdAt"] = previous["createdAt"]!.DeepClone();
        done["createdBy"] = previous["createdBy"]!.DeepClone();
        done["archivedAt"] = null;
        done["archivedBy"] = null;

        var acceptanceError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                done,
                new TaskBoardV3MutationContext(
                    "agent-1",
                    TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus)));
        Assert.Equal("E2D-TASK-V3-TRANSITION-ACCEPTANCE-CAPABILITY", acceptanceError.Code);

        TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            done,
            new TaskBoardV3MutationContext(
                "auditor-2",
                TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus | TaskBoardV3Capability.AcceptanceDecision,
                TaskBoardV3Role.Auditor,
                "Agent"));

        var reopened = CreateTask(TaskUidA, "T-0001");
        reopened["revision"] = 3;
        reopened["createdAt"] = done["createdAt"]!.DeepClone();
        reopened["createdBy"] = done["createdBy"]!.DeepClone();
        reopened["activity"] = done["activity"]!.DeepClone();
        reopened["lastActivitySequence"] = done["lastActivitySequence"]!.DeepClone();
        var reopenError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateTask(
                done,
                reopened,
                new TaskBoardV3MutationContext(
                    "agent-1",
                    TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus)));
        Assert.Equal("E2D-TASK-V3-TRANSITION-TRUSTED-REOPEN-REQUIRED", reopenError.Code);
    }

    [Fact]
    public void BoardTransitionRequiresCasRevisionAndMigrationCapability()
    {
        var previous = CreateBoard();
        var next = previous.DeepClone().AsObject();
        next["revision"] = 2;
        next["migration"] = new JsonObject
        {
            ["sourceVersion"] = 2,
            ["reportPath"] = ".taskboard/.migration/v2/report.json",
            ["reportSha256"] = new string('a', 64),
            ["sourceBoardRevision"] = 1,
            ["sourceDigests"] = new JsonObject(),
            ["migratedAt"] = "2026-07-13T00:01:00+03:00",
            ["finalized"] = false
        };

        var capabilityError = Assert.Throws<TaskBoardV3ValidationException>(() =>
            TaskBoardV3TransitionValidator.ValidateBoard(
                previous,
                next,
                new TaskBoardV3MutationContext("agent-1", TaskBoardV3Capability.EditBoard)));
        Assert.Equal("E2D-TASK-V3-TRANSITION-MIGRATION-CAPABILITY", capabilityError.Code);

        TaskBoardV3TransitionValidator.ValidateBoard(
            previous,
            next,
            new TaskBoardV3MutationContext("cli", TaskBoardV3Capability.EditBoard | TaskBoardV3Capability.Migrate));
    }

    [Fact]
    public void V2ToV3MigrationPlanIsDeterministicLosslessAndUidBased()
    {
        var projectRoot = CreateV2MigrationFixture();
        try
        {
            var instant = new DateTimeOffset(2026, 7, 13, 0, 30, 0, TimeSpan.FromHours(3));
            var first = TaskBoardV3Migration.BuildPlan(projectRoot, instant);
            var second = TaskBoardV3Migration.BuildPlan(projectRoot, instant);

            Assert.Equal(first.ReportSha256, second.ReportSha256);
            Assert.Equal(2, first.SourceVersion);
            Assert.Equal(7, first.SourceBoardRevision);
            Assert.Equal(3, first.Board["version"]!.GetValue<int>());
            Assert.Equal("main", first.Board["boardId"]!.GetValue<string>());
            Assert.Equal(TaskUidA, first.Board["placements"]![0]!["taskUid"]!.GetValue<string>());
            Assert.Equal("000000001000", first.Board["placements"]![0]!["rank"]!.GetValue<string>());

            var child = Assert.Single(first.ActiveTasks, task => task["taskId"]!.GetValue<string>() == "T-0002");
            Assert.Equal(TaskUidA, child["parentTaskUid"]!.GetValue<string>());
            Assert.False(child.ContainsKey("parentTaskId"));
            Assert.False(child.ContainsKey("subtasks"));
            Assert.False(child.ContainsKey("dependencies"));
            Assert.Equal(TaskUidA, child["relations"]![0]!["targetTaskUid"]!.GetValue<string>());

            var command = child["executionContract"]!["commands"]![0]!.AsObject();
            Assert.Equal("LegacyShell", command["kind"]!.GetValue<string>());
            Assert.Equal("dotnet test --filter Child", command["text"]!.GetValue<string>());
            Assert.Equal("ForbiddenUntilReviewed", command["execution"]!.GetValue<string>());

            TaskBoardV3SemanticValidator.Validate(
                projectRoot,
                first.Board,
                first.ActiveTasks,
                first.CompletedTasks);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void V2ToV3MigrationReportChangesWhenAnySourceChanges()
    {
        var projectRoot = CreateV2MigrationFixture();
        try
        {
            var instant = new DateTimeOffset(2026, 7, 13, 0, 30, 0, TimeSpan.FromHours(3));
            var before = TaskBoardV3Migration.BuildPlan(projectRoot, instant);
            var taskPath = Path.Combine(projectRoot, ".taskboard", "tasks", "T-0002.e2task");
            File.AppendAllText(taskPath, " ");
            var after = TaskBoardV3Migration.BuildPlan(projectRoot, instant);

            Assert.NotEqual(before.ReportSha256, after.ReportSha256);
            Assert.NotEqual(before.SourceDigests[".taskboard/tasks/T-0002.e2task"], after.SourceDigests[".taskboard/tasks/T-0002.e2task"]);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void V2ToV3ApplyRevalidatesUnderLockAndWritesOneRecoverableCutover()
    {
        var projectRoot = CreateV2MigrationFixture();
        try
        {
            var instant = new DateTimeOffset(2026, 7, 13, 0, 30, 0, TimeSpan.FromHours(3));
            var reviewed = TaskBoardV3Migration.BuildPlan(projectRoot, instant);
            var store = new TaskBoardDiskStore(projectRoot);

            var result = store.ApplyV3Migration(
                reviewed.ReportSha256,
                reviewed.SourceBoardRevision,
                instant,
                dryRun: false);

            Assert.NotEmpty(result.ChangedFiles);
            var board = JsonNode.Parse(File.ReadAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")))!.AsObject();
            var active = Directory.EnumerateFiles(Path.Combine(projectRoot, ".taskboard", "tasks"), "*.e2task")
                .Select(path => JsonNode.Parse(File.ReadAllText(path))!.AsObject()).ToArray();
            Assert.Equal(3, board["version"]!.GetValue<int>());
            Assert.All(active, task => Assert.Equal(3, task["version"]!.GetValue<int>()));
            Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", ".migration", "v2", "board.e2tasks")));
            TaskBoardV3SemanticValidator.Validate(projectRoot, board, active, []);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void V2ToV3ApplyRejectsStaleReviewedReportWithoutChangingCanonicalFiles()
    {
        var projectRoot = CreateV2MigrationFixture();
        try
        {
            var instant = new DateTimeOffset(2026, 7, 13, 0, 30, 0, TimeSpan.FromHours(3));
            var reviewed = TaskBoardV3Migration.BuildPlan(projectRoot, instant);
            var taskPath = Path.Combine(projectRoot, ".taskboard", "tasks", "T-0002.e2task");
            File.AppendAllText(taskPath, " ");
            var before = File.ReadAllBytes(taskPath);

            var error = Assert.Throws<InvalidOperationException>(() => new TaskBoardDiskStore(projectRoot).ApplyV3Migration(
                reviewed.ReportSha256,
                reviewed.SourceBoardRevision,
                instant,
                dryRun: false));

            Assert.Contains("report SHA-256", error.Message, StringComparison.Ordinal);
            Assert.Equal(before, File.ReadAllBytes(taskPath));
            Assert.False(Directory.Exists(Path.Combine(projectRoot, ".taskboard", ".migration")));
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    [Fact]
    public void V3FinalizeVerifiesSnapshotBeforeRemovingV2Sources()
    {
        var projectRoot = CreateV2MigrationFixture();
        try
        {
            var instant = new DateTimeOffset(2026, 7, 13, 0, 30, 0, TimeSpan.FromHours(3));
            var reviewed = TaskBoardV3Migration.BuildPlan(projectRoot, instant);
            var store = new TaskBoardDiskStore(projectRoot);
            store.ApplyV3Migration(reviewed.ReportSha256, reviewed.SourceBoardRevision, instant, dryRun: false);

            var finalized = store.FinalizeV3Migration(
                reviewed.ReportSha256,
                reviewed.SourceBoardRevision,
                instant.AddMinutes(1),
                dryRun: false);

            Assert.NotEmpty(finalized.ChangedFiles);
            Assert.False(File.Exists(Path.Combine(projectRoot, ".taskboard", ".migration", "v2", "board.e2tasks")));
            Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", ".migration", "v2", "report.json")));
            var board = JsonNode.Parse(File.ReadAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")))!.AsObject();
            Assert.True(board["migration"]!["finalized"]!.GetValue<bool>());
            Assert.Equal(reviewed.SourceBoardRevision + 1, board["revision"]!.GetValue<long>());
            Assert.Empty(new TaskBoardV3DiskStore(projectRoot).Verify().CompletedTasks);
        }
        finally
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }

    private static JsonDocument ReadSchema(string fileName)
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "data",
            "schemas",
            "project-system",
            fileName);
        Assert.True(File.Exists(path), $"Taskboard schema '{fileName}' must be published.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string CreateNativeV3Project()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-TaskBoardV3ConcurrentWriter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "tasks"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "completed"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "attachments"));
        File.WriteAllText(
            Path.Combine(projectRoot, ".taskboard", "board.e2tasks"),
            TaskBoardV3Migration.Serialize(TaskBoardV3DiskStore.CreateNativeBoard()));
        return projectRoot;
    }

    private static JsonElement ResolveLocalReference(JsonDocument schema, JsonElement element)
    {
        if (!element.TryGetProperty("$ref", out var reference))
        {
            return element;
        }

        const string definitionsPrefix = "#/$defs/";
        var value = reference.GetString();
        Assert.NotNull(value);
        Assert.StartsWith(definitionsPrefix, value, StringComparison.Ordinal);
        return schema.RootElement.GetProperty("$defs").GetProperty(value![definitionsPrefix.Length..]);
    }

    private static JsonObject CreateTask(
        string taskUid,
        string taskId,
        string boardId = "main",
        string status = "Ready")
    {
        var terminal = status is "Done" or "Cancelled";
        var task = JsonNode.Parse($$"""
        {
          "format": "Electron2D.TaskFile",
          "version": 3,
          "boardId": "{{boardId}}",
          "taskUid": "{{taskUid}}",
          "revision": 1,
          "taskId": "{{taskId}}",
          "legacyAliases": [],
          "title": "Задача",
          "description": "Описание",
          "status": "{{status}}",
          "acceptanceState": "{{(status == "Done" ? "Accepted" : status == "Cancelled" ? "Cancelled" : "NotSubmitted")}}",
          "priority": "P1",
          "tagIds": [],
          "deadline": null,
          "createdBy": "cli",
          "assignee": "worker-1",
          "parentTaskUid": null,
          "relations": [],
          "acceptanceCriteria": [
            { "criterionId": "criterion-1", "description": "Результат проверен", "state": "{{(status == "Done" ? "Passed" : "Open")}}", "evidence": {{(status == "Done" ? "[{ \"kind\": \"File\", \"path\": \"docs/evidence.md\" }]" : "[]")}} }
          ],
          "blockers": [],
          "lastActivitySequence": 0,
          "activity": [],
          "auditRuns": [],
          "conversation": { "lastMessageSequence": 0, "messages": [], "contextCheckpoints": [] },
          "contextSnapshot": null,
          "workspaceChanges": { "baseRevision": null, "currentRevision": null, "files": [] },
          "links": [],
          "executionContract": {
            "taskType": "general", "readyToStart": [], "stopConditions": [],
            "allowedChanges": [], "forbiddenChanges": [], "requiredOutputs": [],
            "commands": [],
            "externalAudit": { "mode": "None", "independence": "NotRequired", "instructions": null, "requiredVerdicts": [] }
          },
          "attachments": [],
          "previewAttachmentId": null,
          "legacySourceFragments": [],
          "createdAt": "2026-07-13T00:00:00+03:00",
          "updatedAt": "2026-07-13T00:00:00+03:00",
          "submittedAt": {{(status == "Done" ? "\"2026-07-13T00:00:00+03:00\"" : "null")}},
          "completedAt": {{(status == "Done" ? "\"2026-07-13T00:00:00+03:00\"" : "null")}},
          "acceptedAt": {{(status == "Done" ? "\"2026-07-13T00:00:00+03:00\"" : "null")}},
          "acceptedBy": {{(status == "Done" ? "\"auditor-2\"" : "null")}},
          "cancelledAt": {{(status == "Cancelled" ? "\"2026-07-13T00:00:00+03:00\"" : "null")}},
          "cancellationReason": {{(status == "Cancelled" ? "\"Больше не требуется\"" : "null")}},
          "archivedAt": {{(terminal ? "\"2026-07-13T00:01:00+03:00\"" : "null")}},
          "archivedBy": {{(terminal ? "\"cli\"" : "null")}}
        }
        """)!.AsObject();

        if (status == "Done")
        {
            task["activity"]!.AsArray().Add(JsonNode.Parse("""
             {
              "activityEntryId": "activity-acceptance-1", "sequence": 1, "actorId": "auditor-2", "actorKind": "Agent",
              "createdAt": "2026-07-13T00:00:00+03:00", "kind": "AcceptanceResult",
              "payload": { "decision": "Accepted", "reason": "Принято", "authorityActorId": "auditor-2", "authorityRole": "Auditor", "auditRunId": null }
            }
            """));
            task["lastActivitySequence"] = 1;
        }

        return task;
    }

    private static JsonObject CreateBoard(params (string TaskUid, string? GroupId, string Rank)[] placements)
    {
        var board = JsonNode.Parse("""
        {
          "format": "Electron2D.TaskBoard", "version": 3, "boardId": "main", "revision": 1,
          "idPolicy": { "prefix": "T-", "padding": 4, "nextNumber": 3 },
          "attachmentPolicy": { "perFileByteLimit": 26214400, "perTaskByteLimit": 104857600, "boardByteLimit": 262144000 },
          "validationContract": {
            "semanticValidator": "TaskBoardSemanticValidatorV3",
            "transitionValidator": "TaskTransitionValidatorV3",
            "contextBuilder": "AgentContextBuilderV3",
            "executionPolicy": "TaskExecutionPolicyV3",
            "formatAssertions": true
          },
          "migration": null,
          "tags": [], "groups": [], "placements": []
        }
        """)!.AsObject();
        foreach (var placement in placements)
        {
            board["placements"]!.AsArray().Add(new JsonObject
            {
                ["taskUid"] = placement.TaskUid,
                ["groupId"] = placement.GroupId,
                ["rank"] = placement.Rank
            });
        }

        return board;
    }

    private static void AddRelation(JsonObject task, string relationId, string kind, string targetTaskUid)
    {
        task["relations"]!.AsArray().Add(new JsonObject
        {
            ["relationId"] = relationId,
            ["kind"] = kind,
            ["targetTaskUid"] = targetTaskUid
        });
    }

    private static JsonObject CreateCommentActivity(string activityEntryId)
    {
        return JsonNode.Parse($$"""
        {
          "activityEntryId": "{{activityEntryId}}", "sequence": 1, "actorId": "agent-1", "actorKind": "Agent",
          "createdAt": "2026-07-13T00:01:00+03:00", "kind": "Comment",
          "payload": { "markdown": "Комментарий" }
        }
        """)!.AsObject();
    }

    private static JsonObject CreateStatusActivity(string activityEntryId, long sequence)
    {
        return new JsonObject
        {
            ["activityEntryId"] = activityEntryId,
            ["sequence"] = sequence,
            ["actorId"] = "agent-1",
            ["actorKind"] = "Agent",
            ["createdAt"] = "2026-07-13T00:01:00+03:00",
            ["kind"] = "StatusChange",
            ["payload"] = new JsonObject
            {
                ["previous"] = "Ready",
                ["next"] = "InProgress",
                ["reason"] = "Начало работы"
            }
        };
    }

    private static JsonObject CreateProcessCommand(string commandId, string executable, params string[] requestedCapabilities)
    {
        var capabilities = new JsonArray();
        foreach (var capability in requestedCapabilities)
        {
            capabilities.Add(capability);
        }

        return new JsonObject
        {
            ["commandId"] = commandId,
            ["kind"] = "Process",
            ["executable"] = executable,
            ["arguments"] = new JsonArray("--info"),
            ["workingDirectory"] = ".",
            ["platforms"] = new JsonArray("Any"),
            ["timeoutSeconds"] = 60,
            ["expectedExitCodes"] = new JsonArray(0),
            ["requestedCapabilities"] = capabilities,
            ["confirmation"] = "PolicyDecides"
        };
    }

    private static JsonObject CreateAttachment(string taskUid, string attachmentId, long byteLength, long? derivativeLength = null)
    {
        var hash = new string('0', 64);
        var derivatives = new JsonArray();
        foreach (var kind in new[] { "ExtractedText", "Ocr", "Preview" })
        {
            var ready = derivativeLength is not null && kind == "ExtractedText";
            derivatives.Add(new JsonObject
            {
                ["derivativeId"] = $"derivative-{kind.ToLowerInvariant()}",
                ["kind"] = kind,
                ["status"] = ready ? "Ready" : "Pending",
                ["failureReason"] = null,
                ["relativePath"] = ready ? $".taskboard/derived/{taskUid}/{attachmentId}/derivative-{kind.ToLowerInvariant()}/extracted.txt" : null,
                ["mediaType"] = ready ? "text/plain" : null,
                ["byteLength"] = ready ? derivativeLength!.Value : null,
                ["sha256"] = ready ? hash : null,
                ["sourceSha256"] = hash,
                ["extractor"] = ready ? new JsonObject { ["name"] = "test", ["version"] = "1" } : null,
                ["createdAt"] = "2026-07-13T00:00:00+03:00"
            });
        }

        return new JsonObject
        {
            ["attachmentId"] = attachmentId,
            ["displayName"] = "evidence.txt",
            ["relativePath"] = $".taskboard/attachments/{taskUid}/{attachmentId}/evidence.txt",
            ["mediaType"] = "text/plain",
            ["byteLength"] = byteLength,
            ["sha256"] = hash,
            ["addedAt"] = "2026-07-13T00:00:00+03:00",
            ["addedBy"] = "cli",
            ["derivatives"] = derivatives
        };
    }

    private static JsonObject CreateAuditRun(
        string runId,
        string stage,
        string auditorId,
        long taskRevision,
        string contextDigest,
        string reportAttachmentId,
        params string[] previousVerdictChain)
    {
        var workspaceChangesDigest = WorkspaceChangesBuilderV3.ComputeSnapshotDigest(new JsonObject
        {
            ["baseRevision"] = null,
            ["currentRevision"] = null,
            ["files"] = new JsonArray()
        });
        var primaryReportAttachmentId = reportAttachmentId.Replace("control", "primary", StringComparison.Ordinal);
        var controlContext = stage == "Control"
            ? new JsonObject
            {
                ["primaryRunId"] = previousVerdictChain[0],
                ["excludedRunIds"] = new JsonArray(previousVerdictChain.Select(value => (JsonNode)value).ToArray()),
                ["excludedReportAttachmentIds"] = new JsonArray(primaryReportAttachmentId)
            }
            : null;
        var packageManifest = new JsonObject
        {
            ["taskRevision"] = taskRevision,
            ["contextDigest"] = contextDigest,
            ["workspaceChangesDigest"] = workspaceChangesDigest,
            ["inputAttachmentIds"] = new JsonArray(),
            ["excludedAttachmentIds"] = new JsonArray(
                (stage == "Control" ? new[] { primaryReportAttachmentId, reportAttachmentId } : new[] { reportAttachmentId })
                    .Select(value => (JsonNode)value).ToArray())
        };
        var packageDigest = AgentContextBuilderV3.HashCanonical(new JsonObject
        {
            ["packageManifest"] = packageManifest.DeepClone(),
            ["controlContext"] = controlContext?.DeepClone()
        });
        return new JsonObject
        {
            ["runId"] = runId,
            ["stage"] = stage,
            ["auditorIdentity"] = new JsonObject
            {
                ["actorId"] = auditorId,
                ["actorKind"] = "Agent",
                ["role"] = "Auditor"
            },
            ["createdAt"] = "2026-07-13T00:01:00+03:00",
            ["taskRevision"] = taskRevision,
            ["recordedAtRevision"] = taskRevision + 1,
            ["contextDigest"] = contextDigest,
            ["workspaceChangesDigest"] = workspaceChangesDigest,
            ["packageManifest"] = packageManifest,
            ["controlContext"] = controlContext,
            ["packageDigest"] = packageDigest,
            ["reportAttachmentId"] = reportAttachmentId,
            ["decision"] = "Accepted",
            ["previousVerdictChain"] = new JsonArray(previousVerdictChain.Select(value => (JsonNode)value).ToArray())
        };
    }

    private static void CreateDirectoryLink(string linkPath, string targetPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return;
        }

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add("New-Item -ItemType Junction -Path $env:E2D_LINK -Target $env:E2D_TARGET | Out-Null");
        startInfo.Environment["E2D_LINK"] = linkPath;
        startInfo.Environment["E2D_TARGET"] = targetPath;
        using var process = System.Diagnostics.Process.Start(startInfo) ??
            throw new InvalidOperationException("PowerShell junction helper did not start.");
        Assert.True(process.WaitForExit(10_000), "PowerShell junction helper timed out.");
        var standardError = process.StandardError.ReadToEnd();
        Assert.True(process.ExitCode == 0, $"PowerShell junction helper failed: {standardError}");
    }

    private static JsonObject CreateBlocker(string state, string? resolvedAt, string? resolvedBy)
    {
        return new JsonObject
        {
            ["blockerId"] = "blocker-1",
            ["kind"] = "Manual",
            ["reason"] = "Требуется решение",
            ["state"] = state,
            ["createdAt"] = "2026-07-13T00:00:00+03:00",
            ["createdBy"] = "worker-1",
            ["resolvedAt"] = resolvedAt,
            ["resolvedBy"] = resolvedBy
        };
    }

    private static void SetAcceptanceAuthority(JsonObject task, string actorId, string actorKind, string role)
    {
        task["acceptedBy"] = actorId;
        var activity = task["activity"]![0]!.AsObject();
        activity["actorId"] = actorId;
        activity["actorKind"] = actorKind;
        activity["payload"]!["authorityActorId"] = actorId;
        activity["payload"]!["authorityRole"] = role;
    }

    private static void AddMessage(JsonObject task, string messageId, long sequence, string markdown)
    {
        var conversation = task["conversation"]!.AsObject();
        conversation["lastMessageSequence"] = sequence;
        conversation["messages"]!.AsArray().Add(new JsonObject
        {
            ["messageId"] = messageId,
            ["sequence"] = sequence,
            ["author"] = new JsonObject
            {
                ["actorId"] = "worker-1",
                ["actorKind"] = "Agent",
                ["role"] = "Worker"
            },
            ["createdAt"] = "2026-07-13T00:01:00+03:00",
            ["replyToMessageId"] = null,
            ["agentRunId"] = "run-1",
            ["content"] = new JsonArray(new JsonObject { ["kind"] = "Markdown", ["markdown"] = markdown })
        });
    }

    private static string CreateV2MigrationFixture()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-TaskBoardV3Migration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "tasks"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "completed"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "attachments"));

        var parent = CreateV2Task(TaskUidA, "T-0001", "Родительская задача");
        parent.Subtasks.Add("T-0002");
        var child = CreateV2Task(TaskUidB, "T-0002", "Дочерняя задача");
        child.ParentTaskId = "T-0001";
        child.Dependencies.Add("T-0001");
        child.ExecutionContract.RequiredCommands.Add("dotnet test --filter Child");
        child.Activity.Add(new TaskActivityEntry(
            "activity-comment-1",
            "agent-1",
            PrincipalKind.Agent,
            new DateTimeOffset(2026, 7, 13, 0, 10, 0, TimeSpan.FromHours(3)),
            TaskActivityKind.Comment,
            "**Проверено** агентом"));

        var board = new TaskBoard(
            "main",
            revision: 7,
            groups: [],
            placements:
            [
                new TaskBoardPlacement("T-0001", null, "00001000"),
                new TaskBoardPlacement("T-0002", null, "00002000")
            ]);
        board.IdPolicy.NextNumber = 3;

        File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks"), ProjectTaskSerializer.SerializeBoard(board));
        File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task"), ProjectTaskSerializer.Serialize(parent));
        File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "tasks", "T-0002.e2task"), ProjectTaskSerializer.Serialize(child));
        return projectRoot;
    }

    private static ProjectTask CreateV2Task(string taskUid, string taskId, string title)
    {
        var task = new ProjectTask
        {
            TaskUid = taskUid,
            TaskId = taskId,
            Title = title,
            Description = "Описание",
            Status = ProjectTaskStatus.Ready,
            Priority = "P1",
            CreatedBy = "cli",
            Revision = 1,
            CreatedAt = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.FromHours(3)),
            UpdatedAt = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.FromHours(3)),
            AcceptanceState = ProjectTaskAcceptanceState.Open
        };
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            "criterion-1",
            "Результат проверен",
            AcceptanceCriterionState.Open,
            []));
        return task;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
