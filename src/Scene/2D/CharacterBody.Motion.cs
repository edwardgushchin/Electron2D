namespace Electron2D;

public partial class CharacterBody
{
    private const float FloorAngleTolerance = 0.01f;
    private readonly RID[] _platformExclusion = new RID[1];

    /// <summary>Moves the body by Velocity over the current frame, sliding and classifying contacts.</summary>
    /// <returns>True when this movement recorded at least one body contact.</returns>
    /// <exception cref="InvalidOperationException">The body is detached or is called off the scene owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Velocity times frame delta exceeds the finite physics range.</exception>
    public bool MoveAndSlide()
    {
        EnsureMutable();
        if (!HasBackend) throw new InvalidOperationException("A character must be attached to move and slide.");
        var delta = CurrentDelta();
        if (!float.IsFinite(delta) || !FiniteMotion(_velocity * delta))
            throw new ArgumentOutOfRangeException(nameof(Velocity), "Character motion exceeds the finite physics range.");
        var currentPlatformVelocity = _platformVelocity;
        var lostPlatform = false;
        if ((_onFloor || _onWall) && _platformRID.IsValid())
        {
            var layers = _onFloor ? _platformFloorLayers : _platformWallLayers;
            if ((layers & _platformLayer) != 0 &&
                Space!.TryGetBodyPointMotion(_platformRID, GlobalPosition, out var pointVelocity, out _))
                currentPlatformVelocity = pointVelocity;
            else
            {
                currentPlatformVelocity = Vector2.Zero;
                lostPlatform = true;
            }
        }
        if (!FiniteMotion(currentPlatformVelocity * delta))
            throw new InvalidOperationException("Platform motion exceeds the finite physics range.");
        Space!.PrepareForQuery();
        if (lostPlatform) _platformRID = default;
        _previousPosition = GlobalPosition;
        _slideResults.Clear();
        _lastMotion = Vector2.Zero;
        var wasOnFloor = _onFloor;
        _onFloor = _onWall = _onCeiling = false;

        if (!currentPlatformVelocity.IsZeroApprox())
        {
            var ride = Move(currentPlatformVelocity * delta, recoveryAsCollision: true, excluded: _platformRID);
            if (ride.Collided)
            {
                _slideResults.Add(ride);
                Classify(ride);
            }
        }

        if (_motionMode == CharacterMotionMode.Grounded) MoveGrounded(delta, wasOnFloor);
        else MoveFloating(delta);

        _realVelocity = delta == 0 ? Vector2.Zero : (GlobalPosition - _previousPosition) / delta;
        if (_platformOnLeave != CharacterPlatformOnLeave.DoNothing && !_onFloor && !_onWall)
        {
            if (_platformOnLeave == CharacterPlatformOnLeave.AddUpwardVelocity &&
                currentPlatformVelocity.Dot(_upDirection) < 0)
                currentPlatformVelocity = currentPlatformVelocity.Slide(_upDirection);
            var combined = _velocity + currentPlatformVelocity;
            if (!FiniteMotion(combined))
                throw new InvalidOperationException("Platform departure velocity exceeds the finite physics range.");
            _velocity = combined;
        }
        return _slideResults.Count != 0;
    }

    /// <summary>Snaps this character toward a qualifying floor if it is not already on one.</summary>
    /// <exception cref="InvalidOperationException">The body is detached or is called off the scene owner thread.</exception>
    public void ApplyFloorSnap()
    {
        EnsureMutable();
        if (!HasBackend) throw new InvalidOperationException("A character must be attached to snap to a floor.");
        SnapToFloor(wallAsFloor: false);
    }

