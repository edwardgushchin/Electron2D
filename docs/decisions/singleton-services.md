# Process-wide service API decisions

Last updated: 2026-10-05

This document owns public access to process-wide engine services. Owning domain decisions continue to define state, lifetime, threading and resource ownership.

<a id="adr-0095"></a>
## ADR 0095: Static public operations over retained service objects

### Status

Accepted.

### Context

`Engine.Instance.Run(window)` repeats an access step for a service that is already process-wide. Engine, ProjectSettings, Input, InputMap, ThemeDB, AudioServer, PhysicsServer, ResourceLoader, ResourceSaver and ResourceUID retain permanent objects; DisplayServer and RenderingServer retain objects only during an active native session. Those objects also support identity, typed property discovery, service lookup and native resource ownership.

The selected convention shortens ordinary calls to `Engine.Run(window)` across the family while preserving the stateful implementations.

### Decision

- Public service operations, properties and events are static delegates to the retained internal object. No public `Instance` accessor or parallel declared instance-operation alias remains on a service type. Existing static constants, settings, nested types and pure helpers keep their identities.
- The twelve public service types remain sealed object types with private constructors. ProjectSettings derives from ProjectSettingsRegistry so its permanent object retains identity and property descriptors; the separate registry owns the existing state implementation and exposes instance operations for explicit registry contexts, including independent tools and unopened projects. Typed lookup can still inspect the runtime object through its registry base. Fields, locks, owner-thread checks, property descriptors, identity and existing named-service registrations retain their object semantics. Internal implementation operations are not exported.
- Permanent services retain construction timing and disposal policy. Static access neither creates an independent runtime nor changes callback thread safety or initialization order.
- DisplayServer.IsAvailable and RenderingServer.IsAvailable report whether an active object is published. Static operations and event subscription changes require an active service and throw InvalidOperationException when absent. Availability is observational: it neither reserves a session nor permits off-owner operations.
- Native services keep deterministic teardown. DisplayServer.Open still returns the caller-owned object used to close an explicitly opened display; Engine.Run owns its ordinary window/rendering session. Holding an object does not create a second public operations surface. Internal resource owners keep addressing their exact object so an old resource cannot silently bind to a later session.
- Permanent events retain their subscription lifetime. Native event subscriptions belong to the active object: subscribe and unsubscribe during that session. Cleanup does not transfer subscriptions to a later session.
- Existing typed object inspection and named-service lookup remain available. SettingsChanged uses ProjectSettingsRegistry as its sender type; the permanent sender is still the same ProjectSettings object. They do not authorize instance calls to static operations.
- Migrate runtime integration, consumers, tests, XML, current documents and coverage together. This deliberately changes source and binary API; isolated construction migrates from new ProjectSettings(...) to new ProjectSettingsRegistry(...); do not add obsolete aliases.

### Consequences

Ordinary code uses Engine.Run(window), Input.IsActionPressed(action) and ProjectSettings.Get(setting). State and native ownership still live on objects. Operation names, overloads, defaults, validation, event payloads, ordering and execution behavior remain intact. Property descriptors inspect the same retained state.

### Rejected alternatives

- Static classes and fields: these remove existing identity, inheritance and typed object contracts and require redesigning native ownership.
- Both Instance and static operations: two conventions would leave the migration incomplete.
- Implicit native opening on an ordinary call: startup configuration, owner-thread requirements and teardown would become implicit.

### Verification boundary

Check compiled declared public operations for static access and absence of Instance. Exercise descriptors, retained identities, events, lifecycle failures, unavailable native calls and reopening. Existing domain tests establish only their exercised behavior. Compilation and managed checks do not establish native rendering, other platforms, unmeasured performance or human acceptance.

### Related decisions

- [0016: Engine runtime](core-object-runtime.md#adr-0016)
- [0019: Project settings](core-data-io.md#adr-0019)
- [0038: Input services](input.md#adr-0038)
- [0040: Display lifecycle](display.md#adr-0040)
- [0047: Audio service](audio.md#adr-0047)
- [0083: Theme service](rendering.md#adr-0083)
