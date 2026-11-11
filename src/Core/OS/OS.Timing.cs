using System.Diagnostics;

namespace Electron2D;

public sealed partial class OS
{
    /// <summary>Blocks the calling thread for at least the specified number of milliseconds.</summary>
    /// <param name="msec">The duration in milliseconds. Zero returns immediately.</param>
    /// <remarks>Host scheduling may extend the wait. No engine loop or display session is required.
    /// Use a <see cref="SceneTreeTimer"/> for a delay that lets the scene continue processing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="msec"/> is negative.</exception>
    /// <exception cref="ThreadInterruptedException">A managed interrupt wakes the sleeping caller.</exception>
    public static void DelayMSEC(int msec)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(msec);
        if (msec != 0) Delay(TimeSpan.FromTicks((long)msec * TimeSpan.TicksPerMillisecond));
    }

    /// <summary>Blocks the calling thread for at least the specified number of microseconds.</summary>
    /// <param name="usec">The duration in microseconds. Zero returns immediately.</param>
    /// <remarks>Host scheduling may extend the wait. No engine loop or display session is required.
    /// Use a <see cref="SceneTreeTimer"/> for a delay that lets the scene continue processing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="usec"/> is negative.</exception>
    /// <exception cref="ThreadInterruptedException">A managed interrupt wakes the sleeping caller.</exception>
    public static void DelayUSEC(int usec)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(usec);
        if (usec != 0) Delay(TimeSpan.FromTicks((long)usec * TimeSpan.TicksPerMicrosecond));
    }

    private static void Delay(TimeSpan duration)
    {
        var start = Stopwatch.GetTimestamp();
        var remaining = duration;
        var canSleep = !OperatingSystem.IsBrowser();
        while (remaining > TimeSpan.Zero)
        {
            var milliseconds = (int)(remaining.Ticks / TimeSpan.TicksPerMillisecond);
            // The browser UI thread cannot use a blocking host sleep.
            if (milliseconds > 0 && canSleep) Thread.Sleep(milliseconds);
            else Thread.SpinWait(32);
            remaining = duration - Stopwatch.GetElapsedTime(start);
        }
    }

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
