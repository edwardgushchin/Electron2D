namespace Electron2D;

/// <summary>Layers up to four independently modulated stereo voices over bus audio.</summary>
/// <remarks>Each instance owns a prepared delay line and independent voice phase/filter histories. Live edits
/// take effect on the next block without clearing the delayed signal.</remarks>
public sealed class AudioEffectChorus : AudioEffect
{
    private readonly object _gate = new();
    private readonly Voice[] _voices = new Voice[4];
    private readonly PropertyDescriptor[] _voiceProperties = new PropertyDescriptor[24];
    private int _voiceCount = 2;
    private float _dry = 1, _wet = .5f;

    /// <summary>Creates two active voices with their default delay, rate, depth, filter and pan.</summary>
    public AudioEffectChorus()
    {
        for (var i = 0; i < 4; i++) _voices[i] = new(12, 1, 0, 0, 16000, 0);
        _voices[0] = new(15, .8f, 2, 0, 8000, -.5f);
        _voices[1] = new(20, 1.2f, 3, 0, 8000, .5f);
        for (var i = 0; i < 4; i++)
        {
            var index = i;
            var path = $"Voice/{i + 1}/";
            var offset = i * 6;
            _voiceProperties[offset] = new PropertyDescriptor<AudioEffectChorus, float>(path + "DelayMS", e => e.GetVoiceDelayMS(index), (e, v) => e.SetVoiceDelayMS(index, v), _ => index == 0 ? 15 : index == 1 ? 20 : 12, stored: true);
            _voiceProperties[offset + 1] = new PropertyDescriptor<AudioEffectChorus, float>(path + "RateHZ", e => e.GetVoiceRateHZ(index), (e, v) => e.SetVoiceRateHZ(index, v), _ => index == 0 ? .8f : index == 1 ? 1.2f : 1, stored: true);
            _voiceProperties[offset + 2] = new PropertyDescriptor<AudioEffectChorus, float>(path + "DepthMS", e => e.GetVoiceDepthMS(index), (e, v) => e.SetVoiceDepthMS(index, v), _ => index == 0 ? 2 : index == 1 ? 3 : 0, stored: true);
            _voiceProperties[offset + 3] = new PropertyDescriptor<AudioEffectChorus, float>(path + "LevelDB", e => e.GetVoiceLevelDB(index), (e, v) => e.SetVoiceLevelDB(index, v), _ => 0, stored: true);
            _voiceProperties[offset + 4] = new PropertyDescriptor<AudioEffectChorus, float>(path + "CutoffHZ", e => e.GetVoiceCutoffHZ(index), (e, v) => e.SetVoiceCutoffHZ(index, v), _ => index < 2 ? 8000 : 16000, stored: true);
            _voiceProperties[offset + 5] = new PropertyDescriptor<AudioEffectChorus, float>(path + "Pan", e => e.GetVoicePan(index), (e, v) => e.SetVoicePan(index, v), _ => index == 0 ? -.5f : index == 1 ? .5f : 0, stored: true);
        }
    }

    /// <summary>Gets or sets the number of active voices, from one through four.</summary>
    /// <value>Two by default. Inactive voice settings are retained.</value>
    /// <exception cref="ArgumentOutOfRangeException">The new value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int VoiceCount { get { lock (_gate) { ThrowIfDisposed(); return _voiceCount; } } set { if (value is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_voiceCount == value) return; _voiceCount = value; } EmitChanged(); } }
    /// <summary>Gets or sets the finite original-signal amplitude multiplier.</summary>
    /// <value>One by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The new value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Dry { get { lock (_gate) { ThrowIfDisposed(); return _dry; } } set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_dry == value) return; _dry = value; } EmitChanged(); } }
    /// <summary>Gets or sets the finite shared multiplier of all modulated voices.</summary>
    /// <value>One half by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The new value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float Wet { get { lock (_gate) { ThrowIfDisposed(); return _wet; } } set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_wet == value) return; _wet = value; } EmitChanged(); } }

