namespace Electron2D;

/// <summary>Selects the screen-space antialiasing applied to a completed canvas frame.</summary>
public enum ScreenSpaceAAMode
{
    /// <summary>Publishes the frame without screen-space antialiasing.</summary>
    Disabled = 0,
    /// <summary>Smooths high-contrast edges in a GPU post-process pass.</summary>
    FXAA = 1,
    /// <summary>Uses edge detection, pattern weights and neighborhood blending on the completed GPU frame.</summary>
    SMAA = 2,
    /// <summary>Exclusive bound for screen-space antialiasing mode identities.</summary>
    Max = 3
}

public abstract partial class Viewport
{
    private ScreenSpaceAAMode _screenSpaceAA;
    private bool _screenSpaceAAAssigned;

    internal static bool IsScreenSpaceAAMode(ScreenSpaceAAMode mode) => (uint)mode < (uint)ScreenSpaceAAMode.Max;

    internal void ApplyProjectScreenSpaceAA()
    {
        if (!_screenSpaceAAAssigned) _screenSpaceAA = ProjectSettings.GetWithOverride(ProjectSettings.ScreenSpaceAA);
    }

    /// <summary>Gets or sets screen-space antialiasing for this viewport's completed canvas frame.</summary>
    /// <value>Disabled initially. An unassigned root window samples ProjectSettings.ScreenSpaceAA at startup.</value>
    /// <remarks>FXAA and SMAA filter the resolved and composited image on the GPU before texture publication and presentation.
    /// FXAA adds -0.25 to the effective mipmap bias. Authored assignments, including equal values, replace renderer-session overrides.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported modes.</exception>
    /// <exception cref="NotSupportedException">The live renderer cannot execute the selected mode.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public ScreenSpaceAAMode ScreenSpaceAA
    {
        get { CheckTransformQuery(); return _screenSpaceAA; }
        set
        {
            EnsureMutable();
            if (!IsScreenSpaceAAMode(value)) throw new ArgumentOutOfRangeException(nameof(value));
            RenderingServer.ValidateViewportScreenSpaceAA(this, value);
            _screenSpaceAA = value;
            _screenSpaceAAAssigned = true;
            RenderingOwner?.PublishViewportScreenSpaceAA(this);
        }
    }

    private static readonly PropertyDescriptor[] ScreenSpaceAAProperties =
    [
        new PropertyDescriptor<Viewport, ScreenSpaceAAMode>(nameof(ScreenSpaceAA), n => n.ScreenSpaceAA, (n, v) => n.ScreenSpaceAA = v,
            _ => ScreenSpaceAAMode.Disabled, (_, v) => IsScreenSpaceAAMode(v), stored: true)
    ];
}
