namespace Electron2D;

/// <summary>Creates independent Ogg Theora video and optional Vorbis audio playback.</summary>
/// <remarks>Preparation validates compressed packets and reserves decoder working storage. The source keeps
/// compressed data, not a complete decoded movie. Visible 4:2:0, 4:2:2 and 4:4:4 pictures use opaque RGBA8
/// textures. Source input is bounded to 512 MiB and coded/visible picture axes to 16384 pixels. Borrowed source resources
/// must remain live until their playbacks are disposed. Preparation reports storage refusal through
/// <see cref="OutOfMemoryException"/>; a failed audio-track replacement preserves the active playback.</remarks>
public sealed class VideoStreamTheora : VideoStream
{
    /// <summary>Creates an empty source; set File before creating playback.</summary>
    public VideoStreamTheora() { }
    internal byte[] ReadFile()
    {
        ThrowIfDisposed();
        if (PreparedFile is { } data) return data;
        using var input = FileAccess.Open(File, FileAccessModeFlags.Read);
        if (input.Length is <= 0 or > 536870912) throw new InvalidDataException("Video source must contain 1 through 512 MiB of compressed data.");
        return PreparedFile = input.ReadBytes(checked((int)input.Length));
    }
    /// <inheritdoc />
    protected override VideoStreamPlayback OnInstantiatePlayback() => new TheoraPlayback(this, ReadFile());
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VideoStreamTheora();

    internal static void PreserveFrame(VideoStreamPlayback previous, VideoStreamPlayback next)
    {
        if (previous is TheoraPlayback old && next is TheoraPlayback replacement) replacement.PreserveFrameFrom(old);
    }

    private sealed class TheoraPlayback : VideoStreamPlayback
    {
        private VideoStreamTheora? _source;
        private byte[] _data;
        private NativeVideoDecoder _decoder;
        private ImageTexture? _texture;
        private readonly float[] _pcm = new float[4096 * 8];
        private int _track, _pending, _offset;
        private bool _playing, _paused;
        private long _displayFrame = -1;
        private double _time, _delay;
        internal TheoraPlayback(VideoStreamTheora source, byte[] data)
        {
            _source = source; _data = data; _decoder = new(data, 0);
            try { _texture = ImageTexture.CreatePreparedRGBA(_decoder.Info.Width, _decoder.Info.Height); Publish(0); }
            catch { _decoder.Dispose(); throw; }
        }
        private void CheckSource() => ObjectDisposedException.ThrowIf(_source is null || _source.IsDisposed, this);
        protected override int OnGetChannels() => _decoder.Info.Channels;
        protected override int OnGetMixRate() => _decoder.Info.Rate;
        protected override double OnGetLength() => _decoder.Info.Duration;
        protected override double OnGetPlaybackPosition() => _time - _delay;
        protected override Texture? OnGetTexture() => _texture;
        protected override bool OnIsPlaying() => _playing;
        protected override bool OnIsPaused() => _paused;
        private void Publish(long frame)
        {
            if (_decoder.Info.VideoFrames == 0) return;
            frame = Math.Clamp(frame, 0, _decoder.Info.VideoFrames - 1);
            if (_displayFrame == frame) return;
            var decoder = _decoder;
            try { var pixels = decoder.Frame(frame); _texture!.PublishPreparedRGBA(pixels, decoder.Info.Width, decoder.Info.Height); _displayFrame = frame; }
            finally { GC.KeepAlive(decoder); }
        }
        private void RewindAudio(double time)
        {
            _decoder.SeekAudio(AudioFrame(time, _decoder.Info.Rate));
            _pending = _offset = 0;
        }
        private static long AudioFrame(double time, int rate)
        {
            var frame = Math.Floor(Math.Max(0, time) * rate);
            return frame >= long.MaxValue ? long.MaxValue : (long)frame;
        }
        protected override void OnPlay() { CheckSource(); RewindAudio(0); _delay = ProjectSettings.GetWithOverride(ProjectSettings.AudioVideoDelayCompensationMSEC) / 1000d; _time = 0; _playing = true; }
        protected override void OnStop() { RewindAudio(0); _time = 0; _playing = false; }
        protected override void OnSetPaused(bool paused) => _paused = paused;
        protected override void OnSeek(double time)
        {
            CheckSource(); if (time >= _decoder.Info.Duration) return;
            RewindAudio(time); _time = time;
            Publish((long)Math.Floor(Math.Max(0, time) * _decoder.Info.FPSNumerator / _decoder.Info.FPSDenominator));
        }
        internal override bool SeekForPlayer(double time)
        {
            var accepted = time < _decoder.Info.Duration;
            Seek(time); return accepted;
        }
        protected override void OnSetAudioTrack(int index)
        {
            CheckSource(); if (_track == index) return;
            var replacement = new NativeVideoDecoder(_data, index);
            try { replacement.SeekAudio(AudioFrame(_time, replacement.Info.Rate)); }
            catch { replacement.Dispose(); throw; }
            var previous = _decoder; _decoder = replacement; _track = index; _pending = _offset = 0; previous.Dispose();
        }
        internal override void RestoreTimingFrom(VideoStreamPlayback previous)
        {
            if (previous is not TheoraPlayback other) { base.RestoreTimingFrom(previous); return; }
            RewindAudio(other._time); _time = other._time; _delay = other._delay; _playing = other._playing;
            Publish((long)Math.Min(_decoder.Info.VideoFrames, Math.Floor(Math.Max(0, _time - _delay) * _decoder.Info.FPSNumerator / _decoder.Info.FPSDenominator)));
        }
        internal void PreserveFrameFrom(TheoraPlayback previous)
        {
            if (!ReferenceEquals(_data, previous._data) || previous._texture is not { IsDisposed: false } texture) return;
            var prepared = _texture;
            _texture = texture; _displayFrame = previous._displayFrame; previous._texture = null;
            prepared?.Dispose();
        }
        protected override void OnUpdate(double delta)
        {
            CheckSource(); if (!_playing || _paused) return;
            var next = _time + delta;
            if (!double.IsFinite(next)) throw new ArithmeticException("Video timestamp exceeds the finite timeline range.");
            _time = next;
            var position = _time - _delay;
            Publish((long)Math.Min(_decoder.Info.VideoFrames, Math.Floor(Math.Max(0, position) * _decoder.Info.FPSNumerator / _decoder.Info.FPSDenominator)));
            if (AudioSink is not null && _decoder.Info.Channels > 0)
                while (true)
                {
                    if (_pending == 0) { _pending = _decoder.ReadAudio(_pcm, 4096); _offset = 0; if (_pending == 0) break; }
                    var accepted = MixAudio(_pending, _pcm, _offset * _decoder.Info.Channels);
                    if (accepted <= 0) break;
                    _pending -= accepted; _offset += accepted;
                }
            if (position >= _decoder.Info.Duration) { _playing = false; _time = 0; }
        }
        protected override void Dispose(bool disposing)
        {
            _source = null; _data = [];
            try { _decoder.Dispose(); }
            finally { try { _texture?.Dispose(); } finally { _texture = null; base.Dispose(disposing); } }
        }
    }
}
