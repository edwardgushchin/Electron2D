using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>A scalar Bézier key with relative time/value offsets for its control handles.</summary>
public readonly struct AnimationBezierKey
{
    /// <summary>Creates scalar key data; the receiving track validates and normalizes handles.</summary>
    /// <param name="value">The finite scalar value.</param><param name="inHandle">Incoming relative time/value offset.</param><param name="outHandle">Outgoing relative time/value offset.</param>
    public AnimationBezierKey(double value, Vector2 inHandle = default, Vector2 outHandle = default) { Value = value; InHandle = inHandle; OutHandle = outHandle; }
    /// <summary>Gets the scalar value.</summary><value>Finite scalar key data when accepted by a track.</value>
    public double Value { get; init; }
    /// <summary>Gets the incoming relative time/value offset.</summary><value>The track clamps positive x to zero.</value>
    public Vector2 InHandle { get; init; }
    /// <summary>Gets the outgoing relative time/value offset.</summary><value>The track clamps negative x to zero.</value>
    public Vector2 OutHandle { get; init; }
}
/// <summary>A named animation callback key with an exact Node receiver and privately typed payload.</summary>
/// <typeparam name="TOwner">The receiver Node type shared by one method track.</typeparam>
/// <remarks>Different payload signatures may coexist in one track; no untyped payload accessor is exposed.</remarks>
public abstract class AnimationMethodKey<TOwner> where TOwner : Node
{
    /// <summary>Validates a nonblank diagnostic name.</summary>
    /// <param name="name">The exact diagnostic name.</param>
    /// <exception cref="ArgumentException">The name is null or blank.</exception>
    protected AnimationMethodKey(string name) { ArgumentException.ThrowIfNullOrWhiteSpace(name); Name = name; }
    /// <summary>Gets the exact diagnostic method name.</summary>
    /// <value>The validated nonblank name; the delegate supplies executable identity.</value>
    public string Name { get; }
    internal abstract void Invoke(TOwner owner);
    internal abstract T ReadArguments<T>();
    internal abstract AnimationMethodKey<TOwner> Copy(bool deep, Func<Resource?, Resource?>? duplicate);
}
/// <summary>An immutable named callback and exact typed argument payload for an animation method key.</summary>
/// <typeparam name="TOwner">The relative target Node type.</typeparam>
/// <typeparam name="TArguments">The exact payload type, such as an immutable record or value tuple.</typeparam>
/// <remarks>Callbacks and custom references are borrowed. No reflection invocation or object argument array is used.</remarks>
public sealed class AnimationMethodKey<TOwner, TArguments> : AnimationMethodKey<TOwner> where TOwner : Node
{
    /// <summary>Creates a typed callback key.</summary>
    /// <param name="name">The nonblank diagnostic method name.</param>
    /// <param name="callback">The callback accepting a resolved target and typed arguments.</param>
    /// <param name="arguments">The typed payload; custom mutable references remain borrowed.</param>
    /// <exception cref="ArgumentException">The name is null or blank.</exception>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    public AnimationMethodKey(string name, Action<TOwner, TArguments> callback, TArguments arguments) : base(name) { ArgumentNullException.ThrowIfNull(callback); Callback = callback; Arguments = arguments; }
    /// <summary>Gets the borrowed typed callback.</summary>
    /// <value>The validated nonnull delegate.</value>
    public Action<TOwner, TArguments> Callback { get; }
    /// <summary>Gets the exact typed payload.</summary>
    /// <value>The payload supplied at construction; custom references remain borrowed.</value>
    public TArguments Arguments { get; }
    internal override void Invoke(TOwner owner) => Callback(owner, Arguments);
    internal override T ReadArguments<T>() { if (typeof(T) != typeof(TArguments)) throw new InvalidCastException("Argument payload type differs from this key."); var args = Arguments; return Unsafe.As<TArguments, T>(ref args); }
    internal override AnimationMethodKey<TOwner> Copy(bool deep, Func<Resource?, Resource?>? duplicate) { var args = Arguments; if (args is Resource resource && deep) args = (TArguments)(object)duplicate!(resource)!; else if (args is Array array) args = (TArguments)(object)array.Clone(); return new AnimationMethodKey<TOwner, TArguments>(Name, Callback, args); }
}
public sealed partial class Animation
{
    /// <summary>Adds a scalar Bézier property track with an immutable float/double descriptor.</summary>
    /// <typeparam name="TOwner">The target Node type.</typeparam>
    /// <typeparam name="TValue">Exactly float or double.</typeparam>
    /// <param name="property">A writable scalar descriptor.</param>
    /// <param name="atPosition">Insertion index or minus one to append.</param>
    /// <returns>The inserted index.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="ArgumentException">The descriptor is read-only.</exception>
    /// <exception cref="ArgumentNullException">The descriptor is null.</exception>
    /// <exception cref="NotSupportedException">The descriptor value type is neither float nor double.</exception>
    public int AddBezierTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, int atPosition = -1) where TOwner : Node
    { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(property); if (property.IsReadOnly) throw new ArgumentException("Bézier tracks require a writable descriptor.", nameof(property)); if (typeof(TValue) != typeof(float) && typeof(TValue) != typeof(double)) throw new NotSupportedException("Bézier property values must be float or double."); return InsertTrack(new AnimationBezierTrack<TOwner, TValue>(property), atPosition); }
    /// <summary>Adds an event track whose keys use exact typed callback and argument payloads.</summary>
    /// <typeparam name="TOwner">The target Node type.</typeparam>
    /// <param name="atPosition">Insertion index or minus one to append.</param>
    /// <returns>The inserted index.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    public int AddMethodTrack<TOwner>(int atPosition = -1) where TOwner : Node => InsertTrack(new AnimationMethodTrack<TOwner>(), atPosition);
    /// <summary>Inserts a callback key without erasing its exact payload signature.</summary>
    /// <typeparam name="TOwner">The track receiver type.</typeparam><typeparam name="TArguments">The key payload type.</typeparam>
    /// <param name="track">An existing method track.</param><param name="time">Finite key seconds.</param><param name="value">The typed callback key.</param><param name="transition">Finite stored easing metadata.</param><returns>The sorted key index.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidCastException">The track receiver or key payload type differs.</exception>
    public int TrackInsertKey<TOwner, TArguments>(int track, double time, AnimationMethodKey<TOwner, TArguments> value, double transition = 1) where TOwner : Node => TrackInsertKey<AnimationMethodKey<TOwner>>(track, time, value, transition);
    /// <summary>Replaces a callback key while retaining exact receiver/payload typing.</summary>
    /// <typeparam name="TOwner">The track receiver type.</typeparam><typeparam name="TArguments">The key payload type.</typeparam>
    /// <param name="track">An existing method track.</param><param name="key">An existing key index.</param><param name="value">The typed callback key.</param>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidCastException">The track receiver or key payload type differs.</exception>
    public void TrackSetKeyValue<TOwner, TArguments>(int track, int key, AnimationMethodKey<TOwner, TArguments> value) where TOwner : Node => TrackSetKeyValue<AnimationMethodKey<TOwner>>(track, key, value);
    private int InsertTrack(AnimationTrack track, int index) { ThrowIfDisposed(); if (index == -1) index = _tracks.Count; if ((uint)index > (uint)_tracks.Count) throw new ArgumentOutOfRangeException(nameof(index)); _tracks.Insert(index, track); EmitChanged(); return index; }
    private AnimationBezierTrackData Bezier(int track) => Get(track) as AnimationBezierTrackData ?? throw new InvalidOperationException("Operation requires a Bézier track.");
    /// <summary>Inserts/replaces a scalar Bézier key, clamping incoming/outgoing handle time signs.</summary>
    /// <param name="track">An existing Bézier track.</param><param name="time">Finite seconds.</param><param name="value">The finite scalar value.</param><param name="inHandle">Incoming offset.</param><param name="outHandle">Outgoing offset.</param>
    /// <returns>The sorted key index.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public int BezierTrackInsertKey(int track, double time, double value, Vector2 inHandle = default, Vector2 outHandle = default) { _ = Bezier(track); return TrackInsertKey(track, time, new AnimationBezierKey(value, inHandle, outHandle)); }
    /// <summary>Returns a scalar Bézier key value.</summary><param name="track">The track index.</param><param name="key">The key index.</param><returns>The scalar value.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public double BezierTrackGetKeyValue(int track, int key) => Bezier(track).Values[key].Value;
    /// <summary>Returns the incoming key control offset.</summary><param name="track">The track index.</param><param name="key">The key index.</param><returns>The incoming offset.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public Vector2 BezierTrackGetKeyInHandle(int track, int key) => Bezier(track).Values[key].InHandle;
    /// <summary>Returns the outgoing key control offset.</summary><param name="track">The track index.</param><param name="key">The key index.</param><returns>The outgoing offset.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public Vector2 BezierTrackGetKeyOutHandle(int track, int key) => Bezier(track).Values[key].OutHandle;
    /// <summary>Replaces a finite scalar Bézier value.</summary><param name="track">The track index.</param><param name="key">The key index.</param><param name="value">The scalar value.</param>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public void BezierTrackSetKeyValue(int track, int key, double value) { var item = Bezier(track).Values[key]; TrackSetKeyValue(track, key, item with { Value = value }); }
    /// <summary>Sets an incoming handle, clamping positive time offset to zero.</summary>
    /// <param name="track">The track index.</param><param name="key">The key index.</param><param name="inHandle">The incoming offset.</param><param name="balancedValueTimeRatio">Finite authoring balance metadata; runtime handles are free and are not editor-balanced.</param>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public void BezierTrackSetKeyInHandle(int track, int key, Vector2 inHandle, float balancedValueTimeRatio = 1) { Finite(balancedValueTimeRatio); var item = Bezier(track).Values[key]; TrackSetKeyValue(track, key, item with { InHandle = inHandle }); }
    /// <summary>Sets an outgoing handle, clamping negative time offset to zero.</summary>
    /// <param name="track">The track index.</param><param name="key">The key index.</param><param name="outHandle">The outgoing offset.</param><param name="balancedValueTimeRatio">Finite authoring balance metadata; runtime handles are free and are not editor-balanced.</param>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public void BezierTrackSetKeyOutHandle(int track, int key, Vector2 outHandle, float balancedValueTimeRatio = 1) { Finite(balancedValueTimeRatio); var item = Bezier(track).Values[key]; TrackSetKeyValue(track, key, item with { OutHandle = outHandle }); }
    /// <summary>Samples scalar Bézier time/value geometry; no seam interpolation is performed.</summary><param name="track">The track index.</param><param name="time">Finite seconds.</param><returns>The scalar value, or zero when no key lies within the clip length.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public double BezierTrackInterpolate(int track, double time) { Finite(time); return Bezier(track).InterpolateBezier(time, this); }
    /// <summary>Returns a method key's diagnostic name.</summary><param name="track">The method track index.</param><param name="key">The key index.</param><returns>The exact name.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    public string MethodTrackGetName(int track, int key) => Get(track).MethodName(key);
    /// <summary>Returns a method key's exact typed argument payload.</summary><typeparam name="TArguments">The exact declared payload type.</typeparam><param name="track">The method track index.</param><param name="key">The key index.</param><returns>The typed payload; custom references remain borrowed.</returns>
    /// <exception cref="ObjectDisposedException">The animation has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is invalid or a numeric argument is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The track kind differs from the requested operation.</exception>
    /// <exception cref="InvalidCastException">The track receiver or key payload type differs.</exception>
    public TArguments MethodTrackGetParams<TArguments>(int track, int key) => Get(track).MethodArguments<TArguments>(key);
}
internal abstract class AnimationBezierTrackData : AnimationTypedTrack<AnimationBezierKey>
{
    protected AnimationBezierTrackData() : base(null) { Interpolation = Animation.InterpolationType.Linear; }
    internal override Animation.TrackType Kind => Animation.TrackType.Bezier;
    internal override AnimationBezierKey Normalize(AnimationBezierKey value) { Animation.Finite(value.Value); if (!value.InHandle.IsFinite() || !value.OutHandle.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); return value with { InHandle = new(Math.Min(0, value.InHandle.X), value.InHandle.Y), OutHandle = new(Math.Max(0, value.OutHandle.X), value.OutHandle.Y) }; }
    internal override void ValidateInterpolation(Animation.InterpolationType mode) { }
    internal double InterpolateBezier(double time, Animation clip)
    {
        var lastWithin = Times.BinarySearch(clip.Length); if (lastWithin < 0) lastWithin = ~lastWithin - 1;
        if (lastWithin < 0) return 0; if (lastWithin == 0) return Values[0].Value;
        var index = Times.BinarySearch(time); if (index >= 0) return Values[index].Value; index = ~index - 1;
        if (index < 0) return Values[0].Value; if (index >= Times.Count - 1) return Values[^1].Value;
        var start = Values[index]; var end = Values[index + 1]; var duration = Times[index + 1] - Times[index]; var t = time - Times[index]; double low = 0, high = 1;
        static double Cubic(double a, double b, double c, double d, double s) { var r = 1 - s; return r * r * r * a + 3 * r * r * s * b + 3 * r * s * s * c + s * s * s * d; }
        double X(double s) => Cubic(0, start.OutHandle.X, duration + end.InHandle.X, duration, s);
        double Y(double s) => Cubic(start.Value, start.Value + start.OutHandle.Y, end.Value + end.InHandle.Y, end.Value, s);
        for (var i = 0; i < 10; i++) { var middle = (low + high) * .5; if (X(middle) < t) low = middle; else high = middle; }
        var x0 = X(low); var x1 = X(high); var mix = x1 == x0 ? 0 : (t - x0) / (x1 - x0); var result = Y(low) + (Y(high) - Y(low)) * mix; Animation.Finite(result); return result;
    }
}
internal sealed class AnimationBezierTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> descriptor) : AnimationBezierTrackData where TOwner : Node
{
    internal override string PropertyName => descriptor.Name;
    internal override AnimationTrack Copy(bool deep = false, Func<Resource?, Resource?>? duplicate = null) { var copy = new AnimationBezierTrack<TOwner, TValue>(descriptor); CopyTo(copy, deep, duplicate); return copy; }
    internal override AnimationBinding? Bind(Node root) { var target = NodePath.Length == 0 ? root : root.GetNodeOrNull(NodePath); return target is TOwner owner ? new Binding(this, owner, descriptor) : null; }
    private static TValue Convert(double value) { if (typeof(TValue) == typeof(float)) { var scalar = (float)value; Animation.Finite(scalar); return Unsafe.As<float, TValue>(ref scalar); } return Unsafe.As<double, TValue>(ref value); }
    private sealed class Binding(AnimationBezierTrack<TOwner, TValue> track, TOwner owner, PropertyDescriptor<TOwner, TValue> property) : AnimationBinding
    {
        private AnimationBlendProperty<TValue>? _blend;
        internal override void AttachBlend(AnimationMixer mixer) => _blend = mixer.BlendProperty(owner, property, TweenValue<TValue>.Interpolate);
        internal override void AddWeight(double weight, int pass) { if (track.Enabled && track.Times.Count != 0) _blend?.AddWeight(weight, pass); }
        internal override void SetRest() { if (track.Values.Count != 0) _blend?.SetRest(Convert(track.Values[0].Value)); }
        internal override AnimationCaptureValue? Capture(Animation animation, int index) => null;
        private bool Live(AnimationMixer mixer) => track.Enabled && track.Values.Count != 0 && !owner.IsDisposed && !owner.IsQueuedForDeletion && ReferenceEquals(owner.Tree, mixer.Tree);
        internal override void Mix(AnimationMixFrame frame, int index, double weight, AnimationMixer mixer, long generation) { if (_blend is null || !Live(mixer) || Math.Abs(weight) < 1e-12) return; var revision = frame.Animation.ChangeRevision; var value = mixer.ProcessKey(frame.Animation, index, Convert(track.InterpolateBezier(frame.Time, frame.Animation)), owner.InstanceID); if (mixer.EvaluationCurrent(generation) && !frame.Animation.IsDisposed && revision == frame.Animation.ChangeRevision) _blend.Add(value, weight, mixer.Deterministic); }
        internal override void Apply(Animation animation, int index, double time, bool backward, double? previous, AnimationMixer mixer, long generation) { if (!Live(mixer)) return; var revision = animation.ChangeRevision; var value = mixer.ProcessKey(animation, index, Convert(track.InterpolateBezier(time, animation)), owner.InstanceID); if (mixer.IsBindingCurrent(animation, generation) && !animation.IsDisposed && revision == animation.ChangeRevision && !owner.IsDisposed) property.SetValue(owner, value); }
    }
}
internal sealed class AnimationMethodTrack<TOwner> : AnimationTypedTrack<AnimationMethodKey<TOwner>> where TOwner : Node
{
    internal AnimationMethodTrack() : base(null) { }
    internal override Animation.TrackType Kind => Animation.TrackType.Method;
    internal override string PropertyName => "";
    internal override AnimationMethodKey<TOwner> Normalize(AnimationMethodKey<TOwner> value) { ArgumentNullException.ThrowIfNull(value); return value; }
    internal override TValue ReadKey<TValue>(int key) { var value = Values[key]; if (typeof(TValue) != typeof(AnimationMethodKey<TOwner>) && typeof(TValue) != value.GetType()) throw new InvalidCastException("Key type differs from the declared method signature."); return (TValue)(object)value; }
    internal override string MethodName(int key) => Values[key].Name;
    internal override T MethodArguments<T>(int key) => Values[key].ReadArguments<T>();
    internal override void ValidateInterpolation(Animation.InterpolationType mode) { }
    internal override AnimationTrack Copy(bool deep = false, Func<Resource?, Resource?>? duplicate = null)
    {
        var copy = new AnimationMethodTrack<TOwner>(); CopyTo(copy, false, null);
        for (var i = 0; i < copy.Values.Count; i++) copy.Values[i] = Values[i].Copy(deep, duplicate);
        return copy;
    }
    internal override AnimationBinding? Bind(Node root) { var target = NodePath.Length == 0 ? root : root.GetNodeOrNull(NodePath); return target is TOwner owner ? new Binding(this, owner) : null; }
    private sealed class Binding : AnimationBinding
    {
        private readonly AnimationMethodTrack<TOwner> _track;
        private readonly TOwner _owner;
        private readonly Stack<Call> _free = new();
        private int _prepared;
        internal Binding(AnimationMethodTrack<TOwner> track, TOwner owner) { _track = track; _owner = owner; PrepareMethodCalls(Math.Max(16, track.Times.Count)); }
        private sealed class Call
        {
            internal readonly Action Dispatch; internal AnimationMethodKey<TOwner>? Key; internal SceneTree? Tree;
            private readonly Binding _binding;
            internal Call(Binding binding) { _binding = binding; Dispatch = Invoke; }
            private void Invoke() { var key = Key; var tree = Tree; try { if (key is not null && !_binding._owner.IsDisposed && !_binding._owner.IsQueuedForDeletion && ReferenceEquals(_binding._owner.Tree, tree)) key.Invoke(_binding._owner); } finally { Key = null; Tree = null; _binding._free.Push(this); } }
        }
        internal override void PrepareMethodCalls(int capacity) { while (_prepared < capacity) { _free.Push(new(this)); _prepared++; } }
        internal override void AttachBlend(AnimationMixer mixer) => PrepareMethodCalls(mixer.MethodCallbackCapacity);
        internal override void AddWeight(double weight, int pass) { }
        internal override void SetRest() { }
        internal override AnimationCaptureValue? Capture(Animation animation, int index) => null;
        private bool Current(Animation animation, AnimationMixer mixer, long generation, long revision) => _track.Enabled && !_owner.IsDisposed && !_owner.IsQueuedForDeletion && !animation.IsDisposed && revision == animation.ChangeRevision && ReferenceEquals(_owner.Tree, mixer.Tree) && mixer.IsBindingCurrent(animation, generation);
        private void Invoke(int key, AnimationMixer mixer)
        {
            var value = _track.Values[key];
            if (mixer.CallbackModeMethod == AnimationMixer.AnimationCallbackModeMethod.Immediate || _owner.Tree is null) { value.Invoke(_owner); return; }
            if (!_free.TryPop(out var call)) throw new InvalidOperationException("Prepared method callback capacity exhausted. Prepare a larger pending capacity before advancing.");
            call.Key = value; call.Tree = _owner.Tree; try { call.Tree.Defer(call.Dispatch); } catch { call.Key = null; call.Tree = null; _free.Push(call); throw; }
        }
        private void SeekKey(Animation animation, int track, double time, bool external, AnimationMixer mixer) { var i = animation.TrackFindKey(track, time, external ? Animation.FindMode.Nearest : Animation.FindMode.Exact, true); if (i >= 0) Invoke(i, mixer); }
        private void Range(Animation animation, double from, double to, bool backward, AnimationMixer mixer, long generation, long revision)
        {
            if (backward) { for (var i = _track.Times.Count - 1; i >= 0; i--) { var time = _track.Times[i]; if (time >= from || time < to) continue; Invoke(i, mixer); if (!Current(animation, mixer, generation, revision)) return; } }
            else for (var i = 0; i < _track.Times.Count; i++) { var time = _track.Times[i]; if (time <= from || time > to) continue; Invoke(i, mixer); if (!Current(animation, mixer, generation, revision)) return; }
        }
        internal override void Mix(AnimationMixFrame frame, int index, double weight, AnimationMixer mixer, long generation)
        {
            var clip = frame.Animation; var revision = clip.ChangeRevision;
            if (frame.UpdateOnly || Math.Abs(weight) < 1e-5 || !Current(clip, mixer, generation, revision) || _track.Times.Count == 0) return;
            if (!frame.Previous.HasValue) { SeekKey(clip, index, frame.Time, frame.ExternalSeeking, mixer); return; }
            var cursor = frame.Previous.Value;
            if (frame.IncludeStart) { SeekKey(clip, index, cursor, frame.ExternalSeeking, mixer); if (!Current(clip, mixer, generation, revision)) return; }
            var remaining = frame.Movement; var start = frame.Start; var end = frame.End < 0 ? clip.Length : frame.End;
            if (end <= start) return;
            while (remaining != 0 && Current(clip, mixer, generation, revision))
            {
                var reverse = remaining < 0; var endpoint = reverse ? start : end; var travel = Math.Min(Math.Abs(remaining), Math.Abs(endpoint - cursor)); var next = cursor + (reverse ? -travel : travel);
                Range(clip, cursor, next, reverse, mixer, generation, revision); if (!Current(clip, mixer, generation, revision)) return;
                var rest = remaining + (reverse ? travel : -travel); if (travel > 0 && rest == remaining) throw new InvalidOperationException("Method timeline delta cannot make representable progress."); remaining = rest; cursor = next;
                if (cursor != endpoint || clip.LoopMode == SpriteFrames.LoopMode.None) break;
                if (clip.LoopMode == SpriteFrames.LoopMode.PingPong) remaining = -remaining;
                else { cursor = reverse ? end : start; SeekKey(clip, index, cursor, false, mixer); }
            }
        }
        internal override void Apply(Animation animation, int index, double time, bool backward, double? previous, AnimationMixer mixer, long generation)
        { if (mixer.MethodUpdateOnly || !Current(animation, mixer, generation, animation.ChangeRevision)) return; if (previous.HasValue) Range(animation, previous.Value, time, backward, mixer, generation, animation.ChangeRevision); else SeekKey(animation, index, time, mixer.MethodExternalSeeking, mixer); }
    }
}
