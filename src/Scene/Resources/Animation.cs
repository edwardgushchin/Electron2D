using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>A reusable timeline of strongly typed property keys and named time markers.</summary>
/// <remarks>Author on the scene owner thread. Tracks carry immutable typed descriptors and relative node paths;
/// they never own target nodes. Keys are sorted, equal times replace, and copies have independent containers.
/// Continuous tracks interpolate; discrete tracks hold the last key. Capture and other track families require
/// their separate execution contracts. Edits raise Changed except imported metadata. Length defaults to one second.</remarks>
public sealed class Animation : Resource
{
    private static readonly PropertyDescriptor[] AnimationProperties =
    [
        new PropertyDescriptor<Animation, bool>(nameof(CaptureIncluded), n => n.CaptureIncluded),
        new PropertyDescriptor<Animation, double>(nameof(Length), n => n.Length, (n, v) => n.Length = v, _ => 1),
        new PropertyDescriptor<Animation, double>(nameof(Step), n => n.Step, (n, v) => n.Step = v, _ => .033333335),
        new PropertyDescriptor<Animation, SpriteFrames.LoopMode>(nameof(LoopMode), n => n.LoopMode, (n, v) => n.LoopMode = v, _ => SpriteFrames.LoopMode.None),
    ];
    /// <summary>Identifies executable timeline track kinds.</summary>
    public enum TrackType
    {
        /// <summary>Keys target one typed property.</summary>
        Value = 0,
    }
    /// <summary>Selects how neighboring value keys are sampled.</summary>
    public enum InterpolationType
    {
        /// <summary>Hold the preceding key.</summary>
        Nearest = 0,
        /// <summary>Interpolate neighboring values.</summary>
        Linear = 1,
        /// <summary>Interpolate using neighboring time-aware cubic control values.</summary>
        Cubic = 2,
        /// <summary>Interpolate scalar radians along the shortest arc.</summary>
        LinearAngle = 3,
        /// <summary>Interpolate scalar radians using a time-aware cubic curve.</summary>
        CubicAngle = 4,
    }
    /// <summary>Selects continuous evaluation or discrete key holding.</summary>
    public enum UpdateMode
    {
        /// <summary>Evaluate interpolated values on each update.</summary>
        Continuous = 0,
        /// <summary>Hold values between keys.</summary>
        Discrete = 1,
        /// <summary>Continuously evaluate while admitting capture of the current target value.</summary>
        Capture = 2,
    }
    /// <summary>Selects preceding, approximate or exact key lookup.</summary>
    public enum FindMode
    {
        /// <summary>Find the preceding key, or following key when searching backwards.</summary>
        Nearest = 0,
        /// <summary>Require approximate equality at the candidate time.</summary>
        Approx = 1,
        /// <summary>Require exact time equality.</summary>
        Exact = 2,
    }
    private readonly List<AnimationTrack> _tracks = [];
    private readonly Dictionary<string, (double Time, Color Color)> _markers = new(StringComparer.Ordinal);
    private double _length = 1, _step = 0.033333335;
    private SpriteFrames.LoopMode _loopMode;
    private bool _captureIncluded;

    /// <summary>Gets whether any property track admits capture.</summary>
    public bool CaptureIncluded { get { ThrowIfDisposed(); return _captureIncluded; } }
    /// <summary>Gets or sets duration in seconds; finite values below 0.001 are clamped.</summary>
    public double Length { get { ThrowIfDisposed(); return _length; } set { ThrowIfDisposed(); Finite(value); _length = Math.Max(.001, value); EmitChanged(); } }
    /// <summary>Gets or sets the authoring time-step hint, initially approximately one thirtieth second.</summary>
    public double Step { get { ThrowIfDisposed(); return _step; } set { ThrowIfDisposed(); Finite(value); _step = value; EmitChanged(); } }
    /// <summary>Gets or sets endpoint behavior, sharing the existing animation endpoint contract.</summary>
    public SpriteFrames.LoopMode LoopMode { get { ThrowIfDisposed(); return _loopMode; } set { ThrowIfDisposed(); Valid(value); _loopMode = value; EmitChanged(); } }

