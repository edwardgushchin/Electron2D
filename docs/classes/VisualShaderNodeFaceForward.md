# VisualShaderNodeFaceForward

Last updated: 2026-10-24

Source: [VisualShaderNodeFaceForward.cs](../../src/Scene/Resources/VisualShaderNodeFaceForward.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeFaceForward
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Orients a numeric vector using the incident/reference dot product. It executes through the existing offline import and ShaderMaterial pipeline in canvas vertex/fragment, all five particle lifecycle stages and writable-texture graphs.

## Ports and arithmetic

| Port | Role | Default |
| --- | --- | --- |
| Input 0 | Numeric vector N | Zero Vector3 |
| Input 1 | Numeric incident vector I | Zero Vector3 |
| Input 2 | Numeric reference vector Nref | Zero Vector3 |
| Output 0 | Oriented vector | Computed |

Returns N when dot(Nref, I) is strictly negative; otherwise returns -N, including at zero. Inputs are not normalized. Inherited `OpType` selects two, three or four numeric components and converts every stored default using the shared vector-width contract. These numeric components add no spatial scene type.

```csharp
using var orientation = new VisualShaderNodeFaceForward();
orientation.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
orientation.SetInputPortDefaultValue(0, new Vector4(1, 2, 3, 4));
```

Connect the outputs through [the shared author/import workflow](../components/visual-shaders.md#numeric-orientation). Material parameters can supply connected inputs without reimport.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeFaceForward()`](#constructor) | Creates the typed input defaults above. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#duplicate) | Constructs an empty definition for inherited Resource copying. |

<a id="constructor"></a>
### `.ctor`

```csharp
public VisualShaderNodeFaceForward()
```

Creates the listed defaults. Inherited setters reject incorrect types, nonfinite authored values and invalid input ports before mutation.

<a id="duplicate"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

Constructs a fresh concrete definition. Inherited Resource copying preserves independent defaults. Compiled archive factories restore its concrete type and shared graph aliases, including compressed big-endian archives.

## Lifecycle and verification

This is a cold, single-threaded Resource definition borrowed by its graph. Source edits require offline reimport; material parameters use the retained compiled program. See [numeric orientation](../components/visual-shaders.md#numeric-orientation) for executable verification and remaining platform gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Public API policy](../decisions/product.md#adr-0004)
- [Typed resources](../decisions/resources.md#adr-0013)
