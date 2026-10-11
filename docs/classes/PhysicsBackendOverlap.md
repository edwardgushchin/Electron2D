# PhysicsBackendOverlap

Last updated: 2026-10-11

**Declaration:** `public readonly record struct PhysicsBackendOverlap` · **Source:** [PhysicsBackendObservations.cs](../../src/Servers/Physics/PhysicsBackendObservations.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Logical remote collider RID and remote/local shape indices proposed by an Area implementation. Complete output validates before tracker mutation; the engine samples object identity and checks membership, enabled slots, layers and monitorability.

The record owns values and no native handles, scene storage or transferable world lifetime. It is internal input/output for the selected implementation contract. Claims about public extension API are tracked separately under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Verification and limits

The [independent probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) covers its exercised adapter paths; custom geometry, external consumers and full portable episode support remain acceptance work.
