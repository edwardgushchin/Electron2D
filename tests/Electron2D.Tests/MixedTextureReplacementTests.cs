using Electron2D;
using static TextureArrayTests;

internal static class MixedTextureReplacementTests
{
    private sealed class Patch(RID texture = default) : Entity
    {
        internal int Draws { get; private set; }

        protected override void OnDraw()
        {
            Draws++;
            if (texture.IsValid()) RenderingServer.CanvasItemAddTextureRect(GetCanvasItem(), new(0, 0, 12, 12), texture);
            else DrawRect(new(0, 0, 12, 12), Colors.White);
        }
    }

    internal static void Run()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var red = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        using var green = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        red.Fill(Colors.Red);
        green.Fill(Colors.Green);
        white.Fill(Colors.White);
        using var whiteTexture = ImageTexture.CreateFromImage(white);
        using var shader = Load("ArrayGLSL");
        var root = new Window { Size = new(108, 28) };
        var patches = new List<Patch>();
        var phase = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            using var format = new RDTextureFormat
            {
                Width = 4,
                Height = 4,
                MIPMaps = 2,
                Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
                UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo,
            };
            using var view = new RDTextureView();
            var yellowDevice = device.TextureCreate(format, view);
            device.TextureClear(yellowDevice, Colors.Yellow, 0, 2, 0, 1);
            var blueDevice = device.TextureCreate(format, view);
            device.TextureClear(blueDevice, Colors.Blue, 0, 2, 0, 1);
            format.TextureType = RenderingDevice.TextureType.TextureArray;
            format.ArrayLayers = 2;
            var yellowArrayDevice = device.TextureCreate(format, view);
            device.TextureClear(yellowArrayDevice, Colors.Yellow, 0, 2, 0, 2);
            var blueArrayDevice = device.TextureCreate(format, view);
            device.TextureClear(blueArrayDevice, Colors.Blue, 0, 2, 0, 2);

            var ordinaryCPU = RenderingServer.Texture2DCreate(red);
            var ordinaryGPU = RenderingServer.TextureRDCreate(blueDevice);
            var nextGPU = RenderingServer.TextureRDCreate(yellowDevice);
            var nextCPU = RenderingServer.Texture2DCreate(green);
            var sourceProxy = RenderingServer.TextureProxyCreate(RenderingServer.TextureProxyCreate(nextGPU));
            var arrayCPU = RenderingServer.Texture2DLayeredCreate([red, red], TextureLayered.LayeredType.Array);
            var arrayGPU = RenderingServer.TextureRDCreate(blueArrayDevice);
            var nextArrayGPU = RenderingServer.TextureRDCreate(yellowArrayDevice);
            var nextArrayCPU = RenderingServer.Texture2DLayeredCreate([green, green], TextureLayered.LayeredType.Array);
            var arrayProxy = RenderingServer.TextureProxyCreate(nextArrayGPU);
            RenderingServer.TextureSetPath(nextGPU, "device-source");
            RenderingServer.TextureSetSizeOverride(nextGPU, 7, 9);
            RenderingServer.TextureSetPath(nextCPU, "image-source");
            RenderingServer.TextureSetPath(nextArrayGPU, "device-array");

