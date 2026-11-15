using Electron2D;

using (var viewport = new SubViewport { Size = new(200, 150) })
using (var tree = new SceneTree(viewport))
{
    var control = new Control
    {
        Position = new(60, 50),
        Size = new(20, 10),
        GrowHorizontal = GrowDirection.Begin,
        GrowVertical = GrowDirection.Both
    };
    viewport.AddChild(control);
    control.CustomMinimumSize = new(40, 30);
    tree.FlushDeferred();
    Check(control.Position == new Vector2(40, 40) && control.Size == new Vector2(40, 30), "Independent minimum growth.");
    control.CustomMaximumSize = new(8, 4);
    tree.FlushDeferred();
    Check(control.Position == new Vector2(72, 53) && control.Size == new Vector2(8, 4), "Independent maximum shrinkage.");
}

var backend = args.Length == 0 ? "gpu" : args[0];
ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
using var window = new Window { Size = new(80, 60), Borderless = true, WrapControls = true };
var outer = new MarginContainer { AnchorRight = 1, AnchorBottom = 1 };
var inner = new MarginContainer { AnchorRight = 1, AnchorBottom = 1 };
var content = new ColorRect { Color = Colors.Red, CustomMinimumSize = new(20, 10) };
window.AddChild(outer); outer.AddChild(inner); inner.AddChild(content);
var phase = 0;
var frames = 0;
var driver = "";
window.Ready += _ =>
{
    driver = RenderingServer.GetCurrentRenderingDriverName();
    Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend must execute.");
    content.CustomMinimumSize = new(164, 124);
    RenderingServer.FramePostDraw += Capture;
};
try { Check(Engine.Run(window) == 0 && phase == 3, "All public size propagation phases must complete."); }
finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
Console.WriteLine($"Control sizing public consumer passed: {backend}/{driver}; nested containers, window bounds and pixels.");

void Capture()
{
    Check(++frames < 120, "Window fitting timed out.");
    Vector2i expected = phase == 0 ? new(164, 124) : new(140, 100);
    if (window.Size != expected) return;
    using var image = window.GetTexture().GetImage();
    if (image is null || image.Size != expected) return;
    var point = phase == 2 ? new Vector2i(90, 70) : expected - new Vector2i(4, 4);
    Check(image.GetPixel(point.X, point.Y).IsEqualApprox(Colors.Red), "Resized child must produce actual pixels.");
    Check(DisplayServer.WindowGetSize() == expected, "Native client geometry must match.");
    if (phase == 0)
    {
        window.MaxSize = new(140, 100);
        content.CustomMinimumSize = new(200, 150);
    }
    else if (phase == 1)
    {
        content.CustomMaximumSize = new(100, 80);
    }
    else
    {
        Check(inner.GetBoundMinimumSize() == new Vector2(100, 80)
            && outer.GetBoundMinimumSize() == new Vector2(100, 80), "Maximum changes must invalidate both container levels.");
        phase++;
        window.Tree!.Quit();
        return;
    }
    phase++;
}

static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