    /// <summary>Adds a property track with an immutable typed descriptor and optional interpolation override.</summary>
    /// <typeparam name="TOwner">The node type accepted by the descriptor.</typeparam>
    /// <typeparam name="TValue">The exact key value type.</typeparam>
    /// <param name="property">A writable descriptor; shared safely by resource copies.</param>
    /// <param name="atPosition">Insertion index, or minus one to append.</param>
    /// <param name="interpolate">Optional typed linear interpolator; otherwise the engine math profile is used.</param>
    /// <returns>The inserted track index. Unsupported value types initially use nearest interpolation.</returns>
    public int AddTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, int atPosition = -1,
        Func<TValue, TValue, double, TValue>? interpolate = null) where TOwner : Node
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(property);
        if (property.IsReadOnly) throw new ArgumentException("Animation requires a writable property.", nameof(property));
        if (atPosition == -1) atPosition = _tracks.Count;
        if ((uint)atPosition > (uint)_tracks.Count) throw new ArgumentOutOfRangeException(nameof(atPosition));
        _tracks.Insert(atPosition, new AnimationValueTrack<TOwner, TValue>(property, interpolate ?? TweenValue<TValue>.Interpolate));
        EmitChanged(); return atPosition;
    }
    /// <summary>Returns the number of property tracks.</summary>
    public int GetTrackCount() { ThrowIfDisposed(); return _tracks.Count; }
    /// <summary>Removes one track and emits Changed.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public void RemoveTrack(int track) { Get(track); _tracks.RemoveAt(track); RefreshCaptureIncluded(); EmitChanged(); }
    /// <summary>Copies one track and all keys into another animation without sharing its key container.</summary>
    /// <param name="toAnimation">The live destination animation, whose track list receives an independent copy.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public void CopyTrack(int track, Animation toAnimation) { var copy = Get(track).Copy(); ArgumentNullException.ThrowIfNull(toAnimation); toAnimation.ThrowIfDisposed(); toAnimation._tracks.Add(copy); toAnimation.RefreshCaptureIncluded(); toAnimation.EmitChanged(); }
    /// <summary>Returns the first track with this exact relative path, or minus one.</summary>
    /// <param name="path">The relative target path.</param>
    /// <param name="type">The executable track kind.</param>
    public int FindTrack(string path, TrackType type = TrackType.Value) { ThrowIfDisposed(); Valid(type); ArgumentNullException.ThrowIfNull(path); return _tracks.FindIndex(t => t.Path == path); }
    /// <summary>Returns the track kind.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public TrackType TrackGetType(int track) { Get(track); return TrackType.Value; }
    /// <summary>Returns a track's relative target node path, including its property suffix.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public string TrackGetPath(int track) => Get(track).Path;
    /// <summary>Sets a relative node path with an optional colon and the descriptor's exact property name.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="path">The relative node/property path.</param>
    public void TrackSetPath(int track, string path)
    {
        var item = Get(track); ArgumentNullException.ThrowIfNull(path);
        var colon = path.IndexOf(':');
        if (colon >= 0 && path[(colon + 1)..] != item.PropertyName) throw new ArgumentException("Property suffix differs from the typed descriptor.", nameof(path));
        item.Path = path; item.NodePath = colon < 0 ? path : path[..colon]; EmitChanged();
    }
    /// <summary>Returns whether a track contributes values.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public bool TrackIsEnabled(int track) => Get(track).Enabled;
    /// <summary>Enables or disables a track.</summary>
    /// <param name="enabled">Whether this track contributes values.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public void TrackSetEnabled(int track, bool enabled) { Get(track).Enabled = enabled; EmitChanged(); }
    /// <summary>Returns whether this track uses compressed storage; value-property tracks never do.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public bool TrackIsCompressed(int track) { Get(track); return false; }
    /// <summary>Returns imported authoring metadata.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public bool TrackIsImported(int track) => Get(track).Imported;
    /// <summary>Sets imported metadata without emitting Changed.</summary>
    /// <param name="imported">The authoring-only imported flag.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public void TrackSetImported(int track, bool imported) { Get(track).Imported = imported; }
    /// <summary>Returns the interpolation mode.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public InterpolationType TrackGetInterpolationType(int track) => Get(track).Interpolation;
    /// <summary>Sets interpolation, rejecting unsupported value/mode combinations before mutation.</summary>
    /// <param name="interpolation">The interpolation mode or loop-wrap flag.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public void TrackSetInterpolationType(int track, InterpolationType interpolation) { Valid(interpolation); var item = Get(track); item.ValidateInterpolation(interpolation); item.Interpolation = interpolation; EmitChanged(); }
    /// <summary>Returns whether looping interpolation connects the last and first key.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public bool TrackGetInterpolationLoopWrap(int track) => Get(track).LoopWrap;
    /// <summary>Sets interpolation across the linear-loop seam.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="interpolation">The typed argument for this operation, using the defaults described above.</param>
    public void TrackSetInterpolationLoopWrap(int track, bool interpolation) { Get(track).LoopWrap = interpolation; EmitChanged(); }
    /// <summary>Returns the value update mode.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public UpdateMode ValueTrackGetUpdateMode(int track) => Get(track).Update;
    /// <summary>Sets continuous interpolation or discrete key holding.</summary>
    /// <param name="mode">The defined update or process mode.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public void ValueTrackSetUpdateMode(int track, UpdateMode mode) { Valid(mode); Get(track).Update = mode; RefreshCaptureIncluded(); EmitChanged(); }
    /// <summary>Returns the number of keys.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public int TrackGetKeyCount(int track) => Get(track).Times.Count;
    /// <summary>Returns a key time in seconds.</summary>
    /// <param name="key">The zero-based existing key index.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public double TrackGetKeyTime(int track, int key) => Get(track).Times[key];
    /// <summary>Returns a key's easing transition, initially one.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="key">The zero-based existing key index.</param>
    public double TrackGetKeyTransition(int track, int key) => Get(track).Transitions[key];
    /// <summary>Sets finite easing; zero holds the source key until the destination time.</summary>
    /// <param name="transition">The finite easing exponent; one is linear.</param>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="key">The zero-based existing key index.</param>
    public void TrackSetKeyTransition(int track, int key, double transition) { Finite(transition); Get(track).Transitions[key] = transition; EmitChanged(); }
    /// <summary>Inserts a typed key, replacing an existing exactly equal time.</summary>
    /// <typeparam name="TValue">The exact declared track value type.</typeparam>
    /// <param name="time">A finite key or marker time in seconds.</param>
    /// <param name="value">The exact declared typed key value.</param>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="transition">The typed argument for this operation, using the defaults described above.</param>
    public int TrackInsertKey<TValue>(int track, double time, TValue value, double transition = 1)
    { Finite(time); Finite(transition); var index = Typed<TValue>(track).Insert(time, value, transition); EmitChanged(); return index; }
    /// <summary>Reads a key of the exact declared value type.</summary>
    /// <typeparam name="TValue">The exact declared track value type.</typeparam>
    /// <param name="key">The zero-based existing key index.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public TValue TrackGetKeyValue<TValue>(int track, int key) => Typed<TValue>(track).Values[key];
    /// <summary>Replaces a typed key value.</summary>
    /// <typeparam name="TValue">The exact declared track value type.</typeparam>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="key">The zero-based existing key index.</param>
    /// <param name="value">The typed argument for this operation, using the defaults described above.</param>
    public void TrackSetKeyValue<TValue>(int track, int key, TValue value) { Typed<TValue>(track).Values[key] = value; EmitChanged(); }
    /// <summary>Moves a key in sorted time order; an exactly equal destination time is replaced.</summary>
    /// <param name="time">A finite key or marker time in seconds.</param>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="key">The zero-based existing key index.</param>
    public void TrackSetKeyTime(int track, int key, double time) { Finite(time); Get(track).MoveKey(key, time); EmitChanged(); }
    /// <summary>Removes a key by index.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="key">The zero-based existing key index.</param>
    public void TrackRemoveKey(int track, int key) { Get(track).RemoveKey(key); EmitChanged(); }
    /// <summary>Removes a key at an approximately matching time, if present.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="time">The finite time in seconds.</param>
    public void TrackRemoveKeyAtTime(int track, double time) { var key = TrackFindKey(track, time, FindMode.Approx); if (key >= 0) TrackRemoveKey(track, key); }
    /// <summary>Finds a preceding or following key; limit restricts candidates to the animation duration.</summary>
    /// <param name="findMode">The key matching rule.</param>
    /// <param name="limit">Whether to exclude keys outside the resource duration.</param>
    /// <param name="backward">Whether to sample or search in reverse direction.</param>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="time">The finite time in seconds.</param>
    public int TrackFindKey(int track, double time, FindMode findMode = FindMode.Approx, bool limit = false, bool backward = false)
    {
        Finite(time); Valid(findMode); var times = Get(track).Times; var key = times.BinarySearch(time);
        if (key < 0) key = backward ? ~key : ~key - 1;
        if (findMode == FindMode.Approx)
        {
            var tolerance = 1e-5 * Math.Max(1, Math.Abs(time));
            var neighbor = backward ? key - 1 : key + 1;
            if ((uint)neighbor < (uint)times.Count && Math.Abs(times[neighbor] - time) <= tolerance) key = neighbor;
            if ((uint)key >= (uint)times.Count || Math.Abs(times[key] - time) > tolerance) return -1;
        }
        if ((uint)key >= (uint)times.Count || (limit && (times[key] < 0 || times[key] > _length))) return -1;
        if (findMode == FindMode.Exact && times[key] != time) return -1;
        return key;
    }
    /// <summary>Samples the typed value at a finite time; throws if no keys exist.</summary>
    /// <typeparam name="TValue">The exact declared track value type.</typeparam>
    /// <param name="track">The zero-based existing track index.</param>
    /// <param name="time">A finite key or marker time in seconds.</param>
    /// <param name="backward">Whether to sample or search in reverse direction.</param>
    public TValue ValueTrackInterpolate<TValue>(int track, double time, bool backward = false) { Finite(time); return Typed<TValue>(track).Sample(time, this, backward); }
    /// <summary>Moves a track toward the end by one position.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public void TrackMoveUp(int track) { Get(track); if (track + 1 < _tracks.Count) TrackSwap(track, track + 1); }
    /// <summary>Moves a track toward the beginning by one position.</summary>
    /// <param name="track">The zero-based existing track index.</param>
    public void TrackMoveDown(int track) { Get(track); if (track > 0) TrackSwap(track, track - 1); }
    /// <summary>Moves a track before the original insertion boundary; the count appends.</summary>
    /// <param name="toIndex">The insertion boundary in the original track list, including its count.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public void TrackMoveTo(int track, int toIndex) { var item = Get(track); if ((uint)toIndex > (uint)_tracks.Count) throw new ArgumentOutOfRangeException(nameof(toIndex)); if (track == toIndex || track == toIndex - 1) return; _tracks.RemoveAt(track); _tracks.Insert(toIndex > track ? toIndex - 1 : toIndex, item); EmitChanged(); }
    /// <summary>Swaps two track positions.</summary>
    /// <param name="withTrack">The second zero-based track index.</param>
    /// <param name="track">The zero-based existing track index.</param>
    public void TrackSwap(int track, int withTrack) { var a = Get(track); var b = Get(withTrack); if (track == withTrack) return; _tracks[track] = b; _tracks[withTrack] = a; EmitChanged(); }
    /// <summary>Removes tracks and restores length and loop defaults, retaining markers and the step hint.</summary>
    public void Clear() { ThrowIfDisposed(); _tracks.Clear(); _captureIncluded = false; _length = 1; _loopMode = SpriteFrames.LoopMode.None; EmitChanged(); }
    /// <summary>Adds or moves a named marker, replacing any marker at approximately the same time and resetting its color.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    /// <param name="time">A finite key or marker time in seconds.</param>
    public void AddMarker(string name, double time) { ThrowIfDisposed(); ArgumentException.ThrowIfNullOrEmpty(name); Finite(time); var replaced = GetMarkerAtTime(time); if (replaced.Length != 0) _markers.Remove(replaced); _markers[name] = (time, new Color(1, 1, 1)); EmitChanged(); }
    /// <summary>Removes a marker if it exists.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public void RemoveMarker(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); if (_markers.Remove(name)) EmitChanged(); }
    /// <summary>Tests marker membership.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public bool HasMarker(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _markers.ContainsKey(name); }
    /// <summary>Returns a marker time, or minus one when absent.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public double GetMarkerTime(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _markers.TryGetValue(name, out var marker) ? marker.Time : -1; }
    /// <summary>Returns the marker color.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public Color GetMarkerColor(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _markers.TryGetValue(name, out var marker) ? marker.Color : Colors.Black; }
    /// <summary>Changes a marker's finite color.</summary>
    /// <param name="color">A color with finite components.</param>
    /// <param name="name">The exact ordinal name.</param>
    public void SetMarkerColor(string name, Color color) { ThrowIfDisposed(); if (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) || !float.IsFinite(color.A)) throw new ArgumentOutOfRangeException(nameof(color)); _markers[name] = (_markers[name].Time, color); EmitChanged(); }
    /// <summary>Returns marker names sorted by time and then ordinal name.</summary>
    public string[] GetMarkerNames() { ThrowIfDisposed(); return _markers.OrderBy(m => m.Value.Time).ThenBy(m => m.Key, StringComparer.Ordinal).Select(m => m.Key).ToArray(); }
    /// <summary>Returns the first marker approximately at a time, or empty.</summary>
    /// <param name="time">A finite key or marker time in seconds.</param>
    public string GetMarkerAtTime(double time) => Marker(time, 0);
    /// <summary>Returns the nearest marker strictly after the time, or empty.</summary>
    /// <param name="time">The finite time in seconds.</param>
    public string GetNextMarker(double time) => Marker(time, 1);
    /// <summary>Returns the nearest marker at or before the time, or empty.</summary>
    /// <param name="time">The finite time in seconds.</param>
    public string GetPrevMarker(double time) => Marker(time, -1);
    private string Marker(double time, int direction)
    {
        ThrowIfDisposed(); Finite(time); string result = ""; var best = double.PositiveInfinity;
        foreach (var (name, marker) in _markers) { var delta = marker.Time - time; if (direction == 0 ? Math.Abs(delta) > 1e-5 : (direction > 0 ? delta <= 0 : delta > 0)) continue; var distance = Math.Abs(delta); if (distance < best || (distance == best && string.CompareOrdinal(name, result) < 0)) { best = distance; result = name; } }
        return result;
    }
    internal bool HasDiscreteTracks() { foreach (var track in _tracks) if (track.Enabled && track.Update == UpdateMode.Discrete) return true; return false; }
    private void RefreshCaptureIncluded() { _captureIncluded = false; foreach (var track in _tracks) if (track.Update == UpdateMode.Capture) { _captureIncluded = true; break; } }
    internal void AddCapturedTrack(AnimationTrack track) => _tracks.Add(track);
    internal AnimationTrack Get(int track) { ThrowIfDisposed(); return _tracks[track]; }
    private AnimationTypedTrack<TValue> Typed<TValue>(int track) => Get(track) as AnimationTypedTrack<TValue> ?? throw new InvalidCastException("Key type differs from the track's declared type.");
    internal static void Finite(double value) { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "A finite value is required."); }
    internal static void Valid<T>(T value) where T : struct, Enum { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Animation();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (Animation)target; copy._tracks.Clear(); foreach (var track in _tracks) copy._tracks.Add(track.Copy(deep, duplicate)); copy._markers.Clear(); foreach (var marker in _markers) copy._markers.Add(marker.Key, marker.Value); copy._length = _length; copy._step = _step; copy._loopMode = _loopMode; copy._captureIncluded = _captureIncluded; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _tracks.Clear(); _markers.Clear(); } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(AnimationProperties);
}

