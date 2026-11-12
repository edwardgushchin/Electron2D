namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects the background mode of a live canvas environment.</summary>
    /// <param name="environment">A live borrowed environment identity.</param>
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
    /// <param name="environment">A live borrowed environment identity.</param>
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
    /// <param name="environment">A live borrowed environment identity.</param>
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
        if (viewport.TransparentBG) return;

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
}
