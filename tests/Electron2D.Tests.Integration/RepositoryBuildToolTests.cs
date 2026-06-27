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
using System.Collections;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class RepositoryBuildToolTests
{
    [Fact]
    public async Task UnknownCommandReturnsNonZeroStructuredDiagnostic()
    {
        var result = await RunBuildToolAsync("__unknown__");

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("__unknown__", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("__unknown__", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-CLI-UNKNOWN-COMMAND", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.Contains("__unknown__", diagnostic.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("verify", "verify")]
    [InlineData("verify readme", "verify", "readme")]
    [InlineData("verify docs", "verify", "docs")]
    [InlineData("update wiki --check", "update", "wiki", "--check")]
    public async Task SkeletonCommandsRouteToStableDiagnosticShape(string expectedStep, params string[] arguments)
    {
        var result = await RunBuildToolAsync(arguments);

        Assert.Equal(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal(arguments[0], diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal(expectedStep, diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("info", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-ROUTED", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.False(diagnostic.RootElement.TryGetProperty("processExitCode", out _));
        Assert.False(diagnostic.RootElement.TryGetProperty("timedOut", out _));
    }

    [Fact]
    public async Task ReleaseVerifyFailsClosedWithoutArtifacts()
    {
        using var workspace = TemporaryDirectory.Create("release-verify");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "release", "verify");

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("release", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("release verify", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-RELEASE-VERIFY-BLOCKED", diagnostic.RootElement.GetProperty("code").GetString());
        AssertNoReleaseArtifacts(workspace.Root);
    }

    [Fact]
    public async Task PackageWithRidFailsClosedWithoutArtifacts()
    {
        using var workspace = TemporaryDirectory.Create("package-rid");

        var result = await RunBuildToolFromDirectoryAsync(workspace.Root, "package", "--rid", "win-x64");

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("package", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("package", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-PACKAGE-BLOCKED", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.Equal("win-x64", diagnostic.RootElement.GetProperty("runtimeIdentifier").GetString());
        AssertNoReleaseArtifacts(workspace.Root);
    }

    [Theory]
    [InlineData("missing rid flag", "package")]
    [InlineData("missing rid value", "package", "--rid")]
    [InlineData("blank rid value", "package", "--rid", "")]
    [InlineData("misplaced rid flag", "package", "win-x64", "--rid")]
    [InlineData("extra argument", "package", "--rid", "win-x64", "--extra")]
    [InlineData("duplicate rid flag", "package", "--rid", "win-x64", "--rid", "linux-x64")]
    [InlineData("unknown flag", "package", "--runtime", "win-x64")]
    public async Task PackageRejectsInvalidArgumentShape(string caseName, params string[] arguments)
    {
        var result = await RunBuildToolAsync(arguments);

        Assert.NotEqual(0, result.ExitCode);
        using var diagnostic = ReadFirstDiagnostic(result);
        Assert.Equal("package", diagnostic.RootElement.GetProperty("command").GetString());
        Assert.Equal("package", diagnostic.RootElement.GetProperty("step").GetString());
        Assert.Equal("error", diagnostic.RootElement.GetProperty("severity").GetString());
        Assert.Equal("E2D-BUILD-CLI-INVALID-ARGUMENTS", diagnostic.RootElement.GetProperty("code").GetString());
        Assert.False(
            diagnostic.RootElement.TryGetProperty("runtimeIdentifier", out _),
            $"Invalid package arguments should not accept a runtime identifier for case '{caseName}'.");
    }

    [Fact]
    public async Task ProcessRunnerCapturesOutputErrorAndChildExitCode()
    {
        using var child = await CreateBuiltChildProjectAsync(
            "exit-code",
            """
            Console.WriteLine("child stdout");
            Console.Error.WriteLine("child stderr");
            Environment.ExitCode = 37;
            """);

        var result = await RunProcessRunnerAsync(
            "child-exit",
            DotnetExecutable,
            [child.AssemblyPath],
            Path.GetDirectoryName(child.AssemblyPath)!,
            TimeSpan.FromSeconds(10));

        Assert.Equal(37, GetProperty<int?>(result, "ExitCode"));
        Assert.False(GetProperty<bool>(result, "TimedOut"));
        Assert.Contains("child stdout", GetProperty<string>(result, "StandardOutput"), StringComparison.Ordinal);
        Assert.Contains("child stderr", GetProperty<string>(result, "StandardError"), StringComparison.Ordinal);

        var diagnostic = Assert.Single(GetDiagnostics(result));
        Assert.Equal("process", GetProperty<string>(diagnostic, "Command"));
        Assert.Equal("child-exit", GetProperty<string>(diagnostic, "Step"));
        Assert.Equal("error", GetProperty<string>(diagnostic, "Severity"));
        Assert.Equal("E2D-BUILD-PROCESS-EXITED", GetProperty<string>(diagnostic, "Code"));
        Assert.Equal(37, GetProperty<int?>(diagnostic, "ProcessExitCode"));
        Assert.False(GetProperty<bool>(diagnostic, "TimedOut"));
    }

    [Fact]
    public async Task ProcessRunnerTimeoutCancelsChildAndReturnsTimeoutDiagnostic()
    {
        using var child = await CreateBuiltChildProjectAsync(
            "timeout",
            """
            using System.Threading;

            Console.WriteLine("timeout child started");
            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);

        var result = await RunProcessRunnerAsync(
            "child-timeout",
            DotnetExecutable,
            [child.AssemblyPath],
            Path.GetDirectoryName(child.AssemblyPath)!,
            TimeSpan.FromMilliseconds(250));

        Assert.Null(GetProperty<int?>(result, "ExitCode"));
        Assert.True(GetProperty<bool>(result, "TimedOut"));
        Assert.Contains("timeout child started", GetProperty<string>(result, "StandardOutput"), StringComparison.Ordinal);

        var diagnostic = Assert.Single(GetDiagnostics(result));
        Assert.Equal("process", GetProperty<string>(diagnostic, "Command"));
        Assert.Equal("child-timeout", GetProperty<string>(diagnostic, "Step"));
        Assert.Equal("error", GetProperty<string>(diagnostic, "Severity"));
        Assert.Equal("E2D-BUILD-PROCESS-TIMEOUT", GetProperty<string>(diagnostic, "Code"));
        Assert.Null(GetProperty<int?>(diagnostic, "ProcessExitCode"));
        Assert.True(GetProperty<bool>(diagnostic, "TimedOut"));
    }

    [Fact]
    public async Task ProcessRunnerExternalCancellationKillsChildAndReturnsCancellationDiagnostic()
    {
        using var child = await CreateBuiltChildProjectAsync(
            "external-cancel",
            """
            using System.Threading;

            Console.WriteLine("external cancel child started");
            Console.Out.Flush();
            Thread.Sleep(TimeSpan.FromSeconds(30));
            """);
        var assembly = await BuildAndLoadBuildToolAssemblyAsync();
        using var cancellation = new CancellationTokenSource();

        var runTask = RunProcessRunnerAsync(
            assembly,
            "child-external-cancel",
            DotnetExecutable,
            [child.AssemblyPath],
            Path.GetDirectoryName(child.AssemblyPath)!,
            TimeSpan.FromSeconds(30),
            cancellation.Token);
        cancellation.CancelAfter(TimeSpan.FromSeconds(1));

        var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.Same(runTask, completed);

        object? result = null;
        var exception = await Record.ExceptionAsync(async () => result = await runTask.ConfigureAwait(false));

        Assert.Null(exception);
        Assert.NotNull(result);
        Assert.Null(GetProperty<int?>(result, "ExitCode"));
        Assert.False(GetProperty<bool>(result, "TimedOut"));

        var diagnostic = Assert.Single(GetDiagnostics(result));
        Assert.Equal("process", GetProperty<string>(diagnostic, "Command"));
        Assert.Equal("child-external-cancel", GetProperty<string>(diagnostic, "Step"));
        Assert.Equal("error", GetProperty<string>(diagnostic, "Severity"));
        Assert.Equal("E2D-BUILD-PROCESS-CANCELED", GetProperty<string>(diagnostic, "Code"));
        Assert.Null(GetProperty<int?>(diagnostic, "ProcessExitCode"));
        Assert.False(GetProperty<bool>(diagnostic, "TimedOut"));
    }

    [Fact]
    public async Task AuditPackageGeneratesAndVerifiesMinimalFixtureRepository()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-minimal");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves that the audit package patch restores repository-owned files.
        """);
        fixture.WriteTextFile("audit-evidence/checks/result.txt", """
        fixture evidence
        second line
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-PACKAGE-CREATED");
        var zipPath = fixture.ZipPath(taskId);
        Assert.True(File.Exists(zipPath), $"Audit ZIP was not created: {zipPath}");

        var entries = ReadZipEntryNames(zipPath);
        Assert.Contains("AUDIT-MANIFEST.md", entries);
        Assert.Contains($"{taskId}.patch", entries);
        Assert.Contains("SHA256SUMS.txt", entries);
        Assert.Contains("repo-file-hashes.json", entries);
        Assert.Contains("AUDIT-REQUEST.md", entries);
        Assert.Contains("metadata/audit-package.input.json", entries);
        Assert.Contains(entries, entry => entry.StartsWith($"evidence/{taskId}-r01/checks/git-status/", StringComparison.Ordinal));
        Assert.Contains(entries, entry => entry.StartsWith($"evidence/{taskId}-r01/archive-only/", StringComparison.Ordinal));
        Assert.All(entries, AssertPosixArchivePath);

        var manifest = ReadZipEntryText(zipPath, "AUDIT-MANIFEST.md");
        Assert.Contains(taskId, manifest, StringComparison.Ordinal);
        Assert.Contains("docs/release-management/audit-fixture.md", manifest, StringComparison.Ordinal);
        Assert.Contains($"evidence/{taskId}-r01/checks/git-status/stdout.txt", manifest, StringComparison.Ordinal);

        var request = ReadZipEntryText(zipPath, "AUDIT-REQUEST.md");
        Assert.Contains($"{taskId} audit r01", request, StringComparison.Ordinal);
        Assert.Contains($"{taskId}-audit-r01.zip", request, StringComparison.Ordinal);

        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-minimal");
        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.Equal(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-PACKAGE-VERIFIED");
    }

    [Fact]
    public async Task AuditPackageWritesCheckMetadataThatMatchesRawEvidenceFiles()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-check-metadata");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves that check metadata mirrors raw evidence files.
        """);
        fixture.WriteTextFile("audit-evidence/checks/result.txt", """
        fixture evidence
        second line
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var metadata = JsonDocument.Parse(ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/metadata.json"));
        var exitCode = ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/exit-code.txt");
        var timeoutSeconds = ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/timeout-seconds.txt");

        Assert.Equal(ParseEvidenceValue(exitCode, "expected"), metadata.RootElement.GetProperty("expectedExitCode").GetInt32());
        Assert.Equal(ParseEvidenceValue(exitCode, "actual"), metadata.RootElement.GetProperty("actualExitCode").GetInt32());
        Assert.Equal(int.Parse(timeoutSeconds, System.Globalization.CultureInfo.InvariantCulture), metadata.RootElement.GetProperty("timeoutSeconds").GetInt32());
        Assert.Equal($"evidence/{taskId}-r01/checks/git-status/stdout.txt", metadata.RootElement.GetProperty("stdoutPath").GetString());
        Assert.Equal($"evidence/{taskId}-r01/checks/git-status/stderr.txt", metadata.RootElement.GetProperty("stderrPath").GetString());
    }

    [Fact]
    public async Task AuditPackageCopiesTrxEvidenceAndLinksItFromManifest()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-trx-evidence");
        const string taskId = "T-0001";
        const string sourceTrxName = "synthetic-runner_SYNTHETIC-WORKSTATION_2026-06-27_06_08_30.trx";
        var runUserAttributeName = string.Concat("run", "User");
        var computerNameAttributeName = string.Concat("computer", "Name");
        var deploymentRootAttributeName = string.Concat("run", "Deployment", "Root");
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves that raw output and TRX evidence remain inspectable.
        """);
        var trxDocument = new XDocument(
            new XElement(
                "TestRun",
                new XAttribute("id", "fixture"),
                new XAttribute("name", "synthetic-runner@SYNTHETIC-WORKSTATION 2026-06-27 06:08:30"),
                new XAttribute(runUserAttributeName, "SYNTHETIC-WORKSTATION\\synthetic-runner"),
                new XElement(
                    "Results",
                    new XElement(
                        "UnitTestResult",
                        new XAttribute("executionId", "11111111-1111-1111-1111-111111111111"),
                        new XAttribute("testId", "22222222-2222-2222-2222-222222222222"),
                        new XAttribute("testName", "Fixture.Test"),
                        new XAttribute("outcome", "Passed"),
                        new XAttribute(computerNameAttributeName, "SYNTHETIC-WORKSTATION"))),
                new XElement(
                    "Deployment",
                    new XAttribute(deploymentRootAttributeName, "synthetic-runner_SYNTHETIC-WORKSTATION_2026-06-27_06_08_30"))));
        fixture.WriteTextFile($"test-results/audit-package/{sourceTrxName}", trxDocument.ToString(SaveOptions.DisableFormatting));
        fixture.WriteTextFile("audit-evidence/checks/result.txt", """
        fixture evidence
        second line
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            checks:
            [
                new AuditFixtureCheck(
                    "git-status",
                    "git",
                    ["status", "--short", "--untracked-files=all"],
                    ".",
                    [],
                    10,
                    0,
                    ["test-results/**/*.trx"])
            ]);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var stdout = ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/stdout.txt");
        var trxPath = $"evidence/{taskId}-r01/checks/git-status/trx/test-result-001.trx";
        var trxText = ReadZipEntryText(zipPath, trxPath);
        var metadata = JsonDocument.Parse(ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/metadata.json"));
        var manifest = ReadZipEntryText(zipPath, "AUDIT-MANIFEST.md");
        var archiveText = string.Join(
            "\n",
            stdout,
            trxText,
            manifest,
            ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/metadata.json"),
            ReadZipEntryText(zipPath, "SHA256SUMS.txt"));

        Assert.Contains("docs/release-management/audit-fixture.md", stdout, StringComparison.Ordinal);
        Assert.Contains(trxPath, ReadZipEntryNames(zipPath));
        Assert.Contains($"`{trxPath}`", manifest, StringComparison.Ordinal);
        Assert.Contains("name=\"&lt;test-run&gt;\"", trxText, StringComparison.Ordinal);
        Assert.DoesNotContain(sourceTrxName, archiveText, StringComparison.Ordinal);
        Assert.DoesNotContain("SYNTHETIC-WORKSTATION", archiveText, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic-runner", archiveText, StringComparison.Ordinal);
        Assert.DoesNotContain(runUserAttributeName + "=", archiveText, StringComparison.Ordinal);
        Assert.DoesNotContain(computerNameAttributeName + "=", archiveText, StringComparison.Ordinal);
        Assert.DoesNotContain(deploymentRootAttributeName + "=", archiveText, StringComparison.Ordinal);
        var trxFile = Assert.Single(metadata.RootElement.GetProperty("trxFiles").EnumerateArray());
        Assert.Equal(trxPath, trxFile.GetProperty("path").GetString());
        Assert.Equal(Sha256ZipEntry(zipPath, trxPath), trxFile.GetProperty("sha256").GetString());
    }

    [Fact]
    public async Task AuditPackageCopiesDirectChildTrxMatchedByRecursiveGlob()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-direct-trx-evidence");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves recursive TRX globs include direct child files.
        """);
        fixture.WriteTextFile("test-results/results.trx", """
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun id="direct-child">
          <ResultSummary outcome="Completed" />
        </TestRun>
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            checks:
            [
                new AuditFixtureCheck(
                    "git-status",
                    "git",
                    ["status", "--short"],
                    ".",
                    [],
                    10,
                    0,
                    ["test-results/**/*.trx"])
            ]);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var trxPath = $"evidence/{taskId}-r01/checks/git-status/trx/test-result-001.trx";
        var metadata = JsonDocument.Parse(ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/metadata.json"));

        Assert.Contains(trxPath, ReadZipEntryNames(zipPath));
        var trxFile = Assert.Single(metadata.RootElement.GetProperty("trxFiles").EnumerateArray());
        Assert.Equal(trxPath, trxFile.GetProperty("path").GetString());
        Assert.Equal(Sha256ZipEntry(zipPath, trxPath), trxFile.GetProperty("sha256").GetString());
    }

    [Fact]
    public async Task AuditPackageCopiesFocusedTestTrxWithSyntheticSecretCaseNames()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-focused-trx-secret-fixtures");
        const string taskId = "T-0001";
        var codeBasePath = Path.Combine(
            fixture.RepositoryRoot,
            "tests",
            "Electron2D.Tests.Integration",
            "bin",
            "Debug",
            "net10.0",
            "Electron2D.Tests.Integration.dll");
        var storagePath = codeBasePath.ToLowerInvariant();
        var syntheticTokenValue = SecretAssignment("token", "not-a-real-token-value");
        var syntheticPasswordValue = SecretAssignment("password", "not-a-real-password-value");
        var syntheticPrivateKeyValue = PrivateKeyStartMarker();
        var syntheticTokenCaseName = SecretFixtureCaseName(syntheticTokenValue);
        var syntheticPasswordCaseName = SecretFixtureCaseName(syntheticPasswordValue);
        var syntheticPrivateKeyCaseName = SecretFixtureCaseName(syntheticPrivateKeyValue);
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves focused test TRX evidence remains inspectable.
        """);
        fixture.WriteTextFile("test-results/focused/results.trx", $"""
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun id="focused">
          <Results>
            <UnitTestResult executionId="11111111-1111-1111-1111-111111111111" testId="22222222-2222-2222-2222-222222222222" testName="{syntheticTokenCaseName}" outcome="Passed" />
            <UnitTestResult executionId="33333333-3333-3333-3333-333333333333" testId="44444444-4444-4444-4444-444444444444" testName="{syntheticPrivateKeyCaseName}" outcome="Passed" />
          </Results>
          <TestDefinitions>
            <UnitTest name="{syntheticPasswordCaseName}" storage="{storagePath}" id="22222222-2222-2222-2222-222222222222">
              <TestMethod codeBase="{codeBasePath}" adapterTypeName="executor://xunit/VsTestRunner3/netcore/" className="Electron2D.Tests.Integration.RepositoryBuildToolTests" name="AuditPackageRejectsSecretValuesInSelectedTextFiles" />
            </UnitTest>
          </TestDefinitions>
        </TestRun>
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            checks:
            [
                new AuditFixtureCheck(
                    "git-status",
                    "git",
                    ["status", "--short"],
                    ".",
                    [],
                    10,
                    0,
                    ["test-results/**/*.trx"])
            ]);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var trxPath = $"evidence/{taskId}-r01/checks/git-status/trx/test-result-001.trx";
        var trxText = ReadZipEntryText(zipPath, trxPath);
        var metadata = JsonDocument.Parse(ReadZipEntryText(zipPath, $"evidence/{taskId}-r01/checks/git-status/metadata.json"));
        var manifest = ReadZipEntryText(zipPath, "AUDIT-MANIFEST.md");

        Assert.Contains(trxPath, ReadZipEntryNames(zipPath));
        Assert.Contains($"`{trxPath}`", manifest, StringComparison.Ordinal);
        Assert.Contains(syntheticTokenValue, trxText, StringComparison.Ordinal);
        Assert.Contains("&lt;repo-root&gt;/tests/Electron2D.Tests.Integration", trxText, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.RepositoryRoot, trxText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.RepositoryRoot.Replace('\\', '/'), trxText, StringComparison.OrdinalIgnoreCase);
        var trxFile = Assert.Single(metadata.RootElement.GetProperty("trxFiles").EnumerateArray());
        Assert.Equal(trxPath, trxFile.GetProperty("path").GetString());
        Assert.Equal(Sha256ZipEntry(zipPath, trxPath), trxFile.GetProperty("sha256").GetString());
    }

    [Fact]
    public async Task AuditPackageRejectsSecretValuesInTrxResultText()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-trx-secret-result-text");
        const string taskId = "T-0001";
        var secretLine = string.Concat("token", "=real-trx-secret-value");
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves TRX result text is still secret-scanned.
        """);
        fixture.WriteTextFile("test-results/results.trx", $"""
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun id="focused">
          <Results>
            <UnitTestResult executionId="11111111-1111-1111-1111-111111111111" testId="22222222-2222-2222-2222-222222222222" testName="Electron2D.Tests.Integration.RepositoryBuildToolTests.LeaksOutput" outcome="Passed">
              <Output>
                <StdOut>{secretLine}</StdOut>
              </Output>
            </UnitTestResult>
          </Results>
        </TestRun>
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            checks:
            [
                new AuditFixtureCheck(
                    "git-status",
                    "git",
                    ["status", "--short"],
                    ".",
                    [],
                    10,
                    0,
                    ["test-results/**/*.trx"])
            ]);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-SECRET-DETECTED");
    }

    [Fact]
    public async Task AuditPackageUsesDefaultConfigPathWhenConfigArgumentIsOmitted()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-default-config");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves that the default config path is part of the command contract.
        """);
        fixture.WriteTextFile("audit-evidence/checks/result.txt", """
        fixture evidence
        second line
        """);
        fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath: null);

        Assert.Equal(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-PACKAGE-CREATED");
        Assert.True(File.Exists(fixture.ZipPath(taskId)));
    }

    [Fact]
    public async Task AuditPackageCreatesByteStableZipWhenForced()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-deterministic");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves that package bytes stay stable across repeated creation.
        """);
        fixture.WriteTextFile("audit-evidence/checks/result.txt", """
        fixture evidence
        second line
        """);
        var configPath = fixture.WriteConfig(taskId);

        var first = await RunAuditPackageAsync(fixture, taskId, configPath);
        Assert.Equal(0, first.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var firstHash = Sha256File(zipPath);

        var second = await RunAuditPackageAsync(fixture, taskId, configPath, force: true);
        Assert.Equal(0, second.ExitCode);

        Assert.Equal(firstHash, Sha256File(zipPath));
        using var archive = ZipFile.OpenRead(zipPath);
        var entryNames = archive.Entries.Select(entry => entry.FullName).ToArray();
        Assert.Equal(entryNames.Order(StringComparer.Ordinal), entryNames);
        Assert.All(archive.Entries, entry => Assert.Equal(DeterministicZipTimestamp.DateTime, entry.LastWriteTime.DateTime));
    }

    [Fact]
    public async Task AuditPackageWritesDeterministicUtf8ZipMetadataForUnicodeArchiveEvidence()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-unicode-archive-entry");
        const string taskId = "T-0001";
        const string unicodeEvidencePath = "audit-evidence/логи/проверка.txt";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves that Unicode archive evidence paths are preserved.
        """);
        fixture.WriteTextFile(unicodeEvidencePath, """
        evidence with a Cyrillic path
        second line
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var expectedArchivePath = $"evidence/{taskId}-r01/archive-only/{unicodeEvidencePath}";
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)).ToArray();
        var entryNames = entries.Select(entry => entry.FullName).ToArray();
        Assert.Contains(expectedArchivePath, entryNames);
        Assert.Equal(entryNames.Order(StringComparer.Ordinal), entryNames);
        Assert.All(entries, entry => Assert.Equal(DeterministicZipTimestamp.DateTime, entry.LastWriteTime.DateTime));
        Assert.All(entries, entry => Assert.Equal(0, entry.ExternalAttributes));

        var centralDirectory = ReadZipCentralDirectory(zipPath);
        Assert.Equal(entryNames, centralDirectory.Select(entry => entry.Name).ToArray());
        Assert.All(centralDirectory, entry => Assert.Equal(0, entry.ExternalAttributes));
        Assert.All(
            centralDirectory.Where(entry => entry.Name.Any(ch => ch > 127)),
            entry => Assert.True(entry.HasUtf8Flag, $"ZIP entry with non-ASCII path must set UTF-8 flag: {entry.Name}"));

        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-unicode-archive-entry");
        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.Equal(0, verify.ExitCode);
    }

    [Fact]
    public async Task AuditPackagePreservesUnicodeRepositoryPathsInPatchAndVerify()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-unicode");
        const string taskId = "T-0001";
        var diaryPath = "dev-diary/2026/06 Июнь/26-06-2026.md";
        fixture.WriteTextFile(diaryPath, """
        # Дневник разработки: 26-06-2026

        ## 18:10 +03:00 - Agent: Codex

        - Действия:
          - 18:10 - добавлена Unicode-фикстура для проверки путей.
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: [diaryPath],
            archiveOnlyEvidenceGlobs: []);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var patch = ReadZipEntryText(zipPath, $"{taskId}.patch");
        var manifest = ReadZipEntryText(zipPath, "AUDIT-MANIFEST.md");
        Assert.Contains(diaryPath, patch, StringComparison.Ordinal);
        Assert.Contains(diaryPath, manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("????", patch, StringComparison.Ordinal);
        AssertPatchControlLinesUsePosixPaths(patch);

        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-unicode");
        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.Equal(0, verify.ExitCode);
    }

    [Fact]
    public async Task AuditPackageAllowsDocumentedSecretPolicyWordsWithoutSecretValues()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-policy-words");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-policy.md", """
        # Audit policy fixture

        The documentation names `secretScanPolicy`, `secret`, `token`, `password`,
        and `api_key` as forbidden policy terms without carrying real credentials.
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-PACKAGE-CREATED");
    }

    [Fact]
    public async Task AuditPackageAllowsAuditPackageDomainDocumentSecretScanDescription()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-domain-document-secret-policy");
        const string taskId = "T-0001";
        var syntheticTokenPlaceholder = SecretAssignment("token", "...");
        fixture.WriteTextFile("docs/release-management/audit-package.md", $"""
        # Deterministic audit package

        The `secretScanPolicy` field selects the basic secret scanning policy.
        TRX evidence may include display names with synthetic examples like `{syntheticTokenPlaceholder}`.
        Documentation may name `secret`, `token`, `password`, `api_key`, and `api-key`
        while describing forbidden assignments without carrying real credentials.
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-PACKAGE-CREATED");
    }

    [Fact]
    public async Task AuditPackageRejectsConcreteSecretAssignmentsInDomainDocumentation()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-domain-document-real-secret");
        const string taskId = "T-0001";
        var secretLine = string.Concat("token", "=real-domain-document-secret-value");
        fixture.WriteTextFile("docs/release-management/audit-package.md", $"""
        # Deterministic audit package

        This file must still fail if documentation accidentally contains a concrete credential.

        {secretLine}
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-SECRET-DETECTED");
    }

    [Fact]
    public async Task AuditPackageAllowsSyntheticSecretPlaceholdersFollowedByProse()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-prose-secret-placeholders");
        const string taskId = "T-0001";
        var syntheticTokenPlaceholder = SecretAssignment("token", "...");
        var redactedPasswordPlaceholder = SecretAssignment("password", "<redacted>");
        fixture.WriteTextFile("TASKS.md", $"""
        # Workflow notes

        The scanner allows synthetic examples like `{syntheticTokenPlaceholder}`: ordinary prose continues on the same line.
        The scanner also allows replacement markers like `{redactedPasswordPlaceholder}`; ordinary prose continues after the marker.
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: ["TASKS.md"],
            archiveOnlyEvidenceGlobs: []);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-PACKAGE-CREATED");
    }

    [Fact]
    public async Task AuditPackageRejectsConcreteSecretAssignmentsFollowedByProse()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-prose-real-secret");
        const string taskId = "T-0001";
        var secretLine = string.Concat("token", "=real-workflow-secret-value");
        fixture.WriteTextFile("TASKS.md", $"""
        # Workflow notes

        The scanner must reject {secretLine}: ordinary prose continues on the same line.
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: ["TASKS.md"],
            archiveOnlyEvidenceGlobs: []);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-SECRET-DETECTED");
    }

    [Theory]
    [MemberData(nameof(SecretValueCases))]
    public async Task AuditPackageRejectsSecretValuesInSelectedTextFiles(string secretLine)
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-secret-values");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/credential-fixture.md", $"""
        # Credential fixture

        {secretLine}
        second line
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-SECRET-DETECTED");
    }

    public static IEnumerable<object[]> SecretValueCases()
    {
        yield return [SecretAssignment("token", "not-a-real-token-value")];
        yield return [SecretAssignment("password", "not-a-real-password-value")];
        yield return [SecretAssignment("api_key", "not-a-real-api-key-value", separator: " = ")];
        yield return [PrivateKeyStartMarker()];
    }

    private static string SecretAssignment(string key, string value, string separator = "=")
    {
        return string.Concat(key, separator, value);
    }

    private static string PrivateKeyStartMarker()
    {
        return string.Concat("-----BEGIN ", "PRIVATE KEY-----");
    }

    private static string SecretFixtureCaseName(string secretLine)
    {
        return $"Electron2D.Tests.Integration.RepositoryBuildToolTests.AuditPackageRejectsSecretValuesInSelectedTextFiles(secretLine: &quot;{secretLine}&quot;)";
    }

    [Theory]
    [InlineData(".env")]
    [InlineData("docs/release-management/token.txt")]
    [InlineData("docs/release-management/secret-export.md")]
    public async Task AuditPackageRejectsDangerousSecretPathNames(string relativePath)
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-secret-paths");
        const string taskId = "T-0001";
        fixture.WriteTextFile(relativePath, """
        # Dangerous path fixture

        The file body is harmless, but the path is reserved for secret-bearing material.
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: [relativePath],
            archiveOnlyEvidenceGlobs: []);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-FORBIDDEN-PATH");
    }

    [Fact]
    public async Task AuditPackageRestoresIgnoredTasksAllowlistFileInPatchAndVerify()
    {
        using var fixture = await AuditFixture.CreateAsync(
            "audit-package-ignored-allowlist",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [".gitignore"] = "/TASKS.md\n/dev-diary/\n"
            });
        const string taskId = "T-0001";
        fixture.WriteTextFile("TASKS.md", """
        # TASKS

        - T-0001: fixture task that is ignored by Git but owned by the audit package allowlist.
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: ["TASKS.md"],
            archiveOnlyEvidenceGlobs: []);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var patch = ReadZipEntryText(zipPath, $"{taskId}.patch");
        Assert.Contains("TASKS.md", patch, StringComparison.Ordinal);

        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-ignored-allowlist");
        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.Equal(0, verify.ExitCode);
        Assert.True(File.Exists(Path.Combine(cleanRepo.Root, "TASKS.md")));
    }

    [Fact]
    public async Task AuditPackageNormalizesTextLineEndingsInPatchWithoutFullFileRewrite()
    {
        using var fixture = await AuditFixture.CreateAsync(
            "audit-package-line-ending-churn",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["docs/release-management/audit-fixture.md"] = "line one\nline two\n"
            });
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", "line one\r\nline two\r\nline three\r\n");
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: ["docs/release-management/audit-fixture.md"],
            archiveOnlyEvidenceGlobs: []);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var patch = ReadZipEntryText(fixture.ZipPath(taskId), $"{taskId}.patch");
        Assert.DoesNotContain('\r', patch);
        Assert.Contains("+line three", patch, StringComparison.Ordinal);
        Assert.DoesNotContain("-line one", patch, StringComparison.Ordinal);
        Assert.DoesNotContain("-line two", patch, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuditPackageRejectsWindowsDrivePathsInArchiveContent(bool useBackslash)
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-windows-drive-path");
        const string taskId = "T-0001";
        var separator = useBackslash ? "\\" : "/";
        var localPath = string.Concat("G:", separator, "local", separator, "copied-task.md");
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", $"""
        # Audit fixture

        Local task copy: {localPath}
        """);
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-ABSOLUTE-PATH");
    }

    [Theory]
    [InlineData("docs\\bad.md")]
    [InlineData("docs/bad\u0001.md")]
    public async Task AuditPackageRejectsBackslashAndControlCharactersInConfiguredPaths(string badPath)
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-bad-path");
        const string taskId = "T-0001";
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: [badPath],
            archiveOnlyEvidenceGlobs: []);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-PATH-INVALID");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsBrokenPatch()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-broken-patch", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        ReplaceZipEntryText(
            zipPath,
            "T-0001.patch",
            patch => patch.Replace("@@ -0,0", "@@ -broken", StringComparison.Ordinal),
            updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-broken-patch");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-PATCH-FAILED");
    }

    [Fact]
    public async Task AuditPackageRejectsOneLineNewTextFileStubDuringRestoreRehearsal()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-one-line-stub");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/one-line-stub.md", "stub\n");
        var configPath = fixture.WriteConfig(taskId);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.NotEqual(0, package.ExitCode);
        AssertDiagnosticCode(package, "E2D-BUILD-AUDIT-ONE-LINE-STUB");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsIncompletePatchAgainstRestoreManifest()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-incomplete-patch", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        ReplaceZipEntryText(zipPath, "T-0001.patch", _ => string.Empty, updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-incomplete-patch");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-PATCH-FAILED");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsPatchThatChangesPathOutsideRestoreManifest()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-extra-restore-path");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file is the only repo-owned path declared in the restore manifest.
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            repoFileGlobs: [],
            repoFileAllowlist: ["docs/release-management/audit-fixture.md"]);
        var package = await RunAuditPackageAsync(fixture, taskId, configPath);
        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        fixture.WriteTextFile("docs/release-management/extra-added.md", """
        # Extra

        tampered file outside restore manifest
        """);
        var extraPatch = await fixture.CreateDiffForCurrentPathAsync("docs/release-management/extra-added.md");
        ReplaceZipEntryText(
            zipPath,
            $"{taskId}.patch",
            patch => patch.EndsWith('\n') ? patch + extraPatch : patch + "\n" + extraPatch,
            updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-extra-restore-path");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-RESTORE-MISMATCH");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsMissingEvidenceLinkInManifest()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-missing-evidence-link", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        ReplaceZipEntryText(
            zipPath,
            "AUDIT-MANIFEST.md",
            manifest => manifest.Replace("- `evidence/T-0001-r01/checks/git-status/stdout.txt`", "- evidence link removed", StringComparison.Ordinal),
            updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-missing-evidence-link");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-MANIFEST-INCOMPLETE");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsStaleChecksum()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-stale-checksum", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        ReplaceZipEntryText(
            zipPath,
            "AUDIT-REQUEST.md",
            request => request + "\nchecksum drift\n",
            updateChecksums: false);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-stale-checksum");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-CHECKSUM-FAILED");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsForbiddenArchiveEntry()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-forbidden-entry", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        AddZipEntry(zipPath, ".git/config", "[core]\nrepositoryformatversion = 0\n", updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-forbidden-entry");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-FORBIDDEN-PATH");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsZipEntryWithPlatformExternalAttributes()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-external-attributes", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        RewriteZip(zipPath, _ => { }, externalAttributes: 32);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-external-attributes");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC");
    }

    [Fact]
    public async Task AuditPackageRejectsExistingTargetZipWithoutForce()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-existing-zip", "T-0001");
        var configPath = fixture.ConfigPath("T-0001");

        var secondRun = await RunAuditPackageAsync(fixture, "T-0001", configPath);

        Assert.NotEqual(0, secondRun.ExitCode);
        AssertDiagnosticCode(secondRun, "E2D-BUILD-AUDIT-ZIP-EXISTS");
    }

    [Theory]
    [InlineData("T-0001")]
    [InlineData("T-9999")]
    public async Task AuditPackageUsesConfiguredSyntheticTaskIdEverywhere(string taskId)
    {
        using var fixture = await CreatePackagedFixtureAsync($"audit-package-{taskId.ToLowerInvariant()}", taskId);
        var zipPath = fixture.ZipPath(taskId);
        var generatedText = string.Join(
            "\n",
            ReadZipEntryText(zipPath, "AUDIT-MANIFEST.md"),
            ReadZipEntryText(zipPath, "AUDIT-REQUEST.md"),
            ReadZipEntryText(zipPath, "repo-file-hashes.json"));
        var generatedPaths = string.Join("\n", ReadZipEntryNames(zipPath));

        Assert.Equal($"{taskId}-audit-r01.zip", Path.GetFileName(zipPath));
        Assert.Contains($"{taskId}.patch", generatedPaths, StringComparison.Ordinal);
        Assert.Contains($"evidence/{taskId}-r01/", generatedPaths, StringComparison.Ordinal);
        Assert.All(TaskIdPattern.Matches(generatedText + "\n" + generatedPaths).Cast<Match>(), match => Assert.Equal(taskId, match.Value));
    }

    [Fact]
    public async Task AuditPackageAllowsHistoricalTaskIdsInConfiguredVerdictAndBlockerLists()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-historical-task-ids");
        const string taskId = "T-0001";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves historical task references stay allowed in configured prose.
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            previousVerdictChain: ["docs/verdicts/release-management/t-0207-audit-r04.md"],
            blockerClosureList: ["Закрыт blocker из T-0207: restore verification now checks exact file sets."]);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var request = ReadZipEntryText(zipPath, "AUDIT-REQUEST.md");
        Assert.Contains("t-0207-audit-r04.md", request, StringComparison.Ordinal);
        Assert.Contains("T-0207", request, StringComparison.Ordinal);

        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-historical-task-ids");
        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.Equal(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-PACKAGE-VERIFIED");
    }

    [Fact]
    public async Task AuditPackageIncludesExistingPreviousVerdictsInRestoreModel()
    {
        using var fixture = await AuditFixture.CreateAsync("audit-package-previous-verdict-restore");
        const string taskId = "T-0001";
        const string previousVerdictPath = "docs/verdicts/release-management/t-0001-audit-r00.md";
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves ordinary task documentation still restores.
        """);
        var historicalExternalEvidencePath = string.Concat(
            "Z",
            ":",
            "/",
            "synthetic-audit-host",
            "/",
            "synthetic-reviewer",
            "/",
            "electron2d",
            "/",
            "T-0001-r00",
            "/",
            "notes.md");
        fixture.WriteTextFile(previousVerdictPath, $"""
        # Аудит T-0001 r00

        VERDICT: NEEDS_FIXES

        Исторический внешний ответ процитировал локальный путь аудитора:
        {historicalExternalEvidencePath}
        """);
        var configPath = fixture.WriteConfig(
            taskId,
            previousVerdictChain: [previousVerdictPath]);

        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        var zipPath = fixture.ZipPath(taskId);
        var patch = ReadZipEntryText(zipPath, $"{taskId}.patch");
        var manifest = ReadZipEntryText(zipPath, "AUDIT-MANIFEST.md");
        using var restoreManifest = JsonDocument.Parse(ReadZipEntryText(zipPath, "repo-file-hashes.json"));

        Assert.Contains(previousVerdictPath, patch, StringComparison.Ordinal);
        Assert.Contains(historicalExternalEvidencePath, patch, StringComparison.Ordinal);
        Assert.Contains(previousVerdictPath, manifest, StringComparison.Ordinal);
        Assert.Contains(
            restoreManifest.RootElement.GetProperty("repoFiles").EnumerateArray(),
            file => string.Equals(file.GetProperty("path").GetString(), previousVerdictPath, StringComparison.Ordinal));

        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-previous-verdict-restore");
        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.Equal(0, verify.ExitCode);
        Assert.True(File.Exists(Path.Combine(cleanRepo.Root, previousVerdictPath.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsMismatchedTaskIdInGeneratedMetadata()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-task-mismatch", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        ReplaceZipEntryText(
            zipPath,
            "metadata/audit-package.input.json",
            metadata => metadata.Replace("\"taskId\": \"T-0001\"", "\"taskId\": \"T-9999\"", StringComparison.Ordinal),
            updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-task-mismatch");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-TASK-ID-MISMATCH");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsMismatchedTaskIdInArchivePath()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-task-path-mismatch", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        AddZipEntry(zipPath, "evidence/T-9999-r01/path-mismatch.txt", "wrong task path\n", updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-task-path-mismatch");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-TASK-ID-MISMATCH");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsWindowsDrivePathsInArchiveContent()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-verify-drive-path", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        AddZipEntry(
            zipPath,
            "evidence/T-0001-r01/archive-only/local-path.txt",
            string.Concat("Local task copy: ", "G:", "/", "local", "/", "copied-task.md\n"),
            updateChecksums: true);
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-drive-path");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-ABSOLUTE-PATH");
    }

    [Fact]
    public async Task AuditPackageVerifyRejectsNondeterministicZipTimestamp()
    {
        using var fixture = await CreatePackagedFixtureAsync("audit-package-nondeterministic-timestamp", "T-0001");
        var zipPath = fixture.ZipPath("T-0001");
        RewriteZip(zipPath, _ => { }, entryTimestamp: new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero));
        using var cleanRepo = await fixture.CreateCleanCloneAsync("verify-nondeterministic-timestamp");

        var verify = await RunAuditVerifyAsync(fixture, zipPath, cleanRepo.Root);

        Assert.NotEqual(0, verify.ExitCode);
        AssertDiagnosticCode(verify, "E2D-BUILD-AUDIT-ZIP-NONDETERMINISTIC");
    }

    private static async Task<object> RunProcessRunnerAsync(
        string stepName,
        string fileName,
        string[] arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var assembly = await BuildAndLoadBuildToolAssemblyAsync();
        return await RunProcessRunnerAsync(assembly, stepName, fileName, arguments, workingDirectory, timeout, cancellationToken);
    }

    private static async Task<object> RunProcessRunnerAsync(
        Assembly assembly,
        string stepName,
        string fileName,
        string[] arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var requestType = assembly.GetType("Electron2D.Build.ProcessRunRequest", throwOnError: true)!;
        var runnerType = assembly.GetType("Electron2D.Build.ProcessRunner", throwOnError: true)!;
        var request = Activator.CreateInstance(
            requestType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [stepName, fileName, arguments, workingDirectory, timeout],
            culture: null)!;
        var runner = Activator.CreateInstance(
            runnerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [],
            culture: null)!;
        var method = runnerType.GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(runnerType.FullName, "RunAsync");
        var task = (Task)method.Invoke(runner, [request, cancellationToken])!;

        await task.ConfigureAwait(false);

        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static async Task<Assembly> BuildAndLoadBuildToolAssemblyAsync()
    {
        var root = FindRepositoryRoot();
        var projectPath = BuildToolProjectPath(root);

        Assert.True(File.Exists(projectPath), $"Build tool project was not found: {projectPath}");

        var build = await RunProcessAsync(
            DotnetExecutable,
            ["build", projectPath],
            root,
            TimeSpan.FromSeconds(60));

        Assert.True(
            build.ExitCode == 0,
            $"Build tool project failed to build with exit code {build.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{build.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{build.Stderr}");

        var assemblyPath = Path.Combine(root, "eng", "Electron2D.Build", "bin", "Debug", "net10.0", "Electron2D.Build.dll");
        Assert.True(File.Exists(assemblyPath), $"Build tool assembly was not found: {assemblyPath}");

        return Assembly.LoadFrom(assemblyPath);
    }

    private static async Task<BuiltChildProject> CreateBuiltChildProjectAsync(string name, string program)
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-RepositoryBuildToolTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectRoot);

        File.WriteAllText(
            Path.Combine(projectRoot, "Child.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(projectRoot, "Program.cs"), program);

        var build = await RunProcessAsync(
            DotnetExecutable,
            ["build", Path.Combine(projectRoot, "Child.csproj")],
            projectRoot,
            TimeSpan.FromSeconds(60));

        Assert.True(
            build.ExitCode == 0,
            $"Child project failed to build with exit code {build.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{build.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{build.Stderr}");

        var assemblyPath = Path.Combine(projectRoot, "bin", "Debug", "net10.0", "Child.dll");
        Assert.True(File.Exists(assemblyPath), $"Child assembly was not found: {assemblyPath}");

        return new BuiltChildProject(projectRoot, assemblyPath);
    }

    private static async Task<CommandResult> RunBuildToolAsync(params string[] arguments)
    {
        return await RunBuildToolFromDirectoryAsync(FindRepositoryRoot(), arguments);
    }

    private static async Task<CommandResult> RunBuildToolFromDirectoryAsync(string workingDirectory, params string[] arguments)
    {
        var root = FindRepositoryRoot();
        return await RunProcessAsync(
            DotnetExecutable,
            ["run", "--project", BuildToolProjectPath(root), "--", .. arguments],
            workingDirectory,
            TimeSpan.FromSeconds(120));
    }

    private static async Task<CommandResult> RunProcessAsync(
        string fileName,
        string[] arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            throw new TimeoutException($"Process did not exit within {timeout}: {fileName} {string.Join(" ", arguments)}");
        }

        return new CommandResult(process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
    }

    private static JsonDocument ReadFirstDiagnostic(CommandResult result)
    {
        var line = result.Stdout
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        Assert.False(
            string.IsNullOrWhiteSpace(line),
            $"Expected a structured diagnostic on stdout.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");

        return JsonDocument.Parse(line);
    }

    private static void AssertDiagnosticCode(CommandResult result, string expectedCode)
    {
        var diagnostics = ReadDiagnosticCodes(result);

        Assert.True(
            diagnostics.Contains(expectedCode, StringComparer.Ordinal),
            $"Expected diagnostic code '{expectedCode}'. Actual codes: {string.Join(", ", diagnostics)}.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");
    }

    private static List<string> ReadDiagnosticCodes(CommandResult result)
    {
        var codes = new List<string>();
        foreach (var line in result.Stdout.Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            using var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("code", out var code))
            {
                codes.Add(code.GetString() ?? string.Empty);
            }
        }

        Assert.NotEmpty(codes);
        return codes;
    }

    private static List<object> GetDiagnostics(object result)
    {
        var diagnostics = Assert.IsAssignableFrom<IEnumerable>(GetRawProperty(result, "Diagnostics"));
        return diagnostics.Cast<object>().ToList();
    }

    private static T? GetProperty<T>(object instance, string name)
    {
        return (T?)GetRawProperty(instance, name);
    }

    private static object? GetRawProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(instance.GetType().FullName, name);
        return property.GetValue(instance);
    }

    private static string BuildToolProjectPath(string root)
    {
        return Path.Combine(root, "eng", "Electron2D.Build", "Electron2D.Build.csproj");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private static void TryKillProcessTree(Process process)
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

    private static void AssertNoReleaseArtifacts(string root)
    {
        var forbiddenFiles = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(IsReleaseArtifactFile)
            .ToArray();
        var forbiddenDirectories = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .Where(IsReleaseArtifactDirectory)
            .ToArray();

        Assert.Empty(forbiddenFiles);
        Assert.Empty(forbiddenDirectories);
    }

    private static bool IsReleaseArtifactFile(string path)
    {
        var name = Path.GetFileName(path);
        return name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "release-manifest.json", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".sha256sum", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("checksums", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReleaseArtifactDirectory(string path)
    {
        var name = Path.GetFileName(path);
        return string.Equals(name, "artifacts", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "release", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "releases", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "release-output", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CommandResult> RunAuditPackageAsync(AuditFixture fixture, string taskId, string? configPath, bool force = false)
    {
        var arguments = new List<string>
        {
            "audit",
            "package",
            "--task",
            taskId,
            "--iteration",
            "r01",
            "--baseline",
            fixture.Baseline,
            "--out",
            ".temp/audit"
        };

        if (configPath is not null)
        {
            arguments.Add("--config");
            arguments.Add(configPath);
        }

        if (force)
        {
            arguments.Add("--force");
        }

        return await RunBuildToolFromDirectoryAsync(fixture.RepositoryRoot, [.. arguments]);
    }

    private static async Task<CommandResult> RunAuditVerifyAsync(AuditFixture fixture, string zipPath, string cleanRepoRoot)
    {
        return await RunBuildToolFromDirectoryAsync(
            fixture.RepositoryRoot,
            "audit",
            "package",
            "verify",
            "--zip",
            zipPath,
            "--baseline",
            fixture.Baseline,
            "--repo",
            cleanRepoRoot);
    }

    private static async Task<AuditFixture> CreatePackagedFixtureAsync(string name, string taskId)
    {
        var fixture = await AuditFixture.CreateAsync(name);
        fixture.WriteTextFile("docs/release-management/audit-fixture.md", """
        # Audit fixture

        This file proves that package verification can restore changed files.
        """);
        fixture.WriteTextFile("audit-evidence/checks/result.txt", """
        fixture evidence
        second line
        """);
        var configPath = fixture.WriteConfig(taskId);
        var package = await RunAuditPackageAsync(fixture, taskId, configPath);

        Assert.Equal(0, package.ExitCode);
        return fixture;
    }

    private static IReadOnlyList<string> ReadZipEntryNames(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        return archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .Select(entry => entry.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string ReadZipEntryText(string zipPath, string entryName)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"ZIP entry was not found: {entryName}");
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string Sha256ZipEntry(string zipPath, string entryName)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"ZIP entry was not found: {entryName}");
        using var stream = entry.Open();
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static int ParseEvidenceValue(string text, string key)
    {
        var prefix = $"{key}: ";
        var line = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Single(line => line.StartsWith(prefix, StringComparison.Ordinal));
        return int.Parse(line[prefix.Length..], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<ZipCentralDirectoryEntry> ReadZipCentralDirectory(string zipPath)
    {
        var bytes = File.ReadAllBytes(zipPath);
        var eocdOffset = FindEndOfCentralDirectory(bytes);
        var entryCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(eocdOffset + 10, 2));
        var centralDirectoryOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(eocdOffset + 16, 4));
        var entries = new List<ZipCentralDirectoryEntry>(entryCount);
        var offset = centralDirectoryOffset;

        for (var i = 0; i < entryCount; i++)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4)) != 0x02014b50)
            {
                throw new InvalidOperationException("ZIP central directory entry signature was not found.");
            }

            var flags = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 8, 2));
            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 28, 2));
            var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 30, 2));
            var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 32, 2));
            var externalAttributes = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 38, 4));
            var name = Encoding.UTF8.GetString(bytes.AsSpan(offset + 46, nameLength));
            entries.Add(new ZipCentralDirectoryEntry(name, (flags & 0x0800) != 0, externalAttributes));
            offset += 46 + nameLength + extraLength + commentLength;
        }

        return entries;
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

        throw new InvalidOperationException("ZIP end of central directory record was not found.");
    }

    private static void ReplaceZipEntryText(string zipPath, string entryName, Func<string, string> replace, bool updateChecksums)
    {
        RewriteZip(
            zipPath,
            entries =>
            {
                entries[entryName] = Encoding.UTF8.GetBytes(replace(Encoding.UTF8.GetString(entries[entryName])));
                if (updateChecksums)
                {
                    entries["SHA256SUMS.txt"] = CreateChecksumFile(entries);
                }
            });
    }

    private static void AddZipEntry(string zipPath, string entryName, string text, bool updateChecksums)
    {
        RewriteZip(
            zipPath,
            entries =>
            {
                entries[entryName] = Encoding.UTF8.GetBytes(text);
                if (updateChecksums)
                {
                    entries["SHA256SUMS.txt"] = CreateChecksumFile(entries);
                }
            });
    }

    private static void RewriteZip(
        string zipPath,
        Action<Dictionary<string, byte[]>> mutate,
        DateTimeOffset? entryTimestamp = null,
        int externalAttributes = 0)
    {
        var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using (var archive = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)))
            {
                using var stream = entry.Open();
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                entries[entry.FullName] = memory.ToArray();
            }
        }

        mutate(entries);

        File.Delete(zipPath);
        using var output = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var rewritten = new ZipArchive(output, ZipArchiveMode.Create);
        foreach (var pair in entries.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var entry = rewritten.CreateEntry(pair.Key, CompressionLevel.SmallestSize);
            entry.LastWriteTime = entryTimestamp ?? DeterministicZipTimestamp;
            entry.ExternalAttributes = externalAttributes;
            using var stream = entry.Open();
            stream.Write(pair.Value);
        }
    }

    private static byte[] CreateChecksumFile(Dictionary<string, byte[]> entries)
    {
        var builder = new StringBuilder();
        foreach (var pair in entries.Where(pair => pair.Key != "SHA256SUMS.txt").OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            builder.Append(Convert.ToHexString(SHA256.HashData(pair.Value)).ToLowerInvariant());
            builder.Append("  ");
            builder.Append(pair.Key);
            builder.Append('\n');
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void AssertPosixArchivePath(string path)
    {
        Assert.DoesNotContain("\\", path, StringComparison.Ordinal);
        Assert.False(Path.IsPathRooted(path), $"Archive path must be relative: {path}");
        Assert.DoesNotContain("//", path, StringComparison.Ordinal);
        Assert.DoesNotContain("/../", $"/{path}/", StringComparison.Ordinal);
        Assert.DoesNotContain(path, ch => char.IsControl(ch));
    }

    private static void AssertPatchControlLinesUsePosixPaths(string patch)
    {
        foreach (var line in patch.Split('\n'))
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal) ||
                line.StartsWith("--- ", StringComparison.Ordinal) ||
                line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("\\", line, StringComparison.Ordinal);
            }
        }
    }

    private const string DotnetExecutable = "dotnet";
    private static readonly DateTimeOffset DeterministicZipTimestamp = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Regex TaskIdPattern = new(@"\bT-\d{4}\b", RegexOptions.CultureInvariant);

    private sealed record CommandResult(int ExitCode, string Stdout, string Stderr);

    private sealed record ZipCentralDirectoryEntry(string Name, bool HasUtf8Flag, int ExternalAttributes);

    private sealed record AuditFixtureCheck(
        string Name,
        string FileName,
        string[] Arguments,
        string Cwd,
        string[] EnvAllowlist,
        int TimeoutSeconds,
        int ExpectedExitCode,
        string[] TrxGlobs);

    private sealed record BuiltChildProject(string Root, string AssemblyPath) : IDisposable
    {
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

    private sealed record TemporaryDirectory(string Root) : IDisposable
    {
        public static TemporaryDirectory Create(string name)
        {
            var root = Path.Combine(Path.GetTempPath(), "Electron2D-RepositoryBuildToolTests", name, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TemporaryDirectory(root);
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

    private sealed record AuditFixture(string Root, string RepositoryRoot, string Baseline) : IDisposable
    {
        public static async Task<AuditFixture> CreateAsync(string name, IReadOnlyDictionary<string, string>? baselineFiles = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "Electron2D-RepositoryBuildToolTests", name, Guid.NewGuid().ToString("N"));
            var repositoryRoot = Path.Combine(root, "repo");
            Directory.CreateDirectory(repositoryRoot);
            await RunGitAsync(repositoryRoot, "init");
            await RunGitAsync(repositoryRoot, "config", "user.email", "fixture@example.invalid");
            await RunGitAsync(repositoryRoot, "config", "user.name", "Fixture User");
            WriteFile(repositoryRoot, "README.md", """
            # Fixture repository

            Baseline content.
            """);
            foreach (var file in baselineFiles ?? new Dictionary<string, string>(StringComparer.Ordinal))
            {
                WriteFile(repositoryRoot, file.Key, file.Value);
            }

            await RunGitAsync(repositoryRoot, "add", ".");
            await RunGitAsync(repositoryRoot, "commit", "-m", "initial fixture baseline");
            var baseline = await RunGitCaptureAsync(repositoryRoot, "rev-parse", "HEAD");

            return new AuditFixture(root, repositoryRoot, baseline.Trim());
        }

        public string ConfigPath(string taskId)
        {
            return Path.Combine(RepositoryRoot, ".temp", "audit", taskId, "audit-package.input.json");
        }

        public string ZipPath(string taskId)
        {
            return Path.Combine(RepositoryRoot, ".temp", "audit", $"{taskId}-audit-r01.zip");
        }

        public void WriteTextFile(string relativePath, string content)
        {
            WriteFile(RepositoryRoot, relativePath, content);
        }

        public string WriteConfig(
            string taskId,
            string[]? repoFileGlobs = null,
            string[]? repoFileAllowlist = null,
            string[]? archiveOnlyEvidenceGlobs = null,
            AuditFixtureCheck[]? checks = null,
            string[]? previousVerdictChain = null,
            string[]? blockerClosureList = null)
        {
            var config = new
            {
                taskId,
                iteration = "r01",
                baseline = Baseline,
                branch = "codex/audit-package-fixture",
                domain = "release-management",
                repoFileGlobs = repoFileGlobs ?? ["docs/release-management/*.md"],
                repoFileAllowlist = repoFileAllowlist ?? Array.Empty<string>(),
                archiveOnlyEvidenceGlobs = archiveOnlyEvidenceGlobs ?? ["audit-evidence/**"],
                checks = checks ??
                [
                    new AuditFixtureCheck(
                        "git-status",
                        "git",
                        ["status", "--short"],
                        ".",
                        [],
                        10,
                        0,
                        [])
                ],
                forbiddenPatterns = Array.Empty<string>(),
                secretScanPolicy = "basic",
                maxFileSize = 1_048_576,
                outputDirectory = ".temp/audit",
                previousVerdictChain = previousVerdictChain ?? Array.Empty<string>(),
                blockerClosureList = blockerClosureList ?? Array.Empty<string>(),
                oneLineStubAllowlist = Array.Empty<string>()
            };

            var path = ConfigPath(taskId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(
                    config,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    }) + Environment.NewLine,
                Encoding.UTF8);
            return path;
        }

        public async Task<string> CreateDiffForCurrentPathAsync(string relativePath)
        {
            var result = await CreateDiffAsync(relativePath);
            Assert.True(
                result.ExitCode == 0,
                $"git diff for {relativePath} failed with exit code {result.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");
            if (string.IsNullOrWhiteSpace(result.Stdout) &&
                File.Exists(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))))
            {
                await RunGitAsync(RepositoryRoot, "add", "--intent-to-add", "--", relativePath);
                result = await CreateDiffAsync(relativePath);
            }

            Assert.False(string.IsNullOrWhiteSpace(result.Stdout), $"git diff for {relativePath} was empty.");
            return result.Stdout;
        }

        private async Task<CommandResult> CreateDiffAsync(string relativePath)
        {
            return await RunProcessAsync(
                "git",
                StableGitArguments(["diff", "--binary", "--full-index", "--no-ext-diff", Baseline, "--", relativePath]),
                RepositoryRoot,
                TimeSpan.FromSeconds(30));
        }

        public async Task<TemporaryDirectory> CreateCleanCloneAsync(string name)
        {
            var cloneRoot = Path.Combine(Root, name);
            await RunGitAsync(Root, "clone", RepositoryRoot, cloneRoot);
            await RunGitAsync(cloneRoot, "checkout", Baseline);
            await RunGitAsync(cloneRoot, "clean", "-fdx");
            return new TemporaryDirectory(cloneRoot);
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

        private static void WriteFile(string repositoryRoot, string relativePath, string content)
        {
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        private static async Task RunGitAsync(string workingDirectory, params string[] arguments)
        {
            var result = await RunProcessAsync("git", StableGitArguments(arguments), workingDirectory, TimeSpan.FromSeconds(30));
            Assert.True(
                result.ExitCode == 0,
                $"git {string.Join(" ", arguments)} failed with exit code {result.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");
        }

        private static async Task<string> RunGitCaptureAsync(string workingDirectory, params string[] arguments)
        {
            var result = await RunProcessAsync("git", StableGitArguments(arguments), workingDirectory, TimeSpan.FromSeconds(30));
            Assert.True(
                result.ExitCode == 0,
                $"git {string.Join(" ", arguments)} failed with exit code {result.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{result.Stdout}{Environment.NewLine}stderr:{Environment.NewLine}{result.Stderr}");
            return result.Stdout;
        }

        private static string[] StableGitArguments(string[] arguments)
        {
            return ["-c", "core.quotepath=false", "-c", "core.autocrlf=false", "-c", "core.safecrlf=false", .. arguments];
        }
    }
}
