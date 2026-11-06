using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class StencilTexturesTests
{
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); using var array = new TextureArrayRD();
        var root = new Window { Size = new(32, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var frames = 0;
        RenderingDevice device = null!; RID image = default, layers = default, server = default;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black);
            image = Image(device, DataFormat.S8UINT, Roles | Transfers, data: [Enumerable.Repeat((byte)197, 16).ToArray()]); texture.TextureRDRID = image;
            using var snapshot = texture.GetImage()!; Check(snapshot.GetPixel(0, 0) == new Color(197, 0, 0, 1), "TextureRD stencil readback preserves raw unsigned component values.");
            layers = Image(device, DataFormat.S8UINT, Roles | Transfers, layers: 2, data: [new byte[16], Enumerable.Repeat((byte)231, 16).ToArray()]); array.TextureRDRID = layers;
            using var layer = array.GetLayerData(1)!; Check(layer.GetPixel(0, 0) == new Color(231, 0, 0, 1), "TextureArrayRD stencil readback selects exact layer bytes.");
            server = RenderingServer.TextureRDCreate(image); var borrowed = RenderingServer.TextureGetRDTexture(server);
            using var format = device.TextureGetFormat(borrowed); Check(format.Format == DataFormat.S8UINT && device.TextureGetData(borrowed, 0).All(v => v == 197), "RenderingServer texture roundtrip retains the stencil role and portable bytes.");
            root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured()
        {
            if (++frames < 3) return;
            using var screenshot = root.GetTexture().GetImage()!; var pixel = screenshot.GetPixel(8, 8);
            Check(pixel.R > .99f && pixel.G < .01f && pixel.B < .01f, "Retained canvas draws stencil storage through the unsigned native sampled view.");
            if (_output is not null) screenshot.SavePNG(System.IO.Path.Combine(_output, "stencil-canvas.png"));
            RenderingServer.FramePostDraw -= Captured; RenderingServer.FreeRID(server); texture.TextureRDRID = default; array.TextureRDRID = default; device.FreeRID(image); device.FreeRID(layers);
            root.Tree!.Quit();
        }
        Engine.Run(root);
    }
}
