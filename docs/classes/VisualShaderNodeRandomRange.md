# VisualShaderNodeRandomRange

Last updated: 2026-10-22

Source: [VisualShaderNodeRandomRange.cs](../../src/Scene/Resources/VisualShaderNodeRandomRange.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeRandomRange
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Produces repeatable scalar variation from three numeric seed components and two scalar bounds. The definition uses the shared offline import and ShaderMaterial path in canvas, particle and writable-texture graphs. It consumes no mutable random state and has no width selector.

## Ports and arithmetic

| Port | Role | Default |
| --- | --- | --- |
| Input 0 | Vector3 numeric seed | (1, 1, 1) |
| Input 1 | Scalar first bound | 0 |
| Input 2 | Scalar second bound | 1 |
| Output 0 | Scalar result | Computed |

The seed multiplies a fixed numeric matrix as a row vector. Nested fractional parts produce a factor in [-1, 1), then the bounds are mixed **without clamping**. The result can be below the first bound. A zero seed yields `2 * first - second`; equal bounds yield that bound, and reversed bounds retain the same arithmetic. Finite authored values are required. Large values remain subject to native shader overflow and floating-point precision; results are not a portable random sequence across devices.

```csharp
using var variation = new VisualShaderNodeRandomRange();
variation.SetInputPortDefaultValue(0, new Vector3(.125f, .5f, .875f));
variation.SetInputPortDefaultValue(1, .25f);
variation.SetInputPortDefaultValue(2, .75f);
```

Connect the scalar output through [the shared author/import workflow](../components/visual-shaders.md#repeatable-scalar-variation). Material parameters can supply the seed and either bound. Numeric Vector3 data adds no spatial scene type.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeRandomRange()`](#constructor) | Creates the seed and bounds above. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#duplicate) | Constructs an empty definition for inherited Resource copying. |

<a id="constructor"></a>
### `.ctor`

```csharp
public VisualShaderNodeRandomRange()
```

Creates the definition with seed (1, 1, 1), first bound zero and second bound one. Inherited typed default setters reject incorrect types, nonfinite values and invalid ports before mutation.

<a id="duplicate"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

Constructs a new RandomRange definition. The inherited Resource copy operation copies defaults independently. Compiled resource factories restore the same concrete type and shared aliases from graph archives, including compressed big-endian archives.

## Lifecycle and verification

This is a cold, single-threaded Resource definition borrowed by its graph. Source edits require reimport; material parameter updates use the existing retained runtime program. See the [component verification record](../components/visual-shaders.md#repeatable-scalar-variation) for exercised profiles and remaining platform gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Public API policy](../decisions/product.md#adr-0004)
- [Typed resources](../decisions/resources.md#adr-0013)
