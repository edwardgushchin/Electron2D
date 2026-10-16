using System.Numerics;

namespace Electron2D;

internal sealed class VideoAudioStream : AudioStream
{
    internal readonly QueuePlayback Queue;
    private bool _created;
    internal VideoAudioStream(int rate, int channels, int milliseconds)
    {
        if (rate <= 0) throw new InvalidOperationException("Video audio requires a positive source frequency.");
        var frames = checked((long)rate * milliseconds / 1000);
        if (frames >= 1 << 24) throw new ArgumentOutOfRangeException(nameof(milliseconds), "Video audio queue exceeds 2^24 storage frames.");
        var count = Math.Max(1u, checked((uint)frames));
        Queue = new(rate, channels, 1 << (BitOperations.Log2(count) + 1));
    }
    protected override AudioStreamPlayback OnInstantiatePlayback()
    {
        if (_created) throw new InvalidOperationException("A private video audio queue has one transport owner.");
        _created = true; return Queue;
    }
    protected override void Dispose(bool disposing) { try { Queue.Dispose(); } finally { base.Dispose(disposing); } }

    internal sealed class QueuePlayback : AudioStreamPlaybackResampled
    {
        private Vector2[] _ring;
        private readonly int _rate, _channels;
        private int _read, _write;
        private bool _active;
        private double _position;
        private float _gain = 1;
        internal QueuePlayback(int rate, int channels, int capacity) { _rate = rate; _channels = channels; _ring = new Vector2[capacity]; }
        internal float Gain { get { lock (ResampleGate) return _gain; } set { lock (ResampleGate) _gain = value; } }
        internal int Push(ReadOnlySpan<float> samples, int frames)
        {
            lock (ResampleGate)
            {
                ThrowIfDisposed();
                var count = Math.Min(frames, (_read - _write - 1) & (_ring.Length - 1));
                var cursor = _write;
                for (var i = 0; i < count; i++)
                {
                    var frame = Downmix(samples.Slice(i * _channels, _channels));
                    if (!frame.IsFinite()) throw new ArithmeticException("Video audio downmix exceeded finite PCM range.");
                    _ring[cursor] = frame; cursor = (cursor + 1) & (_ring.Length - 1);
                }
                _write = cursor; return count;
            }
        }
        private static Vector2 Downmix(ReadOnlySpan<float> s)
        {
            const float center = .7071067811865476f;
            return s.Length switch
            {
                1 => new(s[0], s[0]),
                2 => new(s[0], s[1]),
                3 => new(s[0] + s[1] * center, s[2] + s[1] * center),
                4 => new(s[0] + s[2] * .5f, s[1] + s[3] * .5f),
                5 or 6 => new(s[0] + s[1] * center + s[3] * .5f, s[2] + s[1] * center + s[4] * .5f),
                7 => new(s[0] + s[1] * center + s[3] * center + s[5] * .5f, s[2] + s[1] * center + s[4] * center + s[5] * .5f),
                _ => new(s[0] + s[1] * center + s[3] * center + s[5] * .5f, s[2] + s[1] * center + s[4] * center + s[6] * .5f)
            };
        }
        internal void Reset()
        {
            lock (ResampleGate)
            {
                ThrowIfDisposed(); _active = false; _read = _write = 0; _position = 0;
                BeginResample(); _active = true;
            }
        }
        protected override void OnStart(double fromPosition) => Reset();
        protected override void OnStop() { lock (ResampleGate) _active = false; }
        protected override bool OnIsPlaying() { lock (ResampleGate) return _active; }
        protected override double OnGetPlaybackPosition() { lock (ResampleGate) return _position; }
        protected override void OnSeek(double time) => Reset();
        protected override float OnGetStreamSamplingRate() => _rate;
        protected override int OnMixResampled(Span<Vector2> buffer)
        {
            buffer.Clear(); if (!_active) return buffer.Length;
            var count = Math.Min(buffer.Length, (_write - _read) & (_ring.Length - 1));
            var first = Math.Min(count, _ring.Length - _read);
            _ring.AsSpan(_read, first).CopyTo(buffer); _ring.AsSpan(0, count - first).CopyTo(buffer[first..]);
            _read = (_read + count) & (_ring.Length - 1); _position += buffer.Length / (double)_rate;
            return buffer.Length;
        }
        protected override int OnMix(Span<Vector2> buffer, float rateScale)
        {
            lock (ResampleGate)
            {
                if (!_active) { buffer.Clear(); return 0; }
                // Video follows its scene clock, independently of the global ordinary-audio speed.
                var count = MixPrepared(buffer, rateScale, 1);
                for (var i = 0; i < buffer.Length; i++) buffer[i] *= _gain;
                return count;
            }
        }
        protected override void Dispose(bool disposing)
        {
            lock (ResampleGate) { _active = false; _ring = []; _read = _write = 0; }
            base.Dispose(disposing);
        }
    }
}
