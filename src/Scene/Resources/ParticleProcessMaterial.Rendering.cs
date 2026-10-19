namespace Electron2D;

public sealed partial class ParticleProcessMaterial
{
    internal override MaterialState GetCanvasState()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_preparing) throw new InvalidOperationException("Particle material preparation cannot reenter itself.");
            _preparing = true;
            try
            {
                foreach (var texture in _curves) if (texture is not null) ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
                if (_emissionPointTexture is not null) ObjectDisposedException.ThrowIf(_emissionPointTexture.IsDisposed, _emissionPointTexture);
                if (_emissionNormalTexture is not null) ObjectDisposedException.ThrowIf(_emissionNormalTexture.IsDisposed, _emissionNormalTexture);
                if (_emissionColorTexture is not null) ObjectDisposedException.ThrowIf(_emissionColorTexture.IsDisposed, _emissionColorTexture);
                if (_emissionRingInnerRadius > _emissionRingRadius) throw new InvalidOperationException("The ring inner radius exceeds its outer radius.");
                var pointCount = _emissionPointTexture is null ? 0 : _emissionPointCount;
                ThrowIfDisposed();
                _state ??= new MaterialState(_gate, BuiltInShader.Value.GetProgram(), null) { ParticleMotion = new MaterialState(_gate, MotionShader.Value.GetProgram(), null) };
                if (_dirty)
                {
                    Write("Ranges", _ranges);
                    Span<Vector4> data = stackalloc Vector4[10]; data.Clear();
                    data[0] = new(_direction.X, _direction.Y, _spread, _lifetimeRandomness);
                    var flags = ((_flags & 1) != 0 ? 1 : 0) | ((_flags & 8) != 0 ? 2 : 0) | ((_flags & 16) != 0 ? 4 : 0);
                    data[1] = new(_gravity.X, _gravity.Y, _inheritVelocityRatio, flags);
                    data[2] = new(_emissionShapeOffset.X, _emissionShapeOffset.Y, _emissionShapeScale.X, _emissionShapeScale.Y);
                    data[3] = new(_emissionRectExtents.X, _emissionRectExtents.Y, _emissionSphereRadius, _emissionRingRadius);
                    data[4] = new(_emissionRingInnerRadius, _collisionFriction, _collisionBounce, _subEmitterFrequency);
                    data[5] = new(_velocityPivot.X, _velocityPivot.Y, 0, 0);
                    data[6] = new(_turbulenceNoiseSpeed.X, _turbulenceNoiseSpeed.Y, _turbulenceNoiseSpeed.Z, _turbulenceNoiseSpeedRandom);
                    data[7] = new(_turbulenceNoiseStrength, MathF.Pow(_turbulenceNoiseScale, .25f) * (4 / MathF.Pow(10, .25f)) - 4, _turbulenceEnabled ? 1 : 0, 0);
                    data[8] = new(_color.R, _color.G, _color.B, _color.A);
                    data[9] = new(_emissionPointTexture is null ? 0 : 1, _emissionNormalTexture is null ? 0 : 1, _emissionColorTexture is null ? 0 : 1, _collisionUseScale ? 1 : 0);
                    Write("Data", data);
                    Write("SubConfig", new Vector4i(_subEmitterAmountAtStart, _subEmitterAmountAtEnd, _subEmitterAmountAtCollision, _subEmitterKeepVelocity ? 1 : 0));
                    uint mask = 0, red = 0;
                    for (var i = 0; i < _curves.Length; i++)
                    {
                        if (_curves[i] is not null) mask |= 1u << i;
                        if (_curves[i] is CurveTexture) red |= 1u << i;
                    }
                    Write("CurveOptions", new Vector4i((int)mask, (int)red, 0, 0));
                    BindTextures(_state); BindTextures(_state.ParticleMotion!);
                    _dirty = false;
                }
                Write("SourceConfig", new Vector4i((int)_emissionShape, pointCount, (int)_subEmitterMode, (int)_collisionMode));
                return _state;
            }
            finally { _preparing = false; }
        }
    }
    private void BindTextures(MaterialState state)
    {
        for (var i = 0; i < state.Textures.Length; i++)
        {
            var binding = state.Program.Textures[i];
            if (binding.IsEngineTexture) continue;
            var texture = binding.Name switch
            {
                "Points" => _emissionPointTexture,
                "Normals" => _emissionNormalTexture,
                "PointColors" => _emissionColorTexture,
                _ when binding.Name.StartsWith("Curve", StringComparison.Ordinal) && int.TryParse(binding.Name.AsSpan(5), out var index) && (uint)index < _curves.Length => _curves[index],
                _ => throw new InvalidOperationException("Unknown built-in particle texture binding."),
            };
            state.Textures[i] = texture ?? Fallback.Value;
        }
    }
    private void Write(string name, ReadOnlySpan<Vector4> values)
    {
        Write(_state!, name, values); Write(_state!.ParticleMotion!, name, values);
    }
    private static void Write(MaterialState state, string name, ReadOnlySpan<Vector4> values)
    {
        var uniform = state.Program.Uniforms[name];
        for (var i = 0; i < values.Length; i++) uniform.Write(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset + i * uniform.Stride, uniform.ElementSize), values[i]);
    }
    private void Write(string name, Vector4i value)
    {
        Write(_state!, name, value); Write(_state!.ParticleMotion!, name, value);
    }
    private static void Write(MaterialState state, string name, Vector4i value)
    {
        var uniform = state.Program.Uniforms[name]; uniform.Write(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.ElementSize), value);
    }
}
