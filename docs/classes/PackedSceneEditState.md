# PackedSceneEditState

Last updated: 2026-09-21

## Declaration

- Source: [`PackedSceneEditState.cs`](../../src/Scene/Resources/PackedSceneEditState.cs)
- Namespace: `Electron2D`
- Declaration: `public enum PackedSceneEditState`
- Domain: [Scene](../domains/scene.md)
- Component: [Packed scenes](../components/packed-scenes.md)

## Responsibility

`PackedSceneEditState` is the stable typed policy accepted by [`PackedScene.Instantiate()`](PackedScene.md). The runtime implements only ordinary instantiation; the remaining numeric values reserve editor meanings and fail explicitly.

## Values

| Value | Number | Current behavior |
| --- | ---: | --- |
| `Disabled` | `0` | Supported; creates an ordinary detached runtime instance |
| `Instance` | `1` | Reserved for local editable-instance metadata; throws `NotSupportedException` |
| `Main` | `2` | Reserved for main-scene editor metadata; throws `NotSupportedException` |
| `MainInherited` | `3` | Reserved for inherited-main-scene editor metadata; throws `NotSupportedException` |

Undefined numeric values throw `ArgumentOutOfRangeException` before any node or resource is created.

## Lifecycle, ownership, and threading

The enum has no mutable state, lifetime, ownership, allocation, or thread-affinity behavior. It is consumed synchronously by `PackedScene.Instantiate()`.

## Dependencies and limitations

The type has no dependencies beyond the CLR enum contract. It does not make editor behavior available; editable instances, pinned properties, inherited scene authoring, placeholder workflows, and script preservation remain absent.

## Verification

The executable harness verifies `Disabled`, rejection of the defined `Instance` editor mode, and rejection of an undefined numeric value. The common implementation branch rejects `Main` and `MainInherited` as well, but those two values do not yet have separate regression cases.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/0023-typed-packed-scenes.md)
