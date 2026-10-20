namespace Electron2D;

public sealed partial class OS
{
    private int _deltaSmoothing = 1;
    /// <summary>Gets or sets whether ordinary owned window loops smooth measured frame deltas at classical VSync cadence.</summary>
    /// <value>True initially; each owned startup samples ProjectSettings.DeltaSmoothing.</value>
    /// <remarks>Access is thread-safe. Each owned startup reapplies the effective project setting before readiness.
    /// Engine.Run/RunAsync estimate stable refresh cadence before quantizing simulation
    /// input to whole refresh intervals with carried remainder. Disabled, Adaptive/Mailbox VSync, nonconverged/unstable
    /// cadence and resume intervals over one second pass through unchanged. Changes take effect on the next host sample;
    /// bypassing preserves estimator history, while each new engine run resets it. The existing physics jitter/catch-up
    /// policy follows this filter. Explicit AdvanceFrame/batch elapsed input and fixed movie cadence are unchanged.
    /// FPS and frame-monitor sampling retain measured unscaled elapsed time. This policy does not force VSync or guarantee
    /// native presentation cadence, and does not introduce an editor-context flag.</remarks>
    public static bool DeltaSmoothing
    {
        get => Volatile.Read(ref Service._deltaSmoothing) != 0;
        set => Volatile.Write(ref Service._deltaSmoothing, value ? 1 : 0);
    }
}
