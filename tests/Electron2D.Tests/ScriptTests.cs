using Electron2D;
using Path = System.IO.Path;
internal static class ScriptTests
{
    private static bool _registered;
    private static ScriptActor CreateActor() => new();
    private static ScriptPaintEffect CreatePaint() => new();
    internal static void Register()
    {
        if (_registered) return;
        Script.RegisterAbstract<ScriptBaseActor>("ScriptTest.Base", ScriptBaseActor.SourcePath, ScriptBaseActor.PowerProperty);
        Script.RegisterNode("ScriptTest.Actor", ScriptActor.SourcePath, CreateActor, ScriptActor.CountProperty, ScriptActor.PeerProperty);
        Script.RegisterResource("ScriptTest.Paint", ScriptPaintEffect.SourcePath, CreatePaint);
        _registered = true;
    }
    internal static void Run()
    {
        Register();
        using var script = ResourceLoader.Load<Script>(ScriptActor.SourcePath);
        Check(script.CanInstantiate() && script.IsTool() && !script.IsAbstract() && script.HasSourceCode() && script.GetGlobalName() == "ScriptActor" && script.GetInstanceBaseType() == typeof(Node), "compiled source/type metadata");
        Check(script.SourceCode.Contains("class ScriptActor") && script.HasScriptMethod("Twice") && script.HasScriptMethod("Ping") && script.HasScriptSignal("Pulse") && !script.HasScriptMethod("missing"), "source and inherited CLR metadata");
        Check(script.GetScriptConstantMap<int>()["Limit"] == 12 && script.GetScriptConstantMap<string>()["Category"] == "Actor" && script.GetScriptPropertyList().Length == 3, "typed constants and explicit inherited schema");
        var basis = script.GetBaseScript()!; Check(basis.IsAbstract() && !basis.CanInstantiate(), "abstract source association");
        using var actor = script.New<ScriptActor>(); Check(actor.Count == 3 && script.GetPropertyDefaultValue(ScriptActor.CountProperty, actor) == 3, "typed factory/default");
        using var args = script.New(static () => new ScriptActor(9)); Check(args.Count == 9, "typed constructor arguments");
        Reject<InvalidOperationException>(() => script.New<RichTextEffect>()); Reject<InvalidOperationException>(() => basis.New<Node>()); Reject<ArgumentException>(() => script.GetScriptConstantMap<object>());
        using var copy = (Script)script.Duplicate(); Check(copy.CompiledType == script.CompiledType && copy.SourceCode == script.SourceCode, "resource duplicate association");
        using var ignore = ResourceLoader.Load<Script>(ScriptActor.SourcePath, ResourceLoader.CacheMode.Ignore); Check(!ReferenceEquals(ignore, script), "source ignore identity");
        Check(ResourceLoader.GetRecognizedExtensionsForType<Script>().Contains("cs") && ResourceLoader.GetClassesUsed(ScriptActor.SourcePath).Contains(typeof(ScriptActor)), "loader source/compiled class discovery");
        using var tree = new SceneTree(actor); tree.ProcessFrame(.01); Check(actor.ReadyCalls == 1 && actor.Frames > 0, "ordinary typed callbacks");
        var frames = actor.Frames; script.Dispose(); tree.ProcessFrame(.01); Check(actor.Frames > frames, "source resource disposal leaves constructed CLR execution alive"); Reject<ObjectDisposedException>(() => script.New<Node>());
        using var failing = copy.New<ScriptActor>(); failing.FailReady = true; Reject<AggregateException>(() => new SceneTree(failing)); Check(failing.Tree == null, "failed script activation rolls back");
        using var effectScript = ResourceLoader.Load<Script>(ScriptPaintEffect.SourcePath); using var rich = new RichTextLabel { BBCodeEnabled = true, Text = "[compiled_paint]typed[/compiled_paint]", Size = new(250, 100) }; var effect = rich.InstallEffect(effectScript); Check(effect is ScriptPaintEffect && rich.GetParsedText() == "typed", "source effect installation"); using var effectCopy = (ScriptPaintEffect)effect.Duplicate(); Check(effectCopy.BBCode == "compiled_paint", "compiled effect duplicate"); rich.CustomEffects = []; Check(effect.IsDisposed, "removed label-owned script effect retirement");
        Edges(copy); Stored(copy); Console.WriteLine("Compiled C# script source/type/schema/factories, callbacks and fresh-process scene storage passed.");
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
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ScriptTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_SCRIPT"); start.Environment.Remove("ELECTRON2D_TEST_SCRIPT_HOST"); start.Environment["ELECTRON2D_TEST_SCRIPT_CHILD"] = scenePath;
            using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("script scene"); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh script scene passed"), error.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void RunChild(string path)
    {
        Register(); using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var actor = (ScriptActor)packed.Instantiate(); using var tree = new SceneTree(actor); tree.ProcessFrame(.01); Check(actor.Count == 6 && actor.Power == 7 && ReferenceEquals(actor.Peer, actor.GetNode<Node>("Peer")) && actor.ReadyCalls == 1 && actor.Frames > 0, "fresh exact compiled callbacks/state/references"); Console.WriteLine("Fresh script scene passed");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
