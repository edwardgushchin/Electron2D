# Resources domain

Last updated: 2026-09-21

## Responsibility

The Resources domain defines the reusable typed data base used by future textures, audio data, fonts, scripts, and other assets across Linux, Windows, macOS, Android, and iOS. Its present production type remains the portable common resource contract, now consumed by Scene's in-memory packed-scene component; no concrete asset formats are claimed.

Its production sources live under `src/Core/IO/`, matching their low-level engine module while the living architecture retains Resources as a separate logical domain. The public namespace remains `Electron2D`.

## Component inventory

| Component | Types | State |
| --- | --- | --- |
| [Resource base](../components/resources.md) | [`Resource`](../classes/Resource.md), [`DeepDuplicateMode`](../classes/DeepDuplicateMode.md) | Implemented and verified |

## Public surface

The domain exposes resource name/path/scene configuration, built-in classification, synchronous change/setup events, local-scene association, reset and raw-cache hooks, copy and graph-preserving duplication, explicit deep-copy policy, scene ID generation, path takeover, typed property descriptors, and deterministic disposal through Core.

## Dependency direction

Resources depends on Core and, narrowly, Scene's `Node` type for `Resource.GetLocalScene()`. Scene's packed-scene component in turn depends on Resources for typed resource duplication, so ADR 0023 accepts a contained Resources↔Scene type cycle inside the single `Electron2D.dll`. The Resource base does not depend on `PackedScene`, `SceneTree`, rendering, SDL integration, input, audio, physics, scripting, file serialization, importing, or an editor.

## Domain-wide invariants

- Electron2D-owned code stays in `Electron2D.dll`.
- Resource state is typed; there is no dynamic property bag or untyped reflection-based copier.
- Registered paths are ordinal, process-wide, weakly held, and single-owner.
- Derived resources explicitly define construction and stored-state copying.
- Successful duplication preserves graph topology and clears external identity.
- Scene-local duplicates preserve graph topology, receive their owning scene root before setup, run setup once, and are disposed by that root.
- Failed duplication disposes its complete partial result.
- Base state is concurrently safe; derived state must define any stronger contract.
- Managed object memory is reclaimed by the runtime; deterministic disposal controls logical and native-resource lifetime, not managed memory reclamation.
- Public resources do not expose manual reference counting. A future asset manager may count internal disposable leases solely to retain shared native-backed payloads.
- Serialized resource state and ownership semantics must remain portable across all five runtime targets; platform-native payloads require explicit internal backends.

## Current limitations

There are no concrete asset types, asset loader/saver, cache modes, importer, renderer RID, editor resource-ID map, script resource, automatic file-serialization discovery, resource manager, or asset lease type. In-memory packed scenes now perform automatic per-instance local duplication, association, setup, and root ownership, but there is no disk format, UID/import integration, or cross-platform asset import/package verification. Internal asset leases are an accepted future ownership boundary, not an implemented API.

## Decisions

- [ADR 0001: Typed C# without Variant](../decisions/0001-typed-csharp-without-variant.md)
- [ADR 0002: C# events for signals](../decisions/0002-csharp-events-for-signals.md)
- [ADR 0003: ElectronObject lifetime](../decisions/0003-electron-object-lifetime.md)
- [ADR 0004: 2D API in one Electron2D-owned assembly](../decisions/0004-2d-api-single-assembly.md)
- [ADR 0013: Managed typed Resource contract](../decisions/0013-managed-resource-contract.md)
- [ADR 0014: Managed Resource lifetime and realtime allocation](../decisions/0014-managed-resource-lifetime.md)
- [ADR 0017: Source-tree module layout](../decisions/0017-source-tree-layout.md)
- [ADR 0021: Cross-platform runtime target matrix](../decisions/0021-cross-platform-runtime-targets.md)
- [ADR 0023: Typed in-memory packed scenes](../decisions/0023-typed-packed-scenes.md)

## Verification

Resource and packed-scene checks live in `tests/Electron2D.Tests/Program.cs`. They exercise base duplication plus per-instance local graph association/setup/ownership without requiring an absent asset loader/saver, file serialization, editor, or rendering subsystem.
