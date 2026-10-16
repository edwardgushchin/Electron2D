using Electron2D;
using Path = System.IO.Path;

internal static partial class CompressedArrayTests
{
    private sealed class Patch : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 24, 24), Colors.White);
    }

    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-array-render-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "layers.ctexarray");
            using var first = Source(0); using var second = Source(1);
            File.WriteAllBytes(path, Container([(first, 0, null), (second, 1, null)]));
            using var array = new CompressedTextureArray(); array.Load(path); var identity = array.GetRID();
            using var shader = TextureArrayTests.Load("ImportedArray"); shader.SetDefaultLayeredTextureParameter("layers", array);
            using var leftMaterial = new ShaderMaterial { Shader = shader }; using var rightMaterial = new ShaderMaterial { Shader = shader };
            rightMaterial.SetShaderLayeredParameter("layers", array);
            var root = new Window { Size = new(64, 32) };
            root.AddChild(new Patch { Position = new(4, 4), Material = leftMaterial }); root.AddChild(new Patch { Position = new(36, 4), Material = rightMaterial });
            if (backend == "compatibility")
            {
                Reject<NotSupportedException>(() => Engine.Run(root)); Check(root.IsDisposed && RenderingServer.Service is null, "Compatibility shader gate releases the host.");
                Console.WriteLine("Compressed array shader sampling explicitly rejected on Compatibility; host cleanup passed."); return;
            }
            var frames = 0; long before = 0, allocated = 0; float exposure = 1;
            root.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == "gpu" && RenderingServer.GetCurrentRenderingDriverName() != "software", "Actual GPU backend selected.");
                RenderingServer.SetDefaultClearColor(Colors.Black);
                Check(RenderingServer.TextureGetPath(identity) == path, "Direct load supplies a renderer path.");
                array.SetPathCache("res://imported-array"); Check(RenderingServer.TextureGetPath(identity) == "res://imported-array", "Resource path supersedes the load label.");
                array.SetPathCache(""); Check(RenderingServer.TextureGetPath(identity) == "", "Clearing ResourcePath clears the renderer label.");
                root.Tree!.ProcessFrameStarted += _ =>
                {
                    before = GC.GetAllocatedBytesForCurrentThread();
                    Parameters(leftMaterial, frames % 2, frames % 3, exposure); Parameters(rightMaterial, (frames + 1) % 2, 2 - frames % 3, exposure);
                };
                RenderingServer.FramePostDraw += () =>
                {
                    var bytes = GC.GetAllocatedBytesForCurrentThread() - before; frames++; if (frames > 24) allocated += bytes;
                    if (frames == 1)
                    {
                        VerifyFrame(root, first, second, 0, 0, 1, 2, 1);
                        var device = RenderingServer.GetRenderingDevice()!; var old = RenderingServer.TextureGetRDTexture(identity);
                        Check(device.TextureGetData(old, 0).AsSpan().SequenceEqual(first.GetData()) && device.TextureGetData(old, 1).AsSpan().SequenceEqual(second.GetData()), "Physical uploaded layers retain every mip.");
                        File.WriteAllBytes(path, Container([(second, 2, null), (first, 0, null)])); array.Load(path);
                        var replacement = RenderingServer.TextureGetRDTexture(identity);
                        Check(array.GetRID() == identity && replacement != old && !device.TextureIsValid(old), "File reload retains resource RID and expires replaced device storage.");
                        Check(device.TextureGetData(replacement, 0).AsSpan().SequenceEqual(second.GetData()), "Replacement is visible to the retained renderer.");
                    }
                    if (frames == 2)
                    {
                        VerifyFrame(root, second, first, 1, 1, 0, 1, 1);
                        using var hdr = HDRSource(); File.WriteAllBytes(path, Container([(hdr, 0, null), (hdr, 0, null)])); array.Load(path); exposure = .1f;
                        var device = RenderingServer.GetRenderingDevice()!; var storage = RenderingServer.TextureGetRDTexture(identity);
                        Check(device.TextureGetData(storage, 0).AsSpan().SequenceEqual(hdr.GetData()), "HDR upload preserves values above one and all mips.");
                    }
                    if (frames == 3)
                    {
                        using var hdr = HDRSource(); VerifyFrame(root, hdr, hdr, 0, 2, 1, 0, .1f);
                        File.WriteAllBytes(path, Container([(first, 1, null), (second, 2, null)])); array.Load(path); exposure = 1;
                    }
                    if (frames == 16) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
                    if (frames != 88) return;
                    VerifyFrame(root, first, second, 1, 0, 0, 2, 1);
                    Check(allocated == 0, $"Prepared compressed-array frames allocated {allocated} owner-thread bytes."); root.Tree!.Quit();
                };
            };
            Engine.Run(root); Check(root.IsDisposed && RenderingServer.Service is null && frames == 88, "GPU array lifecycle.");
            Console.WriteLine($"Compressed arrays: actual GPU layer/mip samples, raw/PNG/WebP reloads, HDR, default/override samplers, RD readback and expiration; 24 warmup + 64 active frames, {allocated} owner-thread bytes.");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Parameters(ShaderMaterial material, int layer, int mip, float exposure)
    {
        material.SetShaderParameter("selectedLayer", (float)layer); material.SetShaderParameter("selectedMip", (float)mip); material.SetShaderParameter("exposure", exposure);
    }

    private static Image HDRSource()
    {
        using var image = Image.CreateEmpty(7, 5, true, Image.Format.Rgbaf); var data = image.GetData();
        for (var mip = 0; mip <= image.MIPMapCount; mip++)
        {
            using var level = Image.CreateEmpty(Math.Max(1, 7 >> mip), Math.Max(1, 5 >> mip), false, Image.Format.Rgbaf);
            level.Fill(new(1.5f + mip, 2.5f + mip, .5f, 1)); level.GetData().CopyTo(data, image.GetMIPMapOffset(mip));
        }
        return Image.CreateFromData(7, 5, true, Image.Format.Rgbaf, data);
    }

    private static void VerifyFrame(Window root, Image first, Image second, int leftLayer, int leftMip, int rightLayer, int rightMip, float exposure)
    {
        using var frame = root.GetTexture().GetImage()!;
        Pixel(16, leftLayer == 0 ? first : second, leftMip); Pixel(48, rightLayer == 0 ? first : second, rightMip);
        void Pixel(int x, Image source, int mip)
        {
            var data = source.GetData(); var offset = mip == 0 ? 0 : source.GetMIPMapOffset(mip);
            using var level = Image.CreateFromData(Math.Max(1, source.Width >> mip), Math.Max(1, source.Height >> mip), false, source.PixelFormat, data.AsSpan(offset, (mip == source.MIPMapCount ? data.Length : source.GetMIPMapOffset(mip + 1)) - offset).ToArray());
            var expected = level.GetPixel(0, 0); var actual = frame.GetPixel(x, 16);
            Check(Math.Abs(actual.R - expected.R * exposure) < .02f && Math.Abs(actual.G - expected.G * exposure) < .02f && Math.Abs(actual.B - expected.B * exposure) < .02f, $"Imported array frame {x}: {actual}, expected {expected} at exposure {exposure}.");
        }
    }
}
