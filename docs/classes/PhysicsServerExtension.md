# PhysicsServerExtension

Last updated: 2026-10-11

Caller-derived scoped numerical implementation base inheriting the full protected PhysicsServerBackend family. Implement real body, joint, geometry, query, observation and history operations; the world supplies engine identities and owns lifetime.

See [registered backend contract](../components/physics-registered-adapter.md) for ownership, construction, verification and remaining acceptance.

The [complete force and axis family](../components/physics-registered-adapter.md#force-impulse-and-axis-response) uses common validated scene/raw authoring and selected numerical hooks. Shared CPU/GPU/registered public-only checks exercise actual response, lifetime, invalid inputs and 128 edited frames after collection at zero managed allocation. Full numerical-extension acceptance remains open.
