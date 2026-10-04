namespace Electron2D;

/// <summary>Shapes stereo bus audio into a decaying room tail with predelay, damping and diffusion.</summary>
/// <remarks>Each bus stereo pair owns an independent prepared instance borrowing this resource. Scalar edits are
/// sampled once per processed block without clearing the comb, all-pass or predelay histories.</remarks>
public sealed class AudioEffectReverb : AudioEffect
{
    private readonly object _gate = new();
    private float _predelayMSEC = 150, _predelayFeedback = .4f, _roomSize = .8f, _damping = .5f;
    private float _spread = 1, _hipass, _dry = 1, _wet = .5f;

    /// <summary>Creates the default medium-room configuration.</summary>
    public AudioEffectReverb() { }

    /// <summary>Gets or sets the requested early-reflection predelay in milliseconds.</summary>
    /// <value>150 initially; the prepared 500 ms line clamps its effective delay to ten frames through its capacity.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float PredelayMSEC { get { lock (_gate) { ThrowIfDisposed(); return _predelayMSEC; } } set { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { ThrowIfDisposed(); if (_predelayMSEC == value) return; _predelayMSEC = value; } EmitChanged(); } }
    /// <summary>Gets or sets predelay repetition gain.</summary>
    /// <value>0.4 initially; finite requests clamp to zero through 0.98 before storage.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float PredelayFeedback { get { lock (_gate) { ThrowIfDisposed(); return _predelayFeedback; } } set { Finite(value); value = Math.Clamp(value, 0, .98f); lock (_gate) { ThrowIfDisposed(); if (_predelayFeedback == value) return; _predelayFeedback = value; } EmitChanged(); } }
    /// <summary>Gets or sets the room-size control for comb feedback.</summary>
    /// <value>0.8 initially; effective feedback is bounded between 0.7 and 0.98.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float RoomSize { get { lock (_gate) { ThrowIfDisposed(); return _roomSize; } } set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_roomSize == value) return; _roomSize = value; } EmitChanged(); } }
    /// <summary>Gets or sets the comb high-frequency damping control.</summary>
    /// <value>0.5 initially; a larger normal value retains more high-frequency tail content.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float Damping { get { lock (_gate) { ThrowIfDisposed(); return _damping; } } set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_damping == value) return; _damping = value; } EmitChanged(); } }
    /// <summary>Gets or sets stereo-tail spread between coincident and widened delay-line lengths.</summary>
    /// <value>One initially; valid from zero through one.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside zero through one or nonfinite.</exception>
    public float Spread { get { lock (_gate) { ThrowIfDisposed(); return _spread; } } set { Unit(value); lock (_gate) { ThrowIfDisposed(); if (_spread == value) return; _spread = value; } EmitChanged(); } }
    /// <summary>Gets or sets the normalized wet-input high-pass control.</summary>
    /// <value>Zero initially; finite raw values clamp to zero through one during processing.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float Hipass { get { lock (_gate) { ThrowIfDisposed(); return _hipass; } } set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_hipass == value) return; _hipass = value; } EmitChanged(); } }
    /// <summary>Gets or sets the original-signal amplitude multiplier.</summary>
    /// <value>One initially; finite raw values are retained.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float Dry { get { lock (_gate) { ThrowIfDisposed(); return _dry; } } set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_dry == value) return; _dry = value; } EmitChanged(); } }
    /// <summary>Gets or sets the reverberated-signal amplitude multiplier.</summary>
    /// <value>0.5 initially; finite raw values are retained and the tail also uses its 0.6 wet scale.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float Wet { get { lock (_gate) { ThrowIfDisposed(); return _wet; } } set { Finite(value); lock (_gate) { ThrowIfDisposed(); if (_wet == value) return; _wet = value; } EmitChanged(); } }

    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Unit(float value) { if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); }

    internal readonly record struct Settings(float PredelayMSEC, float PredelayFeedback, float RoomSize, float Damping,
        float Spread, float Hipass, float Dry, float Wet);
    internal Settings Snapshot()
    {
        lock (_gate) { ThrowIfDisposed(); return new(_predelayMSEC, _predelayFeedback, _roomSize, _damping, _spread, _hipass, _dry, _wet); }
    }

    /// <inheritdoc />
    protected override AudioEffectInstance OnInstantiate() => new AudioEffectReverbInstance(this);
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(PredelayMSEC), e => e.PredelayMSEC, (e, v) => e.PredelayMSEC = v, _ => 150, stored: true),
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(PredelayFeedback), e => e.PredelayFeedback, (e, v) => e.PredelayFeedback = v, _ => .4f, stored: true),
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(RoomSize), e => e.RoomSize, (e, v) => e.RoomSize = v, _ => .8f, stored: true),
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(Damping), e => e.Damping, (e, v) => e.Damping = v, _ => .5f, stored: true),
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(Spread), e => e.Spread, (e, v) => e.Spread = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(Hipass), e => e.Hipass, (e, v) => e.Hipass = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(Dry), e => e.Dry, (e, v) => e.Dry = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioEffectReverb, float>(nameof(Wet), e => e.Wet, (e, v) => e.Wet = v, _ => .5f, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AudioEffectReverb();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var state = Snapshot(); var copy = (AudioEffectReverb)target;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed();
            copy._predelayMSEC = state.PredelayMSEC; copy._predelayFeedback = state.PredelayFeedback;
            copy._roomSize = state.RoomSize; copy._damping = state.Damping; copy._spread = state.Spread;
            copy._hipass = state.Hipass; copy._dry = state.Dry; copy._wet = state.Wet;
        }
    }
}

