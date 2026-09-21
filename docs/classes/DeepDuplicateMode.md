# DeepDuplicateMode

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/IO/DeepDuplicateMode.cs`](../../src/Core/IO/DeepDuplicateMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum DeepDuplicateMode`

> Controls which nested resources are copied during deep resource duplication.

## Description

Controls which nested resources are copied during deep resource duplication.

`DeepDuplicateMode` selects which nested [`Resource`](Resource.md) instances are copied by `Resource.DuplicateDeep`. It does not decide whether typed collection containers are copied: a derived resource copies its containers whenever the `deep` argument supplied to its duplication hook is `true`.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = DeepDuplicateMode.None;
```

## Constants

| Member | Description |
| --- | --- |
| [`None = 0`](#f-electron2d-deepduplicatemode-none) | Shares every nested resource while still allowing derived resources to copy their collection containers. |
| [`Internal = 1`](#f-electron2d-deepduplicatemode-internal) | Duplicates only nested resources that are embedded or do not have an external path. |
| [`All = 2`](#f-electron2d-deepduplicatemode-all) | Duplicates every nested resource, including resources with external paths. |

## Constant Descriptions

<a id="f-electron2d-deepduplicatemode-none"></a>
### `None = 0`

Shares every nested resource while still allowing derived resources to copy their collection containers.

<a id="f-electron2d-deepduplicatemode-internal"></a>
### `Internal = 1`

Duplicates only nested resources that are embedded or do not have an external path.

<a id="f-electron2d-deepduplicatemode-all"></a>
### `All = 2`

Duplicates every nested resource, including resources with external paths.

## Errors, threading, and dependencies

`Resource.DuplicateDeep` rejects values outside the declared enum. The enum owns no state and has no threading restrictions. It depends only on the resource duplication contract.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies all three modes, typed-container behavior, built-in versus external nested resources, repeated references, cycles, cleared duplicate identity, and invalid values.

Container copying remains an explicit responsibility of each derived typed resource. Automatic reflective copying is intentionally absent.
