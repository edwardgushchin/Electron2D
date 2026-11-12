namespace Electron2D;

/// <summary>Selects how a canvas environment initializes the viewport background.</summary>
public enum CanvasBackgroundMode
{
    /// <summary>Clears to the rendering session's default clear color.</summary>
    ClearColor = 0,
    /// <summary>Clears to the environment's authored background color.</summary>
    Color = 1,
    /// <summary>Draws the background canvas layers before environment effects and higher layers afterward.</summary>
    Canvas = 3,
    /// <summary>Retains the preceding unprocessed canvas unless a one-time clear is requested.</summary>
    Keep = 4,
    /// <summary>Draws the selected camera feed behind the canvas.</summary>
    CameraFeed = 5,
    /// <summary>Terminal marker. It cannot be selected as a mode.</summary>
    Max = 6
}

public sealed partial class CanvasEnvironment
{
    private CanvasBackgroundSettings _background = CanvasBackgroundSettings.Default;

    /// <summary>Gets or sets how this environment initializes the viewport background.</summary>
    /// <value><see cref="CanvasBackgroundMode.ClearColor"/> initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined or the terminal marker.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public CanvasBackgroundMode BackgroundMode
    {
        get => Read(ref _background.Mode);
        set { ValidateBackgroundMode(value); Set(ref _background.Mode, value); }
    }

    /// <summary>Gets or sets the sRGB clear color used in Color mode.</summary>
    /// <value>Opaque black initially.</value>
    /// <remarks>The viewport determines whether the clear is opaque or transparent.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public Color BackgroundColor
    {
        get => Read(ref _background.Color);
        set
        {
            if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _background.Color, value);
        }
    }

    /// <summary>Gets or sets the linear energy multiplier for the default or authored background clear color.</summary>
    /// <value>One initially.</value>
    /// <remarks>Writing this property also restores unit exposure for the background.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The multiplier is negative or nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public float BackgroundEnergyMultiplier
    {
        get => Read(ref _background.Energy);
        set
        {
            ValidateToneValue(value, nameof(value), allowZero: true);
            SetBackgroundEnergy(value, 1);
        }
    }

    /// <summary>Gets or sets the process-local camera feed ID used in CameraFeed mode.</summary>
    /// <value>One initially. Zero selects no source.</value>
    /// <remarks>Only an active registered feed with a supported frame supplies pixels. Selection never activates the camera.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The ID is negative.</exception>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public int BackgroundCameraFeedID
    {
        get => Read(ref _background.CameraFeedID);
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            Set(ref _background.CameraFeedID, value);
        }
    }

    /// <summary>Gets or sets the greatest canvas layer drawn before environment effects in Canvas mode.</summary>
    /// <value>Zero initially. All sublayers of the selected layer are included.</value>
    /// <exception cref="ObjectDisposedException">This environment is disposed.</exception>
    public int BackgroundCanvasMaxLayer
    {
        get => Read(ref _background.CanvasMaxLayer);
        set => Set(ref _background.CanvasMaxLayer, value);
    }

    internal static void ValidateBackgroundMode(CanvasBackgroundMode mode)
    {
        if (mode is not (CanvasBackgroundMode.ClearColor or CanvasBackgroundMode.Color or CanvasBackgroundMode.Canvas or CanvasBackgroundMode.Keep or CanvasBackgroundMode.CameraFeed))
            throw new ArgumentOutOfRangeException(nameof(mode));
    }

    internal void SetBackgroundEnergy(float energy, float exposure)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_background.Energy == energy && _background.Exposure == exposure) return;
            _background.Energy = energy;
            _background.Exposure = exposure;
        }
        EmitChanged();
    }

    private static readonly PropertyDescriptor[] BackgroundProperties =
    [
        new PropertyDescriptor<CanvasEnvironment, CanvasBackgroundMode>(nameof(BackgroundMode), e => e.BackgroundMode, (e, value) => e.BackgroundMode = value, _ => CanvasBackgroundMode.ClearColor, stored: true),
        new PropertyDescriptor<CanvasEnvironment, Color>(nameof(BackgroundColor), e => e.BackgroundColor, (e, value) => e.BackgroundColor = value, _ => Colors.Black, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(BackgroundEnergyMultiplier), e => e.BackgroundEnergyMultiplier, (e, value) => e.BackgroundEnergyMultiplier = value, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, int>(nameof(BackgroundCameraFeedID), e => e.BackgroundCameraFeedID, (e, value) => e.BackgroundCameraFeedID = value, _ => 1, stored: true),
        new PropertyDescriptor<CanvasEnvironment, int>(nameof(BackgroundCanvasMaxLayer), e => e.BackgroundCanvasMaxLayer, (e, value) => e.BackgroundCanvasMaxLayer = value, _ => 0, stored: true)
    ];
}

internal struct CanvasBackgroundSettings
{
    internal CanvasBackgroundMode Mode;
    internal Color Color;
    internal float Energy, Exposure;
    internal int CameraFeedID, CanvasMaxLayer;
    internal static CanvasBackgroundSettings Default => new() { Color = Colors.Black, Energy = 1, Exposure = 1, CameraFeedID = 1 };
}
