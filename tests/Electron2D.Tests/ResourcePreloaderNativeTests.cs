using Electron2D;

internal static class ResourcePreloaderNativeTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-preloader-render-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "render.e2dscene");
            using (var source = new Window { Size = new(96, 64), Title = "Preloaded resource scene" })
            using (var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8))
            using (var scene = new PackedScene())
            {
                image.Fill(Colors.Blue); using var texture = ImageTexture.CreateFromImage(image);
                var assets = new ResourcePreloader { Name = "Assets" }; assets.AddResource("tile", texture); assets.AddResource("alias", texture);
                var sprite = new Sprite { Name = "Sprite", Position = new(12, 12), Scale = new(8, 8), Centered = false };
                source.AddChild(assets); assets.Owner = source; source.AddChild(sprite); sprite.Owner = source;
                scene.Pack(source); ResourceSaver.Save(scene, path);
            }
            var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
            using var window = (Window)loaded.Instantiate(); loaded.Dispose();
            var holder = (ResourcePreloader)window.GetNode("Assets"); var draw = (Sprite)window.GetNode("Sprite");
            var resource = holder.GetResource("tile");
            if (!ReferenceEquals(holder.GetResource("tile"), holder.GetResource("alias"))) throw new InvalidOperationException("File texture aliases.");
            using var target = window.GetTexture();
            var frames = 0; var assigned = false;
            window.Ready += _ => { draw.Texture = (Texture)holder.GetResource("tile"); assigned = true; RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Capture; };
            void Capture()
            {
                frames++;
                if (frames < 3) return;
                using var image = target.GetImage() ?? throw new InvalidOperationException("Rendered texture.");
                var pixel = image.GetPixel(20, 20);
                if (!assigned || pixel.B < .95f || pixel.R > .05f || pixel.G > .05f) throw new InvalidOperationException("Preloaded texture was not rendered: " + pixel);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } dir) { System.IO.Directory.CreateDirectory(dir); image.SavePNG(System.IO.Path.Combine(dir, "preloader-" + backend + ".png")); }
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            try { if (Engine.Run(window) != 0 || frames != 3) throw new InvalidOperationException("Preloader Window loop did not complete."); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
            window.Dispose();
            if (!resource.IsDisposed) throw new InvalidOperationException("Loaded file graph leaked after Window disposal.");
            Console.WriteLine("ResourcePreloader " + backend + ": author/save/load, file aliases/lease, Ready lookup and real rendered blue texture/cleanup passed.");
        }
        finally { System.IO.Directory.Delete(folder, true); }
    }
}
