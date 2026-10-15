# Runtime diagnostics decisions

Last updated: 2026-10-15

This bounded decision owns process diagnostic delivery and immutable compiled C# stack snapshots. Current behavior belongs to the diagnostics component and class pages.

<a id="adr-0108"></a>
## ADR 0108: Deliver typed runtime diagnostics through retained logger registrations

- Status: Accepted under the standing complete-API implementation goal.
- Refines [ADR 0016](core-object-runtime.md#adr-0016), [ADR 0004](product.md#adr-0004), [ADR 0091](scripting.md#adr-0091), and [ADR 0095](singleton-services.md#adr-0095).
- Preserves [ADR 0014](resources.md#adr-0014) and the host gates in [ADR 0021](product.md#adr-0021).

### Decision

Logger derives from ElectronObject and supplies protected virtual OnLogMessage and OnLogError callbacks. LoggerErrorType preserves Error=0, Warning=1, Script=2 and Shader=3. OS.AddLogger/RemoveLogger are static operations over one retained internal registration service. Registrations retain logger references until removal or disposal, deduplicate by identity and publish copy-on-write dispatch snapshots. Callbacks may execute concurrently on sending threads; no runtime registry lock is held across user code. Mutation during a callback affects later messages. Recursive logging and callback exceptions may reach the ordinary console but cannot recursively deliver callbacks or replace the original engine failure. No diagnostic callback exception crosses a native callback boundary.

OS.LogMessage and OS.LogError are the typed CLR ingress for engine diagnostic messages and structured errors. They supply the callable language-independent print/error role without a Variant argument array or additional global class. Engine.PrintToSTDOUT and PrintErrorMessages gate physical engine/native output, while registered loggers continue to receive diagnostics. Host-owned Console writers remain ordinary C# objects; the engine neither replaces nor disposes them. Node.PrintTree/PrintTreePretty and runtime diagnostic writes use the common ingress. Arbitrary consumer Console.Write calls remain host output outside the engine stream.

Engine's manual frame and owned window-run error boundaries report exceptions with immutable C# backtraces while preserving the same exception, owner-state recovery and cleanup behavior. Manual frames and windowed runs each report a failure once. Diagnostics must never turn an observed callback failure into successful execution. Shutdown errors remain separately observable. No AppDomain unhandled-exception interception or process-wide console redirection is introduced.

The SDL core resolver notifies the diagnostics service when the selected native core becomes available. Only then may registered loggers or nondefault output flags install a native log callback. This preserves managed-only diagnostics without a native package and avoids initializing video to log. The service retains and forwards the previous native output callback, gates its physical output, and restores it on final unregistration/default flags only if it still owns the installed callback. Native errors/warnings become structured Error/Warning messages; other native messages retain their ordinary message role. Platforms with a distinct statically linked core require their actual host hook before native-log acceptance; they must not claim desktop native interception from a managed test.

Engine.CaptureScriptBacktraces() returns a caller-owned array containing an immutable ScriptBacktrace snapshot for the calling C# thread. This is a data-result class with ordinary managed lifetime, no mutable engine identity or borrowed native handle. Frame filename/line use available .NET debug information; absent symbols yield empty filename/zero line. Function names use available runtime stack metadata, with explicit unavailable text when metadata is absent. It does not scan assemblies, invoke reflected game members, load scripts, or retain MethodBase/exception objects. Snapshots format deterministically and validate frame indices and indentation. Error callbacks receive borrowed read-only spans of immutable snapshots that may safely be retained as objects.

The reference variable-value APIs remain excluded by the universal-value boundary. Variable count/name inspection and the capture-locals option require an executing CLR debugger/symbol adapter and a separately accepted typed variable projection. They are not represented by constant-zero getters or an inert boolean. The metadata-only CaptureScriptBacktraces overload omits that unavailable option. These boundaries are recorded row by row in coverage; ScriptBacktrace is not claimed complete while debugger inspection is absent.

This is synchronous, allocating diagnostic work. Registration, formatting and stack capture do not run on warmed successful frames. Native output restoration, reentry, throwing/disposed loggers, multithreaded delivery, stdout/stderr flags, manual and native window failures, trimmed/NativeAOT metadata and fresh-process consumers require executable checks. Foreign hosts, debugger locals, editor notifications and user acceptance remain distinct gates.
