using Float4 = System.Numerics.Vector4;

namespace Electron2D.Examples.WaterPlayground;

internal enum WaterToy { Duck, Boat, Bucket, Wheel, Gate, Ball, Wood, Steel, Platform }

internal sealed partial class WaterSimulation
{
    internal const int BucketSlot = 2, WheelSlot = 3, GateSlot = 4, BallSlot = 5, CargoStart = 6, PlatformSlot = 14;
    private readonly WaterToy[] _kinds = new WaterToy[BodyCount];
    private readonly Dictionary<Vector2, RectangleShape> _rectangles = [];
    private readonly Dictionary<float, CircleShape> _circles = [];
    private readonly List<RID> _joints = [];
    private int _actorLimit;
    private double _emitCredit;
    private long _emitted;
    private float _wheelAngle, _lastWheelRotation;
    private Vector2 _gateVelocity;
    private bool _gateEntering;
    internal float FaucetX { get; set; } = WorldSize.X * .47f;
    internal float FaucetAngle { get; set; }
    internal float FaucetFlow { get; set; } = 1;
    internal bool DrainOpen { get; set; }
    internal int StoredCount => Count - ActiveCount;
    internal Vector2 DrainPosition => new(Size.X - 65, Size.Y - 8);
    internal RID ActorBody(int slot) => _actors[slot];
    internal Transform ActorPose(int slot) => Pose(_actors[slot]);
    internal WaterToy ActorKind(int slot) => _kinds[slot];
    internal float WheelTurns => _wheelAngle / MathF.Tau;
    internal int KindCount(WaterToy kind)
    { var count = 0; for (var i = 0; i < BodyCount; i++) if (_actors[i].IsValid() && _kinds[i] == kind) count++; return count; }