    /// <summary>Gets a voice's delay in milliseconds.</summary>
    /// <param name="voiceIndex">Zero-based voice index, including inactive voices.</param>
    /// <returns>The stored delay.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The voice index is outside zero through three.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetVoiceDelayMS(int voiceIndex) => GetVoice(voiceIndex).DelayMS;
    /// <summary>Sets a voice's delay from zero through fifty milliseconds.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><param name="delayMS">Finite delay.</param>
    /// <exception cref="ArgumentOutOfRangeException">The voice index or setting is outside its supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetVoiceDelayMS(int voiceIndex, float delayMS) { Range(delayMS, 0, 50); SetVoice(voiceIndex, v => v with { DelayMS = delayMS }); }
    /// <summary>Gets a voice's oscillator frequency in hertz.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><returns>The stored frequency.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The voice index is outside zero through three.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetVoiceRateHZ(int voiceIndex) => GetVoice(voiceIndex).RateHZ;
    /// <summary>Sets a voice's oscillator frequency from 0.1 through 20 hertz.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><param name="rateHZ">Finite frequency.</param>
    /// <exception cref="ArgumentOutOfRangeException">The voice index or setting is outside its supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetVoiceRateHZ(int voiceIndex, float rateHZ) { Range(rateHZ, .1f, 20); SetVoice(voiceIndex, v => v with { RateHZ = rateHZ }); }
    /// <summary>Gets a voice's maximum delay modulation in milliseconds.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><returns>The stored depth.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The voice index is outside zero through three.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetVoiceDepthMS(int voiceIndex) => GetVoice(voiceIndex).DepthMS;
    /// <summary>Sets a voice's delay modulation from zero through twenty milliseconds.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><param name="depthMS">Finite modulation depth.</param>
    /// <exception cref="ArgumentOutOfRangeException">The voice index or setting is outside its supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetVoiceDepthMS(int voiceIndex, float depthMS) { Range(depthMS, 0, 20); SetVoice(voiceIndex, v => v with { DepthMS = depthMS }); }
    /// <summary>Gets a voice's decibel gain.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><returns>The stored gain.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The voice index is outside zero through three.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetVoiceLevelDB(int voiceIndex) => GetVoice(voiceIndex).LevelDB;
    /// <summary>Sets a voice's decibel gain; negative infinity silences it.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><param name="levelDB">Finite convertible gain or negative infinity.</param>
    /// <exception cref="ArgumentOutOfRangeException">The voice index or setting is outside its supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetVoiceLevelDB(int voiceIndex, float levelDB) { if (float.IsNaN(levelDB) || levelDB == float.PositiveInfinity || !float.IsFinite(Mathf.DBToLinear(levelDB))) throw new ArgumentOutOfRangeException(nameof(levelDB)); SetVoice(voiceIndex, v => v with { LevelDB = levelDB }); }
    /// <summary>Gets a voice's one-pole low-pass cutoff in hertz.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><returns>The stored cutoff; zero silences the voice.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The voice index is outside zero through three.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetVoiceCutoffHZ(int voiceIndex) => GetVoice(voiceIndex).CutoffHZ;
    /// <summary>Sets a voice's cutoff from zero through 20500 hertz.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><param name="cutoffHZ">Finite cutoff; zero silences the voice.</param>
    /// <exception cref="ArgumentOutOfRangeException">The voice index or setting is outside its supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetVoiceCutoffHZ(int voiceIndex, float cutoffHZ) { Range(cutoffHZ, 0, 20500); SetVoice(voiceIndex, v => v with { CutoffHZ = cutoffHZ }); }
    /// <summary>Gets a voice's signed stereo pan.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><returns>The stored pan.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The voice index is outside zero through three.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetVoicePan(int voiceIndex) => GetVoice(voiceIndex).Pan;
    /// <summary>Sets a voice's signed stereo pan from minus one through one.</summary>
    /// <param name="voiceIndex">Zero-based voice index.</param><param name="pan">Finite pan.</param>
    /// <exception cref="ArgumentOutOfRangeException">The voice index or setting is outside its supported finite range.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetVoicePan(int voiceIndex, float pan) { Range(pan, -1, 1); SetVoice(voiceIndex, v => v with { Pan = pan }); }

    private Voice GetVoice(int index) { lock (_gate) { ThrowIfDisposed(); CheckIndex(index); return _voices[index]; } }
    private void SetVoice(int index, Func<Voice, Voice> update) { lock (_gate) { ThrowIfDisposed(); CheckIndex(index); var voice = update(_voices[index]); if (_voices[index] == voice) return; _voices[index] = voice; } EmitChanged(); }
    private static void CheckIndex(int index) { if ((uint)index >= 4) throw new ArgumentOutOfRangeException(nameof(index)); }
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Range(float value, float min, float max) { if (!float.IsFinite(value) || value < min || value > max) throw new ArgumentOutOfRangeException(nameof(value)); }

