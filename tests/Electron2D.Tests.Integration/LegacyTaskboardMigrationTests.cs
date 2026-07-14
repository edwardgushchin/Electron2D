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
using Electron2D.ProjectSystem;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class LegacyTaskboardMigrationTests
{
    [Fact]
    public void LegacyMigrationContractIsAvailableInProjectSystem()
    {
        var migrationType = typeof(ProjectTask).Assembly.GetType("Electron2D.ProjectSystem.LegacyTaskboardMigration");

        Assert.NotNull(migrationType);
    }

    [Fact]
    public void LegacyMigrationSegmentsOutsideFencesAndReconstructsExactSourceBytes()
    {
        var activeBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(
            "# Преамбула\r\n" +
            "## T-0001 [ ] P1: Активная задача\r\n" +
            "- Создана: 2026-07-01T10:00:00+03:00\r\n" +
            "- Зависимости: нет\r\n" +
            "```markdown\r\n" +
            "## T-9999 [ ] P0: Это вложенный снимок\r\n" +
            "```\r\n" +
            "## ROADMAP\r\n" +
            "- T-0001\r\n")).ToArray();
        var archiveBytes = Encoding.UTF8.GetBytes(
            "# Архив\n\n" +
            "# T-0002: Завершённая задача\n" +
            "- Статус: завершена\n" +
            "- Завершена: 2026-07-02T10:00:00+03:00\n");
        var sources = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["TASKS.md"] = activeBytes,
            ["data/completed-tasks/2026/07 Июль.md"] = archiveBytes
        };
        var migrationType = typeof(ProjectTask).Assembly.GetType("Electron2D.ProjectSystem.LegacyTaskboardMigration")!;
        var parse = migrationType.GetMethod("ParseSources", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(parse);
        var plan = parse.Invoke(null, [sources, "TASKS.md"]);
        Assert.NotNull(plan);
        var tasks = ((IEnumerable)plan.GetType().GetProperty("Tasks")!.GetValue(plan)!).Cast<object>().ToArray();
        Assert.Equal(2, tasks.Length);
        Assert.Equal(["T-0001", "T-0002"], tasks.Select(task => task.GetType().GetProperty("TaskId")!.GetValue(task)?.ToString() ?? string.Empty).ToArray());
        Assert.DoesNotContain(tasks, task => task.GetType().GetProperty("TaskId")!.GetValue(task)?.ToString() == "T-9999");

        var reconstruct = plan.GetType().GetMethod("ReconstructSource")!;
        Assert.Equal(activeBytes, (byte[])reconstruct.Invoke(plan, ["TASKS.md"])!);
        Assert.Equal(archiveBytes, (byte[])reconstruct.Invoke(plan, ["data/completed-tasks/2026/07 Июль.md"])!);
    }

    [Fact]
    public void FinalizedRepositoryV3TaskboardRetainsLegacyInventoryAndMigrationProvenance()
    {
        var root = FindRepositoryRoot();
        var snapshot = new TaskBoardV3DiskStore(root).Verify();
        var tasks = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).ToArray();
        var migratedTasks = tasks.Where(task => RequiredArray(task, "legacySourceFragments").Count > 0).ToArray();
        var taskUidById = tasks.ToDictionary(task => RequiredString(task, "taskId"), task => RequiredString(task, "taskUid"), StringComparer.Ordinal);
        var migration = RequiredObject(snapshot.Board, "migration");

        Assert.Equal(1143, migratedTasks.Length);
        Assert.Equal(1143, migratedTasks.Select(task => RequiredString(task, "taskId")).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(1143, migratedTasks.Sum(task => RequiredArray(task, "legacySourceFragments").Count));
        Assert.True(snapshot.ActiveTasks.Count >= 941);
        Assert.True(snapshot.CompletedTasks.Count >= 202);
        Assert.Equal(tasks.Length, tasks.Select(task => RequiredString(task, "taskUid")).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(8036, migratedTasks.Sum(DependsOnCount));
        Assert.DoesNotContain(taskUidById["T-0980"], DependsOnTargets(TaskById(tasks, "T-1015")));
        Assert.DoesNotContain(taskUidById["T-0980"], DependsOnTargets(TaskById(tasks, "T-1016")));
        Assert.Contains("T-0129", RequiredArray(TaskById(tasks, "T-0129-diary"), "legacyAliases").Select(node => node!.GetValue<string>()));
        Assert.Contains("T-0129", RequiredArray(TaskById(tasks, "T-0129-public-docs"), "legacyAliases").Select(node => node!.GetValue<string>()));
        Assert.Contains("T-0228", RequiredArray(TaskById(tasks, "T-0228-commit-policy"), "legacyAliases").Select(node => node!.GetValue<string>()));
        Assert.Equal(46, RequiredArray(snapshot.Board, "groups").Count);
        Assert.Equal(snapshot.ActiveTasks.Count, RequiredArray(snapshot.Board, "placements").Count);
        Assert.Equal(2, migration["sourceVersion"]!.GetValue<int>());
        Assert.Equal(".taskboard/.migration/v2/report.json", RequiredString(migration, "reportPath"));
        Assert.True(migration["finalized"]!.GetValue<bool>());
        Assert.NotEmpty(RequiredObject(migration, "sourceDigests"));
    }

    [Fact]
    public void FinalizedRepositoryV3TasksRemainSchemaValidWithParentDagAndAliases()
    {
        var root = FindRepositoryRoot();
        var snapshot = new TaskBoardV3DiskStore(root).Verify();
        var tasks = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).ToArray();
        var migratedTasks = tasks.Where(task => RequiredArray(task, "legacySourceFragments").Count > 0).ToArray();

        Assert.Equal(1143, migratedTasks.Length);
        Assert.Equal(1143, migratedTasks.Select(task => RequiredString(task, "taskUid")).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(16, migratedTasks.Count(task => task["parentTaskUid"] is not null));
        Assert.Equal(8036, migratedTasks.Sum(DependsOnCount));
        Assert.Equal(202, migratedTasks.Count(task => RequiredString(task, "status") == "Done"));
        Assert.Equal("Cancelled", RequiredString(TaskById(tasks, "T-0963"), "status"));
        Assert.All(tasks, task =>
        {
            Assert.Equal(3, task["version"]!.GetValue<int>());
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(task, "createdAt")));
            Assert.False(string.IsNullOrWhiteSpace(RequiredString(task, "description")));
            TaskBoardV3SchemaValidator.ValidateTask(task);
        });
    }

    [Fact]
    public void FinalizedRepositoryV3TasksKeepExecutionContractsCriteriaActivityAndLinks()
    {
        var root = FindRepositoryRoot();
        var snapshot = new TaskBoardV3DiskStore(root).Verify();
        var tasks = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).ToArray();
        var bootstrap = TaskById(tasks, "T-1147");
        var executionContract = RequiredObject(bootstrap, "executionContract");

        Assert.Equal("cross-cutting taskboard foundation, repository workflow migration and IDE integration.", RequiredString(executionContract, "taskType"));
        Assert.NotEmpty(RequiredArray(executionContract, "readyToStart"));
        Assert.NotEmpty(RequiredArray(executionContract, "stopConditions"));
        Assert.NotEmpty(RequiredArray(executionContract, "requiredOutputs"));
        Assert.NotEmpty(RequiredArray(executionContract, "commands"));
        Assert.Equal(8, RequiredArray(bootstrap, "acceptanceCriteria").Count);
        Assert.True(tasks.Sum(task => RequiredArray(task, "activity").Count + RequiredArray(RequiredObject(task, "conversation"), "messages").Count) >= 2045);
        Assert.True(tasks.Sum(task => RequiredArray(task, "links").Count) > 1000);
    }

    private static JsonObject TaskById(IEnumerable<JsonObject> tasks, string taskId)
    {
        return tasks.Single(task => string.Equals(RequiredString(task, "taskId"), taskId, StringComparison.Ordinal));
    }

    private static int DependsOnCount(JsonObject task)
    {
        return RequiredArray(task, "relations").Count(node =>
            string.Equals(RequiredString(node!.AsObject(), "kind"), "DependsOn", StringComparison.Ordinal));
    }

    private static IEnumerable<string> DependsOnTargets(JsonObject task)
    {
        return RequiredArray(task, "relations")
            .Select(node => node!.AsObject())
            .Where(relation => string.Equals(RequiredString(relation, "kind"), "DependsOn", StringComparison.Ordinal))
            .Select(relation => RequiredString(relation, "targetTaskUid"));
    }

    private static JsonArray RequiredArray(JsonObject source, string propertyName)
    {
        return Assert.IsType<JsonArray>(source[propertyName]);
    }

    private static JsonObject RequiredObject(JsonObject source, string propertyName)
    {
        return Assert.IsType<JsonObject>(source[propertyName]);
    }

    private static string RequiredString(JsonObject source, string propertyName)
    {
        return Assert.IsAssignableFrom<JsonValue>(source[propertyName]).GetValue<string>();
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
