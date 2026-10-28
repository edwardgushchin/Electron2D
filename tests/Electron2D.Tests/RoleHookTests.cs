using System.Diagnostics;
using System.Reflection;
using Electron2D;
using Path = System.IO.Path;

internal static class RoleHookTests
{
    private static bool _registered;
    private static Resource CreateResource() => new();
    private static RigidBody CreateBody() => new() { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
    private static Button CreateButton() => new() { Text = "Role button", Size = new(160, 32), FocusMode = FocusMode.Accessibility };
    internal sealed class NativeResource : Resource
    {
        internal bool ResetVisited, FailReset;
        internal string PathVisited = "";
        internal int PathChanges;
        protected override void OnResetState() { ResetVisited = true; if (FailReset) throw RoleResourceCode.Error; }
        protected override void OnPathCacheSet(string path) => PathVisited = path;
        protected override void OnResourcePathChanged(string path) => PathChanges++;
    }
    private sealed class NativeBody : RigidBody
    {
        protected override string[] OnGetAccessibilityConfigurationWarnings() => [.. base.OnGetAccessibilityConfigurationWarnings(), "native accessibility"];
        internal RID SelectFocus() => base.OnGetFocusedAccessibilityElement();
    }
    private static void Register()
    {
        if (_registered) return;
        Script.RegisterBinding<Resource, RoleResourceCode>("RoleHooks.Resource", RoleResourceCode.SourcePath, CreateResource, RoleResourceCode.Bind, RoleResourceCode.Clear, RoleResourceCode.Value);
        Script.RegisterBinding<RigidBody, RoleBodyCode>("RoleHooks.Body", RoleBodyCode.SourcePath, CreateBody, RoleBodyCode.Bind, RoleBodyCode.Clear, RoleBodyCode.Speed, RoleBodyCode.Controller);
        Script.RegisterBinding<Button, RoleButtonCode>("RoleHooks.Button", RoleButtonCode.SourcePath, CreateButton, RoleButtonCode.Bind, RoleButtonCode.Clear);
        _registered = true;
    }
    internal static void Child(string path)
    {
        Register(); using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var body = (RigidBody)scene.Instantiate();
        Check(RoleBodyCode.Speed.GetValue(body) == 13 && body.GetConfigurationWarnings().AsSpan().SequenceEqual(["role accessibility", "role warning"]), "Fresh child restores typed body role queries.");
        Console.WriteLine("Role hook fresh scene restored.");
    }
    internal static void Run(PhysicsServer.Backend backend, bool separate)
    {
        Register(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        using var code = ResourceLoader.Load<Script>(RoleResourceCode.SourcePath); using var bodyCode = ResourceLoader.Load<Script>(RoleBodyCode.SourcePath);
        using var geometry = new CircleShape(); var canonical = geometry.GetRID(); geometry.SetScript(code); var geometryState = RoleResourceCode.Get(geometry);
        Check(geometry.GetRID() == canonical && geometryState.RIDCalls == 0, "Native geometry identity precedes the base Resource RID hook.");
        using var resource = code.New<Resource>(); var state = RoleResourceCode.Get(resource); state.Identity = canonical;
        Check(resource.GetType() == typeof(Resource) && resource.GetRID() == canonical, "Actual native base Resource returns a borrowed RID.");
        state.FailRID = true; try { Expect(RoleResourceCode.Error, () => resource.GetRID()); } finally { state.FailRID = false; }
        using (var native = new NativeResource())
        {
            native.SetScript(code); var observed = RoleResourceCode.Get(native); native.ResetState(); native.SetPathCache("raw"); native.SetPathCache("raw");
            Check(native.ResetVisited && observed.Resets == 1 && observed.Transient == 0 && observed.Paths == 2 && native.PathChanges == 2, "Native reset/path hooks precede Script callbacks; equal raw paths still deliver.");
            observed.FailPath = true; try { Expect(RoleResourceCode.Error, () => native.SetPathCache("committed")); } finally { observed.FailPath = false; }
            Check(native.ResourcePath == "committed" && native.PathChanges == 3, "A Script path error retains committed metadata and the general path hook.");
            native.FailReset = true; var resets = observed.Resets; try { Expect(RoleResourceCode.Error, native.ResetState); } finally { native.FailReset = false; }
            Check(observed.Resets == resets, "A native reset error stops the following Script hook.");
        }
        RoleResourceCode.Value.SetValue(resource, 11); state.Transient = 17; state.FailReset = true;
        try { Expect(RoleResourceCode.Error, resource.ResetState); } finally { state.FailReset = false; }
        Check(state.Transient == 17, "A throwing Script reset retains its uncommitted state.");
        using var source = code.New<Resource>(); RoleResourceCode.Value.SetValue(source, 21); resource.CopyFromResource(source);
        Check(state.Resets == 2 && state.ValueAtReset == 11 && state.Transient == 0 && state.Value == 21 && RoleResourceCode.Get(source).Resets == 0, "Public data copying resets the target before stored restoration.");
        Cache(resource);
        LocalCopies(code, bodyCode);
        using (var native = new NativeBody())
        {
            native.SetScript(bodyCode); var data = RoleBodyCode.Get(native); data.Focus = canonical;
            Check(native.SelectFocus() == canonical && native.GetConfigurationWarnings().AsSpan().SequenceEqual(["role accessibility", "native accessibility", "role warning"]), "Protected native warning/focus chains reach the installed hooks.");
        }
        using var world = new World(backend); var viewport = new SubViewport { World = world }; using var tree = new SceneTree(viewport); var body = bodyCode.New<RigidBody>(); body.Name = "Body"; viewport.AddChild(body); tree.ProcessFrame(0);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Role hook profile uses the requested solver."); var bodyState = RoleBodyCode.Get(body);
        var queries = bodyState.Queries; tree.EditedSceneRoot = body; var changes = 0; tree.NodeConfigurationWarningChanged += (_, _) => changes++; body.UpdateConfigurationWarnings();
        Check(changes == 1 && bodyState.Queries == queries, "Warning refresh does not implicitly evaluate callbacks.");
        Check(body.GetConfigurationWarnings().AsSpan().SequenceEqual(["role accessibility", "role warning"]), "Actual native body combines typed role warnings.");
        Task.Run(() => Reject<InvalidOperationException>(() => body.GetConfigurationWarnings())).GetAwaiter().GetResult();
        bodyState.Fail = true; try { Expect(RoleBodyCode.Error, () => body.GetConfigurationWarnings()); } finally { bodyState.Fail = false; }
        RoleBodyCode.Speed.SetValue(body, 13); Stored(body);
        void Frame() { Check(resource.GetRID() == canonical && geometry.GetRID() == canonical, "Prepared borrowed/canonical resource identity."); tree.ProcessFrame(1d / 60); tree.PhysicsFrame(1d / 60); Check(MathF.Abs(body.LinearVelocity.X - 13) <= 2e-6f, "Scene/backend velocity conversion permits two float ULP near thirteen units per second."); }
        for (var i = 0; i < 256; i++) Frame(); AllocationWarmupCheck.Collect(); for (var i = 0; i < 128; i++) Frame(); queries = bodyState.Queries;
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); for (var i = 0; i < 128; i++) Frame();
        Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all && bodyState.Queries == queries, "128 full warmed physical/RID frames allocate zero without querying warnings.");
        body.SetScript(null); resource.SetScript(null); geometry.SetScript(null);
        Check(bodyState.Clears == 1 && bodyState.Retired == 1 && body.GetConfigurationWarnings().Length == 0 && state.Clears == 1 && state.Retired == 1 && !resource.GetRID().IsValid() && geometry.GetRID() == canonical, "Retirement restores native defaults without freeing borrowed identities.");
        RoleResourceCode.LastReset = null;
        Console.WriteLine($"Role Script hooks {backend}, separate={separate}: RID/reset/path, node queries, copy/cache/fresh files, lifecycle/errors and 128 zero-allocation full frames passed.");
    }
    private static void Cache(Resource resource)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-role-cache-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "resource.e2dres"); ResourceSaver.Save(resource, path, SaverFlags.BundleResources); using var loaded = ResourceLoader.Load<Resource>(path); var state = RoleResourceCode.Get(loaded); state.Transient = 90;
            RoleResourceCode.Value.SetValue(resource, 31); ResourceSaver.Save(resource, path, SaverFlags.BundleResources); var same = ResourceLoader.Load<Resource>(path, ResourceLoader.CacheMode.Replace);
            var after = RoleResourceCode.Get(loaded);
            Check(ReferenceEquals(loaded, same) && state.Resets == 1 && state.ValueAtReset == 21 && after.Value == 31 && (ReferenceEquals(after, state) ? state.Transient == 0 : state.Clears == 1 && state.Retired == 1 && after.Resets == 0 && after.Transient == 44), $"File replacement resets its existing target before retaining/replacing code: same={ReferenceEquals(loaded, same)}, resets={state.Resets}, observed={state.ValueAtReset}, value={after.Value}, reusedState={ReferenceEquals(after, state)}, clears={state.Clears}, retired={state.Retired}, newResets={after.Resets}, transient={after.Transient}.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void LocalCopies(Script resourceCode, Script bodyCode)
    {
        using var local = resourceCode.New<Resource>(); local.ResourceLocalToScene = true; RoleResourceCode.Value.SetValue(local, 33);
        using var root = bodyCode.New<RigidBody>(); RoleBodyCode.Controller.SetValue(root, local); using var template = new PackedScene(); template.Pack(root);
        using var first = template.Instantiate(); var resource = RoleBodyCode.Controller.GetValue((RigidBody)first)!; RoleResourceCode.Value.SetValue(resource, 77);
        using var outer = new Node(); first.Name = "nested"; outer.AddChild(first); first.Owner = outer; outer.SetEditableInstance(first, true);
        using var overlay = new PackedScene(); overlay.Pack(outer); RoleResourceCode.LastReset = null;
        using var instance = overlay.Instantiate(); var reused = RoleBodyCode.Controller.GetValue((RigidBody)instance.GetChild(0))!;
        Check(ReferenceEquals(reused, RoleResourceCode.LastReset) && RoleResourceCode.Value.GetValue(reused) == 77, "Linked local-scene reuse resets the existing Resource identity before restoring override data.");
    }
    private static void Stored(RigidBody body)
    {
        var folder = Path.Combine(Path.GetTempPath(), "e2d-role-scene-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var scene = new PackedScene(); scene.Pack(body); var path = Path.Combine(folder, "scene.e2dscene"); ResourceSaver.Save(scene, path);
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location); start.ArgumentList.Add("--role-hook-child"); start.ArgumentList.Add(path);
            foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
            start.Environment["SDL_VIDEODRIVER"] = "dummy"; start.Environment["VK_DRIVER_FILES"] = start.Environment["VK_ICD_FILENAMES"] = "/nonexistent/e2d-role-hooks.json";
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            try { if (!process.WaitForExit(30000)) throw new TimeoutException("Role scene worker."); Check(process.ExitCode == 0 && output.Result.Contains("Role hook fresh scene restored.", StringComparison.Ordinal), output.Result + error.Result); }
            finally { if (!process.HasExited) { process.Kill(true); process.WaitForExit(5000); } }
        }
        finally { Directory.Delete(folder, true); }
    }
    internal static void NativeFocus(string renderer, bool invalid)
    {
        Register(); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always); ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var code = ResourceLoader.Load<Script>(RoleButtonCode.SourcePath); var button = code.New<Button>(); var state = RoleButtonCode.Get(button); var window = new Window { Title = "Script role focus", Size = new(240, 120) }; window.AddChild(button);
        var frames = 0; var proved = false; var rejected = false; var started = Stopwatch.GetTimestamp();
        window.Ready += _ => { Check(AccessibilityServer.IsSupported(), "Native accessibility service is required."); button.GrabFocus(); };
        try
        {
            try
            {
                var result = Engine.Run((_, _) =>
                {
                    frames++; if (Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(20)) throw new TimeoutException("Native role focus.");
                    if (state.FocusQueries > 2 && !proved)
                    {
                        Check(AccessibilityServer.HasElement(state.Element) && state.Element != button.GetAccessibilityElement() && button.GetConfigurationWarnings().Contains("button role"), "Actual native scene publisher selects a live Script sub-element."); proved = true;
                        if (invalid) state.Invalid = true;
                    }
                    if (proved && !invalid && frames > 16) window.Tree!.Quit();
                }, window);
                Check(result == 0, "Native focus host exits successfully.");
            }
            catch (InvalidOperationException error) when (invalid && error.Message.Contains("focused accessibility element is not live", StringComparison.Ordinal)) { rejected = true; }
            catch (AggregateException error) when (invalid && error.Flatten().InnerExceptions is { Count: 1 } failures && failures[0] is InvalidOperationException failure && failure.Message.Contains("focused accessibility element is not live", StringComparison.Ordinal)) { rejected = true; }
        }
        finally { if (!window.IsDisposed) window.Dispose(); }
        Check(proved && (invalid ? rejected : state.FocusQueries > 2) && state.Clears == 1 && state.Retired == 1 && !AccessibilityServer.HasElement(state.Element), "Native focus delivery/validation and lifetime complete.");
        Console.WriteLine($"Native Script role focus {renderer}, invalid={invalid}: live sub-element dispatch, publisher validation and retirement passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Expect(Exception expected, Action action) { try { action(); } catch (Exception e) when (ReferenceEquals(e, expected)) { return; } throw new InvalidOperationException("Missing original role-hook error."); }
}
