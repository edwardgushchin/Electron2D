using Electron2D;
using static TextureArrayTests;

internal static partial class SamplerArrayTests
{
    private static void Operations(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ImageTexture Map(int size, Color color) { using var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image); }
        TextureArray Layers(Color first, Color second)
        {
            using var a = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); using var b = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); a.Fill(first); b.Fill(second);
            var texture = new TextureArray(); texture.CreateFromImages([a, b]); return texture;
        }
        using var red = Map(2, Colors.Red); using var blue = Map(4, Colors.Blue); using var layers0 = Layers(Colors.Red, Colors.Green); using var layers1 = Layers(Colors.Blue, Colors.Yellow);
        using var shader = Shader.CreateFromSPIRV(Module(language, "SamplerOperations")); using var scalar = Shader.CreateFromSPIRV(Module("GLSL", "SamplerScalar"));
        using var arrays = new ShaderMaterial { Shader = shader }; using var reference0 = new ShaderMaterial { Shader = scalar }; using var reference1 = new ShaderMaterial { Shader = scalar };
        using var root = new Window { Size = new(144, 32) }; var nodes = new[] { new Patch { Material = arrays, Position = new(0, 4) }, new Patch { Material = reference0, Position = new(48, 4) }, new Patch { Material = reference1, Position = new(96, 4) } };
        foreach (var node in nodes) root.AddChild(node);
        Texture?[] maps = [red, blue]; TextureLayered?[] layers = [layers0, layers1]; var signed = new TextureRD[2]; var unsigned = new TextureRD[2]; var deviceTextures = new List<RID>(); RenderingDevice device = null!;
        var materials = new[] { arrays, reference0, reference1 };
        var frames = 0; var verified = new bool[24]; long before = 0, allocated = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); device = RenderingServer.GetRenderingDevice()!;
            for (var i = 0; i < 2; i++)
            {
                using var format = new RDTextureFormat { Width = 1, Height = 1, Format = RenderingDevice.DataFormat.R8G8B8A8SINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling };
                using var view = new RDTextureView(); var signedID = device.TextureCreate(format, view, [new byte[] { i == 0 ? (byte)254 : (byte)2, 0, 0, 1 }]);
                format.Format = RenderingDevice.DataFormat.R8G8B8A8UINT; var unsignedID = device.TextureCreate(format, view, [new byte[] { i == 0 ? (byte)0 : (byte)255, 0, 0, 1 }]);
                signed[i] = new TextureRD { TextureRDRID = signedID }; unsigned[i] = new TextureRD { TextureRDRID = unsignedID }; deviceTextures.Add(signedID); deviceTextures.Add(unsignedID);
            }
            arrays.SetShaderTextureParameterArray("maps", maps); arrays.SetShaderLayeredParameterArray("layers", layers); arrays.SetShaderTextureParameterArray("signedMaps", signed); arrays.SetShaderTextureParameterArray("unsignedMaps", unsigned);
            for (var i = 0; i < 2; i++)
            {
                var reference = i == 0 ? reference0 : reference1;
                reference.SetShaderParameter("maps", maps[i]); reference.SetShaderLayeredParameter("layers", layers[i]); reference.SetShaderParameter("signedMaps", signed[i]); reference.SetShaderParameter("unsignedMaps", unsigned[i]);
            }
            Reject<ArgumentException>(() => arrays.SetShaderTextureParameterArray("signedMaps", new Texture?[] { red, blue }));
            Reject<ArgumentException>(() => arrays.SetShaderLayeredParameterArray("maps", layers));
            var layeredCopy = new TextureLayered?[2]; arrays.CopyShaderLayeredParameterArray("layers", layeredCopy); Check(ReferenceEquals(layeredCopy[0], layers0) && ReferenceEquals(layeredCopy[1], layers1), "Layered descriptor array access.");
            root.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread(); var mode = frames % 12; var selected = frames / 12 % 2;
                foreach (var material in materials) { material.SetShaderParameter("mode", mode); material.SetShaderParameter("selected", selected); }
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (frames >= 48) allocated += bytes;
                if (frames < 24)
                {
                    using var image = root.GetTexture().GetImage()!; var mode = frames % 12; var selected = frames / 12 % 2;
                    for (var x = 12; x <= 17; x++)
                    {
                        var physicalX = x * image.Width / root.Size.X; var slot = mode >= 10 ? physicalX & 1 : selected;
                        var expectedX = (x + 48 + slot * 48) * image.Width / root.Size.X; var y = 16 * image.Height / root.Size.Y;
                        var actual = image.GetPixel(physicalX, y); var expected = image.GetPixel(expectedX, y);
                        Check(Math.Abs(actual.R - expected.R) < .015 && Math.Abs(actual.G - expected.G) < .015 && Math.Abs(actual.B - expected.B) < .015, $"{language} mode {mode}/{selected}, lane {physicalX}: {actual} vs independent scalar {expected}.");
                    }
                    verified[frames] = true;
                }
                if (++frames != 112) return;
                Check(verified.All(x => x), "Every operation and selected element matched the scalar oracle.");
                Check(allocated == 0, $"Prepared operation frames allocated {allocated} managed bytes.");
                foreach (var texture in deviceTextures) device.FreeRID(texture); root.Tree!.Quit();
            };
        };
        try { Engine.Run(root); Check(root.IsDisposed && frames == 112, "Operation renderer lifecycle."); }
        finally { foreach (var texture in signed) texture?.Dispose(); foreach (var texture in unsigned) texture?.Dispose(); }
        Console.WriteLine($"Sampler operations {language}: 12 operations, ordinary/layered/signed/unsigned arrays, distinct sizes and divergent quad lanes matched independent scalar shaders; 64 active={allocated} managed bytes.");
    }
}
