# VisualShaderVaryingType

Last updated: 2026-10-25

Source: [VisualShaderVaryingType.cs](../../src/Scene/Resources/VisualShaderVaryingType.cs). Shared by VisualShader declarations and VisualShaderNodeVarying accesses.

```csharp
public enum VisualShaderVaryingType
```

## Values

| Value | Number | Contract |
| --- | ---: | --- |
| <a id="float"></a>`Float` | 0 | Single-precision scalar. |
| <a id="int"></a>`Int` | 1 | Signed 32-bit scalar with flat interpolation. |
| <a id="uint"></a>`UInt` | 2 | Unsigned 32-bit scalar with flat interpolation. |
| <a id="vector2"></a>`Vector2` | 3 | Two smoothly interpolated floating components. |
| <a id="vector3"></a>`Vector3` | 4 | Three smoothly interpolated floating components. |
| <a id="vector4"></a>`Vector4` | 5 | Four smoothly interpolated floating components. |
| <a id="boolean"></a>`Boolean` | 6 | Flat Boolean transport encoded privately as uint. |
| <a id="transform"></a>`Transform` | 7 | Numeric System.Numerics.Matrix4x4; four interpolated column vectors. |
| <a id="max"></a>`Max` | 8 | Nonselectable upper bound. |

Each scalar/vector consumes one of four native custom varying locations. Transform consumes all four; its components interpolate independently without scene-space transform semantics. Boolean, signed and unsigned data preserve flat primitive interpolation. Floating data uses ordinary smooth interpolation. Max is not selectable. No sampled-image or universal runtime value role is present.

## Example

```csharp
graph.AddVarying("numeric_basis", VisualShader.VaryingMode.VertexToFragmentLight, VisualShaderVaryingType.Transform);
```

The complete [node example](VisualShaderNodeVarying.md#example) and [transport contract](../components/visual-shaders.md#typed-varying-transport) describe ownership, import, capacity and verification.
