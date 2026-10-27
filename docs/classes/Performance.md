# Performance

[Object lifetime monitors](../components/object-lifetime-monitors.md) add live object, cached-resource, attached-node and orphan-node populations without owning targets, forcing collection or opening a native device.

Last updated: 2026-10-27

**Declaration:** `public sealed class Performance : ElectronObject` ·
**Source:** [Performance.cs](../../src/Core/Config/Performance.cs) ·
**Component:** [Runtime diagnostics](../components/runtime-diagnostics.md).

A retained process-wide diagnostics service with static public access. Engine's
named singleton lookup returns the same borrowed object; consumer disposal or
unregistration throws without changing its identity. Construction is private.

## API

| Signature | Contract |
| --- | --- |
| `public static double GetMonitor(Performance.Monitor monitor)` | Read the selected object population, reserved-byte peak, completed frame rate, duration or physical count as a double. |
| `protected override void ValidateDisposal()` | Reject consumer disposal of the permanent service. |

The [Monitor](Performance.Monitor.md) enum exposes timing, object population, canvas, physics, navigation and audio producers. Prepared reads may run on any thread without advancing simulation, synchronizing a device or allocating managed memory. Physical selectors share [PhysicsServer.GetProcessInfo](PhysicsServer.md#statistics) snapshots; object selectors perform current weak/cache population scans under the [object lifetime contract](../components/object-lifetime-monitors.md). Undefined monitor values throw ArgumentOutOfRangeException. Aggregate physical integer overflow throws OverflowException.

[Frame timing monitors](../components/frame-monitors.md) publish per-window maxima after one unscaled Engine host second. Successful attachment resets samples; stop and failed frames retain completed values. Process timing includes presentation only for the owned Window lifecycle. Independent direct frame calls do not publish Engine timing windows. Multiple reads do not form an atomic snapshot; durations measure owner-side wall time rather than native device timestamps.

```csharp
double peakSeconds = Performance.GetMonitor(Performance.Monitor.TimeProcess);
double active = Performance.GetMonitor(Performance.Monitor.PhysicsActiveObjects);
double pairs = Performance.GetMonitor(Performance.Monitor.PhysicsCollisionPairs);
double islands = Performance.GetMonitor(Performance.Monitor.PhysicsIslandCount);
```

[Server monitor snapshots](../components/runtime-diagnostics.md#server-monitor-snapshots) add all ten aggregate and ten planar navigation counts plus cached audio output latency. [Deferred message memory](../components/runtime-diagnostics.md#deferred-message-memory) adds a process-lifetime peak of actual reserved scene message storage. Total current/peak memory and renderer memory/compilation producers remain unimplemented. Custom monitor registration/lifecycle now executes through typed handles. No unsupported selector or constant-return placeholder is exposed.
See the component for sample lifetime, verification and platform limits.

## Custom monitors

| Signature | Contract |
| --- | --- |
| `public static CustomMonitor<TValue> AddCustomMonitor<TValue>(string id, Func<TValue> callback, MonitorType type = MonitorType.Quantity)` | Register a retained compiled provider without sampling. |
| `public static CustomMonitor<TValue> AddCustomMonitor<TArgument, TValue>(string id, Func<TArgument, TValue> callback, TArgument argument, MonitorType type = MonitorType.Quantity)` | Register a copied typed argument or borrowed reference; tuples carry multiple arguments. |
| `public static TValue GetCustomMonitor<TValue>(CustomMonitor<TValue> monitor)` | Invoke the exact current provider outside the lock on the caller thread. |
| `public static bool HasCustomMonitor(string id)` | Test literal ordinal name membership. |
| `public static void RemoveCustomMonitor(string id)` | Invalidate the registration and release captures. |
| `public static string[] GetCustomMonitorNames()` | Copy names in registration order. |
| `public static MonitorType[] GetCustomMonitorTypes()` | Copy units in the same registration order. |
| `public static ulong GetMonitorModificationTime()` | Last successful mutation's elapsed microsecond tick. |

<a id="addcustommonitor"></a>
### AddCustomMonitor

Returns a typed [CustomMonitor](Performance.CustomMonitor.Generic.md) identity. ID/provider null throws ArgumentNullException; undefined unit throws ArgumentOutOfRangeException; duplicate ordinal ID throws InvalidOperationException. Empty and slash-delimited IDs are distinct literal identities. Capture typed arguments in a normal C# closure, or use the explicit typed-argument overload. That overload captures the argument value at registration; referenced objects remain borrowed, and tuples represent multiple arguments. Registration allocates and retains that closure until removal. Provider result precision/type is preserved.

<a id="getcustommonitor"></a>
### GetCustomMonitor

Returns the raw exact TValue result. No presentation clamping, boxing, conversion or caching occurs. Null handle throws ArgumentNullException; stale/replaced identity throws KeyNotFoundException. Provider exceptions propagate. Direct/indirect same-thread cycles throw InvalidOperationException and release the guard. Acyclic nesting and concurrent callers remain valid; the provider owns thread safety and borrowed-object lifetime.

<a id="hascustommonitor"></a>
### HasCustomMonitor

Reads name membership under the retained registry lock. Null ID throws ArgumentNullException. It does not sample a value or validate an old handle generation.

<a id="removecustommonitor"></a>
### RemoveCustomMonitor

Removes one exact ordinal ID and clears its provider/captures. Null ID throws ArgumentNullException; missing ID throws KeyNotFoundException. Application objects are not disposed, and an already-started read may finish. Reregistering creates a different handle.

<a id="getcustommonitornames"></a>
### GetCustomMonitorNames

Returns a caller-owned array. Removing/readding an ID moves it to the end. No callback runs. Concurrent registry edits between separate name/type queries may change alignment.

<a id="getcustommonitortypes"></a>
### GetCustomMonitorTypes

Returns a caller-owned typed unit array in the same registration order. [MonitorType](Performance.MonitorType.md) describes Quantity, Memory, Time and Percentage units. Separate queries require application synchronization when an atomic pair is needed.

<a id="getmonitormodificationtime"></a>
### GetMonitorModificationTime

Returns zero before mutation and then a nondecreasing successful add/remove tick in microseconds since retained diagnostics initialization. It persists across runtime sessions and ignores reads/failed mutations. Multiple mutations may share a tick; it is not a unique revision. Wall-clock changes do not affect it.

```csharp
var count = Performance.AddCustomMonitor("Game/Actors", () => actorCount);
try { int sample = Performance.GetCustomMonitor(count); }
finally { Performance.RemoveCustomMonitor(count.ID); }
```

Requires an application integer actorCount value. [CustomMonitorTests](../../tests/Electron2D.Tests/CustomMonitorTests.cs) and [runtime diagnostics](../components/runtime-diagnostics.md) record managed execution, zero warmed reads, lifetime/concurrency and native verification limits. Debugger charts and other built-in producers remain separate.

## Completed canvas submission counters

[Canvas counters](../components/canvas-counters.md) now publish successful per-viewport/global drawing records, topology/instance budgets and native geometry calls. Empty completed frames reset; skipped or failed frames retain prior values. GPU-authored activity uses explicit capacity/base-geometry budgets without readback. Owner/lifetime, backend granularity and exact verification limits are defined in that component.
