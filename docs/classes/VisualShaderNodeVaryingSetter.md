# VisualShaderNodeVaryingSetter

Last updated: 2026-10-25

Source: [VisualShaderNodeVaryingSetter.cs](../../src/Scene/Resources/VisualShaderNodeVaryingSetter.cs). Inherits [VisualShaderNodeVarying](VisualShaderNodeVarying.md).

```csharp
public sealed class VisualShaderNodeVaryingSetter : VisualShaderNodeVarying
```

## Description

Writes input zero into the selected declaration from its declared canvas vertex or fragment producer. It has one input and no outputs. The stage planner executes it as a side-effect root even when no path connects it to the reserved output. Input defaults and connected expressions retain their ordinary typed behavior. It evaluates the reserved output's dependencies first, then setter roots in ascending node ID with each root's dependencies before its write. Multiple writes therefore preserve root order even when earlier writes need extra input evaluation. Named writes require exactly the declared producer; light, particle and writable-texture writes reject import. FragmentToLight feeds private per-fragment variables used by each light invocation.

## Member summary

| Signature | Contract |
| --- | --- |
| `public VisualShaderNodeVaryingSetter()` | Creates an unselected Float setter with zero input. |
| `protected override Resource CreateDuplicateInstance()` | Constructs an independent definition for inherited duplication. |

The inherited `VaryingName` and `VaryingType` properties own selection, validation and stored state. Definitions are borrowed by graphs. Copying preserves their role, defaults and names; loaded graphs retain aliases. Dispose does not remove a declaration from its graph. Disposed accesses fail through inherited lifetime checks.

## Example

See the complete [declaration/write/read example](VisualShaderNodeVarying.md#example). Runtime materials use the last imported program; graph edits require offline reimport. The GPU backend executes these programs; compatibility retains its explicit shader refusal.

## Verification

The public [CPU](../../tests/VisualShader.Consumer/Varyings.CPU.cs), [authoring](../../tests/VisualShader.Consumer/Varyings.Author.cs) and [native](../../tests/VisualShader.Consumer/Varyings.Render.cs) consumers exercise declaration access, all eight numeric roles, defaults, persistence and actual geometry streams. [The connected component contract](../components/visual-shaders.md#typed-varying-transport) records the verified profiles and remaining limits.

## Authored canvas lights

See [canvas light graphs](../components/canvas-light-graphs.md) for the executing stage, typed inputs/outputs, fragment/vertex varying producers, shared native resource slots, custom provider activation, portable program validation and measured limits.
