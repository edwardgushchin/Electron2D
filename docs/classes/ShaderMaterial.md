# ShaderMaterial

Last updated: 2026-09-22

- Declaration: `public sealed class ShaderMaterial : Material`
- Source: [Material.cs](../../src/Scene/Resources/Material.cs)
- Inherits: [Material](Material.md), [Resource](Resource.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

Pairs a borrowed Shader with independently owned parameter values and borrowed texture overrides. Multiple materials may share one Shader and hold different values; nodes sharing one material see the same updates. Null Shader selects ordinary canvas color drawing. Any assigned Shader requires GPU rendering. Material disposal clears its own state and never disposes the borrowed Shader or textures.

State operations are serialized per material. Native uniform upload uses the same gate. Notifications run after mutation outside the gate. Successful scalar/vector or span updates reuse initialized storage. Initial values are zero; no source defaults are extracted. Shader replacement migrates matching names, element types and array lengths to new offsets/bindings on the next parameter access or draw. Incompatible or newly introduced members start at zero; assigning null discards values.

## Example

```csharp
using var shader = Shader.CreateFromSPIRV(System.IO.File.ReadAllBytes("material.spv"));
using var material = new ShaderMaterial { Shader = shader };
// The imported program declares these names and types.
material.SetShaderParameter("tint", Colors.Red);
material.SetShaderParameter("gain", 0.5f);
material.SetShaderParameter<float>("weights", new float[] { 0.25f, 0.75f }.AsSpan());
Color tint = material.GetShaderParameter<Color>("tint");
```

## API summary

| Declaration | Contract |
| --- | --- |
| `ShaderMaterial()` | Creates a material with no Shader. |
| `Shader? Shader { get; set; }` | Borrowed program, null by default. |
| `void SetShaderParameter<T>(string name, T value) where T : unmanaged` | Sets a matching scalar/vector. |
| `void SetShaderParameter<T>(string name, ReadOnlySpan<T> values) where T : unmanaged` | Copies a complete fixed-size array. |
| `void SetShaderParameter(string name, Texture? texture)` | Sets a borrowed texture override; null uses the Shader default. |
| `Texture? GetShaderParameter(string name)` | Returns the explicit texture override or null. |
| `T GetShaderParameter<T>(string name) where T : unmanaged` | Reads a matching scalar/vector. |
| `T[] GetShaderParameterArray<T>(string name) where T : unmanaged` | Returns a copy of a complete fixed-size array. |

## Property descriptions

### Shader

Assignment borrows a live program. Assigning the same instance is a no-op; a changed reference emits `Changed`. A disposed material or supplied Shader causes `ObjectDisposedException`. Shader code may later be replaced through `Shader.SetSPIRV`; code/layout publication and the next material migration use whole versions. A disposed borrowed Shader makes parameter access or rendering fail explicitly.

## Method descriptions

### SetShaderParameter

Names are nonblank and case-sensitive. Supported types currently include float, int, uint, Vector2, Vector4, Color, Vector2I and Vector4I, subject to the reflected element type. Color aliases float4 without implicit gamma conversion. Every floating component must be finite. Array calls require exactly the reflected array length and copy into internal padded storage.

Texture overloads require a reflected sampled image name and a live Texture, or null to use the Shader default. Missing defaults or unreadable/disposed textures fail before rendering. Texture updates and changes to bindings affect later frames without redrawing geometry.

The full request is validated before writing, including all array elements. A failed update preserves previous values. A successful update emits `Changed` and takes effect on later draws without `QueueRedraw`. Callback failure propagates after committed mutation. No Shader causes `InvalidOperationException`; bad names/types/shapes/lengths/nonfinite values cause `ArgumentException`; disposal causes `ObjectDisposedException`. The generic unmanaged constraint does not imply arbitrary unmanaged types are accepted.

### GetShaderParameter

The generic overload returns the matching scalar/vector value, initially zero. The nongeneric overload returns the explicit borrowed texture override, or null when the Shader default applies. The type and scalar shape must match. Uses the same name, missing-Shader and disposal checks as setters. No mutable backing storage is returned.

### GetShaderParameterArray

Returns an independent `T[]` containing all elements without buffer padding. Caller writes cannot mutate the material. Type/array shape, missing-Shader and disposal checks match setters. This convenience reader allocates an array; scalar/vector updates and renderer capture do not.

## Tooling and protected hooks

Inherited property discovery includes Shader and typed parameter descriptors named `shader_parameter/<uniform>`. Their zero revert values and setters share the parameter API. `Shader.GetShaderUniformList()` offers descriptors with unprefixed uniform names for explicit shader inspection.

`CreateDuplicateInstance()` creates a fresh ShaderMaterial. `CopyCustomStateTo(Resource, bool, DeepDuplicateMode, Func<Resource?, Resource?>, Func<Resource?, Resource?>)` independently copies all parameter buffers and applies the resource graph policy to Shader and texture overrides, preserving aliases shared with Shader defaults. `GetPropertyDescriptors()` supplies the stored Shader and parameter descriptors. `Dispose(bool)` clears borrowed/program references and value storage before base cleanup. Resource path, identity and local-scene rules remain inherited.

## Verification and remaining work

[RenderingRuntimeTests.cs](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) checks named typed access, failure atomicity, independent copies and returned arrays, property discovery, cross-layout reload, disposal and zero allocations over warmed repeated updates. Four GPU frames per imported language check float/int/uint/vector/array values, distinct materials sharing a shader, and image changes without drawing-command regeneration. Native coverage is Linux Wayland/Vulkan only. Texture arrays and sampler configuration, matrices, nested structures, boolean/other numeric mappings and per-instance parameters remain unimplemented; see [the component limits](../components/shader-materials.md).