    private RectangleShape Rectangle(float width, float height)
    {
        var size = new Vector2(width, height);
        if (!_rectangles.TryGetValue(size, out var shape)) _rectangles[size] = shape = Own(new RectangleShape { Size = size });
        return shape;
    }
    private CircleShape Round(float radius)
    {
        if (!_circles.TryGetValue(radius, out var shape)) _circles[radius] = shape = Own(new CircleShape { Radius = radius });
        return shape;
    }
    internal bool AddToy(WaterToy kind)
    {
        var slot = kind switch { WaterToy.Bucket => BucketSlot, WaterToy.Wheel => WheelSlot, WaterToy.Gate => GateSlot, WaterToy.Ball => BallSlot, _ => -1 };
        if (kind is WaterToy.Wood or WaterToy.Steel)
        {
            for (var i = CargoStart; i < PlatformSlot; i++) if (!_actors[i].IsValid()) { slot = i; break; }
        }
        if (slot < 0 || _actors[slot].IsValid()) return false;
        _kinds[slot] = kind;
        var position = new Vector2(kind == WaterToy.Bucket ? Size.X * .22f : kind == WaterToy.Ball ? Size.X * .8f : FaucetX, SpawnY(slot));
        RID body;
        switch (kind)
        {
            case WaterToy.Bucket:
                body = Body(position, Rectangle(114, 12), 8);
                PhysicsServer.BodySetShapeTransform(body, 0, new Transform(0, new(0, 44)));
                PhysicsServer.BodyAddShape(body, Rectangle(10, 90).GetRID(), new Transform(0, new(-52, 0)));
                PhysicsServer.BodyAddShape(body, Rectangle(10, 90).GetRID(), new Transform(0, new(52, 0)));
                PhysicsServer.BodySetMass(body, 8); PhysicsServer.BodySetAngularDamp(body, 2);
                break;
            case WaterToy.Wheel:
                position = new(Math.Clamp(FaucetX + 42, 90, Size.X - 230), 438);
                body = Body(position, Round(16), 18);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * MathF.PI / 4;
                    PhysicsServer.BodyAddShape(body, Rectangle(50, 16).GetRID(), new Transform(angle, new Vector2(45, 0).Rotated(angle)));
                }
                PhysicsServer.BodySetMass(body, 18); PhysicsServer.BodySetAngularDamp(body, 2);
                var joint = PhysicsServer.JointCreate(); _joints.Add(joint); PhysicsServer.JointMakePin(joint, position, body);
                AddPlatform(position + new Vector2(165, 42));
                break;
            case WaterToy.Gate:
                position = new(Size.X * .72f, 260); _gateEntering = true;
                body = Body(position, Rectangle(12, 360), 1, PhysicsServer.BodyMode.Kinematic);
                break;
            case WaterToy.Ball: body = Body(position, Round(28), 6); break;
            case WaterToy.Wood: body = Body(position, Rectangle(36, 36), 4); break;
            case WaterToy.Steel: body = Body(position, Round(14), 16); break;
            default: return false;
        }
        _actors[slot] = body;
        return true;
    }
    /// <summary>Removes toys of this kind, including the wheel's lift and any held grab.</summary>
    /// <param name="kind">The optional toy kind to remove.</param>
    internal void RemoveToy(WaterToy kind)
    {
        if (kind is not (WaterToy.Bucket or WaterToy.Wheel or WaterToy.Gate or WaterToy.Ball or WaterToy.Wood or WaterToy.Steel)) return;
        if (kind == WaterToy.Wheel)
        {
            foreach (var joint in _joints) PhysicsServer.FreeRID(joint);
            _joints.Clear(); RemoveActor(PlatformSlot);
            _wheelAngle = _lastWheelRotation = 0;
        }
        if (kind == WaterToy.Gate) { _gateEntering = false; _gateVelocity = Vector2.Zero; }
        for (var slot = BucketSlot; slot < BodyCount; slot++)
            if (_kinds[slot] == kind) RemoveActor(slot);
    }
    private void RemoveActor(int slot)
    {
        var body = _actors[slot]; if (!body.IsValid()) return;
        if (_dragged == body) EndDrag();
        PhysicsServer.FreeRID(body); _bodies.Remove(body); _actors[slot] = default;
    }
    private void AddPlatform(Vector2 position)
    {
        _kinds[PlatformSlot] = WaterToy.Platform;
        var body = _actors[PlatformSlot] = Body(position, Rectangle(100, 12), 4, PhysicsServer.BodyMode.RigidLinear);
        var joint = PhysicsServer.JointCreate(); _joints.Add(joint);
        PhysicsServer.JointMakeGroove(joint, new(position.X, 305), new(position.X, 660), position, _walls[0], body);
    }
    private void Mechanisms(float interval)
    {
        if (_actors[GateSlot].IsValid() && _dragged != _actors[GateSlot])
        { if (_gateEntering) MoveGate(620, interval); else { _gateVelocity = Vector2.Zero; PhysicsServer.BodySetLinearVelocity(_actors[GateSlot], Vector2.Zero); } }
        if (!_actors[WheelSlot].IsValid()) return;
        var rotor = PhysicsServer.BodyGetDirectState(_actors[WheelSlot])!;
        var rotation = rotor.Transform.Rotation;
        _wheelAngle += Mathf.Wrap(rotation - _lastWheelRotation, -MathF.PI, MathF.PI); _lastWheelRotation = rotation;
        var platform = PhysicsServer.BodyGetDirectState(_actors[PlatformSlot])!;
        var target = Math.Clamp(480 + _wheelAngle * 12, 325, 630);
        var force = Math.Clamp(((target - platform.Transform.Origin.Y) * 400 - platform.LinearVelocity.Y * 40) /
            ((1 + 40 * interval + 400 * interval * interval) * platform.InverseMass), -20000, 20000);
        PhysicsServer.BodyApplyCentralImpulse(_actors[PlatformSlot], new(0, force * interval));
        PhysicsServer.BodyApplyTorqueImpulse(_actors[WheelSlot], -force * 12 * interval);
    }
    private void MoveGate(float target, float interval)
    {
        var body = _actors[GateSlot]; var pose = ActorPose(GateSlot);
        var error = target - pose.Origin.Y;
        _gateVelocity = new(0, Math.Clamp(error * 14, -220, 220));
        if (Math.Abs(error) < .001f) _gateEntering = false;
        PhysicsServer.BodySetLinearVelocity(body, _gateVelocity);
        PhysicsServer.BodySetTransform(body, new Transform(0, pose.Origin + _gateVelocity * interval));
    }
    internal void TiltHeld(float amount)
    {
        if (!_dragged.IsValid() || _dragged == _actors[GateSlot]) return;
        var state = PhysicsServer.BodyGetDirectState(_dragged)!;
        if (state.InverseInertia > 0) PhysicsServer.BodyApplyTorqueImpulse(_dragged, amount * .7f / state.InverseInertia);
    }
    private void Drain()
    {
        if (!DrainOpen) return;
        var before = ActiveCount;
        for (var i = 0; i < ActiveCount;)
        {
            var p = XY(_state[i]) * 100;
            if (p.Y > Size.Y - 22 && Math.Abs(p.X - DrainPosition.X) < 28) _state[i] = _state[--ActiveCount];
            else i++;
        }
        if (UseGPU && ActiveCount != before && ActiveCount > 0)
            _device!.BufferUpdate(_buffers[_gpuSource], 0, (uint)(ActiveCount * 16), System.Runtime.InteropServices.MemoryMarshal.AsBytes(_state.AsSpan(0, ActiveCount)));
    }
    internal Rect2 LocalBounds(int slot) => Bounds(_kinds[slot]);
    private static Rect2 Bounds(WaterToy kind) => kind switch
    {
        WaterToy.Duck => new(-74, -64, 152, 110),
        WaterToy.Boat => new(-86, -118, 172, 148),
        WaterToy.Bucket => new(-62, -82, 124, 136),
        WaterToy.Wheel => new(-74, -74, 148, 148),
        WaterToy.Gate => new(-20, -206, 40, 386),
        WaterToy.Ball => new(-28, -28, 56, 56),
        WaterToy.Wood => new(-18, -18, 36, 36),
        WaterToy.Steel => new(-14, -14, 28, 28),
        _ => new(-50, -6, 100, 12)
    };
    internal int BucketWaterCount()
    {
        if (!_actors[BucketSlot].IsValid()) return 0;
        var inverse = ActorPose(BucketSlot).AffineInverse(); var count = 0;
        for (var i = 0; i < ActiveCount; i++) if (new Rect2(-46, -44, 92, 81).HasPoint(inverse * Positions[i])) count++;
        return count;
    }
    private static Float4 Box(Vector2 p, Vector2 half)
    {
        var q = p.Abs() - half; var outside = q.Max(Vector2.Zero); var length = outside.Length();
        var normal = length > 1e-7f ? outside / length * p.Sign() : q.X > q.Y ? new Vector2(MathF.CopySign(1, p.X), 0) : new Vector2(0, MathF.CopySign(1, p.Y));
        return new(normal.X, normal.Y, length + Math.Min(Math.Max(q.X, q.Y), 0), 0);
    }
    private static Float4 Near(Float4 a, Float4 b) => a.Z < b.Z ? a : b;
}
