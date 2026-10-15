# ScriptBacktrace

Last updated: 2026-10-15

**Inherits:** ordinary managed data/enum lifetime

- **Source:** [`src/Core/Diagnostics/ScriptBacktrace.cs`](../../src/Core/Diagnostics/ScriptBacktrace.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class Electron2D.ScriptBacktrace`

> Immutable available C# stack metadata with checked access and formatting.

See [runtime diagnostics](../components/diagnostics.md) for threading, ownership, physical output and metadata limits.

## API

- `public System.String Format(System.Int32 indentAll = 0, System.Int32 indentFrames = 4)`
- `public System.Int32 GetFrameCount()`
- `public System.String GetFrameFile(System.Int32 index)`
- `public System.String GetFrameFunction(System.Int32 index)`
- `public System.Int32 GetFrameLine(System.Int32 index)`
- `public System.String GetLanguageName()`
- `public System.Boolean IsEmpty()`
- `public override System.String ToString()`
