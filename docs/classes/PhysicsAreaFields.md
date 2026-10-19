# PhysicsAreaFields

Last updated: 2026-10-19

[Complete Area parameter operations](../components/physics-areas.md#complete-area-parameter-operations) now have a public-only component, lifetime, selected-preparation failure and warmed complete-frame audit across CPU/GPU/registered implementations. The engine-owned ten-field contract preserves independent defaults, prepared reads and retry without replaying Solve; broader numerical/native/network/performance acceptance remains open.

**Declaration:** `internal sealed class PhysicsAreaFields` · **Source:** [PhysicsAreaFields.cs](../../src/Servers/Physics/PhysicsAreaFields.cs)

Shared storage for ten typed gravity/damping/priority capabilities. A scene Area owns a profile initialized with 980 and (0, 1); a server collider Area owns one initialized with 9.80665 and (0, -1). Space default profiles sample ProjectSettings and use priority -1. Profiles have no RID or backend handles and live with their owning Area/space; no public construction/API is exposed.

Internal fields hold three Area.SpaceOverride modes, signed finite strength/vector/damping, point flag/unit distance and integer priority. ComputeGravity uses the supplied global pose for point centers and leaves directional gravity unnormalized. HasOverrides selects bounded reducer entries; space defaults form the final unbounded fallback. Validation helpers are shared by scene descriptors and typed server setters. Owners enforce lifetime, thread/phase/capture access before writes; PhysicsSpace validates computed per-body fields/motion and reuses sorted tuple scratch storage.

[PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) and existing [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) verify both profiles, reduction, projection, errors and warmed zero managed allocation. Native/platform/owner acceptance limits remain in [ADR 0056](../decisions/physics-fields.md#adr-0056).

## Selected checkpoint ownership

Configuration samples complete immutable [PhysicsAreaFieldConfiguration](PhysicsAreaFieldConfiguration.md) values for replay/portable schema validation, without solver storage.

The resident point-field reducer preserves guarded float operation order with GLSL precise intermediates and propagates reconstruction validity separately from the value. Values beyond the finite output range do not enter subsequent arithmetic as infinity sentinels. A later Replace/ReplaceCombine contribution resets validity with the channel; Combine retains an invalid earlier contribution. Final invalid fields fail before body-field publication or solver motion through the existing eight-byte status read and terminal GPU-world policy. Mantissa/exponent normalization avoids reciprocals outside the normal range for large/tiny coordinates. No CPU mirror, buffer growth, new readback or wait is introduced. [Hardware verification](../components/physics-areas.md#hardware-point-field-guards-2026-10-14).
