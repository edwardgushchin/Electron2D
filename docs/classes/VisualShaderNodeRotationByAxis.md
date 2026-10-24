# VisualShaderNodeRotationByAxis

Last updated: 2026-10-24

Source: [VisualShaderNodeRotationByAxis.cs](../../src/Scene/Resources/VisualShaderNodeRotationByAxis.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeRotationByAxis
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Rotates three numeric components and independently exposes the matching numeric matrix. It executes through the existing offline import and ShaderMaterial pipeline in canvas vertex/fragment, all five particle lifecycle stages and writable-texture graphs.

## Ports and arithmetic

| Port | Role | Default |
| --- | --- | --- |
| Input 0 | Vector3 value | Zero |
| Input 1 | Scalar angle in radians | 0 |
| Input 2 | Vector3 axis | Zero |
| Output 0 | Vector3 row-vector product | Computed |
| Output 1 | Matrix4x4 numeric rotation | Computed |

Normalizes the supplied axis and constructs the axis-angle rotation matrix. Output zero multiplies the input as a row vector by its upper three-by-three block. Output one exposes that same matrix extended with a homogeneous identity row and column; it is not transposed to change multiplication direction. The scalar angle is in radians. Nonunit, negative and oblique finite axes retain their numeric meaning. A zero axis, including the initial default, follows native normalization semantics and has no portable finite-result guarantee. Supply a nonzero axis for a defined rotation. This is numeric shader data, independent of planar scene transform types.

```csharp
using var rotation = new VisualShaderNodeRotationByAxis();
rotation.SetInputPortDefaultValue(0, new Vector3(1, 0, 0));
rotation.SetInputPortDefaultValue(1, MathF.PI / 2);
rotation.SetInputPortDefaultValue(2, new Vector3(0, 0, 2));
```

Connect the outputs through [the shared author/import workflow](../components/visual-shaders.md#numeric-orientation). Material parameters can supply connected inputs without reimport.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeRotationByAxis()`](#constructor) | Creates the typed input defaults above. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#duplicate) | Constructs an empty definition for inherited Resource copying. |

<a id="constructor"></a>
### `.ctor`

```csharp
public VisualShaderNodeRotationByAxis()
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
