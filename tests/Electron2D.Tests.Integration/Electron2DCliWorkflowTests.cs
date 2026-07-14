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
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class Electron2DCliWorkflowTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RootAndGroupHelpExposeRequiredGroupsAndCommonFlags()
    {
        var root = RunCli(CliExecutionContext.ForTests(FixedInstant), "--help");

        Assert.Equal(0, root.ExitCode);
        foreach (var group in RequiredGroups)
        {
            Assert.Contains(group, root.Output, StringComparison.Ordinal);
        }

        foreach (var group in RequiredGroups)
        {
            var help = RunCli(CliExecutionContext.ForTests(FixedInstant), group, "--help");
            var expectedFormats = string.Equals(group, "docs", StringComparison.Ordinal)
                ? "--format text|json"
                : string.Equals(group, "tasks", StringComparison.Ordinal)
                    ? "--format text|json|markdown"
                : string.Equals(group, "context", StringComparison.Ordinal)
                    ? "--format text|json"
                : "--format text|json|jsonl|sarif";

            Assert.Equal(0, help.ExitCode);
            Assert.Contains("--project <path>", help.Output, StringComparison.Ordinal);
            Assert.Contains(expectedFormats, help.Output, StringComparison.Ordinal);
            Assert.Contains("--quiet", help.Output, StringComparison.Ordinal);
            Assert.Contains("--verbose", help.Output, StringComparison.Ordinal);
            if (MutatingOrJobGroups.Contains(group, StringComparer.Ordinal))
            {
                Assert.Contains("--dry-run", help.Output, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TasksHelpExposesTaskboardCommandFamilies()
    {
        var help = RunCli(CliExecutionContext.ForTests(FixedInstant), "tasks", "--help");

        Assert.Equal(0, help.ExitCode);
        foreach (var command in new[] { "board", "list", "get", "create", "update", "move", "set-status", "submit", "accept", "request-changes", "archive", "delete", "comment", "parent", "dependency", "group", "attachment", "verify", "migrate", "export" })
        {
            Assert.Contains(command, help.Output, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TasksInitCreatesCanonicalTaskboardAndReturnsJsonEnvelope()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksInit-");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "tasks",
            "init",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);

        using var envelopeJson = JsonDocument.Parse(result.Output);
        var envelope = envelopeJson.RootElement;
        Assert.Equal("tasks init", envelope.GetProperty("command").GetString());
        Assert.True(envelope.GetProperty("succeeded").GetBoolean());
        Assert.Equal("none", envelope.GetProperty("route").GetString());
        Assert.Contains(ProjectTaskStorage.BoardDocumentPath, envelope.GetProperty("changedFiles").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("tasks.init", envelope.GetProperty("data").GetProperty("mode").GetString());

        var taskboardRoot = Path.Combine(projectRoot, ".taskboard");
        Assert.True(Directory.Exists(Path.Combine(taskboardRoot, "tasks")));
        Assert.True(Directory.Exists(Path.Combine(taskboardRoot, "completed")));
        Assert.True(Directory.Exists(Path.Combine(taskboardRoot, "attachments")));
        Assert.True(File.Exists(Path.Combine(taskboardRoot, "board.e2tasks")));
        Assert.True(File.Exists(Path.Combine(taskboardRoot, ".gitignore")));

        using var boardJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(taskboardRoot, "board.e2tasks")));
        Assert.Equal("Electron2D.TaskBoard", boardJson.RootElement.GetProperty("format").GetString());
        Assert.Equal(3, boardJson.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(JsonValueKind.Null, boardJson.RootElement.GetProperty("migration").ValueKind);
        Assert.Equal(0, boardJson.RootElement.GetProperty("groups").GetArrayLength());
        Assert.Equal(0, boardJson.RootElement.GetProperty("placements").GetArrayLength());
    }

    [Fact]
    public void TasksCreatePersistsTaskAndBoardPlacementWithJsonEnvelope()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksCreate-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);

        var result = RunCli(
            context,
            "tasks",
            "create",
            "--project",
            projectRoot,
            "--title",
            "Implement canonical task creation",
            "--priority",
            "P1",
            "--expected-board-revision",
            "1",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);

        using var envelopeJson = JsonDocument.Parse(result.Output);
        var envelope = envelopeJson.RootElement;
        Assert.Equal("tasks create", envelope.GetProperty("command").GetString());
        Assert.True(envelope.GetProperty("succeeded").GetBoolean());
        var task = envelope.GetProperty("data").GetProperty("task");
        Assert.Equal("T-0001", task.GetProperty("taskId").GetString());
        Assert.Equal("Implement canonical task creation", task.GetProperty("title").GetString());
        Assert.Equal("P1", task.GetProperty("priority").GetString());

        var taskPath = Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task");
        Assert.True(File.Exists(taskPath));
        using var taskJson = JsonDocument.Parse(File.ReadAllText(taskPath));
        Assert.Equal("Ready", taskJson.RootElement.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(taskJson.RootElement.GetProperty("taskUid").GetString()));

        using var boardJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")));
        var placement = Assert.Single(boardJson.RootElement.GetProperty("placements").EnumerateArray());
        Assert.Equal(taskJson.RootElement.GetProperty("taskUid").GetString(), placement.GetProperty("taskUid").GetString());
        Assert.False(placement.TryGetProperty("taskId", out _));
    }

    [Fact]
    public async Task TasksConcurrentCreateAllocatesFromLockedStateWithoutExpectedBoardRevision()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksConcurrentCreate-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCliExact(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);

        var commands = Enumerable.Range(1, 8).Select(index => Task.Run(() => RunCliExact(
            context,
            "tasks", "create",
            "--project", projectRoot,
            "--title", $"Concurrent {index}",
            "--operation-id", $"concurrent-create-{index}",
            "--format", "json"))).ToArray();

        var results = await Task.WhenAll(commands).WaitAsync(TimeSpan.FromSeconds(15));
        Assert.All(results, result => Assert.Equal(0, result.ExitCode));
        var taskIds = results.Select(result =>
        {
            using var json = JsonDocument.Parse(result.Output);
            return json.RootElement.GetProperty("data").GetProperty("task").GetProperty("taskId").GetString();
        }).ToArray();
        Assert.Equal(8, taskIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TasksOperationIdReplaysOriginalResultAndRejectsDifferentPayload()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksIdempotency-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCliExact(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);

        string[] SameCommand(string title) =>
        [
            "tasks", "create",
            "--project", projectRoot,
            "--title", title,
            "--operation-id", "agent-run-42-create",
            "--format", "json"
        ];

        var first = RunCliExact(context, SameCommand("Одна задача"));
        var replay = RunCliExact(context, SameCommand("Одна задача"));
        var conflict = RunCliExact(context, SameCommand("Другая задача"));

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, replay.ExitCode);
        using (var replayJson = JsonDocument.Parse(replay.Output))
        {
            Assert.True(replayJson.RootElement.GetProperty("data").GetProperty("replayed").GetBoolean());
            Assert.Equal("T-0001", replayJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("taskId").GetString());
        }

        Assert.NotEqual(0, conflict.ExitCode);
        using (var conflictJson = JsonDocument.Parse(conflict.Output))
        {
            Assert.Equal("E2D-TASK-0007", conflictJson.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString());
        }

        var snapshot = new TaskBoardV3DiskStore(projectRoot).Verify();
        Assert.Single(snapshot.ActiveTasks);
        Assert.Equal(2, snapshot.Board["revision"]!.GetValue<long>());
    }

    [Fact]
    public void TasksOperationIdDoesNotDuplicateMessageRelationAttachmentOrStatus()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksMutationIdempotency-");
        var attachmentPath = Path.Combine(projectRoot, "evidence.txt");
        File.WriteAllText(attachmentPath, "evidence");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--title", "Основная", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--title", "Зависимая", "--project", projectRoot, "--format", "json").ExitCode);

        void RunTwice(params string[] arguments)
        {
            var first = RunCliExact(context, arguments);
            var replay = RunCliExact(context, arguments);
            Assert.True(first.ExitCode == 0, first.Output + first.Error);
            Assert.True(replay.ExitCode == 0, replay.Output + replay.Error);
            using var json = JsonDocument.Parse(replay.Output);
            Assert.True(json.RootElement.GetProperty("data").GetProperty("replayed").GetBoolean());
        }

        RunTwice(
            "tasks", "comment", "add", "T-0001",
            "--text", "Одна реплика", "--expected-revision", "1",
            "--operation-id", "message-1", "--project", projectRoot, "--format", "json");
        RunTwice(
            "tasks", "dependency", "add", "T-0002",
            "--depends-on", "T-0001", "--expected-revision", "1",
            "--operation-id", "relation-1", "--project", projectRoot, "--format", "json");
        RunTwice(
            "tasks", "attachment", "add", "T-0001",
            "--file", attachmentPath, "--expected-revision", "2",
            "--operation-id", "attachment-1", "--project", projectRoot, "--format", "json");
        RunTwice(
            "tasks", "set-status", "T-0001",
            "--status", "InProgress", "--expected-revision", "3",
            "--operation-id", "status-1", "--project", projectRoot, "--format", "json");

        var snapshot = new TaskBoardV3DiskStore(projectRoot).Verify();
        var primary = snapshot.ActiveTasks.Single(task => task["taskId"]!.GetValue<string>() == "T-0001");
        var dependent = snapshot.ActiveTasks.Single(task => task["taskId"]!.GetValue<string>() == "T-0002");
        Assert.Equal(4, primary["revision"]!.GetValue<long>());
        Assert.Equal("InProgress", primary["status"]!.GetValue<string>());
        Assert.Single(primary["conversation"]!["messages"]!.AsArray());
        Assert.Single(primary["attachments"]!.AsArray());
        Assert.Equal(2, dependent["revision"]!.GetValue<long>());
        Assert.Single(dependent["relations"]!.AsArray());
        Assert.Single(primary["activity"]!.AsArray(), activity => activity!["kind"]!.GetValue<string>() == "StatusChange");
    }

    [Fact]
    public void TasksWriterTimeoutReturnsRetryableDiagnostic()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksWriterTimeout-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCliExact(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        using var heldLock = new TaskBoardDiskStore(projectRoot).AcquireWriteLock();

        var result = RunCliExact(
            context,
            "tasks", "create",
            "--project", projectRoot,
            "--title", "Timeout",
            "--lock-timeout-ms", "40",
            "--lock-backoff-ms", "5",
            "--format", "json");

        Assert.NotEqual(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal("E2D-TASK-0004", json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("retryable").GetBoolean());
    }

    [Fact]
    public void TasksWriterCancellationReturnsRetryableDiagnostic()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksWriterCancellation-");
        Assert.Equal(0, RunCliExact(CliExecutionContext.ForTests(FixedInstant), "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        using var heldLock = new TaskBoardDiskStore(projectRoot).AcquireWriteLock();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = RunCliExact(
            CliExecutionContext.ForTests(FixedInstant, cancellationToken: cancellation.Token),
            "tasks", "create",
            "--project", projectRoot,
            "--title", "Cancelled wait",
            "--format", "json");

        Assert.NotEqual(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal("E2D-TASK-0005", json.RootElement.GetProperty("diagnostics")[0].GetProperty("code").GetString());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("retryable").GetBoolean());
    }

    [Fact]
    public void TasksCliAcceptsAllowlistedJsonInputFromFileAndStdin()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksInput-");
        var inputRoot = CreateTemporaryDirectory("Electron2D-TasksInputPayload-");
        var createInputPath = Path.Combine(inputRoot, "create.json");
        File.WriteAllText(createInputPath, """
        {
          "title": "Created from input",
          "description": "Structured description",
          "priority": "P1",
          "expectedBoardRevision": 1,
          "executionContract": {
            "taskType": "verification",
            "readyToStart": ["Входные данные доступны."],
            "stopConditions": ["Нарушен публичный контракт."],
            "allowedChanges": ["Тесты и документация."],
            "forbiddenChanges": ["Публичный API."],
            "requiredOutputs": ["Отчёт проверки."],
            "commands": [{
              "commandId": "command-verify",
              "kind": "Process",
              "executable": "dotnet",
              "arguments": ["test", "--no-restore"],
              "workingDirectory": "tests",
              "platforms": ["Any"],
              "timeoutSeconds": 300,
              "expectedExitCodes": [0],
              "requiredAccess": "ReadOnly",
              "confirmation": "PolicyDecides"
            }],
            "externalAudit": "not-required"
          },
          "acceptanceCriteria": [{
            "criterionId": "criterion-build",
            "description": "Проверка проходит.",
            "state": "Open",
            "evidence": []
          }],
          "links": [{
            "linkId": "link-doc",
            "kind": "File",
            "value": "docs/project-system/project-task-manager.md"
          }]
        }
        """);
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);

        var created = RunCli(
            context,
            "tasks", "create", "--input", createInputPath,
            "--project", projectRoot, "--format", "json");
        Assert.True(created.ExitCode == 0, created.Output);
        using (var createdJson = JsonDocument.Parse(created.Output))
        {
            var task = createdJson.RootElement.GetProperty("data").GetProperty("task");
            Assert.Equal("Created from input", task.GetProperty("title").GetString());
            Assert.Equal("Structured description", task.GetProperty("description").GetString());
            Assert.Equal("P1", task.GetProperty("priority").GetString());
            Assert.Equal("verification", task.GetProperty("executionContract").GetProperty("taskType").GetString());
            Assert.Equal("dotnet", task.GetProperty("executionContract").GetProperty("commands")[0].GetProperty("executable").GetString());
            Assert.Equal("criterion-build", task.GetProperty("acceptanceCriteria")[0].GetProperty("criterionId").GetString());
            Assert.Equal("link-doc", task.GetProperty("links")[0].GetProperty("linkId").GetString());
        }

        var updateInput = """
        {
          "title": "Updated from stdin",
          "expectedRevision": 1,
          "links": [{
            "linkId": "link-source",
            "kind": "Directory",
            "value": "src/Electron2D.ProjectSystem"
          }]
        }
        """;
        var stdinContext = CliExecutionContext.ForTests(FixedInstant, input: new StringReader(updateInput));
        var updated = RunCli(
            stdinContext,
            "tasks", "update", "T-0001", "--input", "-",
            "--project", projectRoot, "--format", "json");
        Assert.True(updated.ExitCode == 0, updated.Output);
        using var updatedJson = JsonDocument.Parse(updated.Output);
        var updatedTask = updatedJson.RootElement.GetProperty("data").GetProperty("task");
        Assert.Equal("Updated from stdin", updatedTask.GetProperty("title").GetString());
        Assert.Equal("link-source", updatedTask.GetProperty("links")[0].GetProperty("linkId").GetString());

        var conflict = RunCli(
            CliExecutionContext.ForTests(FixedInstant, input: new StringReader("""{ "title": "JSON" }""")),
            "tasks", "update", "T-0001", "--input", "-", "--title", "flag",
            "--expected-revision", "2", "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, conflict.ExitCode);
        Assert.Contains("conflicts", conflict.Output, StringComparison.OrdinalIgnoreCase);

        File.WriteAllText(createInputPath, """
        {
          "title": "Broken command",
          "expectedBoardRevision": 2,
          "executionContract": {
            "taskType": "verification",
            "readyToStart": [], "stopConditions": [], "allowedChanges": [],
            "forbiddenChanges": [], "requiredOutputs": [],
            "commands": [{ "commandId": "broken", "kind": "Process", "executable": "dotnet" }],
            "externalAudit": "not-required"
          }
        }
        """);
        var invalidCommand = RunCli(
            context,
            "tasks", "create", "--input", createInputPath,
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, invalidCommand.ExitCode);
        Assert.Contains("arguments", invalidCommand.Output, StringComparison.OrdinalIgnoreCase);

        File.WriteAllText(createInputPath, """{ "title": "Forbidden", "acceptedAt": "2026-01-01T00:00:00Z" }""");
        var rejected = RunCli(
            context,
            "tasks", "create", "--input", createInputPath,
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, rejected.ExitCode);
        Assert.Contains("acceptedAt", rejected.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void TasksCliReadsUpdatesCommentsAndEnforcesDependencyReadiness()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksCrud-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Foundation", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Feature", "--format", "json").ExitCode);

        var dependency = RunCli(
            context,
            "tasks", "dependency", "add", "T-0002",
            "--depends-on", "T-0001",
            "--expected-revision", "1",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, dependency.ExitCode);

        var update = RunCli(
            context,
            "tasks", "update", "T-0002",
            "--title", "Feature updated",
            "--expected-revision", "2",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, update.ExitCode);

        var comment = RunCli(
            context,
            "tasks", "comment", "add", "T-0002",
            "--text", "Agent note",
            "--expected-revision", "3",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, comment.ExitCode);

        var blocked = RunCli(
            context,
            "tasks", "set-status", "T-0002",
            "--status", "InProgress",
            "--expected-revision", "4",
            "--project", projectRoot,
            "--format", "json");
        Assert.NotEqual(0, blocked.ExitCode);
        Assert.Contains("unfinished dependencies", blocked.Output, StringComparison.OrdinalIgnoreCase);

        var get = RunCli(context, "tasks", "get", "T-0002", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, get.ExitCode);
        using var getJson = JsonDocument.Parse(get.Output);
        var task = getJson.RootElement.GetProperty("data").GetProperty("task");
        Assert.Equal("Feature updated", task.GetProperty("title").GetString());
        Assert.Equal("Ready", task.GetProperty("status").GetString());
        Assert.Equal(4, task.GetProperty("revision").GetInt64());
        var message = Assert.Single(task.GetProperty("conversation").GetProperty("messages").EnumerateArray());
        Assert.Equal(1, message.GetProperty("sequence").GetInt64());
        var content = Assert.Single(message.GetProperty("content").EnumerateArray());
        Assert.Equal("Markdown", content.GetProperty("kind").GetString());
        Assert.Equal("Agent note", content.GetProperty("markdown").GetString());
        var canonicalTask = getJson.RootElement.GetProperty("data").GetProperty("canonicalTask");
        Assert.Equal(1, canonicalTask.GetProperty("conversation").GetProperty("lastMessageSequence").GetInt64());
        Assert.Equal("Electron2D.TaskFile", canonicalTask.GetProperty("format").GetString());
        var agentContext = getJson.RootElement.GetProperty("data").GetProperty("agentContext");
        Assert.Equal(4, agentContext.GetProperty("taskRevision").GetInt64());
        Assert.Equal(1, agentContext.GetProperty("lastMessageSequence").GetInt64());
        Assert.Equal(64, agentContext.GetProperty("contextDigest").GetString()!.Length);

        var list = RunCli(context, "tasks", "list", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, list.ExitCode);
        using var listJson = JsonDocument.Parse(list.Output);
        Assert.Equal(2, listJson.RootElement.GetProperty("data").GetProperty("tasks").GetArrayLength());
    }

    [Fact]
    public void TasksCliConversationAppendUsesCasWithoutDroppingTheWinningMessage()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksConversationCas-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Conversation", "--format", "json").ExitCode);

        var winner = RunCli(
            context, "tasks", "comment", "add", "T-0001", "--text", "Первый append", "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, winner.ExitCode);
        var stale = RunCli(
            context, "tasks", "comment", "add", "T-0001", "--text", "Потерянный stale append", "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, stale.ExitCode);
        Assert.Contains("revision conflict", stale.Output, StringComparison.OrdinalIgnoreCase);

        var second = RunCli(
            context, "tasks", "comment", "add", "T-0001", "--text", "Второй append", "--expected-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, second.ExitCode);

        var get = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using var json = JsonDocument.Parse(get.Output);
        var task = json.RootElement.GetProperty("data").GetProperty("canonicalTask");
        Assert.Equal(3, task.GetProperty("revision").GetInt64());
        Assert.Equal(2, task.GetProperty("conversation").GetProperty("lastMessageSequence").GetInt64());
        var messages = task.GetProperty("conversation").GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal([1L, 2L], messages.Select(message => message.GetProperty("sequence").GetInt64()).ToArray());
        Assert.Equal(
            ["Первый append", "Второй append"],
            messages.Select(message => message.GetProperty("content")[0].GetProperty("markdown").GetString()!).ToArray());
    }

    [Fact]
    public void TasksCliHumanMessageRequiresPrivateCapabilityAndPreservesTrustedIdentity()
    {
        const string capability = "VscHumanMessageCapability0123456789abcdef0123456789";
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksHumanMessage-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Human chat", "--format", "json").ExitCode);

        var rejectedInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanMessage/1",
            capability = "wrong-capability",
            text = "This must not be stored."
        });
        var rejectedContext = CliExecutionContext.ForTests(
            FixedInstant,
            input: new StringReader(rejectedInput),
            humanCapability: capability,
            humanActorId: "vscode:user-1");
        var rejected = RunCli(
            rejectedContext,
            "tasks", "__human-message", "T-0001", "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, rejected.ExitCode);

        var bridgeInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanMessage/1",
            capability,
            text = "Проверь контекст задачи."
        });
        var humanContext = CliExecutionContext.ForTests(
            FixedInstant.AddMinutes(1),
            input: new StringReader(bridgeInput),
            humanCapability: capability,
            humanActorId: "vscode:user-1");
        var appended = RunCli(
            humanContext,
            "tasks", "__human-message", "T-0001", "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");

        Assert.Equal(0, appended.ExitCode);
        using var json = JsonDocument.Parse(appended.Output);
        var task = json.RootElement.GetProperty("data").GetProperty("task");
        Assert.Equal(2, task.GetProperty("revision").GetInt64());
        var message = Assert.Single(task.GetProperty("conversation").GetProperty("messages").EnumerateArray());
        Assert.Equal("Проверь контекст задачи.", message.GetProperty("content")[0].GetProperty("markdown").GetString());
        Assert.Equal("vscode:user-1", message.GetProperty("author").GetProperty("actorId").GetString());
        Assert.Equal("Human", message.GetProperty("author").GetProperty("actorKind").GetString());
        Assert.Equal("Owner", message.GetProperty("author").GetProperty("role").GetString());
        Assert.Equal(JsonValueKind.Null, message.GetProperty("agentRunId").ValueKind);
    }

    [Fact]
    public async Task TasksCliProcessPreservesUtf8HumanMessageBridge()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string capability = "VscUtf8MessageCapability0123456789abcdef0123456789";
        const string expectedText = "Ты тут? Проверка UTF-8 — всё работает.";
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksUtf8HumanMessage-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "UTF-8 chat", "--format", "json").ExitCode);

        var bridgeInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanMessage/1",
            capability,
            text = expectedText
        });
        var executable = Path.Combine(AppContext.BaseDirectory, "e2d.exe");
        Assert.True(File.Exists(executable), $"CLI process was not found at '{executable}'.");

        var startInfo = new System.Diagnostics.ProcessStartInfo(executable)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };
        startInfo.ArgumentList.Add("tasks");
        startInfo.ArgumentList.Add("__human-message");
        startInfo.ArgumentList.Add("T-0001");
        startInfo.ArgumentList.Add("--expected-revision");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectRoot);
        startInfo.ArgumentList.Add("--format");
        startInfo.ArgumentList.Add("json");
        startInfo.Environment["E2D_TASKBOARD_HUMAN_CAPABILITY"] = capability;

        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.NotNull(process);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(bridgeInput);
        process.StandardInput.Close();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);
        var outputText = await output;
        var errorText = await error;
        Assert.True(
            process.ExitCode == 0,
            $"CLI process exited with {process.ExitCode}. stdout: {outputText} stderr: {errorText}");
        Assert.DoesNotContain("failed", errorText, StringComparison.OrdinalIgnoreCase);

        var get = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, get.ExitCode);
        using var json = JsonDocument.Parse(get.Output);
        var task = json.RootElement.GetProperty("data").GetProperty("canonicalTask");
        var message = Assert.Single(task.GetProperty("conversation").GetProperty("messages").EnumerateArray());
        Assert.Equal(expectedText, message.GetProperty("content")[0].GetProperty("markdown").GetString());
    }

    [Fact]
    public void TasksCliEntryPointPinsStandardStreamsToUtf8()
    {
        var repositoryRoot = FindRepositoryRootForTasks();
        var source = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Electron2D.Cli", "Program.cs"));

        Assert.Contains(
            "Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void TasksCliPreservesCodexAgentIdentity()
    {
        const string originatorVariable = "CODEX_INTERNAL_ORIGINATOR_OVERRIDE";
        var previousOriginator = Environment.GetEnvironmentVariable(originatorVariable);
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksCodexIdentity-");

        try
        {
            Environment.SetEnvironmentVariable(originatorVariable, null);
            var terminalContext = CliExecutionContext.Default();
            Assert.Equal(0, RunCli(terminalContext, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
            Assert.Equal(0, RunCli(terminalContext, "tasks", "create", "--project", projectRoot, "--title", "Actor identity", "--format", "json").ExitCode);
            Assert.Equal(0, RunCli(
                terminalContext,
                "tasks", "set-status", "T-0001", "--status", "InProgress", "--expected-revision", "1",
                "--project", projectRoot, "--format", "json").ExitCode);

            Environment.SetEnvironmentVariable(originatorVariable, "Codex Desktop");
            var codexContext = CliExecutionContext.Default();
            Assert.Equal(0, RunCli(
                codexContext,
                "tasks", "comment", "add", "T-0001", "--text", "Codex note", "--agent-run", "run-codex-note",
                "--expected-revision", "2",
                "--project", projectRoot, "--format", "json").ExitCode);
            Assert.Equal(0, RunCli(
                codexContext,
                "tasks", "set-status", "T-0001", "--status", "Review", "--expected-revision", "3",
                "--project", projectRoot, "--format", "json").ExitCode);

            var get = RunCli(codexContext, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
            Assert.Equal(0, get.ExitCode);
            using var json = JsonDocument.Parse(get.Output);
            var task = json.RootElement.GetProperty("data").GetProperty("canonicalTask");
            var activity = task.GetProperty("activity").EnumerateArray().ToArray();
            Assert.Equal(3, activity.Length);
            Assert.Equal("WorkspaceChangesUpdated", activity[0].GetProperty("kind").GetString());
            Assert.Equal("Cli", activity[1].GetProperty("actorKind").GetString());
            Assert.Equal("cli", activity[1].GetProperty("actorId").GetString());
            Assert.Equal("Agent", activity[2].GetProperty("actorKind").GetString());
            Assert.Equal("Codex", activity[2].GetProperty("actorId").GetString());

            var message = Assert.Single(task.GetProperty("conversation").GetProperty("messages").EnumerateArray());
            var author = message.GetProperty("author");
            Assert.Equal("Agent", author.GetProperty("actorKind").GetString());
            Assert.Equal("Codex", author.GetProperty("actorId").GetString());
            Assert.Equal("Worker", author.GetProperty("role").GetString());
            Assert.Equal("run-codex-note", message.GetProperty("agentRunId").GetString());
        }
        finally
        {
            Environment.SetEnvironmentVariable(originatorVariable, previousOriginator);
        }
    }

    [Fact]
    public void TasksCliContextCheckpointRecordsRevisionSequenceAndDigest()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksContextCheckpoint-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "create", "--project", projectRoot, "--title", "Context owner", "--format", "json").ExitCode);

        var checkpointContext = CliExecutionContext.ForTests(FixedInstant.AddMinutes(1));
        var checkpoint = RunCli(
            checkpointContext,
            "tasks", "context", "checkpoint", "T-0001",
            "--agent-run", "run-1",
            "--expected-revision", "1",
            "--project", projectRoot,
            "--format", "json");
        Assert.True(checkpoint.ExitCode == 0, checkpoint.Output);

        var get = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, get.ExitCode);
        using var getJson = JsonDocument.Parse(get.Output);
        var data = getJson.RootElement.GetProperty("data");
        var canonicalTask = data.GetProperty("canonicalTask");
        Assert.Equal(2, canonicalTask.GetProperty("revision").GetInt64());
        var storedCheckpoint = Assert.Single(canonicalTask
            .GetProperty("conversation")
            .GetProperty("contextCheckpoints")
            .EnumerateArray());
        Assert.Equal("run-1", storedCheckpoint.GetProperty("agentRunId").GetString());
        Assert.Equal("Worker", storedCheckpoint.GetProperty("role").GetString());
        Assert.Equal(2, storedCheckpoint.GetProperty("taskRevision").GetInt64());
        Assert.Equal(0, storedCheckpoint.GetProperty("lastMessageSequence").GetInt64());
        Assert.Equal(JsonValueKind.Null, storedCheckpoint.GetProperty("rebaseOfCheckpointId").ValueKind);

        var agentContext = data.GetProperty("agentContext");
        Assert.Equal(2, agentContext.GetProperty("taskRevision").GetInt64());
        Assert.Equal(0, agentContext.GetProperty("lastMessageSequence").GetInt64());
        Assert.Equal(
            storedCheckpoint.GetProperty("contextDigest").GetString(),
            agentContext.GetProperty("contextDigest").GetString());
    }

    [Fact]
    public void StructuredUpdatePreservesExistingContextCheckpointPrefix()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksCheckpointUpdate-");
        var inputRoot = CreateTemporaryDirectory("Electron2D-TasksCheckpointUpdateInput-");
        var updateInputPath = Path.Combine(inputRoot, "update.json");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "create", "--project", projectRoot, "--title", "Checkpoint update", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "context", "checkpoint", "T-0001",
            "--agent-run", "run-checkpoint-update",
            "--expected-revision", "1",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "set-status", "T-0001", "--status", "InProgress", "--expected-revision", "2",
            "--project", projectRoot, "--format", "json").ExitCode);

        using var beforeJson = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task")));
        var beforeTask = beforeJson.RootElement;
        var beforeCheckpoint = Assert.Single(beforeTask
            .GetProperty("conversation")
            .GetProperty("contextCheckpoints")
            .EnumerateArray());
        var beforeCheckpointJson = beforeCheckpoint.GetRawText();
        var beforeCheckpointNode = JsonNode.Parse(beforeCheckpointJson);
        var beforeActivity = beforeTask.GetProperty("activity").EnumerateArray().ToArray();
        Assert.True(
            beforeCheckpoint.GetProperty("lastActivitySequence").GetInt64() <
            beforeTask.GetProperty("lastActivitySequence").GetInt64());

        var executionContract = JsonNode.Parse(beforeTask.GetProperty("executionContract").GetRawText())!.AsObject();
        executionContract["requiredOutputs"] = new JsonArray("Структурное обновление сохранило checkpoint prefix.");
        File.WriteAllText(
            updateInputPath,
            new JsonObject
            {
                ["expectedRevision"] = beforeTask.GetProperty("revision").GetInt64(),
                ["executionContract"] = executionContract
            }.ToJsonString());

        var updated = RunCli(
            context,
            "tasks", "update", "T-0001", "--input", updateInputPath,
            "--project", projectRoot, "--format", "json");
        Assert.True(updated.ExitCode == 0, updated.Output);
        using var updatedJson = JsonDocument.Parse(updated.Output);
        var updatedTask = updatedJson.RootElement.GetProperty("data").GetProperty("task");
        var updatedCheckpoint = Assert.Single(updatedTask
            .GetProperty("conversation")
            .GetProperty("contextCheckpoints")
            .EnumerateArray());
        Assert.True(
            JsonNode.DeepEquals(beforeCheckpointNode, JsonNode.Parse(updatedCheckpoint.GetRawText())),
            "Structured update must preserve every existing context checkpoint field.");
        Assert.Equal(
            "Структурное обновление сохранило checkpoint prefix.",
            Assert.Single(updatedTask.GetProperty("executionContract").GetProperty("requiredOutputs").EnumerateArray()).GetString());

        var updatedActivity = updatedTask.GetProperty("activity").EnumerateArray().ToArray();
        Assert.Equal(beforeActivity.Length + 1, updatedActivity.Length);
        Assert.Equal("TaskPatched", updatedActivity[^1].GetProperty("kind").GetString());
        Assert.Equal(
            updatedTask.GetProperty("lastActivitySequence").GetInt64(),
            updatedActivity[^1].GetProperty("sequence").GetInt64());
    }

    [Fact]
    public void TasksCliSubmitAllowsAgentButAcceptRequiresTrustedHumanStdioBridge()
    {
        const string capability = "VscHumanCapability0123456789abcdef0123456789abcdef";
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksAcceptance-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Review me", "--format", "json").ExitCode);
        AddPassedCriterion(context, projectRoot, "T-0001", 1);
        Assert.Equal(0, RunCli(context, "tasks", "set-status", "T-0001", "--status", "InProgress", "--expected-revision", "4", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "set-status", "T-0001", "--status", "Review", "--expected-revision", "5", "--project", projectRoot, "--format", "json").ExitCode);

        var submitted = RunCli(
            context,
            "tasks", "submit", "T-0001", "--reason", "Ready for review", "--expected-revision", "6",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, submitted.ExitCode);
        using (var submittedJson = JsonDocument.Parse(submitted.Output))
        {
            var task = submittedJson.RootElement.GetProperty("data").GetProperty("task");
            Assert.Equal("Review", task.GetProperty("status").GetString());
            Assert.Equal("Submitted", task.GetProperty("acceptanceState").GetString());
        }

        var unavailable = RunCli(
            context,
            "tasks", "accept", "T-0001", "--reason", "Looks good", "--expected-revision", "7",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, unavailable.ExitCode);
        Assert.Contains("trusted human", unavailable.Output, StringComparison.OrdinalIgnoreCase);

        var bridgeInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanDecision/1",
            capability,
            decision = "accept",
            reason = "Accepted in VS Code after confirmation."
        });
        var humanContext = CliExecutionContext.ForTests(
            FixedInstant,
            input: new StringReader(bridgeInput),
            humanCapability: capability,
            humanActorId: "user-1");
        var accepted = RunCli(
            humanContext,
            "tasks", "__human-decision", "T-0001", "--expected-revision", "7",
            "--project", projectRoot, "--format", "json");

        Assert.Equal(0, accepted.ExitCode);
        using var acceptedJson = JsonDocument.Parse(accepted.Output);
        var acceptedTask = acceptedJson.RootElement.GetProperty("data").GetProperty("task");
        Assert.Equal("Done", acceptedTask.GetProperty("status").GetString());
        Assert.Equal("Accepted", acceptedTask.GetProperty("acceptanceState").GetString());
        Assert.Equal("user-1", acceptedTask.GetProperty("acceptedBy").GetString());
        Assert.Equal(FixedInstant, acceptedTask.GetProperty("acceptedAt").GetDateTimeOffset());
    }

    [Fact]
    public void TasksCliHumanBridgeCanRequestChangesButRejectsMismatchedCapability()
    {
        const string capability = "VscHumanCapabilityabcdef0123456789abcdef0123456789";
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksRequestChanges-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Review me", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "set-status", "T-0001", "--status", "InProgress", "--expected-revision", "1", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "set-status", "T-0001", "--status", "Review", "--expected-revision", "2", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "submit", "T-0001", "--expected-revision", "3", "--project", projectRoot, "--format", "json").ExitCode);

        var bridgeInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanDecision/1",
            capability = "different-capability",
            decision = "request-changes",
            reason = "Please add evidence."
        });
        var rejectedContext = CliExecutionContext.ForTests(
            FixedInstant,
            input: new StringReader(bridgeInput),
            humanCapability: capability,
            humanActorId: "user-1");
        var rejected = RunCli(
            rejectedContext,
            "tasks", "__human-decision", "T-0001", "--expected-revision", "4",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, rejected.ExitCode);

        var malformedInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanDecision/1",
            capability,
            decision = "request-changes",
            unexpected = "field"
        });
        var malformedContext = CliExecutionContext.ForTests(
            FixedInstant,
            input: new StringReader(malformedInput),
            humanCapability: capability,
            humanActorId: "user-1");
        var malformed = RunCli(
            malformedContext,
            "tasks", "__human-decision", "T-0001", "--expected-revision", "4",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, malformed.ExitCode);
        Assert.Contains("bridge validation failed", malformed.Output, StringComparison.OrdinalIgnoreCase);

        var acceptedInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanDecision/1",
            capability,
            decision = "request-changes",
            reason = "Please add evidence."
        });
        var humanContext = CliExecutionContext.ForTests(
            FixedInstant,
            input: new StringReader(acceptedInput),
            humanCapability: capability,
            humanActorId: "user-1");
        var requested = RunCli(
            humanContext,
            "tasks", "__human-decision", "T-0001", "--expected-revision", "4",
            "--project", projectRoot, "--format", "json");

        Assert.Equal(0, requested.ExitCode);
        using var requestedJson = JsonDocument.Parse(requested.Output);
        var task = requestedJson.RootElement.GetProperty("data").GetProperty("task");
        Assert.Equal("InProgress", task.GetProperty("status").GetString());
        Assert.Equal("ChangesRequested", task.GetProperty("acceptanceState").GetString());
        Assert.Null(task.GetProperty("acceptedAt").GetString());
    }

    [Fact]
    public void TasksCliArchivesUnarchivesReopensAndVerifiesTaskboard()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksArchive-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Disposable", "--format", "json").ExitCode);

        Assert.Equal(0, RunCli(
            context, "tasks", "cancel", "T-0001",
            "--expected-revision", "1", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "archive", "T-0001",
            "--expected-revision", "2", "--expected-board-revision", "2",
            "--project", projectRoot, "--format", "json").ExitCode);

        Assert.False(File.Exists(Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", "completed", "T-0001.e2task")));

        var archived = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, archived.ExitCode);
        using (var archivedJson = JsonDocument.Parse(archived.Output))
        {
            Assert.Equal("Cancelled", archivedJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("status").GetString());
        }

        var activeBoard = RunCli(context, "tasks", "board", "--project", projectRoot, "--format", "json");
        using (var activeBoardJson = JsonDocument.Parse(activeBoard.Output))
        {
            Assert.Empty(activeBoardJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray());
        }

        var archiveBoard = RunCli(
            context, "tasks", "board", "--include-archived", "true",
            "--project", projectRoot, "--format", "json");
        using (var archiveBoardJson = JsonDocument.Parse(archiveBoard.Output))
        {
            var archivedTask = Assert.Single(archiveBoardJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray());
            Assert.Equal("T-0001", archivedTask.GetProperty("taskId").GetString());
            Assert.Equal(FixedInstant, archivedTask.GetProperty("archivedAt").GetDateTimeOffset());
        }

        Assert.Equal(0, RunCli(
            context, "tasks", "unarchive", "T-0001",
            "--expected-revision", "3", "--expected-board-revision", "3",
            "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "reopen", "T-0001",
            "--expected-revision", "4",
            "--project", projectRoot, "--format", "json").ExitCode);

        var reopened = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using (var reopenedJson = JsonDocument.Parse(reopened.Output))
        {
            Assert.Equal("Ready", reopenedJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("status").GetString());
        }

        var verify = RunCli(context, "tasks", "verify", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, verify.ExitCode);
        using var verifyJson = JsonDocument.Parse(verify.Output);
        Assert.True(verifyJson.RootElement.GetProperty("data").GetProperty("valid").GetBoolean());
        Assert.Equal(1, verifyJson.RootElement.GetProperty("data").GetProperty("activeTaskCount").GetInt32());
        Assert.Equal(0, verifyJson.RootElement.GetProperty("data").GetProperty("completedTaskCount").GetInt32());
    }

    [Fact]
    public void TasksBoardCompactReturnsCardFieldsAndDefersFullDetailsToGet()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksCompactBoard-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "create",
            "--title", "Compact card",
            "--description", new string('\u0416', 4096),
            "--project", projectRoot,
            "--format", "json").ExitCode);

        var board = RunCli(
            context,
            "tasks", "board",
            "--compact", "true",
            "--project", projectRoot,
            "--format", "json");

        Assert.Equal(0, board.ExitCode);
        using (var boardJson = JsonDocument.Parse(board.Output))
        {
            var card = Assert.Single(boardJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray());
            Assert.Equal("T-0001", card.GetProperty("taskId").GetString());
            Assert.Equal("Compact card", card.GetProperty("title").GetString());
            Assert.Equal("NotSubmitted", card.GetProperty("acceptanceState").GetString());
            Assert.False(card.TryGetProperty("description", out _));
            Assert.False(card.TryGetProperty("legacySourceFragments", out _));
            Assert.False(card.TryGetProperty("activity", out _));
            Assert.False(card.TryGetProperty("acceptanceCriteria", out _));
        }

        var details = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, details.ExitCode);
        using var detailsJson = JsonDocument.Parse(details.Output);
        Assert.Equal(4096, detailsJson.RootElement.GetProperty("data").GetProperty("task")
            .GetProperty("description").GetString()!.Length);
    }

    [Fact]
    public void TasksTagCommandsCreateGlobalTagAndCompactCardMetrics()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TaskTags-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "create",
            "--title", "Карточка с показателями",
            "--deadline", "2026-08-26",
            "--project", projectRoot,
            "--format", "json").ExitCode);

        var created = RunCli(
            context,
            "tasks", "tag", "create",
            "--name", "Интерфейс",
            "--color", "Blue",
            "--assign-to", "T-0001",
            "--expected-task-revision", "1",
            "--expected-board-revision", "2",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, created.ExitCode);

        Assert.Equal(0, RunCli(
            context,
            "tasks", "criterion", "add", "T-0001",
            "--criterion", "criterion-001",
            "--description", "Карточка показывает прогресс.",
            "--expected-revision", "2",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "criterion", "add-evidence", "T-0001",
            "--criterion", "criterion-001",
            "--kind", "File",
            "--value", "docs/card-metrics.md",
            "--expected-revision", "3",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "criterion", "set-state", "T-0001",
            "--criterion", "criterion-001",
            "--state", "Passed",
            "--expected-revision", "4",
            "--project", projectRoot,
            "--format", "json").ExitCode);

        var board = RunCli(
            context,
            "tasks", "board", "--compact", "true",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, board.ExitCode);
        using var boardJson = JsonDocument.Parse(board.Output);
        var boardData = boardJson.RootElement.GetProperty("data");
        var tag = Assert.Single(boardData.GetProperty("board").GetProperty("tags").EnumerateArray());
        Assert.Equal("Интерфейс", tag.GetProperty("name").GetString());
        Assert.Equal("Blue", tag.GetProperty("color").GetString());
        var card = Assert.Single(boardData.GetProperty("tasks").EnumerateArray());
        Assert.Equal(tag.GetProperty("tagId").GetString(), Assert.Single(card.GetProperty("labels").EnumerateArray()).GetString());
        Assert.Equal("2026-08-26", card.GetProperty("deadline").GetString());
        Assert.Equal(1, card.GetProperty("acceptanceCriteriaProgress").GetProperty("passed").GetInt32());
        Assert.Equal(1, card.GetProperty("acceptanceCriteriaProgress").GetProperty("total").GetInt32());
        Assert.Equal(0, card.GetProperty("attachmentCount").GetInt32());

        Assert.Equal(0, RunCli(
            context,
            "tasks", "group", "add",
            "--kind", "Epoch",
            "--title", "Проверка сохранения тегов",
            "--expected-board-revision", "3",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        var boardAfterGroup = RunCli(context, "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        using (var boardAfterGroupJson = JsonDocument.Parse(boardAfterGroup.Output))
        {
            Assert.Equal(tag.GetProperty("tagId").GetString(), Assert.Single(boardAfterGroupJson.RootElement
                .GetProperty("data").GetProperty("board").GetProperty("tags").EnumerateArray()).GetProperty("tagId").GetString());
        }

        var duplicateLocalValue = RunCli(
            context,
            "tasks", "tag", "assign", "T-0001",
            "--tag", "missing-tag",
            "--expected-task-revision", "5",
            "--expected-board-revision", "4",
            "--project", projectRoot,
            "--format", "json");
        Assert.NotEqual(0, duplicateLocalValue.ExitCode);

        var tagId = tag.GetProperty("tagId").GetString()!;
        Assert.Equal(0, RunCli(
            context,
            "tasks", "tag", "unassign", "T-0001",
            "--tag", tagId,
            "--expected-task-revision", "5",
            "--expected-board-revision", "4",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "tag", "update", tagId,
            "--name", "Пользовательский интерфейс",
            "--color", "Purple",
            "--expected-board-revision", "4",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "tag", "delete", tagId,
            "--expected-board-revision", "5",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "update", "T-0001",
            "--clear-deadline", "true",
            "--expected-revision", "6",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        var cleared = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using var clearedJson = JsonDocument.Parse(cleared.Output);
        Assert.Equal(JsonValueKind.Null, clearedJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("deadline").ValueKind);
    }

    [Fact]
    public void TasksTagCommandsNormalizeCustomHexColorsAndRejectInvalidForms()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TaskTagColors-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);

        var created = RunCliExact(
            context,
            "tasks", "tag", "create",
            "--name", "Custom color",
            "--color", "#a1b2c3",
            "--expected-board-revision", "1",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, created.ExitCode);
        using var createdJson = JsonDocument.Parse(created.Output);
        var tag = Assert.Single(createdJson.RootElement.GetProperty("data").GetProperty("board")
            .GetProperty("tags").EnumerateArray());
        Assert.Equal("#A1B2C3", tag.GetProperty("color").GetString());
        var tagId = tag.GetProperty("tagId").GetString()!;

        var updated = RunCliExact(
            context,
            "tasks", "tag", "update", tagId,
            "--color", "#0f80ff",
            "--expected-board-revision", "2",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, updated.ExitCode);
        using var updatedJson = JsonDocument.Parse(updated.Output);
        Assert.Equal("#0F80FF", Assert.Single(updatedJson.RootElement.GetProperty("data").GetProperty("board")
            .GetProperty("tags").EnumerateArray()).GetProperty("color").GetString());

        foreach (var invalidColor in new[] { "#ABC", "#AABBCCDD", "rgba(1,2,3,1)" })
        {
            var invalid = RunCliExact(
                context,
                "tasks", "tag", "update", tagId,
                "--color", invalidColor,
                "--expected-board-revision", "3",
                "--project", projectRoot,
                "--format", "json");
            Assert.NotEqual(0, invalid.ExitCode);
        }
    }

    [Fact]
    public void TasksTagApplyUpdatesWholePlanAtomically()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TaskTagApply-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--title", "Первая задача", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--title", "Вторая задача", "--project", projectRoot, "--format", "json").ExitCode);
        var createdTag = RunCli(
            context,
            "tasks", "tag", "create",
            "--name", "Пакетный тег",
            "--color", "Blue",
            "--expected-board-revision", "3",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, createdTag.ExitCode);
        using var createdTagJson = JsonDocument.Parse(createdTag.Output);
        var tagId = createdTagJson.RootElement.GetProperty("data").GetProperty("board").GetProperty("tags")[0].GetProperty("tagId").GetString()!;

        var planPath = Path.Combine(projectRoot, "tag-plan.json");
        File.WriteAllText(planPath, $$"""
        {
          "expectedBoardRevision": 4,
          "tagUpdates": [
            { "taskId": "T-0001", "expectedRevision": 1, "tagIds": ["{{tagId}}"] },
            { "taskId": "T-0002", "expectedRevision": 99, "tagIds": ["{{tagId}}"] }
          ]
        }
        """);
        var stale = RunCli(context, "tasks", "tag", "apply", "--input", planPath, "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, stale.ExitCode);
        var unchanged = RunCli(context, "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        using (var unchangedJson = JsonDocument.Parse(unchanged.Output))
        {
            Assert.All(unchangedJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray(), task =>
            {
                Assert.Equal(1, task.GetProperty("revision").GetInt64());
                Assert.Empty(task.GetProperty("labels").EnumerateArray());
            });
        }

        File.WriteAllText(planPath, $$"""
        {
          "expectedBoardRevision": 4,
          "tagUpdates": [
            { "taskId": "T-0001", "expectedRevision": 1, "tagIds": ["{{tagId}}"] },
            { "taskId": "T-0002", "expectedRevision": 1, "tagIds": ["{{tagId}}"] }
          ]
        }
        """);
        var applied = RunCli(context, "tasks", "tag", "apply", "--input", planPath, "--project", projectRoot, "--format", "json");
        Assert.Equal(0, applied.ExitCode);
        using var appliedJson = JsonDocument.Parse(applied.Output);
        Assert.Equal(2, appliedJson.RootElement.GetProperty("data").GetProperty("updatedCount").GetInt32());

        var board = RunCli(context, "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        using var boardJson = JsonDocument.Parse(board.Output);
        Assert.All(boardJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray(), task =>
        {
            Assert.Equal(2, task.GetProperty("revision").GetInt64());
            Assert.Equal(tagId, Assert.Single(task.GetProperty("labels").EnumerateArray()).GetString());
        });
    }

    [Fact]
    public void TasksAttachmentPreviewSelectsRasterCoverAndLosslessRemovalFailsClosed()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TaskPreview-");
        var sourceRoot = Path.Combine(projectRoot, "source");
        Directory.CreateDirectory(sourceRoot);
        var firstImage = Path.Combine(sourceRoot, "first.png");
        var textFile = Path.Combine(sourceRoot, "notes.txt");
        var secondImage = Path.Combine(sourceRoot, "second.jpg");
        File.WriteAllBytes(firstImage, [0x89, 0x50, 0x4e, 0x47]);
        File.WriteAllText(textFile, "not an image");
        File.WriteAllBytes(secondImage, [0xff, 0xd8, 0xff, 0xd9]);

        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--title", "Карточка с обложкой", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "attachment", "add", "T-0001", "--file", firstImage, "--expected-revision", "1", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "attachment", "add", "T-0001", "--file", textFile, "--expected-revision", "2", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "attachment", "add", "T-0001", "--file", secondImage, "--expected-revision", "3", "--project", projectRoot, "--format", "json").ExitCode);

        var invalid = RunCli(
            context,
            "tasks", "attachment", "set-preview", "T-0001",
            "--attachment", "A-0002",
            "--expected-revision", "4",
            "--project", projectRoot,
            "--format", "json");
        Assert.NotEqual(0, invalid.ExitCode);

        Assert.Equal(0, RunCli(
            context,
            "tasks", "attachment", "set-preview", "T-0001",
            "--attachment", "A-0003",
            "--expected-revision", "4",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        var selectedBoard = RunCli(context, "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        using (var selectedJson = JsonDocument.Parse(selectedBoard.Output))
        {
            var preview = Assert.Single(selectedJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray())
                .GetProperty("cardPreview");
            Assert.Equal("A-0003", preview.GetProperty("attachmentId").GetString());
            Assert.Equal("image/jpeg", preview.GetProperty("mediaType").GetString());
        }

        Assert.Equal(0, RunCli(
            context,
            "tasks", "attachment", "clear-preview", "T-0001",
            "--expected-revision", "5",
            "--project", projectRoot,
            "--format", "json").ExitCode);
        var automaticBoard = RunCli(context, "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        using (var automaticJson = JsonDocument.Parse(automaticBoard.Output))
        {
            var preview = Assert.Single(automaticJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray())
                .GetProperty("cardPreview");
            Assert.Equal("A-0001", preview.GetProperty("attachmentId").GetString());
        }

        Assert.Equal(0, RunCli(context, "tasks", "attachment", "set-preview", "T-0001", "--attachment", "A-0003", "--expected-revision", "6", "--project", projectRoot, "--format", "json").ExitCode);
        var remove = RunCli(context, "tasks", "attachment", "remove", "T-0001", "--attachment", "A-0003", "--expected-revision", "7", "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, remove.ExitCode);
        Assert.Contains("lossless", remove.Output, StringComparison.OrdinalIgnoreCase);

        var details = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using var detailsJson = JsonDocument.Parse(details.Output);
        Assert.Equal("A-0003", detailsJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("previewAttachmentId").GetString());
        Assert.Equal(3, detailsJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("attachments").GetArrayLength());
        var retainedBoard = RunCli(context, "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        using var retainedJson = JsonDocument.Parse(retainedBoard.Output);
        Assert.Equal("A-0003", Assert.Single(retainedJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray())
            .GetProperty("cardPreview").GetProperty("attachmentId").GetString());
    }

    [Fact]
    public void TasksNormalizeRewritesEscapedUnicodeWithoutChangingRevisionsAndIsIdempotent()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksNormalize-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "create",
            "--title", "Привести Unicode к UTF-8",
            "--project", projectRoot,
            "--format", "json").ExitCode);

        var taskPath = Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task");
        var escapedWord = string.Concat("Привести".Select(character => $"\\u{(int)character:X4}"));
        var legacyText = File.ReadAllText(taskPath)
            .Replace("Привести", escapedWord, StringComparison.Ordinal);
        File.WriteAllText(taskPath, legacyText);
        Assert.Contains("\\u041F", File.ReadAllText(taskPath), StringComparison.Ordinal);

        var normalized = RunCli(
            context,
            "tasks", "normalize",
            "--expected-board-revision", "2",
            "--project", projectRoot,
            "--format", "json");

        Assert.Equal(0, normalized.ExitCode);
        Assert.Contains("Привести", File.ReadAllText(taskPath), StringComparison.Ordinal);
        Assert.DoesNotContain("\\u041F", File.ReadAllText(taskPath), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"description\":", File.ReadAllText(taskPath), StringComparison.Ordinal);
        Assert.Contains("\"status\": \"Ready\"", File.ReadAllText(taskPath), StringComparison.Ordinal);
        using (var normalizedJson = JsonDocument.Parse(normalized.Output))
        {
            Assert.Contains(
                ".taskboard/tasks/T-0001.e2task",
                normalizedJson.RootElement.GetProperty("changedFiles").EnumerateArray().Select(item => item.GetString()));
        }

        using (var taskJson = JsonDocument.Parse(File.ReadAllText(taskPath)))
        {
            Assert.Equal(1, taskJson.RootElement.GetProperty("revision").GetInt64());
        }

        var repeated = RunCli(
            context,
            "tasks", "normalize",
            "--expected-board-revision", "2",
            "--project", projectRoot,
            "--format", "json");
        Assert.Equal(0, repeated.ExitCode);
        using var repeatedJson = JsonDocument.Parse(repeated.Output);
        Assert.Empty(repeatedJson.RootElement.GetProperty("changedFiles").EnumerateArray());
    }

    [Fact]
    public void TasksNormalizePreservesTypedCommandsAndLinksWithoutLogicalRevisionChanges()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksNormalizeLinks-");
        var inputRoot = CreateTemporaryDirectory("Electron2D-TasksNormalizeLinksInput-");
        var inputPath = Path.Combine(inputRoot, "task.json");
        File.WriteAllText(inputPath, """
        {
          "title": "Classify typed links",
          "expectedBoardRevision": 1,
          "links": [
            { "linkId": "link-doc", "kind": "File", "value": "docs/testing/harness.md" },
            { "linkId": "link-src", "kind": "Directory", "value": "src/Electron2D" }
          ],
          "executionContract": {
            "taskType": "verification",
            "readyToStart": [], "stopConditions": [], "allowedChanges": [],
            "forbiddenChanges": [], "requiredOutputs": [],
            "commands": [{
              "commandId": "command-docs", "kind": "Process", "executable": "dotnet",
              "arguments": ["run", "--project", "eng/Electron2D.Build", "--", "verify", "docs"],
              "workingDirectory": "eng", "platforms": ["Any"], "timeoutSeconds": 300,
              "expectedExitCodes": [0], "requiredAccess": "ReadOnly", "confirmation": "PolicyDecides"
            }],
            "externalAudit": "not-required"
          }
        }
        """);
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "create", "--input", inputPath, "--project", projectRoot, "--format", "json").ExitCode);

        var normalized = RunCli(
            context,
            "tasks", "normalize", "--expected-board-revision", "2",
            "--project", projectRoot, "--format", "json");

        Assert.Equal(0, normalized.ExitCode);
        var actual = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using var actualJson = JsonDocument.Parse(actual.Output);
        var actualTask = actualJson.RootElement.GetProperty("data").GetProperty("task");
        Assert.Equal(1, actualTask.GetProperty("revision").GetInt64());
        Assert.Equal(
            ["docs/testing/harness.md", "src/Electron2D/"],
            actualTask.GetProperty("linkedArtifacts").EnumerateArray().Select(item => item.GetString()!).ToArray());
        Assert.Single(actualTask.GetProperty("executionContract").GetProperty("commands").EnumerateArray());

        var repeated = RunCli(
            context,
            "tasks", "normalize", "--expected-board-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, repeated.ExitCode);
        using var repeatedJson = JsonDocument.Parse(repeated.Output);
        Assert.Empty(repeatedJson.RootElement.GetProperty("changedFiles").EnumerateArray());
    }

    [Fact]
    public void TasksNormalizeRejectsImpossibleLegacyLifecycleInV3()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksNormalizeAwaiting-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "create", "--title", "Legacy submitted task", "--project", projectRoot, "--format", "json").ExitCode);

        var taskPath = Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task");
        var legacyText = File.ReadAllText(taskPath)
            .Replace("\"status\": \"Ready\"", "\"status\": \"AwaitingAcceptance\"", StringComparison.Ordinal)
            .Replace("\"acceptanceState\": \"Open\"", "\"acceptanceState\": \"Open\"", StringComparison.Ordinal);
        File.WriteAllText(taskPath, legacyText);

        var normalized = RunCli(
            context,
            "tasks", "normalize", "--expected-board-revision", "2", "--project", projectRoot, "--format", "json");

        Assert.NotEqual(0, normalized.ExitCode);
        Assert.Contains("AwaitingAcceptance", File.ReadAllText(taskPath), StringComparison.Ordinal);
    }

    [Fact]
    public void TasksCompactBoardDerivesBlockedPlacementAndReturnsToReady()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksBoardStatus-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--title", "Dependency", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--title", "Dependent", "--project", projectRoot, "--format", "json").ExitCode);
        AddPassedCriterion(context, projectRoot, "T-0001", 1);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "dependency", "add", "T-0002", "--depends-on", "T-0001", "--expected-revision", "1",
            "--project", projectRoot, "--format", "json").ExitCode);

        var blocked = RunCli(
            context,
            "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, blocked.ExitCode);
        using (var blockedJson = JsonDocument.Parse(blocked.Output))
        {
            var card = blockedJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray()
                .Single(item => item.GetProperty("taskId").GetString() == "T-0002");
            Assert.Equal("Ready", card.GetProperty("status").GetString());
            Assert.Equal("Blocked", card.GetProperty("boardStatus").GetString());
            Assert.Equal("BlockedByDependencies", card.GetProperty("readiness").GetString());
        }

        Assert.Equal(0, RunCli(context, "tasks", "set-status", "T-0001", "--status", "InProgress", "--expected-revision", "4", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "set-status", "T-0001", "--status", "Review", "--expected-revision", "5", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "submit", "T-0001", "--expected-revision", "6", "--project", projectRoot, "--format", "json").ExitCode);
        const string capability = "VscHumanCapabilityBoardStatusabcdef0123456789";
        var acceptanceInput = JsonSerializer.Serialize(new
        {
            protocol = "Electron2D.TaskHumanDecision/1",
            capability,
            decision = "accept",
            reason = "Зависимость принята пользователем."
        });
        var humanContext = CliExecutionContext.ForTests(
            FixedInstant,
            input: new StringReader(acceptanceInput),
            humanCapability: capability,
            humanActorId: "user-board-status");
        Assert.Equal(0, RunCli(
            humanContext, "tasks", "__human-decision", "T-0001", "--expected-revision", "7",
            "--project", projectRoot, "--format", "json").ExitCode);

        var ready = RunCli(
            context,
            "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, ready.ExitCode);
        using (var readyJson = JsonDocument.Parse(ready.Output))
        {
            var card = readyJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray()
                .Single(item => item.GetProperty("taskId").GetString() == "T-0002");
            Assert.Equal("Ready", card.GetProperty("boardStatus").GetString());
            Assert.Equal("Ready", card.GetProperty("readiness").GetString());
        }

        Assert.Equal(0, RunCli(
            context, "tasks", "set-status", "T-0002", "--status", "Blocked",
            "--reason", "Ручной блокер.", "--expected-revision", "2",
            "--project", projectRoot, "--format", "json").ExitCode);

        var manual = RunCli(
            context,
            "tasks", "board", "--compact", "true", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, manual.ExitCode);
        using var manualJson = JsonDocument.Parse(manual.Output);
        var manualCard = manualJson.RootElement.GetProperty("data").GetProperty("tasks").EnumerateArray()
            .Single(item => item.GetProperty("taskId").GetString() == "T-0002");
        Assert.Equal("Blocked", manualCard.GetProperty("status").GetString());
        Assert.Equal("Blocked", manualCard.GetProperty("boardStatus").GetString());
        Assert.Equal("Ready", manualCard.GetProperty("readiness").GetString());
    }

    [Fact]
    public void TasksCriterionCommandsProvideRevisionAwareCrudAndExplicitStateChanges()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksCriterion-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context,
            "tasks", "create", "--title", "Criterion lifecycle",
            "--project", projectRoot, "--format", "json").ExitCode);

        var added = RunCli(
            context,
            "tasks", "criterion", "add", "T-0001",
            "--criterion", "criterion-001",
            "--description", "Focused tests pass",
            "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, added.ExitCode);
        using (var addedJson = JsonDocument.Parse(added.Output))
        {
            var task = addedJson.RootElement.GetProperty("data").GetProperty("task");
            Assert.Equal(2, task.GetProperty("revision").GetInt64());
            var criterion = Assert.Single(task.GetProperty("acceptanceCriteria").EnumerateArray());
            Assert.Equal("criterion-001", criterion.GetProperty("criterionId").GetString());
            Assert.Equal("Focused tests pass", criterion.GetProperty("description").GetString());
            Assert.Equal("Open", criterion.GetProperty("state").GetString());
        }

        var duplicate = RunCli(
            context,
            "tasks", "criterion", "add", "T-0001",
            "--criterion", "criterion-001",
            "--description", "Duplicate",
            "--expected-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(1, duplicate.ExitCode);

        var afterStatus = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using (var afterStatusJson = JsonDocument.Parse(afterStatus.Output))
        {
            var criterion = Assert.Single(afterStatusJson.RootElement.GetProperty("data").GetProperty("task")
                .GetProperty("acceptanceCriteria").EnumerateArray());
            Assert.Equal("Open", criterion.GetProperty("state").GetString());
        }

        var updated = RunCli(
            context,
            "tasks", "criterion", "update", "T-0001",
            "--criterion", "criterion-001",
            "--description", "Focused tests and docs pass",
            "--expected-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, updated.ExitCode);

        var evidence = RunCli(
            context,
            "tasks", "criterion", "add-evidence", "T-0001",
            "--criterion", "criterion-001", "--kind", "File", "--value", "tests/focused.log",
            "--expected-revision", "3",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, evidence.ExitCode);

        var passed = RunCli(
            context,
            "tasks", "criterion", "set-state", "T-0001",
            "--criterion", "criterion-001", "--state", "Passed",
            "--expected-revision", "4",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, passed.ExitCode);
        using (var passedJson = JsonDocument.Parse(passed.Output))
        {
            var task = passedJson.RootElement.GetProperty("data").GetProperty("task");
            Assert.Equal(5, task.GetProperty("revision").GetInt64());
            var criterion = Assert.Single(task.GetProperty("acceptanceCriteria").EnumerateArray());
            Assert.Equal("Focused tests and docs pass", criterion.GetProperty("description").GetString());
            Assert.Equal("Passed", criterion.GetProperty("state").GetString());
            Assert.Equal("tests/focused.log", Assert.Single(criterion.GetProperty("evidenceLinks").EnumerateArray()).GetString());
        }

        var dryRun = RunCli(
            context,
            "tasks", "criterion", "set-state", "T-0001",
            "--criterion", "criterion-001", "--state", "Failed",
            "--expected-revision", "5", "--dry-run",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, dryRun.ExitCode);
        using (var dryRunJson = JsonDocument.Parse(dryRun.Output))
        {
            Assert.Empty(dryRunJson.RootElement.GetProperty("changedFiles").EnumerateArray());
            var criterion = Assert.Single(dryRunJson.RootElement.GetProperty("data").GetProperty("task")
                .GetProperty("acceptanceCriteria").EnumerateArray());
            Assert.Equal("Failed", criterion.GetProperty("state").GetString());
        }

        var afterDryRun = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using (var afterDryRunJson = JsonDocument.Parse(afterDryRun.Output))
        {
            var task = afterDryRunJson.RootElement.GetProperty("data").GetProperty("task");
            Assert.Equal(5, task.GetProperty("revision").GetInt64());
            var criterion = Assert.Single(task.GetProperty("acceptanceCriteria").EnumerateArray());
            Assert.Equal("Passed", criterion.GetProperty("state").GetString());
        }

        var removed = RunCli(
            context,
            "tasks", "criterion", "remove", "T-0001",
            "--criterion", "criterion-001", "--expected-revision", "5",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, removed.ExitCode);
        using (var removedJson = JsonDocument.Parse(removed.Output))
        {
            var task = removedJson.RootElement.GetProperty("data").GetProperty("task");
            Assert.Equal(6, task.GetProperty("revision").GetInt64());
            Assert.Empty(task.GetProperty("acceptanceCriteria").EnumerateArray());
        }

        var missing = RunCli(
            context,
            "tasks", "criterion", "set-state", "T-0001",
            "--criterion", "criterion-missing", "--state", "Passed", "--expected-revision", "6",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(1, missing.ExitCode);
        var invalidState = RunCli(
            context,
            "tasks", "criterion", "set-state", "T-0001",
            "--criterion", "criterion-missing", "--state", "Unknown", "--expected-revision", "6",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(1, invalidState.ExitCode);
    }

    [Fact]
    public void TasksCliCreatesEpochMilestoneAndMovesBoardPlacement()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksGroups-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "First", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Second", "--format", "json").ExitCode);

        Assert.Equal(0, RunCli(
            context, "tasks", "group", "add",
            "--kind", "Epoch", "--title", "Epoch One", "--expected-board-revision", "3",
            "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "group", "add",
            "--kind", "Milestone", "--title", "Milestone One", "--parent", "G-0001",
            "--expected-board-revision", "4", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "move", "T-0002",
            "--group", "G-0002", "--rank", "00000500", "--expected-board-revision", "5",
            "--project", projectRoot, "--format", "json").ExitCode);

        var board = RunCli(context, "tasks", "board", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, board.ExitCode);
        using var boardJson = JsonDocument.Parse(board.Output);
        var snapshot = boardJson.RootElement.GetProperty("data").GetProperty("board");
        Assert.Equal(6, snapshot.GetProperty("revision").GetInt64());
        Assert.Equal(2, snapshot.GetProperty("groups").GetArrayLength());
        var milestone = snapshot.GetProperty("groups").EnumerateArray().Single(group => group.GetProperty("groupId").GetString() == "G-0002");
        Assert.Equal("G-0001", milestone.GetProperty("parentGroupId").GetString());
        var placement = snapshot.GetProperty("placements").EnumerateArray().Single(item => item.GetProperty("taskId").GetString() == "T-0002");
        Assert.Equal("G-0002", placement.GetProperty("groupId").GetString());
        Assert.Equal("000000000500", placement.GetProperty("rank").GetString());
    }

    [Fact]
    public void TasksCliClearsParentAndUpdatesAndRemovesEmptyGroups()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksGroupLifecycle-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Parent", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Child", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "parent", "set", "T-0002", "--parent", "T-0001",
            "--expected-revision", "1", "--expected-parent-revision", "1",
            "--project", projectRoot, "--format", "json").ExitCode);

        var cleared = RunCli(
            context, "tasks", "parent", "clear", "T-0002",
            "--expected-revision", "2", "--expected-parent-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, cleared.ExitCode);
        using (var childJson = JsonDocument.Parse(cleared.Output))
        {
            Assert.Null(childJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("parentTaskId").GetString());
        }
        var parent = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using (var parentJson = JsonDocument.Parse(parent.Output))
        {
            Assert.Empty(parentJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("subtasks").EnumerateArray());
        }

        Assert.Equal(0, RunCli(
            context, "tasks", "group", "add", "--kind", "Epoch", "--title", "Epoch",
            "--expected-board-revision", "3", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "group", "add", "--kind", "Milestone", "--title", "Milestone", "--parent", "G-0001",
            "--expected-board-revision", "4", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "move", "T-0002", "--group", "G-0002", "--rank", "00000500",
            "--expected-board-revision", "5", "--project", projectRoot, "--format", "json").ExitCode);

        var updated = RunCli(
            context, "tasks", "group", "update", "G-0002", "--title", "Milestone updated", "--rank", "00000750",
            "--expected-board-revision", "6", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, updated.ExitCode);
        using (var updatedJson = JsonDocument.Parse(updated.Output))
        {
            var group = updatedJson.RootElement.GetProperty("data").GetProperty("board").GetProperty("groups")
                .EnumerateArray().Single(item => item.GetProperty("groupId").GetString() == "G-0002");
            Assert.Equal("Milestone updated", group.GetProperty("title").GetString());
            Assert.Equal("000000000750", group.GetProperty("rank").GetString());
        }

        var nonEmpty = RunCli(
            context, "tasks", "group", "remove", "G-0002", "--expected-board-revision", "7",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, nonEmpty.ExitCode);
        Assert.Contains("not empty", nonEmpty.Output, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(0, RunCli(
            context, "tasks", "move", "T-0002", "--rank", "00000500", "--expected-board-revision", "7",
            "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "group", "remove", "G-0002", "--expected-board-revision", "8",
            "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "group", "remove", "G-0001", "--expected-board-revision", "9",
            "--project", projectRoot, "--format", "json").ExitCode);
    }

    [Fact]
    public void TasksCliParentSetMaintainsSubtasksAndRejectsContainmentCycle()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksParent-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Parent", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Child", "--format", "json").ExitCode);

        var set = RunCli(
            context, "tasks", "parent", "set", "T-0002",
            "--parent", "T-0001", "--expected-revision", "1", "--expected-parent-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, set.ExitCode);

        var parent = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using (var parentJson = JsonDocument.Parse(parent.Output))
        {
            Assert.Contains("T-0002", parentJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("subtasks").EnumerateArray().Select(item => item.GetString()));
        }

        var cycle = RunCli(
            context, "tasks", "parent", "set", "T-0001",
            "--parent", "T-0002", "--expected-revision", "1", "--expected-parent-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, cycle.ExitCode);
        Assert.Contains("cycle", cycle.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TasksCliDependencyRemoveUpdatesTaskWithExpectedRevision()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksDependencyRemove-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Dependency", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Dependent", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "dependency", "add", "T-0002", "--depends-on", "T-0001",
            "--expected-revision", "1", "--project", projectRoot, "--format", "json").ExitCode);

        var remove = RunCli(
            context, "tasks", "dependency", "remove", "T-0002", "--depends-on", "T-0001",
            "--expected-revision", "2", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, remove.ExitCode);
        using var removeJson = JsonDocument.Parse(remove.Output);
        Assert.Empty(removeJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("dependencies").EnumerateArray());
        Assert.Equal(3, removeJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("revision").GetInt64());
    }

    [Fact]
    public void TasksCliAttachmentAddRetrievesVerifiedBlobAndRejectsLossyRemoval()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksAttachment-");
        var sourceRoot = CreateTemporaryDirectory("Electron2D-TasksAttachmentSource-");
        var sourcePath = Path.Combine(sourceRoot, "evidence.png");
        var content = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 1, 2, 3 };
        File.WriteAllBytes(sourcePath, content);
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Attachment owner", "--format", "json").ExitCode);

        var add = RunCli(
            context, "tasks", "attachment", "add", "T-0001",
            "--file", sourcePath, "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, add.ExitCode);
        using var addJson = JsonDocument.Parse(add.Output);
        var attachment = Assert.Single(addJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("attachments").EnumerateArray());
        Assert.Equal("A-0001", attachment.GetProperty("attachmentId").GetString());
        Assert.Equal("image/png", attachment.GetProperty("mediaType").GetString());
        Assert.Equal(content.Length, attachment.GetProperty("byteLength").GetInt64());
        Assert.Equal(
            ["ExtractedText:Pending", "Ocr:Pending", "Preview:Pending"],
            attachment.GetProperty("derivatives").EnumerateArray()
                .Select(item => $"{item.GetProperty("kind").GetString()}:{item.GetProperty("status").GetString()}")
                .Order(StringComparer.Ordinal)
                .ToArray());
        var relativePath = attachment.GetProperty("relativePath").GetString()!;
        var ownerUid = addJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("taskUid").GetString();
        var contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant();
        Assert.Equal($".taskboard/attachments/{ownerUid}/A-0001/{contentHash}.png", relativePath);
        Assert.Equal(content, File.ReadAllBytes(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))));

        var read = RunCli(
            context, "tasks", "attachment", "read", "T-0001",
            "--attachment", "A-0001", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, read.ExitCode);
        using (var readJson = JsonDocument.Parse(read.Output))
        {
            var retrieval = readJson.RootElement.GetProperty("data").GetProperty("retrieval");
            Assert.Equal("A-0001", retrieval.GetProperty("attachmentId").GetString());
            Assert.Equal(JsonValueKind.Null, retrieval.GetProperty("derivativeId").ValueKind);
            Assert.Equal("image/png", retrieval.GetProperty("mediaType").GetString());
            Assert.Equal(content.Length, retrieval.GetProperty("byteLength").GetInt64());
            Assert.Equal(contentHash, retrieval.GetProperty("sha256").GetString());
            Assert.Equal(Convert.ToBase64String(content), retrieval.GetProperty("contentBase64").GetString());
        }

        var remove = RunCli(
            context, "tasks", "attachment", "remove", "T-0001",
            "--attachment", "A-0001", "--expected-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, remove.ExitCode);
        Assert.Contains("lossless", remove.Output, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))));

        var get = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        using var getJson = JsonDocument.Parse(get.Output);
        Assert.Single(getJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("attachments").EnumerateArray());
        Assert.Equal(2, getJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("revision").GetInt64());
    }

    [Fact]
    public void TasksCliAttachmentAddRejectsFileLargerThanDefaultLimit()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksAttachmentLimit-");
        var sourcePath = Path.Combine(CreateTemporaryDirectory("Electron2D-TasksAttachmentLimitSource-"), "too-large.bin");
        using (var stream = File.Create(sourcePath))
        {
            stream.SetLength((25L * 1024 * 1024) + 1);
        }

        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Attachment owner", "--format", "json").ExitCode);

        var add = RunCli(
            context, "tasks", "attachment", "add", "T-0001",
            "--file", sourcePath, "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");

        Assert.NotEqual(0, add.ExitCode);
        Assert.Contains("per-file limit", add.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(projectRoot, ".taskboard", "attachments"), "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void TasksCliDeleteRequiresExactConfirmationAndRejectsIncomingDependency()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksDelete-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        Assert.Equal(0, RunCli(context, "tasks", "init", "--project", projectRoot, "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Target", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(context, "tasks", "create", "--project", projectRoot, "--title", "Dependent", "--format", "json").ExitCode);
        Assert.Equal(0, RunCli(
            context, "tasks", "dependency", "add", "T-0002", "--depends-on", "T-0001",
            "--expected-revision", "1", "--project", projectRoot, "--format", "json").ExitCode);

        var wrongConfirmation = RunCli(
            context, "tasks", "delete", "T-0001", "--confirm", "wrong",
            "--expected-revision", "1", "--expected-board-revision", "3",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, wrongConfirmation.ExitCode);
        Assert.Contains("exact", wrongConfirmation.Output, StringComparison.OrdinalIgnoreCase);

        var referenced = RunCli(
            context, "tasks", "delete", "T-0001", "--confirm", "T-0001",
            "--expected-revision", "1", "--expected-board-revision", "3",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, referenced.ExitCode);
        Assert.Contains("incoming dependency", referenced.Output, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(0, RunCli(
            context, "tasks", "dependency", "remove", "T-0002", "--depends-on", "T-0001",
            "--expected-revision", "2", "--project", projectRoot, "--format", "json").ExitCode);
        var deleted = RunCli(
            context, "tasks", "delete", "T-0001", "--confirm", "T-0001",
            "--expected-revision", "1", "--expected-board-revision", "3",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, deleted.ExitCode);
        Assert.False(File.Exists(Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task")));

        var board = RunCli(context, "tasks", "board", "--project", projectRoot, "--format", "json");
        using var boardJson = JsonDocument.Parse(board.Output);
        var placement = Assert.Single(boardJson.RootElement.GetProperty("data").GetProperty("board").GetProperty("placements").EnumerateArray());
        Assert.Equal("T-0002", placement.GetProperty("taskId").GetString());
    }

    [Fact]
    public void TasksMigrateDryRunRejectsFinalizedCanonicalRepository()
    {
        var projectRoot = FindRepositoryRootForTasks();
        Assert.True(Directory.Exists(Path.Combine(projectRoot, ".taskboard")));

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "tasks", "migrate", "--dry-run", "--project", projectRoot, "--format", "json");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("E2D-CLI-0002", result.Output, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")));
    }

    [Fact]
    public void TasksMigrateToV3RequiresReviewedReportAndExpectedBoardRevision()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksMigrateV3-");
        var context = CliExecutionContext.ForTests(FixedInstant);
        WriteV2Taskboard(projectRoot, FixedInstant, "Перенести задачу");

        var dryRun = RunCli(
            context,
            "tasks", "migrate", "--to-version", "3", "--dry-run",
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, dryRun.ExitCode);
        using var dryJson = JsonDocument.Parse(dryRun.Output);
        var report = dryJson.RootElement.GetProperty("data").GetProperty("report");
        Assert.Equal(2, report.GetProperty("sourceVersion").GetInt32());
        Assert.Equal(3, report.GetProperty("targetVersion").GetInt32());
        var reportSha = report.GetProperty("reportSha256").GetString()!;
        var sourceRevision = report.GetProperty("sourceBoardRevision").GetInt64();

        var missingCas = RunCli(
            context,
            "tasks", "migrate", "--to-version", "3", "--apply", "true", "--report-sha", reportSha,
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, missingCas.ExitCode);

        var applied = RunCli(
            context,
            "tasks", "migrate", "--to-version", "3", "--apply", "true", "--report-sha", reportSha,
            "--expected-board-revision", sourceRevision.ToString(CultureInfo.InvariantCulture),
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, applied.ExitCode);
        using var board = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")));
        Assert.Equal(3, board.RootElement.GetProperty("version").GetInt32());

        var boardRead = RunCli(context, "tasks", "board", "--project", projectRoot, "--format", "json");
        var listRead = RunCli(context, "tasks", "list", "--project", projectRoot, "--format", "json");
        var getRead = RunCli(context, "tasks", "get", "T-0001", "--project", projectRoot, "--format", "json");
        var verify = RunCli(context, "tasks", "verify", "--project", projectRoot, "--format", "json");
        Assert.Equal(0, boardRead.ExitCode);
        Assert.Equal(0, listRead.ExitCode);
        Assert.Equal(0, getRead.ExitCode);
        Assert.Equal(0, verify.ExitCode);
        using var boardReadJson = JsonDocument.Parse(boardRead.Output);
        Assert.Equal(3, boardReadJson.RootElement.GetProperty("data").GetProperty("board").GetProperty("version").GetInt32());
        Assert.Equal(
            "T-0001",
            boardReadJson.RootElement.GetProperty("data").GetProperty("tasks")[0].GetProperty("taskId").GetString());

        var created = RunCliExact(
            context,
            "tasks", "create", "--title", "Новая задача v3",
            "--project", projectRoot, "--format", "json");
        Assert.True(created.ExitCode == 0, created.Output);
        using var createdJson = JsonDocument.Parse(created.Output);
        Assert.Equal("T-0002", createdJson.RootElement.GetProperty("data").GetProperty("task").GetProperty("taskId").GetString());

        var updated = RunCli(
            context,
            "tasks", "update", "T-0002", "--description", "Уточнённое описание", "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.True(updated.ExitCode == 0, updated.Output);
        var commented = RunCli(
            context,
            "tasks", "comment", "add", "T-0002", "--text", "**Markdown** комментарий", "--expected-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.True(commented.ExitCode == 0, commented.Output);

        var criterionAdded = RunCli(
            context,
            "tasks", "criterion", "add", "T-0002", "--criterion", "criterion-v3", "--description", "Результат проверен", "--expected-revision", "3",
            "--project", projectRoot, "--format", "json");
        Assert.True(criterionAdded.ExitCode == 0, criterionAdded.Output);
        var criterionEvidence = RunCli(
            context,
            "tasks", "criterion", "add-evidence", "T-0002", "--criterion", "criterion-v3", "--kind", "File", "--value", "docs/migration-result.md", "--expected-revision", "4",
            "--project", projectRoot, "--format", "json");
        Assert.True(criterionEvidence.ExitCode == 0, criterionEvidence.Output);
        var criterionPassed = RunCli(
            context,
            "tasks", "criterion", "set-state", "T-0002", "--criterion", "criterion-v3", "--state", "Passed", "--expected-revision", "5",
            "--project", projectRoot, "--format", "json");
        Assert.True(criterionPassed.ExitCode == 0, criterionPassed.Output);

        var third = RunCli(
            context,
            "tasks", "create", "--title", "Зависимая задача", "--expected-board-revision", (sourceRevision + 1).ToString(CultureInfo.InvariantCulture),
            "--project", projectRoot, "--format", "json");
        Assert.True(third.ExitCode == 0, third.Output);
        var dependency = RunCli(
            context,
            "tasks", "dependency", "add", "T-0003", "--depends-on", "T-0002", "--expected-revision", "1",
            "--project", projectRoot, "--format", "json");
        Assert.True(dependency.ExitCode == 0, dependency.Output);
        var parent = RunCli(
            context,
            "tasks", "parent", "set", "T-0003", "--parent", "T-0002", "--expected-revision", "2",
            "--project", projectRoot, "--format", "json");
        Assert.True(parent.ExitCode == 0, parent.Output);

        var inProgress = RunCli(
            context,
            "tasks", "set-status", "T-0002", "--status", "InProgress", "--expected-revision", "6",
            "--project", projectRoot, "--format", "json");
        Assert.True(inProgress.ExitCode == 0, inProgress.Output);
        var review = RunCli(
            context,
            "tasks", "set-status", "T-0002", "--status", "Review", "--expected-revision", "7",
            "--project", projectRoot, "--format", "json");
        Assert.True(review.ExitCode == 0, review.Output);
        var submitted = RunCli(
            context,
            "tasks", "submit", "T-0002", "--expected-revision", "8",
            "--project", projectRoot, "--format", "json");
        Assert.True(submitted.ExitCode == 0, submitted.Output);

        var imagePath = Path.Combine(projectRoot, "preview.png");
        File.WriteAllBytes(imagePath, [0x89, 0x50, 0x4e, 0x47]);
        var attached = RunCli(
            context,
            "tasks", "attachment", "add", "T-0002", "--file", imagePath, "--expected-revision", "9",
            "--project", projectRoot, "--format", "json");
        Assert.True(attached.ExitCode == 0, attached.Output);
        using var attachedJson = JsonDocument.Parse(attached.Output);
        var attachmentId = attachedJson.RootElement.GetProperty("data").GetProperty("task")
            .GetProperty("attachments")[0].GetProperty("attachmentId").GetString()!;
        var preview = RunCli(
            context,
            "tasks", "attachment", "set-preview", "T-0002", "--attachment", attachmentId, "--expected-revision", "10",
            "--project", projectRoot, "--format", "json");
        Assert.True(preview.ExitCode == 0, preview.Output);
        var removedAttachment = RunCli(
            context,
            "tasks", "attachment", "remove", "T-0002", "--attachment", attachmentId, "--expected-revision", "11",
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, removedAttachment.ExitCode);
        Assert.Contains("lossless", removedAttachment.Output, StringComparison.OrdinalIgnoreCase);

        var cancelled = RunCli(
            context,
            "tasks", "cancel", "T-0003", "--reason", "Больше не требуется", "--expected-revision", "3",
            "--project", projectRoot, "--format", "json");
        Assert.True(cancelled.ExitCode == 0, cancelled.Output);
        var archived = RunCli(
            context,
            "tasks", "archive", "T-0003", "--expected-revision", "4", "--expected-board-revision", (sourceRevision + 2).ToString(CultureInfo.InvariantCulture),
            "--project", projectRoot, "--format", "json");
        Assert.True(archived.ExitCode == 0, archived.Output);
        var unarchived = RunCli(
            context,
            "tasks", "unarchive", "T-0003", "--expected-revision", "5", "--expected-board-revision", (sourceRevision + 3).ToString(CultureInfo.InvariantCulture),
            "--project", projectRoot, "--format", "json");
        Assert.True(unarchived.ExitCode == 0, unarchived.Output);
        var reopened = RunCli(
            context,
            "tasks", "reopen", "T-0003", "--expected-revision", "6",
            "--project", projectRoot, "--format", "json");
        Assert.True(reopened.ExitCode == 0, reopened.Output);
        Assert.Equal(0, RunCli(context, "tasks", "verify", "--project", projectRoot, "--format", "json").ExitCode);

        var finalized = RunCli(
            context,
            "tasks", "migrate", "--to-version", "3", "--finalize", "true",
            "--report-sha", reportSha,
            "--expected-board-revision", (sourceRevision + 4).ToString(CultureInfo.InvariantCulture),
            "--project", projectRoot, "--format", "json");
        Assert.True(finalized.ExitCode == 0, finalized.Output);
        using var finalizedBoard = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")));
        Assert.True(finalizedBoard.RootElement.GetProperty("migration").GetProperty("finalized").GetBoolean());
        var migrationRoot = Path.Combine(projectRoot, ".taskboard", ".migration", "v2");
        Assert.True(File.Exists(Path.Combine(migrationRoot, "report.json")));
        Assert.False(File.Exists(Path.Combine(migrationRoot, "board.e2tasks")));
        Assert.Equal(0, RunCli(context, "tasks", "verify", "--project", projectRoot, "--format", "json").ExitCode);
    }

    [Fact]
    public void TasksMigrateApplyRequiresMatchingReportDigestAndIsIdempotent()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksMigrateApply-");
        Directory.CreateDirectory(Path.Combine(projectRoot, "data", "completed-tasks", "2026"));
        File.WriteAllText(
            Path.Combine(projectRoot, "TASKS.md"),
            "## T-0001 [ ] P1: Active\n" +
            "- Создана: 2026-07-01T10:00:00+03:00\n" +
            "- Состояние: open\n" +
            "- Зависимости: нет\n\n" +
            "## ROADMAP\n\n" +
            "### 1. Epoch\n\n" +
            "- `T-0001` - Active\n");
        File.WriteAllText(
            Path.Combine(projectRoot, "data", "completed-tasks", "2026", "06 Июнь.md"),
            "# June archive\n\n# T-0002: Done\n- Завершена: 2026-07-02T10:00:00+03:00\n");
        File.WriteAllText(
            Path.Combine(projectRoot, "data", "completed-tasks", "2026", "07 Июль.md"),
            "# July archive\n");
        var context = CliExecutionContext.ForTests(FixedInstant);
        var dryRun = RunCli(context, "tasks", "migrate", "--dry-run", "--project", projectRoot, "--format", "json");
        using var dryJson = JsonDocument.Parse(dryRun.Output);
        var reportSha = dryJson.RootElement.GetProperty("data").GetProperty("report").GetProperty("reportSha256").GetString()!;

        var wrongDigest = RunCli(
            context, "tasks", "migrate", "--apply", "true", "--report-sha", new string('0', 64),
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, wrongDigest.ExitCode);
        Assert.False(Directory.Exists(Path.Combine(projectRoot, ".taskboard")));

        var applied = RunCli(
            context, "tasks", "migrate", "--apply", "true", "--report-sha", reportSha,
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, applied.ExitCode);
        Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", "completed", "T-0002.e2task")));

        var repeated = RunCli(
            context, "tasks", "migrate", "--apply", "true", "--report-sha", reportSha,
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, repeated.ExitCode);
        using var repeatedJson = JsonDocument.Parse(repeated.Output);
        Assert.Empty(repeatedJson.RootElement.GetProperty("changedFiles").EnumerateArray());
    }

    [Fact]
    public void TasksMigrateFinalizeRejectsCanonicalDriftBeforeDeletingLegacySources()
    {
        var projectRoot = CreateTemporaryDirectory("Electron2D-TasksMigrateFinalize-");
        Directory.CreateDirectory(Path.Combine(projectRoot, "data", "completed-tasks", "2026"));
        var tasksPath = Path.Combine(projectRoot, "TASKS.md");
        var junePath = Path.Combine(projectRoot, "data", "completed-tasks", "2026", "06 Июнь.md");
        var julyPath = Path.Combine(projectRoot, "data", "completed-tasks", "2026", "07 Июль.md");
        File.WriteAllText(tasksPath, "## T-0001 [ ] P1: Active\n- Создана: 2026-07-01T10:00:00+03:00\n- Состояние: open\n- Зависимости: нет\n\n## ROADMAP\n\n### 1. Epoch\n\n- `T-0001` - Active\n");
        File.WriteAllText(junePath, "# June archive\n\n# T-0002: Done\n- Завершена: 2026-07-02T10:00:00+03:00\n");
        File.WriteAllText(julyPath, "# July archive\n");
        var context = CliExecutionContext.ForTests(FixedInstant);
        var dryRun = RunCli(context, "tasks", "migrate", "--dry-run", "--project", projectRoot, "--format", "json");
        using var dryJson = JsonDocument.Parse(dryRun.Output);
        var reportSha = dryJson.RootElement.GetProperty("data").GetProperty("report").GetProperty("reportSha256").GetString()!;
        Assert.Equal(0, RunCli(
            context, "tasks", "migrate", "--apply", "true", "--report-sha", reportSha,
            "--project", projectRoot, "--format", "json").ExitCode);
        var canonicalTaskPath = Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task");
        var canonicalTaskText = File.ReadAllText(canonicalTaskPath);
        File.WriteAllText(canonicalTaskPath, canonicalTaskText.Replace("\"title\": \"Active\"", "\"title\": \"Drifted\"", StringComparison.Ordinal));

        var rejected = RunCli(
            context, "tasks", "migrate", "--finalize", "true", "--report-sha", reportSha,
            "--project", projectRoot, "--format", "json");
        Assert.NotEqual(0, rejected.ExitCode);
        Assert.True(File.Exists(tasksPath));
        Assert.True(File.Exists(junePath));
        Assert.True(File.Exists(julyPath));

        File.WriteAllText(canonicalTaskPath, canonicalTaskText);
        var finalized = RunCli(
            context, "tasks", "migrate", "--finalize", "true", "--report-sha", reportSha,
            "--project", projectRoot, "--format", "json");
        Assert.Equal(0, finalized.ExitCode);
        Assert.False(File.Exists(tasksPath));
        Assert.False(File.Exists(junePath));
        Assert.False(File.Exists(julyPath));
        using var board = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")));
        Assert.True(board.RootElement.GetProperty("migration").GetProperty("finalized").GetBoolean());
    }

    [Fact]
    public void TasksExportWritesStableMarkdownReportWithoutCreatingWorkflowFiles()
    {
        var projectRoot = CreateProjectRoot("tasks-export-markdown", SceneText(speed: 10));
        WriteTaskDocuments(
            projectRoot,
            CreateReportTask(
                "task-alpha",
                "Ship alpha feature",
                ProjectTaskStatus.Done,
                rank: "0200",
                completedAt: FixedInstant.AddHours(1)),
            CreateReportTask(
                "task-beta",
                "Ship beta feature",
                ProjectTaskStatus.Done,
                rank: "0100",
                completedAt: FixedInstant.AddHours(2)),
            CreateReportTask(
                "task-ready",
                "Prepare ready feature",
                ProjectTaskStatus.Ready,
                rank: "0300",
                completedAt: null));

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "tasks",
            "export",
            "--project",
            projectRoot,
            "--status",
            "done",
            "--milestone",
            "preview",
            "--version",
            "0.1-preview",
            "--epic",
            "editor",
            "--assignee",
            "agent-1",
            "--agent-session",
            "agent-session-1",
            "--format",
            "markdown");

        const string expected = """
        # Project Tasks Report

        > Markdown report only. Canonical task storage stays in `.taskboard/tasks/*.e2task` and `.taskboard/board.e2tasks`.

        - Source: `.taskboard/tasks/*.e2task`
        - Filters: status=Done, milestone=preview, version=0.1-preview, epic=editor, assignee=agent-1, agent-session=agent-session-1
        - Task count: 2

        ## Done

        ### task-beta - Ship beta feature

        - Status: Done
        - Priority: P0
        - Assignee: agent-1
        - Labels: agent-session:agent-session-1, epic:editor, milestone:preview, version:0.1-preview
        - Created: 2026-06-22T12:00:00.0000000+00:00
        - Completed: 2026-06-22T14:00:00.0000000+00:00
        - Accepted: 2026-06-22T14:00:00.0000000+00:00 by user-1
        - Criteria:
          - [x] criterion-task-beta: Golden output is stable.
        - Activity:
          - 2026-06-22T13:30:00.0000000+00:00 Agent agent-1: TestResult - AgentSessionId=agent-session-1; focused tests green.

        ### task-alpha - Ship alpha feature

        - Status: Done
        - Priority: P0
        - Assignee: agent-1
        - Labels: agent-session:agent-session-1, epic:editor, milestone:preview, version:0.1-preview
        - Created: 2026-06-22T12:00:00.0000000+00:00
        - Completed: 2026-06-22T13:00:00.0000000+00:00
        - Accepted: 2026-06-22T13:00:00.0000000+00:00 by user-1
        - Criteria:
          - [x] criterion-task-alpha: Golden output is stable.
        - Activity:
          - 2026-06-22T12:30:00.0000000+00:00 Agent agent-1: TestResult - AgentSessionId=agent-session-1; focused tests green.

        """;

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.Equal(expected.ReplaceLineEndings(Environment.NewLine), result.Output);
        Assert.False(File.Exists(Path.Combine(projectRoot, "TASKS.md")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "completed-tasks")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "dev-diary")));
    }

    [Fact]
    public void ContextBuildCreatesCompactSnapshotWithoutSecretsOrBinaryPayloads()
    {
        var projectRoot = CreateContextProjectRoot("context-build");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "context",
            "build",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);

        using var envelopeJson = JsonDocument.Parse(result.Output);
        var envelope = envelopeJson.RootElement;
        Assert.Equal("context build", envelope.GetProperty("command").GetString());
        Assert.True(envelope.GetProperty("succeeded").GetBoolean());
        Assert.Equal("none", envelope.GetProperty("route").GetString());

        var contextRoot = Path.Combine(projectRoot, ".electron2d", "context");
        var expectedFiles = new[]
        {
            "context-manifest.json",
            "project-summary.json",
            "api-surface.json",
            "godot-differences.json",
            "scene-index.json",
            "resource-graph.json",
            "diagnostics.json",
            "conventions.md"
        };
        foreach (var file in expectedFiles)
        {
            Assert.True(File.Exists(Path.Combine(contextRoot, file)), file);
            Assert.Contains(
                ".electron2d/context/" + file,
                envelope.GetProperty("changedFiles").EnumerateArray().Select(item => item.GetString()));
        }

        var data = envelope.GetProperty("data");
        Assert.Equal("context.build", data.GetProperty("mode").GetString());
        Assert.Equal(".electron2d/context", data.GetProperty("outputPath").GetString());
        Assert.Contains("snapshot", data.GetProperty("snapshotWarning").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(expectedFiles.Length, data.GetProperty("files").GetArrayLength());
        Assert.InRange(data.GetProperty("totalBytes").GetInt64(), 1, 64 * 1024);

        using var summaryJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "project-summary.json")));
        var summary = summaryJson.RootElement;
        Assert.Equal("ContextGame", summary.GetProperty("project").GetProperty("name").GetString());
        Assert.Equal("0.1-preview", summary.GetProperty("engineVersion").GetString());
        Assert.False(string.IsNullOrWhiteSpace(summary.GetProperty("dotnetVersion").GetString()));
        Assert.Equal("Standard", summary.GetProperty("rendererProfile").GetString());
        Assert.Equal("scenes/main.scene.json", summary.GetProperty("mainScene").GetString());
        Assert.Contains(
            "jump",
            summary.GetProperty("inputMap").GetProperty("actions").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Contains(
            "Game.PlayerController",
            summary.GetProperty("customClasses").EnumerateArray().Select(item => item.GetProperty("type").GetString()));
        Assert.Contains(
            "e2d validate --project <project> --format json",
            summary.GetProperty("checkCommands").EnumerateArray().Select(item => item.GetString()));

        using var sceneIndexJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "scene-index.json")));
        var scene = sceneIndexJson.RootElement.GetProperty("scenes")[0];
        Assert.Equal("scenes/main.scene.json", scene.GetProperty("path").GetString());
        Assert.Contains(
            "Player",
            scene.GetProperty("nodes").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Contains(
            "res://assets/player.e2res",
            scene.GetProperty("externalReferences").EnumerateArray().Select(item => item.GetProperty("path").GetString()));

        using var resourceGraphJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "resource-graph.json")));
        Assert.Contains(
            "assets/player.e2res",
            resourceGraphJson.RootElement.GetProperty("resources").EnumerateArray().Select(item => item.GetProperty("path").GetString()));
        Assert.Contains(
            "res://assets/player.e2res",
            resourceGraphJson.RootElement.GetProperty("sceneReferences").EnumerateArray().Select(item => item.GetProperty("target").GetString()));

        using var apiSurfaceJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "api-surface.json")));
        Assert.True(apiSurfaceJson.RootElement.GetProperty("typeCount").GetInt32() > 0);

        var combinedContext = string.Join(
            "\n",
            expectedFiles.Select(file => File.ReadAllText(Path.Combine(contextRoot, file))));
        Assert.DoesNotContain("<redacted>", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain(".git", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("TASKS.md", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("dev-diary", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("completed-tasks", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("import-cache", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("huge.log", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("player.png", combinedContext, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceTransactionDryRunUsesExplicitHeadlessFallbackAndStableJsonEnvelope()
    {
        var projectRoot = CreateProjectRoot("headless-dry-run", SceneText(speed: 10));
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "workspace",
            "transaction",
            "--project",
            projectRoot,
            "--path",
            "scenes/main.scene.json",
            "--expected-revision",
            "1",
            "--text",
            SceneText(speed: 12),
            "--dry-run",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("workspace transaction", root.GetProperty("command").GetString());
        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("headless", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("dryRun").GetBoolean());
        Assert.Contains("scenes/main.scene.json", root.GetProperty("changedFiles").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("workspace.transaction", root.GetProperty("operation").GetProperty("operationKind").GetString());
        Assert.Contains("\"value\": 10", File.ReadAllText(Path.Combine(projectRoot, "scenes", "main.scene.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceTransactionRoutesToActiveEditorWorkspaceWithoutTouchingDisk()
    {
        var projectRoot = CreateProjectRoot("active-editor-route", SceneText(speed: 10));
        var registry = new EditorSessionRegistry(TimeSpan.FromSeconds(30));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-cli-route",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-cli-route"),
            FixedInstant);
        editor.Workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            1,
            ProjectWorkspaceOperationContext.ForTest("open-cli-route-scene"));

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant, registry),
            "workspace",
            "transaction",
            "--project",
            projectRoot,
            "--path",
            "scenes/main.scene.json",
            "--expected-revision",
            "1",
            "--text",
            SceneText(speed: 14),
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;

        Assert.Equal("activeEditor", root.GetProperty("route").GetString());
        Assert.Contains("scenes/main.scene.json", root.GetProperty("dirtyDocuments").EnumerateArray().Select(item => item.GetString()));
        Assert.Empty(root.GetProperty("changedFiles").EnumerateArray());
        Assert.Contains("\"value\": 14", editor.Workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
        Assert.Contains("\"value\": 10", File.ReadAllText(Path.Combine(projectRoot, "scenes", "main.scene.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectCreateCreatesAgentReadyTemplateAndStableJsonEnvelope()
    {
        var projectsRoot = CreateTemporaryDirectory("electron2d-cli-project-create-");

        try
        {
            var result = RunCli(
                CliExecutionContext.ForTests(FixedInstant),
                "project",
                "create",
                "CliAgentGame",
                "--output",
                projectsRoot,
                "--renderer-profile",
                "Compatibility",
                "--format",
                "json");

            Assert.Equal(0, result.ExitCode);
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            var data = root.GetProperty("data");
            var projectRoot = data.GetProperty("projectPath").GetString() ?? string.Empty;

            Assert.True(root.GetProperty("succeeded").GetBoolean());
            Assert.Equal("project create", root.GetProperty("command").GetString());
            Assert.Equal("headless", root.GetProperty("route").GetString());
            Assert.Equal("CliAgentGame", data.GetProperty("projectName").GetString());
            Assert.Equal("Compatibility", data.GetProperty("rendererProfile").GetString());
            Assert.True(data.GetProperty("gitInitialized").GetBoolean());
            Assert.Equal(5, data.GetProperty("starterSkillCount").GetInt32());
            Assert.EndsWith("AGENTS.md", data.GetProperty("agentInstructionsPath").GetString(), StringComparison.Ordinal);
            Assert.EndsWith(".taskboard/board.e2tasks", data.GetProperty("taskBoardPath").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
            Assert.True(File.Exists(data.GetProperty("projectSettingsPath").GetString()));
            Assert.True(File.Exists(data.GetProperty("mainScenePath").GetString()));

            AssertAgentReadyProject(projectRoot, "Compatibility");
        }
        finally
        {
            Directory.Delete(projectsRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("import", "Import")]
    [InlineData("build", "Build")]
    [InlineData("run", "Run")]
    [InlineData("test", "Test")]
    [InlineData("export", "Export")]
    public void JobCommandsEmitStableJsonlWithSnapshotIdentity(string command, string expectedKind)
    {
        var projectRoot = CreateProjectRoot($"job-{command}", SceneText(speed: 10));
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            command,
            "--project",
            projectRoot,
            "--format",
            "jsonl",
            "--input-build-configuration-hash",
            "sha256:test");

        Assert.Equal(0, result.ExitCode);
        var line = Assert.Single(result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(command, root.GetProperty("command").GetString());
        Assert.Equal("operation.queued", root.GetProperty("event").GetString());
        Assert.Equal(expectedKind, root.GetProperty("jobKind").GetString());
        Assert.Equal("Queued", root.GetProperty("jobState").GetString());
        Assert.Equal("sha256:test", root.GetProperty("inputBuildConfigurationHash").GetString());
        Assert.False(root.GetProperty("stale").GetBoolean());
        Assert.Equal(1, root.GetProperty("inputDocumentRevisions").GetProperty("scenes/main.scene.json").GetInt64());
    }

    [Fact]
    public void ApiCompareGodotReturnsProfileApprovalJsonWithoutStrictParityClaim()
    {
        var docsRoot = CreateApiCompareDocsRoot("api-compare-approved", "approved");
        var result = RunCliWithDocsRoot(
            CliExecutionContext.ForTests(FixedInstant),
            docsRoot,
            "api",
            "compare-godot",
            "Control",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var type = data.GetProperty("type");
        var profile = type.GetProperty("profile");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("api compare-godot", root.GetProperty("command").GetString());
        Assert.Equal("API type is approved by the Electron2D public API profile.", root.GetProperty("message").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("api.compareGodot", data.GetProperty("mode").GetString());
        Assert.Equal("data/api/electron2d-api-manifest.json", data.GetProperty("sourcePath").GetString());
        Assert.Equal("data/api/electron2d-public-api-profile.json", data.GetProperty("profileSourcePath").GetString());
        Assert.Equal("Electron2D.Control", type.GetProperty("fullName").GetString());
        Assert.Equal("electron2d://api/type/Electron2D.Control", type.GetProperty("id").GetString());
        Assert.Equal("supported", profile.GetProperty("status").GetString());
        Assert.Equal("profile_approved", profile.GetProperty("parity").GetString());
        Assert.False(profile.GetProperty("outOfProfile").GetBoolean());
        Assert.Equal("approved", profile.GetProperty("decision").GetString());
        Assert.True(type.GetProperty("availability").GetProperty("exported").GetBoolean());
        Assert.Equal("profile_approved", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("diagnostics").GetArrayLength());
        Assert.False(data.TryGetProperty("strictParity", out _));
        var evidence = data.GetProperty("parityEvidence");
        Assert.Equal("not_verified", evidence.GetProperty("status").GetString());
        Assert.Contains("manual public API profile", evidence.GetProperty("reason").GetString(), StringComparison.Ordinal);

        var approvedNotExported = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "api",
            "compare-godot",
            "AcceptDialog",
            "--format",
            "json");
        Assert.Equal(0, approvedNotExported.ExitCode);
        using var approvedNotExportedJson = JsonDocument.Parse(approvedNotExported.Output);
        var approvedNotExportedData = approvedNotExportedJson.RootElement.GetProperty("data");
        Assert.Equal("profile_approved", approvedNotExportedData.GetProperty("result").GetProperty("status").GetString());
        Assert.False(approvedNotExportedData.GetProperty("type").GetProperty("availability").GetProperty("exported").GetBoolean());

        foreach (var identity in new[]
        {
            new { Query = "ResourceUID", FullName = "Electron2D.ResourceUID", Exported = false },
            new { Query = "Electron2D.ResourceUID", FullName = "Electron2D.ResourceUID", Exported = false },
            new { Query = "ResourceUid", FullName = "Electron2D.ResourceUid", Exported = true },
            new { Query = "Electron2D.ResourceUid", FullName = "Electron2D.ResourceUid", Exported = true },
            new { Query = "RID", FullName = "Electron2D.RID", Exported = false },
            new { Query = "Electron2D.RID", FullName = "Electron2D.RID", Exported = false },
            new { Query = "Rid", FullName = "Electron2D.Rid", Exported = true },
            new { Query = "Electron2D.Rid", FullName = "Electron2D.Rid", Exported = true }
        })
        {
            var identityResult = RunCli(
                CliExecutionContext.ForTests(FixedInstant),
                "api",
                "compare-godot",
                identity.Query,
                "--format",
                "json");
            Assert.Equal(0, identityResult.ExitCode);
            using var identityJson = JsonDocument.Parse(identityResult.Output);
            var identityType = identityJson.RootElement.GetProperty("data").GetProperty("type");
            Assert.Equal(identity.FullName, identityType.GetProperty("fullName").GetString());
            Assert.Equal(identity.Exported, identityType.GetProperty("availability").GetProperty("exported").GetBoolean());
            if (identity.Exported)
            {
                Assert.Equal($"electron2d://api/type/{identity.FullName}", identityType.GetProperty("id").GetString());
            }
            else
            {
                Assert.Equal(JsonValueKind.Null, identityType.GetProperty("id").ValueKind);
            }
        }

        var unsupported = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "api",
            "compare-godot",
            "AABB",
            "--format",
            "json");
        Assert.Equal(1, unsupported.ExitCode);
        using var unsupportedJson = JsonDocument.Parse(unsupported.Output);
        Assert.Equal("unsupported", unsupportedJson.RootElement.GetProperty("data").GetProperty("result").GetProperty("status").GetString());

        var unknown = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "api",
            "compare-godot",
            "DefinitelyMissingApiType",
            "--format",
            "json");
        Assert.Equal(1, unknown.ExitCode);
        using var unknownJson = JsonDocument.Parse(unknown.Output);
        Assert.Equal("type_not_found", unknownJson.RootElement.GetProperty("data").GetProperty("result").GetProperty("status").GetString());
    }

    [Fact]
    public void ApiCompareGodotRejectsOutOfProfileTypeWithStableDiagnostic()
    {
        var docsRoot = CreateApiCompareDocsRoot("api-compare-deferred", "deferred");
        var result = RunCliWithDocsRoot(
            CliExecutionContext.ForTests(FixedInstant),
            docsRoot,
            "api",
            "compare-godot",
            "Control",
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var type = data.GetProperty("type");
        var profile = type.GetProperty("profile");
        var diagnostic = root.GetProperty("diagnostics")[0];

        Assert.False(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("api compare-godot", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("deferred", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("Electron2D.Control", type.GetProperty("fullName").GetString());
        Assert.True(profile.GetProperty("outOfProfile").GetBoolean());
        Assert.Equal("deferred", profile.GetProperty("decision").GetString());
        Assert.Equal("E2D-CLI-0002", diagnostic.GetProperty("code").GetString());
        Assert.Contains("manual profile decision 'deferred'", diagnostic.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.DoesNotContain("workaround", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alternative", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportPlanWebReturnsWebAssemblyBrowserPlanWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("web-plan-cli");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "plan-web",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var plan = data.GetProperty("plan");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export plan-web", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("export.web.plan", data.GetProperty("mode").GetString());
        Assert.Equal("WebAssemblyBrowser", data.GetProperty("target").GetString());
        Assert.Equal("browser-wasm", data.GetProperty("runtimeIdentifier").GetString());
        Assert.Equal("browser-wasm", plan.GetProperty("runtimeIdentifier").GetString());
        Assert.EndsWith("exports/web/wwwroot", plan.GetProperty("webRootDirectory").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.Contains("renderingReadiness", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public void ExportBuildWebCreatesBrowserPackageWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("web-build-cli");
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets"));
        File.WriteAllText(Path.Combine(projectRoot, "assets", "sprite.txt"), "sprite");
        Directory.CreateDirectory(Path.Combine(projectRoot, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task"), "local task metadata");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-web",
            "--project",
            projectRoot,
            "--output",
            "exports/web",
            "--skip-publish",
            "true",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var package = data.GetProperty("package");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export build-web", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.web.build", data.GetProperty("mode").GetString());
        Assert.Equal("packaged", data.GetProperty("result").GetProperty("status").GetString());
        Assert.True(data.GetProperty("result").GetProperty("publishSkipped").GetBoolean());
        Assert.Contains("index.html", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("electron2d.loader.js", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("electron2d.webmanifest.json", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("assets/sprite.txt", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "index.html")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "electron2d.loader.js")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "electron2d.webmanifest.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "assets", "sprite.txt")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", ".electron2d")));
    }

    [Fact]
    public void ExportRunWebWritesBrowserSmokeArtifactWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("web-run-cli");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-web",
            "--project",
            projectRoot,
            "--output",
            "exports/web",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-web",
            "--project",
            projectRoot,
            "--output",
            "exports/web",
            "--url",
            "http://127.0.0.1:8080/index.html",
            "--smoke-output",
            ".electron2d/export-smoke/web-smoke.json",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-web", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.web.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-passed", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("http://127.0.0.1:8080/index.html", smoke.GetProperty("launchUrl").GetString());
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "export-smoke", "web-smoke.json")));
        Assert.Contains("startup", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("sceneLoad", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("renderingReadiness", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("inputEventPath", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("audioPolicyState", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("resourceLoading", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("saveDataPolicy", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public void ExportPlanAndroidReturnsAndroidArm64PlanWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("android-plan-cli");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "plan-android",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var plan = data.GetProperty("plan");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export plan-android", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("export.android.plan", data.GetProperty("mode").GetString());
        Assert.Equal("AndroidArm64", data.GetProperty("target").GetString());
        Assert.Equal("android-arm64", data.GetProperty("runtimeIdentifier").GetString());
        Assert.Equal("apk", plan.GetProperty("packageFormat").GetString());
        Assert.Equal("arm64-v8a", plan.GetProperty("abi").GetString());
        Assert.EndsWith("exports/android/debug/android", plan.GetProperty("stagingDirectory").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.Contains("pauseResume", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public void ExportBuildAndroidCreatesStagingProjectWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("android-build-cli");
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets"));
        File.WriteAllText(Path.Combine(projectRoot, "assets", "sprite.txt"), "sprite");
        Directory.CreateDirectory(Path.Combine(projectRoot, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task"), "local task metadata");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var package = data.GetProperty("package");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export build-android", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.android.build", data.GetProperty("mode").GetString());
        Assert.Equal("staged", data.GetProperty("result").GetProperty("status").GetString());
        Assert.True(data.GetProperty("result").GetProperty("publishSkipped").GetBoolean());
        Assert.Contains("Electron2D.Android.csproj", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("MainActivity.cs", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("AndroidManifest.xml", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Assets/electron2d/assets/sprite.txt", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "Electron2D.Android.csproj")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "MainActivity.cs")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "AndroidManifest.xml")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "Assets", "electron2d", ".electron2d")));
    }

    [Fact]
    public void ExportRunAndroidWithoutDeviceWritesBlockedSmokeArtifactWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("android-run-cli");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);
        var adbPath = CreateFakeAdbWithoutDevices(projectRoot);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--smoke-output",
            ".electron2d/export-smoke/android-smoke.json",
            "--adb-path",
            adbPath,
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");
        var diagnostic = root.GetProperty("diagnostics")[0];

        Assert.False(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-android", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.android.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-blocked", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("E2D-CLI-0002", diagnostic.GetProperty("code").GetString());
        Assert.Contains("E2D-EXPORT-ANDROID-0014", diagnostic.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "export-smoke", "android-smoke.json")));
        Assert.Contains("pauseResume", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("render", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("input", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("audio", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("resources", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("filesystem", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public void ExportRunAndroidWithDeviceInstallsLaunchesAndWritesPassedSmokeArtifact()
    {
        var projectRoot = CreateExportProjectRoot("android-run-cli-device");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);

        var apkDirectory = Path.Combine(
            projectRoot,
            "exports",
            "android",
            "debug",
            "android",
            "bin",
            "Debug",
            "net10.0-android",
            "android-arm64");
        Directory.CreateDirectory(apkDirectory);
        File.WriteAllText(Path.Combine(apkDirectory, "electron2d.androidexport-Signed.apk"), "fake apk");
        var adbPath = CreateFakeAdb(projectRoot);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--smoke-output",
            ".electron2d/export-smoke/android-smoke.json",
            "--adb-path",
            adbPath,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-android", root.GetProperty("command").GetString());
        Assert.Equal("export.android.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-passed", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("emulator-5554", smoke.GetProperty("deviceSerial").GetString());
        Assert.All(
            smoke.GetProperty("criteria").EnumerateObject(),
            criterion => Assert.True(criterion.Value.GetProperty("passed").GetBoolean(), criterion.Name));
        Assert.Contains(
            "logcat -d -s Electron2D:I *:S",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell input keyevent KEYCODE_WAKEUP",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell wm dismiss-keyguard",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell input tap",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell am start -n dev.electron2d.referencegame/crc644abc767ad8be2900.MainActivity",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell monkey -p dev.electron2d.referencegame --pct-touch 100",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        var artifactPath = Path.Combine(projectRoot, ".electron2d", "export-smoke", "android-smoke.json");
        Assert.True(File.Exists(artifactPath));
        Assert.Contains("\"status\": \"passed\"", File.ReadAllText(artifactPath), StringComparison.Ordinal);
    }

    [Fact]
    public void ExportRunAndroidUsesRequestedAdbSerialWhenMultipleDevicesAreAvailable()
    {
        var projectRoot = CreateExportProjectRoot("android-run-cli-adb-serial");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);

        var apkDirectory = Path.Combine(
            projectRoot,
            "exports",
            "android",
            "debug",
            "android",
            "bin",
            "Debug",
            "net10.0-android",
            "android-arm64");
        Directory.CreateDirectory(apkDirectory);
        File.WriteAllText(Path.Combine(apkDirectory, "electron2d.androidexport-Signed.apk"), "fake apk");
        var adbPath = CreateFakeAdbWithPhoneAndEmulator(projectRoot);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--smoke-output",
            ".electron2d/export-smoke/android-smoke.json",
            "--adb-path",
            adbPath,
            "--adb-serial",
            "emulator-5554",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var smoke = root.GetProperty("data").GetProperty("smoke");
        var adbLog = File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt"));

        Assert.Equal("emulator-5554", smoke.GetProperty("deviceSerial").GetString());
        Assert.Contains("-s emulator-5554 shell getprop ro.product.cpu.abi", adbLog, StringComparison.Ordinal);
        Assert.Contains("-s emulator-5554 install -r -t", adbLog, StringComparison.Ordinal);
        Assert.DoesNotContain("-s 641d225b0510 install -r -t", adbLog, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportPlanIosReturnsIosArm64PlanWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("ios-plan-cli");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "plan-ios",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var plan = data.GetProperty("plan");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export plan-ios", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.ios.plan", data.GetProperty("mode").GetString());
        Assert.Equal("IosArm64", data.GetProperty("target").GetString());
        Assert.Equal("ios-arm64", data.GetProperty("runtimeIdentifier").GetString());
        Assert.Equal("metal", plan.GetProperty("graphicsBackend").GetString());
        Assert.EndsWith("exports/ios/debug/ios", plan.GetProperty("stagingDirectory").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.Contains("safeArea", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("precompiledArtifacts", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void ExportBuildIosCreatesStagingProjectWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("ios-build-cli");
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets"));
        File.WriteAllText(Path.Combine(projectRoot, "assets", "sprite.txt"), "sprite");
        Directory.CreateDirectory(Path.Combine(projectRoot, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task"), "local task metadata");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-ios",
            "--project",
            projectRoot,
            "--output",
            "exports/ios/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var package = data.GetProperty("package");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export build-ios", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.ios.build", data.GetProperty("mode").GetString());
        Assert.Equal("staged", data.GetProperty("result").GetProperty("status").GetString());
        Assert.True(data.GetProperty("result").GetProperty("publishSkipped").GetBoolean());
        Assert.Contains("Electron2D.iOS.csproj", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("AppDelegate.cs", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Info.plist", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Entitlements.plist", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("ExportMetadata.json", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Assets/electron2d/assets/sprite.txt", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "ios", "debug", "ios", "Electron2D.iOS.csproj")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "ios", "debug", "ios", "Electron2D.iOS.xcodeproj", "project.pbxproj")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "exports", "ios", "debug", "ios", "Assets", "electron2d", ".electron2d")));
    }

    [Fact]
    public void ExportRunIosWithoutSimulatorOrDeviceWritesBlockedSmokeArtifactWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("ios-run-cli");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-ios",
            "--project",
            projectRoot,
            "--output",
            "exports/ios/debug",
            "--smoke-output",
            ".electron2d/export-smoke/ios-smoke.json",
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");
        var diagnostic = root.GetProperty("diagnostics")[0];

        Assert.False(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-ios", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.ios.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-blocked", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("E2D-CLI-0002", diagnostic.GetProperty("code").GetString());
        Assert.Contains("E2D-EXPORT-IOS-0011", diagnostic.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "export-smoke", "ios-smoke.json")));
        Assert.Contains("safeArea", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("input", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("audio", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("resources", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("filesystem", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("precompiledArtifacts", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public void UnknownCommandGroupReturnsStableJsonDiagnostic()
    {
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "unknown",
            "command",
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var diagnostic = json.RootElement.GetProperty("diagnostics")[0];

        Assert.False(json.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal("blocked", json.RootElement.GetProperty("route").GetString());
        Assert.Equal("E2D-CLI-0001", diagnostic.GetProperty("code").GetString());
    }

    private static void AddPassedCriterion(
        CliExecutionContext context,
        string projectRoot,
        string taskId,
        long expectedRevision)
    {
        var added = RunCli(
            context,
            "tasks", "criterion", "add", taskId,
            "--criterion", "criterion-complete",
            "--description", "Результат задачи проверен.",
            "--expected-revision", expectedRevision.ToString(),
            "--project", projectRoot,
            "--format", "json");
        Assert.True(added.ExitCode == 0, added.Output);

        var evidenced = RunCli(
            context,
            "tasks", "criterion", "add-evidence", taskId,
            "--criterion", "criterion-complete",
            "--kind", "File",
            "--value", "docs/criterion-evidence.md",
            "--expected-revision", (expectedRevision + 1).ToString(),
            "--project", projectRoot,
            "--format", "json");
        Assert.True(evidenced.ExitCode == 0, evidenced.Output);

        var passed = RunCli(
            context,
            "tasks", "criterion", "set-state", taskId,
            "--criterion", "criterion-complete",
            "--state", "Passed",
            "--expected-revision", (expectedRevision + 2).ToString(),
            "--project", projectRoot,
            "--format", "json");
        Assert.True(passed.ExitCode == 0, passed.Output);
    }

    private static CliRunResult RunCli(CliExecutionContext context, params string[] args)
    {
        return RunCliExact(context, args);
    }

    private static void WriteV2Taskboard(string projectRoot, DateTimeOffset now, string title)
    {
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "tasks"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "completed"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "attachments"));
        var task = new ProjectTask
        {
            TaskUid = "task-migration-source",
            TaskId = "T-0001",
            Title = title,
            Description = "Исходная задача формата v2 для проверки штатной миграции.",
            Status = ProjectTaskStatus.Ready,
            Readiness = TaskReadiness.Ready,
            Priority = "P1",
            CreatedBy = "test",
            CreatedAt = now,
            UpdatedAt = now,
            AcceptanceState = ProjectTaskAcceptanceState.Open
        };
        var board = new TaskBoard(
            "main",
            revision: 1,
            groups: [],
            placements: [new TaskBoardPlacement(task.TaskId, groupId: null, "00001000")]);
        board.IdPolicy.NextNumber = 2;
        File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task"), ProjectTaskSerializer.Serialize(task));
        File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks"), ProjectTaskSerializer.SerializeBoard(board));
    }

    private static CliRunResult RunCliExact(CliExecutionContext context, params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Electron2DCommandLine.Run(args, output, error, context);

        return new CliRunResult(exitCode, output.ToString(), error.ToString());
    }

    private static CliRunResult RunCliWithDocsRoot(CliExecutionContext context, string docsRoot, params string[] args)
    {
        var previousDocsRoot = Environment.GetEnvironmentVariable("ELECTRON2D_DOCS_ROOT");
        Environment.SetEnvironmentVariable("ELECTRON2D_DOCS_ROOT", docsRoot);
        try
        {
            return RunCli(context, args);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ELECTRON2D_DOCS_ROOT", previousDocsRoot);
        }
    }

    private static string CreateApiCompareDocsRoot(string name, string decision)
    {
        var root = CreateTemporaryDirectory("electron2d-cli-" + name + "-");
        var isApproved = string.Equals(decision, "approved", StringComparison.Ordinal);
        var status = isApproved ? "supported" : decision;
        var parity = isApproved ? "profile_approved" : "not_verified";
        var outOfProfile = isApproved ? "false" : "true";
        Directory.CreateDirectory(Path.Combine(root, "data", "api"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "documentation"));
        File.WriteAllText(
            Path.Combine(root, "data", "api", "electron2d-public-api-profile.json"),
            $$"""
            {
              "schemaVersion": 1,
              "release": "0.1-preview",
              "godotBaseline": "4.7-stable",
              "approvalAuthority": "project-owner",
              "types": [
                {
                  "fullName": "Electron2D.Control",
                  "godotReference": "Control",
                  "decision": "{{decision}}",
                  "rationale": "Fixture decision for CLI profile behavior."
                }
              ]
            }
            """);
        File.WriteAllText(
            Path.Combine(root, "data", "api", "electron2d-api-manifest.json"),
            $$"""
            {
              "schemaVersion": 1,
              "manifestVersion": "0.1-preview",
              "engineVersion": "0.1-preview",
              "profileName": "Electron2D 0.1-preview",
              "godotBaseline": "4.7-stable",
              "generatedFrom": {
                "compiledAssembly": "src/Electron2D/bin/Debug/net10.0/Electron2D.dll",
                "xmlDocumentation": ".temp/api-manifest/Electron2D.xml",
                "publicApiProfile": "data/api/electron2d-public-api-profile.json"
              },
              "strictParityEvidence": {
                "status": "not_verified",
                "reason": "The manual public API profile records owner-approved scope only; strict Godot 4.7 parity is verified by owning class tasks and final gates."
              },
              "types": [
                {
                  "id": "electron2d://api/type/Electron2D.Control",
                  "fullName": "Electron2D.Control",
                  "name": "Control",
                  "profile": {
                    "status": "{{status}}",
                    "parity": "{{parity}}",
                    "outOfProfile": {{outOfProfile}},
                    "godotReference": "Control",
                    "notes": "Fixture decision for CLI profile behavior."
                  }
                }
              ]
            }
            """);
        return root;
    }

    private static string CreateProjectRoot(string name, string sceneText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-CliWorkflowTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), sceneText);
        return root;
    }

    private static string CreateExportProjectRoot(string name)
    {
        var root = CreateProjectRoot(name, SceneText(speed: 10));
        var settings = Electron2D.Electron2DProjectSettings.Capture(
            "ReferenceGame",
            "0.1.0",
            "0.1-preview",
            "scenes/main.scene.json");
        Electron2D.Electron2DSettingsStore.SaveProject(Path.Combine(root, "project.e2d.json"), settings);
        File.WriteAllText(
            Path.Combine(root, "Electron2D.Empty.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        return root;
    }

    private static string CreateContextProjectRoot(string name)
    {
        var root = CreateExportProjectRoot(name);
        var resourceDirectory = Path.Combine(root, "assets");
        var scriptDirectory = Path.Combine(root, "Scripts");
        Directory.CreateDirectory(resourceDirectory);
        Directory.CreateDirectory(scriptDirectory);

        var resourcePath = "res://assets/player.e2res";
        var resourceUid = Electron2D.ResourceUid.CreateIdForPath(resourcePath);
        var resourceDocument = new Electron2D.ResourceFileDocument(
            resourceUid,
            "Electron2D.Texture2D",
            resourcePath);
        File.WriteAllText(
            Path.Combine(resourceDirectory, "player.e2res"),
            Electron2D.ResourceFileTextSerializer.Serialize(resourceDocument));
        File.WriteAllBytes(Path.Combine(resourceDirectory, "player.png"), [0x89, 0x50, 0x4E, 0x47]);

        var sceneDocument = new Electron2D.SceneFileDocument(
            [
                new Electron2D.ResourceFileExternalReference(
                    1,
                    resourceUid,
                    resourcePath,
                    "Electron2D.Texture2D")
            ],
            [],
            [
                new Electron2D.SceneFileNode(1, "Electron2D.Node2D", "Root", null, null, ["gameplay"]),
                new Electron2D.SceneFileNode(2, "Game.PlayerController", "Player", 1, 1, ["player"]),
                new Electron2D.SceneFileNode(3, "Electron2D.Sprite2D", "Sprite", 2, 1)
            ]);
        File.WriteAllText(
            Path.Combine(root, "scenes", "main.scene.json"),
            Electron2D.SceneFileTextSerializer.Serialize(sceneDocument));

        File.WriteAllText(
            Path.Combine(scriptDirectory, "PlayerController.cs"),
            """
            namespace Game;

            public sealed class PlayerController : Electron2D.Node2D
            {
            }
            """);

        Electron2D.InputMap.ClearForTests();
        try
        {
            Electron2D.InputMap.AddAction("jump", 0.25f);
            Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space });
            var settings = Electron2D.Electron2DProjectSettings.Capture(
                "ContextGame",
                "0.1.0",
                "0.1-preview",
                "scenes/main.scene.json");
            settings.RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard;
            Electron2D.Electron2DSettingsStore.SaveProject(Path.Combine(root, "project.e2d.json"), settings);
        }
        finally
        {
            Electron2D.InputMap.ClearForTests();
        }

        Directory.CreateDirectory(Path.Combine(root, ".git"));
        File.WriteAllText(Path.Combine(root, ".git", "config"), "token=<redacted>");
        Directory.CreateDirectory(Path.Combine(root, ".electron2d", "import-cache"));
        File.WriteAllBytes(Path.Combine(root, ".electron2d", "import-cache", "cached-texture.bin"), [1, 2, 3, 4]);
        Directory.CreateDirectory(Path.Combine(root, "dev-diary"));
        Directory.CreateDirectory(Path.Combine(root, "completed-tasks"));
        File.WriteAllText(Path.Combine(root, "TASKS.md"), "password=<redacted>");
        File.WriteAllText(Path.Combine(root, "huge.log"), new string('x', 70 * 1024) + "<redacted>");

        return root;
    }

    private static string FindRepositoryRootForTasks()
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

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteTaskDocuments(string projectRoot, params ProjectTask[] tasks)
    {
        var tasksRoot = Path.Combine(projectRoot, ".taskboard", "tasks");
        Directory.CreateDirectory(tasksRoot);
        foreach (var task in tasks)
        {
            File.WriteAllText(
                Path.Combine(tasksRoot, $"{task.TaskId}.e2task"),
                ProjectTaskSerializer.Serialize(task));
        }

        var board = new TaskBoard(
            "board-main",
            revision: 1,
            groups: [],
            placements: tasks.Select(task => new TaskBoardPlacement(task.TaskId, groupId: null, task.Rank)));
        File.WriteAllText(
            Path.Combine(projectRoot, ".taskboard", "board.e2tasks"),
            ProjectTaskSerializer.SerializeBoard(board));
    }

    private static ProjectTask CreateReportTask(
        string taskId,
        string title,
        ProjectTaskStatus status,
        string rank,
        DateTimeOffset? completedAt)
    {
        var task = new ProjectTask
        {
            TaskId = taskId,
            Title = title,
            Description = "Exercise Project Tasks report export.",
            Status = status,
            Readiness = TaskReadiness.Ready,
            Priority = "P0",
            Rank = rank,
            Assignee = "agent-1",
            CreatedBy = "user-1",
            CreatedAt = FixedInstant,
            UpdatedAt = FixedInstant,
            CompletedAt = completedAt,
            AcceptedAt = completedAt,
            AcceptedBy = completedAt is null ? null : "user-1",
            AcceptanceState = completedAt is null
                ? ProjectTaskAcceptanceState.Open
                : ProjectTaskAcceptanceState.Accepted
        };
        task.Labels.Add("milestone:preview");
        task.Labels.Add("version:0.1-preview");
        task.Labels.Add("epic:editor");
        task.Labels.Add("agent-session:agent-session-1");
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            $"criterion-{taskId}",
            "Golden output is stable.",
            completedAt is null ? AcceptanceCriterionState.Open : AcceptanceCriterionState.Passed,
            []));
        task.Activity.Add(new TaskActivityEntry(
            $"activity-{taskId}",
            "agent-1",
            PrincipalKind.Agent,
            completedAt?.AddMinutes(-30) ?? FixedInstant.AddMinutes(10),
            TaskActivityKind.TestResult,
            "AgentSessionId=agent-session-1; focused tests green."));
        return task;
    }

    private static string CreateFakeAdb(string projectRoot)
    {
        var logPath = Path.Combine(projectRoot, "fake-adb-args.txt");
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(projectRoot, "fake-adb.cmd");
            File.WriteAllText(
                path,
                $$"""
                @echo off
                echo %*>>"{{logPath}}"
                set ARGS=%*
                if "%1"=="devices" (
                  echo List of devices attached
                  echo emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:1
                  exit /b 0
                )
                if "%1"=="-s" (
                  if "%3"=="install" (
                    echo Success
                    exit /b 0
                  )
                  if "%3"=="logcat" (
                    echo I/Electron2D: E2D_SMOKE_LAUNCH_READY
                    echo I/Electron2D: E2D_SMOKE_RENDER_READY
                    echo I/Electron2D: E2D_SMOKE_TOUCH_READY
                    echo I/Electron2D: E2D_SMOKE_PAUSE_READY
                    echo I/Electron2D: E2D_SMOKE_RESUME_READY
                    echo I/Electron2D: E2D_SMOKE_ORIENTATION_READY
                    echo I/Electron2D: E2D_SMOKE_SAFE_AREA_READY
                    echo I/Electron2D: E2D_SMOKE_AUDIO_READY
                    echo I/Electron2D: E2D_SMOKE_RESOURCES_READY
                    echo I/Electron2D: E2D_SMOKE_FILESYSTEM_READY
                    echo I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY
                    echo I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY
                    echo I/Electron2D: E2D_SMOKE_SHUTDOWN_READY
                    exit /b 0
                  )
                  if "%3"=="shell" (
                    if "%4"=="getprop" (
                      echo x86_64
                      exit /b 0
                    )
                    if "%4"=="input" (
                      exit /b 1
                    )
                    exit /b 0
                  )
                )
                echo OK
                exit /b 0
                """,
                System.Text.Encoding.ASCII);
            return path;
        }

        var unixPath = Path.Combine(projectRoot, "fake-adb.sh");
        File.WriteAllText(
            unixPath,
            $$"""
            #!/usr/bin/env sh
            printf '%s\n' "$*" >> "{{logPath}}"
            if [ "$1" = "devices" ]; then
              echo "List of devices attached"
              echo "emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:1"
              exit 0
            fi
            if [ "$1" = "-s" ]; then
              if [ "$3" = "install" ]; then
                echo "Success"
                exit 0
              fi
              if [ "$3" = "logcat" ]; then
                echo "I/Electron2D: E2D_SMOKE_LAUNCH_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDER_READY"
                echo "I/Electron2D: E2D_SMOKE_TOUCH_READY"
                echo "I/Electron2D: E2D_SMOKE_PAUSE_READY"
                echo "I/Electron2D: E2D_SMOKE_RESUME_READY"
                echo "I/Electron2D: E2D_SMOKE_ORIENTATION_READY"
                echo "I/Electron2D: E2D_SMOKE_SAFE_AREA_READY"
                echo "I/Electron2D: E2D_SMOKE_AUDIO_READY"
                echo "I/Electron2D: E2D_SMOKE_RESOURCES_READY"
                echo "I/Electron2D: E2D_SMOKE_FILESYSTEM_READY"
                echo "I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY"
                echo "I/Electron2D: E2D_SMOKE_SHUTDOWN_READY"
                exit 0
              fi
              if [ "$3" = "shell" ]; then
                if [ "$4" = "getprop" ]; then
                  echo "x86_64"
                  exit 0
                fi
                if [ "$4" = "input" ]; then
                  exit 1
                fi
                exit 0
              fi
            fi
            echo "OK"
            exit 0
            """,
            System.Text.Encoding.ASCII);
        MakeExecutable(unixPath);
        return unixPath;
    }

    private static string CreateFakeAdbWithPhoneAndEmulator(string projectRoot)
    {
        var logPath = Path.Combine(projectRoot, "fake-adb-args.txt");
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(projectRoot, "fake-adb-multiple.cmd");
            File.WriteAllText(
                path,
                $$"""
                @echo off
                echo %*>>"{{logPath}}"
                if "%1"=="devices" (
                  echo List of devices attached
                  echo 641d225b0510 device product:eos model:22011119UY device:eos transport_id:1
                  echo emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:2
                  exit /b 0
                )
                if "%1"=="-s" (
                  if "%3"=="install" (
                    echo Success
                    exit /b 0
                  )
                  if "%3"=="logcat" (
                    echo I/Electron2D: E2D_SMOKE_LAUNCH_READY
                    echo I/Electron2D: E2D_SMOKE_RENDER_READY
                    echo I/Electron2D: E2D_SMOKE_TOUCH_READY
                    echo I/Electron2D: E2D_SMOKE_PAUSE_READY
                    echo I/Electron2D: E2D_SMOKE_RESUME_READY
                    echo I/Electron2D: E2D_SMOKE_ORIENTATION_READY
                    echo I/Electron2D: E2D_SMOKE_SAFE_AREA_READY
                    echo I/Electron2D: E2D_SMOKE_AUDIO_READY
                    echo I/Electron2D: E2D_SMOKE_RESOURCES_READY
                    echo I/Electron2D: E2D_SMOKE_FILESYSTEM_READY
                    echo I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY
                    echo I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY
                    echo I/Electron2D: E2D_SMOKE_SHUTDOWN_READY
                    exit /b 0
                  )
                  if "%3"=="shell" (
                    if "%4"=="getprop" (
                      if "%2"=="emulator-5554" (
                        echo x86_64
                      ) else (
                        echo arm64-v8a
                      )
                      exit /b 0
                    )
                    exit /b 0
                  )
                )
                echo OK
                exit /b 0
                """,
                System.Text.Encoding.ASCII);
            return path;
        }

        var unixPath = Path.Combine(projectRoot, "fake-adb-multiple.sh");
        File.WriteAllText(
            unixPath,
            $$"""
            #!/usr/bin/env sh
            printf '%s\n' "$*" >> "{{logPath}}"
            if [ "$1" = "devices" ]; then
              echo "List of devices attached"
              echo "641d225b0510 device product:eos model:22011119UY device:eos transport_id:1"
              echo "emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:2"
              exit 0
            fi
            if [ "$1" = "-s" ]; then
              if [ "$3" = "install" ]; then
                echo "Success"
                exit 0
              fi
              if [ "$3" = "logcat" ]; then
                echo "I/Electron2D: E2D_SMOKE_LAUNCH_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDER_READY"
                echo "I/Electron2D: E2D_SMOKE_TOUCH_READY"
                echo "I/Electron2D: E2D_SMOKE_PAUSE_READY"
                echo "I/Electron2D: E2D_SMOKE_RESUME_READY"
                echo "I/Electron2D: E2D_SMOKE_ORIENTATION_READY"
                echo "I/Electron2D: E2D_SMOKE_SAFE_AREA_READY"
                echo "I/Electron2D: E2D_SMOKE_AUDIO_READY"
                echo "I/Electron2D: E2D_SMOKE_RESOURCES_READY"
                echo "I/Electron2D: E2D_SMOKE_FILESYSTEM_READY"
                echo "I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY"
                echo "I/Electron2D: E2D_SMOKE_SHUTDOWN_READY"
                exit 0
              fi
              if [ "$3" = "shell" ]; then
                if [ "$4" = "getprop" ]; then
                  if [ "$2" = "emulator-5554" ]; then
                    echo "x86_64"
                  else
                    echo "arm64-v8a"
                  fi
                  exit 0
                fi
                exit 0
              fi
            fi
            echo "OK"
            exit 0
            """,
            System.Text.Encoding.ASCII);
        MakeExecutable(unixPath);
        return unixPath;
    }

    private static string CreateFakeAdbWithoutDevices(string projectRoot)
    {
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(projectRoot, "fake-adb-empty.cmd");
            File.WriteAllText(
                path,
                """
                @echo off
                if "%1"=="devices" (
                  echo List of devices attached
                  exit /b 0
                )
                exit /b 0
                """,
                System.Text.Encoding.ASCII);
            return path;
        }

        var unixPath = Path.Combine(projectRoot, "fake-adb-empty.sh");
        File.WriteAllText(
            unixPath,
            """
            #!/usr/bin/env sh
            if [ "$1" = "devices" ]; then
              echo "List of devices attached"
              exit 0
            fi
            exit 0
            """,
            System.Text.Encoding.ASCII);
        MakeExecutable(unixPath);
        return unixPath;
    }

    private static void MakeExecutable(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherExecute);
        }
    }

    private static void AssertAgentReadyProject(string projectRoot, string rendererProfile)
    {
        Assert.True(Directory.Exists(Path.Combine(projectRoot, ".git")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "AGENTS.md")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".gitignore")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", "board.e2tasks")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".taskboard", "tasks", "welcome.e2task")));
        Assert.False(File.Exists(Path.Combine(projectRoot, "TASKS.md")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "completed-tasks")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "dev-diary")));

        var agents = File.ReadAllText(Path.Combine(projectRoot, "AGENTS.md"));
        Assert.Contains("Electron2D 0.1-preview", agents, StringComparison.Ordinal);
        Assert.Contains($"Renderer profile: `{rendererProfile}`", agents, StringComparison.Ordinal);
        Assert.Contains("The command checks only manual profile approval; it does not prove full Godot 4.7 strict parity, which requires separate parity evidence.", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("strict verifier", agents, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("e2d tasks submit", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("TASKS.md", agents, StringComparison.Ordinal);

        var gitIgnoreLines = File.ReadAllLines(Path.Combine(projectRoot, ".gitignore"));
        Assert.Contains(".electron2d/import-cache/", gitIgnoreLines);
        Assert.Contains(".electron2d/workspaces/", gitIgnoreLines);
        Assert.Contains(".electron2d/context/", gitIgnoreLines);
        Assert.Contains(".electron2d/session/", gitIgnoreLines);
        Assert.Contains(".electron2d/user/", gitIgnoreLines);
        Assert.DoesNotContain(".electron2d/", gitIgnoreLines);
        Assert.DoesNotContain(".taskboard/", gitIgnoreLines);

        var skillFiles = Directory.EnumerateFiles(Path.Combine(projectRoot, ".codex", "skills"), "SKILL.md", SearchOption.AllDirectories)
            .ToArray();
        Assert.Equal(5, skillFiles.Length);
    }

    private static string SceneText(int speed)
    {
        return $$"""
        {
          "format": "Electron2D.SceneFile",
          "version": 1,
          "external": [],
          "internal": [],
          "nodes": [
            {
              "id": 1,
              "type": "Electron2D.Node2D",
              "name": "Player",
              "parent": null,
              "owner": null,
              "groups": [],
              "properties": {
                "speed": {
                  "type": "Int",
                  "value": {{speed}}
                }
              }
            }
          ]
        }
        """;
    }

    private static readonly string[] RequiredGroups =
    [
        "project",
        "scene",
        "resource",
        "workspace",
        "import",
        "build",
        "run",
        "test",
        "export",
        "validate",
        "docs",
        "api",
        "mcp",
        "tasks",
        "context",
        "doctor"
    ];

    private static readonly string[] MutatingOrJobGroups =
    [
        "project",
        "scene",
        "resource",
        "workspace",
        "import",
        "build",
        "run",
        "test",
        "export",
        "tasks"
    ];

    private sealed record CliRunResult(int ExitCode, string Output, string Error);
}
