namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Sets a scene or server body's continuous collision detection mode.</summary>
    /// <param name="body">The live body identity; Areas and shapes are rejected.</param>
    /// <param name="mode">Disabled, a leading CastRay, or the complete CastShape trajectory.</param>
    /// <remarks>The mode survives detachment and static/kinematic roles. A changed mode wakes dynamic motion without changing other bodies' modes.</remarks>
    /// <exception cref="ArgumentException">The identity does not refer to a live body.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    public static void BodySetContinuousCollisionDetectionMode(RID body, CCDMode mode) => Service.ForceRuntime(body).SetCCDMode(mode);

    /// <summary>Returns a scene or server body's configured continuous collision detection mode.</summary>
    /// <param name="body">The live body identity.</param>
    /// <returns>The stored mode, Disabled by default, including while detached or not dynamic.</returns>
    /// <exception cref="ArgumentException">The identity does not refer to a live body.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    public static CCDMode BodyGetContinuousCollisionDetectionMode(RID body) => Service.ForceRuntime(body).ContinuousMode;
}

internal sealed partial class PhysicsBodyRuntime
{
    internal CCDMode ContinuousMode { get; private set; }
    internal void SetCCDMode(CCDMode mode)
    {
        EnsureMutable();
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (ContinuousMode == mode) return;
        ContinuousMode = mode; GPUParametersDirty = true; Wake();
    }
}
