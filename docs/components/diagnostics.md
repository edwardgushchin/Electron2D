# Runtime diagnostics

Last updated: 2026-10-15

Logger receives the engine diagnostic stream through static OS.AddLogger/RemoveLogger registration. OS.LogMessage is a typed message ingress; OS.LogError supplies classification, expression/rationale and compiler caller information. Native SDL logs join the same stream after the selected native core loads. Engine lifecycle failures retain their existing exceptions and cleanup behavior while reporting structured C# metadata.

## Delivery and ownership

Registration retains a borrowed Logger until removal or disposal and deduplicates identity. Copy-on-write receiver snapshots avoid holding a registry lock across callbacks. A removal affects subsequent sends; an already selected live receiver may receive the current message. Callbacks execute synchronously on sending threads, may overlap, and must synchronize their own mutable state. Reentry prints permitted output without recursively invoking receivers. A throwing receiver is isolated, reported through the fallback error writer, and cannot replace the original runtime exception. Disposal unregisters but does not wait for a callback already entered; callers must coordinate disposal of their own callback state.

Engine.PrintToSTDOUT and PrintErrorMessages default to true and govern physical engine output; receivers continue to observe suppressed messages. Node.PrintTree/PrintTreePretty, pack verbose reports and restart failures use the same ingress. Direct consumer Console writes remain host-owned output. Existing Console writers are never replaced or disposed. LogMessage appends a physical newline and delivers its original message string. Structured errors use the typed LoggerErrorType identities Error=0, Warning=1, Script=2 and Shader=3; runtime editorNotify remains false.

Managed-only registration/message capture does not initialize video or load SDL. The dynamic core resolver attaches native logging when the actual core becomes available. The prior sink, userdata and managed delegate root are retained coherently; output is forwarded when enabled. Final removal/default flags restore that exact sink only if the service still owns the native callback, preserving a later foreign replacement. SDL Info remains a message, Warn a warning, and Error/Critical errors. Native callbacks cannot propagate managed exceptions across the ABI. Desktop native execution must be measured; distinct statically linked and mobile bootstrap paths retain their host hooks and acceptance gates.

## C# snapshots

Engine.CaptureScriptBacktraces() returns a caller-owned array with an immutable ScriptBacktrace for the calling thread. Snapshot objects own copied filename/function/line strings and integers, without exception objects, MethodBase references or native handles. Function identities use .NET diagnostic metadata through DiagnosticMethodInfo, including NativeAOT, without requiring reflected game methods. Available symbols supply locations; builds that omit stack metadata may have empty filenames, zero lines or an explicit unavailable function marker. Format has independent checked indentation from 0 through 1024; ToString uses its defaults. Frame access checks the full index range. Stack capture and formatting allocate and are unsuitable for frame hot paths.

Error callbacks borrow a read-only span of immutable snapshot references; retaining a snapshot object is safe. C# runtime failures use exception stack metadata. Current script-language support remains C#; no other-language provider is invented. Live local/member/global value getters are excluded by the universal-value policy. Name/count inspection and a capture-locals option require an executing CLR debugger/symbol adapter and an accepted typed variable model; no empty inspectors or inert boolean are exposed. ScriptBacktrace therefore retains a precise partial coverage state.

## Example

```csharp
using Electron2D;

using var logger = new GameLogger();
OS.AddLogger(logger);
Engine.PrintToSTDOUT = false;
OS.LogMessage("Still delivered to GameLogger");
var trace = Engine.CaptureScriptBacktraces()[0];

sealed class GameLogger : Logger
{
    protected override void OnLogMessage(string message, bool error)
    {
        // Store or forward the message with application-owned synchronization.
    }
}
```

[ADR 0108](../decisions/diagnostics.md#adr-0108) owns the contract.

## Local verification (2026-10-15)

Linux managed checks exercise identity registration, descriptor-backed flags, both output streams, all error classifications, immutable known-caller metadata, index/indent validation, recursive logging, throwing receivers, mutation during delivery, 128 concurrent messages and manual frame failure/recovery. Native ABI checks exercise Info/Warn/Error/Critical classification, UTF-8, concurrent delivery, removal from a callback, exact previous sink/userdata restoration, retention of a previous managed delegate across GC and preservation of a foreign replacement.

Fresh GPU and compatibility windows deliver owner/worker messages, render and read back the authored pixel, retain the original startup failure through normal cleanup, and measure 64 warmed successful full frames at zero managed bytes on the owner thread. An independent NuGet NativeAOT consumer passes the same public managed and window profiles with IL warnings treated as errors and LD_LIBRARY_PATH unset. A separate published managed consumer without a native platform package exercises registration, output flags, snapshots and tree printing without loading SDL. These are local Linux gates; foreign hosts, debugger locals and editor notification acceptance remain unverified.

Ignored evidence is retained under bin/runtime-logging-validation/2026-10-15: checks.json, package-checks.json, individual logs, captures and aot-captures. The initial native priority and NativeAOT reflection failures are retained separately; final checks use the corrected SDL enum boundary and DiagnosticMethodInfo metadata path. No native/vendor source changes are included.
