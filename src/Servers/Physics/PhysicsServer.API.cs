namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Enables or suspends subsequent physics-world steps process-wide.</summary>
    /// <param name="active">True advances locally active worlds; false retains their solver state without simulation.</param>
    /// <remarks>The server starts enabled. This atomic policy can change on any thread and takes effect at each
    /// world's next step boundary. An interval already running completes. Scene callbacks, timers, direct queries
    /// and explicit configuration remain available; skipped time is not accumulated.</remarks>
    public static void SetActive(bool active) => Service.SetActiveCore(active);

    /// <summary>Enables or suspends one live scene or caller-owned physics space.</summary>
    /// <param name="space">A live physics space RID.</param>
    /// <param name="active">Whether subsequent nonzero steps may advance this space.</param>
    /// <remarks>SpaceCreate starts inactive; the SceneTree activates its own world. Local policy survives global
    /// suspension. Queries and configuration work while inactive; reactivation consumes no skipped time.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a physics space.</exception>
    /// <exception cref="InvalidOperationException">The space is off-owner or currently solving.</exception>
    public static void SpaceSetActive(RID space, bool active) => Service.SpaceSetActiveCore(space, active);

    /// <summary>Returns a physics space's local activation policy.</summary>
    /// <param name="space">A live scene or caller-owned space RID.</param>
    /// <returns>The local flag, independent of the process-wide suspension policy.</returns>
    /// <exception cref="ArgumentException">The RID is stale or not a physics space.</exception>
    /// <exception cref="InvalidOperationException">The space is off-owner or currently solving.</exception>
    public static bool SpaceIsActive(RID space) => Service.SpaceIsActiveCore(space);

    /// <summary>Sets the gravity reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default Disabled.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetGravitySpaceOverride(RID area, Area.SpaceOverride value) => Service.AreaSetGravitySpaceOverrideCore(area, value);

    /// <summary>Gets the gravity reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default Disabled.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static Area.SpaceOverride AreaGetGravitySpaceOverride(RID area) => Service.AreaGetGravitySpaceOverrideCore(area);

    /// <summary>Sets signed gravity strength in scene units per second squared.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default 9.80665 for server-only Areas; 980 for scene Areas.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetGravity(RID area, float value) => Service.AreaSetGravityCore(area, value);

    /// <summary>Gets signed gravity strength in scene units per second squared.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default 9.80665 for server-only Areas; 980 for scene Areas.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public static float AreaGetGravity(RID area) => Service.AreaGetGravityCore(area);

    /// <summary>Sets the unnormalized direction or local point center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default (0, -1) for server-only Areas; (0, 1) for scene Areas.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetGravityVector(RID area, Vector2 value) => Service.AreaSetGravityVectorCore(area, value);

    /// <summary>Gets the unnormalized direction or local point center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default (0, -1) for server-only Areas; (0, 1) for scene Areas.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public static Vector2 AreaGetGravityVector(RID area) => Service.AreaGetGravityVectorCore(area);

    /// <summary>Sets whether the vector is a transformed attraction center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default false.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void AreaSetGravityPoint(RID area, bool value) => Service.AreaSetGravityPointCore(area, value);

    /// <summary>Gets whether the vector is a transformed attraction center.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default false.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static bool AreaGetGravityPoint(RID area) => Service.AreaGetGravityPointCore(area);

    /// <summary>Sets the unit distance of inverse-square point gravity; nonpositive means constant strength.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default zero.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetGravityPointUnitDistance(RID area, float value) => Service.AreaSetGravityPointUnitDistanceCore(area, value);

    /// <summary>Gets the unit distance of inverse-square point gravity; nonpositive means constant strength.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float AreaGetGravityPointUnitDistance(RID area) => Service.AreaGetGravityPointUnitDistanceCore(area);

    /// <summary>Sets the linear damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default Disabled.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetLinearDampSpaceOverride(RID area, Area.SpaceOverride value) => Service.AreaSetLinearDampSpaceOverrideCore(area, value);

    /// <summary>Gets the linear damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default Disabled.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static Area.SpaceOverride AreaGetLinearDampSpaceOverride(RID area) => Service.AreaGetLinearDampSpaceOverrideCore(area);

    /// <summary>Sets signed linear damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default 0.1.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetLinearDamp(RID area, float value) => Service.AreaSetLinearDampCore(area, value);

    /// <summary>Gets signed linear damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default 0.1.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public static float AreaGetLinearDamp(RID area) => Service.AreaGetLinearDampCore(area);

    /// <summary>Sets the angular damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default Disabled.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetAngularDampSpaceOverride(RID area, Area.SpaceOverride value) => Service.AreaSetAngularDampSpaceOverrideCore(area, value);

    /// <summary>Gets the angular damping reduction mode.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default Disabled.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static Area.SpaceOverride AreaGetAngularDampSpaceOverride(RID area) => Service.AreaGetAngularDampSpaceOverrideCore(area);

    /// <summary>Sets signed angular damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default one.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or the mode is undefined.</exception>
    public static void AreaSetAngularDamp(RID area, float value) => Service.AreaSetAngularDampCore(area, value);

    /// <summary>Gets signed angular damping in inverse seconds.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default one.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    /// <remarks>Space initial values are sampled from the corresponding ProjectSettings override when the world is created.</remarks>
    public static float AreaGetAngularDamp(RID area) => Service.AreaGetAngularDampCore(area);

    /// <summary>Sets the integer reduction priority; larger values run first.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <param name="value">The new value; default zero.</param>
    /// <remarks>Shares the scene owner's stored field state; a space addresses its default Area. Changes affect the next nonzero physics reduction,
    /// independently of monitor callbacks and monitorability; no overlap history is reset. Space defaults
    /// start from sampled ProjectSettings, with priority -1; their modes/priority are stored but do not gate the final fallback.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void AreaSetPriority(RID area, int value) => Service.AreaSetPriorityCore(area, value);

    /// <summary>Gets the integer reduction priority; larger values run first.</summary>
    /// <param name="area">A live scene/server Area RID or a space RID for its unbounded default field.</param>
    /// <returns>The stored value; default zero for a bounded Area, -1 for a space.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area or space.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static int AreaGetPriority(RID area) => Service.AreaGetPriorityCore(area);

    /// <summary>Sets or clears the receiver's body-pair overlap callback.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="callback">Status, other RID, object instance ID (zero for server-only), other logical shape and local shape; null clears.</param>
    /// <remarks>Registration resets pending pair history; current overlaps enter at the next nonzero scan.
    /// Scene-owned overlap snapshots/events remain mandatory and independent of this observer.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public static void AreaSetMonitorCallback(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback) => Service.AreaSetMonitorCallbackCore(area, callback);

    /// <summary>Sets or clears the receiver's monitorable-Area pair callback.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <param name="callback">Status, other RID, object instance ID, other logical shape index and local index; null clears.</param>
    /// <remarks>Replacing either callback resets both pair histories. Only monitorable other Areas enter this lane.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public static void AreaSetAreaMonitorCallback(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback) => Service.AreaSetAreaMonitorCallbackCore(area, callback);

    /// <summary>Sets the Area's directional body/Area detection mask.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <param name="mask">All accepted category bits; default one.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public static void AreaSetCollisionMask(RID area, uint mask) => Service.AreaSetCollisionMaskCore(area, mask);

    /// <summary>Gets the Area's 32 collision-layer bits.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <returns>Current categories; default one.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public static uint AreaGetCollisionLayer(RID area) => Service.AreaGetCollisionLayerCore(area);

    /// <summary>Gets the Area's directional overlap mask.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <returns>Current accepted category bits; default one.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public static uint AreaGetCollisionMask(RID area) => Service.AreaGetCollisionMaskCore(area);

    /// <summary>Gets the Area's current global scene-unit pose.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <returns>Stored scene or server translation and rotation.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public static Transform AreaGetTransform(RID area) => Service.AreaGetTransformCore(area);

    /// <summary>Gets a live owner-thread view of an attached body, or null while detached.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>A cached view tied to this backend attachment; caller disposal invalidates only that view.</returns>
    /// <remarks>Synchronizes the requested body's pending pose and geometry. Whole-space queries synchronize
    /// their world separately; acquiring body views does not scan other bodies.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the solver is stepping.</exception>
    public static PhysicsDirectBodyState? BodyGetDirectState(RID body) => Service.BodyGetDirectStateCore(body);

    /// <summary>Sets or clears the body's post-solver force-integration callback.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="callback">Owner-thread callback invoked before state synchronization, or null to clear.</param>
    /// <remarks>Scene bodies retain their tree-owned pose synchronization. Registration does not enable custom integration.</remarks>
    public static void BodySetForceIntegrationCallback(RID body, Action<PhysicsDirectBodyState>? callback) => Service.BodySetForceIntegrationCallbackCore(body, callback);

    /// <summary>Sets a post-solver force callback with strongly typed user data.</summary>
    /// <typeparam name="T">The user data type.</typeparam>
    /// <param name="body">A live body RID.</param>
    /// <param name="callback">Callback receiving the view and user data, or null to clear.</param>
    /// <param name="userData">Data retained with the registered callback.</param>
    /// <remarks>The adapter is allocated at registration; invocation does not box value-type data.</remarks>
    public static void BodySetForceIntegrationCallback<T>(RID body, Action<PhysicsDirectBodyState, T>? callback, T userData) => Service.BodySetForceIntegrationCallbackCore<T>(body, callback, userData);

    /// <summary>Sets or clears an owner-thread observer of the body's solved state.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="callback">Post-integration state callback, or null to clear.</param>
    /// <remarks>A previous user callback is replaced. Scene-owned pose synchronization remains mandatory.</remarks>
    public static void BodySetStateSyncCallback(RID body, Action<PhysicsDirectBodyState>? callback) => Service.BodySetStateSyncCallbackCore(body, callback);

    /// <summary>Enables or disables omission of automatic gravity, damping and accumulated forces.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="enable">True for manual force integration; impulses and solver contacts remain active.</param>
    public static void BodySetOmitForceIntegration(RID body, bool enable) => Service.BodySetOmitForceIntegrationCore(body, enable);

    /// <summary>Tests whether a body omits automatic force integration.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>The current custom-integration policy.</returns>
    public static bool BodyIsOmittingForceIntegration(RID body) => Service.BodyIsOmittingForceIntegrationCore(body);

    /// <summary>Sets the maximum retained contact-point count for a body.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="amount">A cap from zero through 4095; zero disables snapshots. Assignment clears the old point count.</param>
    /// <remarks>Contact reports include overlapping kinematic/static and kinematic/kinematic pairs without impulse response.
    /// Two static bodies do not create a pair. Enabling reports keeps a kinematic body active immediately;
    /// contact membership is refreshed on the next completed physics step.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The cap is outside zero through 4095.</exception>
    public static void BodySetMaxContactsReported(RID body, int amount) => Service.BodySetMaxContactsReportedCore(body, amount);

    /// <summary>Gets the body's configured contact-point limit.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>The configured limit, from zero through 4095; zero disables contact snapshots.</returns>
    public static int BodyGetMaxContactsReported(RID body) => Service.BodyGetMaxContactsReportedCore(body);

    /// <summary>Excludes two bodies from ordinary contact and motion tests when either body lists the other.</summary>
    /// <param name="body">The live scene or server body that owns the exception entry.</param>
    /// <param name="exceptedBody">Any RID value; an unresolvable target remains inert.</param>
    /// <exception cref="ArgumentException">The owner RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">The attached body is off-owner or its world is stepping.</exception>
    public static void BodyAddCollisionException(RID body, RID exceptedBody) => Service.BodyAddCollisionExceptionCore(body, exceptedBody);

    /// <summary>Removes one body-owned collision exception entry.</summary>
    /// <param name="body">The live scene or server body that owns the entry.</param>
    /// <param name="exceptedBody">The RID to remove; an absent entry is a no-op.</param>
    /// <exception cref="ArgumentException">The owner RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">The attached body is off-owner or its world is stepping.</exception>
    public static void BodyRemoveCollisionException(RID body, RID exceptedBody) => Service.BodyRemoveCollisionExceptionCore(body, exceptedBody);

    /// <summary>Applies a force for the next eligible fixed step without adding torque.</summary>
    /// <remarks>Pending input survives removal, static and dormant participation. Omission clears it on eligible integration.</remarks>
    /// <param name="body">A live scene or server body RID, including a detached body.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyApplyCentralForce(RID body, Vector2 force) => Service.BodyApplyCentralForceCore(body, force);

    /// <summary>Applies a positioned force for the next eligible fixed step.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Global-axis offset from body origin in scene units; zero by default.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyApplyForce(RID body, Vector2 force, Vector2 position = default) => Service.BodyApplyForceCore(body, force, position);

    /// <summary>Applies torque for the next eligible fixed step.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="torque">Finite kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyApplyTorque(RID body, float torque) => Service.BodyApplyTorqueCore(body, torque);

    /// <summary>Applies an instantaneous impulse without adding angular motion.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="impulse">Finite global scene units times kilograms per second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyApplyCentralImpulse(RID body, Vector2 impulse) => Service.BodyApplyCentralImpulseCore(body, impulse);

    /// <summary>Applies an instantaneous positioned impulse using the current mass profile.</summary>
    /// <remarks>Detached resource geometry resolves without creating a native body or world; static/kinematic inverse values ignore impulses.</remarks>
    /// <param name="body">A live body RID.</param>
    /// <param name="impulse">Finite global scene units times kilograms per second.</param>
    /// <param name="position">Global-axis body-origin offset in scene units; zero by default.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyApplyImpulse(RID body, Vector2 impulse, Vector2 position = default) => Service.BodyApplyImpulseCore(body, impulse, position);

    /// <summary>Applies an instantaneous angular impulse.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="impulse">Finite kilograms times squared scene units per second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyApplyTorqueImpulse(RID body, float impulse) => Service.BodyApplyTorqueImpulseCore(body, impulse);

    /// <summary>Adds a persistent central force without changing torque.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyAddConstantCentralForce(RID body, Vector2 force) => Service.BodyAddConstantCentralForceCore(body, force);

    /// <summary>Adds persistent force and its moment about the current center.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Global-axis body-origin offset; zero by default.</param>
    /// <remarks>The moment is captured at this call and persists independently of later center or pose changes.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyAddConstantForce(RID body, Vector2 force, Vector2 position = default) => Service.BodyAddConstantForceCore(body, force, position);

    /// <summary>Adds persistent torque without changing force.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="torque">Finite kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodyAddConstantTorque(RID body, float torque) => Service.BodyAddConstantTorqueCore(body, torque);

    /// <summary>Replaces the persistent global force.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite force; zero clears it without waking the body.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodySetConstantForce(RID body, Vector2 force) => Service.BodySetConstantForceCore(body, force);

    /// <summary>Returns the persistent global force in scene units.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Force in scene units times kilograms per squared second.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static Vector2 BodyGetConstantForce(RID body) => Service.BodyGetConstantForceCore(body);

    /// <summary>Replaces the persistent torque.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="torque">Finite torque; zero clears it without waking the body.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static void BodySetConstantTorque(RID body, float torque) => Service.BodySetConstantTorqueCore(body, torque);

    /// <summary>Returns persistent torque in scene units.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Kilograms times squared scene units per squared second.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public static float BodyGetConstantTorque(RID body) => Service.BodyGetConstantTorqueCore(body);

    /// <summary>Creates an empty caller-owned joint identity.</summary>
    /// <returns>A stable nonempty RID; FreeRID releases it.</returns>
    public static RID JointCreate() => Service.JointCreateCore();

    /// <summary>Clears the body connection while retaining the RID and collision policy.</summary>
    /// <param name="joint">A live scene-owned or caller-owned joint RID.</param>
    /// <remarks>A scene joint remains cleared until scene path/geometry edits or reentry configure it again.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static void JointClear(RID joint) => Service.JointClearCore(joint);

    /// <summary>Returns the current configured role, including Empty for an unconnected resource.</summary>
    /// <param name="joint">A live joint RID.</param>
    /// <returns>The configured role; identity does not change when the role is replaced.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static JointType JointGetType(RID joint) => Service.JointGetTypeCore(joint);

    /// <summary>Changes mutual contact suppression without changing the local anchors.</summary>
    /// <param name="joint">A live joint RID.</param>
    /// <param name="disable">Whether the connected pair omits body contacts.</param>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static void JointDisableCollisionsBetweenBodies(RID joint, bool disable) => Service.JointDisableCollisionsBetweenBodiesCore(joint, disable);

    /// <summary>Returns the joint's stored mutual collision policy.</summary>
    /// <param name="joint">A live joint RID.</param>
    /// <returns>True when mutual body contacts are suppressed.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static bool JointIsDisabledCollisionsBetweenBodies(RID joint) => Service.JointIsDisabledCollisionsBetweenBodiesCore(joint);

    /// <summary>Configures a pin at a finite global anchor.</summary>
    /// <param name="joint">A live joint RID; a scene owner must have the PinJoint role.</param>
    /// <param name="anchor">Global scene-unit pivot.</param>
    /// <param name="bodyA">First live body RID.</param>
    /// <param name="bodyB">Second live body RID, or empty to attach body A to the fixed world.</param>
    /// <remarks>Body-local frames are sampled once. Detached bodies connect when they later share a space.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void JointMakePin(RID joint, Vector2 anchor, RID bodyA, RID bodyB = default) => Service.JointMakePinCore(joint, anchor, bodyA, bodyB);

    /// <summary>Configures a finite body-A groove and a freely rotating body-B anchor.</summary>
    /// <param name="joint">A live joint RID; a scene owner must have the GrooveJoint role.</param>
    /// <param name="groove1A">First global groove endpoint in scene units.</param>
    /// <param name="groove2A">Second global endpoint; equal endpoints form a point constraint.</param>
    /// <param name="anchorB">Global point used to sample body B's local anchor.</param>
    /// <param name="bodyA">First live body RID; empty rejects.</param>
    /// <param name="bodyB">Second live body RID; empty rejects.</param>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void JointMakeGroove(RID joint, Vector2 groove1A, Vector2 groove2A, Vector2 anchorB,
        RID bodyA = default, RID bodyB = default) => Service.JointMakeGrooveCore(joint, groove1A, groove2A, anchorB, bodyA, bodyB);

    /// <summary>Configures spring force between two sampled body-local anchors.</summary>
    /// <param name="joint">A live joint RID; a scene owner must have the DampedSpringJoint role.</param>
    /// <param name="anchorA">First global anchor in scene units.</param>
    /// <param name="anchorB">Second global anchor in scene units.</param>
    /// <param name="bodyA">First live body RID.</param>
    /// <param name="bodyB">Second live body RID; empty rejects.</param>
    /// <remarks>Resets relaxed length to anchor separation, stiffness to 20 and damping to 1.5 kg/s.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void JointMakeDampedSpring(RID joint, Vector2 anchorA, Vector2 anchorB, RID bodyA, RID bodyB = default) => Service.JointMakeDampedSpringCore(joint, anchorA, anchorB, bodyA, bodyB);

    /// <summary>Gets the positional correction fraction.</summary>
    /// <param name="joint">A live joint RID, including an unconfigured joint.</param>
    /// <returns>A finite zero-to-one value; zero inherits the space default.</returns>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static float JointGetBias(RID joint) => Service.JointGetBiasCore(joint);

    /// <summary>Sets the positional correction fraction.</summary>
    /// <param name="joint">A live joint RID, including an unconfigured joint.</param>
    /// <param name="value">A finite zero-to-one value; zero inherits the space default.</param>
    /// <remarks>Preserves sampled anchors and identity, wakes connected bodies and clears old impulses. General settings survive clear and concrete-role replacement.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the documented finite range.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static void JointSetBias(RID joint, float value) => Service.JointSetBiasCore(joint, value);

    /// <summary>Gets the positional correction speed cap.</summary>
    /// <param name="joint">A live joint RID, including an unconfigured joint.</param>
    /// <returns>Nonnegative scene units/s for linear correction and radians/s for angular stops; float.MaxValue by default, additionally bounded by the world correction guard.</returns>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static float JointGetMaxBias(RID joint) => Service.JointGetMaxBiasCore(joint);

    /// <summary>Sets the positional correction speed cap.</summary>
    /// <param name="joint">A live joint RID, including an unconfigured joint.</param>
    /// <param name="value">Nonnegative scene units/s for linear correction and radians/s for angular stops; float.MaxValue by default, additionally bounded by the world correction guard.</param>
    /// <remarks>Preserves sampled anchors and identity, wakes connected bodies and clears old impulses. All linear axes share a vector cap; springs have no positional recovery rows. General settings survive clear and concrete-role replacement.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the documented finite range.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static void JointSetMaxBias(RID joint, float value) => Service.JointSetMaxBiasCore(joint, value);

    /// <summary>Gets the per-second impulse budget.</summary>
    /// <param name="joint">A live joint RID, including an unconfigured joint.</param>
    /// <returns>Nonnegative; float.MaxValue means unlimited. Linear and pure-angular channels use scene-unit and squared-scene-unit force units respectively.</returns>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static float JointGetMaxForce(RID joint) => Service.JointGetMaxForceCore(joint);

    /// <summary>Sets the per-second impulse budget.</summary>
    /// <param name="joint">A live joint RID, including an unconfigured joint.</param>
    /// <param name="value">Nonnegative; float.MaxValue means unlimited. Linear and pure-angular channels use scene-unit and squared-scene-unit force units respectively.</param>
    /// <remarks>Preserves sampled anchors and identity, wakes connected bodies and clears old impulses. Each substep permits the value times its duration; all linear axes share a vector budget. Springs cap their combined elastic and damping impulse. MotorMaxTorque remains an additional cap. General settings survive clear and concrete-role replacement.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the documented finite range.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static void JointSetMaxForce(RID joint, float value) => Service.JointSetMaxForceCore(joint, value);

    /// <summary>Gets the linear anchor compliance.</summary>
    /// <param name="joint">A live pin RID or unconfigured scene PinJoint RID.</param>
    /// <returns>Finite nonnegative inverse-kilogram softness, zero by default.</returns>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static float PinJointGetSoftness(RID joint) => Service.PinJointGetSoftnessCore(joint);

    /// <summary>Sets the linear anchor compliance.</summary>
    /// <param name="joint">A live pin RID or unconfigured scene PinJoint RID.</param>
    /// <param name="value">Finite nonnegative inverse-kilogram softness, zero by default.</param>
    /// <remarks>Preserves sampled anchors and identity, wakes connected bodies and clears old impulses. This affects linear anchors, not an angular spring. A raw concrete-role replacement resets pin-specific softness.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the documented finite range.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static void PinJointSetSoftness(RID joint, float value) => Service.PinJointSetSoftnessCore(joint, value);

    /// <summary>Gets the joint positional correction fraction.</summary>
    /// <param name="space">A live space RID.</param>
    /// <returns>A finite fraction from zero to one; initially sampled from ProjectSettings.Physics2DDefaultConstraintBias.</returns>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static float SpaceGetConstraintDefaultBias(RID space) => Service.SpaceGetConstraintDefaultBiasCore(space);

    /// <summary>Sets the joint positional correction fraction.</summary>
    /// <param name="space">A live space RID.</param>
    /// <param name="value">A finite fraction from zero to one; initially sampled from ProjectSettings.Physics2DDefaultConstraintBias.</param>
    /// <remarks>Updates and wakes zero-bias joints. Explicit nonzero joint bias is preserved; contact-separation bias is separate.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or has the wrong resource or joint role.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the documented finite range.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning world thread or phase.</exception>
    public static void SpaceSetConstraintDefaultBias(RID space, float value) => Service.SpaceSetConstraintDefaultBiasCore(space, value);

    /// <summary>Returns whether the relative angle is limited.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Whether the relative angle is limited.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static bool PinJointGetAngularLimitEnabled(RID joint) => Service.PinJointGetAngularLimitEnabledCore(joint);

    /// <summary>Changes whether the relative angle is limited.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Whether the relative angle is limited.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void PinJointSetAngularLimitEnabled(RID joint, bool value) => Service.PinJointSetAngularLimitEnabledCore(joint, value);

    /// <summary>Returns lower relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Lower relative angle in radians.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static float PinJointGetAngularLimitLower(RID joint) => Service.PinJointGetAngularLimitLowerCore(joint);

    /// <summary>Changes lower relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Lower relative angle in radians.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void PinJointSetAngularLimitLower(RID joint, float value) => Service.PinJointSetAngularLimitLowerCore(joint, value);

    /// <summary>Returns upper relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Upper relative angle in radians.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static float PinJointGetAngularLimitUpper(RID joint) => Service.PinJointGetAngularLimitUpperCore(joint);

    /// <summary>Changes upper relative angle in radians.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Upper relative angle in radians.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void PinJointSetAngularLimitUpper(RID joint, float value) => Service.PinJointSetAngularLimitUpperCore(joint, value);

    /// <summary>Returns whether the angular motor runs.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Whether the angular motor runs.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static bool PinJointGetMotorEnabled(RID joint) => Service.PinJointGetMotorEnabledCore(joint);

    /// <summary>Changes whether the angular motor runs.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Whether the angular motor runs.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static void PinJointSetMotorEnabled(RID joint, bool value) => Service.PinJointSetMotorEnabledCore(joint, value);

    /// <summary>Returns desired relative radians per second.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Desired relative radians per second.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static float PinJointGetMotorTargetVelocity(RID joint) => Service.PinJointGetMotorTargetVelocityCore(joint);

    /// <summary>Changes desired relative radians per second.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Desired relative radians per second.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void PinJointSetMotorTargetVelocity(RID joint, float value) => Service.PinJointSetMotorTargetVelocityCore(joint, value);

    /// <summary>Returns finite nonnegative torque cap in newton-meters.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <returns>Finite nonnegative torque cap in newton-meters.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static float PinJointGetMotorMaxTorque(RID joint) => Service.PinJointGetMotorMaxTorqueCore(joint);

    /// <summary>Changes finite nonnegative torque cap in newton-meters.</summary>
    /// <param name="joint">A live Pin joint RID.</param>
    /// <param name="value">Finite nonnegative torque cap in newton-meters.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void PinJointSetMotorMaxTorque(RID joint, float value) => Service.PinJointSetMotorMaxTorqueCore(joint, value);

    /// <summary>Returns nonnegative relaxed separation in scene units; server zero is literal.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <returns>Nonnegative relaxed separation in scene units; server zero is literal.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static float DampedSpringJointGetRestLength(RID joint) => Service.DampedSpringJointGetRestLengthCore(joint);

    /// <summary>Changes nonnegative relaxed separation in scene units; server zero is literal.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <param name="value">Nonnegative relaxed separation in scene units; server zero is literal.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void DampedSpringJointSetRestLength(RID joint, float value) => Service.DampedSpringJointSetRestLengthCore(joint, value);

    /// <summary>Returns nonnegative Hooke coefficient in kilograms per second squared.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <returns>Nonnegative Hooke coefficient in kilograms per second squared.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static float DampedSpringJointGetStiffness(RID joint) => Service.DampedSpringJointGetStiffnessCore(joint);

    /// <summary>Changes nonnegative Hooke coefficient in kilograms per second squared.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <param name="value">Nonnegative Hooke coefficient in kilograms per second squared.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void DampedSpringJointSetStiffness(RID joint, float value) => Service.DampedSpringJointSetStiffnessCore(joint, value);

    /// <summary>Returns nonnegative axial drag coefficient in kilograms per second.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <returns>Nonnegative axial drag coefficient in kilograms per second.</returns>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    public static float DampedSpringJointGetDamping(RID joint) => Service.DampedSpringJointGetDampingCore(joint);

    /// <summary>Changes nonnegative axial drag coefficient in kilograms per second.</summary>
    /// <param name="joint">A live DampedSpring joint RID.</param>
    /// <param name="value">Nonnegative axial drag coefficient in kilograms per second.</param>
    /// <remarks>Updates the shared scene/server settings and active backend before the next solve.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has the wrong kind or lacks the requested joint role.</exception>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping, or a scene joint would change its concrete role/world.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numeric input is nonfinite, outside the backend extent, or violates the active physical parameter range.</exception>
    public static void DampedSpringJointSetDamping(RID joint, float value) => Service.DampedSpringJointSetDampingCore(joint, value);

    /// <summary>Sets a body's positive finite mass in kilograms.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="mass">Positive kilograms within the finite solver range; default is one.</param>
    /// <remarks>Retains center and inertia policy. Detached configuration is applied on attachment.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mass or resulting geometry is outside the solver range.</exception>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetMass(RID body, float mass) => Service.BodySetMassCore(body, mass);

    /// <summary>Gets the body's configured mass in kilograms.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>Configured positive mass, including while static, kinematic or detached.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float BodyGetMass(RID body) => Service.BodyGetMassCore(body);

    /// <summary>Sets rotational inertia in kilograms times squared scene units.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="inertia">Zero selects geometry-derived inertia; positive values override it.</param>
    /// <exception cref="ArgumentOutOfRangeException">The inertia is negative, nonfinite or outside the solver range.</exception>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetInertia(RID body, float inertia) => Service.BodySetInertiaCore(body, inertia);

    /// <summary>Gets configured or most recently resolved rotational inertia in scene units.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>An explicit override or geometry-derived kilograms times squared scene units; zero before an automatic profile is first resolved by attachment or a geometry-dependent force call.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float BodyGetInertia(RID body) => Service.BodyGetInertiaCore(body);

    /// <summary>Sets a custom center of mass relative to body origin in local scene units.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="center">Finite local offset within the solver range.</param>
    /// <remarks>Scene RigidBody switches to Custom mode. The profile is committed before property-list callbacks.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The center or resulting inertia exceeds the finite solver range.</exception>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetCenterOfMass(RID body, Vector2 center) => Service.BodySetCenterOfMassCore(body, center);

    /// <summary>Gets the configured or most recently resolved local center of mass.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>Local scene-unit center, or zero before automatic geometry is first resolved.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static Vector2 BodyGetCenterOfMass(RID body) => Service.BodyGetCenterOfMassCore(body);

    /// <summary>Restores automatic center and inertia while retaining configured body mass.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <remarks>Scene RigidBody stored Inertia becomes zero and CenterOfMassMode becomes Auto with a zero stored center.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodyResetMassProperties(RID body) => Service.BodyResetMassPropertiesCore(body);

    /// <summary>Tests a live body's shapes through their owning space without moving that body.</summary>
    /// <param name="body">A scene or server-created body RID.</param>
    /// <param name="parameters">Global starting pose, motion, margin and exclusions.</param>
    /// <param name="result">Optional caller-owned result updated after a successful test.</param>
    /// <returns>Whether motion or requested recovery reached a body contact.</returns>
    public static bool BodyTestMotion(RID body, PhysicsTestMotionParameters parameters,
        PhysicsTestMotionResult? result = null) => Service.BodyTestMotionCore(body, parameters, result);

    /// <summary>Sets signed friction; negative values project rough-surface precedence.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="friction">Finite signed coefficient; default one.</param>
    /// <remarks>A scene override belongs to this body and does not mutate a borrowed PhysicsMaterial.
    /// Material assignment/revision reload replaces this raw override.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetFriction(RID body, float friction) => Service.BodySetFrictionCore(body, friction);

    /// <summary>Gets effective signed friction, including a scene material projection.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed coefficient; default one.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float BodyGetFriction(RID body) => Service.BodyGetFrictionCore(body);

    /// <summary>Sets signed restitution; negative values project absorbent-surface subtraction.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="bounce">Finite signed coefficient; default zero.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetBounce(RID body, float bounce) => Service.BodySetBounceCore(body, bounce);

    /// <summary>Gets effective signed restitution, including the scene material projection.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed coefficient; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float BodyGetBounce(RID body) => Service.BodyGetBounceCore(body);

    /// <summary>Sets the signed multiplier of resolved Area/world gravity.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="scale">Finite signed scale; default one.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetGravityScale(RID body, float scale) => Service.BodySetGravityScaleCore(body, scale);

    /// <summary>Gets the body's configured gravity multiplier.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed multiplier; default one.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float BodyGetGravityScale(RID body) => Service.BodyGetGravityScaleCore(body);

    /// <summary>Sets signed linear damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="damp">Finite signed damping; default zero.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetLinearDamp(RID body, float damp) => Service.BodySetLinearDampCore(body, damp);

    /// <summary>Gets configured linear damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed value; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float BodyGetLinearDamp(RID body) => Service.BodyGetLinearDampCore(body);

    /// <summary>Sets signed angular damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="damp">Finite signed damping; default zero.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetAngularDamp(RID body, float damp) => Service.BodySetAngularDampCore(body, damp);

    /// <summary>Gets configured angular damping in inverse seconds.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Finite signed value; default zero.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static float BodyGetAngularDamp(RID body) => Service.BodyGetAngularDampCore(body);

    /// <summary>Chooses whether body linear damping combines with or replaces selected fields.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="mode">Combine/default or Replace.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetLinearDampMode(RID body, RigidBody.DampMode mode) => Service.BodySetLinearDampModeCore(body, mode);

    /// <summary>Gets the linear damping combination policy.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Combine/default or Replace.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static RigidBody.DampMode BodyGetLinearDampMode(RID body) => Service.BodyGetLinearDampModeCore(body);

    /// <summary>Chooses whether body angular damping combines with or replaces selected fields.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="mode">Combine/default or Replace.</param>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static void BodySetAngularDampMode(RID body, RigidBody.DampMode mode) => Service.BodySetAngularDampModeCore(body, mode);

    /// <summary>Gets the angular damping combination policy.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Combine/default or Replace.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or during solver ownership.</exception>
    public static RigidBody.DampMode BodyGetAngularDampMode(RID body) => Service.BodyGetAngularDampModeCore(body);

    /// <summary>Creates an inactive physics space independent of any scene tree.</summary>
    /// <returns>A caller-owned space RID; activate it with SpaceSetActive before advancing simulation.</returns>
    public static RID SpaceCreate() => Service.SpaceCreateCore();

    /// <summary>Advances one explicitly created space by a finite nonnegative fixed delta.</summary>
    /// <param name="space">A caller-owned space RID.</param>
    /// <param name="delta">Elapsed seconds; zero or an inactive space/server leaves solver state unchanged.</param>
    /// <exception cref="ArgumentOutOfRangeException">Delta is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the space owner thread or the world is stepping.</exception>
    public static void SpaceStep(RID space, double delta) => Service.SpaceStepCore(space, delta);

    /// <summary>Creates a detached rigid body with layer and mask one.</summary>
    /// <returns>A caller-owned body RID.</returns>
    public static RID BodyCreate() => Service.BodyCreateCore();

    /// <summary>Creates a detached sensor Area with layer and mask one.</summary>
    /// <returns>A caller-owned Area RID.</returns>
    public static RID AreaCreate() => Service.AreaCreateCore();

    /// <summary>Creates a caller-owned circle shape with its default geometry.</summary>
    /// <returns>A live circle-shape RID.</returns>
    public static RID CircleShapeCreate() => Service.CircleShapeCreateCore();

    /// <summary>Creates a caller-owned rectangle shape with its default geometry.</summary>
    /// <returns>A live rectangle-shape RID.</returns>
    public static RID RectangleShapeCreate() => Service.RectangleShapeCreateCore();

    /// <summary>Creates a caller-owned capsule shape with its default geometry.</summary>
    /// <returns>A live capsule-shape RID.</returns>
    public static RID CapsuleShapeCreate() => Service.CapsuleShapeCreateCore();

    /// <summary>Creates a caller-owned segment shape with its default geometry.</summary>
    /// <returns>A live segment-shape RID.</returns>
    public static RID SegmentShapeCreate() => Service.SegmentShapeCreateCore();

    /// <summary>Creates a caller-owned twenty-unit directed separation ray.</summary>
    /// <returns>A live separation-ray shape RID.</returns>
    public static RID SeparationRayShapeCreate() => Service.SeparationRayShapeCreateCore();

    /// <summary>Creates an owned infinite half-plane shape, upward normal and zero distance by default.</summary>
    /// <returns>A shape RID released with FreeRID; typed data can be copied through ShapeSetData.</returns>
    public static RID WorldBoundaryShapeCreate() => Service.WorldBoundaryShapeCreateCore();

    /// <summary>Returns the concrete geometry type of an owned or borrowed shape RID.</summary>
    /// <param name="shape">A live physics shape RID.</param>
    /// <returns>The geometry type, independent of attached bodies and their current backend.</returns>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a shape.</exception>
    public static ShapeType ShapeGetType(RID shape) => Service.ShapeGetTypeCore(shape);

    /// <summary>Creates a caller-owned empty convex polygon shape.</summary>
    /// <returns>A live convex-polygon-shape RID.</returns>
    public static RID ConvexPolygonShapeCreate() => Service.ConvexPolygonShapeCreateCore();

    /// <summary>Creates a caller-owned empty paired-segment shape.</summary>
    /// <returns>A live concave-polygon-shape RID.</returns>
    public static RID ConcavePolygonShapeCreate() => Service.ConcavePolygonShapeCreateCore();

    /// <summary>Copies typed geometry into a server-owned shape RID.</summary>
    /// <param name="shape">A live server shape of the same concrete type as the supplied resource.</param>
    /// <param name="data">Caller-owned source geometry; later edits do not affect the server copy.</param>
    /// <exception cref="ArgumentException">The RID is not a live shape or the resource has another concrete type.</exception>
    /// <exception cref="ObjectDisposedException">The supplied resource is disposed.</exception>
    public static void ShapeSetData(RID shape, Shape data) => Service.ShapeSetDataCore(shape, data);

    /// <summary>Returns a caller-owned duplicate of server shape geometry.</summary>
    /// <param name="shape">A live server shape RID.</param>
    /// <returns>An independent caller-owned Shape resource.</returns>
    public static Shape ShapeGetData(RID shape) => Service.ShapeGetDataCore(shape);

    /// <summary>Adds a typed server shape to a body as one indexed owner slot.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="shape">A live server shape RID.</param>
    /// <param name="transform">Finite local pose, or null for identity.</param>
    /// <param name="disabled">Whether this slot initially contributes no fixtures.</param>
    public static void BodyAddShape(RID body, RID shape, Transform? transform = null, bool disabled = false) => Service.BodyAddShapeCore(body, shape, transform, disabled);

    /// <summary>Adds a typed server shape to an Area sensor as one indexed owner slot.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="shape">A live server shape RID.</param>
    /// <param name="transform">Finite local pose, or null for identity.</param>
    /// <param name="disabled">Whether this slot initially contributes no fixtures.</param>
    public static void AreaAddShape(RID area, RID shape, Transform? transform = null, bool disabled = false) => Service.AreaAddShapeCore(area, shape, transform, disabled);

    /// <summary>Gets the number of indexed shape slots on a body, including disabled slots.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>The current slot count.</returns>
    public static int BodyGetShapeCount(RID body) => Service.BodyGetShapeCountCore(body);

    /// <summary>Gets the number of indexed shape slots on an Area, including disabled slots.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <returns>The current slot count.</returns>
    public static int AreaGetShapeCount(RID area) => Service.AreaGetShapeCountCore(area);

    /// <summary>Enables or disables one indexed body shape slot.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    /// <param name="disabled">Whether the slot contributes no fixtures.</param>
    public static void BodySetShapeDisabled(RID body, int index, bool disabled) => Service.BodySetShapeDisabledCore(body, index, disabled);

    /// <summary>Enables or disables one indexed Area shape slot.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    /// <param name="disabled">Whether the slot contributes no sensor fixtures.</param>
    public static void AreaSetShapeDisabled(RID area, int index, bool disabled) => Service.AreaSetShapeDisabledCore(area, index, disabled);

    /// <summary>Removes one indexed body shape slot and its fixtures.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    public static void BodyRemoveShape(RID body, int index) => Service.BodyRemoveShapeCore(body, index);

    /// <summary>Removes one indexed Area shape slot and its fixtures.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based shape-owner slot index.</param>
    public static void AreaRemoveShape(RID area, int index) => Service.AreaRemoveShapeCore(area, index);

    /// <summary>Moves a body into a live space, or detaches it with an empty RID.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="space">A live space RID, or default to detach.</param>
    public static void BodySetSpace(RID body, RID space) => Service.BodySetSpaceCore(body, space);

    /// <summary>Moves an Area into a live space, or detaches it with an empty RID.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <param name="space">A live space RID, or default to detach.</param>
    public static void AreaSetSpace(RID area, RID space) => Service.AreaSetSpaceCore(area, space);

    /// <summary>Gets the current body space, or an empty RID while detached.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <returns>The current space identity or default.</returns>
    public static RID BodyGetSpace(RID body) => Service.BodyGetSpaceCore(body);

    /// <summary>Gets the current Area space, or an empty RID while detached.</summary>
    /// <param name="area">A live server Area RID.</param>
    /// <returns>The current space identity or default.</returns>
    public static RID AreaGetSpace(RID area) => Service.AreaGetSpaceCore(area);

    /// <summary>Changes a body's translation and rotation in scene units.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="transform">Finite global pose with unit scale and zero skew.</param>
    /// <remarks>Server kinematic targets after the first pose take effect on the next nonzero active step.
    /// Scene synchronization policy is retained. Moving a static support wakes touching bodies.</remarks>
    /// <exception cref="ArgumentException">The pose is invalid or the RID is not a live body.</exception>
    public static void BodySetTransform(RID body, Transform transform) => Service.BodySetTransformCore(body, transform);

    /// <summary>Changes an Area's translation and rotation in scene units.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="transform">Finite global pose with unit scale and zero skew.</param>
    public static void AreaSetTransform(RID area, Transform transform) => Service.AreaSetTransformCore(area, transform);

    /// <summary>Returns current scene presentation or the server body's current solver transform.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>The current scene-unit pose, including solved dynamic movement.</returns>
    public static Transform BodyGetTransform(RID body) => Service.BodyGetTransformCore(body);

    /// <summary>Sets finite linear velocity, or virtual surface velocity for a static or kinematic body.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="velocity">Finite global scene units per second.</param>
    public static void BodySetLinearVelocity(RID body, Vector2 velocity) => Service.BodySetLinearVelocityCore(body, velocity);

    /// <summary>Gets current linear contact velocity, including virtual surface and completed target motion.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>Global scene units per second; detached bodies return retained configuration.</returns>
    public static Vector2 BodyGetLinearVelocity(RID body) => Service.BodyGetLinearVelocityCore(body);

    /// <summary>Sets finite angular velocity, or virtual surface rotation for a static or kinematic body.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="velocity">Finite radians per second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The velocity is nonfinite.</exception>
    public static void BodySetAngularVelocity(RID body, float velocity) => Service.BodySetAngularVelocityCore(body, velocity);

    /// <summary>Gets current angular contact velocity, including virtual surface and completed target motion.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>Radians per second; detached bodies return retained configuration.</returns>
    public static float BodyGetAngularVelocity(RID body) => Service.BodyGetAngularVelocityCore(body);

    /// <summary>Sets dynamic sleep state; sleeping clears velocity and waking retains pending forces.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="sleeping">True to sleep; false to wake. Static and kinematic roles ignore this assignment.</param>
    public static void BodySetSleeping(RID body, bool sleeping) => Service.BodySetSleepingCore(body, sleeping);

    /// <summary>Gets whether the current body is inactive in the solver.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>The live or retained sleep state; static bodies are inactive.</returns>
    public static bool BodyGetSleeping(RID body) => Service.BodyGetSleepingCore(body);

    /// <summary>Sets automatic sleep permission, waking a dynamic body when disabled.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="canSleep">Whether an idle dynamic body may sleep.</param>
    public static void BodySetCanSleep(RID body, bool canSleep) => Service.BodySetCanSleepCore(body, canSleep);

    /// <summary>Gets retained automatic sleep permission.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>True by default; explicit sleep assignments remain available when false.</returns>
    public static bool BodyGetCanSleep(RID body) => Service.BodyGetCanSleepCore(body);

    /// <summary>Replaces linear velocity along the supplied axis, preserving its perpendicular component.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="axisVelocity">Finite direction and magnitude in scene units per second; zero retains current velocity.</param>
    /// <exception cref="ArgumentOutOfRangeException">The input or resulting velocity is nonfinite.</exception>
    public static void BodySetAxisVelocity(RID body, Vector2 axisVelocity) => Service.BodySetAxisVelocityCore(body, axisVelocity);

    /// <summary>Changes a body among static, kinematic and dynamic modes.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <param name="mode">One of the four declared body modes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    public static void BodySetMode(RID body, BodyMode mode) => Service.BodySetModeCore(body, mode);

    /// <summary>Gets the body's current motion mode.</summary>
    /// <param name="body">A live server body RID.</param>
    /// <returns>The current mode.</returns>
    public static BodyMode BodyGetMode(RID body) => Service.BodyGetModeCore(body);

    /// <summary>Sets a body's 32 collision-layer bits without replacing its geometry.</summary>
    /// <param name="body">A live scene or server body RID, including a generated tile body.</param>
    /// <param name="layer">All layer bits, including zero and bit 32.</param>
    /// <remarks>Assignments wake the body and its contact neighbors, even for unchanged bits. Queries observe changes immediately; contacts and overlap events update on the next step.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">An attached world is accessed off-thread, stepping or failed.</exception>
    public static void BodySetCollisionLayer(RID body, uint layer) => Service.BodySetCollisionLayerCore(body, layer);

    /// <summary>Gets a body's complete 32-bit collision-layer mask.</summary>
    /// <param name="body">A live scene or server body RID, including a generated tile body.</param>
    /// <returns>The authored category bits, including while detached or without active shapes.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">An attached world is accessed off-thread, stepping or failed.</exception>
    public static uint BodyGetCollisionLayer(RID body) => Service.BodyGetCollisionLayerCore(body);

    /// <summary>Sets a body's 32 collision-mask bits without replacing its geometry.</summary>
    /// <param name="body">A live scene or server body RID, including a generated tile body.</param>
    /// <param name="mask">All accepted category bits, including zero and bit 32.</param>
    /// <remarks>Assignments wake the body and its contact neighbors, even for unchanged bits. Motion queries observe changes immediately; contacts and overlap events update on the next step.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">An attached world is accessed off-thread, stepping or failed.</exception>
    public static void BodySetCollisionMask(RID body, uint mask) => Service.BodySetCollisionMaskCore(body, mask);

    /// <summary>Gets a body's complete 32-bit collision-mask value.</summary>
    /// <param name="body">A live scene or server body RID, including a generated tile body.</param>
    /// <returns>The authored accepted-category bits, including while detached or without active shapes.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">An attached world is accessed off-thread, stepping or failed.</exception>
    public static uint BodyGetCollisionMask(RID body) => Service.BodyGetCollisionMaskCore(body);

    /// <summary>Sets an Area's 32 collision-layer bits.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="layer">All accepted layer bits, including zero and bit 32.</param>
    public static void AreaSetCollisionLayer(RID area, uint layer) => Service.AreaSetCollisionLayerCore(area, layer);

    /// <summary>Sets whether scene monitoring Areas may detect a server-created Area.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="monitorable">The sensing policy; server Areas default false.</param>
    /// <remarks>The next nonzero overlap scan adopts the policy. Attached changes require the space owner thread.</remarks>
    public static void AreaSetMonitorable(RID area, bool monitorable) => Service.AreaSetMonitorableCore(area, monitorable);

    /// <summary>Frees a server-owned space, collider, shape or joint RID.</summary>
    /// <param name="rid">A live caller-owned server resource identity.</param>
    /// <remarks>Scene-owned spaces, collision objects and joints are released by their scene owners.
    /// Freeing a body clears dependent joint connections before removing its identity.
    /// After a backend failure, colliders and joints can still be released; their raw storage is reclaimed
    /// with the failed space without traversing partially published simulation graphs. Space release rejects
    /// during live physics callbacks. Once cleanup starts, all owned participants and the selected solver are
    /// attempted; the space RID and direct view are unregistered even when cleanup reports aggregated failures.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or has no server-owned resource.</exception>
    /// <exception cref="InvalidOperationException">The RID belongs to a scene owner or the space is being stepped or dispatching callbacks.</exception>
    /// <exception cref="AggregateException">Space cleanup encountered errors after attempting all owned resources.</exception>
    public static void FreeRID(RID rid) => Service.FreeRIDCore(rid);

    /// <summary>Returns the current shape resource RID at a body's global logical index.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot, including disabled slots.</param>
    /// <returns>The borrowed shape RID, or empty for a retained disposed resource slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not a body.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static RID BodyGetShape(RID body, int index) => Service.BodyGetShapeCore(body, index);

    /// <summary>Returns the current shape resource RID at an Area's global logical index.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot, including disabled slots.</param>
    /// <returns>The borrowed shape RID, or empty for a retained disposed resource slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not an Area.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static RID AreaGetShape(RID area, int index) => Service.AreaGetShapeCore(area, index);

    /// <summary>Returns a body's effective slot-local translation and rotation.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <returns>The configured local pose, including a raw per-slot override.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not a body.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static Transform BodyGetShapeTransform(RID body, int index) => Service.BodyGetShapeTransformCore(body, index);

    /// <summary>Returns an Area's effective slot-local translation and rotation.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <returns>The configured local pose, including a raw per-slot override.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not an Area.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static Transform AreaGetShapeTransform(RID area, int index) => Service.AreaGetShapeTransformCore(area, index);

    /// <summary>Replaces one body's slot resource while retaining its index and local policies.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="shape">A live owned or borrowed shape RID.</param>
    /// <remarks>The collider borrows geometry; it does not free either resource. Child node properties are unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">An owner or shape RID is stale or has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static void BodySetShape(RID body, int index, RID shape) => Service.BodySetShapeCore(body, index, shape);

    /// <summary>Replaces one Area's slot resource while retaining its index and local policies.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="shape">A live owned or borrowed shape RID.</param>
    /// <remarks>The collider borrows geometry; it does not free either resource. Child node properties are unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">An owner or shape RID is stale or has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static void AreaSetShape(RID area, int index, RID shape) => Service.AreaSetShapeCore(area, index, shape);

    /// <summary>Changes one body slot's local pose without moving the body or other slots.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="transform">Finite translation/rotation with unit scale and zero skew.</param>
    /// <remarks>A later group or child transform edit replaces its slot overrides.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The pose is nonfinite/scaled/skewed or the RID has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static void BodySetShapeTransform(RID body, int index, Transform transform) => Service.BodySetShapeTransformCore(body, index, transform);

    /// <summary>Changes one Area slot's local pose without moving the Area or other slots.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="transform">Finite translation/rotation with unit scale and zero skew.</param>
    /// <remarks>A later group or child transform edit replaces its slot overrides.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The pose is nonfinite/scaled/skewed or the RID has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static void AreaSetShapeTransform(RID area, int index, Transform transform) => Service.AreaSetShapeTransformCore(area, index, transform);

    /// <summary>Removes all body slots and fixtures without freeing their shape resources.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <remarks>Scene owner identities and child nodes remain; a later child resource edit can rebuild its group.</remarks>
    /// <exception cref="ArgumentException">The owner RID is stale or not a body.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static void BodyClearShapes(RID body) => Service.BodyClearShapesCore(body);

    /// <summary>Removes all Area slots and sensor fixtures without freeing their shape resources.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <remarks>Scene owner identities and child nodes remain; a later child resource edit can rebuild its group.</remarks>
    /// <exception cref="ArgumentException">The owner RID is stale or not an Area.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static void AreaClearShapes(RID area) => Service.AreaClearShapesCore(area);

    /// <summary>Changes one body slot's one-way policy for contacts and body motion tests.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="enable">Whether the slot permits directional pass-through.</param>
    /// <param name="margin">Finite nonnegative recovery depth in scene units.</param>
    /// <param name="direction">Finite slot-local direction, normalized once; null selects downward and zero remains zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent or margin is invalid.</exception>
    /// <exception cref="ArgumentException">Direction is nonfinite, or the RID is stale/wrong-kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public static void BodySetShapeAsOneWayCollision(RID body, int index, bool enable, float margin, Vector2? direction = null) => Service.BodySetShapeAsOneWayCollisionCore(body, index, enable, margin, direction);

    /// <summary>Returns a live direct-query view of a physics space.</summary>
    /// <param name="space">A live space RID.</param>
    /// <returns>The cached query view, or a fresh view if a caller disposed the previous one.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live physics space.</exception>
    public static PhysicsDirectSpaceState SpaceGetDirectState(RID space) => Service.SpaceGetDirectStateCore(space);

}
