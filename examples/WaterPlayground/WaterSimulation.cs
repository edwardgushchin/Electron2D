using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Float4 = System.Numerics.Vector4;

namespace Electron2D.Examples.WaterPlayground;

/// <summary>A two-way coupled particle liquid with identical CPU and GPU numerical passes.</summary>
internal sealed partial class WaterSimulation : IDisposable
{
    internal const int DefaultCount = 65536;
    internal static readonly Vector2 WorldSize = new(1152, 800);
    internal const float PourDuration = 8;
    internal int ActiveCount { get; private set; }
    internal RID Dragged => _dragged;
    internal const int FishCount = 6;
    private const int BodyCount = FishCount + 2;
    private readonly RID[] _actors = new RID[BodyCount];
    private readonly float[] _fishDirections = [1, -1, 1, -1, 1, -1];
    private readonly int[] _wetCells = new int[58 * 40];
    internal RID FishBody(int index) => _actors[index + 2];
    internal Transform FishPose(int index) => Pose(_actors[index + 2]);
    internal float FishDirection(int index) => _fishDirections[index];
    internal int SwimmingFishCount { get; private set; }
    [InlineArray(BodyCount)]
    private struct BodyVectors { private Float4 _first; }
    private const float RestDensity = 100;
    private readonly RID _space;
    private readonly List<RID> _bodies = [];
    private readonly List<Shape> _shapes = [];
    private readonly RID[] _walls = new RID[3];
    private readonly RectangleShape[] _wallShapes = new RectangleShape[3];
    private Float4[] _state, _scratch;
    private readonly Float4[] _original;
    private readonly Float4[] _reactions;
    private readonly float[] _density, _lambda;
    private readonly int[] _next;
    private int _columns, _rows;
    private float _gridTop;
    private int[] _heads = [];
    private readonly float _mass, _h;
    private RID _dragged;
    private Vector2 _dragAnchor, _pointer;
    private bool _held, _disposed;
    private Settings _settings;
    private readonly Action<int> _densityWorker, _integrateWorker, _finishWorker;
    private const int Chunk = 1024;
    [StructLayout(LayoutKind.Sequential)]
    private struct Settings
    {
        internal Float4 Meta, World, Material, Pointer, Integration;
        internal BodyVectors Poses, Motions, Details;
    }
    internal Vector2[] Positions { get; }
    internal Vector2 Size => WorldSize;
    internal float EntryY { get; set; }
    internal double Time { get; private set; }
    internal RID Duck => _actors[0];
    internal RID Boat => _actors[1];
    internal Transform DuckPose => Pose(Duck);
    internal Transform BoatPose => Pose(Boat);
    private static Transform Pose(RID body) => body.IsValid() ? PhysicsServer.BodyGetTransform(body) : Transform.Identity;
    internal int Count => _state.Length;
    internal float Spacing { get; }
    internal float Volume => Count * _mass / 1000;
    internal double StepMS { get; private set; }
    internal bool UseGPU { get; private set; }
    internal static readonly Vector2[] Hull = [new(-84, -12), new(84, -12), new(57, 26), new(-52, 26)];