    internal readonly record struct Voice(float DelayMS, float RateHZ, float DepthMS, float LevelDB, float CutoffHZ, float Pan);
    internal void Snapshot(Span<Voice> voices, out int count, out float dry, out float wet)
    {
        lock (_gate) { ThrowIfDisposed(); _voices.AsSpan().CopyTo(voices); count = _voiceCount; dry = _dry; wet = _wet; }
    }
    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectChorusInstance(this);
    private static readonly PropertyDescriptor[] Scalars =
    [
        new PropertyDescriptor<AudioEffectChorus, int>(nameof(VoiceCount), e => e.VoiceCount, (e, v) => e.VoiceCount = v, _ => 2, stored: true),
        new PropertyDescriptor<AudioEffectChorus, float>(nameof(Dry), e => e.Dry, (e, v) => e.Dry = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectChorus, float>(nameof(Wet), e => e.Wet, (e, v) => e.Wet = v, _ => .5f, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Scalars).Concat(_voiceProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectChorus();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (AudioEffectChorus)target;
        lock (_gate) { ThrowIfDisposed(); lock (copy._gate) { _voices.CopyTo(copy._voices, 0); copy._voiceCount = _voiceCount; copy._dry = _dry; copy._wet = _wet; } }
    }
}

internal sealed class AudioEffectChorusInstance : AudioEffectInstance
{
    private readonly AudioEffectChorus _source;
    private readonly Vector2[] _ring;
    private readonly Vector2[] _filter = new Vector2[4];
    private readonly ulong[] _cycles = new ulong[4];
    private readonly int _mask;
    private readonly float _rate;
    private int _position;

    internal AudioEffectChorusInstance(AudioEffectChorus source)
    {
        _source = source;
        _rate = AudioServer.GetMixRate();
        if (!float.IsFinite(_rate) || _rate <= 0 || _rate > 1_000_000) throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds chorus storage.");
        var required = (int)Math.Ceiling(.24 * _rate);
        var size = 1; while (size <= required) size <<= 1;
        _ring = new Vector2[size]; _mask = size - 1;
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;
    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        Span<AudioEffectChorus.Voice> voices = stackalloc AudioEffectChorus.Voice[4];
        _source.Snapshot(voices, out var count, out var dry, out var wet);
        for (var offset = 0; offset < source.Length; offset += 256)
        {
            var frames = Math.Min(256, source.Length - offset);
            var input = source.Slice(offset, frames);
            var output = destination.Slice(offset, frames);
            for (var i = 0; i < frames; i++) { _ring[(_position + i) & _mask] = input[i]; output[i] = input[i] * dry; }
            for (var v = 0; v < count; v++)
            {
                var voice = voices[v];
                if (voice.CutoffHZ == 0) continue;
                var delay = (int)MathF.Round((float)(voice.DelayMS / 1000.0 * _rate), MidpointRounding.ToEven);
                var depth = voice.DepthMS / 1000 * _rate;
                if ((int)depth + 10 > delay) delay = (int)depth + 10;
                var lowpass = voice.CutoffHZ >= 16000 ? 0 : (float)Math.Exp(-Math.Tau * voice.CutoffHZ / _rate);
                var left = wet * Mathf.DBToLinear(voice.LevelDB) * Math.Clamp(1 - voice.Pan, 0, 1);
                var right = wet * Mathf.DBToLinear(voice.LevelDB) * Math.Clamp(1 + voice.Pan, 0, 1);
                var phase = _cycles[v];
                var increment = (ulong)Math.Round(voice.RateHZ / _rate * 65536, MidpointRounding.ToEven);
                var history = _filter[v];
                for (var i = 0; i < frames; i++)
                {
                    var wave = (float)Math.Sin((phase & 65535) * Math.Tau / 65536) * depth;
                    var whole = (int)Math.Floor(wave);
                    var fraction = wave - whole;
                    var read = _position + i - delay - whole;
                    var value = _ring[read & _mask];
                    value += (_ring[(read - 1) & _mask] - value) * fraction;
                    value = value * (1 - lowpass) + history * lowpass;
                    if (!value.IsFinite()) { Reset(); throw new ArithmeticException("Chorus filter exceeded finite PCM storage."); }
                    history = value;
                    output[i] += new Vector2(value.X * left, value.Y * right);
                    phase += increment;
                }
                _filter[v] = history;
                _cycles[v] += (ulong)MathF.Round((float)((double)voice.RateHZ * frames / _rate * 65536), MidpointRounding.ToEven);
            }
            for (var i = 0; i < frames; i++)
                if (!output[i].IsFinite()) { Reset(); throw new ArithmeticException("Chorus output exceeded finite PCM storage."); }
            _position = unchecked(_position + frames);
        }
    }

    private void Reset() { Array.Clear(_ring); Array.Clear(_filter); Array.Clear(_cycles); _position = 0; }
}
