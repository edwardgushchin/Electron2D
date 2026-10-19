# PhysicsServerManager

Last updated: 2026-10-19

Retained process-wide static registration/default service. RegisterServer accepts a typed fresh implementation factory. SetDefaultServer uses strict priority; changes affect future captures only. CPU/GPU/DEFAULT are reserved; factories run outside the lock and recursive creation rejects.

See [registered backend contract](../components/physics-registered-adapter.md) for ownership, construction, verification and remaining acceptance.

## Project physics capture

When `ProjectSettings.Physics2DEngine` is DEFAULT, a new implicit scene world captures the current priority-selected registration. A concrete project name captures that registration independently of priority. Existing worlds and caller parameterless/explicit creation keep their choices.

See [the selection checks](../components/physics-backends.md#project-physics-selection).

[Space creation verification](../components/physics-registered-adapter.md#space-creation-operation-audit) exercises factory lock independence, DEFAULT capture, later priority replacement, invalid factory preservation and complete initialization cleanup through the public service. Parameterless and explicit enum creation remain independent of the manager default.
