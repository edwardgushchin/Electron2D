namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Updates the GPU canvas color adjustments of a live environment as one change.</summary>
    /// <param name="environment">A live borrowed environment identity.</param>
    /// <param name="enabled">Whether to apply the adjustments.</param>
    /// <param name="brightness">A finite linear brightness multiplier.</param>
    /// <param name="contrast">A finite contrast multiplier.</param>
    /// <param name="saturation">A finite saturation multiplier.</param>
    /// <param name="colorCorrection">A live borrowed color ramp texture identity, or an empty RID.</param>
    /// <exception cref="ArgumentException">An identity is invalid or identifies a different resource type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A multiplier is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, access is off-owner or a submission is in progress.</exception>
    public static void EnvironmentSetAdjustment(RID environment, bool enabled, float brightness, float contrast,
        float saturation, RID colorCorrection = default)
    {
        RequireService().EnsureServerViewportChange();
        CanvasEnvironment.ValidateAdjustmentValue(brightness, nameof(brightness));
        CanvasEnvironment.ValidateAdjustmentValue(contrast, nameof(contrast));
        CanvasEnvironment.ValidateAdjustmentValue(saturation, nameof(saturation));

        var settings = new CanvasAdjustmentSettings
        {
            Enabled = enabled,
            Brightness = brightness,
            Contrast = contrast,
            Saturation = saturation,
            Lookup = colorCorrection.IsValid() ? RenderingTextureRegistry.Resolve(colorCorrection) : null
        };
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).SetServerAdjustment(settings);
    }
}
