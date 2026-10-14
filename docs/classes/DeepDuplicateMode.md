# DeepDuplicateMode

Last updated: 2026-10-15

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/IO/DeepDuplicateMode.cs`](../../src/Core/IO/DeepDuplicateMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum DeepDuplicateMode`

> Controls which nested resources are copied during deep resource duplication.

## Description

Controls which nested resources are copied during deep resource duplication.

`DeepDuplicateMode` selects which nested [`Resource`](Resource.md) instances are copied by `Resource.DuplicateDeep`. It does not decide whether typed collection containers are copied: a derived resource copies its containers whenever the `deep` argument supplied to its duplication hook is `true`. Ordinary nested `Script` assets stay shared in all three modes. Explicit force-copy and never-copy fields override the mode. Each edge selects its policy before cached targets are used; a previously forced target cannot change a shared edge.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = DeepDuplicateMode.None;
```

## Constants

| Member | Description |
| --- | --- |
| [`None = 0`](#f-electron2d-deepduplicatemode-none) | Shares ordinary nested resources while derived hooks copy their collection containers. |
| [`Internal = 1`](#f-electron2d-deepduplicatemode-internal) | Duplicates ordinary built-in nested resources except script assets. |
| [`All = 2`](#f-electron2d-deepduplicatemode-all) | Duplicates ordinary nested resources, including external resources, except script assets. |

## Constant Descriptions

<a id="f-electron2d-deepduplicatemode-none"></a>
### `None = 0`

Shares ordinary nested resources while derived hooks copy their collection containers.

<a id="f-electron2d-deepduplicatemode-internal"></a>
### `Internal = 1`

Duplicates ordinary built-in nested resources except script assets.

<a id="f-electron2d-deepduplicatemode-all"></a>
### `All = 2`

Duplicates ordinary nested resources, including external resources, except script assets.

## Errors, threading, and dependencies

`Resource.DuplicateDeep` rejects values outside the declared enum. The enum owns no state and has no threading restrictions. It depends only on the resource duplication contract.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` and the public physical resource consumers verify exact values 0/1/2, defaults, all three modes, typed containers, built-in/external/Script references, force/never ordering, aliases, selected cycles, cleared duplicate identity and invalid-value rejection. The 13 physical/base families exercise 390 mode/path/order cases and 30 root-cycle cases. Copy-failure consumers also verify graph rollback and independent recovery; current evidence is recorded in `bin/physics-resource-copy-failures/2026-10-15/validation.json`.

Container copying remains an explicit responsibility of each derived typed resource. Automatic reflective copying is intentionally absent.
