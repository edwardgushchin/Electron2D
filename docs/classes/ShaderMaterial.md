# ShaderMaterial

Last updated: 2026-09-23

- Declaration: `public sealed class ShaderMaterial : Material`
- Source: [Material.cs](../../src/Scene/Resources/Material.cs)
- Inherits: [Material](Material.md), [Resource](Resource.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

Pairs a borrowed Shader with independently owned parameter values and borrowed texture overrides. Multiple materials may share one Shader and hold different values; nodes sharing one material see the same updates. Null Shader selects ordinary canvas color drawing. Any assigned Shader requires GPU rendering. Material disposal clears its own state and never disposes the borrowed Shader or textures.

Reserved TIME is not a user parameter: getters, setters, property discovery, duplication and value migration exclude it. The renderer fills its reflected float32 location immediately before each buffer upload without emitting Changed.

State operations are serialized per material. Native uniform upload uses the same gate. Notifications run after mutation outside the gate. Successful scalar/vector/matrix or span updates reuse initialized storage. Matrices initially contain identity; other stored components contain zero. Float3 Color readers reconstruct alpha one; float2x2 Transform readers reconstruct zero Origin. No source defaults are extracted. Shader replacement migrates matching names, shader element types (including integer signedness) and array lengths to new offsets/bindings on the next parameter access or draw. Incompatible or newly introduced members start at their typed defaults; assigning null discards values.

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
| `void SetShaderParameter<T>(string name, T value) where T : unmanaged` | Sets a matching scalar/vector/matrix. |
| `void SetShaderParameter<T>(string name, ReadOnlySpan<T> values) where T : unmanaged` | Copies a complete fixed-size array. |
| `void SetShaderParameter(string name, Texture? texture)` | Sets a borrowed texture override; null uses the Shader default. |
| `Texture? GetShaderParameter(string name)` | Returns the explicit texture override or null. |
| `T GetShaderParameter<T>(string name) where T : unmanaged` | Reads a matching scalar/vector/matrix. |
| `T[] GetShaderParameterArray<T>(string name) where T : unmanaged` | Returns a copy of a complete fixed-size array. |

## Property descriptions

### Shader

Assignment borrows a live program. Assigning the same instance is a no-op; a changed reference emits `Changed`. A disposed material or supplied Shader causes `ObjectDisposedException`. Shader code may later be replaced through `Shader.SetSPIRV`; code/layout publication and the next material migration use whole versions. A disposed borrowed Shader makes parameter access or rendering fail explicitly.

## Method descriptions

### SetShaderParameter

Names are nonblank and case-sensitive. Supported types currently include float, int, uint, Vector2, Vector4, Color, Rect, Transform, Vector2I and Vector4I, subject to the reflected element type. Color supplies RGB to float3 or RGBA to float4 without implicit gamma conversion. Float3 ignores alpha when storing; reads always reconstruct alpha one. Rect aliases float4 as position.x, position.y, size.x, size.y, including negative sizes. Vector2I and Vector4I carry either signed or unsigned shader vectors with unchanged 32-bit component patterns: -1 supplies 0xffffffffu. Transform supplies X/Y basis columns to float2x2, omits Origin, and reads it back as zero. Row/column storage order and matrix/array strides are reflected automatically. Every floating component must be finite, including unused Color alpha and Transform Origin. Array calls require exactly the reflected array length and copy into internal padded storage.

Texture overloads require a reflected sampled image name and a live Texture, or null to use the Shader default. Missing defaults or unreadable/disposed textures fail before rendering. Texture updates and changes to bindings affect later frames without redrawing geometry. Named samplers use linear filtering, clamp-to-edge coordinates and LOD zero independently of CanvasItem/Viewport properties. Configurable named sampler state remains pending; see [the verified defaults](../components/shader-materials.md#named-sampler-defaults).

The full request is validated before writing, including all array elements. A failed update preserves previous values. A successful update emits `Changed` and takes effect on later draws without `QueueRedraw`. Callback failure propagates after committed mutation. No Shader causes `InvalidOperationException`; bad names/types/shapes/lengths/nonfinite values cause `ArgumentException`; disposal causes `ObjectDisposedException`. The generic unmanaged constraint does not imply arbitrary unmanaged types are accepted.

### GetShaderParameter

The generic overload returns the matching scalar/vector/matrix value, initially identity for float2x2 and zero for other stored components (alpha one for float3 Color). Transform Origin is always zero. The nongeneric overload returns the explicit borrowed texture override, or null when the Shader default applies. The type and scalar shape must match. Uses the same name, missing-Shader and disposal checks as setters. No mutable backing storage is returned.

### GetShaderParameterArray

Returns an independent `T[]` containing all elements without buffer padding. Caller writes cannot mutate the material. Type/array shape, missing-Shader and disposal checks match setters. This convenience reader allocates an array; scalar/vector/matrix updates and renderer capture do not.

## Tooling and protected hooks

Inherited property discovery includes Shader and typed parameter descriptors named `shader_parameter/<uniform>`. Their typed revert values (identity for float2x2, zero components otherwise, alpha one for float3 Color) and setters share the parameter API. `Shader.GetShaderUniformList()` offers descriptors with unprefixed uniform names for explicit shader inspection.

`CreateDuplicateInstance()` creates a fresh ShaderMaterial. `CopyCustomStateTo(Resource, bool, DeepDuplicateMode, Func<Resource?, Resource?>, Func<Resource?, Resource?>)` independently copies all parameter buffers and applies the resource graph policy to Shader and texture overrides, preserving aliases shared with Shader defaults. `GetPropertyDescriptors()` supplies the stored Shader and parameter descriptors. `Dispose(bool)` clears borrowed/program references and value storage before base cleanup. Resource path, identity and local-scene rules remain inherited.

## Verification and remaining work

[RenderingRuntimeTests.cs](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) checks named typed access, failure atomicity, independent copies and returned arrays, property discovery, cross-layout reload, disposal and zero allocations over warmed repeated updates. Four GPU frames per imported language check float/int/uint/vector/array values, distinct materials sharing a shader, and image changes without drawing-command regeneration. [ShaderVectorRenderingTests](../../tests/Electron2D.Tests/ShaderVectorRenderingTests.cs) covers RGB/Rect and unsigned vectors, scalar and arrays, defaults/descriptors, adjacent-field integrity, failures, copies, signedness resets, two-language pixels, shared materials, layout reload and warmed allocation checks. [ShaderMatrixRenderingTests](../../tests/Electron2D.Tests/ShaderMatrixRenderingTests.cs) verifies matrix storage order/stride, identity defaults versus explicit zero, Origin omission, arrays, copying, reload, invalid layouts and GPU pixels for both source languages. Native coverage is Linux Wayland/Vulkan only. Texture arrays and sampler configuration, matrices other than float2x2, nested structures, boolean/other numeric mappings and per-instance parameters remain unimplemented; see [the component limits](../components/shader-materials.md).