    internal WaterSimulation(int count = DefaultCount)
    {
        var size = WorldSize;
        if (count is < 256 or > DefaultCount) throw new ArgumentOutOfRangeException(nameof(count));
        Spacing = MathF.Sqrt(size.X * size.Y / (3 * count)); _h = Spacing * .023333333f; _mass = RestDensity * Spacing * Spacing * .0001f;
        _state = new Float4[count]; _scratch = new Float4[count]; _original = new Float4[count]; _reactions = new Float4[count * BodyCount];
        Positions = new Vector2[count]; _density = new float[count]; _lambda = new float[count]; _next = new int[count];
        _densityWorker = chunk => { for (var i = chunk * Chunk; i < Math.Min(ActiveCount, (chunk + 1) * Chunk); i++) Density(i); };
        _integrateWorker = chunk => { for (var i = chunk * Chunk; i < Math.Min(ActiveCount, (chunk + 1) * Chunk); i++) Integrate(i); };
        _finishWorker = chunk => { for (var i = chunk * Chunk; i < Math.Min(ActiveCount, (chunk + 1) * Chunk); i++) Finish(i); };
        _space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(_space, true);
        PhysicsServer.AreaSetGravity(_space, 980); PhysicsServer.AreaSetLinearDamp(_space, 0); PhysicsServer.AreaSetAngularDamp(_space, 0);
        for (var i = 0; i < 3; i++) { _wallShapes[i] = Own(new RectangleShape()); _walls[i] = Body(Vector2.Zero, _wallShapes[i], 1, PhysicsServer.BodyMode.Static); }
        ConfigureWalls();
        Capture();
    }
    private void Emit()
    {
        var before = ActiveCount;
        ActiveCount = Math.Min(Count, (int)(Time / PourDuration * Count));
        const int columns = 24;
        var rate = Count / PourDuration;
        var speed = rate / columns * Spacing;
        for (var i = before; i < ActiveCount; i++)
        {
            var born = i / rate;
            var hash = unchecked((uint)i * 747796405u + 2891336453u);
            var jitter = ((hash >> 16) / 65535f - .5f) * Spacing * .7f;
            var center = Size.X * .47f + 45 * MathF.Sin(born * 1.4f);
            var x = center + (i % columns - (columns - 1) * .5f) * Spacing + jitter;
            var jitterY = ((unchecked(hash * 277803737u) >> 16) / 65535f - .5f) * Spacing * .7f;
            var age = (float)(Time - born);
            var y = 8 + age * speed + 490 * age * age + jitterY;
            _state[i] = new(x * .01f, y * .01f, .2f * MathF.Cos(born * 1.4f), (speed + 980 * age) * .01f);
        }
        if (UseGPU && ActiveCount > before)
            _device!.BufferUpdate(_buffers[_gpuSource], (uint)(before * 16), (uint)((ActiveCount - before) * 16),
                MemoryMarshal.AsBytes(_state.AsSpan(before, ActiveCount - before)));
    }
    private T Own<T>(T shape) where T : Shape { _shapes.Add(shape); return shape; }
    private RID Body(Vector2 position, Shape shape, float mass, PhysicsServer.BodyMode mode = PhysicsServer.BodyMode.Rigid)
    {
        var body = PhysicsServer.BodyCreate(); _bodies.Add(body); PhysicsServer.BodySetMode(body, mode);
        PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetMass(body, mass);
        PhysicsServer.BodySetFriction(body, .1f); PhysicsServer.BodySetBounce(body, 0); PhysicsServer.BodySetCanSleep(body, false);
        PhysicsServer.BodySetTransform(body, new Transform(0, position)); PhysicsServer.BodySetSpace(body, _space); return body;
    }
    private void ConfigureWalls()
    {
        var size = Size;
        _wallShapes[0].Size = new(size.X + 80, 40); _wallShapes[1].Size = _wallShapes[2].Size = new(40, size.Y * 6);
        PhysicsServer.BodySetTransform(_walls[0], new Transform(0, new(size.X / 2, size.Y + 20)));
        PhysicsServer.BodySetTransform(_walls[1], new Transform(0, new(-20, 0))); PhysicsServer.BodySetTransform(_walls[2], new Transform(0, new(size.X + 20, 0)));
        _gridTop = 0;
        _columns = (int)MathF.Ceiling(size.X * .01f / _h);
        _rows = (int)MathF.Ceiling((size.Y * .01f - _gridTop) / _h) + 2;
        _heads = new int[checked(_columns * _rows)];
    }
    internal void BeginDrag(Vector2 position)
    {
        _held = true; _pointer = ConstrainPointer(position); _dragged = default;
        for (var slot = 0; slot < BodyCount; slot++)
        {
            var body = _actors[slot];
            if (body.IsValid())
            {
                var pose = PhysicsServer.BodyGetTransform(body); var local = pose.AffineInverse() * position;
                if (LocalBounds(slot).Grow(6).HasPoint(local)) { _dragged = body; _dragAnchor = local; break; }
            }
        }
    }
    private Vector2 ConstrainPointer(Vector2 position) => new(Math.Clamp(position.X, 0, Size.X), Math.Min(position.Y, Size.Y));
    internal void MovePointer(Vector2 position) => _pointer = ConstrainPointer(position);
    internal void EndDrag() { _held = false; _dragged = default; }
    internal void Step(double delta)
    {
        if (!double.IsFinite(delta) || delta <= 0 || delta > 1d / 30) throw new ArgumentOutOfRangeException(nameof(delta));
        var start = Stopwatch.GetTimestamp(); Time += delta; Emit();
        if (Time >= PourDuration + 1 && !Duck.IsValid())
        {
            _actors[0] = Body(new(Size.X * .34f, SpawnY(0)), Own(new CapsuleShape { Radius = 32, Height = 108 }), 6);
            PhysicsServer.BodySetShapeTransform(Duck, 0, new Transform(MathF.PI / 2, new(-5, 0)));
            PhysicsServer.BodyAddShape(Duck, Own(new CircleShape { Radius = 27 }).GetRID(), new Transform(0, new(31, -32)));
            PhysicsServer.BodySetCenterOfMass(Duck, new(-5, 10)); PhysicsServer.BodySetAngularDamp(Duck, .5f);
        }
        if (Time >= PourDuration + 3 && !Boat.IsValid())
        {
            _actors[1] = Body(new(Size.X * .69f, SpawnY(1)), Own(new ConvexPolygonShape { Points = Hull }), 10);
            PhysicsServer.BodySetCenterOfMass(Boat, new(0, 18)); PhysicsServer.BodySetAngularDamp(Boat, .5f);
        }
        for (var fish = 0; fish < FishCount; fish++)
            if (Time >= PourDuration + 5 + fish * .3 && !FishBody(fish).IsValid())
            {
                var body = Body(new(Size.X * (.16f + .13f * fish), SpawnY(fish + 2)), Own(new CapsuleShape { Radius = 10, Height = 34 }), 6.5f);
                PhysicsServer.BodySetShapeTransform(body, 0, new Transform(MathF.PI / 2, Vector2.Zero));
                PhysicsServer.BodySetAngularDamp(body, 2);
                _actors[fish + 2] = body;
            }
        const int couplingSteps = 4;
        var interval = (float)delta / couplingSteps;
        _settings = new Settings
        {
            Meta = new(ActiveCount, 0, _columns, _rows),
            World = new(Size.X * .01f, Size.Y * .01f, interval, _h),
            Material = new(_mass, RestDensity, Count, 0),
            Pointer = new(_pointer.X * .01f, _pointer.Y * .01f, _held && !_dragged.IsValid() ? 1 : 0, .65f)
        };
        for (var coupled = 0; coupled < couplingSteps; coupled++)
        {
            PullToy(interval);
            Swim(interval);
            for (var slot = 0; slot < BodyCount; slot++)
            {
                FillToy(_actors[slot], out _settings.Poses[slot], out _settings.Motions[slot], out var inertia);
                _settings.Details[slot] = new(inertia, 0, 0, 0);
            }
            _settings.Integration = new(_gridTop, 0, 0, 0);
            if (UseGPU) StepGPU(coupled == couplingSteps - 1);
            else
            {
                Array.Clear(_reactions);
                for (var i = 0; i < ActiveCount; i++)
                {
                    _original[i] = _state[i];
                    var acceleration = new Vector2(0, 9.8f); var offset = XY(_settings.Pointer) - XY(_state[i]);
                    if (_settings.Pointer.Z > .5f && offset.LengthSquared() < .65f * .65f) acceleration += offset * 18;
                    var velocity = Velocity(_state[i]) + acceleration * interval; var position = XY(_state[i]) + velocity * interval;
                    _state[i] = new(position.X, position.Y, velocity.X, velocity.Y);
                }
                for (var iteration = 0; iteration < 4; iteration++)
                {
                    BuildGrid();
                    Parallel.For(0, (ActiveCount + Chunk - 1) / Chunk, _densityWorker);
                    Parallel.For(0, (ActiveCount + Chunk - 1) / Chunk, _integrateWorker);
                    (_state, _scratch) = (_scratch, _state);
                }
                BuildGrid(); Parallel.For(0, (ActiveCount + Chunk - 1) / Chunk, _finishWorker); (_state, _scratch) = (_scratch, _state);
                for (var slot = 0; slot < BodyCount; slot++)
                {
                    var sum = Float4.Zero;
                    for (var i = 0; i < ActiveCount; i++) sum += _reactions[slot * Count + i];
                    React(_actors[slot], sum);
                }
            }
            PhysicsServer.SpaceStep(_space, interval);
            ContainBodies();
        }
        Capture();
        StepMS = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
    internal static Rect2 LocalBounds(int slot) => slot == 0 ? new(-74, -64, 152, 110) : slot == 1 ? new(-86, -118, 172, 148) : new(-32, -16, 64, 32);
    private float SpawnY(int slot) => EntryY - LocalBounds(slot).End.Y - 24;
    internal Rect2 ActorBounds(int slot) => Pose(_actors[slot]) * LocalBounds(slot);
    internal bool ActorExists(int slot) => _actors[slot].IsValid();
    private void ContainBodies()
    {
        for (var slot = 0; slot < BodyCount; slot++)
        {
            var body = _actors[slot]; if (!body.IsValid()) continue;
            var state = PhysicsServer.BodyGetDirectState(body)!; var bounds = state.Transform * LocalBounds(slot); var shift = Vector2.Zero;
            if (bounds.Position.X < 3) shift.X = 3 - bounds.Position.X;
            else if (bounds.End.X > Size.X - 3) shift.X = Size.X - 3 - bounds.End.X;
            if (bounds.End.Y > Size.Y - 3) shift.Y = Size.Y - 3 - bounds.End.Y;
            if (shift == Vector2.Zero) continue;
            var pose = state.Transform; pose.Origin += shift;
            var velocity = state.LinearVelocity;
            if (shift.X * velocity.X < 0) velocity.X = 0;
            if (shift.Y * velocity.Y < 0) velocity.Y = 0;
            PhysicsServer.BodySetTransform(body, pose); PhysicsServer.BodySetLinearVelocity(body, velocity);
        }
    }
    internal bool FishInWater(int index)
    {
        var body = FishBody(index); if (!body.IsValid()) return false;
        var p = FishPose(index).Origin; var x = (int)(p.X / 20); var y = (int)(p.Y / 20); var particles = 0;
        for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++)
                if ((uint)(x + dx) < 58 && (uint)(y + dy) < 40) particles += _wetCells[(y + dy) * 58 + x + dx];
        return particles * Spacing * Spacing > 1100;
    }
    private void Swim(float interval)
    {
        SwimmingFishCount = 0;
        for (var i = 0; i < FishCount; i++)
        {
            var body = FishBody(i); if (!FishInWater(i)) continue;
            SwimmingFishCount++; if (body == _dragged) continue;
            var state = PhysicsServer.BodyGetDirectState(body)!; var p = state.Transform.Origin;
            if (p.X < 50) _fishDirections[i] = 1; else if (p.X > Size.X - 50) _fishDirections[i] = -1;
            var depth = Size.Y * .83f + 35 * MathF.Sin((float)Time * .45f + i);
            var acceleration = new Vector2((_fishDirections[i] * (38 + i * 3) - state.LinearVelocity.X) * 5,
                (depth - p.Y) * 22 - state.LinearVelocity.Y * 9);
            PhysicsServer.BodyApplyCentralImpulse(body, acceleration.LimitLength(2500) * (interval / state.InverseMass));
            if (state.InverseInertia > 0)
                PhysicsServer.BodyApplyTorqueImpulse(body, (-Mathf.Wrap(state.Transform.Rotation, -MathF.PI, MathF.PI) * 24 - state.AngularVelocity * 9) * interval / state.InverseInertia);
        }
    }
    private void PullToy(float interval)
    {
        if (!_held || !_dragged.IsValid()) return;
        var state = PhysicsServer.BodyGetDirectState(_dragged)!;
        var arm = state.Transform.BasisXform(_dragAnchor);
        var error = _pointer - (state.Transform.Origin + arm);
        const float omega = 32;
        var acceleration = (error * (omega * omega) - state.GetVelocityAtLocalPosition(arm) * (2 * omega) + new Vector2(0, -980))
            / (1 + 2 * omega * interval + omega * omega * interval * interval);
        PhysicsServer.BodyApplyImpulse(_dragged, acceleration.LimitLength(18000) * (interval / state.InverseMass), arm);
    }
    private static void FillToy(RID body, out Float4 pose, out Float4 motion, out float inverseInertia)
    {
        pose = motion = default; inverseInertia = 0; if (!body.IsValid()) return;
        var state = PhysicsServer.BodyGetDirectState(body)!; var transform = state.Transform; var velocity = state.LinearVelocity;
        pose = new(transform.Origin.X * .01f, transform.Origin.Y * .01f, transform.Rotation, 1);
        motion = new(velocity.X * .01f, velocity.Y * .01f, state.AngularVelocity, state.InverseMass); inverseInertia = state.InverseInertia * 10000;
    }
    private static void React(RID body, Float4 impulse)
    { if (body.IsValid()) { PhysicsServer.BodyApplyCentralImpulse(body, new(impulse.X * 100, impulse.Y * 100)); PhysicsServer.BodyApplyTorqueImpulse(body, impulse.Z * 10000); } }
    private void Capture()
    {
        Array.Clear(_wetCells);
        for (var i = 0; i < ActiveCount; i++)
        {
            var p = Positions[i] = XY(_state[i]) * 100;
            var x = Math.Clamp((int)(p.X / 20), 0, 57); var y = Math.Clamp((int)(p.Y / 20), 0, 39); _wetCells[y * 58 + x]++;
        }
    }
    private static Vector2 XY(Float4 v) => new(v.X, v.Y);
    private static Vector2 Velocity(Float4 v) => new(v.Z, v.W);
    private (int X, int Y) Cell(Vector2 p) => (Math.Clamp((int)MathF.Floor(p.X / _h), 0, _columns - 1), Math.Clamp((int)MathF.Floor((p.Y - _gridTop) / _h), 0, _rows - 1));
    private void BuildGrid()
    {
        Array.Fill(_heads, -1);
        for (var i = 0; i < ActiveCount; i++) { var (x, y) = Cell(XY(_state[i])); var cell = y * _columns + x; _next[i] = _heads[cell]; _heads[cell] = i; }
    }
    private bool NeighborOffset(Vector2 position, int j, int mirror, out Vector2 offset)
    {
        var neighbor = XY(_state[j]); offset = default;
        if (mirror == 1) { if (position.X >= _h) return false; neighbor.X = -neighbor.X; }
        if (mirror == 2) { if (position.X <= Size.X * .01f - _h) return false; neighbor.X = Size.X * .02f - neighbor.X; }
        if (mirror == 4) { if (position.Y >= _h) return false; neighbor.Y = -neighbor.Y; }
        if (mirror == 3) { if (position.Y <= Size.Y * .01f - _h) return false; neighbor.Y = Size.Y * .02f - neighbor.Y; }
        offset = position - neighbor; return true;
    }
    private void Density(int i)
    {
        var (x, y) = Cell(XY(_state[i])); var h2 = _h * _h; var kernel = 4 / (MathF.PI * h2 * h2 * h2 * h2);
        var gradient = 30 / (MathF.PI * h2 * h2 * _h); var sum = 0f; var denominator = 0f; var ownGradient = Vector2.Zero;
        for (var cy = Math.Max(0, y - 1); cy <= Math.Min(_rows - 1, y + 1); cy++)
            for (var cx = Math.Max(0, x - 1); cx <= Math.Min(_columns - 1, x + 1); cx++)
                for (var j = _heads[cy * _columns + cx]; j != -1; j = _next[j])
                    for (var mirror = 0; mirror < (XY(_state[i]).X < _h || XY(_state[i]).X > Size.X * .01f - _h || XY(_state[i]).Y > Size.Y * .01f - _h || XY(_state[i]).Y < _h ? 5 : 1); mirror++)
                    {
                        var offset = XY(_state[i]) - XY(_state[j]); if (mirror != 0 && !NeighborOffset(XY(_state[i]), j, mirror, out offset)) continue; var r2 = offset.LengthSquared(); if (r2 >= h2) continue;
                        var q = h2 - r2; sum += _mass * kernel * q * q * q;
                        if (r2 < 1e-10f) continue;
                        var distance = MathF.Sqrt(r2); var grad = -offset * (_mass / RestDensity * gradient * (_h - distance) * (_h - distance) / distance);
                        ownGradient += grad; denominator += grad.LengthSquared();
                    }
        _density[i] = sum; _lambda[i] = -Math.Max(0, sum / RestDensity - 1) / (denominator + ownGradient.LengthSquared() + .01f / h2);
    }
    private void Integrate(int i)
    {
        var state = _state[i]; var position = XY(state); var (x, y) = Cell(position); var h2 = _h * _h;
        var kernel = 4 / (MathF.PI * h2 * h2 * h2 * h2); var gradient = 30 / (MathF.PI * h2 * h2 * _h);
        var reference = kernel * MathF.Pow(h2 * .91f, 3); var correction = Vector2.Zero;
        for (var cy = Math.Max(0, y - 1); cy <= Math.Min(_rows - 1, y + 1); cy++)
            for (var cx = Math.Max(0, x - 1); cx <= Math.Min(_columns - 1, x + 1); cx++)
                for (var j = _heads[cy * _columns + cx]; j != -1; j = _next[j])
                    for (var mirror = 0; mirror < (XY(_state[i]).X < _h || XY(_state[i]).X > Size.X * .01f - _h || XY(_state[i]).Y > Size.Y * .01f - _h || XY(_state[i]).Y < _h ? 5 : 1); mirror++)
                    {
                        var offset = position - XY(_state[j]); if (mirror != 0 && !NeighborOffset(position, j, mirror, out offset)) continue; var r2 = offset.LengthSquared(); if (r2 >= h2 || r2 < 1e-10f) continue;
                        var distance = MathF.Sqrt(r2); var q = h2 - r2; var ratio = kernel * q * q * q / reference;
                        var artificial = 0f;
                        var grad = -offset * (_mass / RestDensity * gradient * (_h - distance) * (_h - distance) / distance);
                        correction += (_lambda[i] + _lambda[j] + artificial) * grad;
                    }
        position += (correction * .25f).LimitLength(_h * .2f); var radius = Spacing * .0045f;
        position.X = Math.Clamp(position.X, radius, Size.X * .01f - radius); position.Y = Math.Clamp(position.Y, radius, Size.Y * .01f - radius);
        for (var slot = 0; slot < BodyCount; slot++)
            Project(ref position, _settings.Poses[slot], _settings.Motions[slot], _settings.Details[slot].X, slot, ref _reactions[slot * Count + i]);
        position = position.Clamp(new(radius, radius), Size * .01f - new Vector2(radius, radius));
        _scratch[i] = new(position.X, position.Y, state.Z, state.W);
    }
    private void Finish(int i)
    {
        var position = XY(_state[i]); var velocity = (position - XY(_original[i])) / _settings.World.Z;
        var (x, y) = Cell(position); var h2 = _h * _h; var kernel = 4 / (MathF.PI * h2 * h2 * h2 * h2); var viscosity = Vector2.Zero;
        for (var cy = Math.Max(0, y - 1); cy <= Math.Min(_rows - 1, y + 1); cy++)
            for (var cx = Math.Max(0, x - 1); cx <= Math.Min(_columns - 1, x + 1); cx++)
                for (var j = _heads[cy * _columns + cx]; j != -1; j = _next[j])
                {
                    var r2 = (position - XY(_state[j])).LengthSquared(); if (i == j || r2 >= h2 || r2 < 1e-10f) continue;
                    var q = h2 - r2; var weight = kernel * q * q * q; var otherVelocity = (XY(_state[j]) - XY(_original[j])) / _settings.World.Z;
                    viscosity += (otherVelocity - velocity) * (_mass / Math.Max(_density[j], 1e-6f) * weight);
                }
        velocity += viscosity * .1f;
        for (var slot = 0; slot < BodyCount; slot++)
            Collide(ref position, ref velocity, _settings.Poses[slot], _settings.Motions[slot], _settings.Details[slot].X, slot, ref _reactions[slot * Count + i]);
        var radius = Spacing * .0045f;
        position = position.Clamp(new(radius, radius), Size * .01f - new Vector2(radius, radius));
        if (position.X <= radius && velocity.X < 0 || position.X >= Size.X * .01f - radius && velocity.X > 0) velocity.X = 0;
        if (position.Y <= radius && velocity.Y < 0 || position.Y >= Size.Y * .01f - radius && velocity.Y > 0) velocity.Y = 0;
        _scratch[i] = new(position.X, position.Y, velocity.X, velocity.Y);
    }
    private void Project(ref Vector2 p, Float4 pose, Float4 motion, float inverseInertia, int slot, ref Float4 impulse)
    {
        if (pose.W < .5f) return;
        var origin = XY(pose) + XY(motion) * _settings.World.Z; var angle = pose.Z + motion.Z * _settings.World.Z;
        if ((p - origin).LengthSquared() > (slot < 2 ? 2 : .12f)) return;
        var distance = Distance((p - origin).Rotated(-angle), slot); var radius = Spacing * .0045f;
        if (distance.Z >= radius) return;
        var normal = XY(distance).Rotated(angle); var center = origin + Center(slot).Rotated(angle);
        var arm = p - center; var tangent = arm.Cross(normal);
        var correction = normal * ((radius - distance.Z) / (1 + _mass * (motion.W + tangent * tangent * inverseInertia)));
        p += correction; var j = correction * (_mass / _settings.World.Z); impulse.X -= j.X; impulse.Y -= j.Y; impulse.Z -= arm.Cross(j);
    }
    private void Collide(ref Vector2 p, ref Vector2 v, Float4 pose, Float4 motion, float inverseInertia, int slot, ref Float4 impulse)
    {
        if (pose.W < .5f) return;
        var origin = XY(pose) + XY(motion) * _settings.World.Z; var angle = pose.Z + motion.Z * _settings.World.Z;
        if ((p - origin).LengthSquared() > (slot < 2 ? 2 : .12f)) return;
        var distance = Distance((p - origin).Rotated(-angle), slot); var radius = Spacing * .0045f;
        if (distance.Z >= radius) return;
        var normal = XY(distance).Rotated(angle); p += normal * (radius - distance.Z);
        var center = origin + Center(slot).Rotated(angle); var arm = p - center;
        var velocity = XY(motion) + motion.Z * new Vector2(-arm.Y, arm.X); var tangent = arm.Cross(normal);
        var j = Math.Max(0, -(v - velocity).Dot(normal)) * _mass / (1 + _mass * (motion.W + tangent * tangent * inverseInertia));
        var action = normal * j; v += action / _mass;
        impulse.X -= action.X; impulse.Y -= action.Y; impulse.Z -= arm.Cross(action);
    }
    private static Float4 Circle(Vector2 p, float radius) { var length = p.Length(); var normal = length > 1e-7f ? p / length : new Vector2(0, -1); return new(normal.X, normal.Y, length - radius, 0); }
    private static Vector2 Center(int slot) => slot == 0 ? new(-.05f, .1f) : slot == 1 ? new(0, .18f) : Vector2.Zero;
    private static Float4 Distance(Vector2 p, int slot)
    {
        if (slot > 1) { p.X -= Math.Clamp(p.X, -.07f, .07f); return Circle(p, .1f); }
        if (slot == 0)
        {
            var q = p - new Vector2(-.05f, 0); q.X -= Math.Clamp(q.X, -.22f, .22f);
            var body = Circle(q, .32f); var head = Circle(p - new Vector2(.31f, -.32f), .27f); return body.Z < head.Z ? body : head;
        }
        var best = float.MaxValue; var normal = new Vector2(0, -1); var inside = true;
        for (var i = 0; i < Hull.Length; i++)
        {
            var a = Hull[i] * .01f; var edge = (Hull[(i + 1) % Hull.Length] - Hull[i]) * .01f;
            inside &= edge.Cross(p - a) >= 0;
            var offset = p - (a + edge * Math.Clamp((p - a).Dot(edge) / edge.LengthSquared(), 0, 1)); var distance = offset.Length();
            if (distance < best) { best = distance; normal = distance > 1e-7f ? offset / distance : new Vector2(edge.Y, -edge.X).Normalized(); }
        }
        if (inside) { normal = -normal; best = -best; }
        return new(normal.X, normal.Y, best, 0);
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; FreeGPU();
        foreach (var body in _bodies) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(_space);
        foreach (var shape in _shapes) shape.Dispose();
    }
}
