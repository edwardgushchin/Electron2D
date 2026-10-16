using Electron2D;
using static TextureArrayTests;

internal static class IntegerLightingTests
{
    private sealed class Patch(Texture texture, Color color = default, int size = 8) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, size, size), false, color == default ? Colors.White : color);
    }
    private static Color Bits(Color value) => new(BitConverter.Int32BitsToSingle((int)value.R), BitConverter.Int32BitsToSingle((int)value.G), BitConverter.Int32BitsToSingle((int)value.B), BitConverter.Int32BitsToSingle((int)value.A));
    internal static void Run(bool programmable = false, bool ordinaryUnsigned = false, bool HDR = false)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(256, 128), UseHDR2D = HDR, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var ambient = HDR ? new Color(.05f, .05f, .05f).SRGBToLinear().R : .05f;
        var resources = new List<Resource>(); var native = new Texture[3, 3]; var reference = new Texture[3, 3]; var ids = new RID[3, 3];
        var values = new Color[,] { { new(1, 1, 1), new(2, -1, 1), new(2, 1, 0) }, { new(.5f, .5f, 0), new(-1, 1, 0), new(0, 0, 0) }, { new(.3f, .5f, .6f, .4f), new(-1, 2, 1, 0), new(1, 2, 0, 0) } };
        var materials = new ShaderMaterial?[3];
        if (programmable)
            for (var scalar = 0; scalar < 3; scalar++)
            {
                var shader = scalar == 0 ? Load("CanvasGLSL") : new Shader();
                if (scalar != 0) shader.SetSPIRV(scalar == 1 ? BuiltInShaders.SignedFragment : BuiltInShaders.UnsignedFragment);
                resources.Add(shader);
                materials[scalar] = new ShaderMaterial { Shader = shader }; resources.Add(materials[scalar]!);
            }
        var frames = 0; long before = 0, allocated = 0; RenderingDevice device = null!; RID maskID = default, signedMaskID = default, clipID = default, unsignedClipID = default;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.CanvasSetModulate(root.FindWorld()!.Canvas, new(.05f, .05f, .05f));
            using var format = new RDTextureFormat { Width = 4, Height = 4, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo | RenderingDevice.TextureUsageBits.CPURead }; using var view = new RDTextureView();
            Texture CPU(Color value)
            {
                using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgbaf); image.Fill(value);
                var texture = ImageTexture.CreateFromImage(image); resources.Add(texture); return texture;
            }
            TextureRD GPU(RenderingDevice.DataFormat kind, Color value, out RID rid)
            {
                format.Format = kind; rid = device.TextureCreate(format, view); device.TextureClear(rid, Bits(value), 0, 1, 0, 1);
                var texture = new TextureRD { TextureRDRID = rid }; resources.Add(texture); return texture;
            }
            for (var channel = 0; channel < 3; channel++) for (var scalar = 0; scalar < 3; scalar++)
                {
                    reference[channel, scalar] = CPU(values[channel, scalar]);
                    if (scalar == 2 && ordinaryUnsigned)
                    {
                        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba16I); image.Fill(values[channel, scalar]);
                        native[channel, scalar] = ImageTexture.CreateFromImage(image); resources.Add(native[channel, scalar]);
                    }
                    else if (scalar != 0) native[channel, scalar] = GPU(scalar == 1 ? RenderingDevice.DataFormat.R32G32B32A32SINT : RenderingDevice.DataFormat.R32G32B32A32UINT, values[channel, scalar], out ids[channel, scalar]);
                }
            for (var diffuse = 0; diffuse < 3; diffuse++) for (var normal = 0; normal < 3; normal++) for (var specular = 0; specular < 3; specular++)
                    {
                        var index = diffuse * 9 + normal * 3 + specular;
                        foreach (var baseline in new[] { false, true })
                        {
                            Texture Source(int channel, int scalar) => baseline || scalar == 0 ? reference[channel, scalar] : native[channel, scalar];
                            var canvas = new CanvasTexture { DiffuseTexture = Source(0, diffuse), NormalTexture = Source(1, normal), SpecularTexture = Source(2, specular), SpecularShininess = .2f }; resources.Add(canvas);
                            root.AddChild(new Patch(canvas) { Name = $"channels_{index}_{baseline}", Material = baseline ? null : materials[diffuse], Position = new(index % 9 * 24 + (baseline ? 12 : 2), index / 9 * 24 + 2) });
                        }
                    }
            root.AddChild(new DirectionalLight { Height = .5f, Energy = .08f, RangeZMax = 0 });
            var mask = GPU(RenderingDevice.DataFormat.R8G8B8A8UINT, Colors.White, out maskID);
            var signedMask = GPU(RenderingDevice.DataFormat.R16G16B16A16SINT, Colors.White, out signedMaskID);
            var atlas = new AtlasTexture { Atlas = mask, Region = new(1, 1, 2, 2), Margin = new(1, 1, 2, 2) }; resources.Add(atlas);
            var white = reference[0, 0];
            root.AddChild(new Patch(white) { Name = "uint_mask_surface", Position = new(4, 88), ZIndex = 2, LightMask = 2 });
            root.AddChild(new PointLight { Name = "uint_point", Texture = mask, Position = new(8, 92), TextureScale = 4, Energy = .25f, RangeItemCullMask = 2, RangeZMin = 2, RangeZMax = 2 });
            root.AddChild(new Patch(white) { Name = "sint_mask_surface", Position = new(28, 88), ZIndex = 2, LightMask = 4 });
            root.AddChild(new PointLight { Name = "sint_point", Texture = signedMask, Position = new(32, 92), TextureScale = 4, Energy = .25f, RangeItemCullMask = 4, RangeZMin = 2, RangeZMax = 2 });
            root.AddChild(new Patch(white, size: 16) { Name = "atlas_mask_surface", Position = new(48, 84), ZIndex = 2, LightMask = 8 });
            root.AddChild(new PointLight { Name = "atlas_point", Texture = atlas, Position = new(56, 92), TextureScale = 4, Energy = .25f, RangeItemCullMask = 8, RangeZMin = 2, RangeZMax = 2 });
            using var unshaded = new CanvasItemMaterial { LightMode = LightMode.Unshaded };
            // Retain this material beyond the setup callback.
            var clipMaterial = (CanvasItemMaterial)unshaded.Duplicate(); resources.Add(clipMaterial);
            var clip = GPU(RenderingDevice.DataFormat.R32G32B32A32SINT, Colors.White, out clipID);
            var clipped = new Patch(clip, new(1, 1, 1, .5f)) { Position = new(80, 88), ClipChildren = ClipChildrenMode.Only }; clipped.AddChild(new Patch(white, Colors.Red) { Material = clipMaterial }); root.AddChild(clipped);
            var unsignedClip = GPU(RenderingDevice.DataFormat.R8G8B8A8UINT, Colors.White, out unsignedClipID);
            var unsignedClipped = new Patch(unsignedClip, new(1, 1, 1, .5f)) { Name = "unsigned_clip", Position = new(104, 88), ClipChildren = ClipChildrenMode.Only }; unsignedClipped.AddChild(new Patch(white, Colors.Red) { Material = clipMaterial }); root.AddChild(unsignedClipped);
            root.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePreDraw += () =>
            {
                var value = frames % 2 == 0 ? Colors.White : new Color(0, 1, 0);
                device.TextureClear(maskID, Bits(value), 0, 1, 0, 1); device.TextureClear(signedMaskID, Bits(value), 0, 1, 0, 1);
                device.TextureClear(clipID, Bits(frames % 2 == 0 ? Colors.White : new Color(1, 1, 1, 0)), 0, 1, 0, 1);
                device.TextureClear(unsignedClipID, Bits(frames % 2 == 0 ? Colors.White : new Color(1, 1, 1, 0)), 0, 1, 0, 1);
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (frames >= 24) allocated += bytes;
                if (frames < 2 || frames == 55)
                {
                    using var pixels = root.GetTexture().GetImage()!;
                    for (var index = 0; index < 27; index++) Equal(pixels.GetPixel(index % 9 * 24 + 6, index / 9 * 24 + 6), pixels.GetPixel(index % 9 * 24 + 16, index / 9 * 24 + 6), $"Scalar combination {index}");
                    var expected = frames % 2 == 0 ? new Color(ambient + .25f, ambient + .25f, ambient + .25f) : new Color(ambient, ambient + .25f, ambient);
                    foreach (var x in new[] { 8, 32, 56 }) Equal(pixels.GetPixel(x, 92), expected, "Live point mask");
                    Equal(pixels.GetPixel(50, 86), new(ambient, ambient, ambient), "Atlas transparent margin");
                    Equal(pixels.GetPixel(84, 92), frames % 2 == 0 ? new(.5f, 0, 0) : Colors.Black, "Signed alpha clip");
                    Equal(pixels.GetPixel(108, 92), frames % 2 == 0 ? new(.5f, 0, 0) : Colors.Black, "Unsigned alpha clip");
                }
                if (frames == 2)
                {
                    device.FreeRID(maskID); Check(mask.GetSize() == Vector2.Zero, "Freed mask binding expires before replacement.");
                    using var replacementFormat = new RDTextureFormat { Width = 8, Height = 4, Format = RenderingDevice.DataFormat.R8G8B8A8UINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo };
                    using var replacementView = new RDTextureView(); maskID = device.TextureCreate(replacementFormat, replacementView); mask.TextureRDRID = maskID;
                }
                frames++; if (frames != 56) return;
                Check(allocated == 0, $"Prepared integer lighting allocated {allocated} managed bytes.");
                device.FreeRID(maskID); Check(mask.GetSize() == Vector2.Zero, "Mask source free expires borrowed dimensions.");
                root.Tree!.Quit();
            };
        };
        try { Check(Engine.Run(root) == 0 && frames == 56 && device.IsDisposed, "Integer lighting shutdown."); }
        finally { foreach (var resource in resources) resource.Dispose(); }
        RejectLinearChannels();
        Console.WriteLine($"Integer lighting: all 27 scalar channel combinations versus float reference, live UInt/SInt point masks, atlas margins, signed/unsigned alpha clipping, free/rebind resize; 24 warm + 32 active frames, {allocated} managed bytes.");
    }
    private static void RejectLinearChannels()
    {
        foreach (var normal in new[] { true, false })
        {
            using var texture = new TextureRD(); using var channels = new CanvasTexture();
            var root = new Window { Size = new(256, 128) }; RenderingDevice device = null!;
            root.Ready += _ =>
            {
                device = RenderingServer.GetRenderingDevice()!;
                using var format = new RDTextureFormat { Format = RenderingDevice.DataFormat.R32G32B32A32SINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling }; using var view = new RDTextureView(); texture.TextureRDRID = device.TextureCreate(format, view);
                if (normal) channels.NormalTexture = texture; else channels.SpecularTexture = texture;
                root.AddChild(new Patch(channels)); root.AddChild(new DirectionalLight());
            };
            Reject<NotSupportedException>(() => Engine.Run(root)); Check(device.IsDisposed && root.IsDisposed && RenderingServer.Service is null, "Linear integer channel rejects before submission and closes renderer.");
        }
    }
    private static void Equal(Color actual, Color expected, string name) => Check(Math.Abs(actual.R - expected.R) < .012f && Math.Abs(actual.G - expected.G) < .012f && Math.Abs(actual.B - expected.B) < .012f, $"{name}: {actual} != {expected}.");
}
