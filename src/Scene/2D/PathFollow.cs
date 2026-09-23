namespace Electron2D;

/// <summary>A spatial node sampling its direct parent Path by distance.</summary>
/// <remarks>Children inherit the sampled transform. No automatic clock advances Progress; use gameplay processing
/// or a Tween. Binding exists only while attached to a scene. Position/rotation updates preserve scale and skew.
/// Owner-thread mutation, scene capture, synchronous transform notifications and lifetime follow Entity.</remarks>
public class PathFollow : Entity
{
    private Path? _path;
    private float _progress, _hOffset, _vOffset;
    private bool _cubicInterp = true, _loop = true, _rotates = true;
    private long _updateVersion;
    private static readonly PropertyDescriptor[] FollowProperties =
    [
        new PropertyDescriptor<PathFollow, float>(nameof(Progress), n => n.Progress, (n, v) => n.Progress = v, _ => 0, stored: true),
        new PropertyDescriptor<PathFollow, float>(nameof(ProgressRatio), n => n.ProgressRatio, (n, v) => n.ProgressRatio = v, _ => 0),
        new PropertyDescriptor<PathFollow, float>(nameof(HOffset), n => n.HOffset, (n, v) => n.HOffset = v, _ => 0, stored: true),
        new PropertyDescriptor<PathFollow, float>(nameof(VOffset), n => n.VOffset, (n, v) => n.VOffset = v, _ => 0, stored: true),
        new PropertyDescriptor<PathFollow, bool>(nameof(Rotates), n => n.Rotates, (n, v) => n.Rotates = v, _ => true, stored: true),
        new PropertyDescriptor<PathFollow, bool>(nameof(CubicInterp), n => n.CubicInterp, (n, v) => n.CubicInterp = v, _ => true, stored: true),
        new PropertyDescriptor<PathFollow, bool>(nameof(Loop), n => n.Loop, (n, v) => n.Loop = v, _ => true, stored: true),
    ];

    /// <summary>Creates a detached follower at zero progress/offset with rotation, cubic interpolation and looping enabled.</summary>
    public PathFollow() { }

    /// <summary>Gets or sets local distance along the parent curve.</summary>
    /// <value>Zero initially; any finite value can be assigned.</value>
    /// <remarks>While attached to a Path with a curve, wraps by its length when Loop is true, otherwise clamps.
    /// A nonzero request wrapping approximately to zero selects the final endpoint. Detached or null-curve state
    /// retains the raw value. Attachment/curve changes sample it without renormalizing. Every assignment updates
    /// the transform, even if unchanged. A zero-length curve clamps progress to zero but retains the transform.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or curve geometry is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This node or its curve is disposed.</exception>
    /// <exception cref="Exception">A transform callback fails after stored progress has changed.</exception>
    public float Progress
    {
        get { ThrowIfDisposed(); return _progress; }
        set
        {
            EnsureMutable(); Finite(value, nameof(value));
            var progress = value;
            if (_path?.Curve is { } curve)
            {
                var length = curve.GetBakedLength();
                if (_loop && length != 0)
                {
                    progress = Mathf.PosMod(value, length);
                    if (!Mathf.IsZeroApprox(value) && Mathf.IsZeroApprox(progress)) progress = length;
                }
                else progress = Math.Clamp(value, 0, length);
            }
            _progress = progress; UpdateFromPath();
        }
    }

    /// <summary>Gets or sets progress as a fraction of the current curve length.</summary>
    /// <value>Zero when unbound, missing a curve or its length is zero; otherwise Progress divided by length.</value>
    /// <remarks>The setter requires a direct Path parent in a tree and a positive-length curve. It multiplies the
    /// finite ratio by length and delegates to Progress, so looping/clamping applies; the getter can exceed one
    /// after a curve changes length. This derived property is not stored in PackedScene.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The ratio or its distance product is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The binding/curve/length is unavailable, or mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">This node or its curve is disposed.</exception>
    /// <exception cref="Exception">A transform callback fails after progress has changed.</exception>
    public float ProgressRatio
    {
        get { ThrowIfDisposed(); var length = _path?.Curve?.GetBakedLength() ?? 0; return length == 0 ? 0 : _progress / length; }
        set
        {
            EnsureMutable(); Finite(value, nameof(value));
            var length = _path?.Curve?.GetBakedLength() ?? 0;
            if (length == 0) throw new InvalidOperationException("A progress ratio requires an attached Path with a nonzero-length curve.");
            Progress = value * length;
        }
    }

