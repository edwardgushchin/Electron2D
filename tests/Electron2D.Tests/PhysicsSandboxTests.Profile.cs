using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using Electron2D.Examples.PhysicsSandbox;
using SDL = SDL3.SDL;

internal static partial class PhysicsSandboxTests
{
    internal static void RunProfile()
    {
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_SERVER") == "1") { RunServerSmashProfile(); return; }
        using var font = new FontFile();
        font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        using var bold = new FontFile();
        bold.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-SemiBold.ttf"));
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_LONG") == "1") { RunLongStress(font, bold, configuration); return; }
        var optimized = !(typeof(Engine).Assembly.GetCustomAttributes(typeof(DebuggableAttribute), false).OfType<DebuggableAttribute>().FirstOrDefault()?.IsJITOptimizerDisabled ?? false);
        var failures = new List<string>();
        PhysicsSpace.ProfilingEnabled = true;
        var sceneFilter = Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_SCENE");
        var indices = sceneFilter is null ? Enumerable.Range(0, SandboxWindow.SceneNames.Length).ToArray() : sceneFilter.Split(',').Select(int.Parse).ToArray();
        var stressCount = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_STRESS_COUNT") ?? "512");
        var warmup = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_WARMUP") ?? "1600");
        var physicsSamples = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_SAMPLES") ?? "256");
        var tag = Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_TAG") ?? "current";
        var path = System.IO.Path.GetFullPath($"bin/physics-sandbox/profile-{configuration}-{tag}.json");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var physics = new List<object>();
        foreach (var index in Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_NATIVE_ONLY") == "1" ? [] : indices)
        {
            var constructionStart = GC.GetAllocatedBytesForCurrentThread();
            using var root = new SubViewport { Size = SandboxWindow.ClientSize };
            var scene = new PhysicsScene(index, font);
            if (index == 8) scene.SetStoryParameter(stressCount);
            if (index == 11) scene.SetSmashPopulation(int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_SMASH_COUNT") ?? "9600"));
            root.AddChild(scene);
            using var tree = new SceneTree(root);
            GPUPhysicsWorld? gpu = null;
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_GPU_SOLVER") == "1")
            {
                var gpuSpace = scene.Colliders.OfType<PhysicsBody>().First().Space!;
                gpu = gpuSpace.EnableGPUSolver();
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_CREATION") == "1")
                    gpu.DisableContactCreation();
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_REMOVAL") == "1")
                {
                    var profileWorld = Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID);
                    profileWorld.destroyDisjointContact = null!; profileWorld.finishContactRemovals = null!;
                }
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_ISLAND_GRAPH") == "1")
                {
                    var profileWorld = Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID);
                    profileWorld.beginIslandChanges = profileWorld.finishIslandChanges = null!; profileWorld.changeContactIsland = null!; profileWorld.islandGraphChanged = null!;
                }
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_FINALIZATION") == "1")
                    Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID).finalizeBodyStates = null!;
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_COLORS") == "1")
                {
                    var profileWorld = Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID);
                    profileWorld.beginConstraintColors = profileWorld.finishConstraintColors = null!; profileWorld.selectConstraintColor = null!;
                }
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_SPLITS") == "1")
                    Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID).splitIsland = null!;
                var cpuContacts = Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_CONTACTS") == "1";
                if (cpuContacts) Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID).generateManifolds = gpu.GenerateManifolds;
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_CPU_PAIRS") == "1")
                    Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID).findBroadPhasePairs = null!;
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_UPLOAD_GEOMETRY") == "1")
                    Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID).generateManifolds = (context, count) =>
                    {
                        context.world.shapeGeometryChanged = null!;
                        if (cpuContacts) gpu.GenerateManifolds(context, count); else gpu.UpdateContacts(context, count);
                    };
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_UPLOAD_MANIFOLDS") == "1")
                    Box2D.NET.B2Worlds.b2GetWorldFromId(gpuSpace.WorldID).solveConstraints = context =>
                    {
                        context.generatedManifoldOwner = null!;
                        gpu.Solve(context);
                    };
            }
            var constructionBytes = GC.GetAllocatedBytesForCurrentThread() - constructionStart;
            for (var i = 0; i < warmup; i++) { Exercise(scene, i); PhysicsTick(tree); }
            var samples = new double[physicsSamples];
            var sampleBytes = new long[physicsSamples];
            var awakeCounts = new int[physicsSamples];
            var phases = new double[8];
            var gpuPhases = new double[4];
            long residentContacts = 0, uploadedManifolds = 0, contactUploadBytes = 0;
            long residentHistories = 0, uploadedHistories = 0, historyUploadBytes = 0;
            long residentGeometry = 0, uploadedGeometry = 0, geometryUploadBytes = 0;
            var phaseBytes = new long[8];
            var space = scene.Colliders.OfType<PhysicsBody>().First().Space!;
            double backendStep = 0, backendSolve = 0, backendPairs = 0, backendCollide = 0, backendSensors = 0, backendTransforms = 0;
            var backendWorld = scene.Colliders.OfType<PhysicsBody>().First().Space!.WorldID;
            var solverSubmissionStart = gpu?.SolverSubmissionCount ?? 0;
            var finalizedStart = gpu?.BodyFinalizationCount ?? 0;
            var finalizationBatchStart = gpu?.BodyFinalizationBatchCount ?? 0;
            var finalizationBytesStart = gpu?.BodyFinalizationTransferBytes ?? 0;
            var colorProfileStart = gpu?.ConstraintColorProfileMS.ToArray();
            var colorChangesStart = gpu?.ConstraintColorChangeCount ?? 0;
            var colorSubmissionsStart = gpu?.ConstraintColorSubmissionCount ?? 0;
            var colorTransferStart = gpu?.ConstraintColorTransferBytes ?? 0;
            var graphProfileStart = gpu?.IslandGraphProfileMS.ToArray();
            var broadPhaseStart = gpu?.BroadPhaseProfileMS.ToArray();
            var broadPhaseUploadStart = gpu?.BroadPhaseUploadBytes ?? 0;
            var broadPhaseReadbackStart = gpu?.BroadPhaseReadbackBytes ?? 0;
            var broadPhaseCandidateStart = gpu?.BroadPhaseCandidateTotal ?? 0;
            var pairTableSnapshotStart = gpu?.PairTableSnapshotCount ?? 0;
            var pairTableRebuildStart = gpu?.PairTableRebuildCount ?? 0;
            var pairTableSlotsStart = gpu?.PairTableUpdatedSlots ?? 0;
            var pairTableUploadStart = gpu?.PairTableUploadBytes ?? 0;
            var treeSnapshotStart = gpu?.TreeSnapshotCount ?? 0;
            var treeRebuildStart = gpu?.TreeRebuildCount ?? 0;
            var treeRefitStart = gpu?.TreeRefitCount ?? 0;
            var treeUpdateStart = gpu?.TreeUpdatedProxies ?? 0;
            var treeUploadStart = gpu?.TreeUploadBytes ?? 0;
            var filterSnapshotStart = gpu?.FilterSnapshotCount ?? 0;
            var filterShapeStart = gpu?.FilterUpdatedShapes ?? 0;
            var filterJointStart = gpu?.FilterUpdatedJoints ?? 0;
            var filterUploadStart = gpu?.FilterUploadBytes ?? 0;
            var geometryResetStart = gpu?.GeometryCacheResetCount ?? 0;
            var updatedContactStart = gpu?.UpdatedContactCount ?? 0;
            var createdContactStart = gpu?.CreatedContactCount ?? 0;
            var contactPoolSnapshotStart = gpu?.ContactPoolSnapshotCount ?? 0;
            var contactPoolUploadStart = gpu?.ContactPoolUploadBytes ?? 0;
            var contactLinkUploadStart = gpu?.ContactLinkUploadBytes ?? 0;
            var graphStart = gpu?.IslandChangeCount ?? 0; var mergeStart = gpu?.MergedIslandCount ?? 0; var graphBytesStart = gpu?.IslandGraphTransferBytes ?? 0;
            var graphUploadStart = gpu?.IslandGraphUploadBytes ?? 0; var graphReadbackStart = gpu?.IslandGraphReadbackBytes ?? 0;
            var graphSnapshotStart = gpu?.IslandGraphSnapshotCount ?? 0; var graphRetryStart = gpu?.IslandGraphReadbackRetries ?? 0;
            var splitStart = gpu?.SplitIslandCount ?? 0;
            var componentStart = gpu?.SplitComponentCount ?? 0;
            var removedStart = gpu?.RemovedContactCount ?? 0;
            var removalReadbackStart = gpu?.ContactRemovalReadbackBytes ?? 0;
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var allThreadsAllocated = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < samples.Length; i++)
            {
                var tickAllocated = GC.GetAllocatedBytesForCurrentThread();
                var start = Stopwatch.GetTimestamp(); Exercise(scene, i + warmup); PhysicsTick(tree);
                samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                sampleBytes[i] = GC.GetAllocatedBytesForCurrentThread() - tickAllocated;
                awakeCounts[i] = Box2D.NET.B2Worlds.b2GetWorldFromId(backendWorld).solverSets.data[(int)Box2D.NET.B2SolverSetType.b2_awakeSet].bodySims.count;
                for (var phase = 0; phase < phases.Length; phase++) { phases[phase] += space.ProfileMS[phase]; phaseBytes[phase] += space.ProfileBytes[phase]; }
                if (gpu is not null)
                {
                    for (var phase = 0; phase < gpuPhases.Length; phase++) gpuPhases[phase] += gpu.SolverProfileMS[phase];
                    residentContacts += gpu.ResidentContactCount; uploadedManifolds += gpu.UploadedManifoldCount; contactUploadBytes += gpu.ContactUploadBytes;
                    residentGeometry += gpu.ResidentGeometryCount; uploadedGeometry += gpu.UploadedGeometryCount; geometryUploadBytes += gpu.GeometryUploadBytes;
                    residentHistories += gpu.ResidentHistoryCount; uploadedHistories += gpu.UploadedHistoryCount; historyUploadBytes += gpu.HistoryUploadBytes;
                }
                var bp = Box2D.NET.B2Worlds.b2World_GetProfile(backendWorld);
                backendStep += bp.step; backendSolve += bp.solve; backendPairs += bp.pairs; backendCollide += bp.collide; backendSensors += bp.sensors; backendTransforms += bp.transforms;
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            allThreadsAllocated = GC.GetTotalAllocatedBytes(true) - allThreadsAllocated;
            if (allocated != 0) failures.Add($"Physics {index}: {allocated} bytes across {samples.Length} ticks.");
            if (allThreadsAllocated != 0) failures.Add($"Physics {index}, all managed threads: {allThreadsAllocated} bytes across {samples.Length} ticks.");
            Console.WriteLine($"Physics {index}: {samples.Average():0.000} ms/tick, {allocated} B/{samples.Length} ticks, {scene.BodyCount} bodies.");
            var constraintColorPhaseMS = gpu?.ConstraintColorProfileMS.Select((value, i) => (value - colorProfileStart![i]) / samples.Length).ToArray();
            var constraintColorChanges = (gpu?.ConstraintColorChangeCount ?? 0) - colorChangesStart;
            var constraintColorSubmissions = (gpu?.ConstraintColorSubmissionCount ?? 0) - colorSubmissionsStart;
            var constraintColorTransferBytes = (gpu?.ConstraintColorTransferBytes ?? 0) - colorTransferStart;
            var islandGraphPhaseMS = gpu?.IslandGraphProfileMS.Select((value, i) => (value - graphProfileStart![i]) / samples.Length).ToArray();
            var broadPhaseMS = gpu?.BroadPhaseProfileMS.Select((value, i) => (value - broadPhaseStart![i]) / samples.Length).ToArray();
            var broadPhaseUploadBytes = (gpu?.BroadPhaseUploadBytes ?? 0) - broadPhaseUploadStart;
            var broadPhaseReadbackBytes = (gpu?.BroadPhaseReadbackBytes ?? 0) - broadPhaseReadbackStart;
            var broadPhaseCandidates = (gpu?.BroadPhaseCandidateTotal ?? 0) - broadPhaseCandidateStart;
            var pairTableSnapshots = (gpu?.PairTableSnapshotCount ?? 0) - pairTableSnapshotStart;
            var pairTableRebuilds = (gpu?.PairTableRebuildCount ?? 0) - pairTableRebuildStart;
            var pairTableUpdatedSlots = (gpu?.PairTableUpdatedSlots ?? 0) - pairTableSlotsStart;
            var pairTableUploadBytes = (gpu?.PairTableUploadBytes ?? 0) - pairTableUploadStart;
            var treeSnapshots = (gpu?.TreeSnapshotCount ?? 0) - treeSnapshotStart;
            var treeRebuilds = (gpu?.TreeRebuildCount ?? 0) - treeRebuildStart;
            var treeRefits = (gpu?.TreeRefitCount ?? 0) - treeRefitStart;
            var treeUpdatedProxies = (gpu?.TreeUpdatedProxies ?? 0) - treeUpdateStart;
            var treeUploadBytes = (gpu?.TreeUploadBytes ?? 0) - treeUploadStart;
            var filterSnapshots = (gpu?.FilterSnapshotCount ?? 0) - filterSnapshotStart;
            var filterUpdatedShapes = (gpu?.FilterUpdatedShapes ?? 0) - filterShapeStart;
            var filterUpdatedJoints = (gpu?.FilterUpdatedJoints ?? 0) - filterJointStart;
            var filterUploadBytes = (gpu?.FilterUploadBytes ?? 0) - filterUploadStart;
            var geometryCacheResets = (gpu?.GeometryCacheResetCount ?? 0) - geometryResetStart;
            var updatedContacts = (gpu?.UpdatedContactCount ?? 0) - updatedContactStart;
            var createdContacts = (gpu?.CreatedContactCount ?? 0) - createdContactStart;
            var contactPoolSnapshots = (gpu?.ContactPoolSnapshotCount ?? 0) - contactPoolSnapshotStart;
            var contactPoolUploadBytes = (gpu?.ContactPoolUploadBytes ?? 0) - contactPoolUploadStart;
            var contactLinkUploadBytes = (gpu?.ContactLinkUploadBytes ?? 0) - contactLinkUploadStart;
            var removedContacts = (gpu?.RemovedContactCount ?? 0) - removedStart;
            var removalReadbackBytes = (gpu?.ContactRemovalReadbackBytes ?? 0) - removalReadbackStart;
            var splitIslands = (gpu?.SplitIslandCount ?? 0) - splitStart;
            var splitComponents = (gpu?.SplitComponentCount ?? 0) - componentStart;
            var islandChanges = (gpu?.IslandChangeCount ?? 0) - graphStart; var mergedIslands = (gpu?.MergedIslandCount ?? 0) - mergeStart; var islandGraphTransferBytes = (gpu?.IslandGraphTransferBytes ?? 0) - graphBytesStart;
            var islandGraphUploadBytes = (gpu?.IslandGraphUploadBytes ?? 0) - graphUploadStart; var islandGraphReadbackBytes = (gpu?.IslandGraphReadbackBytes ?? 0) - graphReadbackStart;
            var islandGraphSnapshots = (gpu?.IslandGraphSnapshotCount ?? 0) - graphSnapshotStart; var islandGraphRetries = (gpu?.IslandGraphReadbackRetries ?? 0) - graphRetryStart;
            var solverSubmissions = (gpu?.SolverSubmissionCount ?? 0) - solverSubmissionStart;
            var finalizedBodies = (gpu?.BodyFinalizationCount ?? 0) - finalizedStart;
            var finalizationBatches = (gpu?.BodyFinalizationBatchCount ?? 0) - finalizationBatchStart;
            var finalizationTransferBytes = (gpu?.BodyFinalizationTransferBytes ?? 0) - finalizationBytesStart;
            physics.Add(new { solverSubmissions, finalizedBodies, finalizationBatches, finalizationTransferBytes, constraintColorPhaseMS, constraintColorChanges, constraintColorSubmissions, constraintColorTransferBytes, islandGraphPhaseMS, islandGraphUploadBytes, islandGraphReadbackBytes, islandGraphSnapshots, islandGraphRetries, islandChanges, mergedIslands, islandGraphTransferBytes, splitIslands, splitComponents, removedContacts, removalReadbackBytes, contactLinkUploadBytes, createdContacts, contactPoolSnapshots, contactPoolUploadBytes, updatedContacts, geometryCacheResets, residentGeometry, uploadedGeometry, geometryUploadBytes, filterSnapshots, filterUpdatedShapes, filterUpdatedJoints, filterUploadBytes, treeSnapshots, treeRebuilds, treeRefits, treeUpdatedProxies, treeUploadBytes, pairTableSnapshots, pairTableRebuilds, pairTableUpdatedSlots, pairTableUploadBytes, broadPhaseMS, broadPhaseUploadBytes, broadPhaseReadbackBytes, broadPhaseCandidates, scene = SandboxWindow.SceneNames[index], bodies = scene.BodyCount, constructionBytes, stateSHA256 = StateHash(scene), meanMS = samples.Average(), p95MS = Percentile(samples), bytesPerTick = (double)allocated / samples.Length, allThreadsAllocated, sampleBytes, awakeCounts, phaseBytes, contacts = Box2D.NET.B2Worlds.b2World_GetCounters(backendWorld).contactCount, backendStepMS = backendStep / samples.Length, backendSolveMS = backendSolve / samples.Length, backendTransformsMS = backendTransforms / samples.Length, backendPairsMS = backendPairs / samples.Length, backendCollideMS = backendCollide / samples.Length, backendSensorsMS = backendSensors / samples.Length, phaseMS = phases.Select(v => v / samples.Length).ToArray(), gpuSolverPhaseMS = gpuPhases.Select(v => v / samples.Length).ToArray(), residentContacts, uploadedManifolds, contactUploadBytes, residentHistories, uploadedHistories, historyUploadBytes });
        }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_HEADLESS") == "1")
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new { configuration, optimized, warmupPhysics = warmup, physicsSamples, physics, failures }, new JsonSerializerOptions { WriteIndented = true }));
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_DIAGNOSTIC") != "1")
                Check(failures.Count == 0, "Zero managed allocation budget: " + string.Join("; ", failures));
            return;
        }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        Engine.MaxFPS = 60;
        var native = new List<object>();
        var nativeWarmup = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_NATIVE_WARMUP") ?? "768");
        var nativeSamples = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_NATIVE_SAMPLES") ?? "192");
        var trialFrames = nativeWarmup + nativeSamples;
        using var window = new SandboxWindow(font, bold);
        bool[]? nativeInput = null;
        var times = new double[nativeSamples]; var renders = new double[nativeSamples]; var bytes = new long[nativeSamples]; var renderBytes = new long[nativeSamples];
        var allThreadBytes = new long[nativeSamples];
        var physicsPhaseBytes = new long[nativeSamples * 8];
        long previousAllThreadBytes = 0;
        long previous = 0, previousBytes = 0, renderStart = 0, renderAllocated = 0;
        var frame = 0;
        Action pre = () => { if (frame % trialFrames != 0) Exercise(window.Scene, frame % trialFrames); renderStart = Stopwatch.GetTimestamp(); renderAllocated = GC.GetAllocatedBytesForCurrentThread(); };
        Action post = () =>
        {
            var now = Stopwatch.GetTimestamp(); var allocated = GC.GetAllocatedBytesForCurrentThread();
            var allAllocated = GC.GetTotalAllocatedBytes(true);
            var renderMS = Stopwatch.GetElapsedTime(renderStart, now).TotalMilliseconds;
            var renderingBytes = allocated - renderAllocated;
            var trial = frame / trialFrames; var phase = frame % trialFrames;
            if (phase == 0)
            {
                if (trial == indices.Length) { window.Tree!.Quit(); return; }
                window.SwitchScene(indices[trial]);
                if (window.Scene.Index == 8) window.Scene.SetStoryParameter(stressCount);
                if (window.Scene.Index == 11) window.Scene.SetSmashPopulation(int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_SMASH_COUNT") ?? "9600"));
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_GPU_SOLVER") == "1") window.Scene.Colliders.OfType<PhysicsBody>().First().Space!.EnableGPUSolver();
                Exercise(window.Scene, 0);
                ClearProfileHover();
            }
            if (phase >= nativeWarmup)
            {
                var i = phase - nativeWarmup;
                times[i] = Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds;
                renders[i] = renderMS; bytes[i] = allocated - previousBytes; renderBytes[i] = renderingBytes;
                allThreadBytes[i] = allAllocated - previousAllThreadBytes;
                var space = ((PhysicsBody)window.Scene.Colliders[0]).Space!;
                space.ProfileBytes.CopyTo(physicsPhaseBytes, i * 8);
            }
            if (phase == trialFrames - 1)
            {
                var mean = times.Average();
                if (bytes.Max() != 0) failures.Add($"Native {trial}: max {bytes.Max()} bytes/frame, render max {renderBytes.Max()}.");
                if (allThreadBytes.Max() != 0) failures.Add($"Native {trial}, all managed threads: max {allThreadBytes.Max()} bytes/frame.");
                Console.WriteLine($"Profile {trial}: {1000 / mean:0.0} FPS, {bytes.Average():0.0} B/frame, max {bytes.Max()}, render {renderBytes.Average():0.0}");
                native.Add(new { scene = SandboxWindow.SceneNames[indices[trial]], bodies = window.Scene.BodyCount, fps = 1000 / mean, frameMS = mean, frameP95MS = Percentile(times), renderMS = renders.Average(), renderP95MS = Percentile(renders), bytesPerFrame = bytes.Average(), renderBytesPerFrame = renderBytes.Average(), maxBytesPerFrame = bytes.Max(), maxRenderBytesPerFrame = renderBytes.Max(), allThreadBytesPerFrame = allThreadBytes.Average(), maxAllThreadBytesPerFrame = allThreadBytes.Max(), sampleBytes = bytes.ToArray(), physicsPhaseBytes = physicsPhaseBytes.ToArray() });
                if (window.Scene.Index == 11)
                {
                    using var capture = RenderingServer.Service!.Readback();
                    capture.SavePNG(System.IO.Path.ChangeExtension(path, ".png"));
                }
            }
            frame++; previous = Stopwatch.GetTimestamp(); previousBytes = GC.GetAllocatedBytesForCurrentThread();
            previousAllThreadBytes = GC.GetTotalAllocatedBytes(true);
        };
        window.Ready += _ => { nativeInput = SuppressProfileInput(); RenderingServer.FramePreDraw += pre; RenderingServer.FramePostDraw += post; };
        try { Check(RunProfileWindow(window) == 0 && frame >= indices.Length * trialFrames, "Complete native performance profile."); }
        finally { RestoreProfileInput(nativeInput); if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= pre; RenderingServer.FramePostDraw -= post; } }
        File.WriteAllText(path, JsonSerializer.Serialize(new { configuration, optimized, platform = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier, backend = "gpu", maxFPS = 60, maxPhysicsStepsPerFrame = 1, warmupPhysics = warmup, physicsSamples, warmupNative = nativeWarmup, nativeSamples, thread = "scene/render owner and all managed threads", physics, native, failures }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PhysicsSandbox performance profile: " + path);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_PROFILE_DIAGNOSTIC") != "1")
            Check(failures.Count == 0, "Zero managed allocation budget: " + string.Join("; ", failures));
    }

    private static void RunLongStress(Font font, Font bold, string configuration)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        Engine.MaxFPS = 60;
        using var window = new SandboxWindow(font, bold);
        bool[]? nativeInput = null;
        var rows = new List<object>();
        var cadence = new List<double>();
        var watch = new Stopwatch(); var interval = new Stopwatch();
        var frames = 0; double render = 0; long stamp = 0, step = 0;
        Action pre = () => stamp = Stopwatch.GetTimestamp();
        Action post = () =>
        {
            render += Stopwatch.GetElapsedTime(stamp).TotalMilliseconds; frames++;
            if (interval.Elapsed.TotalSeconds < 15) return;
            var seconds = interval.Elapsed.TotalSeconds; var elapsed = watch.Elapsed.TotalSeconds;
            var fps = frames / seconds; var renderMS = render / frames;
            var physicsHz = (window.Scene.PhysicsSteps - step) / seconds;
            Console.WriteLine($"Long stress {elapsed:0}s: {fps:0.0} FPS, render {renderMS:0.00} ms, physics {physicsHz:0.0} Hz, impacts {window.Scene.ContactEvents}.");
            rows.Add(new { seconds = elapsed, fps, renderMS, physicsHz, impacts = window.Scene.ContactEvents, managedHeap = GC.GetTotalMemory(false) });
            cadence.Add(fps);
            frames = 0; render = 0; step = window.Scene.PhysicsSteps; interval.Restart();
            if (elapsed >= 180) window.Tree!.Quit();
        };
        window.Ready += _ =>
        {
            nativeInput = SuppressProfileInput();
            window.SwitchScene(8); window.Scene.SetStoryParameter(1024);
            ClearProfileHover(); watch.Start(); interval.Start();
            RenderingServer.FramePreDraw += pre; RenderingServer.FramePostDraw += post;
        };
        try { Check(RunProfileWindow(window) == 0 && rows.Count >= 12, "Three-minute native settling stress."); }
        finally { RestoreProfileInput(nativeInput); if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= pre; RenderingServer.FramePostDraw -= post; } }
        var path = System.IO.Path.GetFullPath($"bin/physics-sandbox/long-stress-{configuration}.json");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { configuration, bodies = 1024, durationSeconds = 180, rows }, new JsonSerializerOptions { WriteIndented = true }));
        var settled = cadence.Skip(3).Take(3).Average();
        var final = cadence.TakeLast(3).Average();
        Check(final >= settled * .8, $"Settled stress cadence does not decay: {settled:0.0} to {final:0.0} FPS.");
    }

    private static int RunProfileWindow(SandboxWindow window)
    {
        var activate = SDL.GetHint(SDL.Hints.WindowActivateWhenShown);
        SDL.SetHint(SDL.Hints.WindowActivateWhenShown, "0");
        try { return Engine.Run(window); }
        finally { if (activate is null) SDL.ResetHint(SDL.Hints.WindowActivateWhenShown); else SDL.SetHint(SDL.Hints.WindowActivateWhenShown, activate); }
    }

    // The prepared-frame budget excludes fresh native input. Interactive input has its own native suite.
    private static readonly SDL.EventType[] ProfileInputEvents = [SDL.EventType.MouseMotion, SDL.EventType.MouseButtonDown, SDL.EventType.MouseButtonUp, SDL.EventType.MouseWheel, SDL.EventType.KeyDown, SDL.EventType.KeyUp, SDL.EventType.TextInput, SDL.EventType.TextEditing];
    private static bool[] SuppressProfileInput()
    {
        var enabled = new bool[ProfileInputEvents.Length];
        for (var i = 0; i < enabled.Length; i++) { var type = (uint)ProfileInputEvents[i]; enabled[i] = SDL.EventEnabled(type); SDL.SetEventEnabled(type, false); }
        return enabled;
    }
    private static void RestoreProfileInput(bool[]? enabled)
    {
        if (enabled is null) return;
        for (var i = 0; i < enabled.Length; i++) SDL.SetEventEnabled((uint)ProfileInputEvents[i], enabled[i]);
    }
    private static void ClearProfileHover()
    {
        Span<SDL.Event> input = stackalloc SDL.Event[1];
        input[0] = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]), X = -20, Y = -20 } };
        Check(SDL.PeepEvents(input, 1, SDL.EventAction.AddEvent, 0, 0) == 1, "Profile hover motion is queued before warmup.");
    }

    private static void Exercise(PhysicsScene scene, int frame)
    {
        var scale = scene.Index == 11 ? PhysicsScene.SmashScale : 1;
        scene.SetPointer(scene.GetGlobalTransformWithCanvas() * (new Vector2(570 + 270 * MathF.Sin(frame * .08f), 380 + 160 * MathF.Cos(frame * .06f)) * scale));
        if (frame == 0 && scene.Index is 9 or 10 or 11) scene.Act(scene.Index == 9 ? 1 : 0);
        if (frame % 48 == 0 && scene.Bodies.Count > 0)
        {
            var body = scene.Bodies[0];
            body.ApplyCentralImpulse(new Vector2(frame % 96 == 0 ? 80 : -80, -40) * scale);
        }
    }

    private static string StateHash(PhysicsScene scene)
    {
        var values = new float[(scene.Bodies.Count + scene.SmashFragmentCount) * 6];
        for (var i = 0; i < scene.Bodies.Count; i++)
        {
            var body = scene.Bodies[i]; var point = body.Position; var velocity = body.LinearVelocity;
            values[i * 6] = point.X; values[i * 6 + 1] = point.Y; values[i * 6 + 2] = body.Rotation;
            values[i * 6 + 3] = velocity.X; values[i * 6 + 4] = velocity.Y; values[i * 6 + 5] = body.AngularVelocity;
        }
        for (var i = 0; i < scene.SmashFragmentCount; i++)
        {
            var fragment = scene.SmashFragments[i]; var state = fragment.State!;
            var offset = (scene.Bodies.Count + i) * 6; var point = fragment.Pose.Origin; var velocity = state.LinearVelocity;
            values[offset] = point.X; values[offset + 1] = point.Y; values[offset + 2] = fragment.Pose.Rotation;
            values[offset + 3] = velocity.X; values[offset + 4] = velocity.Y; values[offset + 5] = state.AngularVelocity;
        }
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan())));
    }

    private static double Percentile(double[] samples)
    {
        var sorted = (double[])samples.Clone(); Array.Sort(sorted); return sorted[(int)(sorted.Length * .95)];
    }
}
