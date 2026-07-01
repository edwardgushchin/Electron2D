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
        @"(?im)-----BEGIN [A-Z ]*PRIVATE\s+KEY-----|-----BEGIN[^\r\n]*PRIVATE\s+KEY|\bBEGIN\s+PRIVATE\s+KEY\b",
        RegexOptions.CultureInvariant);
    private static readonly Regex WindowsDrivePathPattern = new(
        @"(?i)\b[A-Z]:(?:\\|/)",
        RegexOptions.CultureInvariant);
    private const string RepositoryFileSnapshotsArchivePath = "metadata/repo-file-snapshots.json";
    private const string StaticAuditRequestSourcePath = "docs/release-management/AUDIT-REQUEST.md";
    private const string StaticAuditRequestArchivePath = "AUDIT-REQUEST.md";
    private const int OperatorWorkflowEvidenceTimeoutSeconds = 180;
    private static readonly string[] StaticAuditRequestRequiredMarkers =
    [
        "VERDICT: ACCEPT",
        "VERDICT: NEEDS_FIXES",
        "TASK_ASSESSMENT",
        "BLOCKERS",
        "EVIDENCE_REVIEW",
        "RISKS_AND_NOTES",
        "CLOSURE_DECISION",
        "metadata.scopeTaskIds",
        "metadata.scopeSummary",
        "combined scope",
        "metadata.previousVerdictChain",
        "metadata.blockerClosureList",
        "previous verdict files",
        "verbatim preservation",
        "previous blockers closure",
        "metadata/repo-file-snapshots.json",
        "repo-after/",
        "repo-before/",
        "implementation content review",
        "test coverage review",
        "documentation review",
        "task compliance review",
        "secret scanning",
        "scope scanning",
        "evidence gap",
        "patch-only inspection",
        "single final report",
        "no intermediate VERDICT"
    ];
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
        if (args.Length >= 2 && args[1] == "submit")
        {
            try
            {
                await new AuditSubmitCommand().RunAsync(args, cancellationToken).ConfigureAwait(false);
                return RepositoryBuildExitCodes.Success;
            }
            catch (AuditPackageFailure failure)
            {
                WriteError("audit", failure.Step, failure.Code, failure.Message, force: failure.Force, zipPath: failure.ZipPath);
                return RepositoryBuildExitCodes.Failed;
            }
        }

        if (args.Length < 2 || args[1] != "package")
        {
            WriteError(
                "audit",
                "audit",
                "E2D-BUILD-CLI-INVALID-ARGUMENTS",
                "Expected: audit package ..., audit package verify ..., audit package message ..., or audit submit ...");
            return RepositoryBuildExitCodes.Failed;
        }

        try
        {
            if (args.Length >= 3 && args[2] == "verify")
            {
                var options = ParseVerifyOptions(args);
                await VerifyPackageAsync(
                    options,
                    writeSuccessDiagnostic: true,
                    requireOperatorWorkflowSidecar: true,
                    cancellationToken).ConfigureAwait(false);
                return RepositoryBuildExitCodes.Success;
            }

            if (args.Length >= 3 && args[2] == "message")
            {
                var options = ParseMessageOptions(args);
                WritePackageMessage(options);
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
        var operatorWorkflowZipPath = Path.Combine(outputDirectory, $"{options.TaskId}-audit-{options.Iteration}.operator-workflow.zip");

        var existingOutput = new[] { zipPath, operatorWorkflowZipPath }
            .FirstOrDefault(File.Exists);
        if (existingOutput is not null)
        {
            if (!options.Force)
            {
                throw new AuditPackageFailure(
                    "audit package",
                    "E2D-BUILD-AUDIT-ZIP-EXISTS",
                    $"Target audit output already exists: {existingOutput}",
                    ZipPath: existingOutput);
            }

            diagnostics.Write(new BuildDiagnostic(
                "audit",
                "audit package",
                "warning",
                "E2D-BUILD-AUDIT-FORCE-OVERWRITE",
                "Existing audit ZIP will be overwritten because --force was supplied.",
                ZipPath: zipPath,
                Force: true));
            foreach (var path in new[] { zipPath, operatorWorkflowZipPath })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        using var staging = TemporaryWorkspace.Create("Electron2D-AuditPackage");
        var auditRequest = await ReadStaticAuditRequestAsync(repoRoot, cancellationToken).ConfigureAwait(false);
        var repoFiles = await SelectRepositoryFilesAsync(repoRoot, config, cancellationToken).ConfigureAwait(false);
        var importedEvidence = SelectArchiveOnlyEvidenceFiles(repoRoot, config);
        var checkEvidence = await RunConfiguredChecksAsync(repoRoot, staging.Root, config, cancellationToken).ConfigureAwait(false);
        var configuredEvidenceFiles = importedEvidence.Concat(checkEvidence).OrderBy(file => file.ArchivePath, StringComparer.Ordinal).ToArray();

        var previousVerdictPaths = SelectPreviousVerdictPaths(config);
        var patch = await CreatePatchAsync(repoRoot, staging.Root, options.Baseline, repoFiles, previousVerdictPaths, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(patch.PatchText))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-PATCH-EMPTY", "Selected repository files do not produce a patch.");
        }

        ValidatePatchText(patch.PatchText, $"{options.TaskId}.patch", repoRoot, previousVerdictPaths);

        var restoreManifest = await CreateRestoreManifestAsync(repoRoot, config, repoFiles, cancellationToken).ConfigureAwait(false);
        var snapshotArchive = await CreateRepositoryFileSnapshotArchiveAsync(repoRoot, config, repoFiles, cancellationToken).ConfigureAwait(false);
        var normalizedConfigJson = JsonSerializer.Serialize(config, JsonWriteOptions) + "\n";
        var archiveFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [$"{options.TaskId}.patch"] = Encoding.UTF8.GetBytes(patch.PatchText),
            ["repo-file-hashes.json"] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(restoreManifest, JsonWriteOptions) + "\n"),
            [RepositoryFileSnapshotsArchivePath] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshotArchive.Manifest, JsonWriteOptions) + "\n"),
            [StaticAuditRequestArchivePath] = auditRequest.Bytes,
            ["metadata/audit-package.input.json"] = Encoding.UTF8.GetBytes(normalizedConfigJson)
        };
        foreach (var snapshotFile in snapshotArchive.ArchiveFiles)
        {
            archiveFiles[snapshotFile.Key] = snapshotFile.Value;
        }

        foreach (var evidence in configuredEvidenceFiles)
        {
            archiveFiles[evidence.ArchivePath] = await File.ReadAllBytesAsync(evidence.SourcePath, cancellationToken).ConfigureAwait(false);
        }

        RefreshManifestAndChecksums(archiveFiles, config, patch.NameStatus, restoreManifest, configuredEvidenceFiles.Select(file => file.ArchivePath).ToArray());
        ValidateArchiveFiles(archiveFiles, repoRoot, previousVerdictPaths);

        var relativeZipPath = GetRepositoryRelativePath(repoRoot, zipPath, "audit package");
        var evidenceFiles = configuredEvidenceFiles;
        var firstSnapshot = await CreateInputSnapshotAsync(repoRoot, repoFiles, evidenceFiles, auditRequest.AbsolutePath, cancellationToken).ConfigureAwait(false);
        ValidateArchiveFiles(archiveFiles, repoRoot, previousVerdictPaths);
        var secondSnapshot = await CreateInputSnapshotAsync(repoRoot, repoFiles, evidenceFiles, auditRequest.AbsolutePath, cancellationToken).ConfigureAwait(false);
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
            requireOperatorWorkflowSidecar: false,
            cancellationToken).ConfigureAwait(false);

        await CreateOperatorWorkflowSidecarAsync(
            repoRoot,
            config,
            zipPath,
            operatorWorkflowZipPath,
            relativeZipPath,
            cancellationToken).ConfigureAwait(false);

        diagnostics.Write(new BuildDiagnostic(
            "audit",
            "audit package",
            "info",
            "E2D-BUILD-AUDIT-PACKAGE-CREATED",
            $"Created and verified audit package '{Path.GetFileName(zipPath)}' with operator workflow sidecar '{Path.GetFileName(operatorWorkflowZipPath)}'.",
            ZipPath: zipPath,
            Force: options.Force ? true : null));
    }

    private void WritePackageMessage(AuditMessageOptions options)
    {
        Console.Out.Write(CreatePackageMessage(options, Directory.GetCurrentDirectory()));
    }

    internal static string CreatePackageMessage(AuditMessageOptions options, string repoRoot)
    {
        if (!File.Exists(options.ZipPath))
        {
            throw new AuditPackageFailure("audit package message", "E2D-BUILD-AUDIT-ZIP-MISSING", $"Audit ZIP was not found: {options.ZipPath}");
        }

        var entries = ReadZipEntries(options.ZipPath, "audit package message");
        VerifyMessageRequiredFiles(entries);
        var config = ReadConfigurationFromBytes(entries["metadata/audit-package.input.json"], "audit package message");
        NormalizeConfiguration(config);
        ValidateTaskId(config.TaskId, "audit package message");
        ValidateIteration(config.Iteration, "audit package message");
        ValidateAuditMessageFileName(options.ZipPath, config);
        ValidateStaticAuditRequestBytes(entries[StaticAuditRequestArchivePath], repoRoot, "audit package message");

        return StripFirstMarkdownH1(Encoding.UTF8.GetString(entries[StaticAuditRequestArchivePath]));
    }

    private async Task VerifyPackageAsync(
        AuditVerifyOptions options,
        bool writeSuccessDiagnostic,
        bool requireOperatorWorkflowSidecar,
        CancellationToken cancellationToken)
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
        ValidateStaticAuditRequestBytes(entries[StaticAuditRequestArchivePath], options.RepositoryPath, "audit package verify");
        VerifyChecksums(entries);
        await VerifySha256SumToolAsync(extractRoot.Root, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(config.Baseline, options.Baseline, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-BASELINE-MISMATCH", "Archive baseline does not match --baseline.");
        }

        VerifyGeneratedTaskIds(config, entries);
        VerifyManifestInventory(entries);
        if (requireOperatorWorkflowSidecar)
        {
            await VerifyOperatorWorkflowSidecarAsync(
                options.ZipPath,
                options.RepositoryPath,
                entries,
                config,
                cancellationToken).ConfigureAwait(false);
        }

        var patchName = $"{config.TaskId}.patch";
        if (!entries.ContainsKey(patchName))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-PATCH-MISSING", $"Archive does not contain {patchName}.");
        }

        var restoreManifest = ReadRestoreManifest(entries, "audit package verify");
        VerifyRestoreManifestMetadata(config, restoreManifest);
        var snapshotManifest = VerifyRepositoryFileSnapshotManifest(entries, config, restoreManifest);
        VerifyPatchControlPaths(Encoding.UTF8.GetString(entries[patchName]));
        await PrepareCleanRepositoryAsync(options.RepositoryPath, options.Baseline, cancellationToken).ConfigureAwait(false);
        await VerifyRepositoryFileSnapshotBaselineAsync(options.RepositoryPath, config, snapshotManifest, cancellationToken).ConfigureAwait(false);

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

        await VerifyRepositoryFileSnapshotAfterApplyAsync(options.RepositoryPath, snapshotManifest, cancellationToken).ConfigureAwait(false);
        await CleanIgnoredArtifactsAfterPatchApplyAsync(options.RepositoryPath, restoreManifest, cancellationToken).ConfigureAwait(false);
        await VerifyRestoredFileSetAsync(options.RepositoryPath, restoreManifest, cancellationToken).ConfigureAwait(false);
        await VerifyRestoredHashesAsync(options.RepositoryPath, restoreManifest, cancellationToken).ConfigureAwait(false);
        await VerifyStaticAuditRequestMatchesRestoredSourceAsync(options.RepositoryPath, entries[StaticAuditRequestArchivePath], cancellationToken).ConfigureAwait(false);
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

    private static AuditMessageOptions ParseMessageOptions(string[] args)
    {
        var values = ParseNamedArguments(args, startIndex: 3, allowedValueOptions: ["--zip"], allowedFlags: []);
        return new AuditMessageOptions(Require(values, "--zip", "audit package message"));
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

    private static AuditPackageConfiguration ReadConfigurationFromBytes(byte[] bytes, string step = "audit package verify")
    {
        try
        {
            return JsonSerializer.Deserialize<AuditPackageConfiguration>(bytes, JsonReadOptions)
                ?? throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-CONFIG-INVALID", "Archive config is empty or invalid.");
        }
        catch (JsonException exception)
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-CONFIG-INVALID", $"Archive config is empty or invalid: {exception.Message}");
        }
    }

    private static void NormalizeConfiguration(AuditPackageConfiguration config)
    {
        config.TaskId ??= string.Empty;
        config.Iteration ??= string.Empty;
        config.Baseline ??= string.Empty;
        config.Branch ??= string.Empty;
        config.Domain ??= string.Empty;
        config.ScopeTaskIds ??= [];
        config.ScopeSummary ??= string.Empty;
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
        ValidateScopeMetadata(config);
        foreach (var check in config.Checks)
        {
            ValidateCheck(check);
        }
    }

    private static void ValidateScopeMetadata(AuditPackageConfiguration config)
    {
        if (config.ScopeSummary.Any(char.IsControl))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-INVALID", "scopeSummary must not contain control characters.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var taskId in config.ScopeTaskIds)
        {
            ValidateTaskId(taskId, "audit package");
            if (!seen.Add(taskId))
            {
                throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-INVALID", $"scopeTaskIds contains a duplicate task id: {taskId}");
            }
        }

        if (config.ScopeTaskIds.Count > 0 && !config.ScopeTaskIds.Contains(config.TaskId, StringComparer.Ordinal))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-CONFIG-INVALID", "scopeTaskIds must include the primary taskId.");
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
                if (!existingPreviousVerdicts.Contains(file.Path))
                {
                    ValidateSecretPolicy(file.AbsolutePath, file.Path, config.SecretScanPolicy);
                }
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
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in config.PreviousVerdictChain)
        {
            var normalized = AuditPath.NormalizeRelativePath(path, "previousVerdictChain", allowCurrentDirectory: false);
            if (!IsPreviousVerdictPath(normalized))
            {
                throw new AuditPackageFailure(
                    "audit package",
                    "E2D-BUILD-AUDIT-CONFIG-INVALID",
                    $"previousVerdictChain must reference Markdown verdict files under docs/verdicts: {normalized}");
            }

            paths.Add(normalized);
        }

        return paths;
    }

    private static bool IsPreviousVerdictPath(string path)
    {
        return path.StartsWith("docs/verdicts/", StringComparison.Ordinal) &&
            path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
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
                ValidateEvidenceInputPath(path, config);
                var archiveOnlyPath = ToArchiveOnlyEvidenceArchivePath(path);
                var absolutePath = Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar));
                ValidateInputFileSize(absolutePath, path, config.MaxFileSize);
                ValidateSecretPolicy(absolutePath, path, config.SecretScanPolicy);
                return new EvidenceSourceFile(
                    absolutePath,
                    $"evidence/{config.TaskId}-{config.Iteration}/archive-only/{archiveOnlyPath}");
            })
            .ToArray();

        return selected;
    }

    private static string ToArchiveOnlyEvidenceArchivePath(string path)
    {
        var normalized = AuditPath.NormalizeRelativePath(path, "archive-only evidence path", allowCurrentDirectory: false);
        if (normalized.StartsWith(".temp/audit-evidence/", StringComparison.Ordinal))
        {
            return normalized[".temp/".Length..];
        }

        return normalized;
    }

    private static void ValidateEvidenceInputPath(string path, AuditPackageConfiguration config)
    {
        var normalized = AuditPath.NormalizeRelativePath(path, "archive-only evidence path", allowCurrentDirectory: false);
        if (normalized.StartsWith(".temp/audit-evidence/", StringComparison.Ordinal))
        {
            var evidenceRelativePath = normalized[".temp/audit-evidence/".Length..];
            if (ForbiddenPathPolicy.IsForbidden(evidenceRelativePath) || MatchesAny(config.ForbiddenPatterns, normalized))
            {
                throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-FORBIDDEN-PATH", $"Forbidden evidence input path: {normalized}");
            }

            return;
        }

        ValidateInputPath(normalized, config);
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
            var durationMs = NormalizeDurationMs(result.Duration);
            WriteText(localRoot, "duration-ms.txt", FormatDurationMs(durationMs) + "\n");
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
                durationMs,
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

    private async Task CreateOperatorWorkflowSidecarAsync(
        string repoRoot,
        AuditPackageConfiguration config,
        string zipPath,
        string operatorWorkflowZipPath,
        string relativeZipPath,
        CancellationToken cancellationToken)
    {
        using var staging = TemporaryWorkspace.Create("Electron2D-AuditOperatorWorkflow");
        var evidence = new List<EvidenceSourceFile>();
        var projectArgument = ResolveBuildToolProjectArgument(repoRoot);
        string[] verifyDisplayArguments =
        [
            "run",
            "--project",
            "eng/Electron2D.Build",
            "--",
            "audit",
            "package",
            "verify",
            "--zip",
            relativeZipPath,
            "--baseline",
            config.Baseline,
            "--repo",
            "<clean-repo-path>"
        ];

        string[] messageDisplayArguments =
        [
            "run",
            "--project",
            "eng/Electron2D.Build",
            "--",
            "audit",
            "package",
            "message",
            "--zip",
            relativeZipPath
        ];
        string[] messageRunArguments =
        [
            "run",
            "--project",
            projectArgument,
            "--",
            "audit",
            "package",
            "message",
            "--zip",
            relativeZipPath
        ];
        var message = await RunOperatorWorkflowCommandAsync(repoRoot, messageRunArguments, cancellationToken).ConfigureAwait(false);
        var messageEvidence = WriteCommandEvidenceFiles(
            staging.Root,
            config,
            "audit-package-message",
            "dotnet",
            messageDisplayArguments,
            message,
            OperatorWorkflowEvidenceTimeoutSeconds,
            executionMode: "subprocess");

        var provisionalVerify = new CommandEvidenceResult(
            0,
            CreateProvisionalOperatorVerifyStdout(zipPath, relativeZipPath),
            string.Empty,
            TimeSpan.FromMilliseconds(1));
        var provisionalEvidence = WriteCommandEvidenceFiles(
            staging.Root,
            config,
            "audit-package-verify",
            "dotnet",
            verifyDisplayArguments,
            provisionalVerify,
            OperatorWorkflowEvidenceTimeoutSeconds,
            executionMode: "subprocess")
            .Concat(messageEvidence)
            .ToArray();
        await WriteOperatorWorkflowSidecarAsync(
            operatorWorkflowZipPath,
            zipPath,
            relativeZipPath,
            provisionalEvidence,
            cancellationToken).ConfigureAwait(false);

        using (var cleanRepo = await CreateCleanCloneAsync(repoRoot, config.Baseline, cancellationToken).ConfigureAwait(false))
        {
            string[] verifyRunArguments =
            [
                "run",
                "--project",
                projectArgument,
                "--",
                "audit",
                "package",
                "verify",
                "--zip",
                relativeZipPath,
                "--baseline",
                config.Baseline,
                "--repo",
                cleanRepo.Root
            ];
            var verify = await RunOperatorWorkflowCommandAsync(repoRoot, verifyRunArguments, cancellationToken).ConfigureAwait(false);
            evidence.AddRange(WriteCommandEvidenceFiles(
                staging.Root,
                config,
                "audit-package-verify",
                "dotnet",
                verifyDisplayArguments,
                verify,
                OperatorWorkflowEvidenceTimeoutSeconds,
                executionMode: "subprocess"));
        }

        evidence.AddRange(messageEvidence);
        File.Delete(operatorWorkflowZipPath);
        await WriteOperatorWorkflowSidecarAsync(
            operatorWorkflowZipPath,
            zipPath,
            relativeZipPath,
            evidence,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<CommandEvidenceResult> RunOperatorWorkflowCommandAsync(
        string repoRoot,
        string[] arguments,
        CancellationToken cancellationToken)
    {
        var result = await AuditProcessRunner.RunAsync(
            "dotnet",
            arguments,
            repoRoot,
            TimeSpan.FromSeconds(OperatorWorkflowEvidenceTimeoutSeconds),
            cancellationToken).ConfigureAwait(false);
        return new CommandEvidenceResult(result.ExitCode, result.StandardOutput, result.StandardError, result.Duration);
    }

    private static EvidenceSourceFile[] WriteCommandEvidenceFiles(
        string stagingRoot,
        AuditPackageConfiguration config,
        string checkName,
        string fileName,
        string[] arguments,
        CommandEvidenceResult result,
        int timeoutSeconds,
        string? executionMode = null)
    {
        const string relativeCwd = ".";
        const int expectedExitCode = 0;
        var archiveRoot = $"evidence/{config.TaskId}-{config.Iteration}/checks/{checkName}";
        var localRoot = Path.Combine(stagingRoot, archiveRoot.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(localRoot);
        WriteText(localRoot, "command.txt", FormatCommandEvidence(fileName, arguments));
        WriteText(localRoot, "cwd.txt", relativeCwd);
        WriteText(localRoot, "env.json", JsonSerializer.Serialize(new Dictionary<string, string>(StringComparer.Ordinal), JsonWriteOptions) + "\n");
        WriteText(localRoot, "timeout-seconds.txt", timeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n");
        WriteText(localRoot, "exit-code.txt", $"expected: {expectedExitCode}\nactual: {result.ExitCode}\n");
        var durationMs = NormalizeDurationMs(result.Duration);
        WriteText(localRoot, "duration-ms.txt", FormatDurationMs(durationMs) + "\n");
        WriteText(localRoot, "stdout.txt", result.Stdout);
        WriteText(localRoot, "stderr.txt", result.Stderr);
        var metadata = new CheckEvidenceMetadata(
            checkName,
            fileName,
            arguments,
            relativeCwd,
            expectedExitCode,
            timeoutSeconds,
            result.ExitCode,
            durationMs,
            $"{archiveRoot}/stdout.txt",
            $"{archiveRoot}/stderr.txt",
            Sha256File(Path.Combine(localRoot, "stdout.txt")),
            Sha256File(Path.Combine(localRoot, "stderr.txt")),
            [],
            executionMode);
        WriteText(localRoot, "metadata.json", JsonSerializer.Serialize(metadata, JsonWriteOptions) + "\n");
        if (result.ExitCode != expectedExitCode)
        {
            throw new AuditPackageFailure(
                "audit package",
                "E2D-BUILD-AUDIT-OPERATOR-EVIDENCE-FAILED",
                $"Operator evidence check '{checkName}' exited with {result.ExitCode}; expected {expectedExitCode}.");
        }

        return Directory.EnumerateFiles(localRoot, "*", SearchOption.AllDirectories)
            .Select(path => new EvidenceSourceFile(
                path,
                $"{archiveRoot}/{Path.GetRelativePath(localRoot, path).Replace('\\', '/')}"))
            .OrderBy(file => file.ArchivePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task WriteOperatorWorkflowSidecarAsync(
        string operatorWorkflowZipPath,
        string zipPath,
        string relativeZipPath,
        IEnumerable<EvidenceSourceFile> evidence,
        CancellationToken cancellationToken)
    {
        var evidenceFiles = evidence.OrderBy(file => file.ArchivePath, StringComparer.Ordinal).ToArray();
        var sidecarFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in evidenceFiles)
        {
            sidecarFiles[file.ArchivePath] = await File.ReadAllBytesAsync(file.SourcePath, cancellationToken).ConfigureAwait(false);
        }

        AddOperatorWorkflowPayloadFiles(sidecarFiles, zipPath, relativeZipPath, evidenceFiles.Select(file => file.ArchivePath).ToArray());
        sidecarFiles["SHA256SUMS.txt"] = CreateChecksumFile(sidecarFiles);
        WriteDeterministicZip(operatorWorkflowZipPath, sidecarFiles);
    }

    private static string CreateProvisionalOperatorVerifyStdout(string zipPath, string relativeZipPath)
    {
        return JsonSerializer.Serialize(
            new BuildDiagnostic(
                "audit",
                "audit package verify",
                "info",
                "E2D-BUILD-AUDIT-PACKAGE-VERIFIED",
                $"Verified audit package '{Path.GetFileName(zipPath)}'.",
                ZipPath: relativeZipPath),
            JsonWriteOptions) + "\n";
    }

    private static void AddOperatorWorkflowPayloadFiles(
        Dictionary<string, byte[]> sidecarFiles,
        string zipPath,
        string relativeZipPath,
        string[] evidencePaths)
    {
        var entries = ReadZipEntries(zipPath, "audit package operator workflow");
        var archiveEntriesText = CreateArchiveEntriesText(entries);
        var payloadMetadata = new OperatorWorkflowPayloadMetadata(
            relativeZipPath,
            Sha256File(zipPath),
            Sha256Bytes(entries["AUDIT-MANIFEST.md"]),
            Sha256Bytes(entries["SHA256SUMS.txt"]),
            Sha256Bytes(Encoding.UTF8.GetBytes(archiveEntriesText)),
            entries.Keys.Order(StringComparer.Ordinal).ToArray());

        sidecarFiles["payload/metadata.json"] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payloadMetadata, JsonWriteOptions) + "\n");
        sidecarFiles["payload/sha256.txt"] = Encoding.UTF8.GetBytes(payloadMetadata.PayloadSha256 + "\n");
        sidecarFiles["payload/AUDIT-MANIFEST.sha256"] = Encoding.UTF8.GetBytes(payloadMetadata.AuditManifestSha256 + "\n");
        sidecarFiles["payload/SHA256SUMS.sha256"] = Encoding.UTF8.GetBytes(payloadMetadata.Sha256SumsSha256 + "\n");
        sidecarFiles["payload/archive-entries.sha256"] = Encoding.UTF8.GetBytes(payloadMetadata.ArchiveEntriesSha256 + "\n");
        sidecarFiles["payload/archive-entries.txt"] = Encoding.UTF8.GetBytes(archiveEntriesText);
        sidecarFiles["OPERATOR-WORKFLOW.md"] = Encoding.UTF8.GetBytes(CreateOperatorWorkflowManifest(payloadMetadata, evidencePaths));
    }

    private static string CreateOperatorWorkflowManifest(OperatorWorkflowPayloadMetadata payload, IEnumerable<string> evidencePaths)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# OPERATOR-WORKFLOW");
        builder.AppendLine();
        builder.AppendLine("## Payload");
        builder.AppendLine($"- path: `{payload.PayloadPath}`");
        builder.AppendLine($"- sha256: `{payload.PayloadSha256}`");
        builder.AppendLine($"- AUDIT-MANIFEST.md sha256: `{payload.AuditManifestSha256}`");
        builder.AppendLine($"- SHA256SUMS.txt sha256: `{payload.Sha256SumsSha256}`");
        builder.AppendLine($"- archive entries sha256: `{payload.ArchiveEntriesSha256}`");
        builder.AppendLine();
        builder.AppendLine("## Evidence Links");
        foreach (var path in evidencePaths.Order(StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{path}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Contract");
        builder.AppendLine("- Operator workflow evidence is stored outside the primary audit ZIP.");
        builder.AppendLine("- The primary audit ZIP is the immutable payload; this sidecar records its SHA-256, manifest hash, checksum-file hash and archive-entry list hash.");
        builder.AppendLine("- `audit-package-verify` and `audit-package-message` evidence is collected through documented CLI subprocesses after the primary payload is written.");
        return builder.ToString();
    }

    private static async Task VerifyOperatorWorkflowSidecarAsync(
        string zipPath,
        string cleanRepositoryPath,
        Dictionary<string, byte[]> payloadEntries,
        AuditPackageConfiguration config,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sidecarPath = GetOperatorWorkflowSidecarPath(zipPath);
        if (!File.Exists(sidecarPath))
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-SIDECAR-MISSING",
                $"Operator workflow sidecar was not found: {sidecarPath}",
                ZipPath: sidecarPath);
        }

        using var extractRoot = TemporaryWorkspace.Create("Electron2D-AuditOperatorWorkflowVerify");
        var sidecarEntries = ExtractAndReadZip(sidecarPath, extractRoot.Root);
        VerifyOperatorWorkflowSidecarRequiredFiles(sidecarEntries, config);
        VerifyChecksums(sidecarEntries);

        var metadata = ReadOperatorWorkflowPayloadMetadata(sidecarEntries);
        var payloadSha256 = await Sha256FileAsync(zipPath, cancellationToken).ConfigureAwait(false);
        var archiveEntriesText = CreateArchiveEntriesText(payloadEntries);
        var auditManifestSha256 = Sha256Bytes(payloadEntries["AUDIT-MANIFEST.md"]);
        var sha256SumsSha256 = Sha256Bytes(payloadEntries["SHA256SUMS.txt"]);
        var archiveEntriesSha256 = Sha256Bytes(Encoding.UTF8.GetBytes(archiveEntriesText));
        var archiveEntries = payloadEntries.Keys.Order(StringComparer.Ordinal).ToArray();

        VerifySidecarValue("payloadSha256", metadata.PayloadSha256, payloadSha256);
        VerifySidecarTextFile(sidecarEntries, "payload/sha256.txt", payloadSha256 + "\n");
        VerifySidecarValue("auditManifestSha256", metadata.AuditManifestSha256, auditManifestSha256);
        VerifySidecarTextFile(sidecarEntries, "payload/AUDIT-MANIFEST.sha256", auditManifestSha256 + "\n");
        VerifySidecarValue("sha256SumsSha256", metadata.Sha256SumsSha256, sha256SumsSha256);
        VerifySidecarTextFile(sidecarEntries, "payload/SHA256SUMS.sha256", sha256SumsSha256 + "\n");
        VerifySidecarValue("archiveEntriesSha256", metadata.ArchiveEntriesSha256, archiveEntriesSha256);
        VerifySidecarTextFile(sidecarEntries, "payload/archive-entries.sha256", archiveEntriesSha256 + "\n");
        VerifySidecarTextFile(sidecarEntries, "payload/archive-entries.txt", archiveEntriesText);
        VerifySidecarSequence("archiveEntries", metadata.ArchiveEntries, archiveEntries);

        var payloadFileName = Path.GetFileName(zipPath);
        if (string.IsNullOrWhiteSpace(metadata.PayloadPath) ||
            !string.Equals(Path.GetFileName(metadata.PayloadPath), payloadFileName, StringComparison.Ordinal))
        {
            throw OperatorWorkflowSidecarMismatch($"Sidecar payload path does not point to {payloadFileName}.");
        }

        var verifyRoot = $"evidence/{config.TaskId}-{config.Iteration}/checks/audit-package-verify";
        var messageRoot = $"evidence/{config.TaskId}-{config.Iteration}/checks/audit-package-message";
        VerifyOperatorWorkflowEvidence(
            sidecarEntries,
            verifyRoot,
            "audit-package-verify",
            requiredCommandTokens:
            [
                "audit",
                "package",
                "verify",
                "--zip",
                "--baseline",
                config.Baseline,
                "--repo",
                "<clean-repo-path>"
            ],
            forbiddenCommandText: cleanRepositoryPath,
            requiredStdoutText: "E2D-BUILD-AUDIT-PACKAGE-VERIFIED",
            expectedStdoutText: null);
        VerifyOperatorWorkflowEvidence(
            sidecarEntries,
            messageRoot,
            "audit-package-message",
            requiredCommandTokens:
            [
                "audit",
                "package",
                "message",
                "--zip"
            ],
            forbiddenCommandText: null,
            requiredStdoutText: null,
            expectedStdoutText: StripFirstMarkdownH1(Encoding.UTF8.GetString(payloadEntries[StaticAuditRequestArchivePath])));
    }

    private static string GetOperatorWorkflowSidecarPath(string zipPath)
    {
        return Path.ChangeExtension(zipPath, ".operator-workflow.zip");
    }

    private static void VerifyOperatorWorkflowSidecarRequiredFiles(
        Dictionary<string, byte[]> sidecarEntries,
        AuditPackageConfiguration config)
    {
        foreach (var required in new[]
        {
            "OPERATOR-WORKFLOW.md",
            "payload/metadata.json",
            "payload/sha256.txt",
            "payload/AUDIT-MANIFEST.sha256",
            "payload/SHA256SUMS.sha256",
            "payload/archive-entries.sha256",
            "payload/archive-entries.txt",
            "SHA256SUMS.txt"
        })
        {
            if (!sidecarEntries.ContainsKey(required))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SIDECAR-INCOMPLETE", $"Operator workflow sidecar is missing {required}.");
            }
        }

        foreach (var root in new[]
        {
            $"evidence/{config.TaskId}-{config.Iteration}/checks/audit-package-verify",
            $"evidence/{config.TaskId}-{config.Iteration}/checks/audit-package-message"
        })
        {
            foreach (var required in new[]
            {
                "command.txt",
                "stdout.txt",
                "stderr.txt",
                "exit-code.txt",
                "duration-ms.txt",
                "metadata.json",
                "cwd.txt",
                "env.json",
                "timeout-seconds.txt"
            })
            {
                var path = $"{root}/{required}";
                if (!sidecarEntries.ContainsKey(path))
                {
                    throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SIDECAR-INCOMPLETE", $"Operator workflow sidecar is missing {path}.");
                }
            }
        }
    }

    private static OperatorWorkflowPayloadMetadata ReadOperatorWorkflowPayloadMetadata(Dictionary<string, byte[]> sidecarEntries)
    {
        try
        {
            var metadata = JsonSerializer.Deserialize<OperatorWorkflowPayloadMetadata>(sidecarEntries["payload/metadata.json"], JsonReadOptions)
                ?? throw OperatorWorkflowSidecarMismatch("Operator workflow payload metadata is empty.");
            if (string.IsNullOrWhiteSpace(metadata.PayloadPath) ||
                string.IsNullOrWhiteSpace(metadata.PayloadSha256) ||
                string.IsNullOrWhiteSpace(metadata.AuditManifestSha256) ||
                string.IsNullOrWhiteSpace(metadata.Sha256SumsSha256) ||
                string.IsNullOrWhiteSpace(metadata.ArchiveEntriesSha256) ||
                metadata.ArchiveEntries is null)
            {
                throw OperatorWorkflowSidecarMismatch("Operator workflow payload metadata is incomplete.");
            }

            return metadata;
        }
        catch (JsonException exception)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow payload metadata is invalid: {exception.Message}");
        }
    }

    private static void VerifyOperatorWorkflowEvidence(
        Dictionary<string, byte[]> sidecarEntries,
        string root,
        string expectedName,
        string[] requiredCommandTokens,
        string? forbiddenCommandText,
        string? requiredStdoutText,
        string? expectedStdoutText)
    {
        var commandText = ReadSidecarText(sidecarEntries, $"{root}/command.txt");
        if (!ContainsCommandTokens(commandText, requiredCommandTokens))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow command evidence is not the documented command: {root}/command.txt.");
        }

        if (!string.IsNullOrWhiteSpace(forbiddenCommandText) && ContainsPathText(commandText, forbiddenCommandText))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow command evidence contains a local repository path: {root}/command.txt.");
        }

        var metadata = ReadOperatorWorkflowEvidenceMetadata(sidecarEntries, root);
        VerifySidecarValue("name", metadata.Name, expectedName);
        VerifySidecarValue("fileName", metadata.FileName, "dotnet");
        VerifySidecarValue("cwd", metadata.Cwd, ".");
        VerifySidecarValue("executionMode", metadata.ExecutionMode, "subprocess");
        if (!ContainsCommandTokens(string.Join("\n", metadata.Arguments), requiredCommandTokens))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow metadata arguments are not the documented command: {root}/metadata.json.");
        }

        if (!string.IsNullOrWhiteSpace(forbiddenCommandText) &&
            ContainsPathText(string.Join("\n", metadata.Arguments), forbiddenCommandText))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow metadata arguments contain a local repository path: {root}/metadata.json.");
        }

        if (metadata.ExpectedExitCode != 0 || metadata.ActualExitCode != 0)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow metadata exit codes must be 0: {root}/metadata.json.");
        }

        var (expectedExitCode, actualExitCode) = ReadExitCodeEvidence(sidecarEntries, $"{root}/exit-code.txt");
        if (expectedExitCode != 0 || actualExitCode != 0)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow exit-code evidence must be 0: {root}/exit-code.txt.");
        }

        var timeoutSeconds = ReadIntEvidence(sidecarEntries, $"{root}/timeout-seconds.txt");
        if (timeoutSeconds != OperatorWorkflowEvidenceTimeoutSeconds ||
            metadata.TimeoutSeconds != OperatorWorkflowEvidenceTimeoutSeconds ||
            metadata.TimeoutSeconds != timeoutSeconds)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow timeout evidence does not match {OperatorWorkflowEvidenceTimeoutSeconds} seconds: {root}.");
        }

        var durationMs = ReadDoubleEvidence(sidecarEntries, $"{root}/duration-ms.txt");
        if (durationMs <= 0 ||
            metadata.DurationMs <= 0 ||
            Math.Abs(metadata.DurationMs - durationMs) > 0.001d)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow duration evidence is invalid: {root}.");
        }

        VerifySidecarValue("stdoutPath", metadata.StdoutPath, $"{root}/stdout.txt");
        VerifySidecarValue("stderrPath", metadata.StderrPath, $"{root}/stderr.txt");
        VerifySidecarValue("stdoutSha256", metadata.StdoutSha256, Sha256Bytes(sidecarEntries[$"{root}/stdout.txt"]));
        VerifySidecarValue("stderrSha256", metadata.StderrSha256, Sha256Bytes(sidecarEntries[$"{root}/stderr.txt"]));

        var stdout = ReadSidecarText(sidecarEntries, $"{root}/stdout.txt");
        if (requiredStdoutText is not null && !stdout.Contains(requiredStdoutText, StringComparison.Ordinal))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow stdout does not contain the expected diagnostic: {root}/stdout.txt.");
        }

        if (expectedStdoutText is not null && !string.Equals(stdout, expectedStdoutText, StringComparison.Ordinal))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow stdout does not match the expected message body: {root}/stdout.txt.");
        }
    }

    private static CheckEvidenceMetadata ReadOperatorWorkflowEvidenceMetadata(
        Dictionary<string, byte[]> sidecarEntries,
        string root)
    {
        try
        {
            return JsonSerializer.Deserialize<CheckEvidenceMetadata>(sidecarEntries[$"{root}/metadata.json"], JsonReadOptions)
                ?? throw OperatorWorkflowSidecarMismatch($"Operator workflow evidence metadata is empty: {root}/metadata.json.");
        }
        catch (JsonException exception)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow evidence metadata is invalid: {root}/metadata.json ({exception.Message}).");
        }
    }

    private static string CreateArchiveEntriesText(Dictionary<string, byte[]> entries)
    {
        return string.Concat(entries.Keys.Order(StringComparer.Ordinal).Select(path => path + "\n"));
    }

    private static void VerifySidecarTextFile(Dictionary<string, byte[]> sidecarEntries, string path, string expected)
    {
        var actual = ReadSidecarText(sidecarEntries, path);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow sidecar value does not match: {path}.");
        }
    }

    private static string ReadSidecarText(Dictionary<string, byte[]> sidecarEntries, string path)
    {
        return Encoding.UTF8.GetString(sidecarEntries[path]);
    }

    private static void VerifySidecarValue(string name, string? actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow sidecar {name} does not match the primary audit ZIP.");
        }
    }

    private static void VerifySidecarSequence(string name, IReadOnlyList<string> actual, IReadOnlyList<string> expected)
    {
        if (actual.Count != expected.Count)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow sidecar {name} does not match the primary audit ZIP.");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal))
            {
                throw OperatorWorkflowSidecarMismatch($"Operator workflow sidecar {name} does not match the primary audit ZIP.");
            }
        }
    }

    private static bool ContainsCommandTokens(string text, params string[] tokens)
    {
        var cursor = 0;
        foreach (var token in tokens)
        {
            var index = text.IndexOf(token, cursor, StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }

            cursor = index + token.Length;
        }

        return true;
    }

    private static bool ContainsPathText(string text, string path)
    {
        var fullPath = Path.GetFullPath(path);
        return text.Contains(fullPath, StringComparison.OrdinalIgnoreCase) ||
            text.Replace('\\', '/').Contains(fullPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }

    private static (int ExpectedExitCode, int ActualExitCode) ReadExitCodeEvidence(
        Dictionary<string, byte[]> sidecarEntries,
        string path)
    {
        var text = ReadSidecarText(sidecarEntries, path);
        int? expected = null;
        int? actual = null;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("expected: ", StringComparison.Ordinal))
            {
                expected = int.Parse(line["expected: ".Length..], System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (line.StartsWith("actual: ", StringComparison.Ordinal))
            {
                actual = int.Parse(line["actual: ".Length..], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (expected is null || actual is null)
        {
            throw OperatorWorkflowSidecarMismatch($"Operator workflow exit-code evidence is invalid: {path}.");
        }

        return (expected.Value, actual.Value);
    }

    private static int ReadIntEvidence(Dictionary<string, byte[]> sidecarEntries, string path)
    {
        return int.Parse(ReadSidecarText(sidecarEntries, path).Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static double ReadDoubleEvidence(Dictionary<string, byte[]> sidecarEntries, string path)
    {
        return double.Parse(ReadSidecarText(sidecarEntries, path).Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static AuditPackageFailure OperatorWorkflowSidecarMismatch(string message)
    {
        return new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SIDECAR-MISMATCH", message);
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
            ValidateEvidenceInputPath(trxPath, config);
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
        var fullRepoRoot = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        replacements[fullRepoRoot] = "<repo-root>";
        replacements[fullRepoRoot.Replace('\\', '/')] = "<repo-root>";

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
        string auditRequestPath,
        CancellationToken cancellationToken)
    {
        var snapshot = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in repoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            snapshot[$"repo:{file.Path}"] = File.Exists(file.AbsolutePath)
                ? await Sha256FileAsync(file.AbsolutePath, cancellationToken).ConfigureAwait(false)
                : "<deleted>";
        }

        snapshot[$"request:{StaticAuditRequestSourcePath}"] = File.Exists(auditRequestPath)
            ? await Sha256FileAsync(auditRequestPath, cancellationToken).ConfigureAwait(false)
            : "<missing>";

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
        ISet<string> previousVerdictPaths,
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

        var patchText = await CreatePatchTextFromTemporaryIndexAsync(repoRoot, stagingRoot, baseline, env, Array.Empty<string>(), cancellationToken).ConfigureAwait(false);
        var binaryFallbackPaths = SelectBinaryPatchFallbackPaths(patchText, previousVerdictPaths);
        if (binaryFallbackPaths.Count > 0)
        {
            patchText = await CreatePatchTextFromTemporaryIndexAsync(repoRoot, stagingRoot, baseline, env, binaryFallbackPaths, cancellationToken).ConfigureAwait(false);
        }

        var nameStatus = await GitRunner.RunAsync(
            repoRoot,
            ["diff", "--cached", "--find-renames", "--name-status", baseline, "--"],
            env,
            cancellationToken).ConfigureAwait(false);
        if (nameStatus.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git diff --name-status failed: {nameStatus.StandardError}");
        }

        VerifyPatchControlPaths(patchText);
        return new PatchResult(patchText, nameStatus.StandardOutput);
    }

    private static async Task<string> CreatePatchTextFromTemporaryIndexAsync(
        string repoRoot,
        string stagingRoot,
        string baseline,
        Dictionary<string, string> environment,
        IReadOnlyCollection<string> binaryDiffPaths,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>();
        if (binaryDiffPaths.Count > 0)
        {
            var attributesPath = await WriteBinaryDiffAttributesAsync(stagingRoot, binaryDiffPaths, cancellationToken).ConfigureAwait(false);
            arguments.Add("-c");
            arguments.Add($"core.attributesFile={attributesPath.Replace('\\', '/')}");
        }

        arguments.AddRange(["diff", "--cached", "--find-renames", "--binary", "--full-index", "--no-ext-diff", baseline, "--"]);

        var patch = await GitRunner.RunAsync(
            repoRoot,
            arguments.ToArray(),
            environment,
            cancellationToken).ConfigureAwait(false);
        if (patch.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git diff failed: {patch.StandardError}");
        }

        return patch.StandardOutput;
    }

    private static async Task<string> WriteBinaryDiffAttributesAsync(
        string stagingRoot,
        IEnumerable<string> binaryDiffPaths,
        CancellationToken cancellationToken)
    {
        var attributesPath = Path.Combine(stagingRoot, "binary-diff.gitattributes");
        var builder = new StringBuilder();
        foreach (var path in binaryDiffPaths.Order(StringComparer.Ordinal))
        {
            builder.Append(FormatGitAttributesPathPattern(path));
            builder.AppendLine(" -diff");
        }

        await File.WriteAllTextAsync(attributesPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken).ConfigureAwait(false);
        return attributesPath;
    }

    private static string FormatGitAttributesPathPattern(string path)
    {
        if (path.Length == 0 ||
            path[0] is '#' or '!' ||
            path.Any(character => char.IsWhiteSpace(character) || character is '"' or '\\'))
        {
            return "\"" + path.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }

        return path;
    }

    private static HashSet<string> SelectBinaryPatchFallbackPaths(string patch, ISet<string> previousVerdictPaths)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var currentHeader = string.Empty;
        var currentBlock = new StringBuilder();

        foreach (var line in patch.Split('\n'))
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                AddCurrentBlock();
                currentHeader = line.TrimEnd('\r');
                currentBlock.Clear();
            }

            if (currentHeader.Length > 0)
            {
                currentBlock.Append(line);
                currentBlock.Append('\n');
            }
        }

        AddCurrentBlock();
        return paths;

        void AddCurrentBlock()
        {
            if (currentHeader.Length == 0)
            {
                return;
            }

            var blockPaths = GetPatchDiffLinePaths(currentHeader);
            if (blockPaths.Length == 0 ||
                blockPaths.Any(path => previousVerdictPaths.Contains(path)) ||
                !PatchDiffBlockContainsSecret(currentBlock.ToString()))
            {
                return;
            }

            foreach (var path in blockPaths)
            {
                paths.Add(path);
            }
        }
    }

    private static string[] GetPatchDiffLinePaths(string line)
    {
        var normalizedLine = line.TrimEnd('\r');
        const string prefix = "diff --git a/";
        if (!normalizedLine.StartsWith(prefix, StringComparison.Ordinal))
        {
            return [];
        }

        var separator = normalizedLine.IndexOf(" b/", prefix.Length, StringComparison.Ordinal);
        if (separator < 0)
        {
            return [];
        }

        var left = normalizedLine[prefix.Length..separator];
        var right = normalizedLine[(separator + " b/".Length)..];
        if (left.Length == 0 || right.Length == 0)
        {
            return [];
        }

        return string.Equals(left, right, StringComparison.Ordinal)
            ? [left]
            : [left, right];
    }

    private static bool PatchDiffBlockContainsSecret(string block)
    {
        return PrivateKeyPattern.IsMatch(block) || ContainsSecretAssignment(block);
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

    private static async Task<RepositoryFileSnapshotArchive> CreateRepositoryFileSnapshotArchiveAsync(
        string repoRoot,
        AuditPackageConfiguration config,
        SelectedRepositoryFile[] repoFiles,
        CancellationToken cancellationToken)
    {
        var files = new List<RepositoryFileSnapshot>();
        var archiveFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in repoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            byte[]? afterBytes = file.Exists ? ReadRestorableFileBytes(file.AbsolutePath) : null;
            byte[]? beforeBytes = file.ExistsInBaseline
                ? NormalizeRestorableBytes(await ReadBaselineFileBytesAsync(repoRoot, config.Baseline, file.Path, cancellationToken).ConfigureAwait(false))
                : null;
            if (afterBytes is not null && afterBytes.LongLength > config.MaxFileSize)
            {
                throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-FILE-TOO-LARGE", $"After snapshot exceeds maxFileSize: {file.Path}");
            }

            if (beforeBytes is not null && beforeBytes.LongLength > config.MaxFileSize)
            {
                throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-FILE-TOO-LARGE", $"Before snapshot exceeds maxFileSize: {file.Path}");
            }

            var afterSnapshot = afterBytes is not null ? $"repo-after/{file.Path}" : null;
            var beforeSnapshot = beforeBytes is not null ? $"repo-before/{file.Path}" : null;
            var afterSha256 = afterBytes is not null ? Sha256Bytes(afterBytes) : null;
            var beforeSha256 = beforeBytes is not null ? Sha256Bytes(beforeBytes) : null;
            var contentBytes = afterBytes ?? beforeBytes ?? [];
            var snapshot = new RepositoryFileSnapshot(
                file.Path,
                ResolveSnapshotStatus(afterSha256, beforeSha256),
                afterSnapshot,
                beforeSnapshot,
                afterSha256,
                beforeSha256,
                IsTextBytes(contentBytes) ? "text" : "binary",
                FullContentIncluded: true);
            files.Add(snapshot);

            if (afterSnapshot is not null && afterBytes is not null)
            {
                archiveFiles[afterSnapshot] = afterBytes;
            }

            if (beforeSnapshot is not null && beforeBytes is not null)
            {
                archiveFiles[beforeSnapshot] = beforeBytes;
            }
        }

        return new RepositoryFileSnapshotArchive(
            new RepositoryFileSnapshotManifest(config.TaskId, config.Iteration, config.Baseline, files),
            archiveFiles);
    }

    private static string ResolveSnapshotStatus(string? afterSha256, string? beforeSha256)
    {
        if (afterSha256 is not null && beforeSha256 is not null)
        {
            return string.Equals(afterSha256, beforeSha256, StringComparison.Ordinal)
                ? "unchanged"
                : "modified";
        }

        return afterSha256 is not null ? "added" : "deleted";
    }

    private static async Task<byte[]> ReadBaselineFileBytesAsync(
        string repoRoot,
        string baseline,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in new[] { "-c", "core.quotepath=false", "-c", "core.autocrlf=false", "-c", "core.safecrlf=false", "show", $"{baseline}:{relativePath}" })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", "Failed to start git.");
        await using var memory = new MemoryStream();
        var copyTask = process.StandardOutput.BaseStream.CopyToAsync(memory, cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await copyTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-GIT-FAILED", $"git show failed for {relativePath}: {stderr}");
        }

        return memory.ToArray();
    }

    private static async Task<StaticAuditRequest> ReadStaticAuditRequestAsync(string repoRoot, CancellationToken cancellationToken)
    {
        var absolutePath = Path.Combine(repoRoot, StaticAuditRequestSourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            throw new AuditPackageFailure(
                "audit package",
                "E2D-BUILD-AUDIT-REQUEST-MISSING",
                $"Static audit request was not found: {StaticAuditRequestSourcePath}");
        }

        var bytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken).ConfigureAwait(false);
        ValidateStaticAuditRequestBytes(bytes, repoRoot, "audit package");
        return new StaticAuditRequest(absolutePath, bytes);
    }

    private static void ValidateStaticAuditRequestBytes(byte[] bytes, string repoRoot, string step)
    {
        if (bytes.Length == 0 || !IsTextBytes(bytes))
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-REQUEST-INVALID", $"{StaticAuditRequestSourcePath} is empty or not UTF-8 text.");
        }

        var text = Encoding.UTF8.GetString(bytes);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-REQUEST-INVALID", $"{StaticAuditRequestSourcePath} is empty.");
        }

        var meaningfulLines = text
            .Split('\n')
            .Count(line => !string.IsNullOrWhiteSpace(line));
        if (meaningfulLines <= 1)
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-REQUEST-INVALID", $"{StaticAuditRequestSourcePath} looks like a one-line stub.");
        }

        ValidateMachineLocalPathText(text, StaticAuditRequestSourcePath, repoRoot);
        ValidateSecretText(text, StaticAuditRequestSourcePath, step);
        foreach (var marker in StaticAuditRequestRequiredMarkers)
        {
            if (!text.Contains(marker, StringComparison.Ordinal))
            {
                throw new AuditPackageFailure(
                    step,
                    "E2D-BUILD-AUDIT-REQUEST-INVALID",
                    $"{StaticAuditRequestSourcePath} is missing required marker: {marker}");
            }
        }
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
        builder.AppendLine($"- scopeTaskIds: {FormatManifestTaskIdList(GetEffectiveScopeTaskIds(config))}");
        if (!string.IsNullOrWhiteSpace(config.ScopeSummary))
        {
            builder.AppendLine($"- scopeSummary: {config.ScopeSummary}");
        }

        builder.AppendLine($"- requestSource: `{StaticAuditRequestSourcePath}`");
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
        builder.AppendLine("## Scope");
        foreach (var taskId in GetEffectiveScopeTaskIds(config))
        {
            builder.AppendLine($"- `{taskId}`");
        }

        if (!string.IsNullOrWhiteSpace(config.ScopeSummary))
        {
            builder.AppendLine($"- summary: {config.ScopeSummary}");
        }

        builder.AppendLine();
        builder.AppendLine("## Previous Verdict Chain");
        if (config.PreviousVerdictChain.Count == 0)
        {
            builder.AppendLine("- <none>");
        }
        else
        {
            foreach (var path in config.PreviousVerdictChain.Order(StringComparer.Ordinal))
            {
                builder.AppendLine($"- `{path}`");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Blocker Closure List");
        if (config.BlockerClosureList.Count == 0)
        {
            builder.AppendLine("- <none>");
        }
        else
        {
            foreach (var closure in config.BlockerClosureList)
            {
                builder.AppendLine($"- {closure}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Restore Model");
        builder.AppendLine("- `SHA256SUMS.txt` covers all archive files except itself.");
        builder.AppendLine($"- `{config.TaskId}.patch` restores repository-owned files from baseline.");
        builder.AppendLine("- `repo-file-hashes.json` contains expected restored file hashes.");
        builder.AppendLine("- `evidence/` files are archive-only and are not applied to the repository.");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        var configuredChecks = config.Checks
            .Select(check => new ManifestCheck(check.Name, check.ExpectedExitCode))
            .Concat(ExtractEvidenceCheckNames(evidencePaths).Select(name => new ManifestCheck(name, 0)))
            .GroupBy(check => check.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(check => check.Name, StringComparer.Ordinal);
        foreach (var check in configuredChecks)
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

    private static IReadOnlyList<string> GetEffectiveScopeTaskIds(AuditPackageConfiguration config)
    {
        return config.ScopeTaskIds.Count > 0 ? config.ScopeTaskIds : [config.TaskId];
    }

    private static string FormatManifestTaskIdList(IEnumerable<string> taskIds)
    {
        return string.Join(", ", taskIds.Select(taskId => $"`{taskId}`"));
    }

    private static void RefreshManifestAndChecksums(
        Dictionary<string, byte[]> archiveFiles,
        AuditPackageConfiguration config,
        string nameStatus,
        RestoreManifest restoreManifest,
        string[] evidencePaths)
    {
        archiveFiles.Remove("AUDIT-MANIFEST.md");
        archiveFiles.Remove("SHA256SUMS.txt");
        var plannedPaths = archiveFiles.Keys
            .Concat(["AUDIT-MANIFEST.md", "SHA256SUMS.txt"])
            .Order(StringComparer.Ordinal)
            .ToArray();
        var manifest = CreateManifest(config, nameStatus, restoreManifest, plannedPaths, evidencePaths);
        archiveFiles["AUDIT-MANIFEST.md"] = Encoding.UTF8.GetBytes(manifest);
        archiveFiles["SHA256SUMS.txt"] = CreateChecksumFile(archiveFiles);
    }

    private static IEnumerable<string> ExtractEvidenceCheckNames(IEnumerable<string> evidencePaths)
    {
        foreach (var path in evidencePaths)
        {
            var match = Regex.Match(path, @"\Aevidence/[^/]+/checks/(?<name>[^/]+)/", RegexOptions.CultureInvariant);
            if (match.Success)
            {
                yield return match.Groups["name"].Value;
            }
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

    private static Dictionary<string, byte[]> ReadZipEntries(string zipPath, string step)
    {
        var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        try
        {
            using var zipStream = File.OpenRead(zipPath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-PATH-INVALID", $"Directory archive entries are not allowed: {entry.FullName}");
                }

                var normalized = NormalizeArchivePathForStep(entry.FullName, step);
                if (ForbiddenPathPolicy.IsForbidden(normalized))
                {
                    throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-FORBIDDEN-PATH", $"Forbidden archive path: {normalized}");
                }

                if (!entries.TryAdd(normalized, ReadZipEntryBytes(entry)))
                {
                    throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-PATH-INVALID", $"Duplicate archive path: {normalized}");
                }
            }
        }
        catch (InvalidDataException exception)
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-ZIP-INVALID", $"Audit ZIP is invalid: {exception.Message}");
        }

        return entries;
    }

    private static string NormalizeArchivePathForStep(string path, string step)
    {
        try
        {
            return AuditPath.NormalizeArchivePath(path);
        }
        catch (AuditPackageFailure failure)
        {
            throw new AuditPackageFailure(step, failure.Code, failure.Message);
        }
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
        foreach (var required in new[] { "AUDIT-MANIFEST.md", "SHA256SUMS.txt", "repo-file-hashes.json", RepositoryFileSnapshotsArchivePath, "AUDIT-REQUEST.md", "metadata/audit-package.input.json" })
        {
            if (!entries.ContainsKey(required))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-ARCHIVE-INCOMPLETE", $"Archive is missing {required}.");
            }
        }
    }

    private static void VerifyMessageRequiredFiles(Dictionary<string, byte[]> entries)
    {
        foreach (var required in new[] { "AUDIT-REQUEST.md", "metadata/audit-package.input.json" })
        {
            if (!entries.ContainsKey(required))
            {
                throw new AuditPackageFailure("audit package message", "E2D-BUILD-AUDIT-ARCHIVE-INCOMPLETE", $"Archive is missing {required}.");
            }
        }
    }

    private static void ValidateAuditMessageFileName(string zipPath, AuditPackageConfiguration config)
    {
        var expected = $"{config.TaskId}-audit-{config.Iteration}.zip";
        var actual = Path.GetFileName(zipPath);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure(
                "audit package message",
                "E2D-BUILD-AUDIT-FILENAME-MISMATCH",
                $"Audit ZIP filename must match metadata taskId and iteration: expected {expected}, got {actual}.");
        }
    }

    private static string StripFirstMarkdownH1(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (normalized.StartsWith('\uFEFF'))
        {
            normalized = normalized[1..];
        }

        var firstLineEnd = normalized.IndexOf('\n');
        var firstLine = firstLineEnd >= 0 ? normalized[..firstLineEnd] : normalized;
        if (Regex.IsMatch(firstLine, @"\A#(?!#)\s+.+\z", RegexOptions.CultureInvariant))
        {
            normalized = firstLineEnd >= 0 ? normalized[(firstLineEnd + 1)..] : string.Empty;
        }

        return TrimLeadingBlankLines(normalized);
    }

    private static string TrimLeadingBlankLines(string text)
    {
        var start = 0;
        while (start < text.Length)
        {
            var nextLineBreak = text.IndexOf('\n', start);
            var line = nextLineBreak >= 0 ? text[start..nextLineBreak] : text[start..];
            if (!string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            if (nextLineBreak < 0)
            {
                return string.Empty;
            }

            start = nextLineBreak + 1;
        }

        return text[start..];
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
        if (!manifest.Contains($"- requestSource: `{StaticAuditRequestSourcePath}`", StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-MANIFEST-INCOMPLETE", $"Manifest does not list static audit request source: {StaticAuditRequestSourcePath}");
        }

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

    private static RestoreManifest ReadRestoreManifest(Dictionary<string, byte[]> entries, string step)
    {
        return JsonSerializer.Deserialize<RestoreManifest>(
            entries["repo-file-hashes.json"],
            JsonReadOptions) ?? throw new AuditPackageFailure(
                step,
                "E2D-BUILD-AUDIT-RESTORE-MANIFEST-INVALID",
                "repo-file-hashes.json is empty or invalid.");
    }

    private static RepositoryFileSnapshotManifest VerifyRepositoryFileSnapshotManifest(
        Dictionary<string, byte[]> entries,
        AuditPackageConfiguration config,
        RestoreManifest restoreManifest)
    {
        var manifest = JsonSerializer.Deserialize<RepositoryFileSnapshotManifest>(
            entries[RepositoryFileSnapshotsArchivePath],
            JsonReadOptions) ?? throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-SNAPSHOT-INVALID",
                $"{RepositoryFileSnapshotsArchivePath} is empty or invalid.");
        if (!string.Equals(manifest.TaskId, config.TaskId, StringComparison.Ordinal) ||
            !string.Equals(manifest.Iteration, config.Iteration, StringComparison.Ordinal) ||
            !string.Equals(manifest.Baseline, config.Baseline, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"{RepositoryFileSnapshotsArchivePath} metadata does not match archive config.");
        }

        var expectedPaths = restoreManifest.RepoFiles
            .Select(file => file.Path)
            .Concat(restoreManifest.DeletedRepoFiles)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var snapshots = new Dictionary<string, RepositoryFileSnapshot>(StringComparer.Ordinal);
        foreach (var snapshot in manifest.Files)
        {
            var normalized = AuditPath.NormalizeRelativePath(snapshot.Path, RepositoryFileSnapshotsArchivePath, allowCurrentDirectory: false);
            if (!snapshots.TryAdd(normalized, snapshot))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Duplicate repository file snapshot: {normalized}");
            }

            if (!snapshot.FullContentIncluded)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISSING", $"Repository file snapshot is not full content: {normalized}");
            }

            if (snapshot.ContentKind is not ("text" or "binary"))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Repository file snapshot has invalid contentKind: {normalized}");
            }

            var expectedStatus = ResolveSnapshotStatus(
                string.IsNullOrWhiteSpace(snapshot.AfterSha256) ? null : snapshot.AfterSha256,
                string.IsNullOrWhiteSpace(snapshot.BeforeSha256) ? null : snapshot.BeforeSha256);
            if (!string.Equals(snapshot.Status, expectedStatus, StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Repository file snapshot has invalid status for {normalized}: expected {expectedStatus}.");
            }
        }

        var actualPaths = snapshots.Keys.Order(StringComparer.Ordinal).ToArray();
        if (!expectedPaths.SequenceEqual(actualPaths, StringComparer.Ordinal))
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH",
                $"Repository file snapshots do not match repo-file-hashes.json. Expected: {FormatPathList(expectedPaths)}. Actual: {FormatPathList(actualPaths)}.");
        }

        foreach (var file in restoreManifest.RepoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var snapshot = snapshots[file.Path];
            VerifyExpectedSnapshotPath(snapshot.AfterSnapshot, $"repo-after/{file.Path}", required: true, "after", file.Path);
            VerifySnapshotArchiveEntry(entries, snapshot.AfterSnapshot, snapshot.AfterSha256, file.Sha256, true, "after", file.Path);
            if (!string.Equals(snapshot.AfterSha256, file.Sha256, StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"After snapshot SHA-256 does not match repo-file-hashes.json for {file.Path}.");
            }
        }

        foreach (var path in restoreManifest.DeletedRepoFiles.OrderBy(path => path, StringComparer.Ordinal))
        {
            var snapshot = snapshots[path];
            if (!string.IsNullOrWhiteSpace(snapshot.AfterSnapshot) || !string.IsNullOrWhiteSpace(snapshot.AfterSha256))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Deleted file snapshot must not contain an after snapshot: {path}");
            }
        }

        foreach (var snapshot in manifest.Files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            VerifyExpectedSnapshotPath(snapshot.BeforeSnapshot, $"repo-before/{snapshot.Path}", required: false, "before", snapshot.Path);
            VerifySnapshotArchiveEntry(entries, snapshot.BeforeSnapshot, snapshot.BeforeSha256, snapshot.BeforeSha256, false, "before", snapshot.Path);
        }

        VerifyNoOrphanSnapshotArchiveEntries(entries, manifest);

        return manifest;
    }

    private static void VerifyExpectedSnapshotPath(string? actualPath, string expectedPath, bool required, string role, string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(actualPath))
        {
            if (required)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISSING", $"Repository file is missing required {role} snapshot: {repositoryPath}");
            }

            return;
        }

        var normalized = AuditPath.NormalizeArchivePath(actualPath);
        if (!string.Equals(normalized, expectedPath, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH",
                $"Repository file {role} snapshot path for {repositoryPath} must be {expectedPath}, but was {normalized}.");
        }
    }

    private static void VerifyNoOrphanSnapshotArchiveEntries(Dictionary<string, byte[]> entries, RepositoryFileSnapshotManifest manifest)
    {
        var referencedSnapshotPaths = manifest.Files
            .SelectMany(file => new[] { file.AfterSnapshot, file.BeforeSnapshot })
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => AuditPath.NormalizeArchivePath(path!))
            .ToHashSet(StringComparer.Ordinal);
        var orphanSnapshotPaths = entries.Keys
            .Where(path =>
                (path.StartsWith("repo-after/", StringComparison.Ordinal) ||
                    path.StartsWith("repo-before/", StringComparison.Ordinal)) &&
                !referencedSnapshotPaths.Contains(path))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (orphanSnapshotPaths.Length > 0)
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH",
                $"Archive contains repository snapshots that are not listed in {RepositoryFileSnapshotsArchivePath}: {FormatPathList(orphanSnapshotPaths)}.");
        }
    }

    private static void VerifySnapshotArchiveEntry(
        Dictionary<string, byte[]> entries,
        string? path,
        string? actualSha256,
        string? expectedSha256,
        bool required,
        string role,
        string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            if (required)
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISSING", $"Repository file is missing required {role} snapshot: {repositoryPath}");
            }

            return;
        }

        var normalized = AuditPath.NormalizeArchivePath(path);
        if (!entries.TryGetValue(normalized, out var bytes))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISSING", $"Repository file snapshot is missing from archive: {normalized}");
        }

        var computedSha256 = Sha256Bytes(bytes);
        if (!string.Equals(actualSha256, computedSha256, StringComparison.Ordinal) ||
            !string.Equals(expectedSha256, computedSha256, StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Repository file {role} snapshot SHA-256 mismatch for {repositoryPath}.");
        }
    }

    private static async Task VerifyRepositoryFileSnapshotBaselineAsync(
        string repoRoot,
        AuditPackageConfiguration config,
        RepositoryFileSnapshotManifest manifest,
        CancellationToken cancellationToken)
    {
        foreach (var snapshot in manifest.Files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var baselineProbe = await GitRunner.RunAsync(
                repoRoot,
                ["cat-file", "-e", $"{config.Baseline}:{snapshot.Path}"],
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var existsInBaseline = baselineProbe.ExitCode == 0;
            if (existsInBaseline)
            {
                if (string.IsNullOrWhiteSpace(snapshot.BeforeSnapshot) || string.IsNullOrWhiteSpace(snapshot.BeforeSha256))
                {
                    throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISSING", $"Repository file is missing required before snapshot: {snapshot.Path}");
                }

                var expectedBeforeSha256 = Sha256Bytes(NormalizeRestorableBytes(await ReadBaselineFileBytesAsync(repoRoot, config.Baseline, snapshot.Path, cancellationToken).ConfigureAwait(false)));
                if (!string.Equals(snapshot.BeforeSha256, expectedBeforeSha256, StringComparison.Ordinal))
                {
                    throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Before snapshot SHA-256 does not match baseline for {snapshot.Path}.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(snapshot.BeforeSnapshot) || !string.IsNullOrWhiteSpace(snapshot.BeforeSha256))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Added file must not contain a before snapshot: {snapshot.Path}");
            }
        }
    }

    private static async Task VerifyRepositoryFileSnapshotAfterApplyAsync(
        string repoRoot,
        RepositoryFileSnapshotManifest manifest,
        CancellationToken cancellationToken)
    {
        foreach (var snapshot in manifest.Files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var path = Path.Combine(repoRoot, snapshot.Path.Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(snapshot.AfterSnapshot))
            {
                if (File.Exists(path))
                {
                    throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"Deleted file still exists after patch apply: {snapshot.Path}");
                }

                continue;
            }

            if (!File.Exists(path))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISSING", $"Repository file after snapshot has no matching file after patch apply: {snapshot.Path}");
            }

            var actualSha256 = Sha256Bytes(ReadRestorableFileBytes(path));
            if (!string.Equals(snapshot.AfterSha256, actualSha256, StringComparison.Ordinal))
            {
                throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-SNAPSHOT-MISMATCH", $"After snapshot SHA-256 does not match patch-applied tree for {snapshot.Path}.");
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

    private static async Task PrepareCleanRepositoryAsync(string repoRoot, string baseline, CancellationToken cancellationToken)
    {
        var workTree = await GitRunner.RunAsync(repoRoot, ["rev-parse", "--is-inside-work-tree"], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (workTree.ExitCode != 0 || !string.Equals(workTree.StandardOutput.Trim(), "true", StringComparison.Ordinal))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git rev-parse --is-inside-work-tree failed: {workTree.StandardError}");
        }

        await RunGitPreparationAsync(repoRoot, ["config", "--local", "core.autocrlf", "false"], cancellationToken).ConfigureAwait(false);
        await RunGitPreparationAsync(repoRoot, ["config", "--local", "core.eol", "lf"], cancellationToken).ConfigureAwait(false);

        var baselineCommit = await GitRunner.RunAsync(repoRoot, ["rev-parse", "--verify", $"{baseline}^{{commit}}"], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (baselineCommit.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git rev-parse --verify baseline failed: {baselineCommit.StandardError}");
        }

        var resolvedBaseline = baselineCommit.StandardOutput.Trim();
        await RunGitPreparationAsync(repoRoot, ["reset", "--hard", resolvedBaseline], cancellationToken).ConfigureAwait(false);

        var head = await GitRunner.RunAsync(repoRoot, ["rev-parse", "HEAD"], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (head.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git rev-parse failed: {head.StandardError}");
        }

        if (!string.Equals(head.StandardOutput.Trim(), resolvedBaseline, StringComparison.OrdinalIgnoreCase))
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-BASELINE-MISMATCH", "Clean repository HEAD does not match baseline.");
        }

        await RematerializeTrackedFilesAsync(repoRoot, resolvedBaseline, cancellationToken).ConfigureAwait(false);
        await RunGitPreparationAsync(repoRoot, ["clean", "-fdX"], cancellationToken).ConfigureAwait(false);

        var status = await GitRunner.RunAsync(repoRoot, ["status", "--porcelain"], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (status.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git status failed: {status.StandardError}");
        }

        if (!string.IsNullOrWhiteSpace(status.StandardOutput))
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-REPO-DIRTY",
                $"Clean repository path has local changes before restore verification. git status --porcelain output:{Environment.NewLine}{status.StandardOutput.TrimEnd()}");
        }
    }

    private static async Task RematerializeTrackedFilesAsync(string repoRoot, string baseline, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(repoRoot);
        foreach (var path in await GitListForVerifyAsync(repoRoot, ["ls-files", "-z"], cancellationToken).ConfigureAwait(false))
        {
            var normalized = AuditPath.NormalizeRelativePath(path, "tracked Git path", allowCurrentDirectory: false);
            var fullPath = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
            EnsureRepositoryChildPath(root, fullPath, normalized);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            try
            {
                File.SetAttributes(fullPath, File.GetAttributes(fullPath) & ~FileAttributes.ReadOnly);
                File.Delete(fullPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new AuditPackageFailure(
                    "audit package verify",
                    "E2D-BUILD-AUDIT-GIT-FAILED",
                    $"Failed to rematerialize tracked file {normalized}: {ex.Message}");
            }
        }

        await RunGitPreparationAsync(repoRoot, ["reset", "--hard", baseline], cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureRepositoryChildPath(string repoRoot, string fullPath, string relativePath)
    {
        var root = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-PATH-INVALID",
                $"Tracked Git path resolves outside the clean repository: {relativePath}");
        }
    }

    private static async Task RunGitPreparationAsync(string repoRoot, string[] arguments, CancellationToken cancellationToken)
    {
        var result = await GitRunner.RunAsync(repoRoot, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new AuditPackageFailure("audit package verify", "E2D-BUILD-AUDIT-GIT-FAILED", $"git {string.Join(" ", arguments)} failed: {result.StandardError}");
        }
    }

    private static async Task CleanIgnoredArtifactsAfterPatchApplyAsync(string repoRoot, RestoreManifest manifest, CancellationToken cancellationToken)
    {
        var expectedRestoredFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in manifest.RepoFiles.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            expectedRestoredFiles.Add(AuditPath.NormalizeRelativePath(file.Path, "repo-file-hashes.json", allowCurrentDirectory: false));
        }

        var ignoredPaths = (await GitListForVerifyAsync(repoRoot, ["ls-files", "--others", "--ignored", "--exclude-standard", "-z"], cancellationToken).ConfigureAwait(false))
            .Select(path => AuditPath.NormalizeRelativePath(path, "post-apply ignored file", allowCurrentDirectory: false))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var extraIgnoredPaths = ignoredPaths
            .Where(path => !expectedRestoredFiles.Contains(path))
            .ToArray();
        if (extraIgnoredPaths.Length == 0)
        {
            return;
        }

        if (!ignoredPaths.Any(expectedRestoredFiles.Contains))
        {
            await RunGitPreparationAsync(repoRoot, ["clean", "-fdX"], cancellationToken).ConfigureAwait(false);
            return;
        }

        DeleteExtraIgnoredPaths(repoRoot, extraIgnoredPaths, cancellationToken);
    }

    private static void DeleteExtraIgnoredPaths(string repoRoot, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(repoRoot);
        foreach (var relativePath in relativePaths.OrderByDescending(path => path.Length).ThenBy(path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            EnsureRepositoryChildPath(root, fullPath, relativePath);
            try
            {
                if (File.Exists(fullPath))
                {
                    File.SetAttributes(fullPath, File.GetAttributes(fullPath) & ~FileAttributes.ReadOnly);
                    File.Delete(fullPath);
                    DeleteEmptyParentDirectories(root, fullPath);
                }
                else if (Directory.Exists(fullPath) && !Directory.EnumerateFileSystemEntries(fullPath).Any())
                {
                    Directory.Delete(fullPath);
                    DeleteEmptyParentDirectories(root, fullPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new AuditPackageFailure(
                    "audit package verify",
                    "E2D-BUILD-AUDIT-GIT-FAILED",
                    $"Failed to clean ignored post-apply artifact {relativePath}: {ex.Message}");
            }
        }
    }

    private static void DeleteEmptyParentDirectories(string repoRoot, string path)
    {
        var root = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        while (!string.IsNullOrEmpty(directory) &&
            !string.Equals(directory, root, StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(directory) &&
            !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
            directory = Path.GetDirectoryName(directory);
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

    private static async Task VerifyStaticAuditRequestMatchesRestoredSourceAsync(
        string repoRoot,
        byte[] archiveRequestBytes,
        CancellationToken cancellationToken)
    {
        var sourcePath = Path.Combine(repoRoot, StaticAuditRequestSourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-REQUEST-MISMATCH",
                $"Restored static audit request was not found: {StaticAuditRequestSourcePath}");
        }

        var restoredBytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        ValidateStaticAuditRequestBytes(restoredBytes, repoRoot, "audit package verify");
        if (!archiveRequestBytes.SequenceEqual(restoredBytes))
        {
            throw new AuditPackageFailure(
                "audit package verify",
                "E2D-BUILD-AUDIT-REQUEST-MISMATCH",
                $"Archive {StaticAuditRequestArchivePath} differs from restored {StaticAuditRequestSourcePath}.");
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

    private static string ResolveBuildToolProjectArgument(string repoRoot)
    {
        var repositoryProjectPath = Path.Combine(repoRoot, "eng", "Electron2D.Build", "Electron2D.Build.csproj");
        if (File.Exists(repositoryProjectPath))
        {
            return "eng/Electron2D.Build";
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "eng", "Electron2D.Build", "Electron2D.Build.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new AuditPackageFailure(
            "audit package",
            "E2D-BUILD-AUDIT-OPERATOR-EVIDENCE-FAILED",
            "Build tool project path was not found for operator workflow subprocess evidence.");
    }

    private static string GetRepositoryRelativePath(string repoRoot, string path, string step)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(repoRoot), Path.GetFullPath(path)).Replace('\\', '/');
        if (relative.StartsWith("../", StringComparison.Ordinal) ||
            string.Equals(relative, "..", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new AuditPackageFailure(step, "E2D-BUILD-AUDIT-PATH-INVALID", $"Path must be inside the repository for portable evidence: {path}");
        }

        return relative;
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
        if (IsPatchPath(archivePath))
        {
            ValidatePatchText(text, archivePath, repoRoot, previousVerdictPaths);
            return;
        }

        ValidateMachineLocalPathText(text, archivePath, repoRoot);

        if (IsTrxPath(archivePath))
        {
            ValidateTrxSecretText(text, archivePath, "audit package");
            return;
        }

        ValidateSecretText(text, archivePath, "audit package");
    }

    private static void ValidatePatchText(
        string patch,
        string archivePath,
        string repoRoot,
        ISet<string> previousVerdictPaths)
    {
        var scanText = previousVerdictPaths.Count > 0
            ? OmitPreviousVerdictPatchBlocks(patch, previousVerdictPaths)
            : patch;
        ValidateMachineLocalPathText(scanText, archivePath, repoRoot);
        ValidateSecretText(scanText, archivePath, "audit package");
    }

    private static void ValidateMachineLocalPathText(string text, string archivePath, string repoRoot)
    {
        var normalizedText = text.Replace('\\', '/');
        if (GetNormalizedMachineLocalPathCandidates(repoRoot, Path.GetTempPath())
                .Any(candidate => normalizedText.Contains(candidate, StringComparison.OrdinalIgnoreCase)) ||
            WindowsDrivePathPattern.IsMatch(text) ||
            WindowsDrivePathPattern.IsMatch(normalizedText))
        {
            throw new AuditPackageFailure("audit package", "E2D-BUILD-AUDIT-ABSOLUTE-PATH", $"Archive content contains a machine-local path: {archivePath}");
        }
    }

    private static string[] GetNormalizedMachineLocalPathCandidates(params string[] paths)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            AddNormalizedMachineLocalPathCandidate(candidates, path);
            AddNormalizedMachineLocalPathCandidate(candidates, Path.GetFullPath(path));
        }

        return candidates.OrderByDescending(candidate => candidate.Length).ToArray();
    }

    private static void AddNormalizedMachineLocalPathCandidate(HashSet<string> candidates, string path)
    {
        var normalized = path.Replace('\\', '/').TrimEnd('/');
        if (normalized.Length <= 1)
        {
            return;
        }

        candidates.Add(normalized);

        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        if (normalized.StartsWith("/private/var/", StringComparison.Ordinal))
        {
            candidates.Add(normalized["/private".Length..]);
        }
        else if (normalized.StartsWith("/var/", StringComparison.Ordinal))
        {
            candidates.Add("/private" + normalized);
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
        var normalizedLine = line.TrimEnd('\r');
        foreach (var path in paths)
        {
            if (string.Equals(normalizedLine, $"diff --git a/{path} b/{path}", StringComparison.Ordinal))
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
        foreach (var normalizedRepoRoot in GetNormalizedMachineLocalPathCandidates(repoRoot))
        {
            if (normalized.StartsWith(normalizedRepoRoot + "/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, normalizedRepoRoot, StringComparison.OrdinalIgnoreCase))
            {
                return "<repo-root>" + normalized[normalizedRepoRoot.Length..];
            }
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
        return NormalizeRestorableBytes(File.ReadAllBytes(path));
    }

    private static byte[] NormalizeRestorableBytes(byte[] bytes)
    {
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

    private static double NormalizeDurationMs(TimeSpan duration)
    {
        return Math.Round(Math.Max(1d, duration.TotalMilliseconds), 3);
    }

    private static string FormatDurationMs(double durationMs)
    {
        return durationMs.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
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

internal sealed record AuditMessageOptions(string ZipPath);

internal sealed class AuditPackageConfiguration
{
    public string TaskId { get; set; } = string.Empty;
    public string Iteration { get; set; } = string.Empty;
    public string Baseline { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public List<string> ScopeTaskIds { get; set; } = [];
    public string ScopeSummary { get; set; } = string.Empty;
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

internal sealed record StaticAuditRequest(string AbsolutePath, byte[] Bytes);

internal sealed record EvidenceSourceFile(string SourcePath, string ArchivePath);

internal sealed record RepositoryFileSnapshotArchive(
    RepositoryFileSnapshotManifest Manifest,
    IReadOnlyDictionary<string, byte[]> ArchiveFiles);

internal sealed record RepositoryFileSnapshotManifest(
    string TaskId,
    string Iteration,
    string Baseline,
    IReadOnlyList<RepositoryFileSnapshot> Files);

internal sealed record RepositoryFileSnapshot(
    string Path,
    string Status,
    string? AfterSnapshot,
    string? BeforeSnapshot,
    string? AfterSha256,
    string? BeforeSha256,
    string ContentKind,
    bool FullContentIncluded);

internal sealed record PatchResult(string PatchText, string NameStatus);

internal sealed record ManifestCheck(string Name, int ExpectedExitCode);

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
    IReadOnlyList<TrxEvidenceFile> TrxFiles,
    string? ExecutionMode = null);

internal sealed record TrxEvidenceFile(string Path, string Sha256);

internal sealed record CopiedTrxEvidenceFile(string SourceRelativePath, string SourceAbsolutePath, TrxEvidenceFile Metadata);

internal sealed record OperatorWorkflowPayloadMetadata(
    string PayloadPath,
    string PayloadSha256,
    string AuditManifestSha256,
    string Sha256SumsSha256,
    string ArchiveEntriesSha256,
    IReadOnlyList<string> ArchiveEntries);

internal sealed record ZipCentralDirectoryEntry(
    string Name,
    bool HasUtf8Flag,
    bool NameContainsNonAsciiBytes,
    int ExternalAttributes);

internal sealed record CommandEvidenceResult(
    int ExitCode,
    string Stdout,
    string Stderr,
    TimeSpan Duration);

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
