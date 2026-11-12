namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Publishes the operator, exposure and selected white point together on a canvas environment.</summary>
    /// <param name="environment">A live owned or borrowed environment identity.</param>
    /// <param name="mode">The tone operator.</param>
    /// <param name="exposure">A finite, nonnegative linear multiplier.</param>
    /// <param name="white">A finite, positive input white point. AgX writes the separate AgX white point.</param>
    /// <exception cref="ArgumentException">The environment identity is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The operator or a scalar is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, the caller is off-owner or a submission is in progress.</exception>
    public static void EnvironmentSetTonemap(RID environment, CanvasEnvironment.ToneMapper mode, float exposure, float white)
    {
        RequireService().EnsureServerViewportChange();
        CanvasEnvironment.ValidateToneMapper(mode);
        CanvasEnvironment.ValidateToneValue(exposure, nameof(exposure), allowZero: true);
        CanvasEnvironment.ValidateToneValue(white, nameof(white));
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).SetServerTonemap(mode, exposure, white);
    }

    /// <summary>Sets the AgX contrast of a live canvas environment.</summary>
    /// <param name="environment">A live owned or borrowed environment identity.</param>
    /// <param name="contrast">A finite, positive contrast.</param>
    /// <exception cref="ArgumentException">The environment identity is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The contrast is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, the caller is off-owner or a submission is in progress.</exception>
    public static void EnvironmentSetTonemapAGXContrast(RID environment, float contrast)
    {
        RequireService().EnsureServerViewportChange();
        CanvasEnvironment.ValidateToneValue(contrast, nameof(contrast));
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).TonemapAGXContrast = contrast;
    }

    private float TonemapOutputMaximum(Viewport viewport)
    {
        var depth = 0;
        for (Viewport? current = viewport; current is not null; current = RenderParent(current))
        {
            if (ScreenAttachment(current, out _, out var windowID) && NativeWindow(windowID) is { } window)
                return window.GetOutputMaxLinearValue();
            if (++depth > 1024) throw new InvalidOperationException("Viewport environment inheritance exceeds the supported parent depth.");
        }
        return viewport.GetWindow()?.GetOutputMaxLinearValue() ?? 1;
    }
}

internal readonly record struct CanvasPostProcess(GlowSettings Glow, TonemapSettings? Tonemap, CanvasAdjustmentSettings Adjustment, float OutputMaximum = 1);
