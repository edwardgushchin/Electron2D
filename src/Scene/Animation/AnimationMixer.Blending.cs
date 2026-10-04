namespace Electron2D;

public partial class AnimationMixer
{
    /// <summary>Controls precedence between discrete keys and continuously blended values.</summary>
    public enum AnimationCallbackModeDiscrete
    {
        /// <summary>Crossed discrete keys take precedence over continuous values.</summary>
        Dominant = 0,
        /// <summary>Continuous values take precedence over crossed discrete keys.</summary>
        Recessive = 1,
        /// <summary>Sample discrete keys every update and blend them as nearest continuous values.</summary>
        ForceContinuous = 2,
    }
    private readonly Dictionary<Animation, AnimationBlendCache> _blendCaches = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<(ulong Owner, string Property), AnimationBlendProperty> _blendProperties = [];
    private readonly List<AnimationBlendProperty> _blendOrder = [];
    private AnimationBlendProperty[] _blendPropertySnapshot = [];
    private Animation? _captureAnimation;
    private AnimationCaptureValue[] _captureValues = [];
    private double _captureRemaining, _captureDuration;
    private Tween.TransitionType _captureTransition;
    private Tween.EaseType _captureEase;
    private bool _blendDirty = true, _deterministic, _advancing;
    private AnimationCallbackModeDiscrete _callbackModeDiscrete = AnimationCallbackModeDiscrete.Recessive;
    /// <summary>Gets or sets unnormalized accumulation relative to zero or the RESET clip; defaults to false.</summary>
    public bool Deterministic { get { ThrowIfDisposed(); return _deterministic; } set { EnsureAnimationMutable(); _deterministic = value; InvalidateEvaluation(); } }
    /// <summary>Gets or sets discrete/continuous precedence, initially Recessive.</summary>
    public AnimationCallbackModeDiscrete CallbackModeDiscrete { get { ThrowIfDisposed(); return _callbackModeDiscrete; } set { EnsureAnimationMutable(); Animation.Valid(value); _callbackModeDiscrete = value; InvalidateEvaluation(); } }
    /// <summary>Captures current property values from enabled Capture tracks, replacing the prior capture.</summary>
    /// <param name="name">The exact qualified animation name.</param>
    /// <param name="duration">Positive finite capture duration in seconds.</param>
    /// <param name="transitionType">The fade curve, initially Linear.</param>
    /// <param name="easeType">The fade easing, initially In.</param>
    public void Capture(string name, double duration, Tween.TransitionType transitionType = Tween.TransitionType.Linear, Tween.EaseType easeType = Tween.EaseType.In)
    {
        EnsureAnimationMutable(); Animation.Finite(duration); TweenMath.Validate(transitionType, easeType);
        if (!Active) throw new InvalidOperationException("Capture requires an active mixer.");
        if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));
        var animation = GetAnimation(name); ObjectDisposedException.ThrowIf(animation.IsDisposed, animation);
        EnsureBlendCaches(); var cache = _blendCaches[animation]; var values = new List<AnimationCaptureValue>(); var snapshot = new Animation();
        try
        {
            for (var i = 0; i < cache.Bindings.Length; i++)
            { if (!animation.Get(i).Enabled || animation.Get(i).Update != Animation.UpdateMode.Capture || animation.Get(i).Times.Count == 0) continue; var captured = cache.Bindings[i]?.Capture(snapshot, values.Count); if (captured is not null) values.Add(captured); }
        }
        catch { snapshot.Dispose(); throw; }
        ClearCapture(); _captureAnimation = snapshot; _captureValues = values.ToArray(); _captureRemaining = 1; _captureDuration = duration; _captureTransition = transitionType; _captureEase = easeType;
    }
    /// <summary>Processes one typed key after sampling, before discrete writes or weighted accumulation.</summary>
    /// <typeparam name="TValue">The exact track value type.</typeparam>
    /// <param name="animation">The source animation, including an owned snapshot for captured values.</param>
    /// <param name="track">The originating track index.</param>
    /// <param name="value">The sampled typed value.</param>
    /// <param name="objectID">The target node's stable instance identifier.</param>
    /// <param name="objectSubIndex">The indexed target subobject; property tracks use minus one.</param>
    /// <returns>The typed value to accumulate or write.</returns>
    protected virtual TValue OnPostProcessKeyValue<TValue>(Animation animation, int track, TValue value, ulong objectID, int objectSubIndex = -1) => value;
    internal TValue ProcessKey<TValue>(Animation animation, int track, TValue value, ulong objectID) => OnPostProcessKeyValue(animation, track, value, objectID);
    internal void InvalidateEvaluation() { _bindingGeneration++; _cachedAnimation = null; _bindings = []; }
    internal bool EvaluationCurrent(long generation) => !IsDisposed && Active && _bindingGeneration == generation;
    internal AnimationBlendProperty<TValue> BlendProperty<TOwner, TValue>(TOwner owner, PropertyDescriptor<TOwner, TValue> descriptor, Func<TValue, TValue, double, TValue>? interpolate) where TOwner : Node
    {
        var key = (owner.InstanceID, descriptor.Name);
        if (_blendProperties.TryGetValue(key, out var existing)) return existing as AnimationBlendProperty<TValue> ?? throw new InvalidOperationException("Animation tracks disagree about the target property's value type.");
        var property = new AnimationBlendProperty<TValue>(owner, value => descriptor.SetValue(owner, value), AnimationBlendValue<TValue>.Create(interpolate));
        _blendProperties.Add(key, property); _blendOrder.Add(property); return property;
    }
    private void EnsureBlendCaches()
    {
        if (!_blendDirty)
        {
            foreach (var (animation, cache) in _blendCaches) if (!animation.IsDisposed && animation.ChangeRevision != cache.Revision) { _blendDirty = true; break; }
        }
        if (!_blendDirty) return;
        _blendCaches.Clear(); _blendProperties.Clear(); _blendOrder.Clear(); ClearCapture();
        var root = RootNode.Length == 0 ? this : GetNodeOrNull(RootNode);
        foreach (var name in GetAnimationList())
        {
            var animation = GetAnimation(name); if (animation.IsDisposed || _blendCaches.ContainsKey(animation)) continue;
            var bindings = new AnimationBinding?[animation.GetTrackCount()];
            if (root is not null) for (var i = 0; i < bindings.Length; i++) { bindings[i] = animation.Get(i).Bind(root); bindings[i]?.AttachBlend(this); }
            _blendCaches.Add(animation, new(animation.ChangeRevision, bindings));
        }
        if (HasAnimation("RESET"))
        {
            var reset = GetAnimation("RESET"); if (!reset.IsDisposed && _blendCaches.TryGetValue(reset, out var cache))
                for (var i = 0; i < cache.Bindings.Length; i++) if (reset.Get(i).Enabled && reset.Get(i).Times.Count != 0) cache.Bindings[i]?.SetRest();
        }
        _blendPropertySnapshot = _blendOrder.ToArray();
        _blendDirty = false;
    }
    internal bool NeedsBlending => _captureValues.Length != 0 || Deterministic || CallbackModeDiscrete == AnimationCallbackModeDiscrete.ForceContinuous;
    internal void ApplyBlend(ReadOnlySpan<AnimationMixFrame> frames, double captureDelta)
    {
        EnsureBlendCaches(); var generation = _bindingGeneration;
        var remaining = _captureRemaining - captureDelta / (_captureDuration > 0 ? _captureDuration : 1); Animation.Finite(remaining); _captureRemaining = remaining;
        if (_captureRemaining <= 1e-5) ClearCapture();
        var captureWeight = _captureValues.Length == 0 ? 0 : TweenMath.Ease(_captureRemaining, _captureTransition, _captureEase);
        Animation.Finite(captureWeight); var frameWeight = 1 - captureWeight;
        var properties = _blendPropertySnapshot;
        foreach (var property in properties) property.Begin();
        for (var index = 0; index < frames.Length; index++)
        {
            var frame = frames[index]; if (frame.Animation.IsDisposed || !_blendCaches.TryGetValue(frame.Animation, out var cache)) continue;
            for (var i = 0; i < cache.Bindings.Length; i++) cache.Bindings[i]?.AddWeight(frame.Weight * frameWeight * (frame.TrackWeights is null ? 1 : frame.TrackWeights[i]), index);
        }
        foreach (var capture in _captureValues) capture.AddWeight(captureWeight, frames.Length);
        for (var index = 0; index < frames.Length && EvaluationCurrent(generation); index++)
        {
            var frame = frames[index]; if (frame.Animation.IsDisposed || !_blendCaches.TryGetValue(frame.Animation, out var cache)) continue;
            var revision = frame.Animation.ChangeRevision; var weight = frame.Weight * frameWeight;
            for (var i = 0; i < cache.Bindings.Length && EvaluationCurrent(generation) && !frame.Animation.IsDisposed && revision == frame.Animation.ChangeRevision; i++)
                cache.Bindings[i]?.Mix(frame, i, weight * (frame.TrackWeights is null ? 1 : frame.TrackWeights[i]), this, generation);
            if (frame.Animation.IsDisposed || revision != frame.Animation.ChangeRevision) return;
        }
        var captures = _captureValues; var snapshot = _captureAnimation; var captureRevision = snapshot?.ChangeRevision;
        foreach (var capture in captures)
        {
            if (!EvaluationCurrent(generation)) return; capture.Mix(captureWeight, this);
            if (snapshot is not null && (snapshot.IsDisposed || snapshot.ChangeRevision != captureRevision)) { ClearCapture(); InvalidateEvaluation(); return; }
        }
        foreach (var property in properties) { if (!EvaluationCurrent(generation)) return; property.Commit(this); }
    }
    private void ClearCapture() { var snapshot = _captureAnimation; _captureAnimation = null; _captureValues = []; snapshot?.Dispose(); }
    internal void DiscreteWritten(ulong owner, string property) { if (_blendProperties.TryGetValue((owner, property), out var state)) state.DiscreteWritten = true; }
    private sealed record AnimationBlendCache(long Revision, AnimationBinding?[] Bindings);
}
internal readonly record struct AnimationMixFrame(Animation Animation, double Time, bool Backward, double? Previous, double Weight, double Start = 0, double End = -1, double Movement = 0, double[]? TrackWeights = null);
internal abstract class AnimationBlendProperty(Node owner)
{
    internal readonly Node Owner = owner;
    internal double TotalWeight; internal int LastWeightPass; internal bool DiscreteWritten, Angle;
    internal void AddWeight(double weight, int pass) { if (LastWeightPass != pass) { TotalWeight += weight; LastWeightPass = pass; } }
    internal abstract void Begin();
    internal abstract void Commit(AnimationMixer mixer);
}
internal sealed class AnimationBlendProperty<T>(Node owner, Action<T> write, AnimationBlendValue<T>? operation) : AnimationBlendProperty(owner)
{
    private T _rest = operation is null ? default! : operation.Zero;
    private T _fallback = default!;
    private bool _continuous, _wasContinuous = true;
    internal bool Interpolatable => operation is not null;
    internal void SetRest(T value) => _rest = value;
    internal void Add(T value, double weight, bool deterministic)
    {
        if (Math.Abs(weight) < 1e-12) return;
        if (!deterministic) { if (Math.Abs(TotalWeight) < 1e-12) return; weight /= TotalWeight; }
        _continuous = true; if (operation is null) _fallback = value; else operation.Add(value, weight, Angle);
    }
    internal override void Begin() { TotalWeight = 0; LastWeightPass = -1; DiscreteWritten = false; _continuous = false; operation?.Begin(_rest); }
    internal override void Commit(AnimationMixer mixer)
    {
        if (Owner.IsDisposed || Owner.IsQueuedForDeletion || !ReferenceEquals(Owner.Tree, mixer.Tree)) return;
        if (mixer.CallbackModeDiscrete == AnimationMixer.AnimationCallbackModeDiscrete.Dominant && DiscreteWritten) return;
        if (_continuous) { write(operation is null ? _fallback : operation.Finish(TotalWeight)); _wasContinuous = true; }
        else if (mixer.Deterministic && !DiscreteWritten && (_wasContinuous || mixer.CallbackModeDiscrete == AnimationMixer.AnimationCallbackModeDiscrete.ForceContinuous)) { write(_rest); _wasContinuous = false; }
    }
}
internal abstract class AnimationCaptureValue
{
    internal abstract void AddWeight(double weight, int pass);
    internal abstract void Mix(double weight, AnimationMixer mixer);
}
internal sealed class AnimationCaptureValue<T>(Animation animation, int track, T value, AnimationBlendProperty<T> property) : AnimationCaptureValue
{
    internal override void AddWeight(double weight, int pass) => property.AddWeight(weight, pass);
    internal override void Mix(double weight, AnimationMixer mixer)
    { if (!property.Owner.IsDisposed && !property.Owner.IsQueuedForDeletion && ReferenceEquals(property.Owner.Tree, mixer.Tree)) property.Add(mixer.ProcessKey(animation, track, value, property.Owner.InstanceID), weight, mixer.Deterministic); }
}
