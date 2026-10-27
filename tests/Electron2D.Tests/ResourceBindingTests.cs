using System.Diagnostics;
using System.Reflection;
using Electron2D;
using Path = System.IO.Path;

internal sealed class ResourceBindingTests
{
    private static bool _registered;
    private static readonly string SourcePath = Source();
    private static Resource CreatePlain() => new();
    private static LiveScriptResource Create() => new();
    private static void Bind(Resource owner) => owner.ResourceName = "bound";
    private static void Clear(Resource owner) { }
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterResource("ResourceBinding.Owner", Create);
        ResourceFileTypes.RegisterNode("ResourceBinding.Driver", Driver.Create);
        Script.RegisterBinding<LiveScriptResource, ResourceBindingForward>("ResourceBinding.Forward", ResourceBindingForward.SourcePath, Create,
            ResourceBindingForward.Bind, ResourceBindingForward.Clear, LiveScriptResource.GainProperty, LiveScriptResource.LinkProperty);
        Script.RegisterBinding<LiveScriptResource, ResourceBindingReverse>("ResourceBinding.Reverse", ResourceBindingReverse.SourcePath, Create,
            ResourceBindingReverse.Bind, ResourceBindingReverse.Clear, LiveScriptResource.GainProperty, LiveScriptResource.LinkProperty);
        Script.RegisterBinding<Resource, ResourceBindingTests>("ResourceBinding.Plain", SourcePath, CreatePlain, Bind, Clear);
        _registered = true;
    }
    internal static void Run(PhysicsServer.Backend backend, bool separate)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        using var forward = ResourceLoader.Load<Script>(ResourceBindingForward.SourcePath);
        using var reverse = ResourceLoader.Load<Script>(ResourceBindingReverse.SourcePath);
        using var plainScript = ResourceLoader.Load<Script>(SourcePath);
        using (var plain = plainScript.New<Resource>())
        {
            Check(plain.GetType() == typeof(Resource) && plain.ResourceName == "bound", "The standard Resource role admits live code.");
            using var copy = plain.Duplicate(); Check(copy.GetScript() == plainScript && copy.ResourceName == "bound", "Native Resource duplication initializes its binding.");
        }
        using (var sourceAsset = new Script()) Reject<NotSupportedException>(() => sourceAsset.SetScript(plainScript));
        using var source = forward.New<LiveScriptResource>();
        Check(source.ConstructorSource == forward && source.Binds == 1 && source.Evaluate!(source) == 10, "Resource code binds once after the constructor.");
        source.Gain = 3;
        using (var copy = (LiveScriptResource)source.Duplicate())
            Check(copy.GetScript() == forward && copy.Gain == 3 && copy.Binds == 1 && copy.State is ResourceBindingForward && copy.State != source.State, "Copies install independent state before stored values.");
        using (var destination = reverse.New<LiveScriptResource>())
        {
            destination.CopyFromResource(source);
            Check(destination.GetScript() == reverse && destination.Binds == 1 && destination.Evaluate!(destination) == -3 && destination.Resets == 1, "Public data copy retains the target binding and resets ordinary transient state.");
        }
        using (var editedAsset = (Script)forward.Duplicate())
        using (var retained = editedAsset.New<LiveScriptResource>())
        {
            retained.Gain = 5; editedAsset.CopyFromResource(reverse);
            using var duplicate = (LiveScriptResource)retained.Duplicate();
            Check(retained.State is ResourceBindingForward && duplicate.State is ResourceBindingForward && duplicate.Evaluate!(duplicate) == 5,
                "Resource duplication retains the owner's immutable installed code despite borrowed asset metadata changes.");
        }
        var folder = Path.Combine(Path.GetTempPath(), "e2d-resource-binding-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try { Files(folder, source, forward, reverse); Scenes(folder, source, forward, reverse); }
        finally { Directory.Delete(folder, true); }
        using var world = new World(backend); var root = new SubViewport { World = world, Size = new(160, 120) }; var driver = new Driver { Controller = source }; root.AddChild(driver);
        using var tree = new SceneTree(root);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "The requested backend executes without fallback.");
        Measure(); Check(driver.LinearVelocity.X == 3, "Resource functions drive the actual physical owner.");
        var id = source.InstanceID; source.SetScript(reverse); source.Gain = 4; var x = driver.Position.X;
        Measure(); Check(driver.Position.X < x && driver.LinearVelocity.X == -4 && source.InstanceID == id, "Live Resource replacement changes real physics while retaining asset identity.");
        source.SetScript(null); Check(source.Evaluate is null && source.State is null, "Detachment retires typed Resource state.");
        source.SetScript(forward); source.Veto = true; var state = source.State; source.Dispose();
        Check(!source.IsDisposed && source.State == state, "Cancelled disposal preserves Resource code.");
        forward.Dispose(); Check(source.Evaluate!(source) == 10 && source.GetScript() == forward, "Borrowed asset disposal leaves installed code operational.");
        source.Dispose(); Check(source.IsDisposed && source.Evaluate is null && source.State is null, "Committed Resource disposal retires code exactly once.");
        Console.WriteLine($"Resource Script binding {backend}, separate={separate}: native/project owners, copies/cache/local scenes/child files, failures and 0/0 allocated frames passed.");

        void Measure()
        {
            for (var i = 0; i < 256; i++) Frame(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) Frame(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "Resource-driven process/physics frames require zero owner/all-thread bytes.");
        }
        void Frame() { tree.ProcessFrame(1d / 60); tree.PhysicsFrame(1d / 60); }
    }
    private static void Files(string folder, LiveScriptResource source, Script forward, Script reverse)
    {
        var path = Path.Combine(folder, "controller.e2dres"); ResourceSaver.Save(source, path, SaverFlags.BundleResources);
        using var target = ResourceLoader.Load<LiveScriptResource>(path);
        Check(target.Gain == 3 && target.Binds == 1 && target.Evaluate!(target) == 3 && target.GetScript() != forward, "Archives restore bound Resource state after initialization.");
        ChildProcess(path);
        var trace = new List<string>();
        target.PropertyListChanged += _ => trace.Add("properties"); target.ScriptChanged += _ => trace.Add("script"); target.Changed += _ => trace.Add("resource");
        var id = target.InstanceID; source.SetScript(reverse); source.Gain = 7; ResourceSaver.Save(source, path, SaverFlags.BundleResources);
        var same = ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.ReplaceDeep);
        Check(same == target && target.InstanceID == id && target.Evaluate!(target) == -7 && target.Binds == 2 && target.Clears == 1 && trace.SequenceEqual(["properties", "script", "resource"]), "Cache replacement switches code before stored state and publishes both source events before Changed.");
        Check(target.GetScript() is { IsDisposed: false }, "Committed file ownership retains the selected source.");
        target.FailBind = true; trace.Clear(); source.SetScript(forward); source.Gain = 8; ResourceSaver.Save(source, path, SaverFlags.BundleResources);
        Expect(target.BindError, () => ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.ReplaceDeep));
        Check(target.GetScript() is null && target.Evaluate is null && trace.SequenceEqual(["properties", "script", "resource"]), "Failed cache binding clears partial state and publishes its committed unbound result.");
        target.FailBind = false; ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.ReplaceDeep);
        target.FailClear = true; source.SetScript(reverse); source.Gain = 9; ResourceSaver.Save(source, path, SaverFlags.BundleResources);
        Expect(target.ClearError, () => ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.ReplaceDeep));
        Check(target.GetScript() is null && target.State is null, "Failed retirement leaves cached Resource unbound without installing replacement code.");
        target.FailClear = false; ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.ReplaceDeep);
        target.FailCopy = true; trace.Clear(); source.SetScript(forward); source.Gain = 6; ResourceSaver.Save(source, path, SaverFlags.BundleResources);
        Expect(target.CopyError, () => ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.ReplaceDeep));
        Check(target.Gain == 6 && target.Evaluate!(target) == 6 && target.GetScript() is { IsDisposed: false } && trace.SequenceEqual(["properties", "script", "resource"]), "Arbitrary stored-copy failure retains the committed code and new graph ownership.");
        target.FailCopy = false;
        var scriptError = new IOException("script observer"); var resourceError = new IOException("resource observer");
        Action<ElectronObject> failScript = _ => throw scriptError; Action<Resource> failResource = _ => throw resourceError;
        target.ScriptChanged += failScript; target.Changed += failResource;
        try
        {
            try { ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.ReplaceDeep); throw new InvalidOperationException("Missing observer failure."); }
            catch (AggregateException error) { Check(error.InnerExceptions.SequenceEqual([scriptError, resourceError]), "Source and Resource observer failures preserve order and identity."); }
        }
        finally { target.ScriptChanged -= failScript; target.Changed -= failResource; }
        var remap = Path.Combine(folder, "external.e2dres"); ResourceSaver.Save(source, remap);
        ResourceLoader.LoadThreadedRequest<LiveScriptResource>(remap, cacheMode: ResourceLoader.CacheMode.ReplaceDeep);
        using var remapped = ResourceLoader.LoadThreadedGet<LiveScriptResource>(remap);
        Check(remapped.GetScript() == forward && remapped.Binds == 1 && remapped.Gain == 6 && remapped.Evaluate!(remapped) == 6, "Threaded publication remaps source identity without rebinding or resetting prepared state.");
        source.Gain = 3;
    }
    private static void Scenes(string folder, LiveScriptResource source, Script forward, Script reverse)
    {
        source.ResourceLocalToScene = true; forward.ResourceLocalToScene = true;
        var root = new Node(); var driver = new Driver { Name = "driver", Controller = source }; root.AddChild(driver); driver.Owner = root;
        using var template = new PackedScene(); template.Pack(root); root.Dispose();
        using var first = template.Instantiate(); using var second = template.Instantiate();
        var one = ((Driver)first.Children[0]).Controller!; var two = ((Driver)second.Children[0]).Controller!;
        Check(one != source && two != one && one.State != two.State && one.Binds == 1 && two.Binds == 1 && one.Gain == 3 && one.GetScript() != two.GetScript(), "Scene-local owners and source wrappers have independent initialized state.");
        one.SetScript(reverse); one.Gain = 11;
        using (var overlay = new PackedScene())
        using (var outer = new Node())
        {
            first.Name = "nested"; outer.AddChild(first); first.Owner = outer; outer.SetEditableInstance(first, true);
            overlay.Pack(outer); using var overlaid = overlay.Instantiate(); var reused = ((Driver)overlaid.Children[0].Children[0]).Controller!;
            Check(reused.State is ResourceBindingReverse && reused.Evaluate!(reused) == -11 && reused.Binds == 2 && reused.Clears == 1,
                "A linked scene override reuses its local Resource identity and switches code before copying stored values.");
        }
        var path = Path.Combine(folder, "controller.e2dscene"); ResourceSaver.Save(template, path, SaverFlags.BundleResources);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var instance = loaded.Instantiate();
        var restored = ((Driver)instance.Children[0]).Controller!;
        Check(restored.Gain == 3 && restored.Evaluate!(restored) == 3 && restored.Binds == 1, "Scene files reconstruct executable bound local Resources.");
        source.ResourceLocalToScene = false; forward.ResourceLocalToScene = false;
    }
    internal static void Child(string path)
    {
        Register(); using var value = ResourceLoader.Load<LiveScriptResource>(path, ResourceLoader.CacheMode.IgnoreDeep);
        Check(value.Gain == 3 && value.Binds == 1 && value.Evaluate!(value) == 3, "Fresh-process resource code/state restoration.");
        Console.WriteLine("Resource binding child passed.");
    }
    private static void ChildProcess(string path)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location); start.ArgumentList.Add("--resource-binding-child"); start.ArgumentList.Add(path);
        start.Environment.Remove("ELECTRON2D_TEST_RESOURCE_SCRIPT_BINDING"); start.Environment.Remove("ELECTRON2D_TEST_THREADED_COLLIDER");
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Resource binding child."); }
        Check(child.ExitCode == 0 && output.Result.Contains("Resource binding child passed.", StringComparison.Ordinal), "Fresh child failed: " + output.Result + error.Result);
    }
    private sealed class Driver : RigidBody
    {
        private static readonly PropertyDescriptor<Driver, LiveScriptResource?> ControllerProperty = new(nameof(Controller), b => b.Controller, (b, v) => b.Controller = v, _ => null, stored: true);
        public LiveScriptResource? Controller { get; set; }
        internal Driver() { GravityScale = 0; CanSleep = false; LinearDampMode = DampMode.Replace; LinearDamp = 0; PhysicsProcessEnabled = true; }
        protected override void OnPhysicsProcess(double delta) { if (Controller?.Evaluate is { } evaluate) LinearVelocity = new(evaluate(Controller), 0); }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ControllerProperty);
        internal static Driver Create() => new();
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
    }
    private static void Expect(Exception expected, Action action) { try { action(); } catch (Exception error) when (ReferenceEquals(error, expected)) { return; } throw new InvalidOperationException("Missing original error: " + expected.Message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Missing " + typeof(T).Name); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