    /// <summary>Gets or sets the forward offset, or local X offset when Rotates is false.</summary>
    /// <value>Zero initially, in finite local units.</value>
    /// <remarks>Every assignment immediately resamples the attached curve.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The offset is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation or valid curve sampling is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or curve is disposed.</exception>
    /// <exception cref="Exception">A transform callback fails after assignment.</exception>
    public float HOffset { get { ThrowIfDisposed(); return _hOffset; } set { EnsureMutable(); Finite(value, nameof(value)); _hOffset = value; UpdateFromPath(); } }

    /// <summary>Gets or sets the perpendicular offset, or local Y offset when Rotates is false.</summary>
    /// <value>Zero initially, in finite local units.</value>
    /// <remarks>Every assignment immediately resamples the attached curve.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The offset is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation or valid curve sampling is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or curve is disposed.</exception>
    /// <exception cref="Exception">A transform callback fails after assignment.</exception>
    public float VOffset { get { ThrowIfDisposed(); return _vOffset; } set { EnsureMutable(); Finite(value, nameof(value)); _vOffset = value; UpdateFromPath(); } }

    /// <summary>Gets or sets whether +X follows the sampled forward direction.</summary>
    /// <value>True initially.</value>
    /// <remarks>Every assignment resamples. False preserves the current rotation and applies offsets in local X/Y.</remarks>
    /// <exception cref="InvalidOperationException">Mutation or valid curve sampling is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node or curve is disposed.</exception>
    /// <exception cref="Exception">A transform callback fails after assignment.</exception>
    public bool Rotates { get { ThrowIfDisposed(); return _rotates; } set { EnsureMutable(); _rotates = value; UpdateFromPath(); } }

    /// <summary>Gets or sets cubic position interpolation between baked points.</summary>
    /// <value>True initially.</value>
    /// <remarks>Assignment stores the policy without resampling; the next progress/offset/rotation/curve update uses it.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool CubicInterp { get { ThrowIfDisposed(); return _cubicInterp; } set { EnsureMutable(); _cubicInterp = value; } }

    /// <summary>Gets or sets whether future Progress assignments wrap by curve length.</summary>
    /// <value>True initially.</value>
    /// <remarks>Assignment does not change current progress or transform. Nonlooping assignments clamp instead.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool Loop { get { ThrowIfDisposed(); return _loop; } set { EnsureMutable(); _loop = value; } }

    internal void UpdateFromPath()
    {
        EnsureMutable(); var version = ++_updateVersion; var path = _path;
        if (path?.Curve is not { } curve || curve.GetBakedLength() == 0) return;
        if (_rotates)
        {
            var pose = curve.SampleBakedWithRotation(_progress, _cubicInterp);
            var position = pose.Origin + pose.X * _hOffset + pose.Y * _vOffset;
            if (!position.IsFinite()) throw new InvalidOperationException("Path offset overflowed finite coordinates.");
            Rotation = pose.X.Angle();
            if (IsDisposed || version != _updateVersion || !ReferenceEquals(_path, path)) return;
            Position = position;
        }
        else
        {
            var position = curve.SampleBaked(_progress, _cubicInterp) + new Vector2(_hOffset, _vOffset);
            if (!position.IsFinite()) throw new InvalidOperationException("Path offset overflowed finite coordinates.");
            Position = position;
        }
    }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        _path = entering ? Parent as Path : null; ++_updateVersion;
        base.OnTreeMembershipChanged(entering);
        if (entering && !IsDisposed) UpdateFromPath();
    }

    /// <inheritdoc />
    /// <remarks>Stores progress, offsets and policies; ProgressRatio is tooling-only and derived on attachment.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(FollowProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(PathFollow) ? CreateFollower : base.CreateSceneInstanceFactory();
    private static Node CreateFollower() => new PathFollow();
    private static void Finite(float value, string name) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name); }
}
