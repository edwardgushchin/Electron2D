using Electron2D;
using Electron2D.Examples.AsyncGallery;

internal static class ThreadedLoadingRenderingTests
{
    private sealed class Images : ResourceFormatLoader
    {
        internal readonly CountdownEvent Started = new(2);
        internal readonly ManualResetEventSlim Release = new();
        public override string[] GetRecognizedExtensions() => ["png"];
        public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(ImageTexture));
        public override Type? GetResourceType(string path) => typeof(ImageTexture);
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
        {
            Started.Signal(); if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new InvalidOperationException("Image load release timed out.");
            using var image = Image.LoadFromFile(path); return ImageTexture.CreateFromImage(image);
        }
        protected override void Dispose(bool disposing) { Started.Dispose(); Release.Dispose(); base.Dispose(disposing); }
    }
    internal static void RunHost()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_ASYNC_GALLERY") ?? throw new InvalidOperationException("Gallery authoring directory is required.");
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu"; ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var images = new Images(); ResourceLoader.AddResourceFormatLoader(images, true);
        var window = new Window { Size = new(64, 64) }; var gallery = new Gallery(System.IO.Path.Combine(directory, "gallery.e2dscene")); window.AddChild(gallery); var frames = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames == 30) { Check(images.Started.IsSet && !gallery.Loaded, "Both real image loads are pending while 30 frames render."); images.Release.Set(); }
                if (!gallery.Loaded) { Check(frames < 120, "Gallery load completes."); return; }
                if (frames < 40) return;
                using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(16, 24).R > .9f && pixels.GetPixel(48, 24).B > .9f && gallery.Frames == frames, "Fresh-process scene consumes background image graphs on its owner and renders actual pixels.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_ASYNC_SNAPSHOT") is { } capture) pixels.SavePNG(capture); window.Tree!.Quit();
            };
        };
        try { Engine.Run(window); Check(window.IsDisposed && gallery.IsDisposed && ResourceLoader.LoadThreadedGetStatus(System.IO.Path.Combine(directory, "gallery.e2dscene")) == ResourceLoader.ThreadLoadStatus.InvalidResource, "Scene, request and native cleanup."); }
        finally { images.Release.Set(); ResourceLoader.RemoveResourceFormatLoader(images); }
        Console.WriteLine($"Async gallery passed: {backend}, {frames} rendered frames, two blocked image loads, owner scene instantiation and red/blue pixels.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
