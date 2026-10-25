# VisualShader.VaryingMode

Last updated: 2026-10-25

Source: [VisualShader.Varyings.cs](../../src/Scene/Resources/VisualShader.Varyings.cs).

```csharp
public enum VisualShader.VaryingMode
```

| Value | Number | Contract |
| --- | ---: | --- |
| <a id="vertextofragmentlight"></a>`VertexToFragmentLight` | 0 | Vertex writes feed fragments and their authored or automatic scene-light composition. |
| <a id="fragmenttolight"></a>`FragmentToLight` | 1 | Fragment writes feed per-light evaluations of that same fragment through private values. |
| <a id="max"></a>`Max` | 2 | Nonselectable upper bound. |

`AddVarying` accepts both producer roles. Max and undefined values throw `ArgumentOutOfRangeException` before authoring changes. FragmentToLight uses no native varying locations; named setters require the selected producer, and reads require that stage or a later canvas stage. This enum controls graph stage transport; it is independent of fixed material light modes.

```csharp
graph.AddVarying("amount", VisualShader.VaryingMode.VertexToFragmentLight, VisualShaderVaryingType.Float);
```

See [typed varying transport](../components/visual-shaders.md#typed-varying-transport) for storage, interpolation, capacity and verification limits.
