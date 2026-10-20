namespace Electron2D;

public sealed partial class OS
{
    private static readonly PropertyDescriptor[] RenderingProperties =
    [
        new PropertyDescriptor<OS, bool>(nameof(DeltaSmoothing), _ => DeltaSmoothing, (_, value) => DeltaSmoothing = value, _ => true),
        new PropertyDescriptor<OS, bool>(nameof(LowProcessorUsageMode), _ => LowProcessorUsageMode, (_, value) => LowProcessorUsageMode = value, _ => false),
        new PropertyDescriptor<OS, int>(nameof(LowProcessorUsageModeSleepUsec), _ => LowProcessorUsageModeSleepUsec, (_, value) => LowProcessorUsageModeSleepUsec = value, _ => 6900)
    ];

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        foreach (var property in RenderingProperties) yield return property;
    }

    private int _lowProcessorUsageMode;
    private int _lowProcessorUsageModeSleepUsec = 6900;

    /// <summary>Gets or sets whether the owned window loop draws only after rendering changes.</summary>
    /// <value>False initially.</value>
    /// <remarks>Scene processing and input continue. Time-dependent drawing remains active. Movie recording
    /// draws every captured frame. Access is thread-safe; each owned window startup reapplies the project setting.</remarks>
    public static bool LowProcessorUsageMode
    {
        get => Volatile.Read(ref Service._lowProcessorUsageMode) != 0;
        set => Volatile.Write(ref Service._lowProcessorUsageMode, value ? 1 : 0);
    }

    /// <summary>Gets or sets the minimum owned-loop wait in microseconds while low-processor mode is enabled.</summary>
    /// <value>6900 initially; zero disables this additional wait.</value>
    /// <remarks>The wait yields to native events and quit requests. MaxFPS can require a longer wait.
    /// Actual precision depends on the host scheduler. Manual AdvanceFrame and movie recording do not wait.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The interval is negative.</exception>
    public static int LowProcessorUsageModeSleepUsec
    {
        get => Volatile.Read(ref Service._lowProcessorUsageModeSleepUsec);
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); Volatile.Write(ref Service._lowProcessorUsageModeSleepUsec, value); }
    }
}
