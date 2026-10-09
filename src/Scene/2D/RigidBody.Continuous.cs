namespace Electron2D;

public partial class RigidBody
{
    /// <summary>Gets or sets continuous collision detection for this body's dynamic motion.</summary>
    /// <value>Disabled by default; CastRay tests a leading ray and CastShape tests the complete shape trajectory.</value>
    /// <remarks>The setting is stored while detached, frozen or temporarily static. Changes wake an attached dynamic body.
    /// Continuous detection does not change other bodies' configured modes. It reduces tunneling; CastRay may miss off-ray features.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="ObjectDisposedException">This body is disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the world owner thread or the physics world is stepping.</exception>
    public CCDMode ContinuousCD
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Runtime.ContinuousMode; }
        set { EnsureMutable(); Runtime.SetCCDMode(value); }
    }
}
