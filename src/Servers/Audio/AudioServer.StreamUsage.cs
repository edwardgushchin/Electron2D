namespace Electron2D;

public sealed partial class AudioServer
{
    private ulong _streamUsageMixSequence, _completedStreamUsageMix;
    private bool _tagStreamUsage;

    /// <summary>Enables or disables collection of active managed stream playback positions.</summary>
    /// <param name="enable">Whether subsequent output quanta collect stream usage. Disabled by default.</param>
    /// <remarks>Requires the audio owner. Disabling immediately clears the published positions.</remarks>
    /// <exception cref="InvalidOperationException">Called outside the audio owner thread or during an audio callback.</exception>
    public static void SetEnableTaggingUsedAudioStreams(bool enable)
    {
        var server = Service;
        server.Check();
        lock (server._gate)
        {
            if (server._tagStreamUsage == enable) return;
            server._tagStreamUsage = enable;
            server._completedStreamUsageMix = 0;
        }
    }

    // Both native output paths go through the same engine procedure under the service's mix gate.
    internal ulong BeginStreamUsageMix() => _tagStreamUsage ? ++_streamUsageMixSequence : 0;
    internal void EndStreamUsageMix(ulong mix) => _completedStreamUsageMix = mix;

    internal double[] CopyTaggedPlaybackOffsets(AudioStream stream)
    {
        CheckAudioReentrancy();
        lock (_gate) return stream.CopyTaggedPlaybackOffsets(_completedStreamUsageMix);
    }
}
