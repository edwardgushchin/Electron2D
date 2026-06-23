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
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class LeakVerificationTests
{
    private const int Iterations = 64;
    private const long MaxManagedGrowthBytes = 1_048_576;

    private static readonly string[] ScenarioIds =
    [
        "gpu-texture-render-target-cycles",
        "audio-voice-cycles",
        "physics-rid-cycles",
        "scene-load-unload-cycles"
    ];

    [Fact]
    public void LeakSpecificationDefinesVerifierArtifactAndBudgets()
    {
        var root = FindRepositoryRoot();
        var specPath = Path.Combine(root, "docs", "specifications", "quality", "leak-verification.md");

        Assert.True(File.Exists(specPath), $"Missing leak verification specification: {specPath}");

        var spec = File.ReadAllText(specPath);
        Assert.Contains("tools\\Verify-LeakChecks.ps1", spec, StringComparison.Ordinal);
        Assert.Contains("data/quality/leak-verification-report.json", spec, StringComparison.Ordinal);
        Assert.Contains("LeakVerificationTests", spec, StringComparison.Ordinal);
        Assert.Contains("nativeHandleDelta", spec, StringComparison.Ordinal);
        Assert.Contains("monotonicGrowthDetected", spec, StringComparison.Ordinal);
        Assert.Contains("1048576", spec, StringComparison.Ordinal);

        foreach (var scenarioId in ScenarioIds)
        {
            Assert.Contains(scenarioId, spec, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LeakVerifierDeclaresFocusedCycleTestAndReportChecks()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-LeakChecks.ps1");

        Assert.True(File.Exists(verifierPath), $"Missing leak verification verifier: {verifierPath}");

        var verifier = File.ReadAllText(verifierPath);
        Assert.Contains("LeakVerificationTests.LeakVerificationCyclesReleaseSubsystemResourcesAndDoNotGrowMonotonically", verifier, StringComparison.Ordinal);
        Assert.Contains("leak-verification-report.json", verifier, StringComparison.Ordinal);
        Assert.Contains("nativeHandleDelta", verifier, StringComparison.Ordinal);
        Assert.Contains("activeResourceCount", verifier, StringComparison.Ordinal);
        Assert.Contains("monotonicGrowthDetected", verifier, StringComparison.Ordinal);
    }

    [Fact]
    public void LeakVerificationReportCoversSubsystemsBudgetsAndEvidence()
    {
        var root = FindRepositoryRoot();
        var reportPath = Path.Combine(root, "data", "quality", "leak-verification-report.json");

        Assert.True(File.Exists(reportPath), $"Missing leak verification report: {reportPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        var report = document.RootElement;

        Assert.Equal("Electron2D.LeakVerificationReport", report.GetProperty("format").GetString());
        Assert.Equal(1, report.GetProperty("version").GetInt32());
        Assert.Equal("0.1.0-preview", report.GetProperty("release").GetString());

        var budgets = report.GetProperty("budgets");
        Assert.True(budgets.GetProperty("minimumIterations").GetInt32() >= Iterations);
        Assert.Equal(MaxManagedGrowthBytes, budgets.GetProperty("maxManagedGrowthBytes").GetInt64());
        Assert.Equal(0, budgets.GetProperty("maxNativeHandleDelta").GetInt32());
        Assert.Equal(0, budgets.GetProperty("maxActiveResourceCount").GetInt32());

        var scenarios = report.GetProperty("scenarios").EnumerateArray().ToDictionary(
            scenario => scenario.GetProperty("scenarioId").GetString()!,
            StringComparer.Ordinal);
        Assert.Equal(ScenarioIds.Order(StringComparer.Ordinal), scenarios.Keys.Order(StringComparer.Ordinal));

        foreach (var scenarioId in ScenarioIds)
        {
            var scenario = scenarios[scenarioId];
            Assert.False(string.IsNullOrWhiteSpace(scenario.GetProperty("subsystem").GetString()));
            Assert.True(scenario.GetProperty("iterations").GetInt32() >= Iterations);
            Assert.InRange(scenario.GetProperty("managedGrowthBytes").GetInt64(), 0, MaxManagedGrowthBytes);
            Assert.Equal(0, scenario.GetProperty("nativeHandleDelta").GetInt32());
            Assert.Equal(0, scenario.GetProperty("activeResourceCount").GetInt32());
            Assert.False(scenario.GetProperty("monotonicGrowthDetected").GetBoolean());
            Assert.NotEmpty(scenario.GetProperty("evidence").EnumerateArray());
        }

        Assert.Contains(
            scenarios["gpu-texture-render-target-cycles"].GetProperty("evidence").EnumerateArray(),
            evidence => evidence.GetString() == "tests/Electron2D.Tests.Integration/TextureResourceRegistryTests.cs");
        Assert.Contains(
            scenarios["audio-voice-cycles"].GetProperty("evidence").EnumerateArray(),
            evidence => evidence.GetString() == "tests/Electron2D.Tests.Integration/AudioServerVoiceTests.cs");
        Assert.Contains(
            scenarios["physics-rid-cycles"].GetProperty("evidence").EnumerateArray(),
            evidence => evidence.GetString() == "tests/Electron2D.Tests.Integration/PhysicsServer2DTests.cs");
        Assert.Contains(
            scenarios["scene-load-unload-cycles"].GetProperty("evidence").EnumerateArray(),
            evidence => evidence.GetString() == "tests/Electron2D.Tests.Integration/PackedSceneTests.cs");
    }

    [Fact]
    public void LeakVerificationCyclesReleaseSubsystemResourcesAndDoNotGrowMonotonically()
    {
        var gpuGrowth = MeasureManagedGrowth(RunGpuCycles);
        var audioGrowth = MeasureManagedGrowth(RunAudioCycles);
        var physicsGrowth = MeasureManagedGrowth(RunPhysicsCycles);
        var sceneGrowth = MeasureManagedGrowth(RunSceneCycles);

        Assert.InRange(gpuGrowth, 0, MaxManagedGrowthBytes);
        Assert.InRange(audioGrowth, 0, MaxManagedGrowthBytes);
        Assert.InRange(physicsGrowth, 0, MaxManagedGrowthBytes);
        Assert.InRange(sceneGrowth, 0, MaxManagedGrowthBytes);
    }

    [Fact]
    public async Task LeakVerifierPasses()
    {
        var root = FindRepositoryRoot();
        var verifierPath = Path.Combine(root, "tools", "Verify-LeakChecks.ps1");

        Assert.True(File.Exists(verifierPath), $"Missing leak verification verifier: {verifierPath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(verifierPath);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start leak verifier.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Leak verifier failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");
        Assert.Contains("Leak verification passed", output, StringComparison.Ordinal);
    }

    private static long MeasureManagedGrowth(Action action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);

        action();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var after = GC.GetTotalMemory(forceFullCollection: true);
        return Math.Max(0, after - before);
    }

    private static void RunGpuCycles()
    {
        var api = new FakeTextureGpuApi();
        var registry = new Electron2D.TextureResourceRegistry(api);

        for (var index = 0; index < Iterations; index++)
        {
            var texture = registry.Upload(
                new Electron2D.RuntimeTexture2D(32, 32, hasAlpha: true),
                Electron2D.TextureSamplingOptions.Default);
            var renderTarget = registry.CreateRenderTarget(
                new Electron2D.Vector2I(64, 64),
                hasAlpha: true,
                Electron2D.TextureSamplingOptions.Default);

            registry.Reload(texture, new Electron2D.RuntimeTexture2D(32, 32, hasAlpha: false));

            Assert.True(registry.Release(texture));
            Assert.True(registry.Release(renderTarget));
            Assert.Equal(0, registry.ActiveTextureCount);
            Assert.Equal(0, registry.LeakCount);
        }

        Assert.Equal(Iterations * 2, api.ReleaseCalls);
    }

    private static void RunAudioCycles()
    {
        var backend = new Electron2D.ManagedAudioServerBackend();
        var playback = new Electron2D.AudioVoicePlayback(VolumeDb: 0f, PitchScale: 1f, Loop: false);

        for (var index = 0; index < Iterations; index++)
        {
            var voice = backend.Play(new TestAudioStream(length: 0.25f), playback);
            Assert.True(backend.IsPlaying(voice));

            backend.Stop(voice);
            Assert.False(backend.IsPlaying(voice));

            backend.Release(voice);
            Assert.Equal(0, backend.ActiveVoiceCount);
        }
    }

    private static void RunPhysicsCycles()
    {
        var backend = new Electron2D.ManagedPhysicsServer2DBackend();

        for (var index = 0; index < Iterations; index++)
        {
            var space = backend.SpaceCreate();
            var area = backend.AreaCreate();
            var body = backend.BodyCreate(Electron2D.PhysicsBodyKind.Static);
            var joint = backend.JointCreate();
            var shape = backend.ShapeCreate(Electron2D.PhysicsServer2D.ShapeType.Circle);

            Assert.Equal(2, backend.GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects));

            backend.FreeRid(shape);
            backend.FreeRid(joint);
            backend.FreeRid(body);
            backend.FreeRid(area);
            backend.FreeRid(space);
            Assert.Equal(0, backend.GetProcessInfo(Electron2D.PhysicsServer2D.ProcessInfo.ActiveObjects));
        }
    }

    private static void RunSceneCycles()
    {
        var tree = new Electron2D.SceneTree();
        var first = PackScene("LeakCycleA");
        var second = PackScene("LeakCycleB");
        Electron2D.Node? previous = null;

        for (var index = 0; index < Iterations; index++)
        {
            var scene = index % 2 == 0 ? first : second;
            Assert.Equal(Electron2D.Error.Ok, tree.ChangeSceneToPacked(scene));
            tree.ProcessFrame(0d);

            if (previous is not null)
            {
                Assert.False(Electron2D.Object.IsInstanceValid(previous));
            }

            var current = Assert.IsType<Electron2D.Node>(tree.CurrentScene);
            Assert.True(Electron2D.Object.IsInstanceValid(current));
            Assert.Equal(1, tree.Root.GetChildCount());
            previous = current;
        }

        var finalScene = Assert.IsType<Electron2D.Node>(tree.CurrentScene);
        tree.Root.RemoveChild(finalScene);
        finalScene.Free();
        Assert.Equal(0, tree.Root.GetChildCount());
        Assert.False(Electron2D.Object.IsInstanceValid(finalScene));
    }

    private static Electron2D.PackedScene PackScene(string name)
    {
        var root = new Electron2D.Node { Name = name };
        var child = new Electron2D.Node { Name = name + "Child" };
        root.AddChild(child);
        child.Owner = root;

        var scene = new Electron2D.PackedScene();
        Assert.Equal(Electron2D.Error.Ok, scene.Pack(root));
        return scene;
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

    private sealed class FakeTextureGpuApi : Electron2D.ITextureGpuApi
    {
        public int ReleaseCalls { get; private set; }

        public bool Upload(Electron2D.Rid texture, Electron2D.TextureUploadDescriptor descriptor, out string? error)
        {
            _ = texture;
            _ = descriptor;
            error = null;
            return true;
        }

        public bool Reload(Electron2D.Rid texture, Electron2D.TextureUploadDescriptor descriptor, out string? error)
        {
            _ = texture;
            _ = descriptor;
            error = null;
            return true;
        }

        public bool Release(Electron2D.Rid texture, out string? error)
        {
            _ = texture;
            ReleaseCalls++;
            error = null;
            return true;
        }
    }

    private sealed class TestAudioStream(float length) : Electron2D.AudioStream
    {
        public override float GetLength()
        {
            return length;
        }
    }
}
