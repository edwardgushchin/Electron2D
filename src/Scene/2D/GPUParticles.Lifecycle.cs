namespace Electron2D;

public partial class GPUParticles
{
    /// <summary>Updates scene timing, emitter physics velocity, pause state and native membership.</summary>
    /// <param name="what">The inherited notification identity.</param>
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;
        HandleNotification();
        if (reversed) base.OnNotification(what);
        void HandleNotification()
        {
            if (!reversed) base.OnNotification(what);
            if (IsDisposed) return;
            if (what == NotificationEnterTree && IsInsideTree)
            {
                if (_oneShot && _emitting && (!_sceneStarted || _server is not null && !ReferenceEquals(_server, RenderingServer.Service))) BeginCycle();
                _sceneStarted = true;
                _previousPosition = GetGlobalTransform().Origin; _previousVelocity = Vector2.Zero;
                SetInternalProcessing(true, true); if (NativeLive) { BindSubEmitter(_subEmitter); RenderingServer.ParticlesSetSpeedScale(_particles, CanProcess() ? (float)_speedScale : 0); }
            }
            else if (what == NotificationExitTree) { if (NativeLive) RenderingServer.ParticlesSetSubemitter(_particles, default); }
            else if (what == NotificationTransformChanged) { if (NativeLive && !_localCoords && _server!.CanChangeParticleSceneState) { _appliedEmission = GetGlobalTransform(); RenderingServer.ParticlesSetEmissionTransform(_particles, _appliedEmission); } }
            else if (what is NotificationPaused or NotificationUnpaused)
            { if (NativeLive && IsInsideTree) { RenderingServer.NotifyRenderMutation(); if (_server!.CanChangeParticleSceneState) { _appliedSpeed = CanProcess() ? (float)_speedScale : 0; RenderingServer.ParticlesSetSpeedScale(_particles, _appliedSpeed); } } }
            else if (what == NotificationInternalPhysicsProcess && IsInsideTree)
            {
                var position = GetGlobalTransform().Origin; var delta = PhysicsProcessDeltaTime;
                var velocity = delta > 0 ? (position - _previousPosition) / (float)delta : Vector2.Zero;
                if (!velocity.IsFinite()) throw new InvalidOperationException("Particle emitter velocity exceeds finite storage.");
                if (NativeLive && velocity != _previousVelocity) RenderingServer.ParticlesSetEmitterVelocity(_particles, velocity);
                _previousVelocity = velocity; _previousPosition = position;
            }
            else if (what == NotificationInternalProcess && IsInsideTree && _oneShot)
            {
                _time += ProcessDeltaTime;
                if (_time > _emissionTime) _emitting = false;
                var finished = _time > _activeTime && _active && !_signalCanceled;
                if (_time > _activeTime) _active = false;
                if (!_emitting && !_active) SetInternalProcessing(false, true);
                if (finished) BeginEventEmission(_publisherFinished.Handlers)?.Invoke();
            }

        }
    }
    /// <summary>Reports invalid child routing and missing particle process configuration.</summary>
    /// <returns>Independent inherited and emitter warning strings.</returns>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        if (_processMaterial is null) warnings = [.. warnings, "GPU particles have no process material."];
        if (_subEmitter.Length != 0 && (GetNodeOrNull(_subEmitter) is not GPUParticles target || ReferenceEquals(target, this))) warnings = [.. warnings, "The sub-emitter path must resolve to another GPU particle emitter."];
        return warnings;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (NativeLive) RenderingServer.FreeRID(_particles);
            _particles = default; _server = null; if (_texture is not null) _texture.Changed -= TextureChanged;
            _texture = null; _processMaterial = null; _publisherFinished.Handlers = null; _mesh.Dispose(); _random.Dispose();
        }
        base.Dispose(disposing);
    }
}
