# PhysicsContactSettings

Last updated: 2026-10-09

**Declaration:** `internal readonly record struct PhysicsContactSettings(float Bias, float AllowedPenetration, float RecycleRadius = 1, float MaxSeparation = 1.5f)`

**Source:** [PhysicsContactSettings.cs](../../src/Servers/Physics/PhysicsContactSettings.cs)
**Component:** [Contact correction](../components/physics-contact-policy.md)

Shared authored policy captured from typed project settings with feature overrides.
Bias is a finite fraction in [0,1]; slack is finite nonnegative scene distance whose
nonzero value survives backend length conversion and whose squared backend value
is finite. RecycleRadius and MaxSeparation additionally require a finite scene-unit
square and a nonzero squared backend distance for positive values. Zero is accepted.
Validation runs before a world is created or an existing policy changes.
CPU PhysicsSpace and independent GPUPhysicsBodyStore retain the same record; it
contains no physical body state or host mirror. Public GPU binding remains open.
