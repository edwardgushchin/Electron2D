using Electron2D;
using static TextureArrayTests;
using static Electron2D.RenderingDevice;

internal static partial class TextureRedrawTests
{
    private static void GPU(Window root, Control clip, Patch patch)
    {
        using var red = Solid(Colors.Red); using var white = Solid(Colors.White); using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red); using var blue = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); blue.Fill(Colors.Blue); using var array = new TextureArray(); array.CreateFromImages([image, blue]);
        foreach (var language in new[] { "GLSL", "HLSL" })
        {
            using var shader = Load("Array" + language); var material = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(material, shader.GetRID()); RenderingServer.MaterialSetParam(material, "detailMap", white.GetRID()); RenderingServer.MaterialSetParam(material, "layers", array.GetRID()); RenderingServer.CanvasItemSetMaterial(patch.GetCanvasItem(), material);
            Sample(root, clip, patch, array.GetRID(), red, default, language + " layered shader binding"); Sample(root, clip, patch, white.GetRID(), red, default, language + " ordinary shader binding");
            var a = RenderingServer.TextureProxyCreate(array.GetRID()); var b = RenderingServer.TextureProxyCreate(a); RenderingServer.MaterialSetParam(material, "layers", b);
            Sample(root, clip, patch, a, red, default, language + " middle layered proxy"); Sample(root, clip, patch, array.GetRID(), red, default, language + " layered proxy source"); RenderingServer.MaterialSetParam(material, "layers", array.GetRID()); RenderingServer.FreeRID(b); RenderingServer.FreeRID(a);
            var device = RenderingServer.GetRenderingDevice()!; using var format = new RDTextureFormat { Width = 2, Height = 2, ArrayLayers = 2, TextureType = TextureType.TextureArray, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CanUpdate }; using var view = new RDTextureView(); var nativeArray = device.TextureCreate(format, view, [image.GetData(), blue.GetData()]); var wrapped = RenderingServer.TextureRDCreate(nativeArray);
            RenderingServer.MaterialSetParam(material, "layers", wrapped); Sample(root, clip, patch, wrapped, red, default, language + " device array wrapper"); RenderingServer.MaterialSetParam(material, "layers", array.GetRID()); RenderingServer.FreeRID(wrapped); device.FreeRID(nativeArray);
            RenderingServer.CanvasItemSetMaterial(patch.GetCanvasItem(), default); RenderingServer.FreeRID(material);
            using var sampled = Load("SamplerBindings" + language + ".frag"); material = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(material, sampled.GetRID()); RenderingServer.MaterialSetParam(material, "colorMaps", (ReadOnlySpan<RID>)[red.GetRID(), white.GetRID()]); RenderingServer.CanvasItemSetMaterial(patch.GetCanvasItem(), material); Sample(root, clip, patch, white.GetRID(), red, default, language + " fixed sampler-array member"); RenderingServer.CanvasItemSetMaterial(patch.GetCanvasItem(), default); RenderingServer.FreeRID(material);
        }
        using var drawable = new DrawableTexture(); drawable.Setup(2, 2, DrawableTexture.DrawableFormat.RGBA8, Colors.Red); Sample(root, clip, patch, drawable.GetRID(), drawable, default, "drawable image");
        var retained = RenderingServer.GetRenderingDevice()!; using var allocation = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CanUpdate }; using var textureView = new RDTextureView(); var native = retained.TextureCreate(allocation, textureView, [image.GetData()]); using var resource = new TextureRD { TextureRDRID = native }; Sample(root, clip, patch, resource.GetRID(), resource, default, "borrowed device image"); var server = RenderingServer.TextureRDCreate(native); Sample(root, clip, patch, server, null, server, "server device wrapper"); patch.Visible = false; RenderingServer.FreeRID(server); retained.FreeRID(native); patch.Visible = true;
        patch.Use(red); using var light = new PointLight { Texture = white, Position = new(24, 24), TextureScale = 32 }; root.AddChild(light); RenderingServer.TextureSetForceRedrawIfVisible(white.GetRID(), true); Settle(); Check(RenderingServer.HasChanged(), "A submitted light mask observes its image policy.");
        for (var i = 0; i < 16; i++) RenderingServer.ForceDraw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) RenderingServer.ForceDraw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared flagged light masks allocate zero owner bytes.");
        light.Enabled = false; Settle(); Check(!RenderingServer.HasChanged(), "A disabled light stops consuming its mask policy."); RenderingServer.TextureSetForceRedrawIfVisible(white.GetRID(), false); root.RemoveChild(light); light.Dispose();
    }
}
