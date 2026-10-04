using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class AudioServer
{
    private readonly ConditionalWeakTable<AudioStream, AudioSample> _samples = new();
    private bool _sampling;
    internal bool IsStreamRegisteredAsSampleCore(AudioStream stream) { Check(); ArgumentNullException.ThrowIfNull(stream); ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); lock (_gate) return _samples.TryGetValue(stream, out _); }
    internal void RegisterStreamAsSampleCore(AudioStream stream)
    {
        Check(); ArgumentNullException.ThrowIfNull(stream); lock (_gate)
        {
            if (_sampling) throw new InvalidOperationException("Sample factories cannot reenter registration."); _sampling = true;
            try { ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); if (!stream.CanBeSampled()) throw new NotSupportedException("This stream cannot be sampled."); var next = stream.GenerateSample() ?? throw new InvalidOperationException("Sample generation returned null."); try { ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); _ = next.PrepareNative(); if (!ReferenceEquals(next.Stream, stream)) throw new InvalidOperationException("Sample source identity differs."); _samples.AddOrUpdate(stream, next); } catch { next.Dispose(); throw; } }
            finally { _sampling = false; }
        }
    }
    internal AudioSample GetSample(AudioStream stream) { Check(); if (!_samples.TryGetValue(stream, out var sample)) { RegisterStreamAsSampleCore(stream); sample = _samples.GetValue(stream, _ => throw new InvalidOperationException("Sample registration disappeared.")); } return sample; }
    internal FAudioSampleVoice PrepareSample(AudioSamplePlayback request)
    {
        Check(); request.Check(); EnsureNative(); return _native!.CreateSample(request, GetSample(request.Stream), ResolveBus(request.Bus));
    }
}
