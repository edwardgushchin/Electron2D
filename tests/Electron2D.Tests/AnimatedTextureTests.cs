using Electron2D;

internal static class AnimatedTextureTests
{
    internal static void Run()
    {
        using var redImage = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        using var greenImage = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        redImage.Fill(Colors.Red); greenImage.Fill(Colors.Green);
        using var red = ImageTexture.CreateFromImage(redImage);
        using var green = ImageTexture.CreateFromImage(greenImage);
        using var animation = new AnimatedTexture();
        Check(AnimatedTexture.MaxFrames == 256 && animation.Frames == 1 && animation.CurrentFrame == 0 &&
            animation.SpeedScale == 1 && !animation.Pause && !animation.OneShot && animation.GetWidth() == 1 &&
            animation.GetHeight() == 1 && animation.GetImage() is null && !animation.HasAlpha &&
            animation.IsPixelOpaque(0, 0), "Empty animated texture defaults.");
        Check(animation.GetFrameDuration(255) == 1 && animation.GetFrameTexture(255) is null, "Dormant frame defaults.");
        animation.SetFrameTexture(0, red); animation.SetFrameTexture(1, green); animation.Frames = 2;
        animation.SetFrameDuration(0, .5f); animation.SetFrameDuration(1, .5f);
        Check(animation.GetWidth() == 2 && animation.HasAlpha && animation.PixelFormat == Image.Format.Rgba8, "Current frame delegates texture metadata.");
        using (var image = animation.GetImage()!) Check(image.GetPixel(0, 0) == Colors.Red, "Current frame image is copied.");
        Check(animation.AdvanceTime(.5) && animation.CurrentFrame == 1, "Forward playback reaches the next frame at its duration.");
        using (var image = animation.GetImage()!) Check(image.GetPixel(0, 0) == Colors.Green, "Frame change selects new pixels.");
        animation.Pause = true;
        Check(!animation.AdvanceTime(10) && animation.CurrentFrame == 1, "Pause retains progress.");
        animation.Pause = false;
        Check(animation.AdvanceTime(.5) && animation.CurrentFrame == 0, "Resume and loop.");
        animation.SpeedScale = -1;
        Check(animation.AdvanceTime(.5) && animation.CurrentFrame == 1, "Reverse playback wraps.");
        animation.OneShot = true; animation.SpeedScale = 1; animation.CurrentFrame = 1;
        Check(!animation.AdvanceTime(10) && animation.CurrentFrame == 1, "One shot holds the last frame.");
        animation.SpeedScale = -1; animation.CurrentFrame = 0;
        Check(!animation.AdvanceTime(10) && animation.CurrentFrame == 0, "Reverse one shot holds the first frame.");
        animation.OneShot = false; animation.SpeedScale = 2; animation.CurrentFrame = 0;
        Check(animation.AdvanceTime(.25) && animation.CurrentFrame == 1, "Speed multiplier applies to elapsed time.");
        animation.SpeedScale = 1; animation.CurrentFrame = 0;
        Check(!animation.AdvanceTime(.25), "A partial frame retains unscaled elapsed time.");
        animation.SpeedScale = 2;
        Check(animation.AdvanceTime(.001) && animation.CurrentFrame == 1,
            "Changing speed reinterprets time already accumulated in the current frame.");
        animation.SpeedScale = 0;
        Check(!animation.AdvanceTime(10) && animation.CurrentFrame == 1, "Zero speed freezes playback.");
        animation.SpeedScale = 1; animation.SetFrameDuration(0, 0); animation.CurrentFrame = 0;
        Check(animation.AdvanceTime(.001) && animation.CurrentFrame == 1, "Zero-duration frame is skipped.");
        animation.SetFrameDuration(1, 0); animation.CurrentFrame = 0;
        Check(!animation.AdvanceTime(1) && animation.CurrentFrame == 0, "An all-zero loop is bounded.");
        animation.SetFrameDuration(0, .5f); animation.SetFrameDuration(1, .5f);
        Reject<ArgumentOutOfRangeException>(() => animation.Frames = 0);
        Reject<ArgumentOutOfRangeException>(() => animation.Frames = 257);
        Reject<ArgumentOutOfRangeException>(() => animation.CurrentFrame = 2);
        Reject<ArgumentOutOfRangeException>(() => animation.GetFrameTexture(-1));
        Reject<ArgumentOutOfRangeException>(() => animation.SetFrameDuration(256, 1));
        Reject<ArgumentOutOfRangeException>(() => animation.SetFrameDuration(0, float.NaN));
        Reject<ArgumentOutOfRangeException>(() => animation.SpeedScale = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => animation.SpeedScale = 1000);
        Reject<ArgumentException>(() => animation.SetFrameTexture(0, animation));
        using var atlas = new AtlasTexture { Atlas = red };
        Reject<ArgumentException>(() => animation.SetFrameTexture(0, atlas));
        using var nested = new AnimatedTexture(); nested.SetFrameTexture(0, animation);
        Reject<ArgumentException>(() => animation.SetFrameTexture(2, nested));
        var changes = 0; animation.Changed += _ => changes++;
        redImage.Fill(Colors.Blue); red.Update(redImage);
        Check(changes == 1, "Borrowed source changes forward once.");
        using var shallow = (AnimatedTexture)animation.Duplicate();
        using var deep = (AnimatedTexture)animation.Duplicate(true);
        Check(ReferenceEquals(shallow.GetFrameTexture(0), red) &&
            !ReferenceEquals(deep.GetFrameTexture(0), red) && deep.GetFrameDuration(0) == .5f && deep.Frames == 2,
            "Shallow and deep copies preserve frame data and ownership policy.");
        animation.ResourceLocalToScene = red.ResourceLocalToScene = true;
        using var sprite = new Sprite { Texture = animation };
        using var scene = new PackedScene(); scene.Pack(sprite);
        using (var instance = (Sprite)scene.Instantiate())
        {
            var local = (AnimatedTexture)instance.Texture!;
            Check(!ReferenceEquals(local, animation) && !ReferenceEquals(local.GetFrameTexture(0), red) &&
                ReferenceEquals(local.GetLocalScene(), instance), "Scene instantiation duplicates the animated texture graph.");
        }
        using var largeImage = Image.CreateEmpty(4, 4, true, Image.Format.Rgba8);
        largeImage.Fill(Colors.Blue);
        using var large = ImageTexture.CreateFromImage(largeImage);
        animation.SetFrameTexture(2, large); animation.Frames = 3; animation.CurrentFrame = 2;
        Check(animation.GetWidth() == 2 && animation.GetHeight() == 2 && animation.HasMipmaps && animation.MipmapCount == 1,
            "Larger frames crop to the smallest active size, including mipmap metadata.");
        using (var cropped = animation.GetImage()!)
            Check(cropped.Width == 2 && cropped.Height == 2 && cropped.GetPixel(1, 1) == Colors.Blue,
                "The current frame image is cropped from the top left.");
        using var tinyImage = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        tinyImage.Fill(Colors.Yellow); large.SetImage(tinyImage);
        Check(animation.GetWidth() == 1 && animation.GetHeight() == 1 && !animation.HasMipmaps && animation.MipmapCount == 0,
            "A borrowed source resize invalidates the minimum size and mipmap metadata.");
        using (var resized = animation.GetImage()!)
            Check(resized.Width == 1 && resized.Height == 1 && resized.GetPixel(0, 0) == Colors.Yellow,
                "Source replacement invalidates the cached cropped image.");
        animation.Frames = 2;
        Check(animation.CurrentFrame == 1 && animation.GetWidth() == 2, "Shrinking active slots restores their common size.");
        animation.Dispose();
        Check(!red.IsDisposed && !green.IsDisposed, "Disposal leaves borrowed frame textures alive.");
        Reject<ObjectDisposedException>(() => animation.GetFrameTexture(0));
        Console.WriteLine("AnimatedTexture frame, playback, duplication and scene checks passed.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
