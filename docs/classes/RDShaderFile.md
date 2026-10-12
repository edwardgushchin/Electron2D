# RDShaderFile

Last updated: 2026-10-12

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RDShaderFile`. **Source:** [RDShaderFile.cs](../../src/Servers/Rendering/Device/RDShaderFile.cs). **Component:** [Local device](../components/local-compute.md#named-compiled-shader-versions).

**Inherits:** [Resource](Resource.md).

## Description

A named collection of borrowed RDShaderSPIRV import results. Names are case-sensitive valid nonzero Unicode scalar sequences; an empty name selects the default. A missing name returns null, with no fallback to another version. Module identity and stage diagnostics are preserved. BaseError is a separate import diagnostic and does not hide versions. Successful assignments publish Changed after commitment, including equal assignments; listener failure retains the committed edit and listeners may reenter ordinary authoring.

Replacement and disposal release authored references without disposing caller-supplied borrowed modules. File-loaded graph ownership follows Resource and can close loaded children when its last graph owner releases. A selected disposed module rejects on lookup; its name remains listed and can be replaced. Individual operations serialize storage, while callers synchronize multi-step authoring/file saving. Version-list queries and first-name authoring are cold allocation boundaries; warmed existing-key writes and lookup reuse prepared storage. Names sort by Unicode scalar value, independently of culture and UTF-16 surrogate ordering.

Typed resource graphs store paired names/module references, preserving aliases and all diagnostics. Paired replacement validates before committing the version map. Shallow duplication borrows modules, deep duplication copies eligible resources with shared aliases, and local-scene copies follow each resource's ordinary local policy. Compiled resources remain mutable through their own API. Selecting a version supplies its current module to an explicit RenderingDevice factory; existing device shader identities retain their created snapshot. This collection does not compile source or create native identities.

## Example

Partial snippet requiring a live local or renderer-retained device and a saved compiled resource.

```csharp
using var versions = ResourceLoader.Load<RDShaderFile>("res://effects.e2dres");
var module = versions.GetSPIRV("HLSL") ?? throw new InvalidOperationException("Version absent.");
var shader = device.ShaderCreateFromSPIRV(module);
```

## Complete API

| Complete signature | Contract |
| --- | --- |
| `public RDShaderFile()` | Creates an empty version collection with no import diagnostic. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` |  |
| `protected override Electron2D.Resource CreateDuplicateInstance()` |  |
| `protected override System.Void Dispose(System.Boolean disposing)` |  |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` |  |
| `public Electron2D.RDShaderSPIRV GetSPIRV(System.String version = "")` | Returns the borrowed module associated with an exact version name. |
| `public System.String[] GetVersionList()` | Returns an independent sorted list of current version names. |
| `public System.Void SetBytecode(Electron2D.RDShaderSPIRV bytecode, System.String version = "")` | Associates a live compiled resource with an exact version name. |
| `public System.String BaseError { get; set; }` | Gets or sets the import-level diagnostic independently of stage results. |

## Member descriptions

### RDShaderFile

`public RDShaderFile()`

summary: Creates an empty version collection with no import diagnostic.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

### GetSPIRV

`public Electron2D.RDShaderSPIRV GetSPIRV(System.String version = "")`

summary: Returns the borrowed module associated with an exact version name.

version: Case-sensitive version name; empty selects the default.

returns: The same supplied module, or null when that name is absent.

T:System.ArgumentNullException: The name is null.

T:System.ArgumentException: The name contains a zero character or invalid Unicode.

T:System.ObjectDisposedException: This resource or the selected borrowed module is disposed.

### GetVersionList

`public System.String[] GetVersionList()`

summary: Returns an independent sorted list of current version names.

returns: Names in Unicode scalar order, including an empty default name when present.

remarks: This is a cold copying query. Disposing a borrowed module does not remove its name.

### SetBytecode

`public System.Void SetBytecode(Electron2D.RDShaderSPIRV bytecode, System.String version = "")`

summary: Associates a live compiled resource with an exact version name.

bytecode: Borrowed live module; its stage bytecode and diagnostics are preserved.

version: Case-sensitive version name; empty selects the default.

remarks: Replacing a version retains the supplied module identity and does not dispose its predecessor. Every successful assignment publishes Changed after commitment, including an equal assignment. Replacements of prepared names allocate no engine-managed storage.

T:System.ArgumentNullException: The module or name is null.

T:System.ArgumentException: The name contains a zero character or invalid Unicode.

T:System.ObjectDisposedException: This resource or the module is disposed.

### BaseError

`public System.String BaseError { get; set; }`

summary: Gets or sets the import-level diagnostic independently of stage results.

value: Empty initially. A diagnostic does not hide versions already supplied by the importer.

T:System.ArgumentNullException: The value is null.

## Verification and limits

[RDShaderFileTests](../../tests/Electron2D.Tests/RDShaderFileTests.cs) checks borrowed identity, Unicode/non-BMP ordering, absent/exact names, errors and reentry, shallow/deep/local aliases, paired storage validation and fresh-process file loading. Saved GLSL/HLSL compute versions execute native GPU results and prepared allocation checks; a saved vertex/fragment version renders actual target pixels. Cold compiler invocation, native allocator totals, other platforms and user acceptance remain separate boundaries. Source compilation uses the existing explicit RDShaderSource import bridge; this collection adds no source language or compiler dependency.

Relevant decisions: [typed resources](../decisions/resources.md#adr-0013), [resource lifetime](../decisions/resources.md#adr-0014) and [compiled shader contract](../decisions/rendering.md#adr-0028).
