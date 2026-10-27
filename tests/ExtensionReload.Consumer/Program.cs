using Electron2D;
using System.Diagnostics;

Engine.IsEditorHint = args.Contains("--editor");
ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, args.Contains("--threaded"));
var backend = args.Contains("--gpu") ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
Fixture.ParentVersion = 1; Fixture.ChildVersion = 1;
Check(ExtensionManager.LoadExtensionFromFunction("parent", Fixture.OpenParent) == ExtensionManager.LoadStatus.Ok, "Parent module loaded.");
Check(ExtensionManager.LoadExtensionFromFunction("child", Fixture.OpenChild) == ExtensionManager.LoadStatus.Ok, "Child module loaded.");
Fixture.Child = ExtensionManager.GetExtension("child")!;
if (args.Contains("--archive-child"))
{
    using var scene = ResourceLoader.Load<PackedScene>(args[Array.IndexOf(args, "--archive-child") + 1], ResourceLoader.CacheMode.Ignore);
    using var restored = (ReloadBody)scene.Instantiate(); Check(restored.Score == 37 && restored.Tick is not null, "Fresh-process file factory binds the actual extension class.");
    Retire(); Console.WriteLine("archive-child OK"); return;
}
using (var world = new World(backend))
using (var root = new SubViewport { World = world })
using (var shape = new CircleShape { Radius = 2 })
using (var data = new Resource())
using (var floorShape = new RectangleShape { Size = new(50, 2) })
{
    PhysicsServer.AreaSetGravity(world.Space, 980); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
    var a = Fixture.Child.New<ReloadBody>("reload-body"); var b = Fixture.Child.New<ReloadBody>("reload-body");
    using var ordinary = new ReloadBody();
    foreach (var actor in new[] { a, b })
    {
        actor.Name = actor == a ? "A" : "B"; actor.Position = new(actor == a ? 0 : 100, 0);
        actor.Score = actor == a ? 9 : 11; actor.Values = [3, 7]; actor.Data = data;
        actor.GravityScale = 0; actor.CanSleep = false; actor.LinearVelocity = new(60, 0);
        actor.AddChild(new CollisionShape { Shape = shape }); root.AddChild(actor);
    }
    a.Peer = b; b.Peer = a;
    ReloadBody AddBody(string name, Vector2 position)
    {
        var body = Fixture.Child!.New<ReloadBody>("reload-body"); body.KeepName = true; body.Name = name; body.Position = position;
        body.GravityScale = 0; body.CanSleep = false; body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); return body;
    }
    var sleeper = AddBody("Sleeper", new(0, 1000)); sleeper.CanSleep = true;
    var contact = AddBody("Contact", new(2000, 6)); contact.GravityScale = 1; contact.MaxContactsReported = 4;
    var floor = new StaticBody { Position = new(2000, 10) }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
    var jointA = AddBody("JointA", new(1000, 0)); var jointB = AddBody("JointB", new(1100, 0));
    jointA.LinearVelocity = jointB.LinearVelocity = new(60, 0);
    var joint = new PinJoint { NodeA = "../JointA", NodeB = "../JointB", Position = new(1000, 0) }; root.AddChild(joint);

    using var tree = new SceneTree(root);
    Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Selected backend without fallback.");
    var initialTick = a.Tick!;
    a.Tick = (body, delta) =>
    {
        Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension("parent"));
        Reject<InvalidOperationException>(() => ExtensionManager.UnloadExtension("parent"));
        initialTick(body, delta);
    };
    tree.PhysicsFrame(1d / 60); a.Tick = initialTick;
    Check(ExtensionManager.GetExtension("parent")!.IsLibraryOpen() && a.Score == 9, "Active-frame replacement/unload rejects before module mutation.");

    for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
    sleeper.Sleeping = true;
    var contactCount = contact.GetContactCount(); var jointRID = joint.GetRID();
    Check(contactCount > 0 && jointRID.IsValid() && sleeper.Sleeping, "Physical contact, sleep and joint controls are live before replacement.");
    var aID = a.InstanceID; var bID = b.InstanceID; var aRID = a.GetRID(); var bRID = b.GetRID();
    var position = a.Position; var velocity = a.LinearVelocity;
    var notifications = 0; var moduleSignals = 0;
    Action verifyRestored = () =>
    {
        Check(a.Score == 9 && b.Score == 11 && a.Values.SequenceEqual(new[] { 3f, 7f }) && ReferenceEquals(a.Data, data), "All instance values precede every notification.");
        Check(ReferenceEquals(a.Peer, b) && ReferenceEquals(b.Peer, a) && a.Tick is not null && b.Tick is not null, "Both bindings and node references precede notifications.");
        Check(a.InstanceID == aID && b.InstanceID == bID && a.GetRID() == aRID && b.GetRID() == bRID && a.Position == position && a.LinearVelocity == velocity,
            "Object, RID, pose and velocity retain exact identity/state through replacement.");
        Check(contact.GetContactCount() == contactCount && sleeper.Sleeping && joint.GetRID() == jointRID, "Unchanged physical setters do not retire contacts, sleep or joint identity during restoration.");
        notifications++;
    };
    a.Reloaded = verifyRestored; b.Reloaded = verifyRestored;
    Action<Extension> moduleReloaded = _ => { Check(notifications == 2, "All instance notifications precede manager event."); moduleSignals++; };
    ExtensionManager.ExtensionLoaded += moduleReloaded;
    try
    {
        Fixture.ParentVersion = 2;
        Fixture.RestoreProbe = () => Reject<InvalidOperationException>(() => ExtensionManager.ReloadExtension("child"));
        var parent = ExtensionManager.GetExtension("parent");
        Check(ExtensionManager.ReloadExtension("parent") == ExtensionManager.LoadStatus.Ok && ReferenceEquals(parent, ExtensionManager.GetExtension("parent")), "Parent replacement retains its resource.");
        Check(notifications == 2 && moduleSignals == 1 && a.Option == 20 && b.Option == 20, "Nondefault values restore; old defaults adopt the new generation.");
        Check(ordinary.Notifications == 0 && ordinary.Tick is null, "Ordinary CLR class instances are not extension reload recipients.");
    }
    finally { Fixture.RestoreProbe = null; ExtensionManager.ExtensionLoaded -= moduleReloaded; a.Reloaded = b.Reloaded = null; }
    var tick = a.CallbackTicks;
    tree.PhysicsFrame(1d / 60); Check(a.CallbackTicks == tick + 2, "Existing physics callback now executes the replacement module function.");
    for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
    var x = a.Position.X; var all = GC.GetTotalAllocatedBytes(true); var ownerBytes = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < 128; i++) { tree.PhysicsFrame(1d / 60); _ = a.LinearVelocity; }
    ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; all = GC.GetTotalAllocatedBytes(true) - all;
    Check(ownerBytes == 0 && all == 0 && MathF.Abs(a.Position.X - x - 128) < .02f, $"Full post-reload physical frames: {ownerBytes}/{all} B, travel {a.Position.X - x}.");
    Archive();
    Fixture.ChildVersion = 2; Check(ExtensionManager.ReloadExtension("child") == ExtensionManager.LoadStatus.Ok && a.ChildGeneration == 2 && a.Score == 9, "Leaf module replacement also restores its inherited instance.");
    var before = a.Notifications; Fixture.RemoveParent = true;
    Check(ExtensionManager.ReloadExtension("parent") == ExtensionManager.LoadStatus.Ok && a.Tick is null && b.Tick is null && a.Notifications == before, "Removed parent retires descendants without false reload notification.");
    Reject<InvalidOperationException>(() => Fixture.Child.New<ReloadBody>("reload-body"));
}
Retire();
Failures.Run();
Console.WriteLine($"{{\"ok\":true,\"backend\":\"{backend}\",\"editor\":{Engine.IsEditorHint.ToString().ToLowerInvariant()},\"measuredFrames\":128,\"ownerBytes\":0,\"allBytes\":0}}");

