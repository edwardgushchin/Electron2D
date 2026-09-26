namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Sets the gravity reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default Disabled.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetGravitySpaceOverride(RID area, Area.SpaceOverride value)
    {
        PhysicsAreaFields.ValidateMode(value);
        AreaFieldState(area, true).GravitySpaceOverride = value;
    }
    /// <summary>Gets the gravity reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default Disabled.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public Area.SpaceOverride AreaGetGravitySpaceOverride(RID area) => AreaFieldState(area).GravitySpaceOverride;

    /// <summary>Sets signed gravity strength in scene units per second squared.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default 9.80665 for server-only Areas; 980 for scene Areas.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetGravity(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).Gravity = value;
    }
    /// <summary>Gets signed gravity strength in scene units per second squared.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default 9.80665 for server-only Areas; 980 for scene Areas.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public float AreaGetGravity(RID area) => AreaFieldState(area).Gravity;

    /// <summary>Sets the unnormalized direction or local point center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default (0, -1) for server-only Areas; (0, 1) for scene Areas.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetGravityVector(RID area, Vector2 value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).GravityVector = value;
    }
    /// <summary>Gets the unnormalized direction or local point center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default (0, -1) for server-only Areas; (0, 1) for scene Areas.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public Vector2 AreaGetGravityVector(RID area) => AreaFieldState(area).GravityVector;

    /// <summary>Sets whether the vector is a transformed attraction center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default false.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void AreaSetGravityPoint(RID area, bool value)
    {
        AreaFieldState(area, true).GravityPoint = value;
    }
    /// <summary>Gets whether the vector is a transformed attraction center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default false.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public bool AreaGetGravityPoint(RID area) => AreaFieldState(area).GravityPoint;

    /// <summary>Sets the unit distance of inverse-square point gravity; nonpositive means constant strength.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default zero.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetGravityPointUnitDistance(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).GravityPointUnitDistance = value;
    }
    /// <summary>Gets the unit distance of inverse-square point gravity; nonpositive means constant strength.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public float AreaGetGravityPointUnitDistance(RID area) => AreaFieldState(area).GravityPointUnitDistance;

    /// <summary>Sets the linear damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default Disabled.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetLinearDampSpaceOverride(RID area, Area.SpaceOverride value)
    {
        PhysicsAreaFields.ValidateMode(value);
        AreaFieldState(area, true).LinearDampSpaceOverride = value;
    }
    /// <summary>Gets the linear damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default Disabled.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public Area.SpaceOverride AreaGetLinearDampSpaceOverride(RID area) => AreaFieldState(area).LinearDampSpaceOverride;

    /// <summary>Sets signed linear damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default 0.1.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetLinearDamp(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).LinearDamp = value;
    }
    /// <summary>Gets signed linear damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default 0.1.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public float AreaGetLinearDamp(RID area) => AreaFieldState(area).LinearDamp;

    /// <summary>Sets the angular damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default Disabled.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetAngularDampSpaceOverride(RID area, Area.SpaceOverride value)
    {
        PhysicsAreaFields.ValidateMode(value);
        AreaFieldState(area, true).AngularDampSpaceOverride = value;
    }
    /// <summary>Gets the angular damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default Disabled.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public Area.SpaceOverride AreaGetAngularDampSpaceOverride(RID area) => AreaFieldState(area).AngularDampSpaceOverride;

    /// <summary>Sets signed angular damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default one.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public void AreaSetAngularDamp(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).AngularDamp = value;
    }
    /// <summary>Gets signed angular damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default one.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public float AreaGetAngularDamp(RID area) => AreaFieldState(area).AngularDamp;

    /// <summary>Sets the integer reduction priority; larger values run first.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default zero.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public void AreaSetPriority(RID area, int value)
    {
        AreaFieldState(area, true).Priority = value;
    }
    /// <summary>Gets the integer reduction priority; larger values run first.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default zero for a bounded Area, -1 for a space.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public int AreaGetPriority(RID area) => AreaFieldState(area).Priority;

    private PhysicsAreaFields AreaFieldState(RID area, bool writing = false)
    {
        ThrowIfDisposed();
        lock (_registryGate)
            if (_sceneSpaces.TryGetValue(area, out var space))
            {
                space.EnsureQueryAccess();
                return space.DefaultAreaFields;
            }
        var runtime = AreaRuntime(area);
        runtime.EnsureAccess();
        var owners = runtime.Owners;
        if (writing) owners.Scene?.EnsureFieldChange();
        return owners.Scene?.Fields ?? owners.Server!.AreaFields!;
    }
}
