namespace Electron2D;

/// <summary>Resamples an independent captured Vorbis stream cursor with typed loop control.</summary>
/// <remarks>Created by AudioStreamOggVorbis. Packet/granule mutation invalidates this playback;
/// sampling-rate metadata remains live. The resource and sequence are borrowed.</remarks>
public sealed class AudioStreamPlaybackOggVorbis : AudioStreamPlaybackResampled
{
    private readonly AudioFileCursor _cursor;
    private readonly OggPacketSequence _sequence;
    internal AudioStreamPlaybackOggVorbis(AudioStreamOggVorbis source, AudioDecodedPCM pcm, OggPacketSequence sequence, long version)
    { if (sequence.Version != version) throw new InvalidOperationException("Vorbis packets changed before playback capture."); _sequence = sequence; _cursor = new(source, pcm, source.GetLoopSettings, () => sequence.Version, () => sequence.SamplingRate, version); }
    /// <inheritdoc />
    protected override void OnStart(double fromPosition) { _cursor.Start(fromPosition); BeginResample(); }
    /// <inheritdoc />
    protected override void OnStop() => _cursor.Stop();
    /// <inheritdoc />
    protected override bool OnIsPlaying() => _cursor.Active;
    /// <inheritdoc />
    protected override int OnGetLoopCount() => _cursor.Loops;
    /// <inheritdoc />
    protected override double OnGetPlaybackPosition() => _cursor.Position;
    /// <inheritdoc />
    protected override void OnSeek(double time) => _cursor.Seek(time);
    /// <inheritdoc />
    protected override float OnGetStreamSamplingRate() => _sequence.SamplingRate;
    /// <inheritdoc />
    protected override int OnMixResampled(Span<Vector2> buffer) => _cursor.Mix(buffer, LoopingOverride);
}
