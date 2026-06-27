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
using System.Diagnostics;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Electron2D.Build;

internal sealed class AuditPackageCommand(JsonDiagnosticSink diagnostics)
{
    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly DateTimeOffset DeterministicZipTimestamp = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Regex TaskIdPattern = new(@"\bT-\d{4}\b", RegexOptions.CultureInvariant);
    private static readonly Regex SecretValuePattern = new(
        @"(?im)(?:^|[^\p{L}\p{N}_-])[""']?(?:api[_-]?key|password|secret|token)[""']?\s*[:=]\s*[""']?(?<value>[^""'\s#][^#\r\n]*)",
        RegexOptions.CultureInvariant);
    private static readonly Regex PrivateKeyPattern = new(
        @"(?im)-----BEGIN [A-Z ]*PRIVATE\s+KEY-----|\bBEGIN\s+PRIVATE\s+KEY\b",
        RegexOptions.CultureInvariant);
    private static readonly Regex WindowsDrivePathPattern = new(
        @"(?i)\b[A-Z]:(?:\\|/)",
        RegexOptions.CultureInvariant);
    private static readonly string[] RedactedSecretPlaceholderValues =
    [
        "<redacted>",
        "<secret>",
        "<token>",
        "<password>",
        "<api-key>",
        "<api_key>",
        "<value>"
    ];

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length < 2 || args[1] != "package")
        {
            WriteError(
                "audit",
                "audit",
                "E2D-BUILD-CLI-INVALID-ARGUMENTS",
                "Expected: audit package ... or audit package verify ...");
            return RepositoryBuildExitCodes.Failed;
        }

        try
        {
            if (args.Length >= 3 && args[2] == "verify")
            {
                var options = ParseVerifyOptions(args);
                await VerifyPackageAsync(options, writeSuccessDiagnostic: true, cancellationToken).ConfigureAwait(false);
                return RepositoryBuildExitCodes.Success;
            }

            var packageOptions = ParsePackageOptions(args);
            await CreatePackageAsync(packageOptions, cancellationToken).ConfigureAwait(false);
            return RepositoryBuildExitCodes.Success;
        }
        catch (AuditPackageFailure failure)
        {
            WriteError("audit", failure.Step, failure.Code, failure.Message, force: failure.Force, zipPath: failure.ZipPath);
            return RepositoryBuildExitCodes.Failed;
        }
    }

    private async Task CreatePackageAsync(AuditPackageOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var repoRoot = Directory.GetCurrentDirectory();
        var config = await ReadConfigurationAsync(options.ConfigPath, cancellationToken).ConfigureAwait(false);
        NormalizeConfiguration(config);
        ValidateConfigurationAgainstOptions(config, options);

        var outputDirectory = ResolvePath(repoRoot, options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        var zipPath = Path.Combine(outputDirectory, $"{options.TaskId}-audit-{options.Iteration}.zip");

        if (File.Exists(zipPath))
        {
            if (!options.Force)
            {
                throw new AuditPackageFailure(
                    "audit package",
                    "E2D-BUILD-AUDIT-ZIP-EXISTS",
                    $"Target audit ZIP already exists: {zipPath}",
                    ZipPath: zipPath);
            }

            diagnostics.Write(new BuildDiagnostic(
                "audit",
                "audit package",
                "warning",
                "E2D-BUILD-AUDIT-FORCE-OVERWRITE",
                "Existing audit ZIP will be overwritten because --force was supplied.",
                ZipPath: zipPath,
                Force: true));
            File.Delete(zipPath);
        }

        using var staging = TemporaryWorkspace.Create("Electron2D-AuditPackage");
        var repoFiles = await SelectRepositoryFilesAsync(repoRoot, config, cancellationToken).ConfigureAwait(false);
        var importedEvidence = SelectArchiveOnlyEvidenceFiles(repoRoot, config);
        var checkEvidence = await RunConfiguredChecksAsync(repoRoot, staging.Root, config, cancellationToken).ConfigureAwait(false);
        var evidenceFiles = importedEvidence.Concat(checkEvidence).OrderBy(file => file.ArchivePath, StringComparer.Ordinal).ToArray();
        var firstSnapshot = await CreateInputSnapshotAsync(repoRoot, repoFiles, evidenceFiles, cancellationToken).ConfigureAwait(false);

        var patch = await CreatePatchAsync(repoRoot, staging.Root, options.Baseline, repoFiles, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(patch.PatchText))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-PATCH-EMPTY", "Selected repository files do not produce a patch.");
        }

        var restoreManifest = await CreateRestoreManifestAsync(repoRoot, config, repoFiles, cancellationToken).ConfigureAwait(false);
        var normalizedConfigJson = JsonSerializer.Serialize(config, JsonWriteOptions) + "\n";
        var requestText = CreateAuditRequest(config, $"{options.TaskId}-audit-{options.Iteration}.zip", patch.NameStatus);
        var archiveFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [$"{options.TaskId}.patch"] = Encoding.UTF8.GetBytes(patch.PatchText),
            ["repo-file-hashes.json"] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(restoreManifest, JsonWriteOptions) + "\n"),
            ["AUDIT-REQUEST.md"] = Encoding.UTF8.GetBytes(requestText),
            ["metadata/audit-package.input.json"] = Encoding.UTF8.GetBytes(normalizedConfigJson)
        };

        foreach (var evidence in evidenceFiles)
        {
            archiveFiles[evidence.ArchivePath] = await File.ReadAllBytesAsync(evidence.SourcePath, cancellationToken).ConfigureAwait(false);
        }

        var plannedPaths = archiveFiles.Keys
            .Concat(["AUDIT-MANIFEST.md", "SHA256SUMS.txt"])
            .Order(StringComparer.Ordinal)
            .ToArray();
        var manifest = CreateManifest(config, patch.NameStatus, restoreManifest, plannedPaths, evidenceFiles.Select(file => file.ArchivePath).ToArray());
        archiveFiles["AUDIT-MANIFEST.md"] = Encoding.UTF8.GetBytes(manifest);
        archiveFiles["SHA256SUMS.txt"] = CreateChecksumFile(archiveFiles);

        ValidateArchiveFiles(archiveFiles, repoRoot, SelectPreviousVerdictPaths(config));
        var secondSnapshot = await CreateInputSnapshotAsync(repoRoot, repoFiles, evidenceFiles, cancellationToken).ConfigureAwait(false);
        if (!SnapshotsEqual(firstSnapshot, secondSnapshot))
        {
            throw new AuditPackageFailure(
                "audit package",
                "E2D-BUILD-AUDIT-INPUT-MUTATED",
                "Repository files or evidence changed between configured checks and ZIP creation.");
        }

        WriteDeterministicZip(zipPath, archiveFiles);

        using var cleanRepo = await CreateCleanCloneAsync(repoRoot, options.Baseline, cancellationToken).ConfigureAwait(false);
        await VerifyPackageAsync(
            new AuditVerifyOptions(zipPath, options.Baseline, cleanRepo.Root),
            writeSuccessDiagnostic: false,
            cancellationToken).ConfigureAwait(false);

        diagnostics.Write(new BuildDiagnostic(
            "audit",
            "audit package",
            "info",
            "E2D-BUILD-AUDIT-PACKAGE-CREATED",
            $"Created and verified audit package '{Path.GetFileName(zipPath)}'.",
            ZipPath: zipPath,
            Force: options.Force ? true : null));
    }

    private async Task VerifyPackageAsync(AuditVerifyOptions options, bool writeSuccessDiagnostic, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(options.ZipPath))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-MISSING", $"Audit ZIP was not found: {options.ZipPath}");
        }

        if (!Directory.Exists(options.RepositoryPath))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-REPO-MISSING", $"Clean repository path was not found: {options.RepositoryPath}");
        }

        using var extractRoot = TemporaryWorkspace.Create("Electron2D-AuditVerify");
        var entries = ExtractAndReadZip(options.ZipPath, extractRoot.Root);
        VerifyArchiveRequiredFiles(entries);
        var config = ReadConfigurationFromBytes(entries["metadata/audit-package.input.json"]);
        NormalizeConfiguration(config);
        ValidateTaskId(config.TaskId, "audit package verify");
        ValidateIteration(config.Iteration, "audit package verify");
        ValidateBaseline(config.Baseline, "audit package verify");
        ValidateArchiveFiles(entries, options.RepositoryPath, SelectPreviousVerdictPaths(config));
        VerifyChecksums(entries);
        await VerifySha256SumToolAsync(extractRoot.Root, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(config.Baseline, options.Baseline, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-BASELINE-MISMATCH", "Archive baseline does not match --baseline.");
        }

        VerifyGeneratedTaskIds(config, entries);
        VerifyManifestInventory(entries);
        var patchName = $"{config.TaskId}.patch";
        if (!entries.ContainsKey(patchName))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-PATCH-MISSING", $"Archive does not contain {patchName}.");
        }

        VerifyPatchControlPaths(Encoding.UTF8.GetString(entries[patchName]));
        await VerifyCleanRepositoryAsync(options.RepositoryPath, options.Baseline, cancellationToken).ConfigureAwait(false);

        var patchPath = Path.Combine(extractRoot.Root, patchName);
        var applyCheck = await GitRunner.RunAsync(
            options.RepositoryPath,
            ["apply", "--check", patchPath],
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (applyCheck.ExitCode != 0)
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-PATCH-FAILED",
                $"git apply --check failed: {applyCheck.StandardError}");
        }

        var apply = await GitRunner.RunAsync(
            options.RepositoryPath,
            ["apply", patchPath],
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (apply.ExitCode != 0)
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-PATCH-FAILED",
                $"git apply failed: {apply.StandardError}");
        }

        var restoreManifest = JsonSerializer.Deserialize<RestoreManifest>(
            entries["repo-file-hashes.json"],
            JsonReadOptions) ?? throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-RESTORE-MANIFEST-INVALID",
                "repo-file-hashes.json is empty or invalid.");
        VerifyRestoreManifestMetadata(config, restoreManifest);
        await VerifyRestoredFileSetAsync(options.RepositoryPath, restoreManifest, cancellationToken).ConfigureAwait(false);
        await VerifyRestoredHashesAsync(options.RepositoryPath, restoreManifest, cancellationToken).ConfigureAwait(false);
        await VerifyOneLineStubsAsync(options.RepositoryPath, config, restoreManifest, cancellationToken).ConfigureAwait(false);

        if (writeSuccessDiagnostic)
        {
            diagnostics.Write(new BuildDiagnostic(
                "audit",
                "audit package verify",
                "info",
                "E2D-BUILD-AUDIT-PACKAGE-VERIFIED",
                $"Verified audit package '{Path.GetFileName(options.ZipPath)}'.",
                ZipPath: options.ZipPath));
        }
    }

    private static AuditPackageOptions ParsePackageOptions(string[] args)
    {
        var values = ParseNamedArguments(args, startIndex: 2, allowedValueOptions: ["--task", "--iteration", "--baseline", "--config", "--out"], allowedFlags: ["--force"]);
        var taskId = Require(values, "--task", "audit package");
        return new AuditPackageOptions(
            taskId,
            Require(values, "--iteration", "audit package"),
            Require(values, "--baseline", "audit package"),
            values.TryGetValue("--config", out var configPath) && !string.IsNullOrWhiteSpace(configPath)
                ? configPath
                : $".temp/audit/{taskId}/audit-package.input.json",
            Require(values, "--out", "audit package"),
            values.ContainsKey("--force"));
    }

    private static AuditVerifyOptions ParseVerifyOptions(string[] args)
    {
        var values = ParseNamedArguments(args, startIndex: 3, allowedValueOptions: ["--zip", "--baseline", "--repo"], allowedFlags: []);
        return new AuditVerifyOptions(
            Require(values, "--zip", "audit package verify"),
            Require(values, "--baseline", "audit package verify"),
            Require(values, "--repo", "audit package verify"));
    }

    private static Dictionary<string, string?> ParseNamedArguments(
        string[] args,
        int startIndex,
        string[] allowedValueOptions,
        string[] allowedFlags)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var valueOptions = allowedValueOptions.ToHashSet(StringComparer.Ordinal);
        var flags = allowedFlags.ToHashSet(StringComparer.Ordinal);

        for (var i = startIndex; i < args.Length; i++)
        {
            var current = args[i];
            if (flags.Contains(current))
            {
                if (!values.TryAdd(current, null))
                {
                    throw new AuditPackageFailure("audit", "E2D-BUILD-CLI-INVALID-ARGUMENTS", $"Duplicate option: {current}");
                }

                continue;
            }

            if (!valueOptions.Contains(current))
            {
                throw new AuditPackageFailure("audit", "E2D-BUILD-CLI-INVALID-ARGUMENTS", $"Unexpected option: {current}");
            }

            if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit", "E2D-BUILD-CLI-INVALID-ARGUMENTS", $"Missing value for {current}.");
            }

            if (!values.TryAdd(current, args[i + 1]))
            {
                throw new AuditPackageFailure("audit", "E2D-BUILD-CLI-INVALID-ARGUMENTS", $"Duplicate option: {current}");
            }

            i++;
        }

        return values;
    }

    private static string Require(Dictionary<string, string?> values, string option, string step)
    {
        if (!values.TryGetValue(option, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-CLI-INVALID-ARGUMENTS", $"Missing required option {option}.");
        }

        return value;
    }

    private static async Task<AuditPackageConfiguration> ReadConfigurationAsync(string configPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(configPath))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-MISSING", $"Audit package config was not found: {configPath}");
        }

        await using var stream = File.OpenRead(configPath);
        var config = await JsonSerializer.DeserializeAsync<AuditPackageConfiguration>(stream, JsonReadOptions, cancellationToken).ConfigureAwait(false);
        return config ?? throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-INVALID", "Audit package config is empty or invalid.");
    }

    private static AuditPackageConfiguration ReadConfigurationFromBytes(byte[] bytes)
    {
        return JsonSerializer.Deserialize<AuditPackageConfiguration>(bytes, JsonReadOptions)
            ?? throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-CONFIG-INVALID", "Archive config is empty or invalid.");
    }

    private static void NormalizeConfiguration(AuditPackageConfiguration config)
    {
        config.TaskId ??= string.Empty;
        config.Iteration ??= string.Empty;
        config.Baseline ??= string.Empty;
        config.Branch ??= string.Empty;
        config.Domain ??= string.Empty;
        config.RepoFileGlobs ??= [];
        config.RepoFileAllowlist ??= [];
        config.ArchiveOnlyEvidenceGlobs ??= [];
        config.Checks ??= [];
        config.ForbiddenPatterns ??= [];
        config.SecretScanPolicy ??= string.Empty;
        config.OutputDirectory ??= string.Empty;
        config.PreviousVerdictChain ??= [];
        config.BlockerClosureList ??= [];
        config.OneLineStubAllowlist ??= [];

        foreach (var check in config.Checks)
        {
            check.Name ??= string.Empty;
            check.FileName ??= string.Empty;
            check.Arguments ??= [];
            check.Cwd ??= ".";
            check.EnvAllowlist ??= [];
            check.TrxGlobs ??= [];
        }
    }

    private static void ValidateConfigurationAgainstOptions(AuditPackageConfiguration config, AuditPackageOptions options)
    {
        ValidateTaskId(config.TaskId, "audit package");
        ValidateIteration(config.Iteration, "audit package");
        ValidateBaseline(config.Baseline, "audit package");
        if (!string.Equals(config.TaskId, options.TaskId, StringComparison.Ordinal) ||
            !string.Equals(config.Iteration, options.Iteration, StringComparison.Ordinal) ||
            !string.Equals(config.Baseline, options.Baseline, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-MISMATCH", "CLI task, iteration, or baseline does not match the config.");
        }

        if (string.IsNullOrWhiteSpace(config.Branch) ||
            string.IsNullOrWhiteSpace(config.Domain) ||
            string.IsNullOrWhiteSpace(config.OutputDirectory) ||
            string.IsNullOrWhiteSpace(config.SecretScanPolicy) ||
            config.MaxFileSize <= 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-INVALID", "Audit config is missing required metadata fields.");
        }

        if (!string.Equals(config.SecretScanPolicy, "basic", StringComparison.OrdinalIgnoreCase))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-INVALID", "Unsupported secretScanPolicy. Supported value: basic.");
        }

        var configOut = AuditPath.NormalizeRelativePath(config.OutputDirectory, "outputDirectory", allowCurrentDirectory: false);
        var cliOut = AuditPath.NormalizeRelativePath(options.OutputDirectory, "--out", allowCurrentDirectory: false);
        if (!string.Equals(configOut, cliOut, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-MISMATCH", "Config outputDirectory must match --out.");
        }

        ValidateConfiguredPathList(config.RepoFileGlobs, "repoFileGlobs");
        ValidateConfiguredPathList(config.RepoFileAllowlist, "repoFileAllowlist");
        ValidateConfiguredPathList(config.ArchiveOnlyEvidenceGlobs, "archiveOnlyEvidenceGlobs");
        ValidateConfiguredPathList(config.PreviousVerdictChain, "previousVerdictChain");
        ValidateConfiguredPathList(config.ForbiddenPatterns, "forbiddenPatterns");
        ValidateConfiguredPathList(config.OneLineStubAllowlist, "oneLineStubAllowlist");
        foreach (var check in config.Checks)
        {
            ValidateCheck(check);
        }
    }

    private static void ValidateConfiguredPathList(IEnumerable<string> paths, string fieldName)
    {
        foreach (var path in paths)
        {
            AuditPath.ValidatePattern(path, fieldName);
        }
    }

    private static void ValidateCheck(AuditCheckConfiguration check)
    {
        if (string.IsNullOrWhiteSpace(check.Name) ||
            check.Name.Any(ch => char.IsControl(ch) || ch is '/' or '\\') ||
            string.IsNullOrWhiteSpace(check.FileName) ||
            check.TimeoutSeconds <= 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-INVALID", "Check configuration is invalid.");
        }

        AuditPath.NormalizeRelativePath(check.Cwd, "checks.cwd", allowCurrentDirectory: true);
        foreach (var trxGlob in check.TrxGlobs)
        {
            AuditPath.ValidatePattern(trxGlob, "checks.trxGlobs");
        }
    }

    private static void ValidateTaskId(string taskId, string step)
    {
        if (!TaskIdPattern.IsMatch(taskId) || TaskIdPattern.Match(taskId).Value != taskId)
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-TASK-ID-INVALID", "Task id must match T-NNNN.");
        }
    }

    private static void ValidateIteration(string iteration, string step)
    {
        if (!Regex.IsMatch(iteration, @"^r\d{2}$", RegexOptions.CultureInvariant))
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-CONFIG-INVALID", "Iteration must match rNN.");
        }
    }

    private static void ValidateBaseline(string baseline, string step)
    {
        if (!Regex.IsMatch(baseline, @"\A[0-9a-fA-F]{7,64}\z", RegexOptions.CultureInvariant))
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-CONFIG-INVALID", "Baseline must be a Git SHA.");
        }
    }

    private static async Task<SelectedRepositoryFile[]> SelectRepositoryFilesAsync(
        string repoRoot,
        AuditPackageConfiguration config,
        CancellationToken cancellationToken)
    {
        var tracked = await GitListAsync(repoRoot, ["ls-files", "-z"], cancellationToken).ConfigureAwait(false);
        var untracked = await GitListAsync(repoRoot, ["ls-files", "-z", "--others", "--exclude-standard"], cancellationToken).ConfigureAwait(false);
        var baseline = await GitListAsync(repoRoot, ["ls-tree", "-r", "--name-only", "-z", config.Baseline], cancellationToken).ConfigureAwait(false);
        var all = tracked.Concat(untracked).Concat(baseline).ToHashSet(StringComparer.Ordinal);
        foreach (var path in config.RepoFileAllowlist)
        {
            all.Add(AuditPath.NormalizeRelativePath(path, "repoFileAllowlist", allowCurrentDirectory: false));
        }

        var existingPreviousVerdicts = SelectExistingPreviousVerdicts(repoRoot, config).ToHashSet(StringComparer.Ordinal);
        foreach (var path in existingPreviousVerdicts)
        {
            all.Add(path);
        }

        var selected = all
            .Where(path =>
                MatchesAny(config.RepoFileGlobs, path) ||
                config.RepoFileAllowlist.Contains(path, StringComparer.Ordinal) ||
                existingPreviousVerdicts.Contains(path))
            .Order(StringComparer.Ordinal)
            .Select(path => CreateSelectedRepositoryFile(repoRoot, path, baseline.Contains(path)))
            .ToArray();

        if (selected.Length == 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-NO-REPO-FILES", "No repository files matched repoFileGlobs or repoFileAllowlist.");
        }

        foreach (var file in selected)
        {
            ValidateInputPath(file.Path, config);
            if (!file.Exists && !file.ExistsInBaseline)
            {
                throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-INPUT-MISSING", $"Configured repository file does not exist in working tree or baseline: {file.Path}");
            }

            if (file.Exists)
            {
                ValidateInputFileSize(file.AbsolutePath, file.Path, config.MaxFileSize);
                ValidateSecretPolicy(file.AbsolutePath, file.Path, config.SecretScanPolicy);
            }
        }

        return selected;
    }

    private static IEnumerable<string> SelectExistingPreviousVerdicts(string repoRoot, AuditPackageConfiguration config)
    {
        foreach (var normalized in SelectPreviousVerdictPaths(config))
        {
            var absolutePath = Path.Combine(repoRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath))
            {
                yield return normalized;
            }
        }
    }

    private static HashSet<string> SelectPreviousVerdictPaths(AuditPackageConfiguration config)
    {
        return config.PreviousVerdictChain
            .Select(path => AuditPath.NormalizeRelativePath(path, "previousVerdictChain", allowCurrentDirectory: false))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static SelectedRepositoryFile CreateSelectedRepositoryFile(string repoRoot, string path, bool existsInBaseline)
    {
        var absolutePath = Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar));
        return new SelectedRepositoryFile(path, absolutePath, File.Exists(absolutePath), existsInBaseline);
    }

    private static async Task<string[]> GitListAsync(string repoRoot, string[] arguments, CancellationToken cancellationToken)
    {
        var result = await GitRunner.RunAsync(repoRoot, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git {string.Join(" ", arguments)} failed: {result.StandardError}");
        }

        return result.StandardOutput
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    private static EvidenceSourceFile[] SelectArchiveOnlyEvidenceFiles(string repoRoot, AuditPackageConfiguration config)
    {
        var allFiles = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .Where(path => !path.StartsWith(".git/", StringComparison.Ordinal) &&
                !path.StartsWith("bin/", StringComparison.Ordinal) &&
                !path.Contains("/bin/", StringComparison.Ordinal) &&
                !path.StartsWith("obj/", StringComparison.Ordinal) &&
                !path.Contains("/obj/", StringComparison.Ordinal))
            .ToArray();
        var selected = allFiles
            .Where(path => MatchesAny(config.ArchiveOnlyEvidenceGlobs, path))
            .Order(StringComparer.Ordinal)
            .Select(path =>
            {
                ValidateInputPath(path, config);
                var absolutePath = Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar));
                ValidateInputFileSize(absolutePath, path, config.MaxFileSize);
                ValidateSecretPolicy(absolutePath, path, config.SecretScanPolicy);
                return new EvidenceSourceFile(
                    absolutePath,
                    $"evidence/{config.TaskId}-{config.Iteration}/archive-only/{path}");
            })
            .ToArray();

        return selected;
    }

    private static async Task<EvidenceSourceFile[]> RunConfiguredChecksAsync(
        string repoRoot,
        string stagingRoot,
        AuditPackageConfiguration config,
        CancellationToken cancellationToken)
    {
        var evidence = new List<EvidenceSourceFile>();
        foreach (var check in config.Checks)
        {
            var relativeCwd = AuditPath.NormalizeRelativePath(check.Cwd, "checks.cwd", allowCurrentDirectory: true);
            var workingDirectory = relativeCwd == "."
                ? repoRoot
                : Path.Combine(repoRoot, relativeCwd.Replace('/', Path.DirectorySeparatorChar));
            var archiveRoot = $"evidence/{config.TaskId}-{config.Iteration}/checks/{check.Name}";
            var localRoot = Path.Combine(stagingRoot, archiveRoot.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(localRoot);
            var result = await AuditProcessRunner.RunAsync(
                check.FileName,
                check.Arguments,
                workingDirectory,
                TimeSpan.FromSeconds(check.TimeoutSeconds),
                cancellationToken).ConfigureAwait(false);

            var env = check.EnvAllowlist
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToDictionary(name => name, name => Environment.GetEnvironmentVariable(name) ?? string.Empty, StringComparer.Ordinal);
            WriteText(localRoot, "command.txt", FormatCommandEvidence(check.FileName, check.Arguments));
            WriteText(localRoot, "cwd.txt", relativeCwd);
            WriteText(localRoot, "env.json", JsonSerializer.Serialize(env, JsonWriteOptions) + "\n");
            WriteText(localRoot, "timeout-seconds.txt", check.TimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n");
            WriteText(localRoot, "exit-code.txt", $"expected: {check.ExpectedExitCode}\nactual: {result.ExitCode}\n");
            WriteText(localRoot, "duration-ms.txt", "0\n");
            var copiedTrxFiles = CopyTrxFiles(repoRoot, localRoot, archiveRoot, config, check.TrxGlobs);
            var sanitizedStdout = SanitizeCheckOutput(result.StandardOutput, repoRoot, copiedTrxFiles);
            var sanitizedStderr = SanitizeCheckOutput(result.StandardError, repoRoot, copiedTrxFiles);
            WriteText(localRoot, "stdout.txt", sanitizedStdout);
            WriteText(localRoot, "stderr.txt", sanitizedStderr);

            var trxFiles = copiedTrxFiles
                .Select(file => file.Metadata)
                .ToArray();
            var metadata = new CheckEvidenceMetadata(
                check.Name,
                check.FileName,
                check.Arguments,
                relativeCwd,
                check.ExpectedExitCode,
                check.TimeoutSeconds,
                result.ExitCode,
                0,
                $"{archiveRoot}/stdout.txt",
                $"{archiveRoot}/stderr.txt",
                Sha256File(Path.Combine(localRoot, "stdout.txt")),
                Sha256File(Path.Combine(localRoot, "stderr.txt")),
                trxFiles);
            WriteText(localRoot, "metadata.json", JsonSerializer.Serialize(metadata, JsonWriteOptions) + "\n");

            if (result.ExitCode != check.ExpectedExitCode)
            {
                throw new AuditPackageFailure(
                    "audit package",
                    "E2D-BUILD-AUDIT-CHECK-FAILED",
                    $"Check '{check.Name}' exited with {result.ExitCode}; expected {check.ExpectedExitCode}.");
            }

            evidence.AddRange(Directory.EnumerateFiles(localRoot, "*", SearchOption.AllDirectories)
                .Select(path => new EvidenceSourceFile(
                    path,
                    $"{archiveRoot}/{Path.GetRelativePath(localRoot, path).Replace('\\', '/')}")));
        }

        return evidence.OrderBy(file => file.ArchivePath, StringComparer.Ordinal).ToArray();
    }

    private static CopiedTrxEvidenceFile[] CopyTrxFiles(
        string repoRoot,
        string localRoot,
        string archiveRoot,
        AuditPackageConfiguration config,
        IEnumerable<string> trxGlobs)
    {
        var copied = new List<CopiedTrxEvidenceFile>();
        foreach (var trxPath in Directory.EnumerateFiles(repoRoot, "*.trx", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .Where(path => MatchesAny(trxGlobs, path))
            .Order(StringComparer.Ordinal))
        {
            ValidateInputPath(trxPath, config);
            var source = Path.Combine(repoRoot, trxPath.Replace('/', Path.DirectorySeparatorChar));
            ValidateInputFileSize(source, trxPath, config.MaxFileSize);
            var trxBytes = ReadTrxEvidenceBytes(source, trxPath, config.SecretScanPolicy, repoRoot);
            var archivePath = $"{archiveRoot}/trx/test-result-{copied.Count + 1:000}.trx";
            var localPath = Path.Combine(localRoot, "trx", $"test-result-{copied.Count + 1:000}.trx");
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            File.WriteAllBytes(localPath, trxBytes);
            copied.Add(new CopiedTrxEvidenceFile(trxPath, source, new TrxEvidenceFile(archivePath, Sha256File(localPath))));
        }

        return copied.ToArray();
    }

    private static string SanitizeCheckOutput(string text, string repoRoot, IEnumerable<CopiedTrxEvidenceFile> trxFiles)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sanitized = text;
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var trxFile in trxFiles)
        {
            var sourceRelativePath = trxFile.SourceRelativePath;
            var sourceAbsolutePath = trxFile.SourceAbsolutePath;
            var archivePath = trxFile.Metadata.Path;
            var archiveFileName = Path.GetFileName(archivePath);
            replacements[sourceAbsolutePath] = archivePath;
            replacements[sourceAbsolutePath.Replace('\\', '/')] = archivePath;
            replacements[Path.Combine(repoRoot, sourceRelativePath.Replace('/', Path.DirectorySeparatorChar))] = archivePath;
            replacements[sourceRelativePath] = archivePath;
            replacements[sourceRelativePath.Replace('/', '\\')] = archivePath;
            replacements[Path.GetFileName(sourceRelativePath)] = archiveFileName;
        }

        foreach (var replacement in replacements
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .OrderByDescending(pair => pair.Key.Length))
        {
            sanitized = sanitized.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
        }

        return sanitized;
    }

    private static async Task<Dictionary<string, string>> CreateInputSnapshotAsync(
        string repoRoot,
        IEnumerable<SelectedRepositoryFile> repoFiles,
        IEnumerable<EvidenceSourceFile> evidenceFiles,
        CancellationToken cancellationToken)
    {
        var snapshot = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in repoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            snapshot[$"repo:{file.Path}"] = File.Exists(file.AbsolutePath)
                ? await Sha256FileAsync(file.AbsolutePath, cancellationToken).ConfigureAwait(false)
                : "<deleted>";
        }

        foreach (var evidence in evidenceFiles.OrderBy(file => file.ArchivePath, StringComparer.Ordinal))
        {
            snapshot[$"evidence:{evidence.ArchivePath}"] = await Sha256FileAsync(evidence.SourcePath, cancellationToken).ConfigureAwait(false);
        }

        return snapshot;
    }

    private static bool SnapshotsEqual(Dictionary<string, string> left, Dictionary<string, string> right)
    {
        return left.Count == right.Count &&
            left.All(pair => right.TryGetValue(pair.Key, out var value) && string.Equals(pair.Value, value, StringComparison.Ordinal));
    }

    private static async Task<PatchResult> CreatePatchAsync(
        string repoRoot,
        string stagingRoot,
        string baseline,
        SelectedRepositoryFile[] repoFiles,
        CancellationToken cancellationToken)
    {
        var tempIndex = Path.Combine(stagingRoot, "git-index");
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GIT_INDEX_FILE"] = tempIndex
        };
        await RunGitOrThrowAsync(repoRoot, ["read-tree", baseline], env, "audit package", cancellationToken).ConfigureAwait(false);
        foreach (var file in repoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            if (file.Exists)
            {
                await AddNormalizedFileToTemporaryIndexAsync(repoRoot, stagingRoot, baseline, file, env, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await RunGitOrThrowAsync(repoRoot, ["rm", "--cached", "--ignore-unmatch", "--", file.Path], env, "audit package", cancellationToken).ConfigureAwait(false);
            }
        }

        var patch = await GitRunner.RunAsync(
            repoRoot,
            ["diff", "--cached", "--binary", "--full-index", "--no-ext-diff", baseline, "--"],
            env,
            cancellationToken).ConfigureAwait(false);
        if (patch.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git diff failed: {patch.StandardError}");
        }

        var nameStatus = await GitRunner.RunAsync(
            repoRoot,
            ["diff", "--cached", "--name-status", baseline, "--"],
            env,
            cancellationToken).ConfigureAwait(false);
        if (nameStatus.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git diff --name-status failed: {nameStatus.StandardError}");
        }

        VerifyPatchControlPaths(patch.StandardOutput);
        return new PatchResult(patch.StandardOutput, nameStatus.StandardOutput);
    }

    private static async Task AddNormalizedFileToTemporaryIndexAsync(
        string repoRoot,
        string stagingRoot,
        string baseline,
        SelectedRepositoryFile file,
        Dictionary<string, string> environment,
        CancellationToken cancellationToken)
    {
        var bytes = ReadRestorableFileBytes(file.AbsolutePath);
        var blobPath = Path.Combine(stagingRoot, "repo-blobs", file.Path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(blobPath)!);
        await File.WriteAllBytesAsync(blobPath, bytes, cancellationToken).ConfigureAwait(false);

        var hash = await GitRunner.RunAsync(
            repoRoot,
            ["hash-object", "-w", "--", blobPath],
            environment,
            cancellationToken).ConfigureAwait(false);
        if (hash.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git hash-object failed: {hash.StandardError}");
        }

        var mode = await GetRepositoryFileModeAsync(repoRoot, baseline, file.Path, file.ExistsInBaseline, cancellationToken).ConfigureAwait(false);
        await RunGitOrThrowAsync(
            repoRoot,
            ["update-index", "--add", "--cacheinfo", mode, hash.StandardOutput.Trim(), file.Path],
            environment,
            "audit package",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> GetRepositoryFileModeAsync(
        string repoRoot,
        string baseline,
        string relativePath,
        bool existsInBaseline,
        CancellationToken cancellationToken)
    {
        var current = await GitRunner.RunAsync(repoRoot, ["ls-files", "-s", "--", relativePath], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (current.ExitCode == 0 && TryParseGitMode(current.StandardOutput, out var currentMode))
        {
            return currentMode;
        }

        if (existsInBaseline)
        {
            var baselineEntry = await GitRunner.RunAsync(repoRoot, ["ls-tree", baseline, "--", relativePath], cancellationToken: cancellationToken).ConfigureAwait(false);
            if (baselineEntry.ExitCode == 0 && TryParseGitMode(baselineEntry.StandardOutput, out var baselineMode))
            {
                return baselineMode;
            }
        }

        return "100644";
    }

    private static bool TryParseGitMode(string text, out string mode)
    {
        var trimmed = text.TrimStart();
        var separator = trimmed.IndexOfAny([' ', '\t']);
        if (separator > 0)
        {
            mode = trimmed[..separator];
            return Regex.IsMatch(mode, @"\A[0-7]{6}\z", RegexOptions.CultureInvariant);
        }

        mode = string.Empty;
        return false;
    }

    private static async Task RunGitOrThrowAsync(
        string workingDirectory,
        string[] arguments,
        Dictionary<string, string> environment,
        string step,
        CancellationToken cancellationToken)
    {
        var result = await GitRunner.RunAsync(workingDirectory, arguments, environment, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-GIT-FAILED", $"git {string.Join(" ", arguments)} failed: {result.StandardError}");
        }
    }

    private static async Task<RestoreManifest> CreateRestoreManifestAsync(
        string repoRoot,
        AuditPackageConfiguration config,
        SelectedRepositoryFile[] repoFiles,
        CancellationToken cancellationToken)
    {
        var existing = new List<RestoreFileHash>();
        var deleted = new List<string>();
        foreach (var file in repoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            if (file.Exists)
            {
                existing.Add(new RestoreFileHash(file.Path, Sha256Bytes(ReadRestorableFileBytes(file.AbsolutePath))));
            }
            else
            {
                deleted.Add(file.Path);
            }
        }

        return new RestoreManifest(config.TaskId, config.Iteration, config.Baseline, existing, deleted);
    }

    private static string CreateAuditRequest(AuditPackageConfiguration config, string zipName, string nameStatus)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {config.TaskId} audit {config.Iteration}");
        builder.AppendLine();
        builder.AppendLine($"- Archive: `{zipName}`");
        builder.AppendLine($"- Baseline: `{config.Baseline}`");
        builder.AppendLine($"- Branch: `{config.Branch}`");
        builder.AppendLine($"- Domain: `{config.Domain}`");
        builder.AppendLine();
        builder.AppendLine("## Previous Verdict Chain");
        AppendList(builder, config.PreviousVerdictChain);
        builder.AppendLine();
        builder.AppendLine("## Blocker Closure List");
        AppendList(builder, config.BlockerClosureList);
        builder.AppendLine();
        builder.AppendLine("## Restore Model");
        builder.AppendLine("- Verify `SHA256SUMS.txt`.");
        builder.AppendLine("- Apply repository patch with `git apply --check` and `git apply` from baseline.");
        builder.AppendLine("- Compare restored files with `repo-file-hashes.json`.");
        builder.AppendLine("- Treat `evidence/` as archive-only evidence.");
        builder.AppendLine();
        builder.AppendLine("## Diff Name-Status");
        builder.AppendLine("```text");
        builder.Append(nameStatus);
        if (!nameStatus.EndsWith('\n'))
        {
            builder.AppendLine();
        }

        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        AppendList(builder, config.Checks.Select(check => $"{check.Name}: expected exit code {check.ExpectedExitCode}"));
        builder.AppendLine();
        builder.AppendLine("## Required Response");
        builder.AppendLine("Use one of these exact verdict headers:");
        builder.AppendLine("- `VERDICT: ACCEPT`");
        builder.AppendLine("- `VERDICT: NEEDS_FIXES`");
        builder.AppendLine();
        builder.AppendLine("For every blocker include criterion, evidence, fix, and reproduction command.");
        return builder.ToString();
    }

    private static string CreateManifest(
        AuditPackageConfiguration config,
        string nameStatus,
        RestoreManifest restoreManifest,
        string[] archivePaths,
        string[] evidencePaths)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AUDIT-MANIFEST");
        builder.AppendLine();
        builder.AppendLine("## Metadata");
        builder.AppendLine($"- taskId: `{config.TaskId}`");
        builder.AppendLine($"- iteration: `{config.Iteration}`");
        builder.AppendLine($"- baseline: `{config.Baseline}`");
        builder.AppendLine($"- branch: `{config.Branch}`");
        builder.AppendLine($"- domain: `{config.Domain}`");
        builder.AppendLine();
        builder.AppendLine("## Diff Name-Status");
        builder.AppendLine("```text");
        builder.Append(nameStatus);
        if (!nameStatus.EndsWith('\n'))
        {
            builder.AppendLine();
        }

        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Archive Inventory");
        foreach (var path in archivePaths.Order(StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{path}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Repository File Inventory");
        foreach (var file in restoreManifest.RepoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{file.Path}` `{file.Sha256}`");
        }

        foreach (var path in restoreManifest.DeletedRepoFiles.OrderBy(path => path, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{path}` `<deleted>`");
        }

        builder.AppendLine();
        builder.AppendLine("## Restore Model");
        builder.AppendLine("- `SHA256SUMS.txt` covers all archive files except itself.");
        builder.AppendLine($"- `{config.TaskId}.patch` restores repository-owned files from baseline.");
        builder.AppendLine("- `repo-file-hashes.json` contains expected restored file hashes.");
        builder.AppendLine("- `evidence/` files are archive-only and are not applied to the repository.");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        foreach (var check in config.Checks.OrderBy(check => check.Name, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{check.Name}` expected exit code `{check.ExpectedExitCode}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Evidence Links");
        foreach (var path in evidencePaths.Order(StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{path}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Forbidden Policy");
        builder.AppendLine("- `.git`, `bin`, `obj`, temp paths, nested archives, `.env`, secrets and tokens are forbidden.");
        foreach (var pattern in config.ForbiddenPatterns.Order(StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{pattern}`");
        }

        return builder.ToString();
    }

    private static void AppendList(StringBuilder builder, IEnumerable<string> items)
    {
        var any = false;
        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
            any = true;
        }

        if (!any)
        {
            builder.AppendLine("- none");
        }
    }

    private static void ValidateArchiveFiles(
        Dictionary<string, byte[]> archiveFiles,
        string repoRoot,
        ISet<string> previousVerdictPaths)
    {
        foreach (var path in archiveFiles.Keys)
        {
            AuditPath.NormalizeArchivePath(path);
            if (ForbiddenPathPolicy.IsForbidden(path))
            {
                throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-FORBIDDEN-PATH", $"Forbidden archive path: {path}");
            }

            ValidateArchiveContent(path, archiveFiles[path], repoRoot, previousVerdictPaths);
        }
    }

    private static void WriteDeterministicZip(string zipPath, Dictionary<string, byte[]> archiveFiles)
    {
        using var stream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        foreach (var file in archiveFiles.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var entry = archive.CreateEntry(file.Key, CompressionLevel.SmallestSize);
            entry.LastWriteTime = DeterministicZipTimestamp;
            entry.ExternalAttributes = 0;
            using var entryStream = entry.Open();
            entryStream.Write(file.Value);
        }
    }

    private static byte[] CreateChecksumFile(Dictionary<string, byte[]> archiveFiles)
    {
        var builder = new StringBuilder();
        foreach (var file in archiveFiles
            .Where(pair => pair.Key != "SHA256SUMS.txt")
            .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            builder.Append(Sha256Bytes(file.Value));
            builder.Append("  ");
            builder.Append(file.Key);
            builder.Append('\n');
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static Dictionary<string, byte[]> ExtractAndReadZip(string zipPath, string extractRoot)
    {
        var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var centralDirectory = ReadZipCentralDirectory(zipPath);
        using var zipStream = File.OpenRead(zipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        string? previousPath = null;
        var entryIndex = 0;
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-PATH-INVALID", $"Directory archive entries are not allowed: {entry.FullName}");
            }

            var normalized = AuditPath.NormalizeArchivePath(entry.FullName);
            if (entryIndex >= centralDirectory.Length)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", "Archive central directory does not match entry inventory.");
            }

            var centralEntry = centralDirectory[entryIndex++];
            if (!string.Equals(centralEntry.Name, normalized, StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", $"Archive central directory path mismatch: {normalized}");
            }

            if (centralEntry.ExternalAttributes != 0)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", $"Archive entry has platform external attributes: {normalized}");
            }

            if (centralEntry.NameContainsNonAsciiBytes && !centralEntry.HasUtf8Flag)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", $"Archive entry with non-ASCII path is missing the UTF-8 flag: {normalized}");
            }

            if (previousPath is not null && string.CompareOrdinal(previousPath, normalized) >= 0)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", "Archive entries must be sorted by POSIX path.");
            }

            previousPath = normalized;
            if (entry.LastWriteTime.DateTime != DeterministicZipTimestamp.DateTime)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", $"Archive entry timestamp is not deterministic: {normalized}");
            }

            if (ForbiddenPathPolicy.IsForbidden(normalized))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-FORBIDDEN-PATH", $"Forbidden archive path: {normalized}");
            }

            if (!entries.TryAdd(normalized, ReadZipEntryBytes(entry)))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-PATH-INVALID", $"Duplicate archive path: {normalized}");
            }
        }

        if (entryIndex != centralDirectory.Length)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", "Archive central directory contains entries that were not read.");
        }

        foreach (var entry in entries)
        {
            var outputPath = Path.Combine(extractRoot, entry.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllBytes(outputPath, entry.Value);
        }

        return entries;
    }

    private static ZipCentralDirectoryEntry[] ReadZipCentralDirectory(string zipPath)
    {
        var bytes = File.ReadAllBytes(zipPath);
        var eocdOffset = FindEndOfCentralDirectory(bytes);
        var entryCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(eocdOffset + 10, 2));
        var centralDirectoryOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(eocdOffset + 16, 4));
        var entries = new ZipCentralDirectoryEntry[entryCount];
        var offset = centralDirectoryOffset;

        for (var i = 0; i < entryCount; i++)
        {
            if (offset < 0 ||
                offset + 46 > bytes.Length ||
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4)) != 0x02014b50)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", "Archive central directory is invalid.");
            }

            var flags = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 8, 2));
            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 28, 2));
            var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 30, 2));
            var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 32, 2));
            var externalAttributes = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 38, 4));
            var nameOffset = offset + 46;
            var nextOffset = nameOffset + nameLength + extraLength + commentLength;
            if (nextOffset > bytes.Length)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", "Archive central directory entry is truncated.");
            }

            var nameBytes = bytes.AsSpan(nameOffset, nameLength);
            entries[i] = new ZipCentralDirectoryEntry(
                Encoding.UTF8.GetString(nameBytes),
                (flags & 0x0800) != 0,
                ContainsNonAsciiByte(nameBytes),
                externalAttributes);
            offset = nextOffset;
        }

        return entries;
    }

    private static bool ContainsNonAsciiByte(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (value > 127)
            {
                return true;
            }
        }

        return false;
    }

    private static int FindEndOfCentralDirectory(byte[] bytes)
    {
        var minimumOffset = Math.Max(0, bytes.Length - 65_557);
        for (var offset = bytes.Length - 22; offset >= minimumOffset; offset--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4)) == 0x06054b50)
            {
                return offset;
            }
        }

        throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC", "Archive end of central directory was not found.");
    }

    private static byte[] ReadZipEntryBytes(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static void VerifyArchiveRequiredFiles(Dictionary<string, byte[]> entries)
    {
        foreach (var required in new[] { "AUDIT-MANIFEST.md", "SHA256SUMS.txt", "repo-file-hashes.json", "AUDIT-REQUEST.md", "metadata/audit-package.input.json" })
        {
            if (!entries.ContainsKey(required))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ARCHIVE-INCOMPLETE", $"Archive is missing {required}.");
            }
        }
    }

    private static void VerifyChecksums(Dictionary<string, byte[]> entries)
    {
        var checksumText = Encoding.UTF8.GetString(entries["SHA256SUMS.txt"]);
        var expected = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in checksumText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.Length < 66 || trimmed[64] != ' ' || trimmed[65] != ' ')
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-CHECKSUM-FAILED", $"Invalid checksum line: {trimmed}");
            }

            var hash = trimmed[..64];
            var path = AuditPath.NormalizeArchivePath(trimmed[66..]);
            if (!Regex.IsMatch(hash, @"\A[0-9a-fA-F]{64}\z", RegexOptions.CultureInvariant) ||
                !expected.TryAdd(path, hash))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-CHECKSUM-FAILED", $"Invalid checksum line: {trimmed}");
            }
        }

        foreach (var entry in entries.Where(entry => entry.Key != "SHA256SUMS.txt"))
        {
            if (!expected.TryGetValue(entry.Key, out var expectedHash))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-CHECKSUM-FAILED", $"Archive file is missing from SHA256SUMS.txt: {entry.Key}");
            }

            var actualHash = Sha256Bytes(entry.Value);
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-CHECKSUM-FAILED", $"Checksum mismatch for {entry.Key}.");
            }
        }

        foreach (var path in expected.Keys)
        {
            if (!entries.ContainsKey(path))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-CHECKSUM-FAILED", $"SHA256SUMS.txt references missing file: {path}");
            }
        }
    }

    private static async Task VerifySha256SumToolAsync(string extractRoot, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("sha256sum")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = extractRoot,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("SHA256SUMS.txt");

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return;
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-CHECKSUM-FAILED",
                $"sha256sum -c failed: {await stderrTask.ConfigureAwait(false)}{await stdoutTask.ConfigureAwait(false)}");
        }
    }

    private static void VerifyGeneratedTaskIds(AuditPackageConfiguration config, Dictionary<string, byte[]> entries)
    {
        var taskId = config.TaskId;
        var iteration = config.Iteration;
        foreach (var path in entries.Keys)
        {
            foreach (Match match in TaskIdPattern.Matches(path))
            {
                if (!string.Equals(match.Value, taskId, StringComparison.Ordinal))
                {
                    throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-TASK-ID-MISMATCH", $"Archive path contains mismatched task id {match.Value}.");
                }
            }
        }

        var patchEntries = entries.Keys
            .Where(path => Regex.IsMatch(path, @"\AT-\d{4}\.patch\z", RegexOptions.CultureInvariant))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (patchEntries.Length != 1 || !string.Equals(patchEntries[0], $"{taskId}.patch", StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-TASK-ID-MISMATCH", "Archive patch root does not match metadata task id.");
        }

        var evidenceRoot = $"evidence/{taskId}-{iteration}/";
        foreach (var path in entries.Keys.Where(path => path.StartsWith("evidence/", StringComparison.Ordinal)))
        {
            if (!path.StartsWith(evidenceRoot, StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-TASK-ID-MISMATCH", $"Evidence path does not match metadata task id and iteration: {path}");
            }
        }

        var request = Encoding.UTF8.GetString(entries["AUDIT-REQUEST.md"]);
        if (!request.Contains($"# {taskId} audit {iteration}", StringComparison.Ordinal) ||
            !request.Contains($"- Archive: `{taskId}-audit-{iteration}.zip`", StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-TASK-ID-MISMATCH", "AUDIT-REQUEST.md task header or archive name does not match metadata.");
        }

        var manifest = Encoding.UTF8.GetString(entries["AUDIT-MANIFEST.md"]);
        if (!manifest.Contains($"- taskId: `{taskId}`", StringComparison.Ordinal) ||
            !manifest.Contains($"- iteration: `{iteration}`", StringComparison.Ordinal) ||
            !manifest.Contains($"- `{taskId}.patch` restores repository-owned files from baseline.", StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-TASK-ID-MISMATCH", "AUDIT-MANIFEST.md metadata does not match archive config.");
        }
    }

    private static void VerifyManifestInventory(Dictionary<string, byte[]> entries)
    {
        var manifest = Encoding.UTF8.GetString(entries["AUDIT-MANIFEST.md"]);
        foreach (var path in entries.Keys.Order(StringComparer.Ordinal))
        {
            if (!manifest.Contains(path, StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-MANIFEST-INCOMPLETE", $"Manifest does not list archive path: {path}");
            }
        }

        foreach (var path in entries.Keys.Where(path => path.StartsWith("evidence/", StringComparison.Ordinal)))
        {
            if (!manifest.Contains($"`{path}`", StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-MANIFEST-INCOMPLETE", $"Manifest does not link evidence path: {path}");
            }
        }

        var restoreManifest = JsonSerializer.Deserialize<RestoreManifest>(
            entries["repo-file-hashes.json"],
            JsonReadOptions) ?? throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-RESTORE-MANIFEST-INVALID",
                "repo-file-hashes.json is empty or invalid.");
        foreach (var file in restoreManifest.RepoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            if (!manifest.Contains($"`{file.Path}`", StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-MANIFEST-INCOMPLETE", $"Manifest does not list repository file: {file.Path}");
            }
        }

        foreach (var path in restoreManifest.DeletedRepoFiles.OrderBy(path => path, StringComparer.Ordinal))
        {
            if (!manifest.Contains($"`{path}`", StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-MANIFEST-INCOMPLETE", $"Manifest does not list deleted repository file: {path}");
            }
        }
    }

    private static void VerifyPatchControlPaths(string patch)
    {
        foreach (var line in patch.Split('\n'))
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal) ||
                line.StartsWith("--- ", StringComparison.Ordinal) ||
                line.StartsWith("+++ ", StringComparison.Ordinal) ||
                line.StartsWith("rename from ", StringComparison.Ordinal) ||
                line.StartsWith("rename to ", StringComparison.Ordinal) ||
                line.StartsWith("copy from ", StringComparison.Ordinal) ||
                line.StartsWith("copy to ", StringComparison.Ordinal))
            {
                if (line.Contains('\\') || line.Any(ch => char.IsControl(ch) && ch != '\t' && ch != '\r'))
                {
                    throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-PATH-INVALID", "Patch control paths must use POSIX paths without control characters.");
                }
            }
        }
    }

    private static async Task VerifyCleanRepositoryAsync(string repoRoot, string baseline, CancellationToken cancellationToken)
    {
        var head = await GitRunner.RunAsync(repoRoot, ["rev-parse", "HEAD"], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (head.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git rev-parse failed: {head.StandardError}");
        }

        if (!string.Equals(head.StandardOutput.Trim(), baseline, StringComparison.OrdinalIgnoreCase))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-BASELINE-MISMATCH", "Clean repository HEAD does not match baseline.");
        }

        var status = await GitRunner.RunAsync(repoRoot, ["status", "--porcelain"], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (status.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git status failed: {status.StandardError}");
        }

        if (!string.IsNullOrWhiteSpace(status.StandardOutput))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-REPO-DIRTY", "Clean repository path has local changes before restore verification.");
        }
    }

    private static void VerifyRestoreManifestMetadata(AuditPackageConfiguration config, RestoreManifest manifest)
    {
        if (!string.Equals(config.TaskId, manifest.TaskId, StringComparison.Ordinal) ||
            !string.Equals(config.Iteration, manifest.Iteration, StringComparison.Ordinal) ||
            !string.Equals(config.Baseline, manifest.Baseline, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-TASK-ID-MISMATCH", "Restore manifest metadata does not match archive config.");
        }
    }

    private static async Task VerifyRestoredFileSetAsync(string repoRoot, RestoreManifest manifest, CancellationToken cancellationToken)
    {
        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in manifest.RepoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var normalized = AuditPath.NormalizeRelativePath(file.Path, "repo-file-hashes.json", allowCurrentDirectory: false);
            if (!expected.Add(normalized))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-RESTORE-MISMATCH", $"Duplicate restored file in repo-file-hashes.json: {normalized}");
            }
        }

        foreach (var file in manifest.DeletedRepoFiles.OrderBy(path => path, StringComparer.Ordinal))
        {
            var normalized = AuditPath.NormalizeRelativePath(file, "repo-file-hashes.json", allowCurrentDirectory: false);
            if (!expected.Add(normalized))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-RESTORE-MISMATCH", $"Duplicate deleted file in repo-file-hashes.json: {normalized}");
            }
        }

        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in await GitListForVerifyAsync(repoRoot, ["diff", "--name-only", "-z", "--"], cancellationToken).ConfigureAwait(false))
        {
            actual.Add(AuditPath.NormalizeRelativePath(path, "restored Git diff", allowCurrentDirectory: false));
        }

        foreach (var path in await GitListForVerifyAsync(repoRoot, ["ls-files", "--others", "-z"], cancellationToken).ConfigureAwait(false))
        {
            actual.Add(AuditPath.NormalizeRelativePath(path, "restored untracked file", allowCurrentDirectory: false));
        }

        foreach (var path in await GitListForVerifyAsync(repoRoot, ["ls-files", "--others", "--ignored", "--exclude-standard", "-z"], cancellationToken).ConfigureAwait(false))
        {
            actual.Add(AuditPath.NormalizeRelativePath(path, "restored ignored file", allowCurrentDirectory: false));
        }

        var missing = expected.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var extra = actual.Except(expected, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length > 0 || extra.Length > 0)
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-RESTORE-MISMATCH",
                $"Restored file set does not match repo-file-hashes.json. Missing: {FormatPathList(missing)}. Extra: {FormatPathList(extra)}.");
        }
    }

    private static async Task<string[]> GitListForVerifyAsync(string repoRoot, string[] arguments, CancellationToken cancellationToken)
    {
        var result = await GitRunner.RunAsync(repoRoot, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git {string.Join(" ", arguments)} failed: {result.StandardError}");
        }

        return result.StandardOutput
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    private static string FormatPathList(IReadOnlyCollection<string> paths)
    {
        return paths.Count == 0
            ? "none"
            : string.Join(", ", paths);
    }

    private static async Task VerifyRestoredHashesAsync(string repoRoot, RestoreManifest manifest, CancellationToken cancellationToken)
    {
        foreach (var file in manifest.RepoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            AuditPath.NormalizeRelativePath(file.Path, "repo-file-hashes.json", allowCurrentDirectory: false);
            var path = Path.Combine(repoRoot, file.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-RESTORE-MISMATCH", $"Restored file is missing: {file.Path}");
            }

            var actual = await Sha256FileAsync(path, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(file.Sha256, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-RESTORE-MISMATCH", $"Restored hash mismatch for {file.Path}");
            }
        }

        foreach (var file in manifest.DeletedRepoFiles.OrderBy(path => path, StringComparer.Ordinal))
        {
            AuditPath.NormalizeRelativePath(file, "repo-file-hashes.json", allowCurrentDirectory: false);
            var path = Path.Combine(repoRoot, file.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-RESTORE-MISMATCH", $"Deleted file still exists after restore: {file}");
            }
        }
    }

    private static async Task VerifyOneLineStubsAsync(
        string repoRoot,
        AuditPackageConfiguration config,
        RestoreManifest manifest,
        CancellationToken cancellationToken)
    {
        var allowlist = config.OneLineStubAllowlist.ToHashSet(StringComparer.Ordinal);

        foreach (var file in manifest.RepoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var baselineProbe = await GitRunner.RunAsync(
                repoRoot,
                ["cat-file", "-e", $"{config.Baseline}:{file.Path}"],
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (baselineProbe.ExitCode == 0)
            {
                continue;
            }

            if (allowlist.Contains(file.Path))
            {
                continue;
            }

            var path = Path.Combine(repoRoot, file.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!IsTextFile(path))
            {
                continue;
            }

            var meaningfulLines = (await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false))
                .Count(line => !string.IsNullOrWhiteSpace(line));
            if (meaningfulLines <= 1)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ONE-LINE-STUB", $"New text file looks like a one-line stub: {file.Path}");
            }
        }
    }

    private static async Task<TemporaryWorkspace> CreateCleanCloneAsync(string repoRoot, string baseline, CancellationToken cancellationToken)
    {
        var workspace = TemporaryWorkspace.Create("Electron2D-AuditCleanRepo");
        var result = await GitRunner.RunAsync(
            Directory.GetParent(workspace.Root)!.FullName,
            ["clone", "--no-checkout", repoRoot, workspace.Root],
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            workspace.Dispose();
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git clone failed: {result.StandardError}");
        }

        var checkout = await GitRunner.RunAsync(workspace.Root, ["checkout", baseline], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (checkout.ExitCode != 0)
        {
            workspace.Dispose();
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git checkout baseline failed: {checkout.StandardError}");
        }

        var clean = await GitRunner.RunAsync(workspace.Root, ["clean", "-fdx"], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (clean.ExitCode != 0)
        {
            workspace.Dispose();
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git clean failed: {clean.StandardError}");
        }

        return workspace;
    }

    private static string ResolvePath(string root, string path)
    {
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(root, path));
    }

    private static bool MatchesAny(IEnumerable<string> patterns, string path)
    {
        return patterns.Any(pattern => GlobMatcher.IsMatch(pattern, path));
    }

    private static void ValidateInputPath(string path, AuditPackageConfiguration config)
    {
        var normalized = AuditPath.NormalizeRelativePath(path, "input path", allowCurrentDirectory: false);
        if (ForbiddenPathPolicy.IsForbidden(normalized) || MatchesAny(config.ForbiddenPatterns, normalized))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-FORBIDDEN-PATH", $"Forbidden input path: {normalized}");
        }
    }

    private static void ValidateInputFileSize(string path, string relativePath, long maxFileSize)
    {
        var info = new FileInfo(path);
        if (info.Length > maxFileSize)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-FILE-TOO-LARGE", $"File exceeds maxFileSize: {relativePath}");
        }
    }

    private static void ValidateSecretPolicy(string path, string relativePath, string policy)
    {
        if (!string.Equals(policy, "basic", StringComparison.OrdinalIgnoreCase) || !IsTextFile(path))
        {
            return;
        }

        ValidateSecretText(File.ReadAllText(path, Encoding.UTF8), relativePath, "audit package");
    }

    private static void ValidateArchiveContent(
        string archivePath,
        byte[] bytes,
        string repoRoot,
        ISet<string> previousVerdictPaths)
    {
        if (!IsTextBytes(bytes))
        {
            return;
        }

        var text = Encoding.UTF8.GetString(bytes);
        var machinePathScanText = IsPatchPath(archivePath) && previousVerdictPaths.Count > 0
            ? OmitPreviousVerdictPatchBlocks(text, previousVerdictPaths)
            : text;
        ValidateMachineLocalPathText(machinePathScanText, archivePath, repoRoot);

        if (IsTrxPath(archivePath))
        {
            ValidateTrxSecretText(text, archivePath, "audit package");
            return;
        }

        ValidateSecretText(text, archivePath, "audit package");
    }

    private static void ValidateMachineLocalPathText(string text, string archivePath, string repoRoot)
    {
        var normalizedText = text.Replace('\\', '/');
        var normalizedRepoRoot = Path.GetFullPath(repoRoot).Replace('\\', '/').TrimEnd('/');
        var normalizedTempRoot = Path.GetFullPath(Path.GetTempPath()).Replace('\\', '/').TrimEnd('/');
        if (normalizedText.Contains(normalizedRepoRoot, StringComparison.OrdinalIgnoreCase) ||
            normalizedText.Contains(normalizedTempRoot, StringComparison.OrdinalIgnoreCase) ||
            WindowsDrivePathPattern.IsMatch(text) ||
            WindowsDrivePathPattern.IsMatch(normalizedText))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-ABSOLUTE-PATH", $"Archive content contains a machine-local path: {archivePath}");
        }
    }

    private static string OmitPreviousVerdictPatchBlocks(string patch, ISet<string> previousVerdictPaths)
    {
        var builder = new StringBuilder(patch.Length);
        var omitCurrentDiff = false;
        foreach (var line in patch.Split('\n'))
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                omitCurrentDiff = PatchDiffLineReferencesAnyPath(line, previousVerdictPaths);
            }

            if (!omitCurrentDiff)
            {
                builder.Append(line);
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static bool PatchDiffLineReferencesAnyPath(string line, ISet<string> paths)
    {
        foreach (var path in paths)
        {
            if (line.Contains($" a/{path}", StringComparison.Ordinal) ||
                line.Contains($" b/{path}", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateSecretText(string text, string relativePath, string step)
    {
        if (PrivateKeyPattern.IsMatch(text) || ContainsSecretAssignment(text))
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-SECRET-DETECTED", $"Potential secret detected: {relativePath}");
        }
    }

    private static bool ContainsSecretAssignment(string text)
    {
        foreach (Match match in SecretValuePattern.Matches(text))
        {
            if (!IsAllowedSecretPlaceholder(match.Groups["value"].Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAllowedSecretPlaceholder(string value)
    {
        var normalized = NormalizeSecretCandidateValue(value);
        if (normalized.Length == 0)
        {
            return false;
        }

        if (normalized == "\u2026")
        {
            return true;
        }

        if (normalized.All(character => character == '.'))
        {
            return normalized.Length >= 3;
        }

        return RedactedSecretPlaceholderValues.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeSecretCandidateValue(string value)
    {
        var normalized = value.Trim();

        while (normalized.Length > 0 && normalized[0] == '`')
        {
            normalized = normalized[1..].TrimStart();
        }

        var placeholderPrefix = TryGetAllowedSecretPlaceholderPrefix(normalized);
        if (placeholderPrefix is not null)
        {
            return placeholderPrefix;
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            while (normalized.Length > 0 && IsSecretCandidateTrailingDecoration(normalized[^1]))
            {
                normalized = normalized[..^1].TrimEnd();
                changed = true;
            }

            while (normalized.Length > 0 && normalized[^1] == '.' && !normalized.All(character => character == '.'))
            {
                normalized = normalized[..^1].TrimEnd();
                changed = true;
            }
        }

        return normalized;
    }

    private static string? TryGetAllowedSecretPlaceholderPrefix(string value)
    {
        if (value.StartsWith("\u2026", StringComparison.Ordinal) &&
            IsSecretCandidatePlaceholderBoundary(value, 1))
        {
            return "\u2026";
        }

        var dotCount = 0;
        while (dotCount < value.Length && value[dotCount] == '.')
        {
            dotCount++;
        }

        if (dotCount >= 3 && IsSecretCandidatePlaceholderBoundary(value, dotCount))
        {
            return value[..dotCount];
        }

        foreach (var placeholder in RedactedSecretPlaceholderValues)
        {
            if (value.StartsWith(placeholder, StringComparison.OrdinalIgnoreCase) &&
                IsSecretCandidatePlaceholderBoundary(value, placeholder.Length))
            {
                return placeholder;
            }
        }

        return null;
    }

    private static bool IsSecretCandidatePlaceholderBoundary(string value, int index)
    {
        var cursor = index;
        while (cursor < value.Length && IsSecretCandidateTrailingDecoration(value[cursor]))
        {
            cursor++;
        }

        if (cursor >= value.Length)
        {
            return true;
        }

        return char.IsWhiteSpace(value[cursor]) || value[cursor] is '.' or ':' or '!' or '?';
    }

    private static bool IsSecretCandidateTrailingDecoration(char character)
    {
        return character is '`' or '"' or '\'' or ',' or ';' or ')' or ']' or '}';
    }

    private static byte[] ReadTrxEvidenceBytes(string path, string relativePath, string policy, string repoRoot)
    {
        var bytes = File.ReadAllBytes(path);
        if (!IsTextBytes(bytes))
        {
            return bytes;
        }

        var text = Encoding.UTF8.GetString(bytes);
        if (string.Equals(policy, "basic", StringComparison.OrdinalIgnoreCase))
        {
            ValidateTrxSecretText(text, relativePath, "audit package");
        }

        return Encoding.UTF8.GetBytes(SanitizeTrxEvidenceText(text, relativePath, repoRoot));
    }

    private static void ValidateTrxSecretText(string text, string relativePath, string step)
    {
        var document = ParseTrxDocument(text, relativePath, step);
        if (document.Root is null)
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-TRX-INVALID", $"TRX evidence is empty: {relativePath}");
        }

        var scanText = new StringBuilder();
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            foreach (var attribute in element.Attributes())
            {
                if (IsTrxMachinePathAttribute(attribute.Name.LocalName))
                {
                    continue;
                }

                var value = IsTrxDisplayNameAttribute(attribute.Name.LocalName)
                    ? StripTrxDisplayArguments(attribute.Value)
                    : attribute.Value;
                scanText.AppendLine(value);
            }
        }

        foreach (var textNode in document.Root.DescendantNodesAndSelf().OfType<XText>())
        {
            scanText.AppendLine(textNode.Value);
        }

        ValidateSecretText(scanText.ToString(), relativePath, step);
    }

    private static string SanitizeTrxEvidenceText(string text, string relativePath, string repoRoot)
    {
        var document = ParseTrxDocument(text, relativePath, "audit package");
        if (document.Root is null)
        {
            return text;
        }

        foreach (var attribute in document.Root.DescendantsAndSelf()
            .SelectMany(element => element.Attributes())
            .Where(attribute => IsTrxMachinePathAttribute(attribute.Name.LocalName)))
        {
            attribute.Value = RedactTrxMachinePath(attribute.Value, repoRoot);
        }

        foreach (var attribute in document.Root.DescendantsAndSelf()
            .SelectMany(element => element.Attributes())
            .Where(attribute => IsTrxMachineIdentityAttributeRemoved(attribute.Name.LocalName))
            .ToArray())
        {
            attribute.Remove();
        }

        foreach (var element in document.Root.DescendantsAndSelf())
        {
            foreach (var attribute in element.Attributes())
            {
                if (IsTrxMachineIdentityAttribute(element.Name.LocalName, attribute.Name.LocalName))
                {
                    attribute.Value = RedactTrxMachineIdentityAttribute();
                }
            }
        }

        return document.ToString(SaveOptions.DisableFormatting) + "\n";
    }

    private static XDocument ParseTrxDocument(string text, string relativePath, string step)
    {
        try
        {
            return XDocument.Parse(text.TrimStart('\uFEFF'), LoadOptions.PreserveWhitespace);
        }
        catch (XmlException exception)
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-TRX-INVALID", $"TRX evidence is not valid XML: {relativePath} ({exception.Message})");
        }
    }

    private static string StripTrxDisplayArguments(string value)
    {
        return Regex.Replace(value, @"\([^)]*\)", "(<test-arguments>)", RegexOptions.CultureInvariant);
    }

    private static string RedactTrxMachinePath(string value, string repoRoot)
    {
        var normalized = value.Replace('\\', '/');
        var normalizedRepoRoot = Path.GetFullPath(repoRoot).Replace('\\', '/').TrimEnd('/');
        if (normalized.StartsWith(normalizedRepoRoot + "/", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, normalizedRepoRoot, StringComparison.OrdinalIgnoreCase))
        {
            return "<repo-root>" + normalized[normalizedRepoRoot.Length..];
        }

        return Regex.IsMatch(normalized, @"\A[A-Z]:/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            ? $"<machine-path>/{Path.GetFileName(normalized)}"
            : value;
    }

    private static bool IsTrxPath(string path)
    {
        return path.EndsWith(".trx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPatchPath(string path)
    {
        return path.EndsWith(".patch", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrxDisplayNameAttribute(string name)
    {
        return string.Equals(name, "name", StringComparison.Ordinal) ||
            string.Equals(name, "testName", StringComparison.Ordinal);
    }

    private static bool IsTrxMachinePathAttribute(string name)
    {
        return string.Equals(name, "codeBase", StringComparison.Ordinal) ||
            string.Equals(name, "storage", StringComparison.Ordinal);
    }

    private static bool IsTrxMachineIdentityAttribute(string elementName, string attributeName)
    {
        return string.Equals(elementName, "TestRun", StringComparison.Ordinal) &&
            string.Equals(attributeName, "name", StringComparison.Ordinal);
    }

    private static bool IsTrxMachineIdentityAttributeRemoved(string attributeName)
    {
        return string.Equals(attributeName, "runUser", StringComparison.Ordinal) ||
            string.Equals(attributeName, "computerName", StringComparison.Ordinal) ||
            string.Equals(attributeName, "runDeploymentRoot", StringComparison.Ordinal);
    }

    private static string RedactTrxMachineIdentityAttribute()
    {
        return "<test-run>";
    }

    private static bool IsTextBytes(byte[] bytes)
    {
        return !bytes.Contains((byte)0);
    }

    private static bool IsTextFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return !bytes.Contains((byte)0);
    }

    private static byte[] ReadRestorableFileBytes(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (!IsTextBytes(bytes))
        {
            return bytes;
        }

        var text = Encoding.UTF8.GetString(bytes);
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Encoding.UTF8.GetBytes(normalized);
    }

    private static void WriteText(string root, string relativePath, string text)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string FormatCommandEvidence(string fileName, IEnumerable<string> arguments)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"fileName: {fileName}");
        builder.AppendLine("arguments:");
        foreach (var argument in arguments)
        {
            builder.AppendLine($"- {argument}");
        }

        return builder.ToString();
    }

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static async Task<string> Sha256FileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sha256Bytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private void WriteError(string command, string step, string code, string message, string? zipPath = null, bool? force = null)
    {
        diagnostics.Write(new BuildDiagnostic(command, step, "error", code, message, ZipPath: zipPath, Force: force));
    }
}

internal sealed class AuditPackageFailure(
    string step,
    string code,
    string message,
    string? ZipPath = null,
    bool? Force = null) : Exception(message)
{
    public string Step { get; } = step;
    public string Code { get; } = code;
    public string? ZipPath { get; } = ZipPath;
    public bool? Force { get; } = Force;
}

internal sealed record AuditPackageOptions(
    string TaskId,
    string Iteration,
    string Baseline,
    string ConfigPath,
    string OutputDirectory,
    bool Force);

internal sealed record AuditVerifyOptions(string ZipPath, string Baseline, string RepositoryPath);

internal sealed class AuditPackageConfiguration
{
    public string TaskId { get; set; } = string.Empty;
    public string Iteration { get; set; } = string.Empty;
    public string Baseline { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public List<string> RepoFileGlobs { get; set; } = [];
    public List<string> RepoFileAllowlist { get; set; } = [];
    public List<string> ArchiveOnlyEvidenceGlobs { get; set; } = [];
    public List<AuditCheckConfiguration> Checks { get; set; } = [];
    public List<string> ForbiddenPatterns { get; set; } = [];
    public string SecretScanPolicy { get; set; } = "basic";
    public long MaxFileSize { get; set; } = 1_048_576;
    public string OutputDirectory { get; set; } = string.Empty;
    public List<string> PreviousVerdictChain { get; set; } = [];
    public List<string> BlockerClosureList { get; set; } = [];
    public List<string> OneLineStubAllowlist { get; set; } = [];
}

internal sealed class AuditCheckConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = [];
    public string Cwd { get; set; } = ".";
    public List<string> EnvAllowlist { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 30;
    public int ExpectedExitCode { get; set; }
    public List<string> TrxGlobs { get; set; } = [];
}

internal sealed record SelectedRepositoryFile(string Path, string AbsolutePath, bool Exists, bool ExistsInBaseline);

internal sealed record EvidenceSourceFile(string SourcePath, string ArchivePath);

internal sealed record PatchResult(string PatchText, string NameStatus);

internal sealed record RestoreManifest(
    string TaskId,
    string Iteration,
    string Baseline,
    IReadOnlyList<RestoreFileHash> RepoFiles,
    IReadOnlyList<string> DeletedRepoFiles);

internal sealed record RestoreFileHash(string Path, string Sha256);

internal sealed record CheckEvidenceMetadata(
    string Name,
    string FileName,
    IReadOnlyList<string> Arguments,
    string Cwd,
    int ExpectedExitCode,
    int TimeoutSeconds,
    int ActualExitCode,
    double DurationMs,
    string StdoutPath,
    string StderrPath,
    string StdoutSha256,
    string StderrSha256,
    IReadOnlyList<TrxEvidenceFile> TrxFiles);

internal sealed record TrxEvidenceFile(string Path, string Sha256);

internal sealed record CopiedTrxEvidenceFile(string SourceRelativePath, string SourceAbsolutePath, TrxEvidenceFile Metadata);

internal sealed record ZipCentralDirectoryEntry(
    string Name,
    bool HasUtf8Flag,
    bool NameContainsNonAsciiBytes,
    int ExternalAttributes);

internal sealed record ProcessExecutionResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration);

internal static class AuditProcessRunner
{
    public static async Task<ProcessExecutionResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CHECK-FAILED", $"Failed to start check process: {fileName}");
        var stopwatch = Stopwatch.StartNew();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            stopwatch.Stop();
            return new ProcessExecutionResult(-1, await ReadSafeAsync(stdoutTask).ConfigureAwait(false), await ReadSafeAsync(stderrTask).ConfigureAwait(false), stopwatch.Elapsed);
        }

        stopwatch.Stop();
        return new ProcessExecutionResult(
            process.ExitCode,
            await stdoutTask.ConfigureAwait(false),
            await stderrTask.ConfigureAwait(false),
            stopwatch.Elapsed);
    }

    private static async Task<string> ReadSafeAsync(Task<string> task)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}

internal static class GitRunner
{
    public static async Task<ProcessExecutionResult> RunAsync(
        string workingDirectory,
        string[] arguments,
        Dictionary<string, string>? environment = null,
        CancellationToken cancellationToken = default)
    {
        var allArguments = new List<string>
        {
            "-c",
            "core.quotepath=false",
            "-c",
            "core.autocrlf=false",
            "-c",
            "core.safecrlf=false"
        };
        allArguments.AddRange(arguments);
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in allArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var item in environment)
            {
                startInfo.Environment[item.Key] = item.Value;
            }
        }

        using var process = Process.Start(startInfo)
            ?? throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", "Failed to start git.");
        var stopwatch = Stopwatch.StartNew();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        return new ProcessExecutionResult(
            process.ExitCode,
            await stdoutTask.ConfigureAwait(false),
            await stderrTask.ConfigureAwait(false),
            stopwatch.Elapsed);
    }
}

internal static class AuditPath
{
    public static string NormalizeArchivePath(string path)
    {
        return NormalizeRelativePath(path, "archive path", allowCurrentDirectory: false);
    }

    public static string NormalizeRelativePath(string path, string fieldName, bool allowCurrentDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-PATH-INVALID", $"{fieldName} is empty.");
        }

        var normalized = path.Replace('\\', '/');
        if (!string.Equals(path, normalized, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-PATH-INVALID", $"{fieldName} contains a backslash: {path}");
        }

        if (normalized.Any(char.IsControl))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-PATH-INVALID", $"{fieldName} contains a control character.");
        }

        if (Path.IsPathRooted(normalized) || normalized.StartsWith("/", StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-PATH-INVALID", $"{fieldName} must be relative: {path}");
        }

        if (allowCurrentDirectory && normalized == ".")
        {
            return normalized;
        }

        var parts = normalized.Split('/');
        if (parts.Any(part => string.IsNullOrWhiteSpace(part) || part is "." or ".."))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-PATH-INVALID", $"{fieldName} contains an invalid segment: {path}");
        }

        return normalized;
    }

    public static void ValidatePattern(string pattern, string fieldName)
    {
        NormalizeRelativePath(pattern, fieldName, allowCurrentDirectory: false);
    }
}

internal static class GlobMatcher
{
    public static bool IsMatch(string pattern, string path)
    {
        var regex = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];
            if (ch == '*')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                    {
                        regex.Append("(?:.*/)?");
                        i += 2;
                    }
                    else
                    {
                        regex.Append(".*");
                        i++;
                    }
                }
                else
                {
                    regex.Append("[^/]*");
                }
            }
            else if (ch == '?')
            {
                regex.Append("[^/]");
            }
            else
            {
                regex.Append(Regex.Escape(ch.ToString()));
            }
        }

        regex.Append('$');
        return Regex.IsMatch(path, regex.ToString(), RegexOptions.CultureInvariant);
    }
}

internal static class ForbiddenPathPolicy
{
    private static readonly string[] ForbiddenSegments = [".git", "bin", "obj", ".temp", "tmp", "temp"];
    private static readonly string[] ForbiddenExtensions = [".zip", ".tar", ".gz", ".7z"];

    public static bool IsForbidden(string path)
    {
        var lowered = path.ToLowerInvariant();
        var segments = lowered.Split('/');
        if (segments.Any(segment => ForbiddenSegments.Contains(segment, StringComparer.Ordinal)))
        {
            return true;
        }

        var fileName = segments[^1];
        if (fileName == ".env" || fileName.StartsWith(".env.", StringComparison.Ordinal))
        {
            return true;
        }

        if (ForbiddenExtensions.Any(extension => lowered.EndsWith(extension, StringComparison.Ordinal)))
        {
            return true;
        }

        return lowered.Contains("secret", StringComparison.Ordinal) ||
            lowered.Contains("token", StringComparison.Ordinal) ||
            lowered.Contains("private-key", StringComparison.Ordinal);
    }
}

internal sealed record TemporaryWorkspace(string Root) : IDisposable
{
    public static TemporaryWorkspace Create(string prefix)
    {
        var root = Path.Combine(Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new TemporaryWorkspace(root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
