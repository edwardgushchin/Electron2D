using Electron2D;
using static TextureArrayTests;

internal static class MissingResourceNativeTests
{
    internal static void Run()
    {
        MissingResourceTests.Run();
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-missing-native-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = System.IO.Path.Combine(directory, "native.e2dres"); using var resource = new MissingResource { OriginalClass = "tests.MissingGraph", RecordingProperties = true }; resource.SetRecordedProperty("Number", 71); resource.SetRecordedProperty<Resource?>("Link", resource); resource.SetRecordedProperty<Resource?[]?>("Many", [resource, null, resource]); ResourceSaver.Save(resource, path, SaverFlags.BundleResources); MissingResourceTests.MakeScene(path + ".e2dscene"); MissingResourceTests.Child("preserve", path, true); MissingResourceTests.Child("restore", path, true);
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Fresh missing mesh scenes and restored factories render GPU/compatibility pixels with zero warmed complete frames.");
    }
    internal static void Render(PackedScene scene, bool restored)
    {
        foreach (var backend in new[] { "gpu", "compatibility" })
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            using var root = scene.Instantiate(); using var window = new Window { Size = new(128, 80), MinSize = new(128, 80), MaxSize = new(128, 80), Visible = true }; window.AddChild(root);
            var absent = (MeshInstance)root.GetChild(1); var placeholder = absent.Mesh as PlaceholderMesh; var frame = 0; long before = 0, allocated = 0; var deltas = new long[152];
            window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Exact recovery backend without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black); window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
            void Pre() { if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); if (frame >= 6 && frame < 88 && placeholder is not null) placeholder.AABB = new(0, 0, frame % 2 == 0 ? 32 : 36, 32); }
            void Post()
            {
                if (frame >= 24) { deltas[frame] = GC.GetAllocatedBytesForCurrentThread() - before; allocated += deltas[frame]; }
                if (frame == 3)
                {
                    using var pixels = window.GetTexture().GetImage()!; var known = pixels.GetPixel(12, 12); var missing = pixels.GetPixel(72, 12); Check(known.G > .99f && known.R < .01f && known.B < .01f, "Known mesh survives missing asset recovery."); Check(restored ? missing.B > .99f && missing.R < .01f && missing.G < .01f : missing.R < .01f && missing.G < .01f && missing.B < .01f, "Recovered factory draws blue; placeholder draws no invented geometry.");
                    var output = System.IO.Path.Combine("bin", "missing-resource-validation", "2026-10-12"); Directory.CreateDirectory(output); pixels.SavePNG(System.IO.Path.Combine(output, (restored ? "restored-" : "missing-") + backend + ".png"));
                }
                if (++frame == 152) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; window.Tree!.Quit(); }
            }
            Engine.Run(window); if (allocated != 0) for (var i = 24; i < deltas.Length; i++) if (deltas[i] != 0) Console.WriteLine(backend + " recovery allocation frame=" + i + " bytes=" + deltas[i]); Check(allocated == 0, backend + " recovery full frame allocation " + allocated); Console.WriteLine((restored ? "Restored" : "Missing") + " " + backend + ": 64 active +64 idle frames, 0 managed bytes.");
        }
    }
}
