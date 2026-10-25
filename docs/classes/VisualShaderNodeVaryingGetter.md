# VisualShaderNodeVaryingGetter

Last updated: 2026-10-25

Source: [VisualShaderNodeVaryingGetter.cs](../../src/Scene/Resources/VisualShaderNodeVaryingGetter.cs). Inherits [VisualShaderNodeVarying](VisualShaderNodeVarying.md).

```csharp
public sealed class VisualShaderNodeVaryingGetter : VisualShaderNodeVarying
```

## Description

Reads one value from the selected declaration. It has no inputs and one output with the selected numeric role. Fragment reads receive interpolated vertex data; vertex reads observe the value at their graph evaluation point. Values begin at typed zero or matrix identity before any writes. Sharing a name does not create a graph dependency; use the declared vertex/fragment producer and a later canvas stage for transport. Light reads receive interpolated vertex values or private values from the completed fragment according to the declaration.

## Member summary

| Signature | Contract |
| --- | --- |
| `public VisualShaderNodeVaryingGetter()` | Creates an unselected Float getter. |
| `protected override Resource CreateDuplicateInstance()` | Constructs an independent definition for inherited duplication. |

The inherited `VaryingName` and `VaryingType` properties own selection, validation and stored state. Definitions are borrowed by graphs. Copying preserves their role, defaults and names; loaded graphs retain aliases. Dispose does not remove a declaration from its graph. Disposed accesses fail through inherited lifetime checks.

## Example

See the complete [declaration/write/read example](VisualShaderNodeVarying.md#example). Runtime materials use the last imported program; graph edits require offline reimport. The GPU backend executes these programs; compatibility retains its explicit shader refusal.

## Verification

The public [CPU](../../tests/VisualShader.Consumer/Varyings.CPU.cs), [authoring](../../tests/VisualShader.Consumer/Varyings.Author.cs) and [native](../../tests/VisualShader.Consumer/Varyings.Render.cs) consumers exercise declaration access, all eight numeric roles, defaults, persistence and actual geometry streams. [The connected component contract](../components/visual-shaders.md#typed-varying-transport) records the verified profiles and remaining limits.

## Authored canvas lights

See [canvas light graphs](../components/canvas-light-graphs.md) for the executing stage, typed inputs/outputs, fragment/vertex varying producers, shared native resource slots, custom provider activation, portable program validation and measured limits.
