# Shader.Mode

Last updated: 2026-09-22

- Declaration: `public enum Shader.Mode`
- Source: [Shader.cs](../../src/Scene/Resources/Shader.cs)
- Owner: [Shader](Shader.md)

## Values

| Value | Integer | Meaning |
| --- | --- | --- |
| `CanvasItem` | 1 | A fragment shader applied to 2D canvas geometry. |

`Shader.GetMode()` returns this value for every currently constructible shader. Other shader domains have no executable implementation. `RenderingRuntimeTests.VerifyResources` checks the result.
