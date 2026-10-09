# PhysicsContactSettings

Last updated: 2026-10-09

**Declaration:** `internal readonly record struct PhysicsContactSettings(float Bias, float AllowedPenetration)`

**Source:** [PhysicsContactSettings.cs](../../src/Servers/Physics/PhysicsContactSettings.cs)
**Component:** [Contact correction](../components/physics-contact-policy.md)

Shared authored policy captured from typed project settings with feature overrides.
Bias is a finite fraction in [0,1]; slack is finite nonnegative scene distance whose
nonzero value survives backend length conversion and whose squared backend value
is finite. Validation runs before a world is created or an existing policy changes.
CPU PhysicsSpace and independent GPUPhysicsBodyStore retain the same record; it
contains no physical body state or host mirror. Public GPU binding remains open.
