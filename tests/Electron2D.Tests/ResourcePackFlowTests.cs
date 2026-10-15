using Electron2D;
using Path = System.IO.Path;

internal static class ResourcePackFlowTests
{
    internal static void Run(string mode, string folder)
    {
        folder = Path.GetFullPath(folder); Directory.CreateDirectory(folder);
        ProjectSettings.ConfigurePaths(folder, Path.Combine(folder, "user"));
        var game = Path.Combine(folder, "game"); var pack = Path.Combine(folder, "game.pck");
        if (mode == "oracle")
        {
            var key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            if (!ProjectSettings.LoadResourcePack(Path.Combine(folder, "oracle.pck"), key)) throw new InvalidOperationException("Independent pack failed to mount.");
            foreach (var length in new[] { 0, 1, 15, 16, 17, 31, 32, 33 })
                if (!Electron2D.FileAccess.GetFileAsBytes("res://oracle/entry-" + length).SequenceEqual(Enumerable.Range(0, length).Select(i => (byte)i))) throw new InvalidOperationException("Independent ciphertext decode failed.");
            Console.WriteLine("Independent PCK fixture: all 8 encrypted block-edge entries passed."); return;
        }
        if (mode == "author")
        {
            Directory.CreateDirectory(game);
            using (var authored = new Window { Size = new(40, 40) })
            using (var image = Image.CreateEmpty(12, 12, false, Image.Format.Rgba8))
            {
                image.Fill(Colors.Lime); image.SavePNG("res://game/pixel.png");
                using var texture = ResourceLoader.Load<ImageTexture>("res://game/pixel.png");
                var sprite = new Sprite { Name = "Sprite", Texture = texture, Position = new(20, 20) }; authored.AddChild(sprite); sprite.Owner = authored;
                using var scene = new PackedScene(); scene.Pack(authored); ResourceSaver.Save(scene, "res://game/scene.e2dscene", SaverFlags.RelativePaths);
            }
            using (var writer = new PCKPacker())
            {
                writer.PCKStart(pack, encryptDirectory: true);
                writer.AddFile("game/scene.e2dscene", "res://game/scene.e2dscene", true);
                writer.AddFile("game/pixel.png", "res://game/pixel.png", true);
            }
            Directory.Delete(game, true);
            Console.WriteLine("PCK author: encrypted scene and external PNG packed; physical source directory removed."); return;
        }
        if (Directory.Exists(game)) throw new InvalidOperationException("Physical source directory must be absent.");
        if (!ProjectSettings.LoadResourcePack(pack)) throw new InvalidOperationException("Resource pack failed to mount.");
        ResourceLoader.LoadThreadedRequest<PackedScene>("res://game/scene.e2dscene", useSubThreads: true, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
        using var loaded = ResourceLoader.LoadThreadedGet<PackedScene>("res://game/scene.e2dscene");
        var window = (Window)loaded.Instantiate();
        var spriteNode = (Sprite)window.GetChild(0);
        if (spriteNode.Texture is null || spriteNode.Texture.GetWidth() != 12) throw new InvalidOperationException("External texture did not load from the pack.");
        if (mode == "headless") { window.Dispose(); Console.WriteLine("PCK headless: fresh-process threaded scene/dependency load and instantiation passed."); return; }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, mode); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var target = window.GetTexture(); var frames = 0; var passed = false;
        window.Ready += _ => RenderingServer.FramePostDraw += Capture;
        void Capture()
        {
            if (++frames != 4) return;
            using var pixels = target.GetImage()!; var pixel = pixels.GetPixel(20, 20);
            if (pixel.G < .98f || pixel.R > .02f || pixel.B > .02f) throw new InvalidOperationException("PCK scene texture pixel is wrong.");
            pixels.SavePNG(Path.Combine(folder, "resource-pack-" + mode + ".png"));
            passed = true; RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        if (Engine.Run(window) != 0 || !passed) throw new InvalidOperationException("PCK rendered host failed.");
        Console.WriteLine("PCK " + mode + ": fresh-process threaded encrypted scene/dependency load, actual texture rendering and cleanup passed.");
    }
}
