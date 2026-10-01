namespace Electron2D;

/// <summary>Resamples concrete stream frames using prepared cubic-interpolation history.</summary>
/// <remarks>The decode cursor includes a 128-frame prefetch. BeginResample resets history; ordinary Seek does
/// not reset that history unless the concrete playback requests it. Successful warmed mixing reuses all buffers.</remarks>
public abstract class AudioStreamPlaybackResampled : AudioStreamPlayback
{
    /// <summary>Initializes the independent prepared resampling state.</summary>
    protected AudioStreamPlaybackResampled() { }
    private const int BufferLength = 128, FractionBits = 16;
    private readonly Vector2[] _buffer = new Vector2[BufferLength + 4];
    private ulong _offset;
    private int _end = -1;
    /// <summary>Clears interpolation history and prefills the first source block.</summary>
    /// <exception cref="ObjectDisposedException">The playback is disposed.</exception>
    public void BeginResample()
    {
        ThrowIfDisposed(); _buffer.AsSpan(0, 4).Clear(); var count = OnMixResampled(_buffer.AsSpan(4)); if ((uint)count > BufferLength) throw new InvalidOperationException("Audio source reported an invalid mixed frame count."); _end = count == BufferLength ? -1 : count + 4; _offset = 0;
    }
    /// <summary>Supplies source stereo frames before resampling.</summary>
    /// <param name="buffer">Prepared writable output span.</param>
    /// <returns>Frames before the first silence, zero through buffer.Length.</returns>
    protected abstract int OnMixResampled(Span<Vector2> buffer);
    /// <summary>Supplies the source sampling frequency.</summary>
    /// <returns>Sampling rate in Hz.</returns>
    protected abstract float OnGetStreamSamplingRate();
    /// <inheritdoc />
    protected override int OnMix(Span<Vector2> buffer, float rateScale)
    {
        var rate = OnGetStreamSamplingRate(); var ratio = (rate * rateScale * AudioServer.Instance.PlaybackSpeedScale) / (double)AudioServer.Instance.GetMixRate();
        if (!double.IsFinite(ratio) || ratio < 0 || ratio * 65536 > ulong.MaxValue) throw new InvalidOperationException("Audio resampling rate exceeds the finite cursor range.");
        var increment = (ulong)(ratio * 65536); var mixed = -1;
        for (var i = 0; i < buffer.Length; i++)
        {
            var index = 4 + (int)(_offset >> FractionBits); var mu = (_offset & 65535) / 65536f;
            var y0 = _buffer[index - 3]; var y1 = _buffer[index - 2]; var y2 = _buffer[index - 1]; var y3 = _buffer[index];
            if ((uint)index >= (uint)_end && mixed < 0) mixed = i;
            var mu2 = mu * mu; var h11 = mu2 * (mu - 1); var z = mu2 - h11; var h01 = z - h11; var h10 = mu - z;
            buffer[i] = y1 + (y2 - y1) * h01 + ((y2 - y0) * h10 + (y3 - y1) * h11) * .5f;
            _offset = checked(_offset + increment);
            while ((_offset >> FractionBits) >= BufferLength)
            {
                _buffer.AsSpan(BufferLength, 4).CopyTo(_buffer); var count = OnMixResampled(_buffer.AsSpan(4));
                if ((uint)count > BufferLength) throw new InvalidOperationException("Audio source reported an invalid mixed frame count.");
                _end = count == BufferLength ? -1 : count + 4; _offset -= BufferLength << FractionBits;
            }
        }
        return mixed < 0 ? buffer.Length : mixed;
    }
}
