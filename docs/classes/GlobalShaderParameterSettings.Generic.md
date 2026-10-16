# GlobalShaderParameterSettings<T>

Last updated: 2026-10-17

**Namespace:** `Electron2D`. **Declaration:** `public sealed class GlobalShaderParameterSettings<T> where T : notnull`.
**Inherits:** —. **Inherited By:** —.
**Source:** [GlobalShaderParameterSettings.cs](../../src/Servers/Rendering/GlobalShaderParameterSettings.cs).
**Component:** [Global shader parameters](../components/global-shader-parameters.md#project-authored-globals).

Version-one project value for one named shader global. Register a
`ProjectSetting<GlobalShaderParameterSettings<T>>` under `shader_globals/name` before
opening the renderer. This is a serialized value, never an engine-object container.

## Description

The renderer snapshots registered definitions and active feature overrides before
scene readiness. Setting edits take effect on the next session. Project storage
copies mutable values; a returned matrix can be edited without changing saved state.
Names and roles follow the ordinary global registry. A malformed role/carrier/value,
unsupported version or missing/wrong-family image fails preparation before the service publishes.
Sampled format and native device limits validate when a material consumes the image.
Unregistered settings remain preserved until the compiled owner registers their definitions.

## Example

```csharp
var gain = new ProjectSetting<GlobalShaderParameterSettings<float>>(
    "shader_globals/gain", new() { Type = RenderingServer.GlobalShaderParameterType.Float, Value = 1f });
ProjectSettings.Register(gain);
ProjectSettings.Set(gain, new() { Type = RenderingServer.GlobalShaderParameterType.Float, Value = .5f });
ProjectSettings.SaveCustom("project-globals.cfg");
```

A consuming startup registers the same definitions and loads the saved configuration
before Engine.Run. Built-in serializer metadata covers the supported closed types in NativeAOT.

## Constructor and properties

| Complete declaration | Contract |
| --- | --- |
| `public GlobalShaderParameterSettings()` | Creates a schema-version-one value; assign Type and its exact Value carrier. |
| `public int Version { get; init; } = 1` | Only version one is accepted at preparation. |
| `public RenderingServer.GlobalShaderParameterType Type { get; init; }` | Logical role, preserving different meanings of the same carrier. |
| `public T Value { get; init; }` | Copied numeric value or matrix; string path for an ordinary/array image. |

## Property descriptions

<a id="version"></a>
Version is serialized and validated before preparing resources. Unknown JSON members
reject through the configuration parser. Configuration decode failures preserve the old registry.

<a id="type"></a>
Type must match the exact [role/carrier contract](RenderingServer.GlobalShaderParameterType.md).
The supported closed carriers are bool, int, uint, float, Vector2i/Vector3i/Vector4i,
Rect2i, Vector2/Vector3/Vector4, Color, Rect2, Transform, float[] and string.
Max, reserved or unsupported roles cannot create globals.

<a id="value"></a>
Numeric values must be finite. Mat3/Mat4 require nine/sixteen column-major elements.
For Sampler2D/Sampler2DArray the string identifies a resource file, with ordinary
registry virtual-path rules. Empty means an unassigned image; null is invalid.
One normalized path shares one object within a session. The renderer owns loaded
images until shutdown, including entries later removed; getters borrow those images.
Ordinary resources passed to public global add/set remain borrowed from their callers.

## Verification and limits

ProjectShaderGlobalsTests verifies all 25 roles, fresh-process storage, feature
precedence, image aliases and cleanup failures. Native startup checks cover the
exercised Linux backend profiles, actual shader output and restart. No settings
read enters the prepared frame loop. Foreign execution and native allocator totals
remain independent gates. [ADR 0028](../decisions/rendering.md#adr-0028) owns this
projection; [ADR 0019](../decisions/core-data-io.md#adr-0019) owns typed persistence.
