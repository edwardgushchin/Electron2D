# PhysicsBackendOneWay

Last updated: 2026-10-11

**Declaration:** `public readonly record struct PhysicsBackendOneWay` · **Source:** [PhysicsBackendObservations.cs](../../src/Servers/Physics/PhysicsBackendObservations.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Local RID and shape/piece identities plus allowed state of a portable one-way episode. The adapter converts network IDs at the wire boundary; no local RID enters the encoding.

The record owns values and no native handles, scene storage or transferable world lifetime. It is internal input/output for the selected implementation contract. Claims about public extension API are tracked separately under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Verification and limits

The [independent probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) covers its exercised adapter paths; custom geometry, external consumers and full portable episode support remain acceptance work.
