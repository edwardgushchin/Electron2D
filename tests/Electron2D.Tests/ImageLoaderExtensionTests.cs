using Electron2D;
using System.Diagnostics;
using IOPath = System.IO.Path;

internal static partial class ImageLoaderExtensionTests
{
    private sealed class Decoder : ImageFormatLoaderExtension
    {
        internal string[] Suffixes = ["E2DPIX"];
        internal bool Decline, Fail, Empty, RemoveOnDecode, RemoveOnDiscovery;
        internal Electron2D.FileAccess? BorrowedFile;
        internal Image? BorrowedImage;
        internal int Calls;
        internal int ThreadID;
        protected override string[] GetRecognizedExtensions()
        {
            if (RemoveOnDiscovery) RemoveFormatLoader();
            return Suffixes;
        }
        protected override bool LoadImage(Image image, Electron2D.FileAccess fileAccess, LoaderFlags flags, float scale)
        {
            Calls++; ThreadID = Environment.CurrentManagedThreadId;
            BorrowedFile = fileAccess; BorrowedImage = image;
            Check(flags == LoaderFlags.None && scale == 1f && fileAccess.Position == 0, "Ordinary file decoder options and rewind.");
            if (RemoveOnDecode) RemoveFormatLoader();
            var pixels = fileAccess.ReadBytes(4);
            if (Decline) { image.SetData(1, 1, false, Image.Format.Rgba8, [0, 0, 0, 255]); return false; }
            if (Fail) { image.SetData(1, 1, false, Image.Format.Rgba8, [0, 0, 0, 255]); throw new InvalidDataException("fixture failure"); }
            if (Empty) return true;
            if (pixels.Length != 4 || fileAccess.Length != 4) throw new InvalidDataException("Expected one encoded fixture pixel.");
            image.SetData(1, 1, false, Image.Format.Rgba8, pixels);
            return true;
        }
    }
    internal static void Run()
    {
        using var folder = DirAccess.CreateTemp("image-extension-");
        var path = IOPath.Combine(folder.GetCurrentDir(), "one.E2DPIX");
        System.IO.File.WriteAllBytes(path, [255, 0, 0, 255]);
        using var first = new Decoder { Decline = true };
        using var second = new Decoder();
        first.AddFormatLoader(); first.AddFormatLoader(); second.AddFormatLoader();
        try
        {
            Check(ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>().Count(s => s == "e2dpix") == 1, "Normalized independent discovery.");
            var suffixes = ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>(); suffixes[0] = "invalid";
            Check(ResourceLoader.Exists<ImageTexture>(path) && ResourceLoader.ListDirectory(folder.GetCurrentDir()).Contains("one.E2DPIX"), "Resource existence and directory integration.");
            using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.Blue);
            var changes = 0; image.Changed += _ => changes++;
            image.Load(path);
            Check(changes == 1 && image.GetPixel(0, 0) == Colors.Red && first.Calls == 1 && second.Calls == 1, "Decline rewinds, discards changes, ordered registration and one commit.");
            Check(first.BorrowedFile!.IsDisposed && first.BorrowedImage!.IsDisposed && second.BorrowedFile!.IsDisposed && second.BorrowedImage!.IsDisposed, "Borrowed callback arguments expire after loading.");
            first.RemoveFormatLoader(); first.RemoveFormatLoader();
            using (var later = new Decoder { Suffixes = [".invalid"] })
            {
                later.AddFormatLoader(); image.Load(path);
                Check(image.GetPixel(0, 0) == Colors.Red, "Successful earlier decoder skips later invalid discovery.");
            }
            second.Fail = true; Reject<InvalidDataException>(() => image.Load(path));
            Check(changes == 2 && image.GetPixel(0, 0) == Colors.Red, "Throw preserves destination and events.");
            second.Fail = false; second.Empty = true; Reject<InvalidDataException>(() => image.Load(path)); second.Empty = false;
            second.Suffixes = [".invalid"]; Reject<InvalidDataException>(() => image.Load(path)); second.Suffixes = ["E2DPIX"];
            var badPath = IOPath.Combine(folder.GetCurrentDir(), "bad.e2dpix");
            System.IO.File.WriteAllBytes(badPath, [255]); Reject<InvalidDataException>(() => image.Load(badPath));
            System.IO.File.WriteAllBytes(badPath, []); var calls = second.Calls;
            Reject<InvalidDataException>(() => image.Load(badPath)); Check(second.Calls == calls, "Empty input rejected before decoder.");
            using (var oversized = new System.IO.FileStream(badPath, System.IO.FileMode.Create)) oversized.SetLength(64 * 1024 * 1024 + 1);
            Reject<InvalidDataException>(() => image.Load(badPath)); Check(second.Calls == calls, "Oversized file rejected before decoder.");
            var owner = Environment.CurrentManagedThreadId;
            ResourceLoader.LoadThreadedRequest<ImageTexture>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var threaded = ResourceLoader.LoadThreadedGet<ImageTexture>(path);
            using var threadedImage = threaded.GetImage();
            Check(second.ThreadID != owner && threadedImage!.GetPixel(0, 0) == Colors.Red, "Custom image decoder executes in actual resource worker.");
            using var cached = ResourceLoader.Load<ImageTexture>(path);
            Check(ReferenceEquals(cached, ResourceLoader.Load<ImageTexture>(path)), "Ordinary weak path cache.");
            System.IO.File.WriteAllBytes(path, [0, 0, 255, 255]);
            Check(ReferenceEquals(cached, ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Replace)), "Replace retains texture identity.");
            using var replaced = cached.GetImage(); Check(replaced!.GetPixel(0, 0) == Colors.Blue, "Replace executes decoder and updates texture pixels.");
            second.RemoveOnDecode = true; image.Load(path);
            Check(image.GetPixel(0, 0) == Colors.Blue && !ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>().Contains("e2dpix"), "Callback removal completes snapshot and changes later discovery.");
            Reject<NotSupportedException>(() => image.Load(path));
            second.RemoveOnDecode = false; second.AddFormatLoader(); second.RemoveOnDiscovery = true;
            image.Load(path); Check(image.GetPixel(0, 0) == Colors.Blue, "Discovery removal preserves captured decoder.");
            second.RemoveOnDiscovery = false;
            first.AddFormatLoader(); first.Dispose(); Reject<ObjectDisposedException>(() => first.AddFormatLoader());
            Check(!ResourceLoader.GetRecognizedExtensionsForType<ImageTexture>().Contains("e2dpix"), "Disposal unregisters decoder.");
            Capacity();
            FreshProcess(path);
            Console.WriteLine("Image loader registration, atomic fallback, resource workers, cache replacement, disposal and fresh public consumer passed.");
        }
        finally { first.RemoveFormatLoader(); second.RemoveFormatLoader(); }
    }
    private static void Capacity()
    {
        var loaders = Enumerable.Range(0, 65).Select(_ => new Decoder()).ToArray();
        try
        {
            for (var i = 0; i < 64; i++) loaders[i].AddFormatLoader();
            Reject<InvalidOperationException>(() => loaders[64].AddFormatLoader());
            loaders[0].RemoveFormatLoader(); loaders[64].AddFormatLoader();
        }
        finally { foreach (var loader in loaders) loader.Dispose(); }
    }
    internal static void Child(string path)
    {
        using var decoder = new Decoder(); decoder.AddFormatLoader();
        using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
        using var image = texture.GetImage(); Check(image!.GetPixel(0, 0) == Colors.Blue, "Fresh custom format loads through public ResourceLoader.");
        Console.WriteLine("Fresh image decoder consumer passed.");
    }
    private static void FreshProcess(string path)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ImageLoaderExtensionTests).Assembly.Location);
        start.ArgumentList.Add("--image-decoder-child"); start.ArgumentList.Add(path);
        start.Environment["ELECTRON2D_TEST_IMAGE_EXTENSIONS"] = "0";
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Image decoder child did not finish."); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh image decoder consumer passed."), error.GetAwaiter().GetResult());
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
