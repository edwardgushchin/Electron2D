using Electron2D;
using Electron2D.Examples.PrimitiveMeshes;

ResourceFileTypes.RegisterResource("example.DiskMesh", DiskMesh.Create);
var capture = Array.IndexOf(args, "--capture");
if (args.Contains("--compatibility")) ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
using var mesh = new DiskMesh { Radius = 70, AddUV2 = true };
using var window = new Window { Size = new(320, 240), Title = "Primitive mesh: arrows change radius and segments; Esc closes" };
window.AddChild(new MeshInstance { Mesh = mesh, Position = new(160, 120), Modulate = new(.2f, .7f, 1) });
window.Ready += _ => RenderingServer.SetDefaultClearColor(new(.04f, .04f, .06f));
window.AddChild(new DiskControls(mesh));
Action? captureFrame = null;
if (capture >= 0)
{
    if (capture + 1 >= args.Length) throw new ArgumentException("--capture requires a PNG path.");
    var frames = 0;
    captureFrame = () => { if (++frames != 3) return; using var image = window.GetTexture().GetImage() ?? throw new InvalidOperationException("No rendered image."); image.SavePNG(args[capture + 1]); window.Tree!.Quit(); };
    window.Ready += _ => RenderingServer.FramePostDraw += captureFrame;
}
return Engine.Run(window);

internal sealed class DiskControls(DiskMesh mesh) : Node
{
    protected override void OnUnhandledKeyInput(InputEventKey key)
    {
        if (!key.Pressed || key.Echo) return;
        switch (key.Keycode)
        {
            case Key.Left: mesh.Segments = Math.Max(3, mesh.Segments - 1); break;
            case Key.Right: mesh.Segments = Math.Min(4096, mesh.Segments + 1); break;
            case Key.Up: mesh.Radius = Math.Min(100, mesh.Radius + 5); break;
            case Key.Down: mesh.Radius = Math.Max(5, mesh.Radius - 5); break;
            case Key.Escape: Tree!.Quit(); break;
        }
    }
}