    private void MoveGrounded(float delta, bool wasOnFloor)
    {
        var motion = _velocity * delta;
        var horizontal = motion.Slide(_upDirection);
        var previousFloorNormal = _floorNormal;
        _platformRID = default;
        _floorNormal = _platformVelocity = Vector2.Zero;
        var sliding = !_floorStopOnSlope;
        var canApplyConstantSpeed = sliding;
        var applyCeilingVelocity = false;
        var firstSlide = true;
        var facingUp = _velocity.Dot(_upDirection) > 0;
        var lastTravel = Vector2.Zero;

        for (var iteration = 0; iteration < _maxSlides; iteration++)
        {
            var from = GlobalPosition;
            var result = Move(motion, recoveryAsCollision: true);
            _lastMotion = result.Travel;
            var collided = result.Collided;
            if (collided)
            {
                _slideResults.Add(result);
                Classify(result);
                var normal = result.Normal;

                if (_onCeiling && result.ColliderVelocity != Vector2.Zero &&
                    result.ColliderVelocity.Dot(_upDirection) < 0 &&
                    (!_slideOnCeiling || motion.Dot(_upDirection) < 0 || (normal + _upDirection).Length() < 0.01f))
                {
                    applyCeilingVelocity = true;
                    var ceilingVertical = _upDirection * _upDirection.Dot(result.ColliderVelocity);
                    var bodyVertical = _upDirection * _upDirection.Dot(_velocity);
                    if (bodyVertical.Dot(_upDirection) > 0 ||
                        ceilingVertical.LengthSquared() > bodyVertical.LengthSquared())
                        _velocity = ceilingVertical + _velocity.Slide(_upDirection);
                }

                if (_onFloor && _floorStopOnSlope &&
                    (_velocity.Normalized() + _upDirection).Length() < 0.01f)
                {
                    if (result.Travel.Length() <= _safeMargin + 0.00001f)
                        SetGlobalOrigin(GlobalPosition - result.Travel);
                    _velocity = _lastMotion = Vector2.Zero;
                    break;
                }
                if (result.Remainder.IsZeroApprox()) break;

                if (_floorBlockOnWall && _onWall && horizontal.Dot(normal) <= 0)
                {
                    if (wasOnFloor && !_onFloor && !facingUp)
                    {
                        if (result.Travel.Length() <= _safeMargin + 0.00001f)
                            SetGlobalOrigin(GlobalPosition - result.Travel);
                        SnapToFloor(wallAsFloor: true);
                        _velocity = _lastMotion = Vector2.Zero;
                        break;
                    }
                    motion = !_onFloor
                        ? (_upDirection * _upDirection.Dot(result.Remainder)).Slide(normal)
                        : result.Remainder;
                }
                else if (_floorConstantSpeed && IsOnFloorOnly() && canApplyConstantSpeed &&
                         wasOnFloor && motion.Dot(normal) < 0)
                {
                    canApplyConstantSpeed = false;
                    motion = result.Remainder.Slide(normal).Normalized() *
                        (horizontal.Length() - result.Travel.Slide(_upDirection).Length() -
                         lastTravel.Slide(_upDirection).Length());
                }
                else if ((sliding || !_onFloor) && (!_onCeiling || _slideOnCeiling || !facingUp) &&
                         !applyCeilingVelocity)
                {
                    var projected = result.Remainder.Slide(normal);
                    motion = projected.Dot(_velocity) > 0 ? projected : Vector2.Zero;
                    if (_slideOnCeiling && _onCeiling)
                        _velocity = facingUp ? _velocity.Slide(normal)
                            : _upDirection * _upDirection.Dot(_velocity);
                }
                else
                {
                    motion = result.Remainder;
                    if (_onCeiling && !_slideOnCeiling && facingUp)
                    {
                        _velocity = _velocity.Slide(_upDirection);
                        motion = motion.Slide(_upDirection);
                    }
                }
                lastTravel = result.Travel;
            }
            else if (_floorConstantSpeed && firstSlide &&
                     WouldTouchFloorAfterSnap(wasOnFloor, facingUp))
            {
                canApplyConstantSpeed = false;
                sliding = true;
                SetGlobalOrigin(from);
                motion = motion.Slide(previousFloorNormal).Normalized() * horizontal.Length();
                collided = true;
            }

            canApplyConstantSpeed = !canApplyConstantSpeed && !sliding;
            sliding = true;
            firstSlide = false;
            if (!collided || motion.IsZeroApprox()) break;
        }

        if (!_onFloor && wasOnFloor && !facingUp) SnapToFloor(wallAsFloor: false);
        if (IsOnWallOnly() && _slideResults.Count != 0 &&
            horizontal.Dot(_slideResults[0].Normal) < 0)
        {
            var projected = _velocity.Slide(_slideResults[0].Normal);
            _velocity = horizontal.Dot(projected) < 0
                ? _upDirection * _upDirection.Dot(_velocity)
                : _upDirection * _upDirection.Dot(_velocity) + projected.Slide(_upDirection);
        }
        if (_onFloor && !facingUp) _velocity = _velocity.Slide(_upDirection);
    }

