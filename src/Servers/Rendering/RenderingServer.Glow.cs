namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects whether a viewport renders its world's canvas environment.</summary>
    public enum ViewportEnvironmentMode
    {
        /// <summary>Bypasses the environment.</summary>
        Disabled = 0,
        /// <summary>Uses the environment regardless of the parent's mode.</summary>
        Enabled = 1,
        /// <summary>Uses the parent viewport's mode, or Enabled at the root.</summary>
        Inherit = 2,
        /// <summary>Terminal enum marker.</summary>
        Max = 3
    }

    private bool _bicubicGlow;

    /// <summary>The number of available HDR glow levels.</summary>
    public const int MaxGlowLevels = GlowSettings.LevelCount;

    /// <summary>Updates the glow on an environment resource identified by its borrowed RID.</summary>
    /// <param name="environment">A live canvas environment identity.</param>
    /// <param name="enabled">Whether to apply glow to its HDR canvas targets.</param>
    /// <param name="levels">Seven copied, nonnegative blur weights in increasing radius order.</param>
    /// <param name="intensity">Glow intensity outside Mix mode.</param>
    /// <param name="strength">Blur strength.</param>
    /// <param name="mix">The glow interpolation weight in Mix mode.</param>
    /// <param name="bloomThreshold">The minimum contribution from below-threshold colors.</param>
    /// <param name="blendMode">How the glow is combined with the HDR canvas.</param>
    /// <param name="HDRBleedThreshold">The linear brightness threshold.</param>
    /// <param name="HDRBleedScale">The brightness transition width.</param>
    /// <param name="HDRLuminanceCap">The maximum extracted channel value.</param>
    /// <param name="glowMapStrength">The modulation weight of the optional glow map.</param>
    /// <param name="glowMap">A borrowed texture identity, or an empty RID.</param>
    /// <remarks>The supplied weights are used directly. This updates resource authoring and affects all worlds using that resource.</remarks>
    /// <exception cref="ArgumentException">An identity or the level count is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A weight, setting or blend mode is invalid.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable, access is off-owner or a submission is in progress.</exception>
    public static void EnvironmentSetGlow(RID environment, bool enabled, ReadOnlySpan<float> levels, float intensity,
        float strength, float mix, float bloomThreshold, CanvasEnvironment.GlowBlendMode blendMode, float HDRBleedThreshold,
        float HDRBleedScale, float HDRLuminanceCap, float glowMapStrength, RID glowMap)
    {
        var server = RequireService();
        server.EnsureServerViewportChange();
        if (levels.Length != MaxGlowLevels) throw new ArgumentException("Supply seven glow level weights.", nameof(levels));
        if ((uint)blendMode > (uint)CanvasEnvironment.GlowBlendMode.Mix) throw new ArgumentOutOfRangeException(nameof(blendMode));

        CanvasEnvironment.ValidateGlow(intensity);
        CanvasEnvironment.ValidateGlow(strength);
        CanvasEnvironment.ValidateGlow(mix, 1);
        CanvasEnvironment.ValidateGlow(bloomThreshold);
        CanvasEnvironment.ValidateGlow(HDRBleedThreshold);
        CanvasEnvironment.ValidateGlow(HDRBleedScale);
        CanvasEnvironment.ValidateGlow(HDRLuminanceCap);
        CanvasEnvironment.ValidateGlow(glowMapStrength, 1);

        var settings = new GlowSettings
        {
            Enabled = enabled,
            Intensity = intensity,
            Strength = strength,
            Mix = mix,
            Bloom = bloomThreshold,
            Blend = blendMode,
            Threshold = HDRBleedThreshold,
            HDRScale = HDRBleedScale,
            LuminanceCap = HDRLuminanceCap,
            MapStrength = glowMapStrength,
            Map = glowMap.IsValid() ? RenderingTextureRegistry.Resolve(glowMap) : null
        };
        for (var i = 0; i < MaxGlowLevels; i++)
        {
            CanvasEnvironment.ValidateGlow(levels[i]);
            settings.Levels[i] = levels[i];
        }
        RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment).SetServerGlow(settings);
    }

    /// <summary>Selects bicubic upsampling for HDR glow in the active rendering session.</summary>
    /// <param name="enable">True for bicubic filtering, false for bilinear filtering.</param>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable, access is off-owner or a submission is in progress.</exception>
    public static void EnvironmentGlowSetUseBicubicUpscale(bool enable)
    {
        var server = RequireService();
        server.EnsureServerViewportChange();
        server._bicubicGlow = enable;
    }

    /// <summary>Selects environment rendering for a live viewport in the active session.</summary>
    /// <param name="viewport">The live viewport identity.</param>
    /// <param name="mode">Disable, enable, or inherit the parent viewport's selection.</param>
    /// <remarks>The mode gates the world environment. Enabling it still requires a live environment and compatible canvas storage.</remarks>
    /// <exception cref="ArgumentException">The viewport identity is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, the caller is off-owner or a submission is in progress.</exception>
    public static void ViewportSetEnvironmentMode(RID viewport, ViewportEnvironmentMode mode)
    {
        if ((uint)mode >= (uint)ViewportEnvironmentMode.Max) throw new ArgumentOutOfRangeException(nameof(mode));
        RequireService().ChangeViewport(viewport, out _).EnvironmentMode = mode;
    }

    private bool EnvironmentEnabled(Viewport viewport)
    {
        var depth = 0;
        for (Viewport? current = viewport; current is not null; current = RenderParent(current))
        {
            if (_serverViewports.TryGetValue(current, out var state) && state.EnvironmentMode != ViewportEnvironmentMode.Inherit)
                return state.EnvironmentMode == ViewportEnvironmentMode.Enabled;
            if (++depth > 1024) throw new InvalidOperationException("Viewport environment inheritance exceeds the supported parent depth.");
        }
        return true;
    }

    private CanvasPostProcess SelectPostProcess(Viewport viewport, SceneTree tree)
    {
        if (!tree.HasWorldEnvironment || _backend is not GPUCanvasBackend || !ViewportHDR(viewport) || !EnvironmentEnabled(viewport) || viewport.FindWorld() is not { } world) return default;
        if (tree.SelectWorldEnvironment(world.Runtime)?.LiveEnvironment is not { } environment) return default;
        var settings = environment.SnapshotPostProcess();
        var glow = settings.Glow;
        if (!glow.Enabled && settings.Tonemap is not { IsIdentity: false }) return default;
        glow.Bicubic = _bicubicGlow;
        return settings with { Glow = glow, OutputMaximum = TonemapOutputMaximum(viewport) };
    }
}
