namespace Electron2D;

public partial class GPUParticles
{
    private Vector2 TextureSize(Texture? texture)
    {
        var server = RenderingServer.Service; server?.BeginParticleScenePreparation();
        var preparing = _preparing; _preparing = true;
        try
        {
            var size = texture?.GetSize() ?? Vector2.One;
            if (!size.IsFinite() || size.X < 0 || size.Y < 0) throw new InvalidOperationException("Particle texture dimensions must be finite and nonnegative.");
            return size;
        }
        finally { _preparing = preparing; server?.EndParticleScenePreparation(); }
    }
    private float CollisionRadius(Texture? texture, float? multiplier = null)
    {
        var size = TextureSize(texture);
        var scale = texture is null ? 1 : (size.X + size.Y) * .25f;
        var radius = scale * (multiplier ?? _collisionBaseSize);
        if (!float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(texture));
        return radius;
    }
    private RID EnsureNative()
    {
        Read(); if (NativeLive) return _particles;
        if (_preparing) throw new InvalidOperationException("Particle preparation cannot reenter itself.");
        var server = RenderingServer.Service ?? throw new InvalidOperationException("GPU particle processing requires an active GPU rendering session.");
        _preparing = true; var particles = default(RID);
        try
        {
            particles = RenderingServer.ParticlesCreate();
            RenderingServer.ParticlesSetAmount(particles, _amount); RenderingServer.ParticlesSetLifetime(particles, (float)_lifetime);
            RenderingServer.ParticlesSetAmountRatio(particles, _amountRatio); RenderingServer.ParticlesSetExplosivenessRatio(particles, _explosiveness); RenderingServer.ParticlesSetRandomnessRatio(particles, _randomness);
            RenderingServer.ParticlesSetFixedFPS(particles, _fixedFPS); RenderingServer.ParticlesSetFractionalDelta(particles, _fractDelta); RenderingServer.ParticlesSetInterpolate(particles, _interpolate); RenderingServer.ParticlesSetInterpToEnd(particles, _interpToEnd);
            RenderingServer.ParticlesSetPreProcessTime(particles, (float)_preprocess); RenderingServer.ParticlesSetSpeedScale(particles, IsInsideTree && !CanProcess() ? 0 : (float)_speedScale);
            RenderingServer.ParticlesSetOneShot(particles, _oneShot); RenderingServer.ParticlesSetUseLocalCoordinates(particles, _localCoords); server.SetParticleSeed(particles, _seed);
            RenderingServer.ParticlesSetCustomAABB(particles, _visibilityRect); RenderingServer.ParticlesSetDrawOrder(particles, _drawOrder);
            RenderingServer.ParticlesSetProcessMaterial(particles, _processMaterial?.GetRID() ?? default); RenderingServer.ParticlesSetEmissionTransform(particles, GetGlobalTransform());
            RenderingServer.ParticlesSetEmitterVelocity(particles, _previousVelocity); RenderingServer.ParticlesSetEmitting(particles, _emitting);
            RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, _mesh.GetRID());
            RenderingServer.ParticlesSetTrails(particles, _trailEnabled, (float)_trailLifetime);
            RenderingServer.ParticlesSetTransformAlign(particles, _trailEnabled ? RenderingServer.ParticlesTransformAlign.YToVelocity : RenderingServer.ParticlesTransformAlign.Disabled);
            RebuildMesh(particles);
            _server = server; _particles = particles;
            _appliedEmission = GetGlobalTransform(); _appliedSpeed = IsInsideTree && !CanProcess() ? 0 : (float)_speedScale;
            BindSubEmitter(_subEmitter);
            return particles;
        }
        catch
        {
            if (particles.IsValid()) RenderingServer.FreeRID(particles);
            _particles = default; _server = null; _meshDirty = true;
            throw;
        }
        finally { _preparing = false; }
    }
    private void BindSubEmitter(string path)
    {
        var child = IsInsideTree && path.Length != 0 ? GetNodeOrNull(path) as GPUParticles : null;
        RenderingServer.ParticlesSetSubemitter(_particles, child is null || ReferenceEquals(child, this) ? default : child.EnsureNative());
    }
    internal void PrepareParticleFrame()
    {
        if (!NativeLive) return;
        var speed = IsInsideTree && !CanProcess() ? 0 : (float)_speedScale;
        if (speed != _appliedSpeed) { RenderingServer.ParticlesSetSpeedScale(_particles, speed); _appliedSpeed = speed; }
        var emission = GetGlobalTransform();
        if (!_localCoords && emission != _appliedEmission) { RenderingServer.ParticlesSetEmissionTransform(_particles, emission); _appliedEmission = emission; }
        if (_meshDirty) { _preparing = true; try { RebuildMesh(_particles); } finally { _preparing = false; } }
    }
    private void RebuildMesh(RID particles)
    {
        var size = TextureSize(_texture);
        var radius = _texture is null ? _collisionBaseSize : _collisionBaseSize * ((size.X + size.Y) * .25f);
        if (!float.IsFinite(radius)) throw new InvalidOperationException("Particle collision radius exceeds finite storage.");
        MeshSurfaceData data; Transform[] poses;
        if (_trailEnabled)
        {
            var segments = checked(_trailSections * _trailSectionSubdivisions); var count = checked((segments + 1) * 2);
            data = new() { Vertices = new Vector2[count], UVs = new Vector2[count], Bones = new int[checked(count * 4)], Weights = new float[checked(count * 4)], Indices = new int[checked(segments * 6)] };
            for (var j = 0; j <= segments; j++)
            {
                var v = (float)j / segments; var bone = j / _trailSectionSubdivisions; var blend = 1 - (float)(j % _trailSectionSubdivisions) / _trailSectionSubdivisions;
                for (var side = 0; side < 2; side++)
                {
                    var vertex = j * 2 + side; data.Vertices[vertex] = new((side - .5f) * size.X, 0); data.UVs[vertex] = new(side, v);
                    data.Bones[vertex * 4] = bone; data.Bones[vertex * 4 + 1] = Math.Min(_trailSections, bone + 1); data.Weights[vertex * 4] = blend; data.Weights[vertex * 4 + 1] = 1 - blend;
                }
                if (j == 0) continue;
                var b = j * 2 - 2; var offset = (j - 1) * 6;
                data.Indices[offset] = b; data.Indices[offset + 1] = b + 1; data.Indices[offset + 2] = b + 2;
                data.Indices[offset + 3] = b + 1; data.Indices[offset + 4] = b + 3; data.Indices[offset + 5] = b + 2;
            }
            poses = new Transform[_trailSections + 1]; Array.Fill(poses, Transform.Identity);
        }
        else
        {
            var half = size * .5f;
            data = new() { Vertices = [-half, new(half.X, -half.Y), half, new(-half.X, half.Y)], UVs = [Vector2.Zero, new(1, 0), Vector2.One, new(0, 1)], Indices = [0, 1, 2, 0, 2, 3] };
            poses = [];
        }
        var mapping = ParticleUVMapping(_texture);
        if (mapping != new Rect2(Vector2.Zero, Vector2.One))
            for (var i = 0; i < data.UVs.Length; i++) data.UVs[i] = mapping.Position + data.UVs[i] * mapping.Size;
        _mesh.ClearSurfaces(); _mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, data);
        RenderingServer.ParticlesSetTrailBindPoses(particles, poses); RenderingServer.ParticlesSetCollisionBaseSize(particles, radius); _meshDirty = false;
    }
    private Rect2 ParticleUVMapping(Texture? texture)
    {
        var mapping = new Rect2(Vector2.Zero, Vector2.One); var depth = 0;
        while (texture is not null)
        {
            if (++depth > 1024) throw new InvalidOperationException("Particle texture graph exceeds its depth budget.");
            if (texture is CanvasTexture channels) { texture = channels.DiffuseTexture; continue; }
            if (texture is not AtlasTexture atlas || atlas.Atlas is not { } parent) break;
            var size = TextureSize(parent); if (size.X == 0 || size.Y == 0) return default;
            var region = atlas.Region; var extent = new Vector2(region.Size.X == 0 ? size.X : region.Size.X, region.Size.Y == 0 ? size.Y : region.Size.Y);
            var scale = extent / size; mapping = new(region.Position / size + mapping.Position * scale, mapping.Size * scale); texture = parent;
        }
        if (!mapping.IsFinite()) throw new InvalidOperationException("Particle atlas coordinates exceed finite storage.");
        return mapping;
    }
    /// <summary>Records the native particle stream using its independently borrowed process and draw materials.</summary>
    protected override void OnDraw()
    {
        base.OnDraw(); if (IsDisposed) return;
        var particles = EnsureNative();
        if (_meshDirty) { _preparing = true; try { RebuildMesh(particles); } finally { _preparing = false; } }
        RenderingServer.CanvasItemAddParticles(GetCanvasItem(), particles, _texture?.GetRID() ?? default);
    }
    /// <summary>Returns current native particle bounds in emitter coordinates, or empty before native initialization.</summary>
    /// <returns>A cold synchronized bounds snapshot.</returns>
    public Rect2 CaptureRect() { Read(); return NativeLive ? RenderingServer.ParticlesGetCurrentAABB(_particles) : default; }
    /// <summary>Queues explicit simulation time independently of automatic speed scale.</summary>
    /// <param name="processTime">Nonnegative finite time in seconds.</param>
    /// <param name="processTimeResidual">Additional time processed with emission disabled.</param>
    public void RequestParticlesProcess(float processTime, float processTimeResidual = 0)
    { Number(processTime); Number(processTimeResidual); Edit(); RenderingServer.ParticlesRequestProcessTime(EnsureNative(), processTime, processTimeResidual); }
    /// <summary>Queues one manually supplied particle in the current native rendering session.</summary>
    /// <param name="transform">Particle pose.</param><param name="velocity">Particle velocity.</param>
    /// <param name="color">Particle color.</param><param name="custom">Four numeric custom channels.</param><param name="flags">Fields copied into the new particle.</param>
    public void EmitParticle(Transform transform, Vector2 velocity, Color color, Color custom, ParticlesEmitFlags flags)
    { Edit(); RenderingServer.ParticlesEmit(EnsureNative(), transform, velocity, color, custom, (int)flags); }
    /// <summary>Restarts native storage, emission and one-shot completion tracking.</summary>
    /// <param name="keepSeed">Preserves Seed even when UseFixedSeed is false.</param>
    public void Restart(bool keepSeed = false)
    {
        Edit(); if (!keepSeed && !_useFixedSeed) Seed = _random.Randi();
        if (NativeLive) { RenderingServer.ParticlesRestart(_particles); RenderingServer.ParticlesSetEmitting(_particles, true); }
        _emitting = true; BeginCycle(); SetInternalProcessing(_oneShot, true); InvalidateCanvas();
    }
}
