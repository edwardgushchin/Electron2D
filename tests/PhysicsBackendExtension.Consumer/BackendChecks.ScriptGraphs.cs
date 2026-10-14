using Electron2D;

public static partial class BackendChecks
{
    private sealed class CodeGraph : Resource
    {
        internal static readonly PropertyDescriptor<CodeGraph, Script?> SharedProperty = new(nameof(Shared), g => g.Shared, (g, v) => g.Shared = v, _ => null, stored: true);
        public Script? Shared { get; set; }
        public Script? Forced { get; set; }
        public Script? ForcedAlias { get; set; }
        public Script? Never { get; set; }
        public Script[] Codes { get; set; } = [];
        public Dictionary<Script, Script> Map { get; set; } = [];
        public bool ForceFirst { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(SharedProperty);
        protected override Resource CreateDuplicateInstance() => new CodeGraph();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode,
            Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
        {
            var g = (CodeGraph)target; g.ForceFirst = ForceFirst;
            if (ForceFirst) g.Forced = (Script?)force(Shared);
            g.Shared = (Script?)duplicate(Shared);
            g.Codes = deep ? Codes.Select(s => (Script)duplicate(s)!).ToArray() : Codes;
            g.Map = deep ? Map.ToDictionary(p => (Script)duplicate(p.Key)!, p => (Script)duplicate(p.Value)!) : Map;
            if (!ForceFirst) g.Forced = (Script?)force(Shared);
            g.ForcedAlias = (Script?)force(Shared); g.Never = Never;
        }
    }

    public static void VerifyNestedScript(Script script)
    {
        foreach (var first in new[] { false, true })
        {
            using var graph = new CodeGraph { Shared = script, Never = script, Codes = [script, script], Map = new() { [script] = script }, ForceFirst = first };
            for (var mode = 0; mode < 5; mode++)
            {
                using var copy = (CodeGraph)(mode < 2 ? graph.Duplicate(mode == 1) : graph.DuplicateDeep((DeepDuplicateMode)(mode - 2)));
                using var forced = copy.Forced!;
                Check(copy.Shared == script && copy.Never == script && copy.Codes.All(s => s == script) && copy.Map.Single().Key == script && copy.Map.Single().Value == script,
                    "Ordinary Script edges preserve the same source instance in scalar, array and dictionary fields regardless of deep mode or force order");
                Check(forced != script && copy.ForcedAlias == forced && forced.SourceCode == script.SourceCode && forced.CompiledType == script.CompiledType &&
                    forced.ResourcePath.Length == 0 && forced.ResourceSceneUniqueID.Length == 0 && forced.GetLocalScene() is null,
                    "Forced Script edges share one independent copied wrapper and preserve compiled source association");
                Check(mode == 0 ? ReferenceEquals(copy.Codes, graph.Codes) && ReferenceEquals(copy.Map, graph.Map) :
                    !ReferenceEquals(copy.Codes, graph.Codes) && !ReferenceEquals(copy.Map, graph.Map),
                    "Shallow/deep collection containers retain their independent policy while Script elements remain shared");
                if (script.CanInstantiate())
                {
                    using var actor = copy.Shared!.New<Node>();
                    Check(actor.GetType() == script.CompiledType, "The borrowed compiled Script constructs the real registered project class after graph duplication");
                }
            }
        }
        using var rootCopy = (Script)script.DuplicateDeep(DeepDuplicateMode.All);
        Check(rootCopy != script && rootCopy.SourceCode == script.SourceCode && rootCopy.CompiledType == script.CompiledType,
            "A Script explicitly duplicated as the root retains its independent wrapper contract");
    }

