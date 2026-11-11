# PackedSceneEditState

Last updated: 2026-11-12

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Resources/PackedSceneEditState.cs`](../../src/Scene/Resources/PackedSceneEditState.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum PackedSceneEditState`

> Controls editor metadata applied while a packed scene is instantiated.

## Description

Controls editor metadata applied while a packed scene is instantiated.

`PackedSceneEditState` is the stable typed policy accepted by [`PackedScene.Instantiate()`](PackedScene.md). Gameplay uses Disabled. In editor context, Instance opens an editable subscene, Main opens the current authored layer and MainInherited creates a new derived layer.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
Engine.IsEditorHint = true;
using var root = baseScene.Instantiate(PackedSceneEditState.MainInherited);
using var derived = new PackedScene();
derived.Pack(root);
```

## Constants

| Member | Description |
| --- | --- |
| [`Disabled = 0`](#f-electron2d-packedsceneeditstate-disabled) | Creates a runtime scene instance without editable-scene metadata. |
| [`Instance = 1`](#f-electron2d-packedsceneeditstate-instance) | Creates an editable instance with independent scene-local resources and source property revert values. |
| [`Main = 2`](#f-electron2d-packedsceneeditstate-main) | Opens the main authored scene with saved pins and its original scene-local resources. |
| [`MainInherited = 3`](#f-electron2d-packedsceneeditstate-maininherited) | Creates a detached editable root that packs as a new scene derived from the source template. |

## Constant Descriptions

<a id="f-electron2d-packedsceneeditstate-disabled"></a>
### `Disabled = 0`

Creates a runtime scene instance without editable-scene metadata.

<a id="f-electron2d-packedsceneeditstate-instance"></a>
### `Instance = 1`

Creates an editable instance with independent scene-local resources and source property revert values.

<a id="f-electron2d-packedsceneeditstate-main"></a>
### `Main = 2`

Opens the main authored scene with saved pins and its original scene-local resources.

<a id="f-electron2d-packedsceneeditstate-maininherited"></a>
### `MainInherited = 3`

Creates a detached editable root that packs as a new scene derived from the source template.

## Lifecycle, ownership, and threading

The enum has no mutable state, lifetime, ownership, allocation, or thread-affinity behavior. It is consumed synchronously by `PackedScene.Instantiate()`.

## Dependencies and limitations

The type has no dependencies beyond the CLR enum contract. MainInherited uses the [inherited-root contract](../components/linked-scenes.md#inherited-roots). Instance and Main follow the [editor mode ownership and revert rules](../components/linked-scenes.md#editor-modes).

## Verification

The executable harness verifies all four modes, runtime rejection of editor modes, an undefined numeric value, nested revert, pinned override round-trip and borrowed-resource cleanup.

## Decision

- [0023: Typed in-memory packed scenes](../decisions/packed-scenes.md#adr-0023)
