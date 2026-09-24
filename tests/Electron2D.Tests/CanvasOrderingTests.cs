using Mathf = Electron2D.Mathf;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasOrdering(string backend)
    {
        // All test shapes cover the sampled pixel; its color identifies the last submitted item.
        var cases = new (string Name, Func<Node> Create, Color Expected)[]
        {
            ("ordinary children", () => Branch(Box(Colors.Red), Box(Colors.Green)), Colors.Green),
            ("behind parent", () => Branch(Box(Colors.Red), Box(Colors.Green, behind: true)), Colors.Red),
            ("behind subtree", () => Branch(Box(Colors.Red), Branch(Box(Colors.Green, behind: true), Box(Colors.Blue))), Colors.Red),
            ("behind with higher Z", () => Branch(Box(Colors.Red), Box(Colors.Green, z: 1, behind: true)), Colors.Green),
            ("relative Z", () => Branch(Box(Colors.Red, z: 10), Box(Colors.Green, z: -1)), Colors.Red),
            ("absolute Z", () => Branch(Box(Colors.Red, z: 10), new OrderingBox(Colors.Green) { ZIndex = 2, ZAsRelative = false }), Colors.Red),
            ("clamped inherited Z", () => Branch(Box(Colors.Red, z: 4096), Box(Colors.Green, z: 1)), Colors.Green),
            ("top-level subtree", () => Branch(Box(Colors.Red), Box(Colors.Green, top: true), Box(Colors.Blue)), Colors.Green),
            ("top-level root order", () => Branch(new Node(), Branch(Box(Colors.Red), Box(Colors.Green, top: true)), Box(Colors.Blue)), Colors.Blue),
            ("top-level respects Z", () => Branch(Box(Colors.Red, z: 1), Box(Colors.Green, top: true)), Colors.Red),
            ("neutral canvas boundary", () => Branch(Box(Colors.Red), Branch(new Node(), Box(Colors.Green)), Box(Colors.Blue)), Colors.Green),
            ("hidden canvas ancestor", () => Branch(new OrderingBox(Colors.Red) { Visible = false }, Box(Colors.Green, top: true)), Colors.Black),
            ("neutral visibility boundary", () => Branch(new OrderingBox(Colors.Red) { Visible = false }, Branch(new Node(), Box(Colors.Green))), Colors.Green),
            ("Y ascending", () => Branch(Box(Colors.Red, sort: true), Box(Colors.Green, y: 20), Box(Colors.Blue, y: -20)), Colors.Green),
            ("Y includes parent", () => Branch(Box(Colors.Red, sort: true), Box(Colors.Blue, y: -20)), Colors.Red),
            ("Y equal retains order", () => Branch(Box(Colors.Red, sort: true), Box(Colors.Green, y: 20), Box(Colors.Blue, y: 20)), Colors.Blue),
            ("Y approximate tie", () => Branch(Box(Colors.Red, sort: true), Box(Colors.Green, y: 20.000002f), Box(Colors.Blue, y: 20)), Colors.Blue),
            ("Y overrides behind", () => Branch(Box(Colors.Red, sort: true), Box(Colors.Green, y: 20, behind: true)), Colors.Green),
            ("Z overrides Y", () => Branch(Box(Colors.Red, sort: true), Box(Colors.Green, y: 20), Box(Colors.Blue, y: -20, z: 1)), Colors.Blue),
            ("nested Y flattens", () => Branch(Box(Colors.Red, sort: true), Branch(Box(Colors.Blue, y: 20, sort: true), Box(Colors.Yellow, y: -30)), Box(Colors.Green, y: 5)), Colors.Blue),
            ("unsorted child groups subtree", () => Branch(Box(Colors.Red, sort: true), Branch(Box(Colors.Blue, y: 20), Box(Colors.Yellow, y: -30)), Box(Colors.Green, y: 5)), Colors.Yellow),
            ("nested independent group", () => Branch(Box(Colors.Red, sort: true), Branch(Box(Colors.Blue, y: 20), Branch(Box(Colors.Yellow, y: -30, sort: true), Box(Colors.Cyan, y: 10))), Box(Colors.Green, y: 5)), Colors.Cyan),
            ("Y uses local coordinates", () => Branch(new OrderingBox(Colors.Red) { YSortEnabled = true, Rotation = Mathf.Pi, Scale = new(2, 3) }, Box(Colors.Green, y: 20), Box(Colors.Blue, y: -20)), Colors.Green),
            ("nested basis composition", () => Branch(Box(Colors.Red, sort: true), Branch(new OrderingBox(Colors.Blue) { Position = new(0, 20), Rotation = Mathf.Pi / 2, YSortEnabled = true }, new OrderingBox(Colors.Yellow) { Position = new(10, 0) }), Box(Colors.Green, y: 25)), Colors.Yellow),
            ("singular Y root", () => Branch(new Entity { YSortEnabled = true, Transform = new Transform(Vector2.Right, Vector2.Zero, Vector2.Zero) }, new OrderingBox(Colors.Green) { TopLevel = true }), Colors.Green),
            ("neutral root escapes Y group", () => Branch(Box(Colors.Red, sort: true), Branch(new Node(), Box(Colors.Blue, y: -20)), Box(Colors.Green, y: 20)), Colors.Blue),
            ("top-level escapes Y group", () => Branch(Box(Colors.Red, sort: true), Box(Colors.Blue, y: -20, top: true), Box(Colors.Green, y: 20)), Colors.Blue),
            ("abstract canvas Y query", () => Branch(Box(Colors.Red, sort: true), new OrderingCanvas(), Box(Colors.Blue, y: 20)), Colors.Cyan),
            ("MoveToFront tie", () => { var first = Box(Colors.Green, y: 20); var root = Branch(Box(Colors.Red, sort: true), first, Box(Colors.Blue, y: 20)); first.MoveToFront(); return root; }, Colors.Green),
            ("drawing callback changes ordering", () => { var root = new OrderingBox(Colors.Red) { YSortEnabled = true }; root.DuringDraw = () => root.YSortEnabled = false; return Branch(root, Box(Colors.Green, y: 20), Box(Colors.Blue, y: -20)); }, Colors.Blue),
            ("drawing callback reparents canvas", () => { var root = Box(Colors.Red); var child = Box(Colors.Blue); var detachedRoot = new Node(); var green = new OrderingBox(Colors.Green) { Name = "Green" }; green.DuringDraw = () => child.Reparent(detachedRoot); root.AddChild(child); root.AddChild(green); return Branch(new Node(), root, detachedRoot); }, Colors.Blue),
        };
        var window = new Window { Size = new(64, 64) };
        var index = 0;
        Node current = cases[0].Create();
        window.AddChild(current);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!;
            server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var image = server.Readback();
                try { Pixel(image, 8, 8, cases[index].Expected); }
                catch (Exception error) { throw new InvalidOperationException($"Canvas order: {cases[index].Name} ({backend}).", error); }
                if (++index == cases.Length) { window.Tree!.Quit(); return; }
                window.RemoveChild(current); current.Dispose();
                current = cases[index].Create(); window.AddChild(current);
            };
        };
        Engine.Instance.Run(window);
        Check(index == cases.Length && current.IsDisposed, "Ordering scene lifetime.");
        Released(window);
        Console.WriteLine($"Canvas ordering pixel checks passed: {backend}, {cases.Length} cases.");
    }

    private static OrderingBox Box(Color color, float y = 0, int z = 0, bool behind = false, bool top = false, bool sort = false) =>
        new(color) { Position = new(0, y), ZIndex = z, ShowBehindParent = behind, TopLevel = top, YSortEnabled = sort };

    private static T Branch<T>(T parent, params Node[] children) where T : Node
    {
        for (var index = 0; index < children.Length; index++)
        {
            children[index].Name = $"Child{index}";
            parent.AddChild(children[index]);
        }
        return parent;
    }

    private sealed class OrderingBox(Color color) : Entity
    {
        internal Action? DuringDraw;
        protected override void OnDraw()
        {
            DuringDraw?.Invoke();
            DrawRect(new Rect(-1000, -1000, 2000, 2000), color);
        }
    }

    private sealed class OrderingCanvas : CanvasItem
    {
        public override Transform GetTransform() => new(0, new Vector2(0, 30));
        protected override void OnDraw() => DrawRect(new Rect(-1000, -1000, 2000, 2000), Colors.Cyan);
    }
}