static void Archive()
{
    var directory = Directory.CreateTempSubdirectory("e2d-extension-reload-");
    try
    {
        using var actor = Fixture.Child!.New<ReloadBody>("reload-body"); actor.Score = 37;
        using var packed = new PackedScene(); packed.Pack(actor);
        var path = System.IO.Path.Combine(directory.FullName, "class.e2dscene"); ResourceSaver.Save(packed, path);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        using var copy = (ReloadBody)loaded.Instantiate(); Check(copy.Score == 37 && copy.Tick is not null, "Archive factory restores a bound exact class.");
        var executable = Environment.ProcessPath!; var arguments = Environment.GetCommandLineArgs();
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (arguments[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(arguments[0]);
        start.ArgumentList.Add("--archive-child"); start.ArgumentList.Add(path);
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit();
        Check(process.ExitCode == 0 && output.Contains("archive-child OK"), "Fresh-process archive failed: " + error);
    }
    finally { directory.Delete(true); }
}
static void Retire()
{
    foreach (var path in new[] { "child", "parent" }) { var module = ExtensionManager.GetExtension(path)!; ExtensionManager.UnloadExtension(path); module.Dispose(); }
}
static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static T Reject<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

static class Fixture
{
    internal static int ParentVersion, ChildVersion;
    internal static bool RemoveParent;
    internal static Extension? Child;
    internal static Action? RestoreProbe;
    internal static ExtensionInitialization? OpenParent(Extension module) => new(ExtensionInitializationLevel.Scene,
        level =>
        {
            if (level != ExtensionInitializationLevel.Scene || RemoveParent) return;
            var version = ParentVersion;
            module.RegisterClass<ReloadBase>("reload-base", null,
                body => { if (!body.KeepName) body.Name = "Rebound"; body.DefaultOption = version * 10; body.Option = body.DefaultOption; body.Score = 0; body.Values = []; body.Data = null; body.Peer = null; body.Tick = (owner, _) => owner.CallbackTicks += version; },
                body => body.Tick = null);
        }, _ => { });
    internal static ExtensionInitialization? OpenChild(Extension module) => new(ExtensionInitializationLevel.Scene,
        level =>
        {
            if (level != ExtensionInitializationLevel.Scene) return;
            var version = ChildVersion;
            module.RegisterClass<ReloadBody>("reload-body", Create, body => body.ChildGeneration = version, _ => { });
        }, _ => { });
    private static ReloadBody Create() => new();
}
abstract class ReloadBase : RigidBody
{
    internal bool KeepName;
    internal Action<ReloadBase, double>? Tick;
    internal Action? Reloaded;
    internal int Score, Option, DefaultOption, CallbackTicks, Notifications;
    internal float[] Values = [];
    internal Resource? Data;
    internal Node? Peer;
    private static readonly PropertyDescriptor[] Fields = [
        new PropertyDescriptor<ReloadBase, int>("Score", b => b.Score, (b, v) => { Fixture.RestoreProbe?.Invoke(); b.Score = v; }, _ => 0, stored: true),
        new PropertyDescriptor<ReloadBase, int>("Option", b => b.Option, (b, v) => b.Option = v, b => b.DefaultOption, stored: true),
        new PropertyDescriptor<ReloadBase, float[]>("Values", b => b.Values, (b, v) => b.Values = v, stored: true),
        new PropertyDescriptor<ReloadBase, Resource?>("Data", b => b.Data, (b, v) => b.Data = v, stored: true),
        new PropertyDescriptor<ReloadBase, Node?>("Peer", b => b.Peer, (b, v) => b.Peer = v, stored: true)];
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() { foreach (var p in base.GetPropertyDescriptors()) yield return p; foreach (var p in Fields) yield return p; }
    protected override void OnPhysicsProcess(double delta) { Tick?.Invoke(this, delta); base.OnPhysicsProcess(delta); }
    protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationExtensionReloaded) { Notifications++; Reloaded?.Invoke(); } }
}
sealed class ReloadBody : ReloadBase
{
    internal int ChildGeneration;
    protected override Func<Node> CreateSceneInstanceFactory() => Create;
    private static Node Create() => Fixture.Child!.New<ReloadBody>("reload-body");
}
