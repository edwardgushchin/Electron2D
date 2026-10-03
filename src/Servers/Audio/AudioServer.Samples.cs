using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class AudioServer
{
    private readonly ConditionalWeakTable<AudioStream, AudioSample> _samples = new();
    private bool _sampling;
    /// <summary>Gets whether a live stream has a prepared sample snapshot in this engine session.</summary>
    /// <param name="stream">Borrowed stream identity.</param>
    /// <returns>True after successful registration; engine closure clears registration.</returns>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public bool IsStreamRegisteredAsSample(AudioStream stream) { Check(); ArgumentNullException.ThrowIfNull(stream); ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); lock (_gate) return _samples.TryGetValue(stream, out _); }
    /// <summary>Registers or replaces an immutable finite PCM snapshot for native sample playback.</summary>
    /// <param name="stream">Borrowed sample-capable stream.</param>
    /// <remarks>Preparation is cold and transactional. Existing voices retain their old snapshot; explicit
    /// registration captures later edits for subsequent voices. The weak-key cache does not retain unused resources.</remarks>
    /// <exception cref="NotSupportedException">The stream cannot be sampled.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or sampling reenters registration.</exception>
    public void RegisterStreamAsSample(AudioStream stream)
    {
        Check(); ArgumentNullException.ThrowIfNull(stream); lock (_gate)
        {
            if (_sampling) throw new InvalidOperationException("Sample factories cannot reenter registration."); _sampling = true;
            try { ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); if (!stream.CanBeSampled()) throw new NotSupportedException("This stream cannot be sampled."); var next = stream.GenerateSample() ?? throw new InvalidOperationException("Sample generation returned null."); try { ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); _ = next.PrepareNative(); if (!ReferenceEquals(next.Stream, stream)) throw new InvalidOperationException("Sample source identity differs."); _samples.AddOrUpdate(stream, next); } catch { next.Dispose(); throw; } }
            finally { _sampling = false; }
        }
    }
    internal AudioSample GetSample(AudioStream stream) { Check(); if (!_samples.TryGetValue(stream, out var sample)) { RegisterStreamAsSample(stream); sample = _samples.GetValue(stream, _ => throw new InvalidOperationException("Sample registration disappeared.")); } return sample; }
    internal FAudioSampleVoice PrepareSample(AudioSamplePlayback request)
    {
        Check(); request.Check(); EnsureNative(); return _native!.CreateSample(request, GetSample(request.Stream), ResolveBus(request.Bus));
    }
}
