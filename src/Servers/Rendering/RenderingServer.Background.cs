namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects the background mode of a live canvas environment.</summary>
    /// <param name="environment">A live owned or borrowed environment identity.</param>
    /// <param name="mode">The background mode.</param>
    /// <exception cref="ArgumentException">The identity is invalid or has a different resource type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is not selectable.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, access is off-owner or submission is in progress.</exception>
    public static void EnvironmentSetBackground(RID environment, CanvasBackgroundMode mode)
    {
        RequireService().EnsureServerViewportChange();
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).BackgroundMode = mode;
    }

    /// <summary>Sets the sRGB clear color used by a canvas environment in Color mode.</summary>
    /// <param name="environment">A live owned or borrowed environment identity.</param>
    /// <param name="color">A finite clear color. The viewport controls clear alpha.</param>
    /// <exception cref="ArgumentException">The identity is invalid or has a different resource type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A color component is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, access is off-owner or submission is in progress.</exception>
    public static void EnvironmentSetBGColor(RID environment, Color color)
    {
        RequireService().EnsureServerViewportChange();
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).BackgroundColor = color;
    }

    /// <summary>Applies background energy and a runtime linear exposure factor in one update.</summary>
    /// <param name="environment">A live owned or borrowed environment identity.</param>
    /// <param name="multiplier">A finite, nonnegative linear energy multiplier.</param>
    /// <param name="exposureValue">A finite, nonnegative linear exposure multiplier. One preserves ordinary authored exposure.</param>
    /// <remarks>The runtime factor is reset by authoring BackgroundEnergyMultiplier. Scene files store the energy multiplier.</remarks>
    /// <exception cref="ArgumentException">The identity is invalid or has a different resource type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A multiplier is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, access is off-owner or submission is in progress.</exception>
    public static void EnvironmentSetBGEnergy(RID environment, float multiplier, float exposureValue)
    {
        RequireService().EnsureServerViewportChange();
        CanvasEnvironment.ValidateToneValue(multiplier, nameof(multiplier), allowZero: true);
        CanvasEnvironment.ValidateToneValue(exposureValue, nameof(exposureValue), allowZero: true);
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).SetBackgroundEnergy(multiplier, exposureValue);
    }

    /// <summary>Sets the inclusive background layer limit for a live canvas environment in Canvas mode.</summary>
    /// <param name="environment">A live owned or borrowed environment identity.</param>
    /// <param name="maxLayer">The last background canvas layer, including all its sublayers.</param>
    /// <exception cref="ArgumentException">The identity is invalid or has a different resource type.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, access is off-owner or submission is in progress.</exception>
    public static void EnvironmentSetCanvasMaxLayer(RID environment, int maxLayer)
    {
        RequireService().EnsureServerViewportChange();
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).BackgroundCanvasMaxLayer = maxLayer;
    }

    /// <summary>Selects the registered camera feed drawn in CameraFeed mode.</summary>
    /// <param name="environment">A live owned or borrowed canvas environment identity.</param>
    /// <param name="id">The nonnegative process-local feed ID. Zero selects no feed.</param>
    /// <remarks>Selecting a feed does not activate it or transfer its ownership.</remarks>
    /// <exception cref="ArgumentException">The environment identity is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable, access is off-owner or a submission is in progress.</exception>
    public static void EnvironmentSetCameraID(RID environment, int id)
    {
        RequireService().EnsureServerViewportChange();
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).BackgroundCameraFeedID = id;
    }

    private void PrepareFrameBackground(CanvasFrame frame, CanvasBackgroundSettings? settings)
    {
        var viewport = frame.Viewport;
        var clearMode = ClearMode(viewport);
        frame.Clear = FrameClear(viewport);
        frame.ClearEnabled = clearMode != ViewportClearMode.Never;
        if (settings is not { } background || !frame.ClearEnabled) return;
        if (background.Mode == CanvasBackgroundMode.Keep)
        {
            frame.ClearEnabled = clearMode == ViewportClearMode.Once;
            return;
        }
        if (viewport.TransparentBG || background.Mode == CanvasBackgroundMode.CameraFeed) return;

        if (background.Mode == CanvasBackgroundMode.Color) frame.Clear = background.Color with { A = 1 };
        var energy = (double)background.Energy * background.Exposure;
        var color = frame.Clear;
        if (energy == 1 && color.R is >= 0 and <= 1 && color.G is >= 0 and <= 1 && color.B is >= 0 and <= 1) return;
        var hdr = ViewportHDR(viewport);
        var minimum = hdr ? -65504f : 0;
        var maximum = hdr ? 65504f : 1;
        float Scale(float encoded)
        {
            var linear = encoded <= .04045 ? encoded / 12.92 : Math.Pow((encoded + .055) / 1.055, 2.4);
            return (float)Math.Clamp(linear * energy, minimum, maximum);
        }
        frame.Clear = new Color(Scale(color.R), Scale(color.G), Scale(color.B), 1).LinearToSRGB();
    }

    private readonly Dictionary<int, CameraBackgroundTexture> _cameraBackgroundTextures = [];
    private bool _cameraBackgroundSelected, _drawingCameraBackground;

    internal static void NotifyCameraBackgroundMutation()
    {
        if (Service is { } server &&
            (Volatile.Read(ref server._cameraBackgroundSelected) || Volatile.Read(ref server._drawingCameraBackground)))
            server.MarkChanged();
    }

    private void AppendCameraBackground(int feedID, Vector2i size)
    {
        if (feedID == 0) return;
        Volatile.Write(ref _drawingCameraBackground, true);
        if (!_cameraBackgroundTextures.TryGetValue(feedID, out var texture) || texture.CapturedFrame != _particleFrame)
        {
            var feed = CameraServer.Find(feedID);
            if (texture is null)
            {
                if (feed is null) return;
                _cameraBackgroundTextures.Add(feedID, texture = new());
            }
            texture.CapturedFrame = _particleFrame;
            texture.HasFrame = feed is not null && feed.CaptureBackground(texture, out texture.FeedTransform);
            if (!texture.HasFrame) texture.ClearFrame();
        }
        if (!texture.HasFrame) return;

        var transform = texture.FeedTransform;
        var upperLeft = transform * Vector2.Zero;
        var upperRight = transform * new Vector2(1, 0);
        var lowerRight = transform * Vector2.One;
        var lowerLeft = transform * new Vector2(0, 1);
        if (!upperLeft.IsFinite() || !upperRight.IsFinite() || !lowerRight.IsFinite() || !lowerLeft.IsFinite()) return;

        var first = _vertices.Count;
        var a = new CanvasVertex(Vector2.Zero, Colors.White, upperLeft);
        var b = new CanvasVertex(new(size.X, 0), Colors.White, upperRight);
        var c = new CanvasVertex(size, Colors.White, lowerRight);
        var d = new CanvasVertex(new(0, size.Y), Colors.White, lowerLeft);
        _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
        _vertices.Add(a); _vertices.Add(c); _vertices.Add(d);
        _batches.Add(new(first, 6, null, texture, TextureFilter.Nearest, TextureRepeat.Disabled));
    }

    private void ReleaseUnusedCameraBackgrounds()
    {
        Volatile.Write(ref _cameraBackgroundSelected, _drawingCameraBackground);
        foreach (var pair in _cameraBackgroundTextures)
        {
            if (pair.Value.CapturedFrame == _particleFrame) continue;
            pair.Value.Dispose();
            _cameraBackgroundTextures.Remove(pair.Key);
        }
    }
}
