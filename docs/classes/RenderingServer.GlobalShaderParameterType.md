# RenderingServer.GlobalShaderParameterType

Last updated: 2026-10-16

**Namespace:** Electron2D. **Declaration:** public enum Electron2D.RenderingServer.GlobalShaderParameterType. **Source:** [RenderingServer.GlobalParameters.cs](../../src/Servers/Rendering/RenderingServer.GlobalParameters.cs). **Component:** [Global shader parameters](../components/global-shader-parameters.md).

## Description

Identifies the concrete logical role of a renderer-session global. Add registers its role and an exact typed carrier; GetType reports it. Unknown type values and Max cannot be added. Max is the absent-name result. Numeric identities retain reserved gaps; no three-dimensional role or external sampler is exposed.

## Example

```csharp
RenderingServer.GlobalShaderParameterAdd("wind", RenderingServer.GlobalShaderParameterType.Vec2, new Vector2(4, 0));
RenderingServer.GlobalShaderParameterSetOverride<Vector2>("wind", new Vector2(8, 0));
RenderingServer.GlobalShaderParameterSetOverride<Vector2>("wind", null);
```

Use this from a live renderer owner, such as Window.Ready. A Globals shader block reads wind; existing materials share its value.

## API and member descriptions

| Declaration | Meaning |
| --- | --- |
| `public const RenderingServer.GlobalShaderParameterType Bool = 0` | Boolean scalar. |
| `public const RenderingServer.GlobalShaderParameterType BVec2 = 1` | Two boolean components carried by an integer mask. |
| `public const RenderingServer.GlobalShaderParameterType BVec3 = 2` | Three boolean components carried by an integer mask. |
| `public const RenderingServer.GlobalShaderParameterType BVec4 = 3` | Four boolean components carried by an integer mask. |
| `public const RenderingServer.GlobalShaderParameterType Int = 4` | Signed integer scalar. |
| `public const RenderingServer.GlobalShaderParameterType IVec2 = 5` | Two signed integer components. |
| `public const RenderingServer.GlobalShaderParameterType IVec3 = 6` | Three signed integer components. |
| `public const RenderingServer.GlobalShaderParameterType IVec4 = 7` | Four signed integer components. |
| `public const RenderingServer.GlobalShaderParameterType Rect2i = 8` | Integer position and size. |
| `public const RenderingServer.GlobalShaderParameterType UInt = 9` | Unsigned integer scalar. |
| `public const RenderingServer.GlobalShaderParameterType UVec2 = 10` | Two unsigned component bit patterns. |
| `public const RenderingServer.GlobalShaderParameterType UVec3 = 11` | Three unsigned component bit patterns. |
| `public const RenderingServer.GlobalShaderParameterType UVec4 = 12` | Four unsigned component bit patterns. |
| `public const RenderingServer.GlobalShaderParameterType Float = 13` | Single-precision scalar. |
| `public const RenderingServer.GlobalShaderParameterType Vec2 = 14` | Two floating-point components. |
| `public const RenderingServer.GlobalShaderParameterType Vec3 = 15` | Three numeric floating-point components. |
| `public const RenderingServer.GlobalShaderParameterType Vec4 = 16` | Four numeric floating-point components. |
| `public const RenderingServer.GlobalShaderParameterType Color = 17` | Literal RGBA components. |
| `public const RenderingServer.GlobalShaderParameterType Rect2 = 18` | Floating-point position and size. |
| `public const RenderingServer.GlobalShaderParameterType Mat2 = 19` | Two-column numeric basis carried by Transform. |
| `public const RenderingServer.GlobalShaderParameterType Mat3 = 20` | Nine column-major numeric matrix elements. |
| `public const RenderingServer.GlobalShaderParameterType Mat4 = 21` | Sixteen column-major numeric matrix elements. |
| `public const RenderingServer.GlobalShaderParameterType Transform2D = 22` | Planar basis and origin carried by Transform. |
| `public const RenderingServer.GlobalShaderParameterType Sampler2D = 24` | Borrowed ordinary image. |
| `public const RenderingServer.GlobalShaderParameterType Sampler2DArray = 25` | Borrowed image array. |
| `public const RenderingServer.GlobalShaderParameterType Max = 29` | Sentinel returned for an absent declaration. |

The component documents exact C# carriers, copies, base/override behavior, errors, ownership and verification. This enum controls actual shader consumers, rather than an independent data registry.
