# Runtime diagnostics

[Object lifetime monitors](object-lifetime-monitors.md) add live object, cached-resource, attached-node and orphan-node populations without owning targets, forcing collection or opening a native device.

Last updated: 2026-10-21

[Frame timing monitors](frame-monitors.md) add four actual Engine producers: FPS and per-window peak process, physics-step and navigation durations. They retain independent cached reads and explicit reset, error, presentation and direct-host boundaries.

## Scope and owned types

[Performance](../classes/Performance.md) is the retained process-wide diagnostics service under [ADR 0095](../decisions/singleton-services.md#adr-0095). Its existing built-in [physics statistics](physics-statistics.md) read completed producer snapshots. Custom monitors supply application-defined values through direct typed callbacks; [Performance.CustomMonitor](../classes/Performance.CustomMonitor.Generic.md) identifies one registration and [MonitorType](../classes/Performance.MonitorType.md) describes presentation units.

## Runtime flow

AddCustomMonitor accepts an exact ordinal ID, a compiled Func returning a known value type and unit metadata. Typed arguments are captured by ordinary C# closures or the explicit argument overload; tuple arguments cover multiple values. Registration allocates and retains the provider without invoking it. Empty IDs and arbitrary slash-separated IDs retain literal identity; category presentation belongs to a display client.

GetCustomMonitor validates the exact registration handle under the service lock, copies its provider and invokes outside the lock on the calling thread. Results preserve numeric precision and reference identity; the service does not cache, box or convert them. This raw read contract is distinct from a future numeric display's nonnegative/clamping/formatting policy. Quantity describes integer-style presentation, Memory expects bytes, Time expects seconds for millisecond presentation, and Percentage expects a fractional ratio such as 0.5 for 50 percent. Metadata does not transform the returned value.

A thread-local identity set rejects direct and indirect cycles on one thread. Nested acyclic reads and concurrent readers remain valid. Provider exceptions propagate and release the guard. First reads and deeper nesting may allocate guard capacity; warmed reads of prepared depth allocate no engine-managed memory. Provider allocations remain application-owned.

RemoveCustomMonitor detaches the provider and captured arguments without disposing them or waiting for an in-flight read. A read that already copied its provider may finish. Stale handles reject even if the same ID is registered again. Caller-held handles retain only ID/unit metadata after removal. Application code must unregister providers whose captures have shorter lifetimes than the process; the engine does not infer delegate receiver lifetime or synchronize application state.

## Registry invariants

IDs are ordinal and unique; undefined units, null providers/IDs and duplicate registrations reject before mutation. Missing removal or stale reads throw typed exceptions. Names and types are independent copied snapshots in registration order; removing/readding an ID moves it to the end. Each query is atomic, but separate queries need application synchronization when their alignment must survive concurrent mutation.

Modification time starts at zero and records successful add/remove ticks in microseconds relative to initialization of the retained diagnostics registry. It is nondecreasing, survives native session changes and is available in headless use. Reads and failures do not change it; multiple mutations can share one microsecond, so it is not a unique revision counter. Wall-clock changes do not affect it.

## Verification and limits

[CustomMonitorTests](../../tests/Electron2D.Tests/CustomMonitorTests.cs) exercises exact Int64 and raw typed/reference values, capture arguments, units, order, copied containers, modification ticks, invalid input, stale handles, failure recovery, recursive and nested reads, in-flight removal/replacement, self-removal, captured receiver collection and zero warmed managed bytes. The public native host reads live scene position monitors and checks actual pixels and permanent service/registry identity across Linux Wayland GPU and compatibility sessions.

Other built-in producers remain separate coverage dependencies. No debugger chart UI, persistence or foreign-platform execution is supplied by the custom-monitor API. Native/driver allocations and human acceptance remain unverified.