internal abstract class AnimationTrack
{
    internal string Path = "", NodePath = "";
    internal abstract string PropertyName { get; }
    internal bool Enabled = true, Imported, LoopWrap = true;
    internal Animation.InterpolationType Interpolation = Animation.InterpolationType.Linear;
    internal Animation.UpdateMode Update;
    internal readonly List<double> Times = [], Transitions = [];
    internal abstract AnimationTrack Copy(bool deep = false, Func<Resource?, Resource?>? duplicate = null);
    internal abstract void MoveKey(int key, double time);
    internal abstract void RemoveKey(int key);
    internal abstract void ValidateInterpolation(Animation.InterpolationType mode);
    internal abstract AnimationBinding? Bind(Node root);
}
internal abstract class AnimationTypedTrack<T> : AnimationTrack
{
    internal readonly List<T> Values = [];
    internal readonly Func<T, T, double, T>? Interpolate;
    protected AnimationTypedTrack(Func<T, T, double, T>? interpolate) { Interpolate = interpolate; if (interpolate is null) Interpolation = Animation.InterpolationType.Nearest; }
    internal int Insert(double time, T value, double transition)
    { var index = Times.BinarySearch(time); if (index >= 0) { Values[index] = value; Transitions[index] = transition; return index; } index = ~index; Times.Insert(index, time); Values.Insert(index, value); Transitions.Insert(index, transition); return index; }
    internal override void RemoveKey(int key) { Times.RemoveAt(key); Values.RemoveAt(key); Transitions.RemoveAt(key); }
    internal override void MoveKey(int key, double time) { var value = Values[key]; var transition = Transitions[key]; RemoveKey(key); Insert(time, value, transition); }
    internal override void ValidateInterpolation(Animation.InterpolationType mode)
    {
        if (mode != Animation.InterpolationType.Nearest && Interpolate is null) throw new NotSupportedException("This value type requires an explicit typed interpolator.");
        if (mode is Animation.InterpolationType.LinearAngle or Animation.InterpolationType.CubicAngle && typeof(T) != typeof(float) && typeof(T) != typeof(double)) throw new NotSupportedException("Angle interpolation requires scalar radians.");
    }
    internal T Sample(double time, Animation animation, bool backward)
    {
        if (Times.Count == 0) throw new InvalidOperationException("The track has no keys.");
        var last = Times.Count - 1; var i = Times.BinarySearch(time); if (i >= 0) return Values[i]; i = ~i - 1;
        var j = i + 1; double fromTime, toTime;
        if (animation.LoopMode == SpriteFrames.LoopMode.Linear && LoopWrap && last > 0 && Times[last] <= animation.Length && Times[0] >= 0 && (i < 0 || i == last))
        { i = last; j = 0; fromTime = Times[last]; toTime = Times[0] + animation.Length; if (time < Times[0]) time += animation.Length; }
        else { if (i < 0) return Values[0]; if (i >= last) return Values[last]; fromTime = Times[i]; toTime = Times[j]; }
        if (Interpolation == Animation.InterpolationType.Nearest || Update == Animation.UpdateMode.Discrete) return Values[backward ? j : i];
        var linearWeight = (time - fromTime) / (toTime - fromTime);
        var weight = backward ? 1 - Mathf.Ease(1 - linearWeight, Transitions[j]) : Mathf.Ease(linearWeight, Transitions[i]);
        var loop = animation.LoopMode == SpriteFrames.LoopMode.Linear && LoopWrap && last > 0 && Times[last] <= animation.Length && Times[0] >= 0;
        var preIndex = i > 0 ? i - 1 : loop ? last : i; var postIndex = j < last ? j + 1 : loop ? 0 : j;
        var dt = toTime - fromTime; var preTime = Times[preIndex] - fromTime; var postTime = Times[postIndex] - fromTime;
        if (loop && preIndex >= i) preTime -= animation.Length;
        if (loop && (j < i || postIndex <= j)) postTime += animation.Length;
        if (Interpolation is Animation.InterpolationType.LinearAngle or Animation.InterpolationType.CubicAngle)
        {
            var from = ReadScalar(Values[i]); var to = ReadScalar(Values[j]);
            var result = Interpolation == Animation.InterpolationType.LinearAngle ? Mathf.PosMod(Mathf.LerpAngle(from, to, weight), Math.Tau) :
                Mathf.CubicInterpolateAngleInTime(from, to, ReadScalar(Values[preIndex]), ReadScalar(Values[postIndex]), weight, dt, preTime, postTime);
            if (typeof(T) == typeof(float)) { var scalar = (float)result; return Unsafe.As<float, T>(ref scalar); }
            return Unsafe.As<double, T>(ref result);
        }
        if (Interpolation != Animation.InterpolationType.Cubic) return Interpolate!(Values[i], Values[j], weight);
        var t = weight * dt;
        var a1 = Interpolate!(Values[preIndex], Values[i], preTime == 0 ? 0 : (t - preTime) / -preTime);
        var a2 = Interpolate(Values[i], Values[j], t / dt); var a3 = Interpolate(Values[j], Values[postIndex], postTime == dt ? 1 : (t - dt) / (postTime - dt));
        var b1 = Interpolate(a1, a2, dt == preTime ? 0 : (t - preTime) / (dt - preTime)); var b2 = Interpolate(a2, a3, postTime == 0 ? 1 : t / postTime);
        return Interpolate(b1, b2, t / dt);
    }
    private static double ReadScalar(T value) => typeof(T) == typeof(float) ? Unsafe.As<T, float>(ref value) : Unsafe.As<T, double>(ref value);

