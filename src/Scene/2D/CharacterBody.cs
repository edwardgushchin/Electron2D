using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

/// <summary>Selects grounded floor/ceiling classification or floating all-wall motion.</summary>
public enum CharacterMotionMode
{
    /// <summary>Classifies contacts using UpDirection and FloorMaxAngle.</summary>
    Grounded = 0,
    /// <summary>Treats every contact as a wall for omnidirectional motion.</summary>
    Floating = 1
}

/// <summary>Selects what happens to a platform's velocity when a character leaves it.</summary>
public enum CharacterPlatformOnLeave
{
    /// <summary>Adds the last platform point velocity.</summary>
    AddVelocity = 0,
    /// <summary>Adds platform velocity except a downward component.</summary>
    AddUpwardVelocity = 1,
    /// <summary>Leaves character velocity unchanged.</summary>
    DoNothing = 2
}

/// <summary>A manually moved body that slides along contacts and tracks floor, wall and platform state.</summary>
public partial class CharacterBody : PhysicsBody
{
    private static readonly PropertyDescriptor[] CharacterProperties =
    [
        new PropertyDescriptor<CharacterBody, Vector2>(nameof(Velocity), body => body.Velocity,
            (body, value) => body.Velocity = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<CharacterBody, CharacterMotionMode>(nameof(MotionMode), body => body.MotionMode,
            (body, value) => body.MotionMode = value, _ => CharacterMotionMode.Grounded, stored: true),
        new PropertyDescriptor<CharacterBody, CharacterPlatformOnLeave>(nameof(PlatformOnLeave), body => body.PlatformOnLeave,
            (body, value) => body.PlatformOnLeave = value, _ => CharacterPlatformOnLeave.AddVelocity, stored: true),
        new PropertyDescriptor<CharacterBody, float>(nameof(SafeMargin), body => body.SafeMargin,
            (body, value) => body.SafeMargin = value, _ => 0.08f, stored: true),
        new PropertyDescriptor<CharacterBody, bool>(nameof(FloorStopOnSlope), body => body.FloorStopOnSlope,
            (body, value) => body.FloorStopOnSlope = value, _ => true, stored: true),
        new PropertyDescriptor<CharacterBody, bool>(nameof(FloorConstantSpeed), body => body.FloorConstantSpeed,
            (body, value) => body.FloorConstantSpeed = value, _ => false, stored: true),
        new PropertyDescriptor<CharacterBody, bool>(nameof(FloorBlockOnWall), body => body.FloorBlockOnWall,
            (body, value) => body.FloorBlockOnWall = value, _ => true, stored: true),
        new PropertyDescriptor<CharacterBody, bool>(nameof(SlideOnCeiling), body => body.SlideOnCeiling,
            (body, value) => body.SlideOnCeiling = value, _ => true, stored: true),
        new PropertyDescriptor<CharacterBody, int>(nameof(MaxSlides), body => body.MaxSlides,
            (body, value) => body.MaxSlides = value, _ => 4, stored: true),
        new PropertyDescriptor<CharacterBody, float>(nameof(FloorMaxAngle), body => body.FloorMaxAngle,
            (body, value) => body.FloorMaxAngle = value, _ => Mathf.Pi / 4f, stored: true),
        new PropertyDescriptor<CharacterBody, float>(nameof(FloorSnapLength), body => body.FloorSnapLength,
            (body, value) => body.FloorSnapLength = value, _ => 1f, stored: true),
        new PropertyDescriptor<CharacterBody, float>(nameof(WallMinSlideAngle), body => body.WallMinSlideAngle,
            (body, value) => body.WallMinSlideAngle = value, _ => Mathf.Pi / 12f, stored: true),
        new PropertyDescriptor<CharacterBody, Vector2>(nameof(UpDirection), body => body.UpDirection,
            (body, value) => body.UpDirection = value, _ => Vector2.Up, stored: true),
        new PropertyDescriptor<CharacterBody, uint>(nameof(PlatformFloorLayers), body => body.PlatformFloorLayers,
            (body, value) => body.PlatformFloorLayers = value, _ => uint.MaxValue, stored: true),
        new PropertyDescriptor<CharacterBody, uint>(nameof(PlatformWallLayers), body => body.PlatformWallLayers,
            (body, value) => body.PlatformWallLayers = value, _ => 0u, stored: true)
    ];

    private readonly List<MotionResultData> _slideResults = [];
    private Vector2 _velocity;
    private CharacterMotionMode _motionMode;
    private CharacterPlatformOnLeave _platformOnLeave;
    private float _safeMargin = 0.08f;
    private bool _floorStopOnSlope = true;
    private bool _floorConstantSpeed;
    private bool _floorBlockOnWall = true;
    private bool _slideOnCeiling = true;
    private int _maxSlides = 4;
    private float _floorMaxAngle = Mathf.Pi / 4f;
    private float _floorSnapLength = 1f;
    private float _wallMinSlideAngle = Mathf.Pi / 12f;
    private Vector2 _upDirection = Vector2.Up;
    private uint _platformFloorLayers = uint.MaxValue;
    private uint _platformWallLayers;
    private Vector2 _floorNormal;
    private Vector2 _wallNormal;
    private Vector2 _platformVelocity;
    private Vector2 _lastMotion;
    private Vector2 _previousPosition;
    private Vector2 _realVelocity;
    private Vector2 _resolvedGravity;
    private RID _platformRID;
    private uint _platformLayer;
    private bool _onFloor;
    private bool _onWall;
    private bool _onCeiling;
    private Transform _solverPose = Transform.Identity;
    private bool _queryPoseApplied;

    /// <summary>Creates a grounded character with zero velocity and four permitted slides.</summary>
    public CharacterBody() { }

    /// <summary>Gets or sets the desired velocity in scene units per second.</summary>
    /// <value>Zero by default; MoveAndSlide can update this value after floor or ceiling contact.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component or vector length is nonfinite.</exception>
    public Vector2 Velocity
    {
        get { EnsureReadable(); return _velocity; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite() || !float.IsFinite(value.Length()))
                throw new ArgumentOutOfRangeException(nameof(value));
            _velocity = value;
        }
    }

