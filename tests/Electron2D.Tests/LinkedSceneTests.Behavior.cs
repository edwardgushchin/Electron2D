using Electron2D;
using static TextureArrayTests;

internal static partial class LinkedSceneTests
{
    private static bool _registered;
    private static readonly List<string> Notifications = [];
    private static readonly List<WeakReference<Node>> Returned = [];
    private static bool FailNotification;
    private static SceneConnectionBinding<Probe, Probe, int> _pulse = null!;
    private sealed class Probe : Entity
    {
        internal string Tag = "";
        internal Node? Target;
        internal Resource? Data;
        internal int Calls;
        internal RID PrimedTextureRID;
        internal event Action<Probe>? Pulse;
        internal void Emit() => Pulse?.Invoke(this);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateProbe;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationSceneInstantiated) return;
            Notifications.Add(Tag + ":" + Position.X);
            if (GetNodeOrNull("Visual") is Sprite sprite && sprite.Texture is { } texture) PrimedTextureRID = texture.GetRID();
            if (FailNotification && Tag == "Inner")
            {
                var pending = new Stack<Node>(); pending.Push(this);
                while (pending.TryPop(out var node)) { Returned.Add(new(node)); foreach (var child in node.Children) pending.Push(child); }
                throw new ApplicationException("linked-notification");
            }
        }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [
            new PropertyDescriptor<Probe, string>(nameof(Tag), p => p.Tag, (p, v) => p.Tag = v, _ => "", stored: true),
            new PropertyDescriptor<Probe, Node?>(nameof(Target), p => p.Target, (p, v) => p.Target = v, _ => null, stored: true),
            new PropertyDescriptor<Probe, Resource?>(nameof(Data), p => p.Data, (p, v) => p.Data = v, _ => null, stored: true)
        ]);
    }
    private static Probe CreateProbe() => new();
    private static EventConnection SubscribePulse(Probe publisher, Probe subscriber, int amount) =>
        EventConnection.Subscribe<Probe>(h => publisher.Pulse += h, h => publisher.Pulse -= h, _ => subscriber.Calls += amount);
    private static void Register()
    {
        if (_registered) return;
        ResourceFileTypes.RegisterNode("tests.LinkedProbe", CreateProbe);
        _pulse = ResourceFileTypes.RegisterSceneConnection<Probe, Probe, int>("tests.LinkedPulse", "Pulse", "Count", SubscribePulse);
        _registered = true;
    }
    private static Entity MakeActor(Texture texture)
    {
        var root = new Probe { Name = "Actor", Tag = "Inner" };
        var first = new Sprite { Name = "Visual", Texture = texture, Centered = false, TextureFilter = TextureFilter.Nearest };
        var second = new Sprite { Name = "Alias", Texture = texture, Centered = false, Position = new(0, 0), Visible = false };
        root.AddChild(first); root.AddChild(second); first.Owner = root; second.Owner = root;
        return root;
    }
    private static ImageTexture NewTexture(Color color)
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(color);
        var texture = ImageTexture.CreateFromImage(image); texture.SetSizeOverride(new(12, 12)); texture.ResourceLocalToScene = true; return texture;
    }
    private static void Behavior()
    {
        Register();
        using var pixels = NewTexture(Colors.Red); using var source = MakeActor(pixels);
        using var graph = new MissingResource { ResourceLocalToScene = true, RecordingProperties = true };
        using var subgraph = new MissingResource { ResourceLocalToScene = true, RecordingProperties = true }; subgraph.SetRecordedProperty("Number", 0);
        graph.SetRecordedProperty<Resource?>("Self", graph); graph.SetRecordedProperty<Resource?>("Child", subgraph); ((Probe)source).Data = graph;
        var middle = new Probe { Name = "Middle" }; var leaf = new Probe { Name = "Leaf", Data = subgraph }; source.AddChild(middle); middle.Owner = source; middle.AddChild(leaf); leaf.Owner = middle; source.SetEditableInstance(middle, true);
        using var actor = new PackedScene(); actor.Pack(source);
        using var level = new Probe { Name = "Level", Tag = "Outer" };
        var one = (Probe)actor.Instantiate(); one.Name = "One"; one.Tag = "Override"; one.Position = new(4, 4); level.AddChild(one); one.Owner = level;
        var two = (Entity)actor.Instantiate(); two.Name = "Two"; level.AddChild(two); two.Owner = level; two.Position = new(36, 4);
        level.SetEditableInstance(one, true);
        using (var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8)) { image.Fill(Colors.Blue); ((ImageTexture)((Sprite)one.GetNode("Visual")).Texture!).Update(image); }
        var local = (MissingResource)one.Data!; local.SetRecordedProperty("Value", 83); ((MissingResource)local.GetRecordedProperty<Resource?>("Child")!).SetRecordedProperty("Number", 99); one.Target = one.GetNode("Alias");
        var extra = new Probe { Name = "Extra", Target = one.GetNode("Visual") }; one.AddChild(extra); extra.Owner = level; one.MoveChild(extra, 0);
        using var packed = new PackedScene(); packed.Pack(level);
        var state = packed.GetState();
        var extraIndex = Enumerable.Range(0, state.GetNodeCount()).Single(i => state.GetNodeName(i) == "Extra");
        Check(state.GetNodeIndex(extraIndex) == 0 && state.GetNodeType(1) == "" && state.GetNodeInstance(1) == actor, "Actual nested identities, inherited type omission and inserted sibling index.");
        Notifications.Clear();
        using var restored = packed.Instantiate(); var child = (Probe)restored.GetNode("One"); var visual = (Sprite)child.GetNode("Visual"); var alias = (Sprite)child.GetNode("Alias");
        Check(Notifications.SequenceEqual(new[] { "Inner:0", "Inner:0", "Outer:0" }), "Nested notifications precede root overrides and parent completion.");
        Check(child.Tag == "Override" && child.Position == new Vector2(4, 4) && child.GetChild(0).Name == "Extra" && ((Probe)child.GetNode("Extra")).Target == visual && child.Target == alias, "Complete typed state, added sibling placement and forward node references.");
        Check(visual.Texture == alias.Texture && visual.Texture!.GetLocalScene() == child && visual.Texture != ((Sprite)restored.GetNode("Two/Visual")).Texture, "Overrides preserve inherited texture aliases and independent local scenes.");
        using var blue = visual.Texture!.GetImage()!; Check(blue.GetPixel(0, 0) == Colors.Blue, "Local fallback receives edited source pixels.");
        var data = (MissingResource)child.Data!; Check(data.GetRecordedProperty<Resource?>("Self") == data && data.GetRecordedProperty<int>("Value") == 83 && data.GetLocalScene() == child, "Compatible local fallback retains copied cycles, payload and nested association.");
        var nestedData = data.GetRecordedProperty<Resource?>("Child");
        Check(nestedData == ((Probe)child.GetNode("Middle/Leaf")).Data && ((MissingResource)nestedData!).GetRecordedProperty<int>("Number") == 99, "Fallback remapping preserves aliases to unselected nested resource holders.");
        using var deep = (PackedScene)packed.DuplicateDeep(DeepDuplicateMode.All);
        Check(deep.GetState().GetNodeInstance(1) != actor, "Deep graph copying remaps linked templates.");
        using var clone = deep.Instantiate(); Check(((Sprite)clone.GetNode("One/Visual")).Texture!.GetLocalScene() == clone.GetNode("One"), "Deep copied links remain executable.");
        FailNotification = true; Returned.Clear();
        try { Reject<ApplicationException>(() => packed.Instantiate()); }
        finally { FailNotification = false; }
        Check(Returned.Count >= 3 && Returned.All(w => !w.TryGetTarget(out var node) || node.IsDisposed) && !actor.IsDisposed && !pixels.IsDisposed, "Failed nested notification releases all returned nodes while retaining borrowed templates/assets.");
        Connections(); Bounds(); Names(); PreflightBudgets();
    }
    private static void Connections()
    {
        using var source = new Probe { Name = "Actor", Tag = "Inner" }; var receiver = new Probe { Name = "Receiver" }; source.AddChild(receiver); receiver.Owner = source;
        using var token = _pulse.Subscribe(source, receiver, 3); using var actor = new PackedScene(); actor.Pack(source);
        using var level = new Entity { Name = "Level" }; var one = (Probe)actor.Instantiate(); level.AddChild(one); one.Owner = level; level.SetEditableInstance(one, true);
        using var parent = new PackedScene(); parent.Pack(level); Check(parent.GetState().GetConnectionCount() == 0, "Inherited subscriptions are not captured twice.");
        using var extra = _pulse.Subscribe(one, (Probe)one.GetNode("Receiver"), 7); parent.Pack(level);
        Check(parent.GetState().GetConnectionCount() == 1, "A new subscription with the same endpoints remains independent.");
        using var restored = parent.Instantiate(); var child = (Probe)restored.GetChild(0); var result = (Probe)child.GetNode("Receiver"); child.Emit(); Check(result.Calls == 10, "Original and additional rebound subscriptions each deliver once.");
    }
    private static void Bounds()
    {
        using var source = new Entity { Name = "Source" }; using var actor = new PackedScene(); actor.Pack(source);
        using var root = new Entity { Name = "Root" }; var child = actor.Instantiate(); root.AddChild(child); child.Owner = root;
        using var parent = new PackedScene(); parent.Pack(root); actor.ResetState(); Reject<InvalidOperationException>(() => parent.Instantiate());
        actor.Pack(source); Reject<InvalidOperationException>(() => actor.Pack(root)); Check(!actor.CanInstantiate(), "A cyclic capture fails and preserves clear-on-attempt semantics.");
        actor.Pack(source); actor.Dispose(); Reject<ObjectDisposedException>(() => parent.Instantiate());
    }
    private static void Names()
    {
        using var source = new Entity { Name = "Actor" }; var first = new Node { Name = "First" }; var second = new Node { Name = "Second" }; source.AddChild(first); source.AddChild(second); first.Owner = source; second.Owner = source;
        using var actor = new PackedScene(); actor.Pack(source); using var level = new Entity { Name = "Level" }; var child = actor.Instantiate(); level.AddChild(child); child.Owner = level; level.SetEditableInstance(child, true);
        var a = child.GetNode("First"); var b = child.GetNode("Second"); a.Name = "Temporary"; b.Name = "First"; a.Name = "Second";
        using var parent = new PackedScene(); parent.Pack(level); using var restored = parent.Instantiate();
        Check(restored.GetChild(0).GetChild(0).Name == "Second" && restored.GetChild(0).GetChild(1).Name == "First", "Original relative identities survive inherited name swaps.");
        var renamed = child.GetNode("First"); renamed.Name = "Renamed";
        var local = new Node { Name = "First" }; child.AddChild(local); local.Owner = level; child.MoveChild(local, 0);
        parent.Pack(level); using var inserted = parent.Instantiate();
        Check(inserted.GetChild(0).GetChild(0).Name == "First" && inserted.GetChild(0).GetNode("Renamed").Name == "Renamed", "Added node names survive collisions with inherited identities renamed later in the record order.");
    }
    private static int _budgetFactories;
    private static Node BudgetFactory() { _budgetFactories++; return new Node(); }
    private static void PreflightBudgets()
    {
        var scenes = new List<PackedScene>();
        try
        {
            var factory = new SceneFactoryData(BudgetFactory, typeof(Node), 0);
            SceneNodeData Record(string name, int parent, PackedScene? instance = null) => new(factory, name, parent, parent, -1, parent < 0 ? "." : name, ".", parent < 0 ? "" : ".", [], [], instance, "");
            var leaf = new PackedScene(); leaf.LoadFileData(new([Record("Root", -1)])); scenes.Add(leaf);
            var last = leaf;
            for (var i = 0; i < 17; i++)
            {
                var next = new PackedScene(); next.LoadFileData(new([Record("Root", -1), Record("Left", 0, last), Record("Right", 0, last)])); scenes.Add(next); last = next;
            }
            _budgetFactories = 0; Reject<InvalidOperationException>(() => last.Instantiate()); Check(_budgetFactories == 0, "Amplified linked graphs reject before any construction.");
            last = leaf;
            for (var i = 0; i < 129; i++)
            {
                var next = new PackedScene(); next.LoadFileData(new([Record("Root", -1), Record("Child", 0, last)])); scenes.Add(next); last = next;
            }
            Reject<InvalidOperationException>(() => last.Instantiate()); Check(_budgetFactories == 0, "Unsafe template depth rejects before any construction.");
            last = leaf;
            for (var i = 0; i < 20; i++)
            {
                var next = new PackedScene(); next.LoadFileData(new([Record("Root", -1), Record("Child", 0, last)])); scenes.Add(next); last = next;
            }
            var shared = last; PackedScene? boundary = null;
            for (var i = 0; i < 110; i++)
            {
                var next = new PackedScene(); next.LoadFileData(new([Record("Root", -1), Record("Child", 0, last)])); scenes.Add(next); last = next;
                if (i == 105) boundary = next;
            }
            foreach (var shortFirst in new[] { true, false })
            {
                var diamond = new PackedScene(); scenes.Add(diamond);
                diamond.LoadFileData(new([Record("Root", -1), Record("First", 0, shortFirst ? shared : boundary), Record("Second", 0, shortFirst ? boundary : shared)]));
                using (var valid = diamond.Instantiate()) Check(_budgetFactories > 0, "Exactly 128 template levels remain instantiable with shared references.");
                _budgetFactories = 0;
                diamond.LoadFileData(new([Record("Root", -1), Record("First", 0, shortFirst ? shared : last), Record("Second", 0, shortFirst ? last : shared)]));
                Reject<InvalidOperationException>(() => { using var unexpected = diamond.Instantiate(); });
                Check(_budgetFactories == 0, "Shared template depth rejects before construction regardless of reference order.");
            }
        }
        finally { foreach (var scene in scenes) scene.Dispose(); }
    }

}
