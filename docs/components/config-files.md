# Configuration files component

Last updated: 2026-09-24

## Scope

This Core component provides strongly typed sectioned configuration documents with deterministic text encoding, transactional parsing, atomic persistence, and authenticated encryption. It is used by the project-settings layer and remains unsuitable for real-time frame state or live engine objects.

## Owned types

| Type | Role |
| --- | --- |
| [`ConfigKey<T>`](../classes/ConfigKey.Generic.md) | Immutable section/name/type identity for one entry |
| [`ConfigFile`](../classes/ConfigFile.md) | Concurrent document storage, parse/encode, persistence, and encryption |

Both types are implemented in [`src/Core/IO/ConfigFile.cs`](../../src/Core/IO/ConfigFile.cs).

## Runtime flow

1. A caller defines and reuses a `ConfigKey<T>`.
2. `SetValue` serializes the typed value to a compact JSON token before taking the document mutation lock; null removes the entry.
3. Reads copy the stored token under the lock and deserialize a new `T`, preventing mutable aliasing.
   [`Color`](../classes/Color.md), [`Vector2`](../classes/Vector2.md), [`Vector2I`](../classes/Vector2I.md), [`Vector3`](../classes/Vector3.md), [`Vector3I`](../classes/Vector3I.md), [`Vector4`](../classes/Vector4.md), [`Vector4I`](../classes/Vector4I.md), [`Rect`](../classes/Rect.md), [`RectI`](../classes/RectI.md), and [`Transform`](../classes/Transform.md) use exact object converters instead of relying on incidental field/property reflection.
4. `Parse` and all load variants fully validate a temporary operation list before one locked merge.
5. Save variants snapshot the encoded document, then write, flush, and move a same-directory temporary file over the destination.
6. Encrypted loads authenticate/decrypt before UTF-8 decoding and parsing, so failed authentication cannot alter state.

## Dependencies

- Core `ElectronObject` lifetime and diagnostics.
- .NET JSON, collections, strict UTF-8, file-system, randomness, PBKDF2, and AES-GCM primitives.
- No Scene, SDL, renderer, input, physics, resource-loader, scripting, editor, or external package dependency.

## Invariants

- Public value access is generic and keyed; top-level and nested generic/array types reject `object`, `dynamic`, JSON DOM, and engine-object storage boundaries.
- Identifier matching is ordinal and case-sensitive.
- Empty section and entry names are valid; empty entry names use quoted text identifiers.
- Entry/section order is stable after first insertion; replacement does not reorder.
- Sectionless entries precede named sections.
- Failed parsing, decoding, serialization, authentication, or file reads do not mutate in-memory state.
- A null assignment or parsed JSON null removes an entry; empty sections are removed.
- Color snapshots contain exactly four finite numeric `R`, `G`, `B`, and `A` fields. Missing, duplicate, unknown, or non-finite fields are rejected.
- `Vector2` snapshots contain exactly finite numeric `X` and `Y` fields; `Vector2I` snapshots contain exact 32-bit integer `X` and `Y` fields.
- `Vector3` snapshots contain exactly finite numeric `X`, `Y`, and `Z` fields; `Vector3I` snapshots contain exact 32-bit integer fields with the same names.
- `Vector4` snapshots contain exactly finite numeric `X`, `Y`, `Z`, and `W` fields; `Vector4I` snapshots contain exact 32-bit integer fields with the same names.
- Rectangle snapshots contain exactly `Position` and `Size`, each with finite numeric `X` and `Y` fields. Missing, duplicate, unknown, nonnumeric, or non-finite fields are rejected; computed `End` and `Area` are never persisted.
- Integer rectangle snapshots contain exactly `Position` and `Size`, each with exact 32-bit integer `X` and `Y` fields. Missing, duplicate, unknown, non-integer, or out-of-range fields are rejected; computed `End` and `Area` are never persisted.
- Transform snapshots contain exactly `X`, `Y`, and `Origin`, each with finite numeric `X` and `Y` fields. Missing, duplicate, unknown, nonnumeric, or non-finite fields are rejected.
- Text output is strict UTF-8 without a BOM and uses LF line endings.
- Raw-key and password modes cannot be confused; all encrypted contents and headers are authenticated.
- Disposal clears owned state and later entry points fail.

## Current implementation status

The in-memory section/key, text parse/encode and plain file surfaces are implemented and audited under ADR 0018. Text tests cover quoted complex/empty names, JSON values, stable LF output, BOM/CRLF and comment handling, repeated-section merge, ordering, empty input and rollback after malformed lines. Plain file tests cover strict UTF-8, BOM/CRLF, merge/rollback, overwrite, empty output, directory errors and temporary cleanup. Encrypted file methods remain executable but retain Partial coverage until their envelope and error behavior is audited. Error-return APIs are adapted to normal C# exceptions, universal values to `ConfigKey<T>`, and reference-counted lifetime to `ElectronObject`/managed memory.

## Exclusions and deferred integration

- Direct `res://` and `user://` handling stays outside this generic component; `ProjectSettings` and the separate `FileAccess` component resolve them.
- Defaults, validators, feature overrides, restart flags, ordering policy, and change tracking are implemented by the separate [Project settings](project-settings.md) component.
- Asset loader/saver integration, async streaming, and editor configuration UIs do not exist. General blocking file access is implemented by the separate [File access](file-access.md) component.
- The binary encrypted envelope is versioned for Electron2D only; cross-engine compatibility is not promised.
- Comments are accepted but intentionally not retained on re-encoding.

## Verification

`tests/Electron2D.Tests/Program.cs` exercises successful and failing in-memory, concurrent mutation/disposal, filesystem, serializer, strict color, four-vector, floating/integer rectangle, and transform schemas and rollback, UTF-8, raw-key encryption, password encryption, tamper, mode, rollback, disposal, and cleanup paths. Local tests cannot prove crash-time durability on every filesystem, resistance to compromised process memory, or suitability for storing high-value credentials.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0004: 2D API in one Electron2D-owned assembly](../decisions/product.md#adr-0004)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0018: Typed configuration files](../decisions/core-data-io.md#adr-0018)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](../decisions/core-data-io.md#adr-0020)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
