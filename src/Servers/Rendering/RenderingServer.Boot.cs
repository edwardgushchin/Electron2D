namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Specifies boot-image sizing in native main-window client pixels.</summary>
    public enum SplashStretchMode
    {
        /// <summary>Keeps the original size and centers on whole pixels.</summary>
        Disabled = 0,
        /// <summary>Fits the window height in landscape or its width in portrait and square windows.</summary>
        Keep = 1,
        /// <summary>Fits the window width while preserving image aspect.</summary>
        KeepWidth = 2,
        /// <summary>Fits the window height while preserving image aspect.</summary>
        KeepHeight = 3,
        /// <summary>Covers the client while preserving aspect and cropping overflow.</summary>
        Cover = 4,
        /// <summary>Fills the client independently on both axes.</summary>
        Ignore = 5
    }

    /// <summary>Immediately presents a loading image in the main native window.</summary>
    /// <param name="image">Borrowed image copied for this call; null or empty images leave output unchanged.</param>
    /// <param name="color">Finite background color; opaque windows use opaque background alpha.</param>
    /// <param name="stretchMode">Sizing and centered cropping in native client pixels.</param>
    /// <param name="useFilter">True for linear sampling, false for nearest sampling.</param>
    /// <remarks>This cold upload/presentation runs outside scene rendering on the native owner, including during readiness
    /// and with RenderLoopEnabled disabled. It does not advance simulation, rendering time, frame counters or callbacks,
    /// acknowledge pending scene changes, or replace viewport textures. The next scene submission replaces the splash.
    /// Temporary image storage is released before return. Existing backend image/filter capability limits apply.</remarks>
    /// <exception cref="ArgumentException">The background is nonfinite or the image cannot be sampled.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stretch mode is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The supplied image is disposed.</exception>
    /// <exception cref="NotSupportedException">The backend cannot sample the image/filter or client dimensions.</exception>
    /// <exception cref="InvalidOperationException">The renderer is absent, off-owner, rendering or unable to present.</exception>
    public static void SetBootImageWithStretch(Image? image, Color color, SplashStretchMode stretchMode, bool useFilter = true)
    {
        var server = RequireService(); server.EnsureOwner();
        if (server._closing || server._rendering || server._submittingTextures || server._nativeFrameStarted) throw new InvalidOperationException("A loading image cannot reenter scene submission.");
        if (stretchMode is < SplashStretchMode.Disabled or > SplashStretchMode.Ignore) throw new ArgumentOutOfRangeException(nameof(stretchMode));
        if (!color.IsFinite()) throw new ArgumentException("The loading background must be finite.", nameof(color));
        if (image is null || image.IsEmpty) return;
        var backend = server._backend; var size = backend.GetPixelSize(); if (size.X <= 0 || size.Y <= 0) return;
        if (size.X > 16384 || size.Y > 16384) throw new NotSupportedException("Loading targets support at most 16384 client pixels per axis.");
        using var texture = ImageTexture.CreateFromImage(image);
        var rect = BootImageRect(new(texture.GetWidth(), texture.GetHeight()), size, stretchMode);
        if (!server._window.Transparent) color.A = 1;
        var current = backend.CreateTarget(size, color); RenderHandle next;
        try { next = backend.CreateTarget(size, color); } catch { current.Dispose(); throw; }
        using var target = new CanvasRenderTarget(current, next, size);
        Span<CanvasVertex> vertices = stackalloc CanvasVertex[6];
        var a = new CanvasVertex(rect.Position, Colors.White, Vector2.Zero); var b = new CanvasVertex(new(rect.End.X, rect.Position.Y), Colors.White, new(1, 0));
        var c = new CanvasVertex(rect.End, Colors.White, Vector2.One); var d = new CanvasVertex(new(rect.Position.X, rect.End.Y), Colors.White, new(0, 1));
        vertices[0] = a; vertices[1] = b; vertices[2] = c; vertices[3] = a; vertices[4] = c; vertices[5] = d;
        var batch = new CanvasBatch(0, 6, null, texture, useFilter ? TextureFilter.Linear : TextureFilter.Nearest);
        server._rendering = server._submittingTextures = true; server.MarkChanged();
        try
        {
            backend.Draw(target, vertices, [batch], color, true, server.CanvasTime, [], new CanvasLightingFrame(), processParticles: false);
            backend.Present([new(target, new(Vector2.Zero, size))]);
        }
        finally
        {
            try { backend.ReleaseTemporaryTexture(texture); }
            finally { server._rendering = server._submittingTextures = false; }
        }
    }

    private static Rect2 BootImageRect(Vector2i image, Vector2i output, SplashStretchMode mode)
    {
        var width = new Vector2(output.X, (float)image.Y * output.X / image.X);
        var height = new Vector2((float)image.X * output.Y / image.Y, output.Y);
        Vector2 size = mode switch
        {
            SplashStretchMode.Disabled => image,
            SplashStretchMode.Keep => output.X > output.Y ? height : width,
            SplashStretchMode.KeepWidth => width,
            SplashStretchMode.KeepHeight => height,
            SplashStretchMode.Cover => (double)output.X / output.Y > (double)image.X / image.Y ? width : height,
            _ => output
        };
        var position = ((Vector2)output - size) / 2;
        return new(mode == SplashStretchMode.Disabled ? position.Floor() : position, size);
    }
}
