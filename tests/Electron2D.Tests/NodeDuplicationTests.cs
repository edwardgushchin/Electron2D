using Electron2D;
using static TextureArrayTests;
using Flags = Electron2D.Node.DuplicateFlags;

internal static partial class NodeDuplicationTests
{
    private class Actor : Entity
    {
        internal static bool FailSet, FailFactory, MutateRead, FailConnect;
        internal static Node? ForcedFactoryResult;
        internal static int DisposedCount;
        internal int Score = 3, Transient = 4, Calls, Readied;
        internal Node? Link;
        internal Resource? Asset, Forced;
        internal int[] Values = [1, 2];
        internal event Action? Pulse;
        internal void Emit() => Pulse?.Invoke();
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [
            new PropertyDescriptor<Actor, int>(nameof(Score), n => { if (MutateRead) n.Name = "Changed"; return n.Score; }, (n, v) => { if (FailSet) throw new ArithmeticException("copy setter"); n.Score = v; }, stored: true),
            new PropertyDescriptor<Actor, int>(nameof(Transient), n => n.Transient, (n, v) => n.Transient = v),
            new PropertyDescriptor<Actor, Node?>(nameof(Link), n => n.Link, (n, v) => n.Link = v, stored: true),
            new PropertyDescriptor<Actor, Resource?>(nameof(Asset), n => n.Asset, (n, v) => n.Asset = v, stored: true),
            new PropertyDescriptor<Actor, Resource?>(nameof(Forced), n => n.Forced, (n, v) => n.Forced = v, stored: true) { AlwaysDuplicateResource = true },
            new PropertyDescriptor<Actor, int[]>(nameof(Values), n => n.Values, (n, v) => n.Values = v, stored: true)
        ]);
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => ForcedFactoryResult ?? (FailFactory ? throw new ArithmeticException("copy factory") : new Actor());
        protected override void OnReady() => Readied++;
        protected override void OnProcess(double delta) => Calls++;
        protected override void Dispose(bool disposing) { if (disposing) DisposedCount++; base.Dispose(disposing); }
    }
    private sealed class OtherActor : Actor
    {
        internal int Role = 15;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
            [new PropertyDescriptor<OtherActor, int>(nameof(Role), n => n.Role, (n, v) => n.Role = v, stored: true)]);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateOther;
        private static Node CreateOther() => new OtherActor();
    }
    private static EventConnection Subscribe(Actor publisher, Actor subscriber, int amount)
    {
        if (Actor.FailConnect) throw new ArithmeticException("copy subscription");
        return EventConnection.Subscribe(h => publisher.Pulse += h, h => publisher.Pulse -= h, () => subscriber.Calls += amount);
    }
    private static readonly SceneConnectionBinding<Actor, Actor, int> Binding = ResourceFileTypes.RegisterSceneConnection<Actor, Actor, int>("tests.node-duplication", "Pulse", "Add", Subscribe);
    internal static void Run()
    {
        using var source = new Actor { Name = "Source", Position = new(7, 8), Score = 42, Transient = 99, DisplayFolded = true };
        var child = new Actor { Name = "Child" }; source.AddChild(child); child.Owner = source; source.Link = child; child.Link = source;
        source.AddToGroup("runtime"); source.AddToGroup("persistent", true);
        var hidden = new Node { Name = "Hidden" }; source.AddChild(hidden, internalMode: Node.InternalMode.Back);
        using var external = new Actor { Name = "External" }; var scene = new Node { Name = "World" }; scene.AddChild(source); scene.AddChild(external);
        using var ordinary = EventConnection.Subscribe(h => source.Pulse += h, h => source.Pulse -= h, () => source.Calls += 100);
        using var first = Binding.Subscribe(source, child, 2); using var outside = Binding.Subscribe(source, external, 3);
        using var asset = new Gradient(); source.Asset = asset; child.Asset = asset; source.Forced = asset;
        using var copy = (Actor)source.Duplicate(); var copiedChild = (Actor)copy.GetChild(0);
        Check(copy.Parent is null && copy.Tree is null && copy.Readied == 0 && copy.InstanceID != source.InstanceID, "Detached fresh copy without Ready");
        Check(copy.GetChildCount(true) == 1 && copiedChild.Owner is null && copy.Name == source.Name, "Ordinary descendants ignore ownership and internal nodes");
        Check(copy.Score == 42 && copy.Transient == 4 && !copy.DisplayFolded && copy.Position == source.Position, "Stored properties only");
        Check(copy.Link == copiedChild && copiedChild.Link == copy && copy.Asset == asset && copiedChild.Asset == asset && copy.Forced != asset, "Node remap, shared and forced resources");
        Check(!ReferenceEquals(copy.Values, source.Values) && copy.Values.AsSpan().SequenceEqual(source.Values), "Independent array containers");
        copy.Emit(); Check(copiedChild.Calls == 2 && external.Calls == 3 && source.Calls == 0, "Authored internal/external connections, no CLR handler copying");
        Check(copy.IsInGroup("runtime") && copy.IsInGroup("persistent"), "All group memberships");
        using var full = (Actor)source.Duplicate(Flags.Default | Flags.InternalState); Check(full.Transient == 99 && full.DisplayFolded, "Nonstored writable properties");
        using var plain = source.Duplicate(0); Check(plain.GetType() == typeof(Entity) && plain.GetChild(0).GetType() == typeof(Entity) && plain.GetGroups().Count == 0, "No scripts selects engine construction and no groups");
        Check(((Entity)plain).Position == source.Position, "Base state survives script omission");
        using var noSignals = (Actor)source.Duplicate(Flags.Scripts); noSignals.Emit(); Check(((Actor)noSignals.GetChild(0)).Calls == 0 && external.Calls == 3, "Signals flag executes");
        using var tree = new SceneTree(scene); tree.Root.AddChild(copy); Check(copy.Readied == 1 && copiedChild.Readied == 1, "Real activation after duplication"); tree.Root.RemoveChild(copy);
        Reject<ArgumentOutOfRangeException>(() => source.Duplicate((Flags)32)); Reject<InvalidOperationException>(() => Task.Run(() => source.Duplicate()).GetAwaiter().GetResult());
        var before = Actor.DisposedCount; Actor.FailSet = true;
        try { Reject<ArithmeticException>(() => source.Duplicate()); } finally { Actor.FailSet = false; }
        Check(Actor.DisposedCount >= before + 2 && source.Score == 42 && !source.IsDisposed, "Setter failure cleans created nodes and preserves source");
        Actor.FailFactory = true; try { Reject<ArithmeticException>(() => source.Duplicate()); using var noScriptFactory = source.Duplicate(0); Check(noScriptFactory is Entity && noScriptFactory.GetType() != typeof(Actor), "Script omission does not invoke the gameplay factory"); } finally { Actor.FailFactory = false; }
        Actor.ForcedFactoryResult = source; try { Reject<InvalidOperationException>(() => source.Duplicate()); } finally { Actor.ForcedFactoryResult = null; }
        Actor.ForcedFactoryResult = copy; try { Reject<InvalidOperationException>(() => source.Duplicate()); } finally { Actor.ForcedFactoryResult = null; }
        Check(!copy.IsDisposed && !source.IsDisposed, "Borrowed/issued factory results remain caller-owned on rejection");
        using var borrowed = new Actor { Name = "Untouched", Score = 321 };
        using var rejected = new PackedScene(); rejected.Pack(source); Actor.ForcedFactoryResult = borrowed;
        try { Reject<InvalidOperationException>(() => source.Duplicate()); Reject<InvalidOperationException>(() => rejected.Instantiate()); }
        finally { Actor.ForcedFactoryResult = null; }
        Check(!borrowed.IsDisposed && borrowed.Name == "Untouched" && borrowed.Score == 321, "Both runtime duplication and packed factories reject pre-existing identities before acquisition or restoration");
        Actor.FailConnect = true; try { Reject<ArithmeticException>(() => source.Duplicate()); } finally { Actor.FailConnect = false; }
        source.Emit(); Check(child.Calls == 2 && external.Calls == 6 && source.Calls == 100, "Source subscriptions survive failed duplication");
        Actor.MutateRead = true; try { Reject<InvalidOperationException>(() => source.Duplicate()); } finally { Actor.MutateRead = false; }
        Check(source.Name == "Source", "Capture rejects getter mutation");
        Linked(); Ownership(); Catalog(); Files();
        Console.WriteLine("Node duplication: typed hierarchies/flags, references, arrays, resources, authored subscriptions, linked templates, scene activation and failure cleanup passed.");
    }
    private static void Linked()
    {
        using var template = new Actor { Name = "Template", Score = 8 }; var child = new Actor { Name = "Inherited", Score = 9 }; template.AddChild(child); child.Owner = template; var otherChild = new Actor { Name = "Other" }; template.AddChild(otherChild); otherChild.Owner = template;
        using var scene = new PackedScene(); scene.Pack(template); using var source = (Actor)scene.Instantiate();
        source.Score = 81; ((Actor)source.GetChild(0)).Score = 91;
        var added = new Actor { Name = "Added", Score = 10 }; source.AddChild(added);
        var hidden = new Actor { Name = "HiddenAddition", Score = 11 }; source.GetChild(0).AddChild(hidden);
        using var copy = (Actor)source.Duplicate(); Check(copy.Score == 81 && ((Actor)copy.GetNode("Inherited")).Score == 91 && copy.GetChildCount() == 3 && ((Actor)copy.GetNode("Inherited/HiddenAddition")).Score == 11, "Linked overrides and additions at every depth");
        using var plain = source.Duplicate(Flags.UseInstantiation); Check(plain.GetType() == typeof(Actor) && plain.GetNode("Added").GetType() == typeof(Entity), "Template scripts remain while fresh additions omit compiled scripts");
        var replaced = source.GetNode("Inherited"); var other = new OtherActor { Name = "Inherited", Role = 31 }; replaced.ReplaceBy(other); replaced.Dispose();
        using var overridden = source.Duplicate(); Check(overridden.GetNode("Inherited") is OtherActor { Role: 31 }, "Scripts overrides a template compiled role with the exact source type");
        using var retained = source.Duplicate(Flags.UseInstantiation); Check(retained.GetNode("Inherited").GetType() == typeof(Actor), "Omitting Scripts keeps the template role and drops unavailable source-only fields");
        source.GetNode("Inherited").Name = "Renamed"; using var renamed = source.Duplicate(); Check(renamed.GetNode("Renamed") is OtherActor && !renamed.HasNode("Inherited") && renamed.GetChildCount() == source.GetChildCount(), "Renamed ordinary additions and replaced roles survive linkage without stale template nodes");
        source.MoveChild(source.GetNode("Other"), 0); using var reordered = source.Duplicate(); Check(reordered.GetChild(0).Name == "Other" && reordered.GetChild(1).Name == "Renamed", "Inherited source sibling order survives template instantiation");
        source.GetNode("Renamed").Reparent(source.GetNode("Other"), keepGlobalTransform: false); using var reparented = source.Duplicate();
        Check(reparented.GetNode("Other/Renamed") is OtherActor && !reparented.HasNode("Renamed") && reparented.GetNode("Other/Renamed/HiddenAddition") is Actor, "Reparented inherited identities and their additions reconstruct at the actual source parent");
    }
    private static void Ownership()
    {
        var source = new Actor(); var resource = new Gradient(); source.Asset = resource; source.AdoptSceneResources([resource]);
        var one = (Actor)source.Duplicate(Flags.Scripts); var two = (Actor)one.Duplicate(Flags.Scripts);
        source.Dispose(); one.Dispose(); Check(!resource.IsDisposed && two.Asset == resource, "Shared owned resource survives source and intermediate copy disposal");
        two.Dispose(); Check(resource.IsDisposed, "Last duplicate releases shared owner");
        var template = new Actor(); var local = new Gradient { ResourceLocalToScene = true }; template.Asset = local;
        using var scene = new PackedScene(); scene.Pack(template); template.Dispose();
        var instance = (Actor)scene.Instantiate(); var runtimeResource = instance.Asset!; var copy = (Actor)instance.Duplicate(); instance.Dispose(); Check(!runtimeResource.IsDisposed && copy.Asset == runtimeResource, "Real scene-local resource borrowing keeps ownership"); copy.Dispose(); Check(runtimeResource.IsDisposed, "Scene-local resource retires at last duplicate"); local.Dispose();
        var ancestor = new Node(); var asset = new Gradient(); ancestor.AdoptSceneResources([asset]); var child = new Actor { Asset = asset }; ancestor.AddChild(child); child.Owner = ancestor;
        var subtree = (Actor)child.Duplicate(Flags.Scripts); ancestor.Dispose(); Check(!asset.IsDisposed && subtree.Asset == asset, "Copied child retains ancestor-owned resources"); subtree.Dispose(); Check(asset.IsDisposed, "Ancestor ownership retires after the subtree copy");
        ancestor = new Node(); asset = new Gradient(); ancestor.AdoptSceneResources([asset]); child = new Actor { Asset = asset }; ancestor.AddChild(child); child.Owner = ancestor; ancestor.RemoveChild(child);
        subtree = (Actor)child.Duplicate(Flags.Scripts); ancestor.Dispose(); child.Dispose(); Check(!asset.IsDisposed, "Detached child retains its authoring-owner resource lease"); subtree.Dispose(); Check(asset.IsDisposed, "Detached owner lease retires");
    }
    private static void Catalog()
    {
        var count = 0;
        foreach (var type in ResourceLoader.Runtime.FileTypes.ByType.Values.Where(type => typeof(Node).IsAssignableFrom(type.Type) && type.Type.Assembly == typeof(Node).Assembly).ToArray())
        {
            try
            {
                using var source = (Node)type.Factory(); using var copy = source.Duplicate();
                Check(copy.GetType() == source.GetType() && copy.Parent is null && copy.InstanceID != source.InstanceID, "Factory family " + type.Type.Name); count++;
            }
            catch (Exception error) { throw new InvalidOperationException("Node duplicate factory family " + type.Type.Name, error); }
        }
        Console.WriteLine("Node duplication factory families: " + count);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Node duplication: " + message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