    /// <summary>Gets or sets grounded or floating collision classification.</summary>
    /// <value>Grounded by default.</value>
    public CharacterMotionMode MotionMode
    {
        get { EnsureReadable(); return _motionMode; }
        set
        {
            EnsureMutable();
            if (value is not (CharacterMotionMode.Grounded or CharacterMotionMode.Floating))
                throw new ArgumentOutOfRangeException(nameof(value));
            _motionMode = value;
        }
    }

    /// <summary>Gets or sets how departing platform velocity changes Velocity.</summary>
    /// <value>AddVelocity by default.</value>
    public CharacterPlatformOnLeave PlatformOnLeave
    {
        get { EnsureReadable(); return _platformOnLeave; }
        set
        {
            EnsureMutable();
            if (value is not (CharacterPlatformOnLeave.AddVelocity or
                CharacterPlatformOnLeave.AddUpwardVelocity or CharacterPlatformOnLeave.DoNothing))
                throw new ArgumentOutOfRangeException(nameof(value));
            _platformOnLeave = value;
        }
    }

    /// <summary>Gets or sets finite nonnegative recovery margin in scene units.</summary>
    /// <value>0.08 by default.</value>
    public float SafeMargin
    {
        get { EnsureReadable(); return _safeMargin; }
        set { EnsureMutable(); FiniteNonnegative(value); _safeMargin = value; }
    }

    /// <summary>Gets or sets whether downward movement stops on a floor slope.</summary>
    /// <value>True by default.</value>
    public bool FloorStopOnSlope
    {
        get { EnsureReadable(); return _floorStopOnSlope; }
        set { EnsureMutable(); _floorStopOnSlope = value; }
    }

    /// <summary>Gets or sets constant horizontal speed while traversing floor slopes.</summary>
    /// <value>False by default.</value>
    public bool FloorConstantSpeed
    {
        get { EnsureReadable(); return _floorConstantSpeed; }
        set { EnsureMutable(); _floorConstantSpeed = value; }
    }

    /// <summary>Gets or sets whether a wall blocks forward floor motion.</summary>
    /// <value>True by default.</value>
    public bool FloorBlockOnWall
    {
        get { EnsureReadable(); return _floorBlockOnWall; }
        set { EnsureMutable(); _floorBlockOnWall = value; }
    }

    /// <summary>Gets or sets whether upward motion slides along ceiling contact.</summary>
    /// <value>True by default.</value>
    public bool SlideOnCeiling
    {
        get { EnsureReadable(); return _slideOnCeiling; }
        set { EnsureMutable(); _slideOnCeiling = value; }
    }

