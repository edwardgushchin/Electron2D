using System.Text.Json;
using Electron2D;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static class SubtreeResourceAuthoringTests
{
    private sealed class Holder : Node
    {
        internal Resource? Asset; internal int Reads;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<Holder, Resource?>("Asset", n => { n.Reads++; return n.Asset; }), new PropertyDescriptor<Holder, int>("Failure", _ => throw new ArithmeticException())]);
    }
    internal static void Run()
    {
        using var root = new Node { Name = "World" }; using var shape = new CircleShape { Radius = 5 }; using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image); using var atlas = new AtlasTexture { Atlas = texture };
        var sprite = new Sprite { Name = "Picture", Texture = atlas }; root.AddChild(sprite); sprite.Owner = root;
        var holder = new Holder { Name = "Holder", Asset = shape }; root.AddChild(holder);
        var nil = new Sprite { Name = "Empty" }; root.AddChild(nil);
        var found = root.GetNodeAndResource("Picture:Texture:Atlas:ResourcePath:unvalidated"); Check(found.Node == sprite && found.Resource == texture && found.RemainingPath == ":ResourcePath:unvalidated", "Last resource and unvalidated property remainder.");
        Check(root.HasNodeAndResource("Picture:Texture:Atlas") && root.GetNodeAndResource("Picture:Texture:Atlas").Resource == texture, "Complete resource chain.");
        Check(root.GetNodeAndResource("Picture:Texture:").Resource == atlas && root.GetNodeAndResource("Picture:").Node == sprite, "Trailing property separator is accepted.");
        Check(root.GetNode("Picture:Missing") == sprite && root.HasNode("Picture:Missing"), "Sibling node APIs use only node names.");
        Check(sprite.GetNodeAndResource(":Texture").Resource == atlas && sprite.GetNode(":Texture") == sprite, "Omitted node portion addresses self.");
        Check(root.GetNodeAndResource("Picture") == (sprite, null, ""), "Node-only path.");
        Check(root.GetNodeAndResource("Missing:Texture") == (null, null, "") && !root.HasNodeAndResource("Missing:Texture"), "Missing node.");
        found = root.GetNodeAndResource("Picture:Texture:Missing"); Check(found.Node is null && found.Resource == atlas && found.RemainingPath == "" && !root.HasNodeAndResource("Picture:Texture:Missing"), "Missing nested property keeps prior resource.");
        found = root.GetNodeAndResource("Empty:Texture:NotValidated"); Check(found.Node == nil && found.Resource is null && found.RemainingPath == ":Texture:NotValidated" && root.HasNodeAndResource("Empty:Texture:NotValidated"), "Null is a successful property remainder.");
        Check(root.GetNodeAndResource("Picture:Position:X").RemainingPath == ":Position:X", "Non-resource value component remainder.");
        Check(root.GetNodeAndResource("Holder:Asset:Radius").Resource == shape && holder.Reads == 1, "Custom typed getter executes once.");
        Reject<ArithmeticException>(() => root.HasNodeAndResource("Holder:Failure"));
        foreach (var path in new[] { "", " ", ":", "Picture::Texture", "Picture::" }) { Reject<ArgumentException>(() => root.GetNodeAndResource(path)); Reject<ArgumentException>(() => root.HasNodeAndResource(path)); }
        Reject<ArgumentNullException>(() => root.GetNodeAndResource(null!));
        var disposed = new CircleShape(); holder.Asset = disposed; disposed.Dispose(); Reject<ObjectDisposedException>(() => root.GetNodeAndResource("Holder:Asset")); holder.Asset = shape;
        var child = new Node { Name = "Child" }; root.AddChild(child); var grandchild = new Node { Name = "Grandchild" }; child.AddChild(grandchild); var hidden = new Node { Name = "Internal" }; child.AddChild(hidden, internalMode: Node.InternalMode.Front);
        var order = new List<string>(); root.PropagateCall(n => order.Add(n.Name)); Check(string.Join(',', order) == "Picture,Holder,Empty,Internal,Grandchild,Child,World", "Children-first order includes internal children.");
        order.Clear(); root.PropagateCall(n => order.Add(n.Name), parentFirst: true); Check(string.Join(',', order) == "World,Picture,Holder,Empty,Child,Internal,Grandchild", "Parent-first sibling order.");
        var before = sprite.Position; root.PropagateCall<Sprite, Vector2>(static (n, amount) => n.Position += amount, new(8, 4)); Check(sprite.Position == before + new Vector2(8, 4) && nil.Position == new Vector2(8, 4), "Typed role filter and argument value.");
        root.PropagateCall(n => { using var extra = new Node(); Reject<InvalidOperationException>(() => n.AddChild(extra)); if (n.Parent is { } parent) { Reject<InvalidOperationException>(() => parent.RemoveChild(n)); Reject<InvalidOperationException>(() => n.Dispose()); } });
        order.Clear(); Reject<AggregateException>(() => root.PropagateCall(n => { order.Add(n.Name); if (n == sprite || n == child) throw new ArithmeticException(n.Name); }, true)); Check(order.Count == 7, "Failures preserve remaining traversal.");
        var extraAfter = new Node(); root.AddChild(extraAfter); root.RemoveChild(extraAfter); extraAfter.Dispose();
        Reject<ArgumentNullException>(() => root.PropagateCall((Action<Node>)null!));
        var count = 0; root.PropagateCall<Node, Action>(static (_, run) => run(), () => count++); Check(count == 7, "Typed argument reference identity.");
        Action<Node> empty = static _ => { }; for (var i = 0; i < 16; i++) root.PropagateCall(empty); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) root.PropagateCall(empty); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Warmed propagation allocates zero managed bytes.");
        using (var tree = new SceneTree(root))
        {
            sprite.UniqueNameInOwner = true; Check(root.GetNodeAndResource("%Picture:Texture:Atlas").Resource == texture && sprite.GetNodeAndResource("../Picture:Texture").Node == sprite, "Unique/relative resources in live hierarchy.");
            Check(root.GetNodeAndResource("/World/Picture:Texture").Resource == atlas, "Absolute resource path in active tree.");
            Reject<InvalidOperationException>(() => Task.Run(() => root.GetNodeAndResource("Picture:Texture")).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => root.PropagateCall(empty)).GetAwaiter().GetResult());
        }
        Files(); Console.WriteLine("Subtree authoring: typed operations, nested resource paths, stored scenes, guards, failures and zero warmed propagation passed.");
    }
    private static void Files()
    {
        var directory = IOPath.Combine(IOPath.GetTempPath(), "e2d-subtree-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = IOPath.Combine(directory, "scene.e2dscene"); using var image = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red);
            using var texture = ImageTexture.CreateFromImage(image); using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 16, 16), ResourceLocalToScene = true };
            using var root = new Node { Name = "World" }; var sprite = new Sprite { Name = "Picture", Texture = atlas, Centered = false }; root.AddChild(sprite); sprite.Owner = root;
            root.PropagateCall<Sprite, Vector2>(static (n, p) => n.Position = p, new(12, 8)); var selected = (AtlasTexture)root.GetNodeAndResource("Picture:Texture:Region").Resource!; selected.Region = new(16, 0, 16, 16);
            using var packed = new PackedScene(); packed.Pack(root); ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
            using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var instance = loaded.Instantiate();
            var result = instance.GetNodeAndResource("Picture:Texture:Atlas"); Check(result.Node is Sprite { Position: var position } && position == new Vector2(12, 8) && result.Resource is ImageTexture && ((AtlasTexture)instance.GetNodeAndResource("Picture:Texture").Resource!).Region.Position == new Vector2(16, 0), "Edited hierarchy/resources survive file capture and reconstruction.");
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); using var image = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red); for (var y = 0; y < 16; y++) for (var x = 16; x < 32; x++) image.SetPixel(x, y, Colors.Green);
                using var texture = ImageTexture.CreateFromImage(image); using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 16, 16) };
                var window = new Window { Size = new(64, 48) }; var sprite = new Sprite { Name = "Picture", Texture = atlas, Centered = false }; window.AddChild(sprite); window.AddChild(new Driver(window, sprite));
                window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback."); RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); };
                try { Check(Engine.Run(window) == 0 && window.IsDisposed, "Native authoring host cleanup."); } finally { if (!window.IsDisposed) window.Dispose(); }
                Console.WriteLine(JsonSerializer.Serialize(new { scenario = "subtree-resource-authoring", backend, stages = 3, cleanup = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Driver(Window window, Sprite sprite) : Node
    {
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        {
            window.PropagateCall<Sprite, Vector2>(static (n, p) => n.Position = p, new(8, 8)); RenderingServer.ForceDraw(false); Pixel(window, 12, Colors.Red);
            var result = window.GetNodeAndResource("Picture:Texture:Region"); Check(result.Node == sprite && result.Resource is AtlasTexture && result.RemainingPath == ":Region", "Live native resource endpoint."); ((AtlasTexture)result.Resource!).Region = new(16, 0, 16, 16); RenderingServer.ForceDraw(false); Pixel(window, 12, Colors.Green);
            window.PropagateCall<Sprite>(static n => n.Position = new(32, 8)); RenderingServer.ForceDraw(false); Pixel(window, 12, Colors.Black); Pixel(window, 36, Colors.Green); Tree!.Quit();
        }
        private static void Pixel(Window window, int x, Color expected) { using var image = window.GetTexture().GetImage(); var actual = image!.GetPixel(x, 12); Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, "Edited resource/hierarchy reaches real pixels."); }
    }
}