internal sealed class AudioEffectReverbInstance : AudioEffectInstance
{
    private static readonly float[] CombSeconds = [.025306122448979593f, .026938775510204082f, .028956916099773241f,
        .03074829931972789f, .032244897959183672f, .03380952380952381f, .035306122448979592f, .036666666666666667f];
    private static readonly float[] AllPassSeconds = [.0051020408163265302f, .007732426303854875f, .01f, .012607709750566893f];
    private readonly AudioEffectReverb _source;
    private readonly Channel _left, _right;
    private readonly float _rate;

    internal AudioEffectReverbInstance(AudioEffectReverb source)
    {
        _source = source; _rate = AudioServer.GetMixRate();
        if (!float.IsFinite(_rate) || _rate <= 0 || _rate > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(source), "Output rate exceeds prepared reverb storage.");
        _left = new Channel(_rate, 0); _right = new Channel(_rate, .000521f);
    }

    /// <inheritdoc />
    protected override bool OnProcessSilence() => true;

    /// <inheritdoc />
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        var settings = _source.Snapshot();
        var feedback = Math.Clamp(.7f + settings.RoomSize * .28f, .7f, .98f);
        var dampControl = settings.Damping / 2d + .5;
        var damp = (float)Math.Exp(-Math.Tau * dampControl * dampControl * 10000 / _rate);
        var highpass = Math.Clamp(settings.Hipass, 0, 1);
        var hpAux = (float)Math.Exp(-Math.Tau * highpass * 6000 / _rate);
        var hpA1 = (1 + hpAux) * .5f;
        var desiredPredelay = Math.Round(Math.Min(settings.PredelayMSEC, 500) / 1000d * _rate, MidpointRounding.ToEven);
        var predelayFrames = Math.Clamp((int)desiredPredelay, 10, _left.EchoLength - 1);
        var coefficients = new Coefficients(predelayFrames, settings.PredelayFeedback, feedback, damp, settings.Spread,
            highpass > 0, hpA1, hpAux, settings.Dry, settings.Wet * .6f);
        for (var i = 0; i < source.Length; i++)
        {
            var input = source[i];
            var left = _left.Process(input.X, coefficients);
            var right = _right.Process(input.Y, coefficients);
            if (!float.IsFinite(left) || !float.IsFinite(right))
            {
                _left.Clear(); _right.Clear();
                throw new ArithmeticException("Reverb output exceeded finite PCM storage.");
            }
            destination[i] = new(left, right);
        }
    }

    private readonly record struct Coefficients(int PredelayFrames, float PredelayFeedback, float CombFeedback, float Damp,
        float Spread, bool HighpassEnabled, float HighpassA1, float HighpassAux, float Dry, float Wet);

    private sealed class Channel
    {
        private readonly float[] _echo;
        private readonly Comb[] _combs = new Comb[8];
        private readonly AllPass[] _allpasses = new AllPass[4];
        private int _echoPosition;
        private float _hpfInput, _hpfOutput;
        internal int EchoLength => _echo.Length;

        internal Channel(float rate, float spreadBase)
        {
            var extra = (int)MathF.Round(spreadBase * rate, MidpointRounding.ToEven);
            for (var i = 0; i < _combs.Length; i++)
                _combs[i] = new Comb(new float[Math.Max(5, (int)MathF.Round(CombSeconds[i] * rate, MidpointRounding.ToEven) + extra)], extra);
            for (var i = 0; i < _allpasses.Length; i++)
                _allpasses[i] = new AllPass(new float[Math.Max(5, (int)MathF.Round(AllPassSeconds[i] * rate, MidpointRounding.ToEven) + extra)], extra);
            _echo = new float[(int)(.5f * rate + 1)];
        }

        internal float Process(float source, Coefficients c)
        {
            if (_echoPosition >= _echo.Length) _echoPosition = 0;
            var read = _echoPosition - c.PredelayFrames;
            if (read < 0) read += _echo.Length;
            var input = Undenormalize(_echo[read] * c.PredelayFeedback + source);
            if (!float.IsFinite(input)) return float.NaN;
            _echo[_echoPosition++] = input;
            if (c.HighpassEnabled)
            {
                var result = input * c.HighpassA1 - _hpfInput * c.HighpassA1 + _hpfOutput * c.HighpassAux;
                _hpfInput = input; _hpfOutput = result; input = result;
                if (!float.IsFinite(input)) return float.NaN;
            }
            var wet = 0f;
            for (var i = 0; i < _combs.Length; i++)
            {
                ref var comb = ref _combs[i];
                var limit = comb.Buffer.Length - (int)MathF.Round(comb.ExtraFrames * (1 - c.Spread), MidpointRounding.ToEven);
                if (comb.Position >= limit) comb.Position = 0;
                var reflected = Undenormalize(comb.Buffer[comb.Position] * c.CombFeedback);
                reflected = reflected * (1 - c.Damp) + comb.DampHistory * c.Damp;
                var stored = input + reflected;
                if (!float.IsFinite(reflected) || !float.IsFinite(stored)) return float.NaN;
                comb.DampHistory = reflected;
                comb.Buffer[comb.Position++] = stored;
                wet += reflected;
                if (!float.IsFinite(wet)) return float.NaN;
            }
            for (var i = 0; i < _allpasses.Length; i++)
            {
                ref var pass = ref _allpasses[i];
                var limit = pass.Buffer.Length - (int)MathF.Round(pass.ExtraFrames * (1 - c.Spread), MidpointRounding.ToEven);
                if (pass.Position >= limit) pass.Position = 0;
                var prior = pass.Buffer[pass.Position];
                var stored = Undenormalize(.7f * prior + wet);
                if (!float.IsFinite(stored)) return float.NaN;
                pass.Buffer[pass.Position++] = stored;
                wet = prior - .7f * pass.Buffer[pass.Position - 1];
                if (!float.IsFinite(wet)) return float.NaN;
            }
            return wet * c.Wet + source * c.Dry;
        }

        internal void Clear()
        {
            Array.Clear(_echo); _echoPosition = 0; _hpfInput = _hpfOutput = 0;
            foreach (var comb in _combs) Array.Clear(comb.Buffer);
            foreach (var pass in _allpasses) Array.Clear(pass.Buffer);
            for (var i = 0; i < _combs.Length; i++) { _combs[i].Position = 0; _combs[i].DampHistory = 0; }
            for (var i = 0; i < _allpasses.Length; i++) _allpasses[i].Position = 0;
        }
        private static float Undenormalize(float value) =>
            ((uint)BitConverter.SingleToInt32Bits(value) & 0x7f800000u) < 0x08000000u ? 0 : value;
        private struct Comb(float[] buffer, int extraFrames)
        {
            internal readonly float[] Buffer = buffer;
            internal readonly int ExtraFrames = extraFrames;
            internal int Position;
            internal float DampHistory;
        }
        private struct AllPass(float[] buffer, int extraFrames)
        {
            internal readonly float[] Buffer = buffer;
            internal readonly int ExtraFrames = extraFrames;
            internal int Position;
        }
    }
}
