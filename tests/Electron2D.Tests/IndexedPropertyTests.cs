using Electron2D;
using System.Text.Json;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static class IndexedPropertyTests
{
    private sealed class Data : Resource { internal float Value; }
    private sealed class Host : Node { internal Data? Data; internal Vector2 Pair = new(2, 3); internal int[] Items = [1, 2]; internal Dictionary<string, int> Values = new(StringComparer.OrdinalIgnoreCase) { ["One"] = 1 }; internal bool RejectWrites, FailAfterWrite; internal int Writes; }
    private static readonly PropertyDescriptor<Host, Data?> Link = new("Data", n => n.Data, (n, v) => { n.Data = v; n.Writes++; if (n.FailAfterWrite) throw new ArithmeticException(); });
    private static readonly PropertyDescriptor<Data, float> Value = new("Value", n => n.Value, (n, v) => n.Value = v, validator: static (_, v) => float.IsFinite(v));
    private static readonly PropertyDescriptor<Host, Vector2> Pair = new("Pair", n => n.Pair, (n, v) => n.Pair = v, validator: static (n, v) => !n.RejectWrites && v.IsFinite());
    private static readonly PropertyDescriptor<Host, int[]> Items = new("Items", n => n.Items, (n, v) => n.Items = v, validator: static (n, _) => !n.RejectWrites);
    private static readonly PropertyDescriptor<Host, Dictionary<string, int>> Values = new("Values", n => n.Values, (n, v) => n.Values = v, validator: static (n, _) => !n.RejectWrites);
    private static readonly PropertyDescriptor<Sprite, Vector2> Position = new(nameof(Sprite.Position), n => n.Position, (n, v) => n.Position = v);
    private static readonly PropertyDescriptor<Sprite, AtlasTexture?> Atlas = new(nameof(Sprite.Texture), n => n.Texture as AtlasTexture, (n, v) => n.Texture = v);
    private static readonly PropertyDescriptor<AtlasTexture, Rect2> Region = new(nameof(AtlasTexture.Region), n => n.Region, (n, v) => n.Region = v);
    private static readonly PropertyDescriptor<Sprite, float> PositionX = PropertyDescriptor.Member(Position, "X", static p => p.X, static (p, x) => p with { X = x });
    private static readonly PropertyDescriptor<Sprite, float> RegionX = PropertyDescriptor.Member(PropertyDescriptor.Member(PropertyDescriptor.Through(Atlas, Region), "Position", static r => r.Position, static (r, p) => r with { Position = p }), "X", static p => p.X, static (p, x) => p with { X = x });
    internal static void Run()
    {
        using var root = new Host(); using var first = new Data { Value = 1 }; using var second = new Data { Value = 4 }; root.Data = first;
        var leaf = PropertyDescriptor.Through(Link, Value); Check(root.GetIndexed(leaf) == 1, "Object path reads."); root.SetIndexed(leaf, 3); Check(first.Value == 3 && root.Writes == 1, "Leaf then parent writes."); root.Data = second; root.SetIndexed(leaf, 5); Check(first.Value == 3 && second.Value == 5, "Current reference is resolved each time.");
        Reject<ArgumentOutOfRangeException>(() => root.SetIndexed(leaf, float.NaN)); Check(second.Value == 5, "Leaf validation preserves value.");
        root.FailAfterWrite = true; Reject<ArithmeticException>(() => root.SetIndexed(leaf, 6)); Check(second.Value == 6, "Outer notification failure preserves committed inner write."); root.FailAfterWrite = false;
        root.Data = null; Reject<InvalidOperationException>(() => root.GetIndexed(leaf)); root.Data = second;
        var dead = new Data(); root.Data = dead; dead.Dispose(); Reject<ObjectDisposedException>(() => root.GetIndexed(leaf)); root.Data = second;
        var readonlyLink = new PropertyDescriptor<Host, Data?>("ReadonlyLink", n => n.Data); var readonlyLeaf = PropertyDescriptor.Through(readonlyLink, Value); Check(readonlyLeaf.IsReadOnly && root.GetIndexed(readonlyLeaf) == 6, "Readonly chain metadata and reading."); Reject<InvalidOperationException>(() => root.SetIndexed(readonlyLeaf, 7)); Check(second.Value == 6, "Readonly writes do not touch leaf.");
        var x = PropertyDescriptor.Member(Pair, "X", static p => p.X, static (p, value) => p with { X = value }); root.SetIndexed(x, 8); Check(root.Pair == new Vector2(8, 3), "Struct copy updates one component."); root.RejectWrites = true; Reject<ArgumentOutOfRangeException>(() => root.SetIndexed(x, 9)); Check(root.Pair == new Vector2(8, 3), "Parent validation before struct commitment."); root.RejectWrites = false;
        var readonlyX = PropertyDescriptor.Member(Pair, "X", static p => p.X); Reject<InvalidOperationException>(() => root.SetIndexed(readonlyX, 0)); Check(!x.IsStored && !x.CanRevert(root), "Synthetic projection has no stored/revert schema.");
        var element = PropertyDescriptor.Element(Items, 1); var original = root.Items; root.SetIndexed(element, 7); Check(original[1] == 2 && root.Items[1] == 7 && root.Items != original, "Array writes copy before parent commit."); root.RejectWrites = true; original = root.Items; Reject<ArgumentOutOfRangeException>(() => root.SetIndexed(element, 8)); Check(root.Items == original && original[1] == 7, "Array validator failure keeps original container."); root.RejectWrites = false;
        Reject<ArgumentOutOfRangeException>(() => PropertyDescriptor.Element(Items, -1)); root.Items = []; Reject<ArgumentOutOfRangeException>(() => root.GetIndexed(element)); root.Items = null!; Reject<InvalidOperationException>(() => root.SetIndexed(element, 9)); root.Items = [1, 2];
        var key = PropertyDescriptor.Key(Values, "ONE"); var map = root.Values; root.SetIndexed(key, 4); Check(map["One"] == 1 && root.Values["one"] == 4 && root.Values.Comparer == StringComparer.OrdinalIgnoreCase, "Dictionary copy preserves comparer."); var missing = PropertyDescriptor.Key(Values, "Two"); Reject<KeyNotFoundException>(() => root.GetIndexed(missing)); root.SetIndexed(missing, 2); Check(root.Values["TWO"] == 2, "Dictionary writes may add a typed key."); root.RejectWrites = true; map = root.Values; Reject<ArgumentOutOfRangeException>(() => root.SetIndexed(key, 5)); Check(root.Values == map && map["one"] == 4, "Dictionary validator failure keeps input."); root.RejectWrites = false;
        Reject<ArgumentNullException>(() => PropertyDescriptor.Key(Values, null!)); root.Values = null!; Reject<InvalidOperationException>(() => root.GetIndexed(key)); root.Values = new();
        var readonlyItems = PropertyDescriptor.Element(new PropertyDescriptor<Host, int[]>("ReadonlyItems", n => n.Items), 0); Check(readonlyItems.IsReadOnly, "Readonly array projection metadata."); Reject<InvalidOperationException>(() => root.SetIndexed(readonlyItems, 3));
        var readonlyMap = PropertyDescriptor.Key(new PropertyDescriptor<Host, Dictionary<string, int>>("ReadonlyMap", n => n.Values), "new"); Check(readonlyMap.IsReadOnly, "Readonly dictionary projection metadata."); Reject<InvalidOperationException>(() => root.SetIndexed(readonlyMap, 3));
        Reject<ArgumentNullException>(() => root.GetIndexed<Host, float>(null!)); Reject<ArgumentNullException>(() => PropertyDescriptor.Member<Host, Vector2, float>(Pair, "X", null!)); Reject<ArgumentException>(() => PropertyDescriptor.Member(Pair, " ", static p => p.X));
        using (var wrong = new Node()) Reject<ArgumentException>(() => wrong.GetIndexed(x));
        for (var i = 0; i < 16; i++) { root.SetIndexed(x, 8); root.SetIndexed(leaf, 6); }
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) { root.SetIndexed(x, 8); _ = root.GetIndexed(x); root.SetIndexed(leaf, 6); _ = root.GetIndexed(leaf); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Warm scalar/object indexed access has zero managed allocations.");
        using (var tree = new SceneTree(root))
        {
            Reject<InvalidOperationException>(() => Task.Run(() => root.GetIndexed(x)).GetAwaiter().GetResult()); Reject<InvalidOperationException>(() => Task.Run(() => root.SetIndexed(x, 2)).GetAwaiter().GetResult());
            var capture = root.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => root.SetIndexed(x, 9)); } finally { Node.EndSceneCapture(capture); }
        }
        using var sprite = new Sprite(); using (var tree = new SceneTree(sprite))
        { var tween = tree.CreateTween(); tween.TweenProperty(sprite, n => n.GetIndexed(PositionX), (n, value) => n.SetIndexed(PositionX, value), 16f, 1); tween.CustomStep(.5); Check(Math.Abs(sprite.Position.X - 8) < .001 && sprite.Position.Y == 0, "Typed projection drives existing Tween."); }
        Files(); Console.WriteLine("Indexed typed projections: objects, components, arrays/maps, validation, lifetime, readonly, live rebinding, Tween/files and zero warm scalar/object access passed.");
    }
    private static void Files()
    {
        var directory = IOPath.Combine(IOPath.GetTempPath(), "e2d-indexed-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            using var image = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red); using var texture = ImageTexture.CreateFromImage(image); using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 16, 16), ResourceLocalToScene = true };
            using var root = new Node(); var sprite = new Sprite { Name = "Picture", Texture = atlas }; root.AddChild(sprite); sprite.Owner = root; sprite.SetIndexed(RegionX, 16); sprite.SetIndexed(PositionX, 12);
            using var packed = new PackedScene(); packed.Pack(root); var path = IOPath.Combine(directory, "scene.e2dscene"); ResourceSaver.Save(packed, path, SaverFlags.BundleResources); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var instance = loaded.Instantiate(); var copy = instance.GetNode<Sprite>("Picture"); Check(copy.GetIndexed(RegionX) == 16 && copy.GetIndexed(PositionX) == 12, "Indexed changes survive scene/resource file reconstruction.");
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
                using var texture = ImageTexture.CreateFromImage(image); using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 16, 16) }; var window = new Window { Size = new(64, 48) }; var sprite = new Sprite { Texture = atlas, Centered = false, Position = new(8, 8) }; window.AddChild(sprite); window.AddChild(new Driver(window, sprite)); window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback."); RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); };
                try { Check(Engine.Run(window) == 0 && window.IsDisposed, "Native indexed host and cleanup."); } finally { if (!window.IsDisposed) window.Dispose(); }
                Console.WriteLine(JsonSerializer.Serialize(new { scenario = "indexed-property", backend, stages = 3, cleanup = window.IsDisposed }));
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Driver(Window window, Sprite sprite) : Node
    {
        protected override void OnReady() => ProcessEnabled = true;
        protected override void OnProcess(double delta)
        { RenderingServer.ForceDraw(false); Pixel(12, Colors.Red); sprite.SetIndexed(RegionX, 16); RenderingServer.ForceDraw(false); Pixel(12, Colors.Green); sprite.SetIndexed(PositionX, 32); RenderingServer.ForceDraw(false); Pixel(12, Colors.Black); Pixel(36, Colors.Green); Tree!.Quit(); }
        private void Pixel(int x, Color expected) { using var image = window.GetTexture().GetImage(); var actual = image!.GetPixel(x, 12); Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, "Indexed resource/component edits reach native pixels."); }
    }
}
