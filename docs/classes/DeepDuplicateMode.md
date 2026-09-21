# DeepDuplicateMode

Last updated: 2026-09-21

## Declaration

- Source: [`DeepDuplicateMode.cs`](../../src/Core/IO/DeepDuplicateMode.cs)
- Namespace: `Electron2D`
- Declaration: `public enum DeepDuplicateMode`
- Domain: [Resources](../domains/resources.md)
- Component: [Resource base](../components/resources.md)

## Responsibility

`DeepDuplicateMode` selects which nested [`Resource`](Resource.md) instances are copied by `Resource.DuplicateDeep`. It does not decide whether typed collection containers are copied: a derived resource copies its containers whenever the `deep` argument supplied to its duplication hook is `true`.

## Values

| Value | Numeric value | Current behavior |
| --- | ---: | --- |
| `None` | `0` | Shares every nested resource while derived types may still copy their typed collection containers |
| `Internal` | `1` | Duplicates nested resources whose `IsBuiltIn` property is `true`; shares standalone external resources |
| `All` | `2` | Duplicates every reachable nested resource |

Repeated references and cycles retain graph identity for every resource selected for duplication. Every duplicate receives an empty resource path and scene-unique ID.

## Errors, threading, and dependencies

`Resource.DuplicateDeep` rejects values outside the declared enum. The enum owns no state and has no threading restrictions. It depends only on the resource duplication contract.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies all three modes, typed-container behavior, built-in versus external nested resources, repeated references, cycles, cleared duplicate identity, and invalid values.

Container copying remains an explicit responsibility of each derived typed resource. Automatic reflective copying is intentionally absent.
