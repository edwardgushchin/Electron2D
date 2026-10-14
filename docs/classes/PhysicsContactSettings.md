# PhysicsContactSettings

Last updated: 2026-10-14

**Declaration:** `public readonly record struct PhysicsContactSettings(float Bias, float AllowedPenetration, float RecycleRadius = 1, float MaxSeparation = 1.5f)`

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

This immutable value is now public and crosses the [registered implementation boundary](../components/physics-registered-adapter.md); complete sampled fields retain the contracts above. No backend storage or native handles are exposed.

[Shared public contact history](../components/physics-contact-policy.md#shared-public-history-2026-10-14)
now checks seven geometries, live reuse limits, actual support/impulses and scene
sleep/event ordering through explicit CPU/GPU worlds in both numerical-thread modes.
All six public/resident/stage profiles retain 128 edited full-step samples at
0/0 owner/all-thread managed bytes. Software Vulkan evidence leaves hardware,
registered geometry and broader network/platform/performance acceptance open.


[Registered contact numerics](../components/physics-registered-adapter.md#registered-contact-numerics)
now exercises actual world/shape bias and penetration slack, complete contact/joint
sweeps, full physical impulses and identity-preserving live policy through the
public-only provider. Seven source families, dense contacts, CCD durations and
contact/pin convergence use nineteen complete 128-warmup/128-measured brackets
at zero managed allocation. Registered contact-cache, arbitrary coupled/custom,
full inherited/extension/network and native/platform/performance acceptance remain
open.
