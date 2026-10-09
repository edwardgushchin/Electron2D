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
    internal const int BodyCount = 15;
    private readonly RID[] _actors = new RID[BodyCount];
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
    private readonly Float4[] _boundary;
    private readonly float[] _density, _lambda;
    private readonly int[] _next;
    private int _columns, _rows;
    private float _gridTop, _minimumY;
    private const int PressureIterations = 2;
    private int[] _heads = [];
    private readonly float _mass, _h;
    private RID _dragged;
    private Vector2 _dragAnchor, _pointer;
    private bool _held, _disposed;
    private Settings _settings;
    private readonly Action<int> _densityWorker, _integrateWorker, _finishWorker;
    private const int Chunk = 1024;
    private readonly CPUWork[] _cpuWork = new CPUWork[Math.Max(0, Math.Min(Environment.ProcessorCount, 8) - 1)];
    private readonly CountdownEvent _cpuDone = new(0);
    private Action<int> _cpuPass = null!;
    private int _cpuNext, _cpuChunks;
    private Exception? _cpuError;
    private sealed class CPUWork(WaterSimulation simulation) : IThreadPoolWorkItem
    { public void Execute() => simulation.ExecuteParticles(); }
    private void ExecuteParticles()
    {
        try
        {
            int chunk;
            while ((chunk = Interlocked.Increment(ref _cpuNext)) < _cpuChunks) _cpuPass(chunk);
        }
        catch (Exception error) { Interlocked.CompareExchange(ref _cpuError, error, null); }
        finally { _cpuDone.Signal(); }
    }
    private void ParticlePass(Action<int> pass)
    {
        _cpuChunks = (ActiveCount + Chunk - 1) / Chunk;
        if (_cpuChunks <= 1) { if (_cpuChunks == 1) pass(0); return; }
        _cpuPass = pass; _cpuNext = -1; _cpuError = null;
        var workers = Math.Min(_cpuWork.Length, _cpuChunks - 1); _cpuDone.Reset(workers + 1);
        var queued = 0;
        try
        {
            for (; queued < workers; queued++)
                if (!ThreadPool.UnsafeQueueUserWorkItem(_cpuWork[queued], false)) throw new InvalidOperationException("Unable to queue a liquid pass.");
        }
        catch (Exception error)
        { Interlocked.CompareExchange(ref _cpuError, error, null); if (queued < workers) _cpuDone.Signal(workers - queued); }
        ExecuteParticles(); _cpuDone.Wait();
        if (_cpuError is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Settings
    {
        internal Float4 Meta, World, Material, Pointer, Integration, DrainData;
        internal BodyVectors Poses, Motions, Details, Centers, Rotations;
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
        for (var i = 0; i < _cpuWork.Length; i++) _cpuWork[i] = new CPUWork(this);
        var size = WorldSize;
        if (count is < 256 or > DefaultCount) throw new ArgumentOutOfRangeException(nameof(count));
        Spacing = MathF.Sqrt(size.X * size.Y / (3 * count)); _h = Spacing * .023333333f; _mass = RestDensity * Spacing * Spacing * .0001f;
        _state = new Float4[count]; _scratch = new Float4[count]; _original = new Float4[count]; _reactions = new Float4[count * BodyCount];
        _boundary = new Float4[count]; Positions = new Vector2[count]; _density = new float[count]; _lambda = new float[count]; _next = new int[count];
        _densityWorker = chunk => { for (var i = chunk * Chunk; i < Math.Min(ActiveCount, (chunk + 1) * Chunk); i++) Density(i); };
        _integrateWorker = chunk => { for (var i = chunk * Chunk; i < Math.Min(ActiveCount, (chunk + 1) * Chunk); i++) Integrate(i); };
        _finishWorker = chunk => { for (var i = chunk * Chunk; i < Math.Min(ActiveCount, (chunk + 1) * Chunk); i++) Finish(i); };
        _space = PhysicsServer.SpaceCreate(); PhysicsServer.SpaceSetActive(_space, true);
        PhysicsServer.AreaSetGravity(_space, 980); PhysicsServer.AreaSetLinearDamp(_space, 0); PhysicsServer.AreaSetAngularDamp(_space, 0);
        for (var i = 0; i < 3; i++) { _wallShapes[i] = Own(new RectangleShape()); _walls[i] = Body(Vector2.Zero, _wallShapes[i], 1, PhysicsServer.BodyMode.Static); }
        _kinds[0] = WaterToy.Duck; _kinds[1] = WaterToy.Boat;
        ConfigureWalls();
        Capture();
    }
    private void Emit(double delta)
    {
        var before = ActiveCount; var flow = Math.Clamp(FaucetFlow, 0, 1);
        var rate = Count / PourDuration * flow;
        _emitCredit += delta * rate;
        var added = Math.Min(Count - ActiveCount, (int)Math.Floor(_emitCredit + 1e-9));
        _emitCredit = ActiveCount + added == Count ? 0 : Math.Max(0, _emitCredit - added);
        ActiveCount += added;
        const int columns = 24;
        var speed = rate / columns * Spacing;
        var angle = Math.Clamp(FaucetAngle, -.55f, .55f);
        var direction = new Vector2(MathF.Sin(angle), MathF.Cos(angle));
        var tangent = new Vector2(direction.Y, -direction.X);
        for (var i = before; i < ActiveCount; i++)
        {
            var serial = _emitted++;
            var hash = unchecked((uint)serial * 747796405u + 2891336453u);
            var jitter = ((hash >> 16) / 65535f - .5f) * Spacing * .12f;
            var age = rate > 0 ? (ActiveCount - 1 - i) / rate : 0;
            var origin = new Vector2(Math.Clamp(FaucetX, 100, Size.X - 100), EntryY - 64 - Spacing);
            var p = origin + tangent * ((serial % columns - (columns - 1) * .5f) * Spacing + jitter) + direction * (age * speed);
            p += direction * (((unchecked(hash * 277803737u) >> 16) / 65535f - .5f) * Spacing * .12f);
            p.Y += 490 * age * age;
            var velocity = direction * speed + new Vector2(0, 980 * age);
            _minimumY = Math.Min(_minimumY, p.Y * .01f);
            _state[i] = new(p.X * .01f, p.Y * .01f, velocity.X * .01f, velocity.Y * .01f);
        }
        if (UseGPU && added > 0)
            _device!.BufferUpdate(_buffers[_gpuSource], (uint)(before * 16), (uint)(added * 16), MemoryMarshal.AsBytes(_state.AsSpan(before, added)));
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
        _columns = (int)MathF.Ceiling(size.X * .01f / _h);
        ResizeGrid(-size.Y * .01f);
    }
    private void ResizeGrid(float top)
    {
        // ponytail: dense storage grows with flight height; use sparse cells if large offscreen excursions become a target.
        _gridTop = top;
        _rows = (int)MathF.Ceiling((Size.Y * .01f - top) / _h) + 2;
        _heads = new int[checked(_columns * _rows + 1)];
        if (_device is not null) CreateBuffers();
    }
    internal void BeginDrag(Vector2 position)
    {
        _held = true; _pointer = ConstrainPointer(position); _dragged = default;
        for (var slot = BodyCount - 1; slot >= 0; slot--)
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
        var start = Stopwatch.GetTimestamp(); Time += delta; Emit(delta);
        if (_minimumY < _gridTop + 2 * _h)
        {
            ResizeGrid(Math.Min(_gridTop * 2, _minimumY - 4 * _h - 2));
            Capture();
        }
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
        _actorLimit = BodyCount;
        while (_actorLimit > 0 && !_actors[_actorLimit - 1].IsValid()) _actorLimit--;
        const int couplingSteps = 8;
        var interval = (float)delta / couplingSteps;
        _settings = new Settings
        {
            Meta = new(ActiveCount, 0, _columns, _rows),
            World = new(Size.X * .01f, Size.Y * .01f, interval, _h),
            Material = new(_mass, RestDensity, Count, _actorLimit),
            DrainData = new(DrainPosition.X * .01f, DrainPosition.Y * .01f, DrainOpen ? 1 : 0, 1.6f),
            Pointer = new(_pointer.X * .01f, _pointer.Y * .01f, _held && !_dragged.IsValid() ? 1 : 0, .65f)
        };
        for (var coupled = 0; coupled < couplingSteps; coupled++)
        {
            Mechanisms(interval);
            PullToy(interval);
            for (var slot = 0; slot < _actorLimit; slot++)
            {
                FillToy(_actors[slot], out _settings.Poses[slot], out _settings.Motions[slot], out var inertia, out _settings.Centers[slot]);
                if (slot == GateSlot) { _settings.Motions[slot].X = _gateVelocity.X * .01f; _settings.Motions[slot].Y = _gateVelocity.Y * .01f; }
                var angle = _settings.Poses[slot].Z + _settings.Motions[slot].Z * interval;
                var (sin, cos) = MathF.SinCos(angle);
                _settings.Rotations[slot] = new(cos, sin, 0, 0);
                var origin = XY(_settings.Poses[slot]) + XY(_settings.Motions[slot]) * interval;
                var center = origin + RotateBody(XY(_settings.Centers[slot]), slot);
                _settings.Poses[slot] = new(origin.X, origin.Y, angle, _settings.Poses[slot].W);
                _settings.Centers[slot] = new(center.X, center.Y, 0, 0);
                var bounds = LocalBounds(slot);
                var extent = bounds.Position.Abs().Max(bounds.End.Abs());
                var boundaryReach = extent.Length() * .01f + _h;
                _settings.Details[slot] = new(inertia, extent.LengthSquared() * .0001f + .02f, (int)_kinds[slot], boundaryReach * boundaryReach);
            }
            // A small numerical velocity filter controls particle-scale noise independently of pressure.
            _settings.Integration = new(_gridTop, 1 - MathF.Exp(-2.4f * interval), 0, 0);
            if (UseGPU) StepGPU(coupled == 0, coupled == couplingSteps - 1);
            else if (ActiveCount > 0)
            {
                if (coupled == 0)
                {
                    BuildGrid(); var sorted = 0;
                    for (var c = 0; c < _columns * _rows; c++)
                        for (var j = _heads[c]; j != -1; j = _next[j]) _scratch[sorted++] = _state[j];
                    (_state, _scratch) = (_scratch, _state);
                }
                for (var slot = 0; slot < _actorLimit; slot++) Array.Clear(_reactions, slot * Count, ActiveCount);
                for (var i = 0; i < ActiveCount; i++)
                {
                    _original[i] = _state[i];
                    var acceleration = new Vector2(0, 9.8f); var offset = XY(_settings.Pointer) - XY(_state[i]);
                    if (_settings.Pointer.Z > .5f && offset.LengthSquared() < .65f * .65f) acceleration += offset * 18;
                    var drain = XY(_settings.DrainData) - XY(_state[i]);
                    if (_settings.DrainData.Z > .5f && drain.LengthSquared() < 2.56f) acceleration += drain * 14;
                    var velocity = Velocity(_state[i]) + acceleration * interval; var position = XY(_state[i]) + velocity * interval;
                    _state[i] = new(position.X, position.Y, velocity.X, velocity.Y);
                }
                for (var iteration = 0; iteration < PressureIterations; iteration++)
                {
                    BuildGrid();
                    ParticlePass(_densityWorker);
                    ParticlePass(_integrateWorker);
                    (_state, _scratch) = (_scratch, _state);
                }
                BuildGrid(); ParticlePass(_finishWorker); (_state, _scratch) = (_scratch, _state);
                for (var slot = 0; slot < _actorLimit; slot++)
                {
                    var sum = Float4.Zero;
                    for (var i = 0; i < ActiveCount; i++) sum += _reactions[slot * Count + i];
                    React(_actors[slot], sum);
                }
            }
            PhysicsServer.SpaceStep(_space, interval);
            ContainBodies();
        }
        Drain(); Capture();
        StepMS = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
    private float SpawnY(int slot) => EntryY - LocalBounds(slot).End.Y - 24;
    internal Rect2 ActorBounds(int slot) => Pose(_actors[slot]) * LocalBounds(slot);
    internal bool ActorExists(int slot) => _actors[slot].IsValid();
    private void ContainBodies()
    {
        for (var slot = 0; slot < _actorLimit; slot++)
        {
            var body = _actors[slot]; if (!body.IsValid() || _kinds[slot] == WaterToy.Gate) continue;
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
    private void PullToy(float interval)
    {
        if (!_held || !_dragged.IsValid()) return;
        var state = PhysicsServer.BodyGetDirectState(_dragged)!;
        if (_dragged == _actors[GateSlot])
        {
            _gateEntering = false;
            MoveGate(Math.Clamp(_pointer.Y - _dragAnchor.Y, 260, 620), interval);
            return;
        }
        if (_dragged == _actors[WheelSlot] && state.InverseInertia > 0)
            PhysicsServer.BodyApplyTorqueImpulse(_dragged, -state.AngularVelocity * Math.Min(1, interval * 25) / state.InverseInertia);
        var arm = state.Transform.BasisXform(_dragAnchor);
        var error = _pointer - (state.Transform.Origin + arm);
        const float omega = 32;
        var acceleration = (error * (omega * omega) - state.GetVelocityAtLocalPosition(arm) * (2 * omega) + new Vector2(0, -980))
            / (1 + 2 * omega * interval + omega * omega * interval * interval);
        if (_dragged == _actors[BucketSlot])
            acceleration = ((error * 8).LimitLength(450) - state.LinearVelocity) * 40 + new Vector2(0, -980);
        PhysicsServer.BodyApplyImpulse(_dragged, acceleration.LimitLength(18000) * (interval / state.InverseMass), arm);
    }
    private static void FillToy(RID body, out Float4 pose, out Float4 motion, out float inverseInertia, out Float4 center)
    {
        pose = motion = center = default; inverseInertia = 0; if (!body.IsValid()) return;
        var state = PhysicsServer.BodyGetDirectState(body)!; var transform = state.Transform; var velocity = state.LinearVelocity;
        pose = new(transform.Origin.X * .01f, transform.Origin.Y * .01f, transform.Rotation, 1);
        motion = new(velocity.X * .01f, velocity.Y * .01f, state.AngularVelocity, state.InverseMass); inverseInertia = state.InverseInertia * 10000;
        center = new(state.CenterOfMassLocal.X * .01f, state.CenterOfMassLocal.Y * .01f, 0, 0);
    }
    private static void React(RID body, Float4 impulse)
    { if (body.IsValid() && PhysicsServer.BodyGetDirectState(body)!.InverseMass > 0) { PhysicsServer.BodyApplyCentralImpulse(body, new(impulse.X * 100, impulse.Y * 100)); PhysicsServer.BodyApplyTorqueImpulse(body, impulse.Z * 10000); } }
    private void Capture()
    {
        _minimumY = 0;
        for (var i = 0; i < ActiveCount; i++)
        {
            var p = Positions[i] = XY(_state[i]) * 100;
            _minimumY = Math.Min(_minimumY, _state[i].Y + Math.Min(0, _state[i].W) / 30);
        }
    }
    private Vector2 RotateBody(Vector2 p, int slot, bool inverse = false)
    {
        var rotation = _settings.Rotations[slot]; var sin = inverse ? -rotation.Y : rotation.Y;
        return new(rotation.X * p.X - sin * p.Y, sin * p.X + rotation.X * p.Y);
    }
    private static Vector2 XY(Float4 v) => new(v.X, v.Y);
    private static Vector2 Velocity(Float4 v) => new(v.Z, v.W);
    private (int X, int Y) Cell(Vector2 p) => (Math.Clamp((int)MathF.Floor(p.X / _h), 0, _columns - 1), Math.Clamp((int)MathF.Floor((p.Y - _gridTop) / _h), 0, _rows - 1));
    private void BuildGrid()
    {
        Array.Fill(_heads, -1);
        for (var i = 0; i < ActiveCount; i++) { var (x, y) = Cell(XY(_state[i])); var cell = y * _columns + x; _next[i] = _heads[cell]; _heads[cell] = i; }
    }
    private bool NeighborOffset(Vector2 position, int j, int mirror, Float4 boundary, out Vector2 offset)
    {
        var neighbor = XY(_state[j]); offset = default;
        if (mirror == 1) { if (position.X >= _h) return false; neighbor.X = -neighbor.X; }
        if (mirror == 2) { if (position.X <= Size.X * .01f - _h) return false; neighbor.X = Size.X * .02f - neighbor.X; }
        if (mirror == 3) { if (position.Y <= Size.Y * .01f - _h) return false; neighbor.Y = Size.Y * .02f - neighbor.Y; }
        if (mirror == 4)
        {
            if (boundary.W < 1) return false;
            var normal = XY(boundary); var distance = (neighbor - position).Dot(normal) + boundary.Z;
            if (distance < 0) return false;
            neighbor -= normal * (2 * distance);
        }
        offset = position - neighbor; return true;
    }
    private Float4 Boundary(Vector2 position)
    {
        var nearest = new Float4(0, 0, _h, 0);
        for (var slot = 0; slot < _actorLimit; slot++)
        {
            var pose = _settings.Poses[slot]; if (pose.W < .5f) continue;
            var origin = XY(pose);
            if ((position - origin).LengthSquared() > _settings.Details[slot].W) continue;
            var distance = Distance(RotateBody(position - origin, slot, true), _kinds[slot]);
            if (distance.Z < 0 || distance.Z >= nearest.Z) continue;
            var normal = RotateBody(XY(distance), slot); nearest = new(normal.X, normal.Y, distance.Z, slot + 1);
        }
        return nearest;
    }
    private void Density(int i)
    {
        var boundary = _boundary[i] = Boundary(XY(_state[i]));
        var (x, y) = Cell(XY(_state[i])); var h2 = _h * _h; var kernel = 4 / (MathF.PI * h2 * h2 * h2 * h2);
        var gradient = 30 / (MathF.PI * h2 * h2 * _h); var sum = 0f; var denominator = 0f; var ownGradient = Vector2.Zero;
        for (var cy = Math.Max(0, y - 1); cy <= Math.Min(_rows - 1, y + 1); cy++)
            for (var cx = Math.Max(0, x - 1); cx <= Math.Min(_columns - 1, x + 1); cx++)
                for (var j = _heads[cy * _columns + cx]; j != -1; j = _next[j])
                {
                    var neighborGradient = Vector2.Zero;
                    for (var mirror = 0; mirror < (boundary.W > 0 ? 5 : XY(_state[i]).X < _h || XY(_state[i]).X > Size.X * .01f - _h || XY(_state[i]).Y > Size.Y * .01f - _h ? 4 : 1); mirror++)
                    {
                        var offset = XY(_state[i]) - XY(_state[j]); if (mirror != 0 && !NeighborOffset(XY(_state[i]), j, mirror, boundary, out offset)) continue; var r2 = offset.LengthSquared(); if (r2 >= h2) continue;
                        var q = h2 - r2; sum += _mass * kernel * q * q * q;
                        if (r2 < 1e-10f) continue;
                        var distance = MathF.Sqrt(r2); var grad = -offset * (_mass / RestDensity * gradient * (_h - distance) * (_h - distance) / distance);
                        var reflected = grad;
                        if (mirror is 1 or 2) reflected.X = -reflected.X;
                        else if (mirror == 3) reflected.Y = -reflected.Y;
                        else if (mirror == 4) reflected -= 2 * XY(boundary) * grad.Dot(XY(boundary));
                        // A reflected sample moves with its real particle; it is not an independent mass.
                        ownGradient += i == j ? grad - reflected : grad;
                        neighborGradient -= reflected;
                    }
                    if (i != j) denominator += neighborGradient.LengthSquared();
                }
        _density[i] = sum; _lambda[i] = -Math.Max(0, sum / RestDensity - 1) / (denominator + ownGradient.LengthSquared() + .01f / h2);
    }
    private void Integrate(int i)
    {
        var state = _state[i]; var position = XY(state); var (x, y) = Cell(position); var h2 = _h * _h; var boundary = _boundary[i]; var solidCorrection = Vector2.Zero;
        var gradient = 30 / (MathF.PI * h2 * h2 * _h); var correction = Vector2.Zero;
        for (var cy = Math.Max(0, y - 1); cy <= Math.Min(_rows - 1, y + 1); cy++)
            for (var cx = Math.Max(0, x - 1); cx <= Math.Min(_columns - 1, x + 1); cx++)
                for (var j = _heads[cy * _columns + cx]; j != -1; j = _next[j])
                    for (var mirror = 0; mirror < (boundary.W > 0 ? 5 : XY(_state[i]).X < _h || XY(_state[i]).X > Size.X * .01f - _h || XY(_state[i]).Y > Size.Y * .01f - _h ? 4 : 1); mirror++)
                    {
                        var offset = position - XY(_state[j]); if (mirror != 0 && !NeighborOffset(position, j, mirror, boundary, out offset)) continue; var r2 = offset.LengthSquared(); if (r2 >= h2 || r2 < 1e-10f) continue;
                        var distance = MathF.Sqrt(r2);
                        var grad = -offset * (_mass / RestDensity * gradient * (_h - distance) * (_h - distance) / distance);
                        var displacement = (_lambda[i] + _lambda[j]) * grad;
                        correction += displacement; if (mirror == 4) solidCorrection += displacement;
                    }
        var relaxation = Math.Min(.25f, _h * .2f / Math.Max(correction.Length(), 1e-8f));
        if (boundary.W > 0)
        {
            var slot = (int)boundary.W - 1; var impulse = solidCorrection * (relaxation * _mass / _settings.World.Z);
            var pose = _settings.Poses[slot]; var motion = _settings.Motions[slot];
            var center = XY(_settings.Centers[slot]);
            ref var reaction = ref _reactions[slot * Count + i]; reaction.X -= impulse.X; reaction.Y -= impulse.Y; reaction.Z -= (position - center).Cross(impulse);
        }
        position += correction * relaxation; var radius = Spacing * .0045f;
        position.X = Math.Clamp(position.X, radius, Size.X * .01f - radius); position.Y = Math.Min(position.Y, Size.Y * .01f - radius);
        for (var slot = 0; slot < _actorLimit; slot++)
            Project(ref position, _settings.Poses[slot], _settings.Motions[slot], _settings.Details[slot].X, slot, ref _reactions[slot * Count + i]);
        position.X = Math.Clamp(position.X, radius, Size.X * .01f - radius); position.Y = Math.Min(position.Y, Size.Y * .01f - radius);
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
        velocity += viscosity * _settings.Integration.Y;
        for (var slot = 0; slot < _actorLimit; slot++)
            Collide(ref position, ref velocity, _settings.Poses[slot], _settings.Motions[slot], _settings.Details[slot].X, slot, ref _reactions[slot * Count + i]);
        var radius = Spacing * .0045f;
        position.X = Math.Clamp(position.X, radius, Size.X * .01f - radius); position.Y = Math.Min(position.Y, Size.Y * .01f - radius);
        if (position.X <= radius && velocity.X < 0 || position.X >= Size.X * .01f - radius && velocity.X > 0) velocity.X = 0;
        if (position.Y >= Size.Y * .01f - radius && velocity.Y > 0) velocity.Y = 0;
        _scratch[i] = new(position.X, position.Y, velocity.X, velocity.Y);
    }
    private void Project(ref Vector2 p, Float4 pose, Float4 motion, float inverseInertia, int slot, ref Float4 impulse)
    {
        if (pose.W < .5f) return;
        var origin = XY(pose);
        if ((p - origin).LengthSquared() > _settings.Details[slot].Y) return;
        var distance = Distance(RotateBody(p - origin, slot, true), _kinds[slot]); var radius = Spacing * .0045f;
        if (distance.Z >= radius) return;
        var normal = RotateBody(XY(distance), slot); var center = XY(_settings.Centers[slot]);
        var arm = p - center; var tangent = arm.Cross(normal);
        var correction = normal * ((radius - distance.Z) / (1 + _mass * (motion.W + tangent * tangent * inverseInertia)));
        p += correction; var j = correction * (_mass / _settings.World.Z); impulse.X -= j.X; impulse.Y -= j.Y; impulse.Z -= arm.Cross(j);
    }
    private void Collide(ref Vector2 p, ref Vector2 v, Float4 pose, Float4 motion, float inverseInertia, int slot, ref Float4 impulse)
    {
        if (pose.W < .5f) return;
        var origin = XY(pose);
        if ((p - origin).LengthSquared() > _settings.Details[slot].Y) return;
        var distance = Distance(RotateBody(p - origin, slot, true), _kinds[slot]); var radius = Spacing * .0045f;
        if (distance.Z >= radius) return;
        var normal = RotateBody(XY(distance), slot); p += normal * (radius - distance.Z);
        var center = XY(_settings.Centers[slot]); var arm = p - center;
        var velocity = XY(motion) + motion.Z * new Vector2(-arm.Y, arm.X); var tangent = arm.Cross(normal);
        var j = Math.Max(0, -(v - velocity).Dot(normal)) * _mass / (1 + _mass * (motion.W + tangent * tangent * inverseInertia));
        var action = normal * j; v += action / _mass;
        impulse.X -= action.X; impulse.Y -= action.Y; impulse.Z -= arm.Cross(action);
    }
    private static Float4 Circle(Vector2 p, float radius) { var length = p.Length(); var normal = length > 1e-7f ? p / length : new Vector2(0, -1); return new(normal.X, normal.Y, length - radius, 0); }
    private static Float4 Distance(Vector2 p, WaterToy kind)
    {
        if (kind == WaterToy.Bucket) return Near(Box(p - new Vector2(0, .44f), new(.57f, .06f)), Near(Box(p - new Vector2(-.52f, 0), new(.05f, .45f)), Box(p - new Vector2(.52f, 0), new(.05f, .45f))));
        if (kind == WaterToy.Wheel)
        {
            var nearest = Circle(p, .16f);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * MathF.PI / 4; var distance = Box(p.Rotated(-angle) - new Vector2(.45f, 0), new(.25f, .08f));
                var bladeNormal = XY(distance).Rotated(angle); nearest = Near(nearest, new(bladeNormal.X, bladeNormal.Y, distance.Z, 0));
            }
            return nearest;
        }
        if (kind == WaterToy.Gate) return Box(p, new(.06f, 1.8f));
        if (kind == WaterToy.Ball) return Circle(p, .28f);
        if (kind == WaterToy.Wood) return Box(p, new(.18f, .18f));
        if (kind == WaterToy.Steel) return Circle(p, .14f);
        if (kind == WaterToy.Platform) return Box(p, new(.5f, .06f));
        if (kind == WaterToy.Duck)
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
        if (_disposed) return; _disposed = true; FreeGPU(); _cpuDone.Dispose();
        foreach (var joint in _joints) PhysicsServer.FreeRID(joint);
        foreach (var body in _bodies) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(_space);
        foreach (var shape in _shapes) shape.Dispose();
    }
}
