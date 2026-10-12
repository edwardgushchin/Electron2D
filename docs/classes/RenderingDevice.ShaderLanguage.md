# RenderingDevice.ShaderLanguage

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.ShaderLanguage`. **Component:** [Source authoring](../components/local-compute.md#source-authoring-and-build-imports).

## Description

Identifies the two supported authoring languages.



The source family supports the executing vertex, fragment and compute roles. GLSL is numeric zero and the default; HLSL is numeric one. Empty stage strings omit those stages. Null strings and invalid stage/language values reject. Typed property metadata/reversion preserves defaults. Tessellation requires its backend stage adapter; ray stages remain outside the product boundary.

## Example

The authoring host configures the separate ShaderImport executable; the resulting resource follows ordinary shader/pipeline creation.

```csharp
using var source = new RDShaderSource
{
    Language = RenderingDevice.ShaderLanguage.GLSL,
    SourceCompute = "#version 450\nlayout(local_size_x=1) in;\nvoid main() {}"
};
using var compiled = device.ShaderCompileSPIRVFromSource(source);
if (compiled.CompileErrorCompute.Length != 0)
    throw new InvalidOperationException(compiled.CompileErrorCompute);
var shader = device.ShaderCreateFromSPIRV(compiled);
var pipeline = device.ComputePipelineCreate(shader);
```

## API

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.RenderingDevice.ShaderLanguage GLSL = 0`](#member-0f6356e6477a) | GLSL source compiled by the pinned import frontend. |
| [`public const Electron2D.RenderingDevice.ShaderLanguage HLSL = 1`](#member-927686b88b30) | HLSL source compiled by the pinned import frontend. |

## Member descriptions

<a id="member-0f6356e6477a"></a>
### GLSL

```csharp
public const Electron2D.RenderingDevice.ShaderLanguage GLSL = 0
```

GLSL source compiled by the pinned import frontend.

<a id="member-927686b88b30"></a>
### HLSL

```csharp
public const Electron2D.RenderingDevice.ShaderLanguage HLSL = 1
```

HLSL source compiled by the pinned import frontend.

## Compilation, ownership and verification

Descriptions are snapshotted at authoring; returned stage bytes are independent of descriptions, cache storage and later mutations/disposal. The compiler returns stage diagnostics and preserves successful siblings; native creation retains program-role/interface/capability checks. Source compilation is a cold operation through an optional installed tool and is not required by games loading compiled assets. [RenderingDeviceSourceTests](../../tests/Electron2D.Tests/RenderingDeviceSourceTests.cs) covers defaults, errors, copied/cache results, include changes, real GPU compute/graphics/canvas and prepared zero managed recording on Linux/Vulkan. See [the component contract](../components/local-compute.md#source-authoring-and-build-imports) for bounds, cache bypass, toolkit delivery and unverified platforms/native allocations.
