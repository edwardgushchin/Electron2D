# Text and structured diagnostic roles

The pinned source is [4.7.2-stable @GlobalScope.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/@GlobalScope.xml), revision `ed1daf0bf001b61586d9930840f2f1394092c079`.
The eight audited roles use the typed ingress accepted by [ADR 0108](../decisions/diagnostics.md#adr-0108).

| Reference role | Pinned behavior | Exact typed composition and delivery |
| --- | --- | --- |
| print | Concatenated values to stdout, newline | `OS.LogMessage(string.Concat(parts))` |
| printerr | Concatenated values to stderr, newline; ordinary message | `OS.LogMessage(string.Concat(parts), error: true)` |
| prints | One space between values, stdout, newline | `OS.LogMessage(string.Join(" ", parts))` |
| printt | One tab between values, stdout, newline | `OS.LogMessage(string.Join("\t", parts))` |
| printraw | Concatenated values to stdout without a newline | `OS.LogRaw(string.Concat(parts))` |
| print_rich | Terminal markup converted to ANSI with a final reset and newline | `OS.LogRich(string.Concat(parts))` |
| push_error | Structured error to diagnostic receivers and terminal, continuing execution | `OS.LogError(string.Concat(parts), LoggerErrorType.Error)` |
| push_warning | Structured warning to diagnostic receivers and terminal | `OS.LogError(string.Concat(parts), LoggerErrorType.Warning)` |

`parts` contains caller-formatted strings. C# formatting and composition replace dynamic argument conversion under ADR 0108; no Variant, object-array formatter or global facade is added. Empty composition is an empty message. Line modes still add a physical newline, while raw writes zero characters. Embedded newlines remain intact. Logger message callbacks receive exactly the composed text and stream flag, without the appended newline. Structured callbacks receive rationale, optional code, class and origin separately. Caller attributes identify the C# call site; immutable C# backtraces describe available runtime frames. A wrapper must forward metadata to preserve its caller's origin. The runtime host sends `editorNotify=false`; an editor debugger is a separate host under ADR 0091. These are accepted typed/runtime adaptations, not a claim of editor debugger integration.

The physical structured line includes classification and available file/line/function. Both classes obey the stderr gate. All four stdout/stderr gate combinations preserve callback delivery. Direct consumer Console writes stay outside that gate. Writer failures propagate after callback delivery; throwing callbacks are isolated by the common ingress. Registration retains borrowed identities, deduplicates them, and unregisters on disposal without owning Console writers.

## Verification

`tests/Diagnostics.Consumer` uses only the public runtime API for the text profile and reuses the existing diagnostics profiles. `text` checks 72 text/gate cases (empty, single and multiple strings with empty fields, Unicode and embedded newline; three separators; two streams; four flag combinations), 24 structured/class/gate cases (empty, Unicode and multiline rationale) with compiler origin, and five write/flush failures. Exact physical text and callback payloads are compared separately. Host writer identity and non-disposal are checked after registration/removal. Existing `managed` checks cover reentry, throwing callbacks, mutation, disposal, concurrent sends, immutable backtraces and frame failure recovery; `portable`, `output`, `verbose`, `gpu` and `compatibility` retain their original contracts; `DiagnosticsNativeTests` runs in the root test executable because it needs internal backend access. Execution results and versions belong to the task QA receipt, not the list of available profiles.

[Pinned terminal implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/string/print_string.cpp#L105-L322) supplies the tag expansion, reset and raw write reference. Electron2D adapts raw callback delivery to the common diagnostic ingress and omits generated ANSI for plain output. Rich callbacks retain the original markup. See [the runtime output policy](../components/diagnostics.md#raw-and-rich-stdout). The public `raw-rich` profile checks exact outputs and callback payloads and the wire check compares UTF-8 bytes for redirected stdout. This audit does not qualify foreign native packages, editor notification, or full ADR 0021 platform acceptance.