    private sealed class CodeScene : Entity
    {
        private static readonly PropertyDescriptor<CodeScene, CodeGraph?> GraphProperty = new(nameof(Graph), n => n.Graph, (n, v) => n.Graph = v, _ => null, stored: true);
        private static readonly PropertyDescriptor<CodeScene, Script?> DirectProperty = new(nameof(Direct), n => n.Direct, (n, v) => n.Direct = v, _ => null, stored: true);
        public CodeGraph? Graph { get; set; }
        public Script? Direct { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(GraphProperty).Append(DirectProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateCodeScene;
    }
    private static Node CreateCodeScene() => new CodeScene();

    private static void ScriptGraphs(Node physicalRoot, World world)
    {
        foreach (var path in new[] { "", "scene.e2dres::code", "external.cs" })
        {
            using var script = new Script { SourceCode = "source", ResourceName = "code", CanTranslateMessages = false, TranslationDomain = StoredResourceDomain };
            script.SetPathCache(path); script.ResourceSceneUniqueID = "script_source";
            VerifyNestedScript(script);
        }
        foreach (var local in new[] { false, true })
        {
            using var script = new Script { SourceCode = "scene code", ResourceLocalToScene = local };
            using var graph = new CodeGraph { Shared = script, Never = script, Codes = [script, script], Map = new() { [script] = script }, ForceFirst = true, ResourceLocalToScene = true };
            using var shape = new CircleShape { Radius = 2, ResourceLocalToScene = true };
            using var source = new CodeScene { Name = "CodeScene", Graph = graph, Direct = script };
            var body = new StaticBody { Name = "Body" }; var child = new CollisionShape { Name = "Shape", Shape = shape };
            source.AddChild(body); body.Owner = source; body.AddChild(child); child.Owner = source;
            using var packed = new PackedScene(); packed.Pack(source);
            using var first = (CodeScene)packed.Instantiate(); using var second = (CodeScene)packed.Instantiate();
            var a = first.Graph!; var b = second.Graph!;
            Check(a.Forced != b.Forced && a.Forced != a.Never && a.ForcedAlias == a.Forced && !script.IsDisposed,
                "Scene instances acquire independent forced Script wrappers without owning the source");
            if (local)
                Check(a.Shared == a.Forced && b.Shared == b.Forced && first.Direct == a.Shared && second.Direct == b.Shared &&
                    a.Shared!.GetLocalScene() == first && b.Shared!.GetLocalScene() == second,
                    "Local Script references resolve to one owned wrapper per scene across nested and direct stored properties");
            else
                Check(a.Shared == b.Shared && a.Shared != a.Forced && b.Shared != b.Forced && first.Direct == a.Shared && second.Direct == b.Shared &&
                    a.Shared!.GetLocalScene() is null,
                    "Non-local Script references remain shared despite earlier forced copies in nested and direct scene properties");
            var code = a.Shared!.SourceCode; script.SourceCode = "edited source";
            Check(a.Shared.SourceCode == (local ? code : "edited source") && b.Shared!.SourceCode == (local ? code : "edited source") &&
                a.Forced!.SourceCode == code && b.Forced!.SourceCode == code,
                "Borrowed non-local scripts observe source edits while acquired local/forced wrappers keep independent text");
            using var reset = new Animation(); using var library = new AnimationLibrary(); library.AddAnimation("RESET", reset);
            var mixer = new AnimationPlayer { Name = "Reset" }; source.AddChild(mixer); mixer.Owner = source; mixer.AddAnimationLibrary("", library);
            using var authoring = new PackedScene(); authoring.PackForSave(source);
            using var saved = (CodeScene)authoring.Instantiate(); var savedCode = saved.Graph!.Shared!;
            script.SourceCode = "later source";
            Check(savedCode != script && savedCode.SourceCode == "edited source" && saved.Graph.Forced!.SourceCode == "edited source",
                "The real isolated RESET authoring path captures independent Script text for local and non-local flags");
            first.Name = "CodeFirst"; second.Name = "CodeSecond";
            first.Position = new(90, 0); second.Position = new(100, 0); physicalRoot.AddChild(first); physicalRoot.AddChild(second);
            physicalRoot.Tree!.PhysicsFrame(1d / 60);
            using var query = new PhysicsPointQueryParameters { Position = first.Position }; var hits = new PhysicsPointResult[1];
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == bodyRID(first),
                "Restored Script resource graphs coexist with actual selected physical scene/query execution");
            var acquired = a.Forced; var borrowed = a.Shared; first.Dispose();
            Check(acquired!.IsDisposed && !script.IsDisposed && (local || !borrowed.IsDisposed) && !b.Forced!.IsDisposed,
                "Scene retirement releases acquired code without retiring source, shared non-local or another scene's code");
        }
        Console.WriteLine("Script graph policy: builtin/external, all copy modes, force ordering, scalar/array/dictionary identities, local scenes, authoring and physical lifetime passed.");
        static RID bodyRID(CodeScene scene) => scene.GetNode<StaticBody>("Body").GetRID();
    }
}
