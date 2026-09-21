# PackedSceneEditState

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Resources/PackedSceneEditState.cs`](../../src/Scene/Resources/PackedSceneEditState.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum PackedSceneEditState`

> Controls editor metadata applied while a packed scene is instantiated.

## Description

Controls editor metadata applied while a packed scene is instantiated.

`PackedSceneEditState` is the stable typed policy accepted by [`PackedScene.Instantiate()`](PackedScene.md). The runtime implements only ordinary instantiation; the remaining numeric values reserve editor meanings and fail explicitly.

Runtime builds support only [`PackedSceneEditState.Disabled`](PackedSceneEditState.md#f-electron2d-packedsceneeditstate-disabled). The remaining values retain stable serialized identities for a
future editor and are rejected explicitly until that domain exists.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = PackedSceneEditState.Disabled;
```

## Constants

| Member | Description |
| --- | --- |
| [`Disabled = 0`](#f-electron2d-packedsceneeditstate-disabled) | Creates a runtime scene instance without editable-scene metadata. |
| [`Instance = 1`](#f-electron2d-packedsceneeditstate-instance) | Requests local editable-instance metadata from an editor build. |
| [`Main = 2`](#f-electron2d-packedsceneeditstate-main) | Requests main-scene editing metadata from an editor build. |
| [`MainInherited = 3`](#f-electron2d-packedsceneeditstate-maininherited) | Requests inherited-main-scene editing metadata from an editor build. |

## Constant Descriptions

<a id="f-electron2d-packedsceneeditstate-disabled"></a>
### `Disabled = 0`

Creates a runtime scene instance without editable-scene metadata.

<a id="f-electron2d-packedsceneeditstate-instance"></a>
### `Instance = 1`

Requests local editable-instance metadata from an editor build.

<a id="f-electron2d-packedsceneeditstate-main"></a>
### `Main = 2`

Requests main-scene editing metadata from an editor build.

<a id="f-electron2d-packedsceneeditstate-maininherited"></a>
### `MainInherited = 3`

Requests inherited-main-scene editing metadata from an editor build.

## Lifecycle, ownership, and threading

The enum has no mutable state, lifetime, ownership, allocation, or thread-affinity behavior. It is consumed synchronously by `PackedScene.Instantiate()`.

## Dependencies and limitations

The type has no dependencies beyond the CLR enum contract. It does not make editor behavior available; editable instances, pinned properties, inherited scene authoring, placeholder workflows, and script preservation remain absent.

## Verification

The executable harness verifies `Disabled`, rejection of the defined `Instance` editor mode, and rejection of an undefined numeric value. The common implementation branch rejects `Main` and `MainInherited` as well, but those two values do not yet have separate regression cases.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