            var oldOrdinaryExport = RenderingServer.TextureGetRDTexture(ordinaryCPU);
            var oldArrayExport = RenderingServer.TextureGetRDTexture(arrayCPU);
            Check(oldOrdinaryExport.IsValid() && oldArrayExport.IsValid(), "Old CPU storage has live exported device identities.");
            var dependent = RenderingServer.TextureRDCreate(oldOrdinaryExport);
            Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(ordinaryCPU, dependent));
            Check(device.TextureIsValid(oldOrdinaryExport) && RenderingServer.TextureGetRDTexture(dependent) == oldOrdinaryExport,
                "A replacement that would invalidate its own borrow fails before publication.");
            RenderingServer.FreeRID(dependent);
            Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(ordinaryCPU, nextArrayGPU));
            Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(ordinaryCPU, sourceProxy));
            var list = device.ComputeListBegin();
            Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(ordinaryCPU, nextGPU));
            device.ComputeListEnd();

            Add(ordinaryCPU);
            Add(ordinaryGPU);
            Add(sourceProxy);
            Add(nextCPU);
            AddArray(arrayCPU);
            AddArray(arrayGPU);
            AddArray(arrayProxy);

            RenderingServer.FramePostDraw += AfterFrame;
            void AfterFrame()
            {
                using var frame = root.GetTexture().GetImage()!;
                if (phase == 0)
                {
                    Pixels(frame, [Colors.Red, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red, Colors.Blue, Colors.Yellow]);
                    RenderingServer.TextureReplace(ordinaryCPU, nextGPU);
                    RenderingServer.TextureReplace(ordinaryGPU, nextCPU);
                    RenderingServer.TextureReplace(arrayCPU, nextArrayGPU);
                    RenderingServer.TextureReplace(arrayGPU, nextArrayCPU);
                    foreach (var rid in new[] { nextGPU, nextCPU, nextArrayGPU, nextArrayCPU })
                        Reject<ArgumentException>(() => RenderingServer.TextureGetFormat(rid));
                    Check(!device.TextureIsValid(oldOrdinaryExport) && !device.TextureIsValid(oldArrayExport), "Replacing CPU backing expires its borrowed uploads.");
                    Check(RenderingServer.TextureGetRDTexture(ordinaryCPU) == yellowDevice && RenderingServer.TextureGetRDTexture(sourceProxy) == yellowDevice &&
                          RenderingServer.TextureGetRDTexture(arrayCPU) == yellowArrayDevice && RenderingServer.TextureGetRDTexture(arrayProxy) == yellowArrayDevice,
                        "Stable RIDs and aliases borrow exactly the original device images.");
                    Check(device.TextureIsValid(blueDevice) && device.TextureIsValid(blueArrayDevice), "Replacing a borrowing wrapper leaves caller-owned images live.");
                    Check(RenderingServer.TextureGetPath(ordinaryCPU) == "device-source" && RenderingServer.TextureGetPath(ordinaryGPU) == "image-source" &&
                          RenderingServer.TextureGetPath(arrayCPU) == "device-array", "Storage metadata transfers with the replacement.");
                    var sampled = RenderingTextureRegistry.Resolve(ordinaryCPU);
                    Check(sampled.GetSize() == new Vector2(7, 9) && sampled.HasMIPMaps && sampled.MIPMapCount == 1 &&
                          RenderingServer.TextureGetFormat(ordinaryCPU) == Image.Format.Rgba8, "Device size override, mip chain and format transfer.");
                    Check(RenderingServer.Texture2DGet(ordinaryCPU) is null && RenderingServer.Texture2DLayerGet(arrayCPU, 1) is null, "Sampling-only device images transfer without readback.");
                    using var cpu = RenderingServer.Texture2DGet(ordinaryGPU)!;
                    using var layer = RenderingServer.Texture2DLayerGet(arrayGPU, 1)!;
                    Check(cpu.GetPixel(0, 0) == Colors.Green && layer.GetPixel(0, 0) == Colors.Green, "Replacing with copied storage restores CPU snapshots.");
                    Reject<InvalidOperationException>(() => RenderingServer.Texture2DUpdate(ordinaryCPU, red));
                    Reject<InvalidOperationException>(() => RenderingServer.Texture2DUpdate(arrayCPU, red, 1));
                }
                else if (phase == 1)
                {
                    Pixels(frame, [Colors.Yellow, Colors.Green, Colors.Yellow, Colors.Black, Colors.Yellow, Colors.Green, Colors.Yellow]);
                    RenderingServer.Texture2DUpdate(ordinaryGPU, red);
                    RenderingServer.Texture2DUpdate(arrayGPU, red, 1);
                }
                else
                {
                    Pixels(frame, [Colors.Yellow, Colors.Red, Colors.Yellow, Colors.Black, Colors.Yellow, Colors.Red, Colors.Yellow]);
                    Check(patches.All(patch => patch.Draws == 1), "Draw commands retained their destination objects across all storage changes.");
                    RenderingServer.FramePostDraw -= AfterFrame;
                    root.Tree!.Quit();
                }
                phase++;
            }
        };

        Check(Engine.Run(root) == 0 && phase == 3, "The native replacement fixture completed.");
        Console.WriteLine("Mixed texture replacement preserved ordinary and layered pixels, RIDs, aliases and GPU allocation lifetime.");

        Patch Add(RID texture = default)
        {
            var patch = new Patch(texture) { Position = new(2 + patches.Count * 15, 4) };
            patches.Add(patch);
            root.AddChild(patch);
            return patch;
        }

        void AddArray(RID texture)
        {
            var ownedShader = RenderingServer.ShaderCreate();
            RenderingServer.ShaderSetSPIRV(ownedShader, shader.GetSPIRV());
            RenderingServer.ShaderSetDefaultTextureParameter(ownedShader, "detailMap", whiteTexture.GetRID());
            var material = RenderingServer.MaterialCreate();
            RenderingServer.MaterialSetShader(material, ownedShader);
            RenderingServer.MaterialSetParam(material, "layers", texture);
            RenderingServer.MaterialSetParam(material, "selectedLayer", 1f);
            RenderingServer.CanvasItemSetMaterial(Add().GetCanvasItem(), material);
        }
    }

    private static void Pixels(Image image, ReadOnlySpan<Color> expected)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            var actual = image.GetPixel(7 + i * 15, 9);
            Check(Math.Abs(actual.R - expected[i].R) < .005f && Math.Abs(actual.G - expected[i].G) < .005f &&
                  Math.Abs(actual.B - expected[i].B) < .005f, $"Patch {i} expected {expected[i]} and received {actual}.");
        }
    }
}
