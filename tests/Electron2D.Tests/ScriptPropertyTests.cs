using System.Diagnostics;
using System.Reflection;
using Electron2D;
using Path = System.IO.Path;

internal sealed class ScriptPropertyTests
{
    private static readonly string SourcePath = Source();
    private static bool _registered;
    internal static bool FailBind, FailClear;
    internal static readonly Exception BindError = new IOException("property bind"), ClearError = new IOException("property clear");
    private static ElectronObject CreateObject() => new();
    private static RigidBody CreateBody() => new() { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
    private static Resource CreateResource() => new();
    private sealed class AuthoredNode : Node
    {
        internal int Extra;
        internal static readonly PropertyDescriptor<AuthoredNode, int> ExtraProperty = new("Extra", o => o.Extra, (o, v) => o.Extra = v, _ => 0, stored: true);
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([ExtraProperty, ScriptPropertyBase.Value]);
    }
    internal static void InstallBody(RigidBody owner, int mode)
    {
        var state = ScriptPropertyBase.Install(owner, mode);
        owner.SetScriptCallbacks(new RigidBodyScriptCallbacks<RigidBody>
        {
            PhysicsProcess = (body, _) => body.LinearVelocity = new(state.Value * state.Mode, 0),
            Retired = _ => state.Retired++
        });
        if (FailBind) throw BindError;
    }
    private static void FaultBind(RigidBody owner) => InstallBody(owner, 1);
    private static void FaultClear(RigidBody owner) => ScriptPropertyBase.Clear(owner);
    private static void Register()
    {
        if (_registered) return;
        var values = new PropertyDescriptor[] { ScriptPropertyForward.Amount, ScriptPropertyForward.Target };
        Script.RegisterBinding<RigidBody, ScriptPropertyForward>("Properties.Forward", ScriptPropertyForward.SourcePath, CreateBody, ScriptPropertyForward.InstallBody, ScriptPropertyForward.ClearBody, values); values[0] = ScriptPropertyBase.Value;
        Script.RegisterBinding<RigidBody, ScriptPropertyReverse>("Properties.Reverse", ScriptPropertyReverse.SourcePath, CreateBody, ScriptPropertyReverse.InstallBody, ScriptPropertyReverse.ClearBody, ScriptPropertyReverse.Amount, ScriptPropertyReverse.Target);
        using (var script = ResourceLoader.Load<Script>(ScriptPropertyForward.SourcePath))
        using (var early = script.New<RigidBody>()) Check(script.GetScriptPropertyList().Length == 2 && Fields(early).Count() == 2, "Copied derived declarations exist before late base registration.");
        Script.RegisterBinding<ElectronObject, ScriptPropertyBase>("Properties.Base", ScriptPropertyBase.SourcePath, CreateObject, ScriptPropertyBase.Install, ScriptPropertyBase.Clear, ScriptPropertyBase.Value, ScriptPropertyBase.Payload, ScriptPropertyBase.Values);
        Script.RegisterBinding<Resource, ScriptPropertyResource>("Properties.Resource", ScriptPropertyResource.SourcePath, CreateResource, ScriptPropertyResource.InstallResource, ScriptPropertyResource.ClearResource, ScriptPropertyResource.Amount);
        Script.RegisterBinding<RigidBody, ScriptPropertyTests>("Properties.Collision", SourcePath, CreateBody, FaultBind, FaultClear,
            new PropertyDescriptor<RigidBody, string>(nameof(Node.Name), b => b.Name, (b, v) => b.Name = v, _ => "", stored: true));
        _registered = true;
    }
    private static IEnumerable<PropertyDescriptor> Fields(ElectronObject owner) => owner.GetPropertyList().Where(p => p.Name.StartsWith("Script", StringComparison.Ordinal));
    internal static void Child(string path)
    {
        Register(); using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var body = (RigidBody)scene.Instantiate(); VerifyRestored(body);
        Console.WriteLine("Native Script property fresh scene restored.");
    }
    internal static void Run(PhysicsServer.Backend backend, bool separate)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        using var forward = ResourceLoader.Load<Script>(ScriptPropertyForward.SourcePath); using var reverse = ResourceLoader.Load<Script>(ScriptPropertyReverse.SourcePath);
        using var basic = ResourceLoader.Load<Script>(ScriptPropertyBase.SourcePath); using var resourceCode = ResourceLoader.Load<Script>(ScriptPropertyResource.SourcePath); using var collision = ResourceLoader.Load<Script>(SourcePath);
        Check(forward.GetScriptPropertyList().Length == 4 && forward.GetScriptPropertyList()[0] == ScriptPropertyForward.Amount, "Late compatible base fields follow derived name priority.");
        using (var plain = basic.New<ElectronObject>())
        {
            var snapshot = plain.GetPropertyList(); ScriptPropertyBase.Value.SetValue(plain, 7); plain.RevertProperty(ScriptPropertyBase.Value); Check(ScriptPropertyBase.Value.GetValue(plain) == 1 && plain.GetType() == typeof(ElectronObject), "Plain native owner exposes typed defaults/revert.");
            plain.SetScript(null); Check(!Fields(plain).Any() && snapshot.Any(p => p == ScriptPropertyBase.Value), "Retirement removes later discovery without mutating prior snapshots.");
        }
        using (var node = new Node())
        using (var authored = new AuthoredNode { Extra = 9 })
        {
            node.SetScript(basic); authored.SetScript(basic);
            Check(Fields(node).Count() == 3 && authored.GetPropertyList().Single(p => p.Name == "Extra") == AuthoredNode.ExtraProperty && authored.GetPropertyList().Count(p => p.Name == "ScriptValue") == 1, "Native Node schemas coexist with ordinary CLR fields and shared descriptor tokens.");
            authored.SetScript(null); Check(authored.Extra == 9 && authored.GetPropertyList().Contains(AuthoredNode.ExtraProperty), "Retirement preserves ordinary CLR fields.");
        }
        using var payload = resourceCode.New<Resource>(); payload.ResourceName = "payload"; ScriptPropertyResource.Amount.SetValue(payload, 27); ScriptPropertyBase.Values.SetValue(payload, [2, 4]);
        using (var copy = payload.Duplicate(true))
            Check(copy.GetType() == typeof(Resource) && ScriptPropertyResource.Amount.GetValue(copy) == 27 && !ReferenceEquals(ScriptPropertyBase.Get(copy), ScriptPropertyBase.Get(payload)) && !ReferenceEquals(ScriptPropertyBase.Values.GetValue(copy), ScriptPropertyBase.Values.GetValue(payload)), "Resource copy binds independent state before stored array/value restoration.");
        ScriptPropertyBase.Payload.SetValue(payload, payload);
        using (var cyclic = payload.Duplicate(true)) Check(ReferenceEquals(ScriptPropertyBase.Payload.GetValue(cyclic), cyclic), "Injected resource edges preserve deep-copy cycles.");
        ScriptPropertyBase.Payload.SetValue(payload, null);
        using (var shallow = resourceCode.New<Resource>())
        { shallow.CopyFromResource(payload); Check(ScriptPropertyResource.Amount.GetValue(shallow) == 27 && ReferenceEquals(ScriptPropertyBase.Values.GetValue(shallow), ScriptPropertyBase.Values.GetValue(payload)), "Public shallow copy retains target code and collection aliasing."); }
        using (var different = new Resource())
        { different.SetScript(basic); different.CopyFromResource(payload); Check(ReferenceEquals(different.GetScript(), basic) && ScriptPropertyBase.Value.GetValue(different) == 27 && ReferenceEquals(ScriptPropertyBase.Values.GetValue(different), ScriptPropertyBase.Values.GetValue(payload)), "Copy matches typed names while retaining a different target binding."); }
        using var world = new World(backend); var root = new SubViewport { World = world }; using var tree = new SceneTree(root); var body = forward.New<RigidBody>(); body.Name = "Body";
        var child = new Node { Name = "Target" }; body.AddChild(child); child.Owner = body;
        using var circle = new CircleShape(); var shape = new CollisionShape { Shape = circle }; body.AddChild(shape); shape.Owner = body; root.AddChild(body); tree.ProcessFrame(0);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Requested property profile solver is selected.");
        var id = body.InstanceID; var rid = body.GetRID(); var snapshotBody = body.GetPropertyList();
        Check(snapshotBody.Single(p => p.Name == "ScriptValue") == ScriptPropertyForward.Amount && ScriptPropertyForward.Amount.Hint == PropertyHint.Range && forward.GetPropertyDefaultValue(ScriptPropertyForward.Amount, body) == 3, "Native owner discovery uses declared typed hint/default and derived identity.");
        // Scene/backend velocity conversion permits two float ULPs near five or six scene units per second.
        ScriptPropertyForward.Amount.SetValue(body, 5); tree.PhysicsFrame(1d / 60); Check(MathF.Abs(body.LinearVelocity.X - 5) <= 1e-6f && body.Position.X > 0, $"Native script property drives physical motion: ready={body.IsNodeReady}, enabled={body.PhysicsProcessEnabled}, value={ScriptPropertyBase.Get(body).Value}, mode={ScriptPropertyBase.Get(body).Mode}, velocity={body.LinearVelocity}, position={body.Position}.");
        Reject<ArgumentOutOfRangeException>(() => ScriptPropertyForward.Amount.SetValue(body, -1));
        Reject<InvalidOperationException>(() => Task.Run(() => ScriptPropertyForward.Amount.SetValue(body, 8)).GetAwaiter().GetResult()); Check(ScriptPropertyForward.Amount.GetValue(body) == 5, "Authored property guard rejects foreign mutation before state change.");
        var retired = ScriptPropertyBase.Get(body); body.SetScript(reverse); Check(retired.Clears == 1 && retired.Retired == 1 && body.GetPropertyList().Single(p => p.Name == "ScriptValue") == ScriptPropertyReverse.Amount && snapshotBody.Contains(ScriptPropertyForward.Amount), "Source replacement retires old state and switches schema on the same owner.");
        ScriptPropertyReverse.Amount.SetValue(body, 6); tree.PhysicsFrame(1d / 60); Check(MathF.Abs(body.LinearVelocity.X + 6) <= 1e-6f && body.InstanceID == id && body.GetRID() == rid, "Replacement keeps native identity and changes physical property behavior.");
        body.SetScript(forward); var alive = ScriptPropertyBase.Get(body); FailBind = true;
        try { Expect(BindError, () => body.SetScript(reverse)); } finally { FailBind = false; }
        Check(alive.Clears == 1 && alive.Retired == 1 && body.GetScript() is null && !Fields(body).Any(), "Partial bind failure removes its entire property/callback overlay.");
        body.SetScript(forward); alive = ScriptPropertyBase.Get(body); FailClear = true;
        try { Expect(ClearError, () => body.SetScript(reverse)); } finally { FailClear = false; }
        Check(alive.Clears == 1 && alive.Retired == 1 && body.GetScript() is null && !Fields(body).Any(), "Clear failure commits retirement and removes discovery.");
        Reject<InvalidOperationException>(() => body.SetScript(collision)); Check(body.GetScript() is null && !Fields(body).Any(), "Native-name schema collision fails installation and cleans partial code.");
        body.SetScript(forward); ScriptPropertyForward.Amount.SetValue(body, 41); ScriptPropertyForward.Target.SetValue(body, child); ScriptPropertyBase.Payload.SetValue(body, payload); ScriptPropertyBase.Values.SetValue(body, [1, 3, 5]);
        using (var copy = (RigidBody)body.Duplicate())
            Check(ScriptPropertyForward.Amount.GetValue(copy) == 41 && ReferenceEquals(ScriptPropertyForward.Target.GetValue(copy), copy.GetNode("Target")) && ReferenceEquals(ScriptPropertyBase.Payload.GetValue(copy), payload) && !ReferenceEquals(ScriptPropertyBase.Values.GetValue(copy), ScriptPropertyBase.Values.GetValue(body)), "Node copies restore typed values/arrays and remap node references with existing Resource policy.");
        using (var stripped = (RigidBody)body.Duplicate(Node.DuplicateFlags.Groups))
            Check(stripped.GetType() == typeof(RigidBody) && stripped.GetScript() is null && !Fields(stripped).Any() && stripped.GetChildCount() == body.GetChildCount(), "Without Scripts, injected values/references are omitted while native hierarchy remains.");
        Stored(body, payload, basic);
        using (var edited = ResourceLoader.Load<Script>(ScriptPropertyForward.SourcePath).Duplicate() as Script)
        using (var owner = edited!.New<RigidBody>())
        {
            edited.CopyFromResource(reverse); edited.Dispose();
            Check(owner.GetPropertyList().Single(p => p.Name == "ScriptValue") == ScriptPropertyForward.Amount, "Installed schema is independent of source metadata replacement/disposal.");
        }
        Measure(); var finalState = ScriptPropertyBase.Get(body); body.Dispose(); Check(finalState.Clears == 1 && finalState.Retired == 1 && alive.Clears == 1 && alive.Retired == 1, "Disposal releases current state once without repeating previous retirement.");
        Console.WriteLine($"Native Script properties {backend}, separate={separate}: typed native schema, defaults/lifetime, copy/archive/cache and 128 zero-allocation full frames passed.");
        void Measure()
        {
            void Frame() { ScriptPropertyForward.Amount.SetValue(body, 5); tree.ProcessFrame(1d / 60); tree.PhysicsFrame(1d / 60); Check(ScriptPropertyForward.Amount.GetValue(body) == 5 && MathF.Abs(body.LinearVelocity.X - 5) <= 1e-6f, "Typed field and physical state agree."); }
            for (var i = 0; i < 256; i++) Frame(); AllocationWarmupCheck.Collect(); for (var i = 0; i < 128; i++) Frame(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(); Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all, "Prepared typed state/full physical frames allocate zero on owner/all threads.");
        }
    }
    private static void Stored(RigidBody body, Resource payload, Script basic)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-script-properties-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var scene = new PackedScene(); scene.Pack(body); var path = Path.Combine(folder, "scene.e2dscene"); ResourceSaver.Save(scene, path, SaverFlags.BundleResources);
            using (var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep)) using (var restored = (RigidBody)loaded.Instantiate()) VerifyRestored(restored);
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location); start.ArgumentList.Add("--script-property-child"); start.ArgumentList.Add(path);
            foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
            start.Environment["SDL_VIDEODRIVER"] = "dummy"; start.Environment["VK_DRIVER_FILES"] = start.Environment["VK_ICD_FILENAMES"] = "/nonexistent/e2d-properties.json";
            using (var process = Process.Start(start)!)
            {
                var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
                try { if (!process.WaitForExit(30000)) throw new TimeoutException("Property archive worker."); Check(process.ExitCode == 0 && output.Result.Contains("Native Script property fresh scene restored.", StringComparison.Ordinal), output.Result + errors.Result); }
                finally { if (!process.HasExited) { process.Kill(true); process.WaitForExit(5000); } }
            }
            var resourcePath = Path.Combine(folder, "resource.e2dres"); ResourceSaver.Save(payload, resourcePath, SaverFlags.BundleResources);
            var cached = ResourceLoader.Load<Resource>(resourcePath); var old = ScriptPropertyBase.Get(cached);
            using var replacement = new Resource(); replacement.SetScript(basic); ScriptPropertyBase.Value.SetValue(replacement, 44); ResourceSaver.Save(replacement, resourcePath, SaverFlags.BundleResources);
            var same = ResourceLoader.Load<Resource>(resourcePath, ResourceLoader.CacheMode.Replace);
            Check(ReferenceEquals(same, cached) && old.Clears == 1 && cached.GetPropertyList().Single(p => p.Name == "ScriptValue") == ScriptPropertyBase.Value && ScriptPropertyBase.Value.GetValue(cached) == 44, "Cache replacement switches native property schema before restoring stored values on the same Resource."); cached.Dispose();
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void VerifyRestored(RigidBody body)
    {
        Check(body.GetType() == typeof(RigidBody) && ScriptPropertyForward.Amount.GetValue(body) == 41 && ReferenceEquals(ScriptPropertyForward.Target.GetValue(body), body.GetNode("Target")) && ScriptPropertyBase.Values.GetValue(body).AsSpan().SequenceEqual([1f, 3f, 5f]), "Fresh scene restores native typed scalar/array/node reference.");
        var payload = ScriptPropertyBase.Payload.GetValue(body); Check(payload?.GetType() == typeof(Resource) && ScriptPropertyResource.Amount.GetValue(payload) == 27 && ScriptPropertyBase.Values.GetValue(payload).AsSpan().SequenceEqual([2f, 4f]), "Bundled native Resource restores its independently bound schema/state.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Expect(Exception expected, Action action) { try { action(); } catch (Exception error) when (ReferenceEquals(expected, error)) { return; } throw new InvalidOperationException("Missing original failure."); }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
