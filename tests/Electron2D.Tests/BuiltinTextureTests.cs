using Electron2D;
using static MovieWriterTests;

internal static partial class RenderingRuntimeTests
{
    internal static void RunBuiltinTextures(string profile)
    {
        Reject<InvalidOperationException>(() => RenderingServer.GetTestTexture());
        Reject<InvalidOperationException>(() => RenderingServer.GetWhiteTexture());
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "gpu" ? "gpu" : "compatibility"); Engine.MaxFPS = 0;
        RID test = default, white = default, proxy = default;
        try
        {
            var root = new Window { Title = "Builtin texture " + profile, Size = new(576, 256) };
            var node = new CanvasNode { TextureFilter = TextureFilter.Nearest }; root.AddChild(node);
            RID shader = default, material = default; var frames = 0; var phase = 0; var cycles = 0; long before = 0, active = -1, idle = -1;
            void CheckPattern(RID rid)
            {
                using var image = RenderingServer.Texture2DGet(rid)!;
                Check(image.Size == new Vector2i(256, 256) && image.PixelFormat == Image.Format.Rgb8 && !image.HasMIPMaps, "Exact RGB8 pattern storage without mipmaps.");
                Check(image.GetPixel(0, 0) == new Color(0x00ff00ff) && image.GetPixel(2, 2) == new Color(0xfd0202ff) && image.GetPixel(32, 17) == new Color(0x11e720ff) && image.GetPixel(255, 255) == new Color(0x00ffffff), "Grid, gradient, integer rounding and final pixel match the pinned pattern.");
                image.Fill(Colors.Magenta);
            }
            void CheckWhite(RID rid)
            {
                using var image = RenderingServer.Texture2DGet(rid)!;
                Check(image.Size == new Vector2i(4, 4) && image.PixelFormat == Image.Format.Rgb8 && image.GetData().All(x => x == 255), "Exact opaque 4x4 RGB8 white storage.");
            }
            void Pixels(Color expectedWhite, bool proxyLive = true)
            {
                using var image = root.GetTexture().GetImage()!;
                Check(image.GetPixel(4, 8) == new Color(0xf90408ff) && image.GetPixel(264, 8) == expectedWhite, "Live canvas draws cached source and updated white pixels.");
                Check(image.GetPixel(324, 8) == (proxyLive ? new Color(0xf90408ff) : Colors.Black), "Proxy follows replacement, retirement and reconnection.");
                if (profile == "gpu" && (expectedWhite == Colors.White || expectedWhite == Colors.Red))
                    Check(image.GetPixel(264, 104) == expectedWhite, "Named shader sampler consumes the same cached native texture.");
            }
            void After()
            {
                if (++frames > 220) throw new TimeoutException("Builtin texture lifecycle phases did not finish.");
                Check(RenderingServer.GetTestTexture() == test && RenderingServer.GetWhiteTexture() == white, "Cache identity remains stable during ordinary frames.");
                switch (phase)
                {
                    case 0:
                        Pixels(Colors.White); CheckPattern(test); CheckWhite(white);
                        using (var red = Image.CreateEmpty(4, 4, false, Image.Format.Rgb8)) { red.Fill(Colors.Red); RenderingServer.Texture2DUpdate(white, red); }
                        phase++; break;
                    case 1:
                        {
                            Pixels(Colors.Red);
                            var consumed = test; RenderingServer.TextureReplace(white, test);
                            Reject<ArgumentException>(() => RenderingServer.Texture2DGet(consumed));
                            test = RenderingServer.GetTestTexture(); Check(test != consumed, "Consuming a cached source invalidates only that cache.");
                            if (profile == "gpu") RenderingServer.MaterialSetParam(material, "detailMap", test);
                            node.QueueRedraw();
                            Check(RenderingServer.GetWhiteTexture() == white, "Replacement preserves the cached destination identity."); CheckPattern(white);
                            phase++; break;
                        }
                    case 2:
                        {
                            using (var image = root.GetTexture().GetImage()!) Check(image.GetPixel(324, 8) == new Color(0xf90408ff), "Existing proxy redirects to replacement destination.");
                            var retiredTest = test; var retiredWhite = white;
                            RenderingServer.FreeRID(test); RenderingServer.FreeRID(white);
                            Reject<ArgumentException>(() => RenderingServer.Texture2DGet(retiredTest)); Reject<ArgumentException>(() => RenderingServer.Texture2DGet(retiredWhite));
                            test = RenderingServer.GetTestTexture(); white = RenderingServer.GetWhiteTexture();
                            Check(test != retiredTest && white != retiredWhite && test != white, "Explicit retirement recreates both textures with fresh distinct identities.");
                            CheckPattern(test); CheckWhite(white);
                            if (profile == "gpu") { RenderingServer.MaterialSetParam(material, "colorMap", white); RenderingServer.MaterialSetParam(material, "detailMap", test); }
                            node.QueueRedraw(); phase++; break;
                        }
                    case 3:
                        Pixels(Colors.White, false); RenderingServer.TextureProxyUpdate(proxy, test); phase++; break;
                    case 4:
                        Pixels(Colors.White); if (++cycles < 32) break;
                        cycles = 0; before = GC.GetAllocatedBytesForCurrentThread(); phase++; break;
                    case 5:
                        if (++cycles < 64) break;
                        active = GC.GetAllocatedBytesForCurrentThread() - before; node.Hide(); cycles = 0; phase++; break;
                    case 6:
                        if (++cycles < 16) break;
                        cycles = 0; before = GC.GetAllocatedBytesForCurrentThread(); phase++; break;
                    case 7:
                        if (++cycles < 64) break;
                        idle = GC.GetAllocatedBytesForCurrentThread() - before;
                        Check(active == 0 && idle == 0, $"Prepared cache queries and active/idle frames allocate zero managed owner bytes: {active}/{idle}.");
                        RenderingServer.FramePostDraw -= After; root.Tree!.Quit(); break;
                }
            }
            node.DrawAction = item => { Check(RenderingServer.GetTestTexture() == test && RenderingServer.GetWhiteTexture() == white, "Warm cache queries are usable inside canvas authoring."); item.DrawTextureRect(test, new(0, 0, 256, 256), false); item.DrawTextureRect(white, new(256, 0, 64, 64), false); item.DrawTextureRect(proxy, new(320, 0, 256, 256), false); };
            root.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.GetWhiteTexture()).GetAwaiter().GetResult());
                test = RenderingServer.GetTestTexture(); white = RenderingServer.GetWhiteTexture(); proxy = RenderingServer.TextureProxyCreate(test);
                Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.GetTestTexture()).GetAwaiter().GetResult());
                if (profile == "gpu")
                {
                    using var stream = typeof(RenderingRuntimeTests).Assembly.GetManifestResourceStream("TestShaders.TextureHlsl.spv")!;
                    using var bytes = new MemoryStream(); stream.CopyTo(bytes);
                    shader = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(shader, bytes.ToArray()); material = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(material, shader);
                    RenderingServer.MaterialSetParam(material, "colorMap", white); RenderingServer.MaterialSetParam(material, "detailMap", test);
                    RenderingServer.MaterialSetParam(material, "tint", Colors.White); RenderingServer.MaterialSetParam(material, "lod", 0f); RenderingServer.MaterialSetParam(material, "detailAmount", 0f);
                    var item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, node.GetCanvas()); RenderingServer.CanvasItemSetMaterial(item, material); RenderingServer.CanvasItemAddRect(item, new(256, 96, 64, 64), Colors.White);
                }
                RenderingServer.FramePostDraw += After;
            };
            Check(Engine.Run(root) == 0 && phase == 7 && active == 0 && idle == 0, "Complete cached texture native lifecycle.");
            var oldTest = test; var oldWhite = white; var second = new Window { Size = new(80, 80) };
            second.Ready += _ =>
            {
                Reject<ArgumentException>(() => RenderingServer.Texture2DGet(oldTest)); Reject<ArgumentException>(() => RenderingServer.Texture2DGet(oldWhite)); Reject<ArgumentException>(() => RenderingServer.Texture2DGet(proxy));
                test = RenderingServer.GetTestTexture(); white = RenderingServer.GetWhiteTexture(); Check(test != oldTest && white != oldWhite, "New renderer session owns fresh cached textures."); CheckPattern(test); CheckWhite(white);
                RenderingServer.FramePostDraw += () => second.Tree!.Quit();
            };
            Check(Engine.Run(second) == 0, "A later renderer restarts after retained cache cleanup.");
            Reject<InvalidOperationException>(() => RenderingServer.GetWhiteTexture());
            Console.WriteLine($"Builtin textures {profile}: exact pixels, copied snapshots, update/replace/free/proxy and session lifetime, GPU named sampler when available, 64 active/64 idle frames {active}/{idle} B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); Engine.MaxFPS = fps; }
    }
}
