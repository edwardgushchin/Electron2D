using System.Globalization;
using Electron2D;

public static partial class BackendChecks
{
    private const string LocaleDomain = "physics-locale-changes";
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en"), French = CultureInfo.GetCultureInfo("fr");
    private sealed class LocaleBody : RigidBody
    {
        internal int Notifications;
        internal string Caption = "";
        internal Action? Changed;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationTranslationChanged) return;
            Notifications++; Caption = Atr("Status"); Changed?.Invoke();
        }
    }
    private sealed class LocaleNode : Node
    {
        internal int Notifications;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationTranslationChanged) Notifications++; }
    }
    private sealed class LocaleLoop : MainLoop
    {
        internal int Notifications;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationTranslationChanged) Notifications++; }
    }
    public static void RunLocaleChanges(PhysicsServer.Backend? backend, bool threaded)
    {
        var savedCulture = TranslationServer.Culture; var savedThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var savedTest = ProjectSettings.Get(ProjectSettings.LocaleTest); var savedPrefix = ProjectSettings.Get(ProjectSettings.PseudolocalizationPrefix);
        var savedPseudo = TranslationServer.PseudolocalizationEnabled;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded); ProjectSettings.Set(ProjectSettings.LocaleTest, "en");
        if (backend is null) PhysicsServerManager.RegisterServer("LocaleChanges", static () => new Solver());
        TranslationServer.AddTranslation(English, LocaleDomain, "Status", "Ready"); TranslationServer.AddTranslation(French, LocaleDomain, "Status", "Pret");
        TranslationServer.AddPluralTranslation(English, LocaleDomain, "one", "many", static count => count == 1 ? "one-en" : "many-en");
        TranslationServer.AddPluralTranslation(French, LocaleDomain, "one", "many", static count => count == 1 ? "one-fr" : "many-fr");
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("LocaleChanges");
            using var root = new SubViewport { World = world, TranslationDomain = LocaleDomain };
            using var shape = new CircleShape { Radius = 1 };
            var body = new LocaleBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); var sibling = new LocaleNode(); root.AddChild(sibling);
            using var tree = new SceneTree(root); using var independentRoot = new LocaleNode(); using var independent = new SceneTree(independentRoot);
            Engine.Start(tree);
            var before = body.Notifications; var other = independentRoot.Notifications; var rid = body.GetRID(); var identity = body.InstanceID; var tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position;
            TranslationServer.Culture = French;
            Check(body.Notifications == before + 1 && body.Caption == "Pret" && sibling.Notifications > 0, "Culture changes synchronously refresh the active physical scene");
            Check(independentRoot.Notifications == other && body.Position == position && body.GetRID() == rid && body.InstanceID == identity && PhysicsServer.SpaceGetTick(world.Space) == tick, "Application notification leaves independent trees and numerical state unchanged");
            before = body.Notifications; TranslationServer.Culture = new CultureInfo("fr"); Check(body.Notifications == before, "An equal culture name emits no translation notification");
            body.AutoTranslateMode = NodeAutoTranslateMode.Disabled; TranslationServer.Culture = English;
            Check(body.Caption == "Status" && body.Tr("Status") == "Ready", "Automatic disablement preserves explicit translation during global delivery"); body.AutoTranslateMode = NodeAutoTranslateMode.Inherit;
            tree.Paused = true; TranslationServer.Culture = French; Check(body.Caption == "Pret", "Global translation updates paused physical nodes"); tree.Paused = false;
            before = body.Notifications; body.Changed = () => { var read = Task.Run(() => TranslationServer.Culture); Check(read.Wait(5000) && read.Result.Name == "en", "Translation callbacks run outside the server mutation lock"); };
            TranslationServer.Culture = English; body.Changed = null; Check(body.Notifications == before + 1, "One normal mutation delivers one notification");
            var once = false; before = body.Notifications;
            body.Changed = () => { if (!once) { once = true; TranslationServer.Culture = English; } };
            TranslationServer.Culture = French; body.Changed = null;
            Check(TranslationServer.Culture.Name == "en" && body.Caption == "Ready" && body.Notifications == before + 2, "Reentrant language changes retain independent hierarchy snapshots");
            before = sibling.Notifications; body.Changed = static () => throw new ApplicationException("locale observer");
            Reject<AggregateException>(() => TranslationServer.Culture = French); body.Changed = null;
            Check(TranslationServer.Culture.Name == "fr" && sibling.Notifications == before + 1, "Observer failure preserves culture and attempts later nodes");
            var removed = sibling.Notifications; body.Changed = () => { root.RemoveChild(sibling); sibling.Dispose(); };
            TranslationServer.Culture = English; body.Changed = null; Check(sibling.Notifications == removed, "A removed captured candidate is skipped");
            before = body.Notifications;
            Task.Run(() => Reject<InvalidOperationException>(() => TranslationServer.Culture = French)).GetAwaiter().GetResult();
            Check(TranslationServer.Culture.Name == "fr" && body.Notifications == before, "A foreign scene notification rejects after the global culture commits");
            tree.Defer(() => tree.Notify(MainLoop.NotificationTranslationChanged)); tree.ProcessFrame(0);
            Check(body.Caption == "Pret" && body.Notifications == before + 1, "Explicit owner scheduling can publish the committed foreign change");
            before = body.Notifications; var pseudo = TranslationServer.PseudolocalizationEnabled;
            TranslationServer.PseudolocalizationEnabled = pseudo; TranslationServer.PseudolocalizationEnabled = !pseudo;
            Check(body.Notifications == before + 2, "Pseudolocalization setters notify even for equal values"); TranslationServer.PseudolocalizationEnabled = false;
            before = body.Notifications; ProjectSettings.Set(ProjectSettings.PseudolocalizationPrefix, "locale-test"); TranslationServer.ReloadPseudolocalization();
            Check(body.Notifications == before + 1 && TranslationServer.GetOrAddDomain("").PseudolocalizationPrefix == "locale-test" && !TranslationServer.PseudolocalizationEnabled, "Reload notifies after applying options and preserves runtime enablement");
            WarmLocaleFrames(tree, world, body, rid, identity);
            Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Locale changes preserve the requested physical implementation");
            Engine.Stop();
            using var loop = new LocaleLoop(); Engine.Start(loop); before = loop.Notifications;
            Check(before == 1 && TranslationServer.Culture.Name == "en" && TranslationServer.GetOrAddDomain("").PseudolocalizationPrefix == "locale-test", "Startup publishes one fully prepared translation state");
            TranslationServer.Culture = TranslationServer.Culture.Name == "en" ? French : English;
            Check(loop.Notifications == before + 1, "A non-scene MainLoop receives the same application notification"); Engine.Stop();
            Console.WriteLine($"Locale changes {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: synchronous state, reentrancy, errors, host lifetime and128 moving notified physics frames0/0 B passed.");
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop();
            ProjectSettings.Set(ProjectSettings.LocaleTest, savedTest); ProjectSettings.Set(ProjectSettings.PseudolocalizationPrefix, savedPrefix);
            TranslationServer.ReloadPseudolocalization(); TranslationServer.PseudolocalizationEnabled = savedPseudo; TranslationServer.Culture = savedCulture;
            TranslationServer.RemoveDomain(LocaleDomain); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, savedThread);
        }
    }
    private static void WarmLocaleFrames(SceneTree tree, World world, LocaleBody body, RID rid, ulong identity)
    {
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2];
        for (var i = 0; i < 256; i++) Frame(i); Collect(); for (var i = 0; i < 128; i++) Frame(i);
        var before = body.Notifications; var tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position.X;
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && body.Notifications == before + 128 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position.X > position,
            $"Warm changed-locale full physics frames: {owner}/{all} B");
        Console.WriteLine($"Locale full frames:128 measured culture changes/physics/query intervals,{owner}/{all} B.");
        void Frame(int index)
        {
            var french = (index & 1) != 0; TranslationServer.Culture = french ? French : English;
            tree.PhysicsFrame(.01); query.Position = body.GlobalPosition;
            Check(body.Caption == (french ? "Pret" : "Ready") && body.AtrN("one", "many", 2) == (french ? "many-fr" : "many-en") &&
                body.GetRID() == rid && body.InstanceID == identity && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid,
                "Warm locale publication, public translation and physical identity/query state");
        }
    }
}