    protected void CopyTo(AnimationTypedTrack<T> copy, bool deep, Func<Resource?, Resource?>? duplicate) { copy.Path = Path; copy.NodePath = NodePath; copy.Enabled = Enabled; copy.Imported = Imported; copy.LoopWrap = LoopWrap; copy.Interpolation = Interpolation; copy.Update = Update; copy.Times.AddRange(Times); copy.Transitions.AddRange(Transitions); foreach (var value in Values) copy.Values.Add(deep && value is Resource resource ? (T)(object)duplicate!(resource)! : value); }
}
internal sealed class AnimationValueTrack<TOwner, T>(PropertyDescriptor<TOwner, T> property, Func<T, T, double, T>? interpolate) : AnimationTypedTrack<T>(interpolate) where TOwner : Node
{
    internal override string PropertyName => property.Name;
    internal override AnimationTrack Copy(bool deep = false, Func<Resource?, Resource?>? duplicate = null) { var copy = new AnimationValueTrack<TOwner, T>(property, Interpolate); CopyTo(copy, deep, duplicate); return copy; }
    internal override AnimationBinding? Bind(Node root) { var target = string.IsNullOrEmpty(NodePath) ? root : root.GetNodeOrNull(NodePath); return target is TOwner owner ? new Binding(this, owner, property) : null; }
    private sealed class Binding(AnimationValueTrack<TOwner, T> track, TOwner owner, PropertyDescriptor<TOwner, T> descriptor) : AnimationBinding
    {
        private AnimationBlendProperty<T>? _property;
        internal override void AttachBlend(AnimationMixer mixer) { _property = mixer.BlendProperty(owner, descriptor, track.Interpolate); _property.Angle |= track.Interpolation is Animation.InterpolationType.LinearAngle or Animation.InterpolationType.CubicAngle; }
        internal override void AddWeight(double weight, int pass) { if (track.Enabled && track.Times.Count != 0) _property?.AddWeight(weight, pass); }
        internal override void SetRest() { if (track.Times.Count != 0) _property?.SetRest(track.Values[0]); }
        internal override AnimationCaptureValue? Capture(Animation animation, int index)
        { if (_property is null || owner.IsDisposed || owner.IsQueuedForDeletion) return null; var value = descriptor.GetValue(owner); if (typeof(T) == typeof(int[][])) { var source = Unsafe.As<T, int[][]>(ref value); var copy = new int[source.Length][]; for (var i = 0; i < copy.Length; i++) copy[i] = source[i] is null ? null! : (int[])source[i].Clone(); value = Unsafe.As<int[][], T>(ref copy); } else if (value is Array array) value = (T)(object)array.Clone(); var captured = (AnimationTypedTrack<T>)track.Copy(); captured.Times.Clear(); captured.Values.Clear(); captured.Transitions.Clear(); captured.Insert(0, value, 1); captured.Update = Animation.UpdateMode.Continuous; animation.AddCapturedTrack(captured); return new AnimationCaptureValue<T>(animation, index, value, _property); }
        internal override void Mix(AnimationMixFrame frame, int index, double weight, AnimationMixer mixer, long generation)
        {
            var animation = frame.Animation; var time = frame.Time; var backward = frame.Backward; var previous = frame.Previous; var revision = animation.ChangeRevision;
            if (_property is null || !track.Enabled || track.Times.Count == 0 || Math.Abs(weight) < 1e-12 || owner.IsDisposed || owner.IsQueuedForDeletion || !ReferenceEquals(owner.Tree, mixer.Tree)) return;
            if (track.Update == Animation.UpdateMode.Discrete && mixer.CallbackModeDiscrete != AnimationMixer.AnimationCallbackModeDiscrete.ForceContinuous)
            {
                if (!previous.HasValue || frame.Movement == 0) { Apply(animation, index, time, backward, previous, mixer, generation); return; }
                var cursor = previous.Value; var remaining = frame.Movement; var start = frame.Start; var end = frame.End < 0 ? animation.Length : frame.End;
                while (remaining != 0 && mixer.EvaluationCurrent(generation) && !animation.IsDisposed && animation.ChangeRevision == revision)
                {
                    var reverse = remaining < 0; var endpoint = reverse ? start : end; var travel = Math.Min(Math.Abs(remaining), Math.Abs(endpoint - cursor)); var next = cursor + (reverse ? -travel : travel);
                    Apply(animation, index, next, reverse, cursor, mixer, generation);
                    var leftover = remaining + (reverse ? travel : -travel); if (travel > 0 && leftover == remaining) throw new InvalidOperationException("The discrete timeline delta cannot make representable progress."); remaining = leftover; cursor = next;
                    if (!mixer.EvaluationCurrent(generation) || animation.IsDisposed || animation.ChangeRevision != revision || cursor != endpoint || animation.LoopMode == SpriteFrames.LoopMode.None) break;
                    if (animation.LoopMode == SpriteFrames.LoopMode.PingPong) remaining = -remaining;
                    else { cursor = reverse ? end : start; Apply(animation, index, cursor, reverse, null, mixer, generation); }
                }
                return;
            }
            var value = mixer.ProcessKey(animation, index, track.Sample(time, animation, track.Update == Animation.UpdateMode.Discrete && backward), owner.InstanceID);
            if (mixer.EvaluationCurrent(generation) && !animation.IsDisposed && animation.ChangeRevision == revision) _property.Add(value, weight, mixer.Deterministic);
        }
        private void Write(Animation animation, int index, T value, AnimationMixer mixer, long generation, long revision)
        { value = mixer.ProcessKey(animation, index, value, owner.InstanceID); if (!mixer.IsBindingCurrent(animation, generation) || animation.IsDisposed || animation.ChangeRevision != revision || owner.IsDisposed || owner.IsQueuedForDeletion) return; descriptor.SetValue(owner, value); if (track.Update == Animation.UpdateMode.Discrete) mixer.DiscreteWritten(owner.InstanceID, descriptor.Name); }
        private int _lastDiscreteKey = -1;
        internal override void Apply(Animation animation, int trackIndex, double time, bool backward, double? previous, AnimationMixer mixer, long generation)
        {
            if (owner.IsDisposed || owner.IsQueuedForDeletion || !ReferenceEquals(owner.Tree, mixer.Tree) || !track.Enabled || track.Times.Count == 0) return;
            var revision = animation.ChangeRevision;
            if (track.Update == Animation.UpdateMode.Discrete)
            {
                if (previous.HasValue && _lastDiscreteKey < 0)
                {
                    var initialIndex = track.Times.BinarySearch(previous.Value);
                    if (initialIndex < 0) initialIndex = backward ? ~initialIndex : ~initialIndex - 1;
                    _lastDiscreteKey = Math.Clamp(initialIndex, 0, track.Times.Count - 1);
                    Write(animation, trackIndex, track.Sample(previous.Value, animation, backward), mixer, generation, revision);
                    if (!mixer.IsBindingCurrent(animation, generation) || animation.IsDisposed || animation.ChangeRevision != revision || owner.IsDisposed || owner.IsQueuedForDeletion) return;
                }
                if (previous.HasValue)
                {
                    if (backward)
                    {
                        for (var i = track.Times.Count - 1; i >= 0; i--)
                        {
                            if (track.Times[i] >= previous.Value || track.Times[i] < time) continue;
                            _lastDiscreteKey = i; Write(animation, trackIndex, track.Values[i], mixer, generation, revision);
                            if (!mixer.IsBindingCurrent(animation, generation) || animation.IsDisposed || animation.ChangeRevision != revision || owner.IsDisposed || owner.IsQueuedForDeletion) return;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < track.Times.Count; i++)
                        {
                            if (track.Times[i] <= previous.Value || track.Times[i] > time) continue;
                            _lastDiscreteKey = i; Write(animation, trackIndex, track.Values[i], mixer, generation, revision);
                            if (!mixer.IsBindingCurrent(animation, generation) || animation.IsDisposed || animation.ChangeRevision != revision || owner.IsDisposed || owner.IsQueuedForDeletion) return;
                        }
                    }
                    return;
                }
                var index = track.Times.BinarySearch(time);
                if (index < 0) index = backward ? ~index : ~index - 1;
                index = Math.Clamp(index, 0, track.Times.Count - 1);
                _lastDiscreteKey = index;
            }
            Write(animation, trackIndex, track.Sample(time, animation, backward), mixer, generation, revision);
        }
    }

}
internal abstract class AnimationBinding
{
    internal abstract void AttachBlend(AnimationMixer mixer);
    internal abstract void AddWeight(double weight, int pass);
    internal abstract void SetRest();
    internal abstract AnimationCaptureValue? Capture(Animation animation, int index);
    internal abstract void Mix(AnimationMixFrame frame, int index, double weight, AnimationMixer mixer, long generation);
    internal abstract void Apply(Animation animation, int trackIndex, double time, bool backward, double? previous, AnimationMixer mixer, long generation);
}
