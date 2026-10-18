using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Electron2D;

public static partial class BackendChecks
{
    private const string PseudoDomain = "physics-pseudo";
    private const string PseudoSource = "A😀%s";
    private const string PseudoExpected = "[_\u202e**\u202c%s\u202e\u202c_]";
    public static void RunPseudoOracle(string path)
    {
        using var domain = new TranslationDomain(); var rows = 0;
        foreach (var line in System.IO.File.ReadLines(path))
        {
            var fields = line.Split('\t'); Check(fields.Length == 6, "Pseudo oracle record");
            var mask = int.Parse(fields[0], CultureInfo.InvariantCulture); ConfigurePseudo(domain, mask, BitConverter.UInt32BitsToSingle(uint.Parse(fields[1], CultureInfo.InvariantCulture)), Decode(fields[2]), Decode(fields[3]));
            var source = Decode(fields[4]); var expected = Decode(fields[5]);
            Check(domain.Pseudolocalize(source) == expected, $"Pseudo oracle row {rows}, mask {mask}, source {fields[4]}"); rows++;
        }
        Check(rows > 50000, "Pseudo oracle covers complete option combinations and Unicode inputs");
        Console.WriteLine($"Independent pseudo oracle: {rows} exact Unicode/options/rounding records passed.");
        static string Decode(string hex) => Encoding.UTF8.GetString(Convert.FromHexString(hex));
    }
    private static void ConfigurePseudo(TranslationDomain domain, int mask = 25, float ratio = .5f, string prefix = "[", string suffix = "]")
    {
        domain.PseudolocalizationOverrideEnabled = (mask & 1) != 0; domain.PseudolocalizationDoubleVowelsEnabled = (mask & 2) != 0;
        domain.PseudolocalizationAccentsEnabled = (mask & 4) != 0; domain.PseudolocalizationFakeBIDIEnabled = (mask & 8) != 0;
        domain.PseudolocalizationSkipPlaceholdersEnabled = (mask & 16) != 0; domain.PseudolocalizationExpansionRatio = ratio;
        domain.PseudolocalizationPrefix = prefix; domain.PseudolocalizationSuffix = suffix;
    }
    public static void RunPseudolocalization(PhysicsServer.Backend? backend, bool threaded)
    {
        var oldCulture = TranslationServer.Culture; var oldTest = ProjectSettings.Get(ProjectSettings.LocaleTest);
        var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var oldMainEnabled = TranslationServer.PseudolocalizationEnabled;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded); ProjectSettings.Set(ProjectSettings.LocaleTest, "en");
        if (backend is null) PhysicsServerManager.RegisterServer("Pseudolocalization", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("Pseudolocalization");
            using var root = new SubViewport { World = world, TranslationDomain = PseudoDomain, AutoTranslateMode = NodeAutoTranslateMode.Always };
            using var shape = new CircleShape { Radius = 1 };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); using var tree = new SceneTree(root); Engine.Start(tree);
            try
            {
                var domain = TranslationServer.GetOrAddDomain(PseudoDomain); var main = TranslationServer.GetOrAddDomain("");
                using var standalone = new TranslationDomain { LocaleOverride = "en" };
                using var catalog = new Translation { Locale = "en" }; catalog.AddMessage("Status", PseudoSource); catalog.AddPluralMessage("one", ["one-en", "many-en"]);
                foreach (var owner in new[] { domain, standalone, main }) { owner.AddTranslation(catalog); ConfigurePseudo(owner); owner.PseudolocalizationEnabled = true; }
                var warmBytes = MeasurePseudoCalls(domain);
                ConfigurePseudo(domain, 17, 0); Check(domain.Pseudolocalize("😀%s") == "[*%s]", "Override counts one supplementary Unicode character once");
                ConfigurePseudo(domain, 16, 1); Check(domain.Pseudolocalize("😀") == "[😀]" && domain.Pseudolocalize("😀a") == "[_😀a_]", "Padding uses original Unicode code-point length and truncates each side");
                ConfigurePseudo(domain, 20, .5f); Check(domain.Pseudolocalize("F") == "[F́]" && domain.Pseudolocalize("a\u0301") == "[á\u0301]", "Accents preserve combining marks and padding uses the original input");
                ConfigurePseudo(domain, 24, 0); Check(domain.Pseudolocalize("a\n%s") == "[\u202ea\u202c\n\u202e\u202c%s\u202e\u202c]", "Fake BIDI closes at newlines and protected placeholders");
                ConfigurePseudo(domain, 17, 0); Check(domain.Pseudolocalize("%s%c%d%o%x%X%f %i %%s") == "[%s%c%d%o%x%X%f*****%s]", "Only the seven exact two-character placeholders are protected");
                Check(domain.Pseudolocalize("") == "", "Empty messages bypass prefix and suffix");
                ConfigurePseudo(domain, 16, 0); Check(domain.Pseudolocalize("\ud800x\udc00") == "[\ud800x\udc00]", "Unpaired managed surrogate units remain unchanged outside override");
                ConfigurePseudo(domain, 17, 0); Check(domain.Pseudolocalize("\ud800x\udc00") == "[***]", "Unpaired units each consume one override position");
                Reject<ArgumentNullException>(() => domain.Pseudolocalize(null!));
                foreach (var invalid in new[] { -1f, float.NaN, float.PositiveInfinity }) Reject<ArgumentOutOfRangeException>(() => domain.PseudolocalizationExpansionRatio = invalid);
                ConfigurePseudo(domain); var prepared = domain.Pseudolocalize(PseudoSource);
                domain.PseudolocalizationExpansionRatio = float.MaxValue; Reject<OverflowException>(() => domain.Pseudolocalize(PseudoSource));
                ConfigurePseudo(domain); Check(ReferenceEquals(prepared, domain.Pseudolocalize(PseudoSource)), "Failed output preparation preserves an earlier valid cached result");
                domain.PseudolocalizationPrefix = domain.PseudolocalizationPrefix;
                Check(ReferenceEquals(prepared, domain.Pseudolocalize(PseudoSource)), "Equal settings retain prepared output identity");
                domain.PseudolocalizationEnabled = false; Check(ReferenceEquals(prepared, domain.Pseudolocalize(PseudoSource)) && body.Tr("Status") == PseudoSource, "Explicit transforms ignore enablement while ordinary lookup respects it"); domain.PseudolocalizationEnabled = true;
                Check(body.Tr("Status") == PseudoExpected && body.Atr("Status") == PseudoExpected && standalone.Translate("Status") == PseudoExpected && TranslationServer.Translate("", "Status") == PseudoExpected, "Object, automatic, standalone and server routes share Unicode transforms");
                Check(body.TrN("one", "many", 2) == "many-en" && body.AtrN("one", "many", 1) == "one-en", "Plural lookup bypasses singular pseudolocalization");
                body.CanTranslateMessages = false; Check(body.Tr("Status") == "Status", "Per-object disablement returns source without transforming"); body.CanTranslateMessages = true;
                body.AutoTranslateMode = NodeAutoTranslateMode.Disabled; Check(body.Atr("Status") == "Status" && body.Tr("Status") == PseudoExpected, "Automatic disablement leaves explicit translation enabled"); body.AutoTranslateMode = NodeAutoTranslateMode.Inherit;
                catalog.AddMessage("Status", "Changed"); Check(body.Tr("Status") == "[_\u202e*******\u202c_]", "Live catalog edits do not reuse an old transformed message"); catalog.AddMessage("Status", PseudoSource);
                Check(body.Tr("Missing") == "[_\u202e*******\u202c_]", "Missing singular sources are transformed through the same route");
                var weak = PrepareTransientPseudo(domain); for (var i = 0; i < 4; i++) Collect(); Check(!weak.TryGetTarget(out _), "Prepared results must not retain retired caller strings");
                using (var retired = new TranslationDomain())
                {
                    var input = new string('a', 37); var result = RetirePseudo(retired, input); for (var i = 0; i < 4; i++) Collect();
                    Check(!result.TryGetTarget(out _), "Disposal releases prepared outputs even while the input and domain remain live"); GC.KeepAlive(input);
                    Reject<ObjectDisposedException>(() => retired.Pseudolocalize("x"));
                }
                var workers = Enumerable.Range(0, 4).Select(_ => Task.Run(() => MeasurePseudoCalls(domain, false))).ToArray(); Task.WaitAll(workers);
                Check(workers.All(task => task.Result == 0), "Concurrent prepared readers retain zero owner-thread bytes");
                MeasurePseudoCapacity(); WarmPseudoFrames(tree, world, body, standalone);
                Check(warmBytes == 0 && world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Prepared output and requested backend stay stable");
                Engine.Stop(); Console.WriteLine($"Pseudolocalization {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: Unicode, policy, readers/lifetime and 128 complete physics/query intervals at 0/0 B passed.");
            }
            finally { if (ReferenceEquals(Engine.MainLoop, tree)) Engine.Stop(); }
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop(); TranslationServer.RemoveDomain(PseudoDomain);
            ProjectSettings.Set(ProjectSettings.LocaleTest, oldTest); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread);
            TranslationServer.ReloadPseudolocalization(); TranslationServer.PseudolocalizationEnabled = oldMainEnabled; TranslationServer.Culture = oldCulture;
        }
    }
    private static long MeasurePseudoCalls(TranslationDomain domain, bool report = true)
    {
        for (var i = 0; i < 128; i++) _ = domain.Pseudolocalize(PseudoSource);
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) _ = domain.Pseudolocalize(PseudoSource); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        if (report) Console.WriteLine($"Prepared pseudolocalization: {bytes} B / 128 calls."); return bytes;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<string> PrepareTransientPseudo(TranslationDomain domain)
    { var input = new string(PseudoSource.ToCharArray()); _ = domain.Pseudolocalize(input); return new(input); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<string> RetirePseudo(TranslationDomain domain, string input)
    { var output = domain.Pseudolocalize(input); domain.Dispose(); return new(output); }
    private static void MeasurePseudoCapacity()
    {
        using var domain = new TranslationDomain(); ConfigurePseudo(domain);
        var inputs = Enumerable.Range(1, 64).Select(i => new string('a', i) + "😀%s").ToArray(); var outputs = new string[64]; Collect(); var retained = GC.GetTotalMemory(true);
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < inputs.Length; i++) outputs[i] = domain.Pseudolocalize(inputs[i]); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        retained = GC.GetTotalMemory(true) - retained;
        var warm = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < inputs.Length; i++) Check(ReferenceEquals(outputs[i], domain.Pseudolocalize(inputs[i])), "Prepared capacity returns retained outputs"); warm = GC.GetAllocatedBytesForCurrentThread() - warm;
        Check(warm == 0, $"Prepared 64-message capacity allocates {warm} bytes");
        Console.WriteLine($"Pseudo capacity: 64 live inputs, {bytes} cold managed B, {retained} retained managed B estimate, {warm} warm B."); GC.KeepAlive(inputs); GC.KeepAlive(outputs);
    }
    private static void WarmPseudoFrames(SceneTree tree, World world, RigidBody body, TranslationDomain standalone)
    {
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2]; var rid = body.GetRID(); var identity = body.InstanceID;
        for (var i = 0; i < 256; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
        var tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position.X; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(); owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position.X > position, $"Pseudo full physics frames {owner}/{all} B");
        Console.WriteLine($"Pseudo frames: 128 measured complete intervals, {owner}/{all} B.");
        void Frame()
        {
            tree.PhysicsFrame(.01); query.Position = body.GlobalPosition;
            Check(body.Tr("Status") == PseudoExpected && body.Atr("Status") == PseudoExpected && standalone.Translate("Status") == PseudoExpected && TranslationServer.Translate("", "Status") == PseudoExpected && TranslationServer.Pseudolocalize(PseudoSource) == PseudoExpected && body.AtrN("one", "many", 2) == "many-en" && body.InstanceID == identity && body.GetRID() == rid && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid, "Prepared singular/plural policy and physical state");
        }
    }
}
