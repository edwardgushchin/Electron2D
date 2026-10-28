using System.Diagnostics;
using System.Reflection;
using Electron2D;
using Path = System.IO.Path;

internal static class ObjectCallbackTests
{
    private static bool _registered;
    private static ElectronObject CreateObject() => new();
    private static Resource CreateResource() => new() { ResourceLocalToScene = true };
    private static RigidBody CreateBody() => new() { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
    private static void Register()
    {
        if (_registered) return;
        Script.RegisterBinding<ElectronObject, ObjectCallbackCode>("ObjectCallbacks.Base", ObjectCallbackCode.SourcePath, CreateObject, ObjectCallbackCode.Bind, ObjectCallbackCode.Clear, ObjectCallbackCode.Value, ObjectCallbackCode.Controller);
        Script.RegisterBinding<RigidBody, ObjectCallbackBodyCode>("ObjectCallbacks.Body", ObjectCallbackBodyCode.SourcePath, CreateBody, ObjectCallbackBodyCode.BindBody, ObjectCallbackBodyCode.ClearBody);
        Script.RegisterBinding<Resource, ObjectCallbackResourceCode>("ObjectCallbacks.Resource", ObjectCallbackResourceCode.SourcePath, CreateResource, ObjectCallbackResourceCode.BindResource, ObjectCallbackResourceCode.ClearResource);
        _registered = true;
    }
    internal static void Child(string path)
    {
        Register(); using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var body = (RigidBody)scene.Instantiate(); Verify(body);
        Console.WriteLine("Object Script callbacks fresh scene restored.");
    }
    internal static void Run(PhysicsServer.Backend backend, bool separate)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        using var code = ResourceLoader.Load<Script>(ObjectCallbackCode.SourcePath); using var bodyCode = ResourceLoader.Load<Script>(ObjectCallbackBodyCode.SourcePath); using var resourceCode = ResourceLoader.Load<Script>(ObjectCallbackResourceCode.SourcePath);
        using var plain = code.New<ElectronObject>(); var state = ObjectCallbackCode.Get(plain);
        foreach (var input in new InputEvent[] { new InputEventKey(), new InputEventShortcut() })
        {
            using (input) { input.SetScript(code); Check(input.ToString() == "object-code", "Input/resource native formatting honors the installed representation."); }
            Reject<ObjectDisposedException>(() => input.ToString());
        }
        Check(plain.GetType() == typeof(ElectronObject) && plain.ToString() == "object-code" && plain.GetPropertyList().Single(p => p.Name == "LiveValue") == state.MetadataValue && !plain.GetPropertyList().Any(p => p.Name == nameof(ElectronObject.TranslationDomain)), "Actual native object uses installed property discovery, replacement metadata and string representation.");
        state.Hide = false; Check(plain.GetPropertyList().Any(p => p.Name == nameof(ElectronObject.TranslationDomain)), "Later validation observes current owner state.");
        ObjectCallbackCode.Value.SetValue(plain, 9); Check(plain.PropertyCanRevert(state.MetadataValue), "Installed revert policy receives the typed descriptor."); state.AllowRevert = false; Check(!plain.PropertyCanRevert(state.MetadataValue), "Installed policy can reject revert availability."); state.AllowRevert = true; plain.RevertProperty(state.MetadataValue); Check(state.Value == 3, "Typed descriptor retains actual revert values/writes.");
        plain.SetBlockSignals(true); var before = state.Notifications; plain.Notify(400); plain.Notify(401, true); Check(state.Notifications == before + 2, "Signals do not suppress direct object notifications.");
        state.Cancel = true; plain.Dispose(); Check(!plain.IsDisposed && state.Clears == 0 && state.Retired == 0 && plain.ToString() == "object-code", "Cancelled native pre-delete retains the complete callback set."); state.Cancel = false;
        Reject<InvalidOperationException>(() => plain.SetScriptCallbacks(new ElectronObjectScriptCallbacks<ElectronObject>()));
        ObjectCallbackCode.FailValidation = true; try { Expect(ObjectCallbackCode.Error, () => plain.GetPropertyList()); } finally { ObjectCallbackCode.FailValidation = false; }
        ObjectCallbackCode.FailDiscovery = true; try { Expect(ObjectCallbackCode.Error, () => plain.GetPropertyList()); } finally { ObjectCallbackCode.FailDiscovery = false; }
        Check(ReferenceEquals(plain.GetScript(), code), "Tooling errors do not retire valid installed code.");
        Failures(code);
        using var world = new World(backend); var viewport = new SubViewport { World = world }; using var tree = new SceneTree(viewport);
        using (var objectNode = new Node())
        {
            objectNode.SetScript(code); var objectState = ObjectCallbackCode.Get(objectNode); var sourceAtExit = false;
            objectNode.TreeExiting += node => sourceAtExit = ReferenceEquals(node.GetScript(), code) && objectState.Clears == 0 && objectState.Retired == 0;
            viewport.AddChild(objectNode); objectNode.Dispose();
            Check(sourceAtExit && objectState.Clears == 1 && objectState.Retired == 1, "A Node with only common object callbacks exits its tree before retiring source/state.");
        }
        using var controller = resourceCode.New<Resource>(); ObjectCallbackCode.Value.SetValue(controller, 11);
        var body = bodyCode.New<RigidBody>(); body.Name = "Body"; ObjectCallbackCode.Value.SetValue(body, 7); ObjectCallbackCode.Controller.SetValue(body, controller);
        using var circle = new CircleShape(); var shape = new CollisionShape { Shape = circle }; body.AddChild(shape); shape.Owner = body; viewport.AddChild(body); tree.ProcessFrame(0);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Selected callback profile keeps the requested numerical backend.");
        var id = body.InstanceID; var rid = body.GetRID(); var bodyState = ObjectCallbackCode.Get(body);
        using (var copy = (RigidBody)body.Duplicate()) Check(copy.ToString() == "body-code" && ObjectCallbackCode.Value.GetValue(copy) == 7 && ReferenceEquals(ObjectCallbackCode.Controller.GetValue(copy), controller), "Detached node copying restores callbacks and typed values while retaining ordinary borrowed resources.");
        using (var stripped = (RigidBody)body.Duplicate(Node.DuplicateFlags.Groups)) Check(stripped.GetScript() is null && !stripped.GetPropertyList().Any(p => p.Name == "LiveValue") && stripped.ToString().StartsWith("RigidBody#", StringComparison.Ordinal), "Copies without Scripts omit callback properties and string behavior.");
        Stored(body);
        void Frame() { plain.Notify(410); controller.Notify(411); body.Notify(412); Check(plain.ToString() == "object-code" && body.ToString() == "body-code" && body.PropertyCanRevert(bodyState.MetadataValue), "Prepared callback state."); tree.ProcessFrame(1d / 60); tree.PhysicsFrame(1d / 60); Check(MathF.Abs(body.LinearVelocity.X - 7) <= 1e-6f, "Callback-driven scene/backend conversion permits two float ULP near seven units per second."); }
        for (var i = 0; i < 256; i++) Frame(); AllocationWarmupCheck.Collect(); for (var i = 0; i < 128; i++) Frame();
        var discoveries = bodyState.Discoveries; var validations = bodyState.Validations; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(); Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all, "128 complete callback/physics frames allocate zero on owner/all threads.");
        Check(bodyState.Discoveries == discoveries && bodyState.Validations == validations && body.InstanceID == id && body.GetRID() == rid, "Prepared callbacks do not rediscover schemas or replace native identity.");
        plain.SetScript(null); Check(state.Clears == 1 && state.Retired == 1 && plain.ToString().StartsWith("ElectronObject#", StringComparison.Ordinal) && !plain.GetPropertyList().Any(p => p.Name == "LiveValue"), "Retirement removes every common callback and property overlay.");
        body.Dispose(); Check(bodyState.Clears == 1 && bodyState.Retired == 1, "Native node and object callback state retire once.");
        ObjectCallbackCode.Created.Clear(); Console.WriteLine($"Object Script callbacks {backend}, separate={separate}: native object metadata/notifications, local Resource setup, copies/fresh files, failures and 128 zero-allocation full frames passed.");
    }
    private static void Failures(Script code)
    {
        using var target = new ElectronObject();
        foreach (var validation in new[] { false, true })
        {
            if (validation) ObjectCallbackCode.FailValidation = true; else ObjectCallbackCode.FailBind = true;
            try { Expect(ObjectCallbackCode.Error, () => target.SetScript(code)); } finally { ObjectCallbackCode.FailValidation = ObjectCallbackCode.FailBind = false; }
            var state = ObjectCallbackCode.Created[^1]; Check(target.GetScript() is null && state.Clears == 1 && state.Retired == 1 && !target.GetPropertyList().Any(p => p.Name == "LiveValue"), "Bind/schema errors retire installed common slots and state.");
        }
        ObjectCallbackCode.Collision = true;
        try { Reject<InvalidOperationException>(() => target.SetScript(code)); } finally { ObjectCallbackCode.Collision = false; }
        Check(target.GetScript() is null && ObjectCallbackCode.Created[^1].Clears == 1 && ObjectCallbackCode.Created[^1].Retired == 1, "Hiding native metadata cannot admit a different script field under its reserved name.");
        target.SetScript(code); var current = ObjectCallbackCode.Get(target); ObjectCallbackCode.FailClear = ObjectCallbackCode.FailRetired = true;
        try { try { target.SetScript(null); throw new InvalidOperationException("Missing clear/retired errors."); } catch (AggregateException e) { Check(e.InnerExceptions.Contains(ObjectCallbackCode.ClearError) && e.InnerExceptions.Contains(ObjectCallbackCode.RetiredError), "Independent cleanup errors retain identity/order."); } }
        finally { ObjectCallbackCode.FailClear = ObjectCallbackCode.FailRetired = false; }
        Check(current.Clears == 1 && current.Retired == 1 && target.GetScript() is null && target.ToString().StartsWith("ElectronObject#", StringComparison.Ordinal), "Cleanup failures commit complete retirement.");
        var reused = new ElectronObjectScriptCallbacks<ElectronObject>(); ObjectCallbackCode.NextCallbacks = reused;
        try { using var first = code.New<ElectronObject>(); Reject<InvalidOperationException>(() => target.SetScript(code)); Check(ReferenceEquals(first.GetScript(), code) && target.GetScript() is null, "A reused set cannot steal another native owner's callbacks."); }
        finally { ObjectCallbackCode.NextCallbacks = null; }
        using var asset = (Script)code.Duplicate(); using var owner = asset.New<ElectronObject>(); asset.Dispose(); Check(owner.ToString() == "object-code" && owner.GetPropertyList().Any(p => p.Name == "LiveValue"), "Borrowed asset disposal retains installed object callbacks.");
    }
    private static void Verify(RigidBody body)
    {
        var controller = ObjectCallbackCode.Controller.GetValue(body)!; var state = ObjectCallbackCode.Get(controller);
        Check(body.ToString() == "body-code" && ObjectCallbackCode.Value.GetValue(body) == 7 && controller.GetType() == typeof(Resource) && controller.ToString() == "resource-code" && state.Value == 11 && state.Setups == 1 && state.SetupValue == 11 && ReferenceEquals(state.Scene, body), $"Local native Resource callback follows graph restoration: body={body}, value={ObjectCallbackCode.Value.GetValue(body)}, resource={controller}, value={state.Value}, setups={state.Setups}, setupValue={state.SetupValue}, sameScene={ReferenceEquals(state.Scene, body)}.");
    }
    private static void Stored(RigidBody body)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-object-callback-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var scene = new PackedScene(); scene.Pack(body); var path = Path.Combine(folder, "scene.e2dscene"); ResourceSaver.Save(scene, path, SaverFlags.BundleResources);
            using (var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep))
            {
                using (var copy = (RigidBody)loaded.Instantiate()) Verify(copy);
                var count = ObjectCallbackCode.Created.Count; ObjectCallbackResourceCode.FailSetup = true;
                try { Expect(ObjectCallbackCode.Error, () => loaded.Instantiate(), aggregated: true); } finally { ObjectCallbackResourceCode.FailSetup = false; }
                Check(ObjectCallbackCode.Created.Skip(count).All(s => s.Clears == 1 && s.Retired == 1), "Failed local setup releases the entire acquired callback graph.");
            }
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location); start.ArgumentList.Add("--object-callback-child"); start.ArgumentList.Add(path);
            foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
            start.Environment["SDL_VIDEODRIVER"] = "dummy"; start.Environment["VK_DRIVER_FILES"] = start.Environment["VK_ICD_FILENAMES"] = "/nonexistent/e2d-object-callbacks.json";
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            try { if (!process.WaitForExit(30000)) throw new TimeoutException("Object callback archive worker."); Check(process.ExitCode == 0 && output.Result.Contains("Object Script callbacks fresh scene restored.", StringComparison.Ordinal), output.Result + error.Result); }
            finally { if (!process.HasExited) { process.Kill(true); process.WaitForExit(5000); } }
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Expect(Exception expected, Action action, bool aggregated = false) { try { action(); } catch (Exception e) when (ReferenceEquals(e, expected) || aggregated && e is AggregateException a && a.Flatten().InnerExceptions is { Count: 1 } errors && ReferenceEquals(errors[0], expected)) { return; } throw new InvalidOperationException("Missing original callback error."); }
}
