# Shader

Last updated: 2026-09-23

- Declaration: `public sealed class Shader : Resource`
- Source: [Shader.cs](../../src/Scene/Resources/Shader.cs)
- Inherits: [Resource](Resource.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

A managed canvas fragment program loaded from copied SPIR-V. HLSL and GLSL source compilation belongs to import/build tooling. Code and reflected layout are published as one immutable version; failed replacement preserves the previous version. GPU pipelines belong to the renderer and are created on demand. Resource duplication shares immutable program data safely. Disposal prevents further resource access and does not dispose materials that borrow the shader.

Runtime loading checks module structure, capabilities and reflected interfaces through the shared engine/SDL3-CS Shadercross path. Import tooling additionally runs instruction-level validation. Runtime loading does not guarantee that it detects every invalid instruction; successful reflection does not establish that arbitrary SPIR-V is valid. Translation and native program-creation failures propagate when the program is first used by a renderer.

Both creation and replacement reject payloads larger than 16 MiB, shorter than the header or not aligned to 32-bit words before allocating a copy. Subsequent structure/reflection checks operate on the owned copy so later caller mutations cannot change the checked program.

## Example

```csharp
using var shader = Shader.CreateFromSPIRV(System.IO.File.ReadAllBytes("material.spv"));
using var material = new ShaderMaterial { Shader = shader };
// The imported program declares a float4 uniform named tint.
material.SetShaderParameter("tint", Colors.White);
```

## API summary

| Declaration | Contract |
| --- | --- |
| `Shader()` | Precompiled texture-times-color fragment program. |
| `Mode GetMode()` | Returns `Mode.CanvasItem`. |
| `static Shader CreateFromSPIRV(ReadOnlySpan<byte> bytecode)` | Copies and validates a fragment program. |
| `void SetSPIRV(ReadOnlySpan<byte> bytecode)` | Validates then atomically replaces the program and emits `Changed`. |
| `byte[] GetSPIRV()` | Returns an independent copy of the compiled module. |
| `IReadOnlyList<PropertyDescriptor> GetShaderUniformList()` | Immutable typed material descriptors for the current program version. |
| `void SetDefaultTextureParameter(string name, Texture? texture, int index = 0)` | Sets or clears a borrowed default texture. |
| `Texture? GetDefaultTextureParameter(string name, int index = 0)` | Gets the borrowed default texture. |
| [Mode](Shader.Mode.md) | Canvas shader domain. |

The fragment interface optionally accepts float4 color at location 0 and float2 UV at location 1. `TEXTURE` at set 2, binding 0 receives the current command texture or white for untextured geometry. It is reserved and omitted from uniform descriptors/default-parameter APIs. An optional scalar float32 TIME member in any valid fragment uniform buffer receives the renderer clock. Its offset and binding are reflected, not fixed. TIME is omitted from material descriptors and parameter APIs; lowercase time remains an ordinary parameter. See the [shared shader interface](../components/shader-materials.md#current-shader-interface).

## Method descriptions

### Shader

Creates the embedded fragment program multiplying the command texture sample by drawing color. An assigned shader requires GPU rendering. A null `ShaderMaterial.Shader` uses ordinary canvas drawing on either backend.

### GetMode

Returns `Shader.Mode.CanvasItem` (1). Throws `ObjectDisposedException` after disposal.

### CreateFromSPIRV

Copies `bytecode` before validation. The input must contain the supported canvas fragment interface, one `main` entry point and a little-endian SPIR-V header, within 16 MiB. Invalid structure/reflection throws `ArgumentException`; unsupported interfaces/resources throw `NotSupportedException`. Native-library initialization/load failures remain visible. The complete current profile is in the component page.

### SetSPIRV

Uses the same copied-input validation and errors as creation. No mutation occurs until validation succeeds. Publication is synchronized; `Changed` runs afterwards and callback failures propagate without rolling the new program back. Existing materials migrate matching named values and texture overrides on next access; defaults for removed texture names are discarded. Calling on a disposed shader throws.

### GetSPIRV

Returns copied bytes and never exposes the backing module. The snapshot may safely outlive subsequent reload or disposal. Calling on a disposed shader throws.

### GetShaderUniformList

Returns a read-only list with exact uniform names. Descriptors use `ShaderMaterial` as owner, scalar/vector element types, `T[]` for fixed arrays, or `Texture?` for texture bindings. Float4 descriptors use `Vector4`; typed parameter calls also accept `Color`. Getter/setter delegates use the material's current matching parameter, so an incompatible shader replacement is rejected on access. The list itself remains a snapshot after reload. Reserved TEXTURE and TIME are omitted. There are no grouping hints in the current import format.

### SetDefaultTextureParameter

Validates a named sampled texture and stores a borrowed default. Null clears it. A material override wins over this default. Texture arrays are not integrated, so a nonzero index throws ArgumentOutOfRangeException. Bad names/types throw ArgumentException; a disposed shader or supplied texture throws ObjectDisposedException. Mutation precedes Changed; callback failure does not undo it. Matching named defaults survive shader reload.

### GetDefaultTextureParameter

Returns the assigned default or null, without transferring ownership. Uses the same name/index and shader-disposal checks as the setter. The returned texture remains shared.

## Protected resource hooks

`CreateDuplicateInstance()` creates a fresh Shader. `CopyCustomStateTo(Resource, bool, DeepDuplicateMode, Func<Resource?, Resource?>, Func<Resource?, Resource?>)` shares the immutable program snapshot and applies graph-copy policy to default textures; base Resource owns identity/path and graph-copy policy. `Dispose(bool)` releases the program reference and delegates base cleanup.

## Limits and checks

Runtime validation is structural and reflective; full semantic validation currently runs during import. Texture arrays and sampler configuration, matrices, nested structs, other shader modes, source preservation and editor code inspection remain unimplemented. Both language import paths, copied payloads, invalid replacement and live uniform reload have executable checks in [RenderingRuntimeTests.cs](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs); native execution has been checked on Linux Wayland/Vulkan only. See [ADR 0028](../decisions/rendering.md#adr-0028).