    private void MoveFloating(float delta)
    {
        var motion = _velocity * delta;
        _platformRID = default;
        _floorNormal = _platformVelocity = Vector2.Zero;
        var firstSlide = true;
        for (var iteration = 0; iteration < _maxSlides; iteration++)
        {
            var result = Move(motion, recoveryAsCollision: true);
            _lastMotion = result.Travel;
            if (!result.Collided || motion.IsZeroApprox()) break;
            _slideResults.Add(result);
            Classify(result);
            if (result.Remainder.IsZeroApprox()) break;
            if (_wallMinSlideAngle != 0 &&
                Angle(result.Normal, -_velocity.Normalized()) < _wallMinSlideAngle + FloorAngleTolerance)
                motion = Vector2.Zero;
            else if (firstSlide)
                motion = result.Remainder.Slide(result.Normal).Normalized() *
                    (motion.Length() - result.Travel.Length());
            else motion = result.Remainder.Slide(result.Normal);
            if (motion.Dot(_velocity) <= 0 || motion.IsZeroApprox()) break;
            firstSlide = false;
        }
    }

    private MotionResultData Move(Vector2 motion, bool recoveryAsCollision, bool testOnly = false,
        RID excluded = default, bool collideSeparationRay = false)
    {
        if (!FiniteMotion(motion)) throw new ArgumentOutOfRangeException(nameof(motion));
        _platformExclusion[0] = excluded;
        var from = GlobalTransform;
        var result = PhysicsServer.Service.TestMotionData(GetRID(), from, motion, _safeMargin,
            recoveryAsCollision, _platformExclusion, [], collideSeparationRay);
        if (!testOnly && result.Travel != Vector2.Zero)
            SetGlobalOrigin(from.Origin + result.Travel);
        return result;
    }

    private void SnapToFloor(bool wallAsFloor)
    {
        if (_onFloor) return;
        var length = MathF.Max(_floorSnapLength, _safeMargin);
        var result = Move(-_upDirection * length, recoveryAsCollision: true, testOnly: true,
            collideSeparationRay: true);
        if (!result.Collided) return;
        var floor = Angle(result.Normal, _upDirection) <= _floorMaxAngle + FloorAngleTolerance;
        if (!floor && !(wallAsFloor && Angle(result.Normal, -_upDirection) >
                        _floorMaxAngle + FloorAngleTolerance)) return;
        _onFloor = true;
        _floorNormal = result.Normal;
        SetPlatform(result);
        var travel = result.Travel.Length() > _safeMargin
            ? _upDirection * _upDirection.Dot(result.Travel) : Vector2.Zero;
        if (travel != Vector2.Zero) SetGlobalOrigin(GlobalPosition + travel);
    }

    private bool WouldTouchFloorAfterSnap(bool wasOnFloor, bool facingUp)
    {
        if (_onFloor || !wasOnFloor || facingUp) return false;
        var length = MathF.Max(_floorSnapLength, _safeMargin);
        var result = Move(-_upDirection * length, recoveryAsCollision: true, testOnly: true,
            collideSeparationRay: true);
        return result.Collided && Angle(result.Normal, _upDirection) <= _floorMaxAngle + FloorAngleTolerance;
    }

    private void Classify(in MotionResultData result)
    {
        var normal = result.Normal;
        if (_motionMode == CharacterMotionMode.Grounded &&
            Angle(normal, _upDirection) <= _floorMaxAngle + FloorAngleTolerance)
        {
            _onFloor = true;
            _floorNormal = normal;
            SetPlatform(result);
        }
        else if (_motionMode == CharacterMotionMode.Grounded &&
                 Angle(normal, -_upDirection) <= _floorMaxAngle + FloorAngleTolerance)
            _onCeiling = true;
        else
        {
            _onWall = true;
            _wallNormal = normal;
            if (PhysicsServer.Service.ResolveSceneObject(result.ColliderRID) is not CharacterBody)
                SetPlatform(result);
        }
    }

    private void SetPlatform(in MotionResultData result)
    {
        if (!Space!.TryGetBodyPointMotion(result.ColliderRID, result.Point, out _, out var layer)) return;
        _platformRID = result.ColliderRID;
        _platformLayer = layer;
        _platformVelocity = result.ColliderVelocity;
    }

    private float CurrentDelta()
    {
        var tree = Tree!;
        var delta = tree.IsInPhysicsFrame ? PhysicsProcessDeltaTime : ProcessDeltaTime;
        if (delta == 0) delta = PhysicsProcessDeltaTime;
        if (delta == 0 && tree.PhysicsFrameCount == 0) delta = 1d / 60d;
        return (float)delta;
    }

    private void SetGlobalOrigin(Vector2 position) =>
        GlobalTransform = new Transform(GlobalRotation, Vector2.One, 0, position);

    private static float Angle(Vector2 normal, Vector2 up) =>
        Mathf.Acos(Mathf.Clamp(normal.Dot(up), -1f, 1f));

    private static bool FiniteMotion(Vector2 motion) =>
        motion.IsFinite() && float.IsFinite(motion.Length());
}
