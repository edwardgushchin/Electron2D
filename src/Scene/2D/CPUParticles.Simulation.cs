namespace Electron2D;

public partial class CPUParticles
{
    private struct Particle
    {
        internal Transform Pose;
        internal Vector2 Velocity;
        internal Color Tint, BaseColor, InitialColor;
        internal float AngleRandom, ScaleRandom, HueRandom, AnimationRandom, Rotation, Animation;
        internal double Age, Lifetime;
        internal uint Seed;
        internal bool Active;
    }
    private Particle[] _particles = new Particle[8], _scratch = new Particle[8];
    private int[] _order = new int[8];
    private readonly RandomNumberGenerator _rng = new();
    private double _time, _remainder;
    private uint _cycle;
    private bool _advancing, _prepared;
    private Transform _followPrevious, _followCurrent;
    private bool _followValid;
    internal int ActiveParticleCount { get { var count = 0; foreach (var p in _particles) if (p.Active) count++; return count; } }
    internal Transform ParticlePose(int index) => _particles[index].Pose;
    internal Color ParticleColor(int index) => _particles[index].Tint;
    internal double ParticleAge(int index) => _particles[index].Age;
    internal float ParticleAnimation(int index) => _particles[index].Animation;
    internal override bool CanvasUsesWorldCoordinates => !_localCoords;

