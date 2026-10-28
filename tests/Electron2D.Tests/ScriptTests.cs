using Electron2D;
using Path = System.IO.Path;
internal static partial class ScriptTests
{
    private static bool _registered;
    internal static ScriptActor? ActorFactoryTarget;
    internal static ScriptPaintEffect? PaintFactoryTarget;
    internal static Func<ScriptActor>? ActorFactory;
    private static ScriptActor CreateActor() => ActorFactory?.Invoke() ?? ActorFactoryTarget ?? new();
    private static ScriptPaintEffect CreatePaint() => PaintFactoryTarget ?? new();
    internal static void Register()
    {
        if (_registered) return;
        Script.RegisterBinding<BaseButton, ScriptAbstractBindingCode>("ScriptTest.AbstractOwner", ScriptAbstractBindingCode.SourcePath, null, ScriptAbstractBindingCode.Bind, ScriptAbstractBindingCode.Clear);
        Script.RegisterAbstract<ScriptBaseActor>("ScriptTest.Base", ScriptBaseActor.SourcePath, ScriptBaseActor.PowerProperty);
        Script.RegisterNode("ScriptTest.Actor", ScriptActor.SourcePath, CreateActor, ScriptActor.CountProperty, ScriptActor.PeerProperty);
        Script.RegisterResource("ScriptTest.Paint", ScriptPaintEffect.SourcePath, CreatePaint);
        Script.RegisterNode<RequiredScriptActor>("ScriptTest.Required", RequiredScriptActor.SourcePath, null);
        Script.RegisterResource<RequiredScriptResource>("ScriptTest.RequiredResource", RequiredScriptResource.SourcePath, null);
        Script.RegisterNodeWithRPC<RequiredScriptRPCActor>("ScriptTest.RequiredRPC", RequiredScriptRPCActor.SourcePath, null, []);
        _registered = true;
    }
    internal static void Run()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_SCRIPT_RPC_PHASE") is { } phase) { ScriptRPCTests.Run(phase); return; }
        Register();
        VerifyAbstractBindingOwner();
        using var script = ResourceLoader.Load<Script>(ScriptActor.SourcePath);
        BackendChecks.VerifyNestedScript(script);
        Check(script.CanInstantiate() && script.IsTool() && !script.IsAbstract() && script.HasSourceCode() && script.GetGlobalName() == "ScriptActor" && script.GetInstanceBaseType() == typeof(Node), "compiled source/type metadata");
        Check(script.SourceCode.Contains("class ScriptActor") && script.HasScriptMethod("Twice") && script.HasScriptMethod("Ping") && script.HasScriptSignal("Pulse") && !script.HasScriptMethod("missing"), "source and inherited CLR metadata");
        var methods = script.GetScriptMethodList(); var signals = script.GetScriptSignalList();
        Check(methods.Length > 0 && signals.Length > 0, "Project metadata snapshots are populated");
        methods[0] = null!; signals[0] = null!;
        Check(script.GetScriptMethodList().All(m => m is not null) && script.GetScriptSignalList().All(e => e is not null), "Metadata snapshots do not mutate the registration catalog");
        Check(script.GetScriptConstantMap<int>()["Limit"] == 12 && script.GetScriptConstantMap<string>()["Category"] == "Actor" && script.GetScriptPropertyList().Length == 3, "typed constants and explicit inherited schema");
        var basis = script.GetBaseScript()!; Check(basis.IsAbstract() && !basis.CanInstantiate(), "abstract source association");
        using var actor = script.New<ScriptActor>(); Check(actor.Count == 3 && script.GetPropertyDefaultValue(ScriptActor.CountProperty, actor) == 3, "typed factory/default");
        Check(ReferenceEquals(actor.GetScript(), script) && ReferenceEquals(actor.ConstructorScript, script), "exact source identity is available in the derived constructor");
        Identity(script);
        Initialization(script);
        SourceChanges();
        using var args = script.New(static () => new ScriptActor(9)); Check(args.Count == 9, "typed constructor arguments");
        Reject<InvalidOperationException>(() => script.New<RichTextEffect>()); Reject<InvalidOperationException>(() => basis.New<Node>()); Reject<ArgumentException>(() => script.GetScriptConstantMap<object>());
        using var copy = (Script)script.Duplicate(); Check(copy.CompiledType == script.CompiledType && copy.SourceCode == script.SourceCode, "resource duplicate association");
        using var ignore = ResourceLoader.Load<Script>(ScriptActor.SourcePath, ResourceLoader.CacheMode.Ignore); Check(!ReferenceEquals(ignore, script), "source ignore identity");
        Check(ResourceLoader.GetRecognizedExtensionsForType<Script>().Contains("cs") && ResourceLoader.GetClassesUsed(ScriptActor.SourcePath).Contains(typeof(ScriptActor)), "loader source/compiled class discovery");
        using var tree = new SceneTree(actor); tree.ProcessFrame(.01); Check(actor.ReadyCalls == 1 && actor.Frames > 0, "ordinary typed callbacks");
        var frames = actor.Frames; script.Dispose(); tree.ProcessFrame(.01); Check(actor.Frames > frames && ReferenceEquals(actor.GetScript(), script), "source resource disposal leaves compiled execution and borrowed identity alive"); Reject<ObjectDisposedException>(() => script.New<Node>());
        using var failing = copy.New<ScriptActor>(); failing.FailReady = true; Reject<AggregateException>(() => new SceneTree(failing)); Check(failing.Tree == null, "failed script activation rolls back");
        using var effectScript = ResourceLoader.Load<Script>(ScriptPaintEffect.SourcePath); using var rich = new RichTextLabel { BBCodeEnabled = true, Text = "[compiled_paint]typed[/compiled_paint]", Size = new(250, 100) }; var effect = rich.InstallEffect(effectScript); Check(effect is ScriptPaintEffect && rich.GetParsedText() == "typed", "source effect installation"); using var effectCopy = (ScriptPaintEffect)effect.Duplicate(); Check(effectCopy.BBCode == "compiled_paint" && ReferenceEquals(effect.GetScript(), effectScript) && ReferenceEquals(effectCopy.GetScript(), effectScript), "compiled effect duplicate and source identity"); rich.CustomEffects = []; Check(effect.IsDisposed, "removed label-owned script effect retirement");
        Edges(copy); Stored(copy); Console.WriteLine("Compiled C# script source/type/schema/factories, callbacks and fresh-process scene storage passed.");
    }
    internal static void VerifyAbstractBindingOwner()
    {
        Register(); using var script = ResourceLoader.Load<Script>(ScriptAbstractBindingCode.SourcePath);
        Check(!script.IsAbstract() && !script.CanInstantiate(), "Concrete code with an abstract binding owner is attachment-only.");
        Reject<InvalidOperationException>(() => script.New<BaseButton>());
        var binds = ScriptAbstractBindingCode.Binds; var clears = ScriptAbstractBindingCode.Clears;
        using var button = new Button(); var id = button.InstanceID; button.SetScript(script);
        Check(button.InstanceID == id && ReferenceEquals(button.GetScript(), script) && ScriptAbstractBindingCode.Binds == binds + 1, "Abstract owner metadata still admits compatible existing concrete instances.");
        button.SetScript(null); Check(ScriptAbstractBindingCode.Clears == clears + 1, "Abstract-role attachment uses ordinary retirement.");
    }
    private static void Identity(Script script)
    {
        using var plain = new ScriptActor();
        Check(plain.GetScript() is null && plain.ConstructorScript is null, "ordinary compiled construction has no implicit asset");
        using var node = script.New<ScriptActor>();
        using var duplicate = (ScriptActor)node.Duplicate();
        Check(ReferenceEquals(duplicate.GetScript(), script) && ReferenceEquals(duplicate.ConstructorScript, script), "exact node duplication retains constructor source");
        using var native = node.Duplicate(0);
        Check(native.GetType() == typeof(Node) && native.GetScript() is null, "dropping the script role drops its source association");
        using var scene = new PackedScene(); scene.Pack(node);
        using var instance = (ScriptActor)scene.Instantiate();
        Check(ReferenceEquals(instance.GetScript(), script) && ReferenceEquals(instance.ConstructorScript, script), "memory scene construction retains source identity");
        using var local = (Script)script.Duplicate(); local.ResourceLocalToScene = true;
        using var localOwner = local.New<ScriptActor>();
        var localChild = local.New<ScriptActor>(); localChild.Name = "SharedSource"; localOwner.AddChild(localChild); localChild.Owner = localOwner;
        using var localScene = new PackedScene(); localScene.Pack(localOwner);
        using var first = (ScriptActor)localScene.Instantiate(); using var second = (ScriptActor)localScene.Instantiate();
        Check(first.GetScript() != local && second.GetScript() != local && first.GetScript() != second.GetScript() &&
            first.GetScript()!.GetLocalScene() == first && second.GetScript()!.GetLocalScene() == second &&
            first.ConstructorScript == first.GetScript() && second.ConstructorScript == second.GetScript(), "scene-local constructor sources are unique, assigned and owned per instance");
        Check(first.GetNode<ScriptActor>("SharedSource").GetScript() == first.GetScript() && second.GetNode<ScriptActor>("SharedSource").GetScript() == second.GetScript(), "scene-local source aliases use one wrapper for all compiled owners");
        using var nested = (Script)script.Duplicate();
        using var nestedOwner = script.New(() => nested.New<ScriptActor>());
        Check(ReferenceEquals(nestedOwner.GetScript(), nested), "nested construction retains the innermost actual source");
        using var worker = script.New(() => Task.Run(() => new ScriptActor()).GetAwaiter().GetResult());
        Check(ReferenceEquals(worker.GetScript(), script) && ReferenceEquals(worker.ConstructorScript, script), "constructor provenance flows to worker code");
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 256; i++) Check(ReferenceEquals(node.GetScript(), script), "prepared source observation");
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "prepared source reads allocate nothing");
        var retired = script.New<ScriptActor>(); retired.VetoDisposal = true; retired.Dispose();
        Check(!retired.IsDisposed && ReferenceEquals(retired.GetScript(), script), "cancelled disposal preserves association");
        retired.Dispose();
        Reject<ObjectDisposedException>(() => retired.GetScript());
        var (context, weak) = CaptureClosedSource(script);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        Check(!weak.IsAlive, "closed factory contexts release their source asset");
        ExecutionContext.Run(context, _ =>
        {
            using var late = new ScriptActor();
            Check(late.GetScript() is null, "a captured closed scope cannot bind later construction");
        }, null);
        Console.WriteLine("Script construction identity, memory copies, nested/worker provenance and zero-byte reads passed.");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (ExecutionContext Context, WeakReference Source) CaptureClosedSource(Script source)
    {
        var script = (Script)source.Duplicate(); ExecutionContext? context = null;
        var instance = script.New(() => { context = ExecutionContext.Capture(); return new ScriptActor(); });
        instance.ConstructorScript = null; instance.Dispose();
        return (context!, new WeakReference(script));
    }
    private static void Edges(Script script)
    {
        Reject<InvalidOperationException>(() => Script.RegisterNode("ScriptTest.Actor", ScriptActor.SourcePath, CreateActor));
        Reject<ArgumentException>(() => Script.RegisterAbstract<ScriptBaseActor>("wrong", ScriptActor.SourcePath));
        Node? bad = null; Reject<InvalidOperationException>(() => script.New<Node>(() => bad = new Node())); Check(bad!.IsDisposed, "incompatible constructor cleanup");
        Reject<ApplicationException>(() => script.New<ScriptActor>(() => throw new ApplicationException("constructor")));
        using var empty = new Script(); Check(!empty.CanInstantiate() && !empty.HasSourceCode() && empty.CompiledType == null, "unbound asset"); Reject<InvalidOperationException>(() => empty.New<Node>());
        var source = ScriptActor.SourcePath; var original = File.ReadAllBytes(source);
        try { File.WriteAllBytes(source, [.. original, 10, 47, 47, 32, 101, 100, 105, 116]); Reject<InvalidDataException>(() => ResourceLoader.Load<Script>(source, ResourceLoader.CacheMode.Replace)); Check(script.CompiledType == typeof(ScriptActor), "changed source preserves compiled class"); }
        finally { File.WriteAllBytes(source, original); }
        script.SourceCode += "\n// editable text"; using var old = script.New<ScriptActor>(); Check(old.GetType() == typeof(ScriptActor), "source setter does not reload CLR class");
        var folder = Path.Combine(Path.GetTempPath(), "e2d-script-build-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try { File.WriteAllText(Path.Combine(folder, "Invalid.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>"); File.WriteAllText(Path.Combine(folder, "Broken.cs"), "public class Broken { this is not C#; }"); var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true }; foreach (var arg in new[] { "build", "Invalid.csproj", "--nologo", "-v:q" }) start.ArgumentList.Add(arg); using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("failed C# build"); } Check(process.ExitCode != 0 && output.GetAwaiter().GetResult().Contains("error CS"), error.GetAwaiter().GetResult()); Check(script.CanInstantiate(), "failed independent build does not replace loaded class"); }
        finally { Directory.Delete(folder, true); }
    }
    internal static void RunHost()
    {
        Run(); using var source = ResourceLoader.Load<Script>(ScriptActor.SourcePath); using var effects = ResourceLoader.Load<Script>(ScriptPaintEffect.SourcePath);
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_SCRIPT_RENDERER") ?? "gpu"; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            var window = new Window { Size = new(440, 220) }; var actor = source.New<ScriptActor>(); window.AddChild(actor);
            var rich = new RichTextLabel { Position = new(15, 20), Size = new(410, 170), BBCodeEnabled = true, Text = "[compiled_paint]Compiled C# effect[/compiled_paint]" }; var effect = (ScriptPaintEffect)rich.InstallEffect(effects); actor.AddChild(rich);
            var frame = 0; long before = 0, total = 0;
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(new(.07f, .08f, .12f)); RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.FramePostDraw += () => { var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) total += after - before; if (frame == 2) { using var image = RenderingServer.Service!.Readback(); if (Environment.GetEnvironmentVariable("ELECTRON2D_SCRIPT_CAPTURE") is { } path) image.SavePNG(path); var cyan = 0; for (var y = 0; y < 100; y++) for (var x = 0; x < 430; x++) { var c = image.GetPixel(x, y); if (c.G > .7f && c.B > .7f && c.R < .2f) cyan++; } Check(cyan > 100 && effect.Calls > 0 && actor.ReadyCalls == 1 && actor.Frames > 0, "native compiled callbacks/effect pixels"); } rich.QueueRedraw(); if (frame >= 64) total += GC.GetAllocatedBytesForCurrentThread() - after; if (++frame == 128) window.Tree!.Quit(); }; };
            Check(Engine.Run(window) == 0 && frame == 128 && total == 0, "prepared compiled script effect/render bytes " + total); Check(effect.IsDisposed, "window teardown retires label-owned compiled effect"); Console.WriteLine("64 prepared compiled script/effect/render intervals: " + total + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static void Stored(Script script)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-script-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            var sourcePath = Path.Combine(folder, "copy.cs"); ResourceSaver.Save(script, sourcePath); Check(File.ReadAllText(sourcePath) == script.SourceCode, "atomic source saver");
            var resourcePath = Path.Combine(folder, "script.e2dres"); ResourceSaver.Save(script, resourcePath); using var restored = ResourceLoader.Load<Script>(resourcePath, ResourceLoader.CacheMode.Ignore); using var created = restored.New<ScriptActor>(); Check(created.Power == 7 && restored.CompiledTypeID == "ScriptTest.Actor", "stored association factory");
            var root = restored.New<ScriptActor>(); root.Name = "Root"; root.Count = 6; var peer = new Node { Name = "Peer" }; root.AddChild(peer); peer.Owner = root; root.Peer = peer; using var packed = new PackedScene(); packed.Pack(root); root.Dispose();
            var scenePath = Path.Combine(folder, "script.e2dscene"); ResourceSaver.Save(packed, scenePath);
            using var loaded = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.Ignore);
            using var loadedActor = (ScriptActor)loaded.Instantiate();
            Check(loadedActor.GetScript() is { CompiledTypeID: "ScriptTest.Actor" } loadedSource && loadedSource.SourceCode == restored.SourceCode &&
                loadedActor.ConstructorScript == loadedSource, "file scene preserves its concrete source and constructor association");
            using var effects = ResourceLoader.Load<Script>(ScriptPaintEffect.SourcePath);
            using var effect = effects.New<ScriptPaintEffect>(); var effectPath = Path.Combine(folder, "effect.e2dres"); ResourceSaver.Save(effect, effectPath, SaverFlags.BundleResources);
            using var loadedEffect = ResourceLoader.Load<ScriptPaintEffect>(effectPath);
            Check(loadedEffect.GetScript() is { CompiledTypeID: "ScriptTest.Paint" } effectSource && loadedEffect.ConstructorScript == effectSource && effectSource.SourceCode == effects.SourceCode,
                "file resource restores the ready source before its exact constructor");
            using var replacement = ResourceLoader.Load<ScriptPaintEffect>(effectPath, ResourceLoader.CacheMode.ReplaceDeep);
            Check(replacement == loadedEffect && replacement.GetScript() is { IsDisposed: false, CompiledTypeID: "ScriptTest.Paint" }, "deep cache replacement keeps owner identity and a live remapped source");
            ArchiveSourceEdges(effectPath, loadedEffect, resourcePath);
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ScriptTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_SCRIPT"); start.Environment.Remove("ELECTRON2D_TEST_SCRIPT_HOST"); start.Environment["ELECTRON2D_TEST_SCRIPT_CHILD"] = scenePath;
            using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("script scene"); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh script scene passed"), error.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void RunChild(string path)
    {
        Register(); using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var actor = (ScriptActor)packed.Instantiate(); using var tree = new SceneTree(actor); tree.ProcessFrame(.01); Check(actor.Count == 6 && actor.Power == 7 && ReferenceEquals(actor.Peer, actor.GetNode<Node>("Peer")) && actor.ReadyCalls == 1 && actor.Frames > 0, "fresh exact compiled callbacks/state/references");
        Check(actor.GetScript() is { CompiledTypeID: "ScriptTest.Actor" } source && actor.ConstructorScript == source && source.SourceCode.Contains("class ScriptActor"), "fresh process restores exact compiled source identity before construction"); Console.WriteLine("Fresh script scene passed");
        Check(actor.Initializations == 1 && actor.BaseInitializations == 1 && actor.BaseConstructorSource == actor.GetScript() && actor.BaseConstructorIdentity == actor.InstanceID && actor.ConstructorCount == 3 && actor.Count == 6,
            "fresh scene initializes each compiled constructor once before restoring stored values");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
