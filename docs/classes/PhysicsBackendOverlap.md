# PhysicsBackendOverlap

Last updated: 2026-10-19

**Declaration:** `public readonly record struct PhysicsBackendOverlap` · **Source:** [PhysicsBackendObservations.cs](../../src/Servers/Physics/PhysicsBackendObservations.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Logical remote collider RID and remote/local shape indices proposed by an Area implementation. Complete output validates before tracker mutation; the engine samples object identity and checks membership, enabled slots, layers and monitorability.

The record owns values and no native handles, scene storage or transferable world lifetime. Selected implementation hooks exchange this public typed value under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Verification and limits

The [public publication audit](../components/physics-registered-adapter.md#area-overlap-publication-operation-audit) verifies bounded counts, live same-world peers, enabled global logical indices, directional layers and monitorability before a complete receiver snapshot commits. Duplicate numerical pieces collapse into one pair. Invalid output preserves history; independent receivers and later valid frames continue. Exact compound and custom annulus/geometry-edit/private-correction checks retain identities and callback histories. Wider numerical/custom geometry, portable networking, native memory, platform and hardware performance acceptance remains open.
