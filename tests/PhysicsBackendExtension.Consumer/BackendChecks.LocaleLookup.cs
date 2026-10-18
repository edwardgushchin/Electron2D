using System.Globalization;
using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    private const string LookupDomain = "physics-locale-lookup";
    private sealed class LookupCatalog : Translation
    {
        internal Func<string, string?>? Read;
        protected override string? OnGetMessage(string source, string context) => Read?.Invoke(source);
    }
    public static void RunLocaleLookup(PhysicsServer.Backend? backend, bool threaded)
    {
        var oldCulture = TranslationServer.Culture; var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldTest = ProjectSettings.Get(ProjectSettings.LocaleTest); var oldFallback = ProjectSettings.Get(ProjectSettings.LocaleFallback);
        var oldPseudo = ProjectSettings.Get(ProjectSettings.PseudolocalizationEnabled);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded); ProjectSettings.Set(ProjectSettings.LocaleTest, "sr-ME");
        ProjectSettings.Set(ProjectSettings.LocaleFallback, "en"); ProjectSettings.Set(ProjectSettings.PseudolocalizationEnabled, false);
        if (backend is null) PhysicsServerManager.RegisterServer("LocaleLookup", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("LocaleLookup");
            using var root = new SubViewport { World = world, TranslationDomain = LookupDomain, AutoTranslateMode = NodeAutoTranslateMode.Always };
            using var shape = new CircleShape { Radius = 1 };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); using var tree = new SceneTree(root); Engine.Start(tree);
            try
            {
                var domain = TranslationServer.GetOrAddDomain(LookupDomain); using var standalone = new TranslationDomain { LocaleOverride = "sr-ME" };
                using var parent = new Translation { Locale = "sr" }; parent.AddMessage("Status", "Parent"); parent.AddPluralMessage("one", ["parent-one", "parent-few", "parent-many"]);
                using var best = new Translation { Locale = "sr-Latn-ME" }; best.AddMessage("Status", "Best"); best.AddPluralMessage("one", ["best-one", "best-few", "best-many"]);
                using var fallback = new Translation { Locale = "en" }; fallback.AddMessage("Fallback", "English");
                using var invariant = new Translation { Locale = "" }; invariant.AddMessage("Ghost", "Wrong invariant");
                foreach (var catalog in new[] { parent, best, fallback, invariant }) { domain.AddTranslation(catalog); standalone.AddTranslation(catalog); }
                var comparisonBytes = MeasureLocaleScores();
                Check(body.Tr("Status") == "Best" && body.Atr("Status") == "Best" && body.TrN("one", "many", 2) == "best-few" && body.AtrN("one", "many", 5) == "best-many", "Scored resource lookup must precede a weaker parent catalog");
                Check(standalone.Translate("Status") == "Best" && standalone.TranslatePlural("one", "many", 2) == "best-few", "Independent and registered domains use one scored lookup contract");
                Check(body.Tr("Fallback") == "English" && standalone.Translate("Fallback") == "English" && body.Tr("Ghost") == "Ghost" && standalone.Translate("Ghost") == "Ghost", "Only the configured fallback is tried after the selected locale");
                Check(body.TranslationDomain == LookupDomain, "Physical nodes inherit the actual translation domain");
                body.CanTranslateMessages = false; Check(body.Tr("Status") == "Status" && body.Atr("Status") == "Status", "Object translation enablement controls both entry points"); body.CanTranslateMessages = true;
                body.AutoTranslateMode = NodeAutoTranslateMode.Disabled; Check(body.Atr("Status") == "Status" && body.Tr("Status") == "Best", "Automatic policy does not disable explicit lookup"); body.AutoTranslateMode = NodeAutoTranslateMode.Inherit;
                domain.LocaleOverride = "de"; Check(body.Tr("Status") == "Status" && body.Tr("Fallback") == "English", "Domain override and project fallback remain separate"); domain.LocaleOverride = "";
                TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("sr"), LookupDomain, "Status", "Weaker direct");
                Check(body.Tr("Status") == "Best", "A weaker direct parent cannot replace an exact scored resource");
                TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("sr-ME"), LookupDomain, "Direct", "Direct result");
                best.AddMessage("Direct", "Resource result"); Check(body.Tr("Direct") == "Direct result", "Direct registration wins equal locale score");
                TranslationServer.AddPluralTranslation(CultureInfo.GetCultureInfo("sr"), LookupDomain, "one", "many", static _ => "weaker-direct");
                Check(body.TrN("one", "many", 2) == "best-few", "Plural ranking also rejects weaker direct parents");
                TranslationServer.AddPluralTranslation(CultureInfo.GetCultureInfo("sr-ME"), LookupDomain, "direct-one", "direct-many", static n => n == 1 ? "direct-one-result" : "direct-many-result");
                best.AddPluralMessage("direct-one", ["resource-one", "resource-few", "resource-many"]);
                Check(body.TrN("direct-one", "direct-many", 2) == "direct-many-result" && body.TrN("absent", "absents", 1) == "absent" && body.TrN("absent", "absents", -1) == "absents", "Direct plural ties and missing source counts retain public semantics");
                best.AddMessage("Status", "Fresh"); Check(body.Tr("Status") == "Fresh", "Resource edits are observed without cached message results"); best.AddMessage("Status", "Best");
                best.Locale = "de"; Check(body.Tr("Status") == "Weaker direct", "Live locale replacement updates scores"); best.Locale = "sr-Latn-ME";
                using (var later = new Translation { Locale = "sr-Latn-ME" })
                {
                    later.AddMessage("Status", "Later"); domain.AddTranslation(later); Check(body.Tr("Status") == "Later", "Later registration wins equal resource scores");
                    later.Dispose(); Check(body.Tr("Status") == "Best", "Disposed resources disappear from the prepared snapshot");
                }
                using (var probe = new LookupCatalog { Locale = "sr-Latn-ME" })
                {
                    domain.AddTranslation(probe); var error = new ApplicationException("catalog observer"); probe.Read = _ => throw error;
                    try { _ = body.Tr("Status"); throw new InvalidOperationException("Expected original catalog error"); } catch (ApplicationException caught) { Check(ReferenceEquals(caught, error), "Original resource callback failure propagates"); }
                    probe.Read = _ => { Check(Task.Run(() => domain.Enabled).Wait(5000), "Catalog callback executes outside domain lock"); domain.RemoveTranslation(probe); return "Captured"; };
                    Check(body.Tr("Status") == "Captured" && body.Tr("Status") == "Best", "A callback can remove its own captured registration");
                }
                domain.Enabled = false; Check(body.TrN("one", "many", 2) == "many", "Disabled domains preserve plural source fallback"); domain.Enabled = true;
                TranslationServer.AddTranslation(best); TranslationServer.AddTranslation(parent);
                Check(TranslationServer.GetToolLocale() == "sr-ME", "Tool selection uses the same default-aware score as message lookup");
                TranslationServer.GetOrAddDomain("").RemoveTranslation(best); TranslationServer.GetOrAddDomain("").RemoveTranslation(parent);
                WarmLookupFrames(tree, world, body, standalone);
                Check(comparisonBytes == 0, $"Prepared locale scores allocate {comparisonBytes} bytes");
                var weak = MakeLocaleKey(); for (var i = 0; i < 4; i++) Collect(); Check(!weak.TryGetTarget(out _), "Prepared locale identities must not retain caller strings");
                Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Lookup preserves explicit physics selection");
                Engine.Stop();
                Console.WriteLine($"Locale lookup {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: ranked resource/direct fallback, edits, lifetime, callbacks and 128 complete physics/query intervals at 0/0 B passed.");
            }
            finally { if (ReferenceEquals(Engine.MainLoop, tree)) Engine.Stop(); }
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop(); TranslationServer.RemoveDomain(LookupDomain);
            ProjectSettings.Set(ProjectSettings.LocaleTest, oldTest); ProjectSettings.Set(ProjectSettings.LocaleFallback, oldFallback); ProjectSettings.Set(ProjectSettings.PseudolocalizationEnabled, oldPseudo);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); TranslationServer.Culture = oldCulture;
        }
    }
    private static long MeasureLocaleScores()
    {
        for (var i = 0; i < 256; i++) Scores(); Collect();
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Scores(); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        Console.WriteLine($"Prepared locale comparisons: {bytes} B / 128 groups."); return bytes;
        static void Scores() => Check(TranslationServer.CompareLocales("sr-ME", "sr-Latn-ME") == 10 && TranslationServer.CompareLocales("zh", "zh-CN") == 10 && TranslationServer.CompareLocales("sr", "sr-ME") == 4 && TranslationServer.CompareLocales("fr-CA", "fr-FR") == 4, "Fixed locale scores retain script/country defaults");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<string> MakeLocaleKey()
    {
        var key = new string("zh-CN".ToCharArray()); _ = TranslationServer.CompareLocales(key, "zh"); return new(key);
    }
    private static void WarmLookupFrames(SceneTree tree, World world, RigidBody body, TranslationDomain standalone)
    {
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2]; var rid = body.GetRID(); var identity = body.InstanceID;
        for (var i = 0; i < 256; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
        var tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position.X; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(); owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position.X > position, $"Resource lookup full physics frames {owner}/{all} B");
        Console.WriteLine($"Resource lookup frames: 128 measured complete intervals, {owner}/{all} B.");
        void Frame()
        {
            tree.PhysicsFrame(.01); query.Position = body.GlobalPosition;
            Check(body.Tr("Status") == "Best" && body.AtrN("one", "many", 2) == "best-few" && body.Tr("Fallback") == "English" && body.Tr("Ghost") == "Ghost" && standalone.Translate("Status") == "Best" && standalone.TranslatePlural("one", "many", 5) == "best-many" && body.InstanceID == identity && body.GetRID() == rid && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid, "Prepared registered/standalone resource lookup and physical identity/query state");
        }
    }
}
