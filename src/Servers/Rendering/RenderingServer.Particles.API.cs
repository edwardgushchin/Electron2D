namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects planar particle draw ordering.</summary>
    public enum ParticlesDrawOrder
    {
        /// <summary>Original particle index.</summary>
        Index = 0,
        /// <summary>Oldest particles first.</summary>
        Lifetime = 1,
        /// <summary>Youngest particles first.</summary>
        ReverseLifetime = 2,
    }

    /// <summary>Selects planar particle orientation.</summary>
    public enum ParticlesTransformAlign
    {
        /// <summary>Preserves the process transform.</summary>
        Disabled = 0,
        /// <summary>Aligns the selected transform axis to velocity.</summary>
        YToVelocity = 2,
    }

    /// <summary>Selects the planar basis aligned with velocity.</summary>
    public enum ParticlesTransformAlignAxis
    {
        /// <summary>The local X basis.</summary>
        X = 0,
        /// <summary>The local Y basis.</summary>
        Y = 1,
    }

    /// <summary>Appends a retained particle draw command to a canvas item.</summary>
    /// <param name="item">A live canvas item identity.</param>
    /// <param name="particles">A particle identity owned by this renderer.</param>
    /// <param name="texture">A borrowed ordinary texture, or empty.</param>
    /// <remarks>Mesh passes supply geometry; the canvas item's material shades it independently of the particle process material.</remarks>
    public static void CanvasItemAddParticles(RID item, RID particles, RID texture = default)
    {
        var server = RequireService(); var source = server.Particle(particles); var state = server.Change(item, out var target);
        var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null;
        target.ServerCommandTarget(state).AppendServerParticles(source, image);
    }

    /// <summary>Creates owned planar GPU particle storage metadata.</summary>
    /// <returns>A fresh identity; capacity starts at zero.</returns>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Free with RenderingServer.FreeRID. Particle data and native buffers are allocated on demand; Compatibility rejects this family.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="NotSupportedException">The active renderer does not support GPU particles.</exception>
    public static RID ParticlesCreate()
    => RequireService().ParticlesCreateCore();

    /// <summary>Queries whether automatic emission is enabled.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <returns>The committed emission flag.</returns>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static bool ParticlesGetEmitting(RID particles)
    => RequireService().Particle(particles).Emitting;

    /// <summary>Queries whether a stopped emitter has drained its renderer activity interval.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <returns>True only after emission is disabled and the activity interval has elapsed.</returns>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static bool ParticlesIsInactive(RID particles)
    { var state = RequireService().Particle(particles); return !state.Emitting && state.Inactive; }

    /// <summary>Requests one simulation update, including for an emitter without a visible canvas command.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesRequestProcess(RID particles)
    { var state = RequireService().ChangeParticle(particles); state.Requested = true; }

    /// <summary>Queues explicit process and residual intervals for the next update.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="processTime">Nonnegative emitting process seconds.</param>
    /// <param name="processTimeResidual">Nonnegative residual seconds, with emission disabled.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Explicit intervals use fixed subdivisions, independently of SpeedScale. A later request replaces both pending intervals. Processing occurs when the renderer next updates this identity.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesRequestProcessTime(RID particles, float processTime, float processTimeResidual = 0)
    { ParticleNumber(processTime, nameof(processTime)); ParticleNumber(processTimeResidual, nameof(processTimeResidual)); var state = RequireService().ChangeParticle(particles); state.RequestTime = processTime; state.RequestResidual = processTimeResidual; state.Requested = true; }

    /// <summary>Restarts simulation state and its emission phase on the next update.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesRestart(RID particles)
    => RequireService().ChangeParticle(particles).Restart();

    /// <summary>Sets particle capacity and clears prior state when its size changes.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="amount">Nonnegative record capacity within native byte limits.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetAmount(RID particles, int amount)
    => RequireService().ChangeParticle(particles).Allocate(amount);

    /// <summary>Sets automatic emission.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="enabled">Whether the option is enabled.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetEmitting(RID particles, bool enabled)
    { var state = RequireService().ChangeParticle(particles); state.Emitting = enabled; }

    /// <summary>Sets one-cycle automatic emission.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="enabled">Whether the option is enabled.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetOneShot(RID particles, bool enabled)
    { var state = RequireService().ChangeParticle(particles); state.OneShot = enabled; }

    /// <summary>Sets fractional newborn processing.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="enabled">Whether the option is enabled.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetFractionalDelta(RID particles, bool enabled)
    { var state = RequireService().ChangeParticle(particles); state.FractionalDelta = enabled; }

    /// <summary>Sets fixed-step presentation interpolation.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="enabled">Whether the option is enabled.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetInterpolate(RID particles, bool enabled)
    { var state = RequireService().ChangeParticle(particles); state.Interpolate = enabled; }

    /// <summary>Sets emitter-local simulation coordinates.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="enabled">Whether the option is enabled.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetUseLocalCoordinates(RID particles, bool enabled)
    { var state = RequireService().ChangeParticle(particles); state.LocalCoordinates = enabled; }

    /// <summary>Sets the nominal particle lifetime.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">Positive finite value; durations use seconds.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetLifetime(RID particles, float value)
    { ParticleNumber(value, nameof(value), true); RequireService().ChangeParticle(particles).Lifetime = value; }

    /// <summary>Sets the restart preprocessing duration.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">Nonnegative finite value; durations use seconds.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetPreProcessTime(RID particles, float value)
    { ParticleNumber(value, nameof(value), false); RequireService().ChangeParticle(particles).Preprocess = value; }

    /// <summary>Sets the automatic simulation time scale.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">Nonnegative finite value; durations use seconds.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetSpeedScale(RID particles, float value)
    { ParticleNumber(value, nameof(value), false); RequireService().ChangeParticle(particles).SpeedScale = value; }

    /// <summary>Sets the process collision size.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">Nonnegative finite collision-size input, initially 0.01.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetCollisionBaseSize(RID particles, float value)
    { ParticleNumber(value, nameof(value), false); RequireService().ChangeParticle(particles).CollisionBaseSize = value; }

    /// <summary>Sets the active capacity ratio.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">A finite ratio, clamped to zero through one.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetAmountRatio(RID particles, float value)
    { value = ParticleRatio(value, nameof(value)); RequireService().ChangeParticle(particles).AmountRatio = value; }

    /// <summary>Sets the emission concentration.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">A finite ratio, clamped to zero through one.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetExplosivenessRatio(RID particles, float value)
    { value = ParticleRatio(value, nameof(value)); RequireService().ChangeParticle(particles).Explosiveness = value; }

    /// <summary>Sets the cycle/index emission-time randomness.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">A finite ratio, clamped to zero through one.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetRandomnessRatio(RID particles, float value)
    { value = ParticleRatio(value, nameof(value)); RequireService().ChangeParticle(particles).Randomness = value; }

    /// <summary>Sets the process interpolation-to-end input.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">A finite ratio, clamped to zero through one.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetInterpToEnd(RID particles, float value)
    { value = ParticleRatio(value, nameof(value)); RequireService().ChangeParticle(particles).InterpToEnd = value; }

    /// <summary>Selects fixed simulation frequency or variable stepping.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="FPS">Positive frequency; zero or negative selects variable stepping. Changing this option clears numerical state.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetFixedFPS(RID particles, int FPS)
    { var state = RequireService().ChangeParticle(particles); state.FixedFPS = FPS; state.GPU?.Dispose(); state.GPU = null; state.Restart(); }

    /// <summary>Sets the finite planar emission transform.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="transform">Emitter transform; identity initially. World canvas/bounds and child-local routing require an invertible pose.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetEmissionTransform(RID particles, Transform transform)
    { if (!transform.IsFinite()) throw new ArgumentException("Emission transforms must be finite.", nameof(transform)); RequireService().ChangeParticle(particles).Emission = transform; }

    /// <summary>Sets the finite planar emitter velocity supplied to process programs.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="velocity">Emitter velocity in pixels per second.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetEmitterVelocity(RID particles, Vector2 velocity)
    { if (!velocity.IsFinite()) throw new ArgumentException("Emitter velocity must be finite.", nameof(velocity)); RequireService().ChangeParticle(particles).EmitterVelocity = velocity; }

    /// <summary>Sets authored visibility bounds without reading GPU state.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="bounds">Emitter-local visibility bounds; initially (-4,-4,8,8).</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetCustomAABB(RID particles, Rect2 bounds)
    { if (!bounds.IsFinite() || !bounds.End.IsFinite()) throw new ArgumentException("Particle bounds must be finite.", nameof(bounds)); RequireService().ChangeParticle(particles).CustomAABB = bounds; }

    /// <summary>Reads current native particle bounds in emitter-local coordinates.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <returns>Active state bounds expanded by the longest draw-mesh axis; empty before allocation.</returns>
    /// <remarks>Owner-thread operations require an active GPU rendering session. This is an explicit GPU synchronization/readback query, outside native submission. World coordinates are transformed by the current emission inverse. Mesh callbacks must leave particle state stable.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    /// <exception cref="ArithmeticException">Native active coordinates are nonfinite.</exception>
    public static Rect2 ParticlesGetCurrentAABB(RID particles)
    { var server = RequireService(); server.EnsureDeviceAccess(); var state = server.Particle(particles); if (state.Preparing) throw new InvalidOperationException("Particle bounds cannot reenter preparation."); return state.Backend.ReadParticleBounds(state); }

    /// <summary>Sets the borrowed particle process material.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="material">A particle ShaderMaterial identity, or empty for the default process.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    /// <exception cref="NotSupportedException">The material kind or configured shader domain cannot process particles.</exception>
    public static void ParticlesSetProcessMaterial(RID particles, RID material)
    { var server = RequireService(); var value = material.IsValid() ? RenderingProgramRegistry<Material>.Resolve(material) : null; if (value is not null && (value is not ShaderMaterial || value.GetCanvasState() is { } process && process.Program.Mode != Shader.Mode.Particles)) throw new NotSupportedException("Particle process materials require a particle compute program."); server.ChangeParticle(particles).ProcessMaterial = value; }

    /// <summary>Selects the borrowed target for GPU-generated subemissions.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="subemitter">An identity from this rendering session, or empty. Cycles reject.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetSubemitter(RID particles, RID subemitter)
    => RequireService().SetParticleSubemitter(particles, subemitter);

    /// <summary>Sets the number of borrowed mesh draw passes.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="count">Nonnegative pass count. New entries are empty.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetDrawPasses(RID particles, int count)
    { if (count < 0 || (long)count * IntPtr.Size > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(count)); var server = RequireService(); var state = server.Particle(particles); var passes = new Mesh?[count]; Array.Copy(state.DrawPasses, passes, Math.Min(count, state.DrawPasses.Length)); server.ChangeParticle(particles).DrawPasses = passes; }

    /// <summary>Assigns one borrowed planar draw-pass mesh.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="pass">Existing zero-based draw-pass index.</param>
    /// <param name="mesh">Live mesh identity, or empty to disable the pass.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetDrawPassMesh(RID particles, int pass, RID mesh)
    => RequireService().SetParticleDrawPass(particles, pass, mesh);

    /// <summary>Selects particle drawing order.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">A defined planar option.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetDrawOrder(RID particles, ParticlesDrawOrder value)
    { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); RequireService().ChangeParticle(particles).DrawOrder = value; }

    /// <summary>Selects velocity alignment.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">A defined planar option.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetTransformAlign(RID particles, ParticlesTransformAlign value)
    { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); RequireService().ChangeParticle(particles).Align = value; }

    /// <summary>Selects aligned planar basis.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="value">A defined planar option.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetTransformAlignAxis(RID particles, ParticlesTransformAlignAxis value)
    { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); RequireService().ChangeParticle(particles).AlignAxis = value; }

    /// <summary>Sets copied planar trail bind poses.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="bindPoses">Copied per-bone poses. Zero or one disables segmented trail history.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetTrailBindPoses(RID particles, ReadOnlySpan<Transform> bindPoses)
    { foreach (var pose in bindPoses) if (!pose.IsFinite()) throw new ArgumentException("Trail bind poses must be finite.", nameof(bindPoses)); var copy = bindPoses.ToArray(); var state = RequireService().ChangeParticle(particles); if (state.GPU is not null && state.BindPoses.Length != copy.Length) { state.GPU.Dispose(); state.GPU = null; state.Restart(); } state.BindPoses = copy; }

    /// <summary>Configures retained trail history for skinned draw meshes.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="enabled">Whether per-particle history is enabled.</param>
    /// <param name="lengthSeconds">Finite history length of at least 0.01 seconds; capped at ten seconds.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. Stale, foreign or wrong-kind identities reject; configuration cannot change during native submission or particle preparation.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesSetTrails(RID particles, bool enabled, float lengthSeconds)
    { if (!float.IsFinite(lengthSeconds) || lengthSeconds < .01f - 1e-5f) throw new ArgumentOutOfRangeException(nameof(lengthSeconds)); var state = RequireService().ChangeParticle(particles); state.Trails = enabled; state.TrailLength = Math.Min(lengthSeconds, 10); state.GPU?.Dispose(); state.GPU = null; state.Restart(); }

    /// <summary>Queues one copied manual particle and switches off automatic emission.</summary>
    /// <param name="particles">A live caller-owned particle identity.</param>
    /// <param name="transform">Supplied planar particle pose.</param>
    /// <param name="velocity">Supplied planar velocity.</param>
    /// <param name="color">Supplied particle color.</param>
    /// <param name="custom">Supplied four numeric custom channels.</param>
    /// <param name="emitFlags">Bits 1 position, 2 basis, 4 velocity, 8 color and 16 custom select supplied fields. Remaining bits are ignored.</param>
    /// <remarks>Owner-thread operations require an active GPU rendering session. If automatic emission was enabled, its previous state is cleared on processing. The pending queue is bounded by capacity; excess records are dropped. No caller-owned data is retained.</remarks>
    /// <exception cref="InvalidOperationException">No active session, wrong thread, or invalid owner/preparation state.</exception>
    /// <exception cref="ArgumentException">A particle identity or supplied value is invalid.</exception>
    public static void ParticlesEmit(RID particles, Transform transform, Vector2 velocity, Color color, Color custom, int emitFlags)
    { if (!transform.IsFinite() || !velocity.IsFinite() || !color.IsFinite() || !custom.IsFinite()) throw new ArgumentException("Manual emission values must be finite."); RequireService().ChangeParticle(particles).Emit(transform, velocity, color, custom, emitFlags); }

}
