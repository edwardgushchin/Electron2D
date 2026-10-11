# PhysicsServerManager

Last updated: 2026-10-11

Retained process-wide static registration/default service. RegisterServer accepts a typed fresh implementation factory. SetDefaultServer uses strict priority; changes affect future captures only. CPU/GPU/DEFAULT are reserved; factories run outside the lock and recursive creation rejects.

See [registered backend contract](../components/physics-registered-adapter.md) for ownership, construction, verification and remaining acceptance.
