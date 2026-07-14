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
using System.Text;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal sealed partial class TaskBoardDiskStore
{
    public TaskBoardV3BoardMutationResult FinalizeV3Migration(
        string expectedReportSha256,
        long expectedBoardRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReportSha256);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedBoardRevision, 1);
        var v3Store = new TaskBoardV3DiskStore(projectRoot);
        using var writeLock = AcquireWriteLock();
        RecoverTransactions();
        var snapshot = v3Store.LoadSnapshotUnderExistingWriteLock();
        TaskBoardV3SemanticValidator.Validate(projectRoot, snapshot.Board, snapshot.ActiveTasks, snapshot.CompletedTasks);
        var boardRevision = ReadInteger(snapshot.Board, "revision");
        if (boardRevision != expectedBoardRevision)
        {
            throw new InvalidOperationException($"Taskboard revision conflict: expected {expectedBoardRevision}, actual {boardRevision}.");
        }

        var migration = snapshot.Board["migration"] as JsonObject ??
            throw new InvalidOperationException("Native v3 board has no v2 migration to finalize.");
        if (migration["finalized"]?.GetValue<bool>() == true)
        {
            throw new InvalidOperationException("Taskboard v3 migration is already finalized.");
        }

        var reportSha = migration["reportSha256"]?.GetValue<string>();
        if (!string.Equals(reportSha, expectedReportSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Reviewed migration report SHA-256 does not match board provenance.");
        }

        var reportPath = migration["reportPath"]?.GetValue<string>() ??
            throw new InvalidOperationException("Migration report path is missing from board provenance.");
        var fullReportPath = FullPath(reportPath);
        if (!File.Exists(fullReportPath))
        {
            throw new InvalidOperationException($"Migration report '{reportPath}' is missing.");
        }

        var report = JsonNode.Parse(File.ReadAllText(fullReportPath)) as JsonObject ??
            throw new InvalidOperationException($"Migration report '{reportPath}' is not a JSON object.");
        if (!string.Equals(report["reportSha256"]?.GetValue<string>(), reportSha, StringComparison.Ordinal) ||
            !string.Equals(TaskBoardV3Migration.ComputeReportDigest(report), reportSha, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Migration report '{reportPath}' does not match its board provenance digest.");
        }

        var sourceDigests = migration["sourceDigests"] as JsonObject ??
            throw new InvalidOperationException("Migration source digests are missing.");
        var backupFiles = new List<string>();
        var oldBlobFiles = new List<string>();
        var currentBlobs = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks)
            .SelectMany(task => (task["attachments"] as JsonArray ?? []).Select(item => item!.AsObject()))
            .Select(attachment => attachment["relativePath"]!.GetValue<string>())
            .ToHashSet(StringComparer.Ordinal);
        foreach (var digest in sourceDigests.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var sourcePath = digest.Key;
            var expectedDigest = digest.Value?.GetValue<string>() ??
                throw new InvalidOperationException($"Migration digest for '{sourcePath}' is missing.");
            string verificationPath;
            if (sourcePath == ProjectTaskStorage.BoardDocumentPath ||
                sourcePath.StartsWith($"{ProjectTaskStorage.ActiveTasksDirectory}/", StringComparison.Ordinal) ||
                sourcePath.StartsWith($"{ProjectTaskStorage.CompletedTasksDirectory}/", StringComparison.Ordinal))
            {
                var suffix = sourcePath[(ProjectTaskStorage.RootDirectory.Length + 1)..];
                verificationPath = $"{ProjectTaskStorage.RootDirectory}/.migration/v2/{suffix}";
                backupFiles.Add(verificationPath);
            }
            else
            {
                verificationPath = sourcePath;
                if (sourcePath.StartsWith($"{ProjectTaskStorage.AttachmentsDirectory}/", StringComparison.Ordinal) &&
                    !currentBlobs.Contains(sourcePath))
                {
                    oldBlobFiles.Add(sourcePath);
                }
            }

            var fullPath = FullPath(verificationPath);
            if (!File.Exists(fullPath) || !string.Equals(HashFile(fullPath), expectedDigest, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Migration source snapshot '{verificationPath}' is missing or changed.");
            }
        }

        var board = snapshot.Board.DeepClone().AsObject();
        board["revision"] = boardRevision + 1;
        board["migration"]!["finalized"] = true;
        TaskBoardV3TransitionValidator.ValidateBoard(
            snapshot.Board,
            board,
            new TaskBoardV3MutationContext("cli", TaskBoardV3Capability.EditBoard | TaskBoardV3Capability.Migrate));
        TaskBoardV3SemanticValidator.Validate(projectRoot, board, snapshot.ActiveTasks, snapshot.CompletedTasks);
        var changes = new List<TaskBoardBinaryChange>
        {
            new(ProjectTaskStorage.BoardDocumentPath, new UTF8Encoding(false).GetBytes(TaskBoardV3Migration.Serialize(board)))
        };
        changes.AddRange(backupFiles.Concat(oldBlobFiles).Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new TaskBoardBinaryChange(path, null)));
        if (!dryRun)
        {
            ApplyBinaryTransaction(changes);
            DeleteEmptyMigrationDirectories();
        }

        return new TaskBoardV3BoardMutationResult(
            board,
            snapshot.ActiveTasks,
            snapshot.CompletedTasks,
            dryRun ? [] : changes.Select(change => change.Path).ToArray(),
            dryRun);
    }

    private void DeleteEmptyMigrationDirectories()
    {
        var root = FullPath($"{ProjectTaskStorage.RootDirectory}/.migration/v2");
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }

        if (!Directory.EnumerateFileSystemEntries(root).Any())
        {
            Directory.Delete(root);
        }
    }

    private static long ReadInteger(JsonObject value, string propertyName)
    {
        var node = value[propertyName] as JsonValue ?? throw new InvalidOperationException($"Integer '{propertyName}' is missing.");
        if (node.TryGetValue<long>(out var longValue))
        {
            return longValue;
        }

        if (node.TryGetValue<int>(out var intValue))
        {
            return intValue;
        }

        throw new InvalidOperationException($"Integer '{propertyName}' is invalid.");
    }
}