    /// <summary>Clears particles and time, enables emission and optionally retains the current seed.</summary>
    /// <param name="keepSeed">Preserve Seed even when UseFixedSeed is false.</param>
    /// <remarks>Preparation occurs on the next update; detached restart needs no graphics backend.</remarks>
    public void Restart(bool keepSeed = false)
    {
        Edit(); Array.Clear(_particles); _time = _remainder = 0; _cycle = 0; _prepared = false;
        if (!keepSeed && !_useFixedSeed) _seed = _rng.Randi();
        _active = _emitting = true; SetInternalProcessing(true, false); InvalidateCanvas();
    }
    /// <summary>Processes explicit emitting and nonemitting intervals independently of SpeedScale.</summary>
    /// <param name="processTime">Nonnegative seconds processed with emission enabled.</param>
    /// <param name="processTimeResidual">Nonnegative seconds processed with emission disabled.</param>
    /// <remarks>Uses FixedFPS or 30 Hz subdivisions. Each request is bounded to 3600 seconds and 262144 steps.
    /// Explicit requests work detached and while speed is zero. Emitting reflects the last nonempty interval.
    /// Invalid inputs fail before simulation. Finished handlers run after the final state commits.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An interval exceeds the bounds or is nonfinite/negative.</exception>
    /// <exception cref="InvalidOperationException">Reentrant simulation, capture, off-owner access or numeric overflow.</exception>
    public void RequestParticlesProcess(float processTime, float processTimeResidual = 0)
    {
        Edit(); ValidateSeek(processTime); ValidateSeek(processTimeResidual);
        var steps = Math.Ceiling((processTime + (double)processTimeResidual + (_prepared ? 0 : _preprocess)) * (_fixedFPS > 0 ? _fixedFPS : 30));
        if (steps > 262144) throw new ArgumentOutOfRangeException(nameof(processTime));
        RunUpdate(0, processTime, processTimeResidual);
    }
    private void ValidateSeek(double seconds) { Nonnegative(seconds); if (seconds > 3600 || Math.Ceiling(seconds * (_fixedFPS > 0 ? _fixedFPS : 30)) > 262144) throw new ArgumentOutOfRangeException(nameof(seconds)); }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what); if (IsDisposed) return;
        if (what == NotificationEnterTree) { SetInternalProcessing(_emitting || _active, false); _followCurrent = _followPrevious = GetGlobalTransform(); _followValid = true; }
        else if (what == NotificationResetPhysicsInterpolation) { _followCurrent = _followPrevious = GetGlobalTransform(); _followValid = true; }
        else if (what == NotificationInternalProcess && IsVisibleInTree) RunUpdate(ProcessDeltaTime, 0, 0);
    }
    internal override void BeginPhysicsInterpolationTick()
    {
        base.BeginPhysicsInterpolationTick();
        if (Tree?.IsPhysicsInterpolationActive != true) return;
        _followPrevious = _followValid ? _followCurrent : GetGlobalTransform();
    }
    internal override void EndPhysicsInterpolationTick()
    {
        base.EndPhysicsInterpolationTick();
        if (Tree?.IsPhysicsInterpolationActive != true) return;
        _followCurrent = GetGlobalTransform(); _followValid = true;
    }
    private Transform EmissionPose()
    {
        if (_localCoords) return Transform.Identity;
        var current = GetGlobalTransform();
        if (Tree?.IsPhysicsInterpolationActive != true || !_followValid) return current;
        if (current != _followCurrent && !Tree.IsInPhysicsFrame) _followCurrent = _followPrevious = current;
        return _followPrevious.InterpolateWith(_followCurrent, (float)Engine.PhysicsInterpolationFraction);
    }
    private void RunUpdate(double delta, double emitTime, double residual)
    {
        if (_advancing) throw new InvalidOperationException("Particle simulation cannot be reentered.");
        if (!_active && !_emitting && emitTime == 0 && residual == 0) return;
        _advancing = true; bool finished = false, stopped = false;
        try
        {
            // Prepare curve caches outside the first measured update. Live edits are prepared again on demand.
            var pose = EmissionPose(); if (!pose.IsFinite()) throw new InvalidOperationException("Emitter pose overflowed.");
            if (_shape == EmissionShapeMode.Ring && _innerRadius > _ringRadius) throw new InvalidOperationException("Inner ring radius exceeds the outer radius.");
            if (!_prepared) { ValidateSeek(_preprocess); _prepared = true; AdvanceInterval(_preprocess, ref finished, ref stopped); }
            if (emitTime > 0) { _emitting = _active = true; AdvanceInterval(emitTime, ref finished, ref stopped); }
            if (residual > 0) { _emitting = false; AdvanceInterval(residual, ref finished, ref stopped); }
            if (_fixedFPS == 0)
            {
                var scaled = delta * _speedScale; if (!double.IsFinite(scaled)) throw new InvalidOperationException("Particle time overflowed.");
                // Bound long ordinary frames to emission-cycle subdivisions so no birth interval is skipped.
                AdvanceOrdinary(scaled, ref finished, ref stopped);
            }
            else
            {
                var frame = 1d / _fixedFPS; var todo = _remainder + Math.Clamp(delta, 0, .1);
                while (todo >= frame) { AdvanceOrdinary(frame * _speedScale, ref finished, ref stopped); todo -= frame; }
                _remainder = todo;
            }
            InvalidateCanvas();
            if (!_active && !_emitting) { SetInternalProcessing(false, false); _time = _remainder = 0; _cycle = 0; _prepared = false; }
        }
        finally { _advancing = false; }
        // Commit lifecycle before callbacks; failures are not replayed by another process frame.
        Exception? callbackError = null;
        if (stopped) try { NotifyPropertyListChanged(); } catch (Exception error) { callbackError = error; }
        if (finished && !IsDisposed) try { Finished?.Invoke(); } catch (Exception error) { if (callbackError is not null) throw new AggregateException("Particle completion callbacks failed.", callbackError, error); throw; }
        if (callbackError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(callbackError).Throw();
    }
    private void AdvanceInterval(double seconds, ref bool finished, ref bool stopped)
    {
        var frame = 1d / (_fixedFPS > 0 ? _fixedFPS : 30);
        while (seconds > 0) { var step = Math.Min(frame, seconds); AdvanceOrdinary(step, ref finished, ref stopped); seconds = Math.Max(0, seconds - step); }
    }
    private void AdvanceOrdinary(double seconds, ref bool finished, ref bool stopped)
    {
        Nonnegative(seconds); var count = Math.Ceiling(seconds / _lifetime);
        if (!double.IsFinite(count) || count > 262144) throw new InvalidOperationException("Particle step exceeds the cycle budget.");
        if (seconds == 0) return;
        while (seconds > 0) { var step = Math.Min(seconds, _lifetime); Step(step, ref finished, ref stopped); seconds = Math.Max(0, seconds - step); }
    }
    private void Step(double delta, ref bool finished, ref bool stopped)
    {
        var previousTime = _time; var absolute = previousTime + delta;
        if (!double.IsFinite(absolute)) throw new InvalidOperationException("Particle clock overflowed.");
        var wrapped = absolute >= _lifetime; var time = wrapped ? absolute % _lifetime : absolute;
        var cycle = unchecked(_cycle + (wrapped ? 1u : 0u)); var emitting = _emitting;
        var pose = EmissionPose(); var any = false;
        for (var i = 0; i < _particles.Length; i++)
        {
            var p = _particles[i]; if (!emitting && !p.Active) { _scratch[i] = p; continue; }
            var phase = i / (double)_particles.Length;
            if (_randomness > 0)
            {
                var phaseCycle = unchecked(cycle - (phase >= time / _lifetime ? 1u : 0u));
                phase += _randomness * (Hash(unchecked(phaseCycle * (uint)_particles.Length + (uint)i)) % 65536 / 65536d) / _particles.Length;
            }
            var birth = phase * (1 - _explosiveness) * _lifetime;
            var crossed = !wrapped ? birth >= previousTime && birth < time : birth >= previousTime || birth < time;
            var birthBeforeEnd = !wrapped || birth >= previousTime;
            var canEmit = emitting && !(_oneShot && _cycle > 0) && (!wrapped || !_oneShot || birthBeforeEnd);
            var localDelta = delta;
            if (crossed)
            {
                if (canEmit)
                {
                    p = Spawn(i, unchecked(_cycle + (wrapped && !birthBeforeEnd ? 1u : 0u)), pose);
                    if (_fractDelta) localDelta = wrapped && birthBeforeEnd ? _lifetime - birth + time : time - birth;
                    // Birth keeps normalized age zero for its first visual sample; fractional motion still applies.
                    Present(ref p, 0); p.Pose.Origin += p.Velocity * (float)localDelta;
                }
                else p.Active = false;
            }
            else if (p.Active)
            {
                if (p.Age >= p.Lifetime) p.Active = false;
                else
                {
                    p.Age += localDelta; var progress = p.Lifetime == 0 ? 1 : (float)(p.Age / p.Lifetime);
                    Integrate(ref p, pose.Origin, (float)localDelta, progress); Present(ref p, progress); p.Pose.Origin += p.Velocity * (float)localDelta;
                }
            }
            if (p.Active && (!p.Pose.IsFinite() || !p.Velocity.IsFinite() || !p.Tint.IsFinite() || !float.IsFinite(p.Animation))) throw new InvalidOperationException("Particle state exceeded the finite canvas range.");
            any |= p.Active; _scratch[i] = p;
        }
        (_particles, _scratch) = (_scratch, _particles); _time = time; _cycle = cycle;
        if (_oneShot && wrapped && _emitting) { _emitting = false; stopped = true; }
        if (_active && !any && !_emitting) { _active = false; finished = true; }
        else if (any) _active = true;
    }
    private Particle Spawn(int index, uint cycle, Transform emission)
    {
        var p = new Particle { Active = true, Seed = unchecked(_seed + (uint)(index * 2) + cycle), Pose = Transform.Identity, BaseColor = Colors.White };
        _rng.Seed = p.Seed;
        p.AngleRandom = _rng.Randf(); p.ScaleRandom = _rng.Randf(); p.HueRandom = _rng.Randf(); p.AnimationRandom = _rng.Randf();
        p.InitialColor = _initialRamp?.Sample(_rng.Randf()) ?? Colors.White;
        var angle = _direction.Angle() + Mathf.DegToRad((_rng.Randf() * 2 - 1) * _spread);
        p.Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Range(0, _rng.Randf());
        p.Rotation = Mathf.DegToRad(CurveValue(7, 0) * Range(7, p.AngleRandom));
        p.Animation = CurveValue(11, 0) * Range(11, p.AnimationRandom);
        p.Lifetime = _lifetime * (1 - _rng.Randf() * _lifetimeRandomness);
        switch (_shape)
        {
            case EmissionShapeMode.Sphere: { var theta = Mathf.Tau * _rng.Randf(); p.Pose.Origin = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (_sphereRadius * _rng.Randf()); break; }
            case EmissionShapeMode.SphereSurface: { var sample = _rng.Randf(); var theta = Mathf.Tau * _rng.Randf(); p.Pose.Origin = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (_sphereRadius * MathF.Sqrt(1 - sample * sample)); break; }
            case EmissionShapeMode.Rectangle: p.Pose.Origin = new Vector2(_rng.Randf() * 2 - 1, _rng.Randf() * 2 - 1) * _rectExtents; break;
            case EmissionShapeMode.Points:
            case EmissionShapeMode.DirectedPoints:
                if (_points.Length != 0)
                {
                    var selected = (int)(_rng.Randi() % (uint)_points.Length); p.Pose.Origin = _points[selected];
                    if (_shape == EmissionShapeMode.DirectedPoints && _normals.Length == _points.Length) { var normal = _normals[selected]; p.Velocity = normal * p.Velocity.X + normal.Orthogonal() * p.Velocity.Y; }
                    if (_colors.Length == _points.Length) p.BaseColor = _colors[selected];
                }
                break;
            case EmissionShapeMode.Ring:
                var t = Mathf.Tau * _rng.Randf(); var radius = Math.Sqrt(_rng.Randf() * ((double)_ringRadius * _ringRadius - (double)_innerRadius * _innerRadius) + (double)_innerRadius * _innerRadius);
                p.Pose.Origin = new Vector2(MathF.Cos(t), MathF.Sin(t)) * (float)radius; break;
        }
        if (!_localCoords) { p.Velocity = emission.BasisXform(p.Velocity); p.Pose = emission * p.Pose; }
        return p;
    }
    private void Integrate(ref Particle p, Vector2 origin, float delta, float progress)
    {
        var seed = p.Seed; var difference = p.Pose.Origin - origin;
        var force = _gravity + p.Velocity.Normalized() * Value(3, progress, ref seed) + difference.Normalized() * Value(4, progress, ref seed) + new Vector2(-difference.Y, difference.X).Normalized() * Value(5, progress, ref seed);
        p.Velocity += force * delta;
        var orbit = Value(2, progress, ref seed); if (orbit != 0) p.Pose.Origin = origin + difference.Rotated(-orbit * delta * Mathf.Tau);
        if (_curves[0] is not null) p.Velocity = p.Velocity.Normalized() * CurveValue(0, progress);
        if (_maximum[6] + CurveValue(6, progress) > 0) { var damping = Value(6, progress, ref seed); p.Velocity = p.Velocity.Normalized() * Math.Max(0, p.Velocity.Length() - damping * delta); }
        p.Rotation = Mathf.DegToRad(CurveValue(7, progress) * Range(7, p.AngleRandom) + (float)p.Age * Value(1, progress, ref seed));
        p.Animation = CurveValue(11, progress) * Range(11, p.AnimationRandom) + progress * Value(10, progress, ref seed);
    }
    private void Present(ref Particle p, float progress)
    {
        var scale = _splitScale ? new Vector2(_scaleX?.Sample(progress) ?? 1, _scaleY?.Sample(progress) ?? 1) : Vector2.One * CurveValue(8, progress);
        scale *= Range(8, p.ScaleRandom); scale.X = Math.Max(.00001f, scale.X); scale.Y = Math.Max(.00001f, scale.Y);
        if (_alignY) { var y = p.Velocity.Length() > 0 ? p.Velocity.Normalized() : p.Pose.Y.Normalized(); p.Pose.X = y.Orthogonal() * scale.X; p.Pose.Y = y * scale.Y; }
        else { p.Pose.X = new Vector2(MathF.Cos(p.Rotation), -MathF.Sin(p.Rotation)) * scale.X; p.Pose.Y = new Vector2(MathF.Sin(p.Rotation), MathF.Cos(p.Rotation)) * scale.Y; }
        var tint = (_ramp?.Sample(progress) ?? Colors.White) * _color;
        var angle = CurveValue(9, progress, 0) * Mathf.Tau * Range(9, p.HueRandom); tint = Hue(tint, angle);
        p.Tint = tint * p.BaseColor * p.InitialColor;
    }
    private static Color Hue(Color color, float angle)
    {
        var c = MathF.Cos(angle); var s = MathF.Sin(angle); var r = color.R; var g = color.G; var b = color.B;
        return new((.299f + .701f * c + .168f * s) * r + (.587f - .587f * c + .330f * s) * g + (.114f - .114f * c - .497f * s) * b,
            (.299f - .299f * c - .328f * s) * r + (.587f + .413f * c + .035f * s) * g + (.114f - .114f * c + .292f * s) * b,
            (.299f - .300f * c + 1.250f * s) * r + (.587f - .588f * c - 1.050f * s) * g + (.114f + .886f * c - .203f * s) * b, color.A);
    }
    private float CurveValue(int channel, float progress, float fallback = 1) => _curves[channel]?.Sample(progress) ?? fallback;
    private float Range(int channel, float random) => Mathf.Lerp(_minimum[channel], _maximum[channel], random);
    private float Value(int channel, float progress, ref uint seed) => CurveValue(channel, progress) * Range(channel, NextRandom(ref seed));
    private static uint Hash(uint x) { x = unchecked(((x >> 16) ^ x) * 0x45d9f3b); x = unchecked(((x >> 16) ^ x) * 0x45d9f3b); return (x >> 16) ^ x; }
    private static float NextRandom(ref uint seed) { var s = unchecked((int)seed); if (s == 0) s = 305420679; var k = s / 127773; s = unchecked(16807 * (s - k * 127773) - 2836 * k); if (s < 0) s = unchecked(s + 2147483647); seed = unchecked((uint)s); return seed % 65536 / 65535f; }
    /// <inheritdoc />
    protected override void OnDraw()
    {
        base.OnDraw(); if (IsDisposed) return;
        for (var i = 0; i < _order.Length; i++) _order[i] = i;
        if (_drawOrder == DrawOrderMode.Lifetime) SortOrder(0, _order.Length - 1);
        var size = _texture?.GetSize() ?? Vector2.One; var destination = new Rect2(-size * .5f, size);
        foreach (var index in _order)
        {
            ref var p = ref _particles[index]; if (!p.Active) continue;
            DrawSetTransformMatrix(p.Pose);
            DrawParticleQuad(_texture, destination, p.Tint, new(0, (float)(p.Age / _lifetime), p.Animation, (float)(p.Lifetime / _lifetime)));
        }
        DrawSetTransformMatrix(Transform.Identity);
    }
    // ponytail: in-place quicksort reuses the order array; radix sorting can replace it for very large emitters.
    private void SortOrder(int lo, int hi)
    {
        while (lo < hi)
        {
            var pivot = _particles[_order[(lo + hi) / 2]].Age; var i = lo; var j = hi;
            while (i <= j) { while (_particles[_order[i]].Age > pivot) i++; while (_particles[_order[j]].Age < pivot) j--; if (i <= j) { (_order[i], _order[j]) = (_order[j], _order[i]); i++; j--; } }
            if (j - lo < hi - i) { if (lo < j) SortOrder(lo, j); lo = i; } else { if (i < hi) SortOrder(i, hi); hi = j; }
        }
    }
    /// <summary>Reports animation parameters that lack an enabled particle sprite-sheet material.</summary>
    /// <returns>Independent inherited and particle warning strings.</returns>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        if ((_maximum[10] != 0 || _maximum[11] != 0 || _curves[10] is not null || _curves[11] is not null) && CanvasMaterial is not CanvasItemMaterial { ParticlesAnimation: true }) return [.. warnings, "Particle animation requires an enabled CanvasItemMaterial particle sprite sheet."];
        return warnings;
    }
}
