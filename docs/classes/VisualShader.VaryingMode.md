# VisualShader.VaryingMode

Last updated: 2026-10-25

Source: [VisualShader.Varyings.cs](../../src/Scene/Resources/VisualShader.Varyings.cs).

```csharp
public enum VisualShader.VaryingMode
```

| Value | Number | Contract |
| --- | ---: | --- |
| <a id="vertextofragmentlight"></a>`VertexToFragmentLight` | 0 | Vertex writes feed fragments and their automatic scene-light composition. |
| <a id="max"></a>`Max` | 2 | Nonselectable upper bound, retaining the separate fragment-to-light role's identity. |

`AddVarying` accepts only VertexToFragmentLight. Max and undefined values throw `ArgumentOutOfRangeException` before authoring changes. The fragment-to-light selector is absent until the executable light graph producer exists. This enum controls graph stage transport; it is independent of fixed material light modes.

```csharp
graph.AddVarying("amount", VisualShader.VaryingMode.VertexToFragmentLight, VisualShaderVaryingType.Float);
```

See [typed varying transport](../components/visual-shaders.md#typed-varying-transport) for storage, interpolation, capacity and verification limits.
