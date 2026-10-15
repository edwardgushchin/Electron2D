# Logger

Last updated: 2026-10-15

**Inherits:** [ElectronObject](ElectronObject.md)

- **Source:** [`src/Core/Diagnostics/Logger.cs`](../../src/Core/Diagnostics/Logger.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Electron2D.Logger`

> Receives synchronous engine diagnostics through retained OS registration.

See [runtime diagnostics](../components/diagnostics.md) for threading, ownership, physical output and metadata limits.

## API

- `public Logger()`
- `protected override System.Void Dispose(System.Boolean disposing)`
- `protected virtual System.Void OnLogError(System.String function, System.String file, System.Int32 line, System.String code, System.String rationale, System.Boolean editorNotify, Electron2D.LoggerErrorType errorType, System.ReadOnlySpan<Electron2D.ScriptBacktrace> scriptBacktraces)`
- `protected virtual System.Void OnLogMessage(System.String message, System.Boolean error)`