    /// <summary>Gets or sets the positive maximum slide count per MoveAndSlide call.</summary>
    /// <value>Four by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is below one.</exception>
    public int MaxSlides
    {
        get { EnsureReadable(); return _maxSlides; }
        set { EnsureMutable(); if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); _maxSlides = value; }
    }

    /// <summary>Gets or sets the finite maximum floor angle in radians.</summary>
    /// <value>Pi divided by four by default.</value>
    public float FloorMaxAngle
    {
        get { EnsureReadable(); return _floorMaxAngle; }
        set { EnsureMutable(); Finite(value); _floorMaxAngle = value; }
    }

    /// <summary>Gets or sets finite nonnegative snap distance toward the floor.</summary>
    /// <value>One scene unit by default.</value>
    public float FloorSnapLength
    {
        get { EnsureReadable(); return _floorSnapLength; }
        set { EnsureMutable(); FiniteNonnegative(value); _floorSnapLength = value; }
    }

    /// <summary>Gets or sets the finite floating-mode minimum wall slide angle in radians.</summary>
    /// <value>Pi divided by twelve by default.</value>
    public float WallMinSlideAngle
    {
        get { EnsureReadable(); return _wallMinSlideAngle; }
        set { EnsureMutable(); Finite(value); _wallMinSlideAngle = value; }
    }

    /// <summary>Gets or sets the finite nonzero normalized grounded up direction.</summary>
    /// <value>(0, -1) by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or nonfinite.</exception>
    public Vector2 UpDirection
    {
        get { EnsureReadable(); return _upDirection; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite() || value == Vector2.Zero || !float.IsFinite(value.Length()))
                throw new ArgumentOutOfRangeException(nameof(value));
            _upDirection = value.Normalized();
        }
    }

    /// <summary>Gets or sets accepted floor-platform collision layers.</summary>
    /// <value>All 32 bits by default.</value>
    public uint PlatformFloorLayers
    {
        get { EnsureReadable(); return _platformFloorLayers; }
        set { EnsureMutable(); _platformFloorLayers = value; }
    }

    /// <summary>Gets or sets accepted wall-platform collision layers.</summary>
    /// <value>Zero by default.</value>
    public uint PlatformWallLayers
    {
        get { EnsureReadable(); return _platformWallLayers; }
        set { EnsureMutable(); _platformWallLayers = value; }
    }

    /// <summary>Gets the last resolved world and Area gravity for this body.</summary>
    internal override Vector2 EffectiveGravity => _resolvedGravity;
    internal void SetResolvedGravity(Vector2 gravity) => _resolvedGravity = gravity;

    internal override bool MovesWithSimulation => false;

    internal override PhysicsServer.BodyMode RequestedBodyMode => PhysicsServer.BodyMode.Kinematic;

    internal override void ApplySceneTransform(Vector2 position, float rotation)
    {
        b2Body_SetTransform(BackendID, Shape.ToBackend(position), b2MakeRot(rotation));
        _queryPoseApplied = true;
    }

    internal void PrepareMotion(double delta)
    {
        var target = GlobalTransform;
        if (!target.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(target.Skew))
            throw new InvalidOperationException("Physics bodies require unit global scale and zero skew.");
        if (_queryPoseApplied)
            b2Body_SetTransform(BackendID, Shape.ToBackend(_solverPose.Origin), b2MakeRot(_solverPose.Rotation));
        var transform = new B2Transform(Shape.ToBackend(target.Origin), b2MakeRot(target.Rotation));
        if (PhysicsMadeStatic) b2Body_SetTransform(BackendID, transform.p, transform.q);
        else b2Body_SetTargetTransform(BackendID, transform, (float)delta, wake: true);
    }

    internal void CaptureSolverPose()
    {
        var position = b2Body_GetPosition(BackendID);
        _solverPose = new Transform(b2Rot_GetAngle(b2Body_GetRotation(BackendID)), Vector2.One, 0,
            new(position.X * PhysicsSpace.UnitsPerMeter, position.Y * PhysicsSpace.UnitsPerMeter));
        _queryPoseApplied = false;
    }

    /// <summary>Returns whether the last MoveAndSlide touched a floor.</summary>
    /// <returns>True after a classified floor contact or snap.</returns>
    public bool IsOnFloor() { EnsureReadable(); return _onFloor; }
    /// <summary>Returns whether only a floor was touched.</summary>
    /// <returns>True when no wall or ceiling was also touched.</returns>
    public bool IsOnFloorOnly() { EnsureReadable(); return _onFloor && !_onWall && !_onCeiling; }
    /// <summary>Returns whether the last MoveAndSlide touched a wall.</summary>
    /// <returns>True after a classified wall contact.</returns>
    public bool IsOnWall() { EnsureReadable(); return _onWall; }
    /// <summary>Returns whether only a wall was touched.</summary>
    /// <returns>True when no floor or ceiling was also touched.</returns>
    public bool IsOnWallOnly() { EnsureReadable(); return _onWall && !_onFloor && !_onCeiling; }
    /// <summary>Returns whether the last MoveAndSlide touched a ceiling.</summary>
    /// <returns>True after a classified ceiling contact.</returns>
    public bool IsOnCeiling() { EnsureReadable(); return _onCeiling; }
    /// <summary>Returns whether only a ceiling was touched.</summary>
    /// <returns>True when no floor or wall was also touched.</returns>
    public bool IsOnCeilingOnly() { EnsureReadable(); return _onCeiling && !_onFloor && !_onWall; }

    /// <summary>Returns the last classified floor normal in global coordinates.</summary>
    /// <returns>The copied normal, or zero before floor contact.</returns>
    public Vector2 GetFloorNormal() { EnsureReadable(); return _floorNormal; }
    /// <summary>Returns the last classified wall normal in global coordinates.</summary>
    /// <returns>The copied normal, or zero before wall contact.</returns>
    public Vector2 GetWallNormal() { EnsureReadable(); return _wallNormal; }
    /// <summary>Returns the travel of the last slide in scene units.</summary>
    /// <returns>Last slide travel, or zero before movement.</returns>
    public Vector2 GetLastMotion() { EnsureReadable(); return _lastMotion; }
    /// <summary>Returns global translation since the start of the last MoveAndSlide.</summary>
    /// <returns>Current global position minus the last movement's start position.</returns>
    public Vector2 GetPositionDelta() { EnsureReadable(); return GlobalPosition - _previousPosition; }
    /// <summary>Returns realized velocity from the last MoveAndSlide in scene units per second.</summary>
    /// <returns>Last actual displacement divided by its frame delta.</returns>
    public Vector2 GetRealVelocity() { EnsureReadable(); return _realVelocity; }
    /// <summary>Returns the last contacted platform's point velocity in scene units per second.</summary>
    /// <returns>The copied velocity, or zero when no platform was recorded.</returns>
    public Vector2 GetPlatformVelocity() { EnsureReadable(); return _platformVelocity; }

    /// <summary>Returns the positive angle between the last floor normal and an up direction.</summary>
    /// <param name="upDirection">The up direction, or null for global (0, -1).</param>
    /// <returns>An angle in radians.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The supplied direction is nonfinite.</exception>
    public float GetFloorAngle(Vector2? upDirection = null)
    {
        EnsureReadable();
        var up = upDirection ?? Vector2.Up;
        if (!up.IsFinite()) throw new ArgumentOutOfRangeException(nameof(upDirection));
        return up == Vector2.Zero ? 0 : Mathf.Acos(Mathf.Clamp(_floorNormal.Dot(up), -1, 1));
    }

    /// <summary>Returns the number of contacts recorded by the last MoveAndSlide.</summary>
    /// <returns>A count from zero through the configured slide limit, plus any platform carry contact.</returns>
    public int GetSlideCollisionCount() { EnsureReadable(); return _slideResults.Count; }

    /// <summary>Returns a caller-owned copy of one slide contact.</summary>
    /// <param name="index">Zero-based contact index.</param>
    /// <returns>The contact snapshot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the last slide list.</exception>
    public KinematicCollision GetSlideCollision(int index)
    {
        EnsureReadable();
        if ((uint)index >= (uint)_slideResults.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return new KinematicCollision(_slideResults[index]);
    }

    /// <summary>Returns a caller-owned copy of the final slide contact.</summary>
    /// <returns>Null when no collision was recorded.</returns>
    public KinematicCollision? GetLastSlideCollision()
    {
        EnsureReadable();
        return _slideResults.Count == 0 ? null : new(_slideResults[^1]);
    }

    /// <summary>Copies one slide contact into a reusable caller-owned snapshot.</summary>
    /// <param name="index">Zero-based contact index.</param>
    /// <param name="result">Live destination snapshot.</param>
    /// <exception cref="ArgumentNullException">The destination is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the last slide list.</exception>
    /// <exception cref="ObjectDisposedException">The body or destination is disposed.</exception>
    public void GetSlideCollision(int index, KinematicCollision result)
    {
        EnsureReadable();
        ArgumentNullException.ThrowIfNull(result);
        if ((uint)index >= (uint)_slideResults.Count) throw new ArgumentOutOfRangeException(nameof(index));
        result.Set(_slideResults[index]);
    }

    /// <summary>Copies the final slide contact into a reusable caller-owned snapshot.</summary>
    /// <param name="result">Live destination; cleared when there was no contact.</param>
    /// <returns>True when a contact was copied, false when the destination was cleared.</returns>
    /// <exception cref="ArgumentNullException">The destination is null.</exception>
    /// <exception cref="ObjectDisposedException">The body or destination is disposed.</exception>
    public bool GetLastSlideCollision(KinematicCollision result)
    {
        EnsureReadable();
        ArgumentNullException.ThrowIfNull(result);
        result.Set(_slideResults.Count == 0 ? default : _slideResults[^1]);
        return _slideResults.Count != 0;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(CharacterProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CharacterBody)
        ? CreateCharacterBody : base.CreateSceneInstanceFactory();

    private static Node CreateCharacterBody() => new CharacterBody();

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        _solverPose = GlobalTransform;
        _queryPoseApplied = false;
        _onFloor = _onWall = _onCeiling = false;
        _platformRID = default;
        _platformVelocity = default;
        _slideResults.Clear();
    }

    private void EnsureReadable() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }

    private static void Finite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void FiniteNonnegative(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
